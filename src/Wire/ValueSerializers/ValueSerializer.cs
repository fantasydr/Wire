// -----------------------------------------------------------------------
//   <copyright file="ValueSerializer.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
//using System.Linq.Expressions;
using System.Reflection;
using Wire.Internal;

namespace Wire.ValueSerializers
{
    public abstract class ValueSerializer
    {
        public virtual int PreallocatedByteBufferSize => 0;

        public abstract void WriteManifest([NotNull] Stream stream, [NotNull] SerializerSession session);
        public abstract void WriteValue([NotNull] Stream stream, object value, [NotNull] SerializerSession session);
        public abstract object ReadValue([NotNull] Stream stream, [NotNull] DeserializerSession session);
        public abstract Type GetElementType();
    }
}