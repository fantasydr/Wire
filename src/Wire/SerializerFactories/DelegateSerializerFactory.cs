// -----------------------------------------------------------------------
//   <copyright file="DelegateSerializerFactory.cs" company="Asynkron HB">
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
    public class DelegateSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return type.IsSubclassOf(typeof(Delegate));
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            Wire.Helper.Dictionary<Type, ValueSerializer> typeMapping)
        {
            var os = new ObjectSerializer(type);
            typeMapping.TryAdd(type, os);
            var methodInfoSerializer = serializer.GetSerializerByType(typeof(MethodInfo));
            var preserveObjectReferences = serializer.Options.PreserveObjectReferences;

            ObjectReader Reader = delegate (Stream stream, DeserializerSession session)
            {
                var target = StreamEx.ReadObject(stream, session);
                var method = (MethodInfo)StreamEx.ReadObject(stream, session);
                var del = method.CreateDelegate(type, target);
                return del;
            };

            ObjectWriter Writer = delegate (Stream stream, object value, SerializerSession session)
            {
                var d = (Delegate)value;
                var method = d.GetMethodInfo();
                StreamEx.WriteObjectWithManifest(stream, d.Target, session);
                //less lookups, slightly faster
                StreamEx.WriteObject(stream, method, type, methodInfoSerializer, preserveObjectReferences, session);
            };

            os.Initialize(Reader, Writer);
            return os;
        }
    }
}