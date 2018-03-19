// -----------------------------------------------------------------------
//   <copyright file="SessionAwareByteArrayRequiringValueSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
// using System.Linq.Expressions;
using System.Reflection;

namespace Wire.ValueSerializers
{
    public abstract class SessionAwareByteArrayRequiringValueSerializer<TElementType> : ValueSerializer
    {
        private readonly byte _manifest;
        private readonly MethodInfo _read;
        private readonly Func<Stream, byte[], TElementType> _readCompiled;
        private readonly MethodInfo _write;
        private readonly Action<Stream, TElementType, byte[]> _writeCompiled;

        protected SessionAwareByteArrayRequiringValueSerializer(byte manifest,
            Action<Stream, TElementType, byte[]> writeStaticMethod,
            Func<Stream, byte[], TElementType> readStaticMethod)
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
            _writeCompiled(stream, (TElementType)value, session.GetBuffer(PreallocatedByteBufferSize));
        }

        public sealed override object ReadValue(Stream stream, DeserializerSession session)
        {
            return _readCompiled(stream, session.GetBuffer(PreallocatedByteBufferSize));
        }

        public sealed override Type GetElementType()
        {
            return typeof(TElementType);
        }
    }
}