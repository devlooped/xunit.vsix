#region BSD License
/* 
Copyright (c) 2010, NETFx
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.CSharp.RuntimeBinder;

namespace System.Dynamic
{
    /// <summary>
    /// Provides reflection-based dynamic syntax for objects and types. 
    /// This class provides the extension methods <see cref="AsDynamicReflection(object)"/> 
    /// and <see cref="AsDynamicReflection(Type)"/> as entry points.
    /// </summary>
    /// <nuget id="netfx-System.Dynamic.Reflection" />
    static partial class DynamicReflection
    {
        /// <summary>
        /// Provides dynamic syntax for accessing the given object members.
        /// </summary>
        /// <nuget id="netfx-System.Dynamic.Reflection" />
        /// <param name="obj" this="true">The object to access dinamically</param>
        public static dynamic AsDynamicReflection(this object obj)
        {
            if (obj == null)
                return null;

            return new DynamicReflectionObject(obj);
        }

        /// <summary>
        /// Provides dynamic syntax for accessing the given type members.
        /// </summary>
        /// <nuget id="netfx-System.Dynamic.Reflection" />
        /// <param name="type" this="true">The type to access dinamically</param>
        public static dynamic AsDynamicReflection(this Type type)
        {
            if (type == null)
                return null;

            return new DynamicReflectionObject(type);
        }

        /// <summary>
        /// Converts the type to a <see cref="TypeParameter"/> that 
        /// the reflection dynamic must use to make a generic 
        /// method invocation.
        /// </summary>
        /// <nuget id="netfx-System.Dynamic.Reflection" />
        /// <param name="type" this="true">The type to convert</param>
        public static TypeParameter AsGenericTypeParameter(this Type type)
        {
            return new TypeParameter(type);
        }

        class DynamicReflectionObject : DynamicObject
        {
            static readonly BindingFlags s_flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            static readonly MethodInfo s_castMethod = typeof(DynamicReflectionObject).GetMethod("Cast", BindingFlags.Static | BindingFlags.NonPublic);
            object _target;
            Type _targetType;

            public DynamicReflectionObject(object target)
            {
                _target = target;
                _targetType = target.GetType();
            }

            public DynamicReflectionObject(Type type)
            {
                _target = null;
                _targetType = type;
            }

            public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
            {
                if (!base.TryInvokeMember(binder, args, out result))
                {
                    var memberName = (binder.Name == "ctor" || binder.Name == "cctor") ? "." + binder.Name : binder.Name;
                    var method = FindBestMatch(binder, memberName, args);
                    if (method != null)
                    {
                        if (binder.Name == "ctor")
                        {
                            var instance = _target;
                            if (instance == null)
                                instance = FormatterServices.GetSafeUninitializedObject(_targetType);

                            result = Invoke(method, instance, args);
                            result = instance.AsDynamicReflection();
                        }
                        else
                        {
                            result = AsDynamicIfNecessary(Invoke(method, _target, args));
                        }

                        return true;
                    }
                }

                result = default(object);
                return false;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                if (!base.TryGetMember(binder, out result))
                {
                    var field = _targetType.GetField(binder.Name, s_flags);
                    var baseType = _targetType.BaseType;
                    while (field == null && baseType != null)
                    {
                        field = baseType.GetField(binder.Name, s_flags);
                        baseType = baseType.BaseType;
                    }

                    if (field != null)
                    {
                        result = AsDynamicIfNecessary(field.GetValue(_target));
                        return true;
                    }

                    var getter = FindBestMatch(binder, "get_" + binder.Name, new object[0]);
                    if (getter != null)
                    {
                        result = AsDynamicIfNecessary(getter.Invoke(_target, null));
                        return true;
                    }
                }

                // \o/ If nothing else works, and the member is "target", return our target.
                if (binder.Name == "target")
                {
                    result = _target;
                    return true;
                }

                result = default(object);
                return false;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                if (!base.TrySetMember(binder, value))
                {
                    var field = _targetType.GetField(binder.Name, s_flags);
                    var baseType = _targetType.BaseType;
                    while (field == null && baseType != null)
                    {
                        field = baseType.GetField(binder.Name, s_flags);
                        baseType = baseType.BaseType;
                    }

                    if (field != null)
                    {
                        field.SetValue(_target, value);
                        return true;
                    }

                    var setter = FindBestMatch(binder, "set_" + binder.Name, new[] { value });
                    if (setter != null)
                    {
                        setter.Invoke(_target, new[] { value });
                        return true;
                    }
                }

                return false;
            }

            public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
            {
                if (!base.TryGetIndex(binder, indexes, out result))
                {
                    var indexer = FindBestMatch(binder, "get_Item", indexes);
                    if (indexer != null)
                    {
                        result = AsDynamicIfNecessary(indexer.Invoke(_target, indexes));
                        return true;
                    }
                }

                result = default(object);
                return false;
            }

            public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
            {
                if (!base.TrySetIndex(binder, indexes, value))
                {
                    var args = indexes.Concat(new[] { value }).ToArray();
                    var indexer = FindBestMatch(binder, "set_Item", args);
                    if (indexer != null)
                    {
                        indexer.Invoke(_target, args);
                        return true;
                    }
                }

                return false;
            }

            public override bool TryConvert(ConvertBinder binder, out object result)
            {
                try
                {
                    result = s_castMethod.MakeGenericMethod(binder.Type).Invoke(null, new[] { _target });
                    return true;
                }
                catch (Exception) { }

                if (_target is IConvertible convertible)
                {
                    try
                    {
                        result = Convert.ChangeType(convertible, binder.Type);
                        return true;
                    }
                    catch (Exception) { }
                }


                result = default(object);
                return false;
            }

            static object Invoke(IInvocable method, object instance, object[] args)
            {
                var finalArgs = args.Where(x => !(x is TypeParameter)).Select(UnboxDynamic).ToArray();
                var refArgs = new Dictionary<int, RefValue>();
                var outArgs = new Dictionary<int, OutValue>();
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    if (method.Parameters[i].ParameterType.IsByRef)
                    {
                        if (finalArgs[i] is RefValue refArg)
                            refArgs[i] = refArg;
                        else if (finalArgs[i] is OutValue outArg)
                            outArgs[i] = outArg;
                    }
                }

                foreach (var refArg in refArgs)
                {
                    finalArgs[refArg.Key] = refArg.Value.Value;
                }
                foreach (var outArg in outArgs)
                {
                    finalArgs[outArg.Key] = null;
                }

                var result = method.Invoke(instance, finalArgs);

                foreach (var refArg in refArgs)
                {
                    refArg.Value.Value = finalArgs[refArg.Key];
                }
                foreach (var outArg in outArgs)
                {
                    outArg.Value.Value = finalArgs[outArg.Key];
                }

                return result;
            }

            /// <summary>
            /// Converts dynamic objects to object, which may cause unboxing 
            /// of the wrapped dynamic such as in our own DynamicReflectionObject type.
            /// </summary>
            static object UnboxDynamic(object maybeDynamic)
            {
                if (maybeDynamic is not DynamicObject dyn)
                    return maybeDynamic;

                var binder = (ConvertBinder)Microsoft.CSharp.RuntimeBinder.Binder.Convert(CSharpBinderFlags.ConvertExplicit, typeof(object), typeof(DynamicReflectionObject));
                //var site = CallSite<Func<CallSite, object, object>>.Create(binder);
                object result;
                dyn.TryConvert(binder, out result);

                return result;
            }

            IInvocable FindBestMatch(DynamicMetaObjectBinder binder, string memberName, object[] args)
            {
                var finalArgs = args.Where(x => !(x is TypeParameter)).Select(UnboxDynamic).ToArray();
                var genericTypeArgs = new List<Type>();

                if (binder is InvokeBinder || binder is InvokeMemberBinder)
                {
                    IEnumerable typeArgs = binder.AsDynamicReflection().TypeArguments;
                    genericTypeArgs.AddRange(typeArgs.Cast<Type>());
                    genericTypeArgs.AddRange(args.OfType<TypeParameter>().Select(x => x.Type));
                }

                var method = FindBestMatch(binder, finalArgs, genericTypeArgs, _targetType
                    .GetMethods(s_flags)
                    .Where(x => x.Name == memberName && x.GetParameters().Length == finalArgs.Length)
                    .Select(x => new MethodInvocable(x)));

                if (method == null)
                {
                    // Fallback to explicitly implemented members.
                    method = FindBestMatch(binder, finalArgs, genericTypeArgs, _targetType
                        .GetInterfaces()
                        .SelectMany(
                            iface => _targetType
                                .GetInterfaceMap(iface)
                                .TargetMethods.Select(x => new { Interface = iface, Method = x }))
                        .Where(x =>
                            x.Method.GetParameters().Length == finalArgs.Length &&
                            x.Method.Name.Replace(x.Interface.FullName.Replace('+', '.') + ".", "") == memberName)
                        .Select(x => (IInvocable)new MethodInvocable(x.Method))
                        .Concat(_targetType.GetConstructors(s_flags)
                            .Where(x => x.Name == memberName && x.GetParameters().Length == finalArgs.Length)
                            .Select(x => new ConstructorInvocable(x)))
                        .Distinct());
                }

                if (method != null && method is MethodInvocable methodInvocable && methodInvocable.Method.IsGenericMethodDefinition)
                {
                    method = new MethodInvocable(methodInvocable.Method.MakeGenericMethod(genericTypeArgs.ToArray()));
                }

                return method;
            }

            IInvocable FindBestMatch(DynamicMetaObjectBinder binder, object[] args, List<Type> genericArgs, IEnumerable<IInvocable> candidates)
            {
                var result = FindBestMatchImpl(binder, args, genericArgs, candidates, MatchingStyle.ExactType);
                if (result == null)
                    result = FindBestMatchImpl(binder, args, genericArgs, candidates, MatchingStyle.AssignableFrom);
                if (result == null)
                    result = FindBestMatchImpl(binder, args, genericArgs, candidates, MatchingStyle.ExactTypeGenericHint);
                if (result == null)
                    result = FindBestMatchImpl(binder, args, genericArgs, candidates, MatchingStyle.AssignableFromGenericHint);

                return result;
            }

            /// <summary>
            /// Finds the best match among the candidates.
            /// </summary>
            /// <param name="binder">The binder that is requesting the match.</param>
            /// <param name="args">The args passed in to the invocation.</param>
            /// <param name="genericArgs">The generic args if any.</param>
            /// <param name="candidates">The candidate methods to use for the match..</param>
            /// <param name="matching">if set to <c>MatchingStyle.AssignableFrom</c>, uses a more lax matching approach for arguments, with IsAssignableFrom instead of == for arg type, 
            /// and <c>MatchingStyle.GenericTypeHint</c> tries to use the generic arguments as type hints if they match the # of args.</param>
            IInvocable FindBestMatchImpl(DynamicMetaObjectBinder binder, object[] args, List<Type> genericArgs, IEnumerable<IInvocable> candidates, MatchingStyle matching)
            {
                dynamic dynamicBinder = binder.AsDynamicReflection();
                for (int i = 0; i < args.Length; i++)
                {
                    var index = i;
                    if (args[index] != null)
                    {
                        switch (matching)
                        {
                            case MatchingStyle.ExactType:
                                candidates = candidates.Where(x => x.Parameters[index].ParameterType.IsAssignableFrom(GetArgumentType(args[index])));
                                break;
                            case MatchingStyle.AssignableFrom:
                                candidates = candidates.Where(x => x.Parameters[index].ParameterType.IsEquivalentTo(GetArgumentType(args[index])));
                                break;
                            case MatchingStyle.ExactTypeGenericHint:
                                candidates = candidates.Where(x => x.Parameters.Count == genericArgs.Count &&
                                    x.Parameters[index].ParameterType.IsEquivalentTo(genericArgs[index]));
                                break;
                            case MatchingStyle.AssignableFromGenericHint:
                                candidates = candidates.Where(x => x.Parameters.Count == genericArgs.Count &&
                                    x.Parameters[index].ParameterType.IsAssignableFrom(genericArgs[index]));
                                break;
                            default:
                                break;
                        }
                    }

                    IEnumerable enumerable = dynamicBinder.ArgumentInfo;
                    // The binder has the extra argument info for the "this" parameter at the beginning.
                    if (enumerable.Cast<object>().ToList()[index + 1].AsDynamicReflection().IsByRef)
                        candidates = candidates.Where(x => x.Parameters[index].ParameterType.IsByRef);

                    // Only filter by matching generic argument count if the generics isn't being used as parameter type hints.
                    if (genericArgs.Count > 0 && matching != MatchingStyle.AssignableFromGenericHint && matching != MatchingStyle.ExactTypeGenericHint)
                        candidates = candidates.Where(x => x.IsGeneric && x.GenericParameters == genericArgs.Count);
                }

                return candidates.FirstOrDefault();
            }

            static Type GetArgumentType(object arg)
            {
                if (arg is RefValue || arg is OutValue)
                    return arg.GetType().GetGenericArguments()[0].MakeByRefType();
                if (arg is DynamicReflectionObject)
                    return ((DynamicReflectionObject)arg)._target.GetType();

                return arg.GetType();
            }

            object AsDynamicIfNecessary(object value)
            {
                if (value == null)
                    return value;

                var type = value.GetType();
                if (type.IsClass && type != typeof(string))
                    return value.AsDynamicReflection();

                return value;
            }

            static T Cast<T>(object target)
            {
                return (T)target;
            }

            enum MatchingStyle
            {
                ExactType,
                AssignableFrom,
                ExactTypeGenericHint,
                AssignableFromGenericHint,
            }

            interface IInvocable
            {
                bool IsGeneric { get; }
                int GenericParameters { get; }
                IList<ParameterInfo> Parameters { get; }
                object Invoke(object obj, object[] parameters);
            }

            class MethodInvocable : IInvocable
            {
                MethodInfo _method;
                Lazy<IList<ParameterInfo>> _parameters;

                public MethodInvocable(MethodInfo method)
                {
                    _method = method;
                    _parameters = new Lazy<IList<ParameterInfo>>(() => _method.GetParameters());
                }

                public object Invoke(object obj, object[] parameters)
                {
                    return _method.Invoke(obj, parameters);
                }

                public IList<ParameterInfo> Parameters
                {
                    get { return _parameters.Value; }
                }

                public MethodInfo Method { get { return _method; } }

                public bool IsGeneric { get { return _method.IsGenericMethodDefinition; } }

                public int GenericParameters { get { return _method.GetGenericArguments().Length; } }
            }

            class ConstructorInvocable : IInvocable
            {
                ConstructorInfo _ctor;
                Lazy<IList<ParameterInfo>> _parameters;

                public ConstructorInvocable(ConstructorInfo ctor)
                {
                    _ctor = ctor;
                    _parameters = new Lazy<IList<ParameterInfo>>(() => _ctor.GetParameters());
                }

                public object Invoke(object obj, object[] parameters)
                {
                    return _ctor.Invoke(obj, parameters);
                }

                public IList<ParameterInfo> Parameters
                {
                    get { return _parameters.Value; }
                }

                public bool IsGeneric { get { return false; } }

                public int GenericParameters { get { return 0; } }
            }
        }
    }
}