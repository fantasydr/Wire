// -----------------------------------------------------------------------
//   <copyright file="SessionIgnorantValueSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
//using System.Linq.Expressions;
using System.Reflection;

namespace Wire.ValueSerializers
{
    public abstract class SessionIgnorantValueSerializer<TElementType> : ValueSerializer
    {
        private readonly byte _manifest;
        private readonly MethodInfo _read;
        private readonly Func<Stream, TElementType> _readCompiled;
        private readonly MethodInfo _write;
        private readonly Action<Stream, TElementType> _writeCompiled;

        protected SessionIgnorantValueSerializer(byte manifest,
            Action<Stream, TElementType> writeStaticMethod,
            Func<Stream, TElementType> readStaticMethod)
        {
            _manifest = manifest;
            _writeCompiled = writeStaticMethod;
            _readCompiled = readStaticMethod;
        }

        public sealed override void WriteManifest(Stream stream, SerializerSession session)
        {
            stream.WriteByte(_manifest);
        }

        public sealed override void WriteValue(Stream stream, object value, SerializerSession session)
        {
            _writeCompiled(stream, (TElementType)value);
        }

        public sealed override object ReadValue(Stream stream, DeserializerSession session)
        {
            return _readCompiled(stream);
        }

        public sealed override Type GetElementType()
        {
            return typeof(TElementType);
        }
    }
}