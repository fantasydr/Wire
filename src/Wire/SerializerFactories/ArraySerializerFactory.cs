// -----------------------------------------------------------------------
//   <copyright file="ArraySerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
// using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class ArraySerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type) => TypeEx.IsOneDimensionalArray(type);

        public override bool CanDeserialize(Serializer serializer, Type type) => CanSerialize(serializer, type);

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            Wire.Helper.Dictionary<Type, ValueSerializer> typeMapping)
        {
            var arraySerializer = new ObjectSerializer(type);

            var elementType = type.GetElementType();
            var elementSerializer = serializer.GetSerializerByType(elementType);
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            ObjectReader Reader = delegate (Stream stream, DeserializerSession session)
            {
                //Stream stream, DeserializerSession session, bool preserveObjectReferences
                var length = StreamEx.ReadInt32(stream, session);
                var array = Array.CreateInstance(elementType, length);
                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(array);
                }
                for (var i = 0; i < length; i++)
                {
                    var value = StreamEx.ReadObject(stream, session);
                    array.SetValue(value, i);
                }
                return array;
            };

            ObjectWriter Writer = delegate (Stream stream, object arr, SerializerSession session)
            {
                //T[] array, Stream stream, Type elementType, ValueSerializer elementSerializer, SerializerSession session, bool preserveObjectReferences
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(arr);
                }

                Array array = arr as Array;
                Int32Serializer.WriteValueImpl(stream, array.Length, session);
                foreach (var value in array)
                {
                    StreamEx.WriteObject(stream, value, elementType, elementSerializer, preserveObjectReferences, session);
                }
            };

            arraySerializer.Initialize(Reader, Writer);
            typeMapping.TryAdd(type, arraySerializer);
            return arraySerializer;
        }
    }
}