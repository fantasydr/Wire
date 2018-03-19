﻿// -----------------------------------------------------------------------
//   <copyright file="HashSetSerializerFactory.cs" company="Asynkron HB">
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
    public class HashSetSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type) => IsInterface(type);

        private static bool IsInterface(Type type)
        {
            return type.IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(Wire.Helper.HashSet<>);
        }

        public override bool CanDeserialize(Serializer serializer, Type type) => IsInterface(type);

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            Wire.Helper.Dictionary<Type, ValueSerializer> typeMapping)
        {
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;
            var ser = new ObjectSerializer(type);
            typeMapping.TryAdd(type, ser);
            var elementType = type.GetGenericArguments()[0];
            var elementSerializer = serializer.GetSerializerByType(elementType);
            var readGeneric = GetType().GetMethod("ReadHashSet", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(elementType);
            var writeGeneric = GetType().GetMethod("WriteHashSet", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(elementType);

            ObjectReader Reader = delegate (Stream stream, DeserializerSession session)
            {
                var res = readGeneric.Invoke(null, new object[] { stream, session, preserveObjectReferences });
                return res;
            };

            ObjectWriter Writer = delegate (Stream stream, object obj, SerializerSession session)
            {
                writeGeneric.Invoke(null, new[] { obj, stream, session, elementType, elementSerializer, preserveObjectReferences });
            };

            ser.Initialize(Reader, Writer);

            return ser;
        }

        private static Wire.Helper.HashSet<T> ReadHashSet<T>(Stream stream, DeserializerSession session,bool preserveObjectReferences)
        {
            var set = new Wire.Helper.HashSet<T>();
            if (preserveObjectReferences)
            {
                session.TrackDeserializedObject(set);
            }
            var count = StreamEx.ReadInt32(stream, session);
            for (var i = 0; i < count; i++)
            {
                var item = (T)StreamEx.ReadObject(stream, session);
                set.Add(item);
            }
            return set;
        }

        private static void WriteHashSet<T>(Wire.Helper.HashSet<T> set, Stream stream, SerializerSession session, Type elementType,
            ValueSerializer elementSerializer, bool preserveObjectReferences)
        {
            if (preserveObjectReferences)
            {
                session.TrackSerializedObject(set);
            }
            // ReSharper disable once PossibleNullReferenceException
            Int32Serializer.WriteValueImpl(stream, set.Count, session);
            foreach (var item in set)
            {
                StreamEx.WriteObject(stream, item,elementType,elementSerializer, preserveObjectReferences, session);
            }
        }
    }
}