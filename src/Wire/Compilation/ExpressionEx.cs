// -----------------------------------------------------------------------
//   <copyright file="ExpressionEx.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq.Expressions;
using System.Reflection;
using Wire.Extensions;

namespace Wire.Compilation
{
    public static class ExpressionEx
    {
        public static ConstantExpression ToConstant(object self)
        {
            return Expression.Constant(self);
        }

        public static Expression GetNewExpression(Type type)
        {
            if (type.IsValueType)
            {
                var x = Expression.Constant(Activator.CreateInstance(type));
                var convert = Expression.Convert(x, typeof(object));
                return convert;
            }
#if NET45
            var defaultCtor = type.GetConstructor(new Type[] {});
            var il = defaultCtor?.GetMethodBody()?.GetILAsByteArray();
            var sideEffectFreeCtor = il != null && il.Length <= 8; //this is the size of an empty ctor
            if (sideEffectFreeCtor)
            {
                //the ctor exists and the size is empty. lets use the New operator
                return Expression.New(defaultCtor);
            }
#endif
            var emptyObjectMethod = typeof(TypeEx).GetMethod("TypeEx.GetEmptyObject");
            var emptyObject = Expression.Call(null, emptyObjectMethod, ExpressionEx.ToConstant(type));

            return emptyObject;
        }
    }
}