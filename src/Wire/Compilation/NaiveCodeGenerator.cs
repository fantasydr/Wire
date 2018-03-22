// -----------------------------------------------------------------------
//   <copyright file="DefaultCodeGenerator.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Wire.Extensions;
using Wire.Internal;
using Wire.ValueSerializers;

namespace Wire.Compilation
{
    public class NaiveCodeGenerator : ICodeGenerator
    {
        public void BuildSerializer([NotNull] Serializer serializer, [NotNull] ObjectSerializer objectSerializer)
        {
            var type = objectSerializer.Type;
            var fields = ReflectionEx.GetFieldInfosForType(type);
            int preallocatedBufferSize;
            var writer = GetFieldsWriter(serializer, fields, out preallocatedBufferSize);
            var reader = GetFieldsReader(serializer, fields, type);

            objectSerializer.Initialize(reader, writer, preallocatedBufferSize);
        }

        private ObjectReader GetFieldsReader([NotNull] Serializer serializer, [NotNull] FieldInfo[] fields,
                                             [NotNull] Type type)
        {
            var serializers = fields.Select(field => serializer.GetSerializerByType(field.FieldType)).ToArray();
            var preallocatedBufferSize = serializers.Length != 0 ? serializers.Max(s => s.PreallocatedByteBufferSize) : 0;

            // predefine the readers
            var readers = new ObjectReader[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                var s = serializers[i];
                if (!serializer.Options.VersionTolerance && TypeEx.IsWirePrimitive(field.FieldType))
                {
                    // TODO: optimize for primitive types
                    //Only optimize if property names are not included.
                    //if they are included, we need to be able to skip past unknown property data
                    //e.g. if sender have added a new property that the receiveing end does not yet know about
                    //which we cannot do w/o a manifest
                    readers[i] = s.ReadValue;
                }
                else
                {
                    readers[i] = StreamEx.ReadObject;
                }
            }

            return delegate (Stream stream, DeserializerSession session)
            {
                object target = Activator.CreateInstance(type);
                if (serializer.Options.PreserveObjectReferences)
                {
                    session.TrackDeserializedObject(target);
                }

                var PreallocatedByteBuffer = session.GetBuffer(preallocatedBufferSize);

                for (var i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    var reader = readers[i];

                    // skip field.IsInitOnly
                    field.SetValue(target, reader(stream, session));
                }

                return target;
            };
        }

        //this generates a FieldWriter that writes all fields by unrolling all fields and calling them individually
        //no loops involved
        private ObjectWriter GetFieldsWriter([NotNull] Serializer serializer, [NotNull] IEnumerable<FieldInfo> fields,
                                             out int preallocatedBufferSize)
        {
            var fieldsArray = fields.ToArray();
            var serializers = fieldsArray.Select(field => serializer.GetSerializerByType(field.FieldType)).ToArray();

            preallocatedBufferSize = serializers.Length != 0 ? serializers.Max(s => s.PreallocatedByteBufferSize) : 0;
            var _preallocatedBufferSize  = preallocatedBufferSize;

            var writers = new ObjectWriter[fieldsArray.Length];
            for (var i = 0; i < fieldsArray.Length; i++)
            {
                var field = fieldsArray[i];
                //get the serializer for the type of the field
                var valueSerializer = serializers[i];
                //runtime Get a delegate that reads the content of the given field

                //if the type is one of our special primitives, ignore manifest as the content will always only be of this type
                if (!serializer.Options.VersionTolerance && TypeEx.IsWirePrimitive(field.FieldType))
                {
                    writers[i] = delegate (Stream stream, object obj, SerializerSession session)
                    {
                        var readField = field.GetValue(obj);
                        // TODO: optimize for primitive types
                        valueSerializer.WriteValue(stream, readField, session);

                    };
                }
                else
                {
                    var valueType = field.FieldType;
                    if (TypeEx.IsNullable(field.FieldType))
                    {
                        var nullableType = TypeEx.GetNullableElement(field.FieldType);
                        valueSerializer = serializer.GetSerializerByType(nullableType);
                        valueType = nullableType;
                    }

                    writers[i] = delegate (Stream stream, object obj, SerializerSession session)
                    {
                        var readField = field.GetValue(obj);
                        StreamEx.WriteObject(stream, readField, valueType, valueSerializer, false, session);
                    };
                }
            }


            return delegate (Stream stream, object obj, SerializerSession session)
            {
                if (serializer.Options.PreserveObjectReferences)
                {
                    session.TrackSerializedObject(obj);
                }

                session.GetBuffer(_preallocatedBufferSize);
                foreach(var writer in writers)
                {
                    writer(stream, obj, session);
                }
            };
        }
    }
}