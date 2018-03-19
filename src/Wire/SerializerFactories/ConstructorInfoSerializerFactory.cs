// -----------------------------------------------------------------------
//   <copyright file="ConstructorInfoSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
// using System.Collections.Concurrent;
using System.IO;
// using System.Linq;
using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class ConstructorInfoSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return type.IsSubclassOf(typeof(ConstructorInfo));
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

            ObjectReader Reader = delegate (Stream stream, DeserializerSession session)
            {
                var owner = StreamEx.ReadObject(stream, session) as Type;
                var arguments = StreamEx.ReadObject(stream, session) as Type[];

#if NET45
                var ctor = owner.GetConstructor(arguments);
                return ctor;
#else
                return null;
#endif
            };

            ObjectWriter Writer = delegate (Stream stream, object obj, SerializerSession session)
            {
                var ctor = (ConstructorInfo)obj;
                var owner = ctor.DeclaringType;
                var arguments = ctor.GetParameters().Select(p => p.ParameterType).ToArray();
                StreamEx.WriteObjectWithManifest(stream, owner, session);
                StreamEx.WriteObjectWithManifest(stream, arguments, session);
            };

            os.Initialize(Reader, Writer);

            return os;
        }
    }
}