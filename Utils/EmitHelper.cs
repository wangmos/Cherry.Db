using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Cherry.Db.Utils
{
    internal class EmitHelper
    {
        internal static Action<TTarget, TValue> CreateSetter<TTarget, TValue>(PropertyInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (!info.CanWrite)
                throw new ArgumentException("Can not be write", info.Name);

            var method = info.GetSetMethod(true);

            var dm = new DynamicMethod(string.Empty, null,
                new[] { typeof(TTarget), typeof(TValue) }, typeof(TTarget), true);

            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);//实例对象
            il.Emit(OpCodes.Ldarg_1);//值

            //类型不相等
            if (info.PropertyType != typeof(TValue))
            {
                //属性是值类型
                if (info.PropertyType.IsValueType)
                {
                    if (!typeof(TValue).IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, info.PropertyType);//拆箱
                    }
                    else throw new ArgumentException($"{info.Name}'s type[{info.PropertyType}] can not be {typeof(TValue).Name}", info.Name);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, info.PropertyType);//强制转化
                }

            }

            //引用类型的实例方法  虚函数调用
            il.EmitCall(OpCodes.Callvirt, method, null);

            il.Emit(OpCodes.Ret);

            return (Action<TTarget, TValue>)dm.CreateDelegate(typeof(Action<TTarget, TValue>));
        }

        internal static Action<TTarget, TValue> CreateSetter<TTarget, TValue>(FieldInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var dm = new DynamicMethod(string.Empty, null,
                new[] { typeof(TTarget), typeof(TValue) }, typeof(TTarget), true);

            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);//实例对象
            il.Emit(OpCodes.Ldarg_1);//值

            //类型不相等
            if (info.FieldType != typeof(TValue))
            {
                //属性是值类型
                if (info.FieldType.IsValueType)
                {
                    if (!typeof(TValue).IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, info.FieldType);//拆箱
                    }
                    else throw new ArgumentException($"{info.Name}'s type[{info.FieldType}] can not be {typeof(TValue).Name}", info.Name);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, info.FieldType);//强制转化
                }
            }

            //设置实例字段
            il.Emit(OpCodes.Stfld, info);

            il.Emit(OpCodes.Ret);

            return (Action<TTarget, TValue>)dm.CreateDelegate(typeof(Action<TTarget, TValue>));
        }

        internal static Func<TTarget, TValue> CreateGetter<TTarget, TValue>(PropertyInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            if (!info.CanRead)
                throw new ArgumentException("Can not be read", info.Name);

            var method = info.GetGetMethod(true);

            var dm = new DynamicMethod(string.Empty, typeof(TValue),
                new[] { typeof(TTarget) }, typeof(TTarget), true);

            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);//实例对象
            //引用类型的实例方法  虚函数调用
            il.EmitCall(OpCodes.Callvirt, method, null);

            //类型不相等
            if (info.PropertyType != typeof(TValue))
            {
                //属性是值类型
                if (info.PropertyType.IsValueType)
                {
                    if (!typeof(TValue).IsValueType)
                    {
                        il.Emit(OpCodes.Box, info.PropertyType);//装箱
                    }
                    else throw new ArgumentException($"{info.Name}'s type[{info.PropertyType}] can not be {typeof(TValue).Name}", info.Name);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, typeof(TValue));//强制转化
                }
            }

            il.Emit(OpCodes.Ret);

            return (Func<TTarget, TValue>)dm.CreateDelegate(typeof(Func<TTarget, TValue>));
        }

        internal static Func<TTarget, TValue> CreateGetter<TTarget, TValue>(FieldInfo info)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            var dm = new DynamicMethod(string.Empty, typeof(TValue),
                new[] { typeof(TTarget) }, typeof(TTarget), true);

            var il = dm.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);//实例对象
            il.Emit(OpCodes.Ldfld, info);//值

            //类型不相等
            if (info.FieldType != typeof(TValue))
            {
                //属性是值类型
                if (info.FieldType.IsValueType)
                {
                    if (!typeof(TValue).IsValueType)
                    {
                        il.Emit(OpCodes.Box, info.FieldType);//装箱
                    }
                    else throw new ArgumentException($"{info.Name}'s type[{info.FieldType}] can not be {typeof(TValue).Name}", info.Name);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, typeof(TValue));//强制转化
                }
            }

            il.Emit(OpCodes.Ret);

            return (Func<TTarget, TValue>)dm.CreateDelegate(typeof(Func<TTarget, TValue>));
        }

        internal static Func<object, object[], object> CreateMethod(MethodInfo info)
        { 
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");
             
            var parameterExpressions = new List<Expression>();
            var paramInfos = info.GetParameters();
            for (var i = 0; i < paramInfos.Length; i++)
            { 
                //取参数
                var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                //强转到实际类型
                var valueCast = Expression.Convert(valueObj, paramInfos[i].ParameterType);
                parameterExpressions.Add(valueCast);
            }

            //如果非静态 则将参数1强转到实际类型
            var instanceCast = info.IsStatic ? null : Expression.Convert(instanceParameter, info.ReflectedType);

            //调用函数
            var methodCall = Expression.Call(instanceCast, info, parameterExpressions);
             
            //如果没有返回值
            if (methodCall.Type == typeof(void))
            {
                var lambda =
                    Expression.Lambda<Action<object, object[]>>(methodCall, instanceParameter, parametersParameter);

                var execute = lambda.Compile();
                return (instance, parameters) =>
                {
                    execute(instance, parameters);
                    return null;
                };
            }
            else
            {
                //装箱
                var castMethodCall = Expression.Convert(methodCall, typeof(object));
                var lambda = Expression.Lambda<Func<object, object[], object>>(
                    castMethodCall, instanceParameter, parametersParameter);

                return lambda.Compile();
            }
        }
    }
}