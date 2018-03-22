// -----------------------------------------------------------------------
//   <copyright file="EnumerableSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
// using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class EnumerableSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            //TODO: check for constructor with IEnumerable<T> param

            if (!type.GetMethods().Any(m => m.Name == "AddRange" || m.Name == "Add"))
            {
                return false;
            }

            if (type.GetProperty("Count") == null)
            {
                return false;
            }

            var isGenericEnumerable = GetEnumerableType(type) != null;
            if (isGenericEnumerable)
            {
                return true;
            }

            if (typeof(ICollection).IsAssignableFrom(type))
            {
                return true;
            }

            return false;
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        private static Type GetEnumerableType(Type type)
        {
            return type
                .GetInterfaces()
                .Where(
                    intType =>
                        intType.IsGenericType &&
                        intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(intType => intType.GetGenericArguments()[0])
                .FirstOrDefault();
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            Wire.Helper.Dictionary<Type, ValueSerializer> typeMapping)
        {
            var x = new ObjectSerializer(type);
            typeMapping.TryAdd(type, x);
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            var elementType = GetEnumerableType(type) ?? typeof(object);
            var elementSerializer = serializer.GetSerializerByType(elementType);

            var countProperty = type.GetProperty("Count");
            var addRange = type.GetMethod("AddRange");
            var add = type.GetMethod("Add");

            //int CountGetter(object o) => (int) countProperty.GetValue(o);

            ObjectReader Reader = delegate (Stream stream, DeserializerSession session)
            {
                var instance = Activator.CreateInstance(type);
                if (preserveObjectReferences)
                {
                    session.TrackDeserializedObject(instance);
                }

                var count = StreamEx.ReadInt32(stream, session);
                var param = new object[1];
                if (addRange != null)
                {
                    var items = Array.CreateInstance(elementType, count);
                    for (var i = 0; i < count; i++)
                    {
                        var value = StreamEx.ReadObject(stream, session);
                        items.SetValue(value, i);
                    }
                    //HACK: this needs to be fixed, codegenerated or whatever
                    param[0] = items;
                    addRange.Invoke(instance, param);
                    return instance;
                }
                if (add != null)
                {
                    for (var i = 0; i < count; i++)
                    {
                        param[0] = StreamEx.ReadObject(stream, session);
                        add.Invoke(instance, param);
                    }
                }

                return instance;
            };

            ObjectWriter Writer = delegate (Stream stream, object o, SerializerSession session)
            {
                if (preserveObjectReferences)
                {
                    session.TrackSerializedObject(o);
                }
                Int32Serializer.WriteValueImpl(stream, (int)countProperty.GetValue(o, null), session);
                var enumerable = o as IEnumerable;
                // ReSharper disable once PossibleNullReferenceException
                foreach (var value in enumerable)
                {
                    StreamEx.WriteObject(stream, value, elementType, elementSerializer, preserveObjectReferences, session);
                }
            };

            x.Initialize(Reader, Writer);
            return x;
        }
    }
}