// -----------------------------------------------------------------------
//   <copyright file="DefaultDictionarySerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
// using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class DefaultDictionarySerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type) => IsDictionary(type);

        private static bool IsDictionary(Type type)
        {
            return type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public override bool CanDeserialize(Serializer serializer, Type type) => IsDictionary(type);

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            Wire.Helper.Dictionary<Type, ValueSerializer> typeMapping)
        {
            var ser = new ObjectSerializer(type);
            typeMapping.TryAdd(type, ser);
            var elementSerializer = serializer.GetSerializerByType(typeof(DictionaryEntry));
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            ObjectReader Reader = delegate (Stream stream, DeserializerSession session)
            {
                var count = StreamEx.ReadInt32(stream, session);
                var instance = (IDictionary)Activator.CreateInstance(type, count);
                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(instance);
                }

                for (var i = 0; i < count; i++)
                {
                    var entry = (DictionaryEntry)StreamEx.ReadObject(stream, session);
                    instance.Add(entry.Key, entry.Value);
                }
                return instance;
            };

            ObjectWriter Writer = delegate (Stream stream, object obj, SerializerSession session)
            {
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(obj);
                }
                var dict = obj as IDictionary;
                // ReSharper disable once PossibleNullReferenceException
                Int32Serializer.WriteValueImpl(stream, dict.Count, session);
                foreach (DictionaryEntry item in dict)
                {
                    StreamEx.WriteObject(stream, item, typeof(DictionaryEntry), elementSerializer, serializer.Options.PreserveObjectReferences, session);
                    // elementSerializer.WriteValue(stream,item,session);
                }
            };

            ser.Initialize(Reader, Writer);

            return ser;
        }
    }
}