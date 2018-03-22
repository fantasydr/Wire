// -----------------------------------------------------------------------
//   <copyright file="LinkedListSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
// using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class LinkedListSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LinkedList<>);
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            Wire.Helper.Dictionary<Type, ValueSerializer> typeMapping)
        {
            var arraySerializer = new ObjectSerializer(type);

            var elementType = type.GetGenericArguments()[0];
            var elementSerializer = serializer.GetSerializerByType(elementType);
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            // TODO: optimize for primitive types
            var llistType = typeof(LinkedList<>).MakeGenericType(elementType);
            var addLast = llistType.GetMethod("AddLast");

            ObjectReader Reader = delegate (Stream stream, DeserializerSession session)
            {
                //Stream stream, DeserializerSession session, bool preserveObjectReferences
                var length = StreamEx.ReadInt32(stream, session);
                var llist = Activator.CreateInstance(llistType);
                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(llist);
                }
                var param = new object[1];
                for (var i = 0; i < length; i++)
                {
                    param[0] = StreamEx.ReadObject(stream, session);
                    addLast.Invoke(llist, param);
                }
                return llist;
            };

            ObjectWriter Writer = delegate (Stream stream, object arr, SerializerSession session)
            {
                //T[] array, Stream stream, Type elementType, ValueSerializer elementSerializer, SerializerSession session, bool preserveObjectReferences
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(arr);
                }

                var llist = arr as System.Collections.ICollection;
                Int32Serializer.WriteValueImpl(stream, llist.Count, session);
                foreach (var value in llist)
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