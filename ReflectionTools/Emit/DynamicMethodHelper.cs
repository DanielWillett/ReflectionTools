using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif

namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Offers a easier API to create dynamic methods from a <see langword="delegate"/> type.
/// </summary>
public static class DynamicMethodHelper
{
    /// <summary>
    /// Easily create a new static dynamic method with a delegate type.
    /// </summary>
    /// <typeparam name="TDelegateType">The type of a delegate matching the desired signature.</typeparam>
    /// <param name="name">Display name of the function.</param>
    /// <param name="initLocals">If local variables should be initialized to zero/null/default. This is the default behavior.</param>
    /// <returns>A wrapper for <see cref="DynamicMethod"/> that allows a type-safe way to get the delegate when you're done.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static DynamicMethodInfo<TDelegateType> Create<TDelegateType>(string name, Type? owningType = null, bool initLocals = true, IAccessor? accessor = null) where TDelegateType : Delegate
    {
        owningType ??= new StackFrame(1, false).GetMethod()?.DeclaringType;
        accessor ??= Accessor.Active;

        accessor.GetDelegateSignature<TDelegateType>(out Type returnType, out ParameterInfo[] parameters);

        Type[] paramTypes = new Type[parameters.Length];
        for (int i = 0; i < paramTypes.Length; ++i)
            paramTypes[i] = parameters[i].ParameterType;

        accessor.GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions conv);

        DynamicMethod mtd = new DynamicMethod(name, attr, conv, returnType, paramTypes, owningType ?? typeof(DynamicMethodHelper), true)
        {
            InitLocals = initLocals
        };

        return new DynamicMethodInfo<TDelegateType>(mtd, parameters, accessor);
    }

}

/// <summary>
/// Wrapper helper for <see cref="DynamicMethod"/> that allows type-safe accessing of a 
/// </summary>
/// <typeparam name="TDelegateType"></typeparam>
public readonly struct DynamicMethodInfo<TDelegateType> where TDelegateType : Delegate
{
    private readonly IAccessor _accessor;

    /// <summary>
    /// The underlying dynamic method.
    /// </summary>
    public DynamicMethod Method { get; }

    /// <summary>
    /// Create a <see cref="DynamicMethodInfo{TDelegateType}"/> that wraps <paramref name="mtd"/>.
    /// </summary>
    /// <remarks>This will declare all parameters based on the delegate's parameter names and attributes.</remarks>
    public DynamicMethodInfo(DynamicMethod mtd, IAccessor? accessor = null) : this(mtd, (accessor ??= Accessor.Active).GetParameters<TDelegateType>(), accessor) { }
    internal DynamicMethodInfo(DynamicMethod mtd, ParameterInfo[]? predefinedParams, IAccessor accessor)
    {
        Method = mtd;
        _accessor = accessor;
        if (predefinedParams == null)
            return;

        for (int i = 0; i < predefinedParams.Length; ++i)
        {
            ParameterInfo param = predefinedParams[i];
            mtd.DefineParameter(i + 1, param.Attributes, param.Name);
        }
    }

    /// <summary>
    /// Get an <see cref="IOpCodeEmitter"/> for <see cref="Method"/>.
    /// </summary>
    /// <param name="debuggable">Shows debug logging as the method generates.</param>
    /// <param name="addBreakpoints">Shows debug logging as the method executes.</param>
    /// <param name="streamSize">The size of the MSIL stream, in bytes.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public IOpCodeEmitter GetEmitter(bool debuggable = false, bool addBreakpoints = false, int streamSize = 64)
    {
        return debuggable || addBreakpoints
            ? new DebuggableEmitter(new RootEmitterWrapper((ILGeneratorEmitter)Method.GetILGenerator(streamSize)), Method, accessor: _accessor) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : new RootEmitterWrapper((ILGeneratorEmitter)Method.GetILGenerator(streamSize));
    }

    /// <summary>
    /// Builds <see cref="Method"/> and returns a delegate that can be invoked.
    /// </summary>
    [EndsEmitter]
    public TDelegateType CreateDelegate()
    {
        return (TDelegateType)Method.CreateDelegate(typeof(TDelegateType));
    }

    /// <summary>
    /// Builds <see cref="Method"/> and returns a delegate that can be invoked.
    /// </summary>
    [EndsEmitter]
    public TDelegateType CreateDelegate(object? target)
    {
        return target == null
            ? CreateDelegate()
            : (TDelegateType)Method.CreateDelegate(typeof(TDelegateType), target);
    }

    /// <summary>
    /// Override the default parameter definition of a parameter.
    /// </summary>
    /// <param name="paramNumFromOne">The index of the parameter, starting at one.</param>
    public void DefineParameter(int paramNumFromOne, ParameterAttributes attributes, string? parameterName)
    {
        Method.DefineParameter(paramNumFromOne, attributes, parameterName);
    }
}