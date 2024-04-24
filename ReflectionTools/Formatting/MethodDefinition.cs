using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Collections.ObjectModel;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Represents a shell of a method for formatting purposes.
/// </summary>
public class MethodDefinition : IMemberDefinition
{
    private Type? _declaringType;
    internal List<MethodParameterDefinition>? ParametersIntl;
    internal List<string>? GenDefsIntl;
    internal List<Type?>? GenValsIntl;
    private ReadOnlyCollection<MethodParameterDefinition>? _readOnlyParameters;
    private ReadOnlyCollection<string>? _readOnlyGenDefs;
    private ReadOnlyCollection<Type?>? _readOnlyGenVals;
    private string? _name;
    private bool _isConstructor;
    private bool _isStatic;
    private bool _isExtensionMethod;

    /// <summary>
    /// Name of the method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Can not set a name on a DelegateMethodDefinition.</exception>
    public string? Name
    {
        get => _name;
        set
        {
            if (!string.Equals("Invoke", value, StringComparison.Ordinal) && this is DelegateMethodDefinition)
                throw new InvalidOperationException("Can not set a name on a DelegateMethodDefinition.");

            _name = value;
        }
    }

    /// <summary>
    /// If this method is a constructor for <see cref="DeclaringType"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not create a constructor with a DelegateMethodDefinition.</exception>
    public bool IsConstructor
    {
        get => _isConstructor;
        set
        {
            if (value && this is DelegateMethodDefinition)
                throw new InvalidOperationException("Can not create a constructor with a DelegateMethodDefinition.");

            _isConstructor = value;
        }
    }

    /// <summary>
    /// Type the method is declared in.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not set a declaring type on a DelegateMethodDefinition.</exception>
    public Type? DeclaringType
    {
        get => _declaringType;
        set
        {
            if (value != null && this is DelegateMethodDefinition)
                throw new InvalidOperationException("Can not set a declaring type on a DelegateMethodDefinition.");

            _declaringType = value;
        }
    }

    /// <summary>
    /// Type the method returns, or <see langword="void"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public Type? ReturnType { get; set; }

    /// <summary>
    /// Index of the return type in <see cref="GenericDefinitions"/>.
    /// </summary>
    /// <remarks>Defaults to -1.</remarks>
    public int ReturnTypeGenericIndex { get; set; } = -1;

    /// <summary>
    /// Array of int values defining how the generic type index is manipulated as an element type.
    /// <para>
    /// Element meanings:
    /// <code>
    /// <br/>* dim = -1 = pointer
    /// <br/>* dim &gt; 0  = array of rank {n}
    /// <br/>* dim &lt; -1 = -(int){ByRefTypeMode} - 1
    /// </code>
    /// </para>
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public int[]? ReturnTypeGenericTypeElementTypes { get; set; }

    /// <summary>
    /// Actual length of <see cref="ReturnTypeGenericTypeElementTypes"/>, as it could be from the underlying array of a list.
    /// </summary>
    /// <remarks>Defaults to -1.</remarks>
    public int ReturnTypeGenericTypeElementTypesLength { get; set; } = -1;

    /// <summary>
    /// By-ref keyword of the value the method returns.
    /// </summary>
    /// <remarks>Defaults to <see cref="ByRefTypeMode.Ignore"/>.</remarks>
    public ByRefTypeMode ReturnRefTypeMode { get; set; }

    /// <summary>
    /// If the method requires an instance of <see cref="DeclaringType"/> to be invoked.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Always will be <see langword="true"/> if <see cref="IsExtensionMethod"/> is <see langword="true"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not create a static method with a DelegateMethodDefinition.</exception>
    public bool IsStatic
    {
        get => _isStatic;
        set
        {
            if (value && this is DelegateMethodDefinition)
                throw new InvalidOperationException("Can not create a static method with a DelegateMethodDefinition.");

            if (!value)
                _isExtensionMethod = false;

            _isStatic = value;
        }
    }

    /// <summary>
    /// If the method is a static extension method (the first parameter is 'this').
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Always will be <see langword="false"/> if <see cref="IsStatic"/> is <see langword="false"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not create a static extension method with a DelegateMethodDefinition.</exception>
    public bool IsExtensionMethod
    {
        get => _isExtensionMethod;
        set
        {
            if (value && this is DelegateMethodDefinition)
                throw new InvalidOperationException("Can not create a static method with a DelegateMethodDefinition.");

            if (value)
                _isStatic = true;

            _isExtensionMethod = value;
        }
    }

    /// <summary>
    /// All parameters in the method. <see langword="null"/> if no parameters are provided.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/> (parameters not specified).</remarks>
#if !NETFRAMEWORK || NET45_OR_GREATER
    public IReadOnlyList<MethodParameterDefinition>? Parameters
#else
    public ReadOnlyCollection<MethodParameterDefinition>? Parameters
#endif
    {
        get
        {
            if (_readOnlyParameters != null)
                return _readOnlyParameters;

            if (ParametersIntl == null)
                return null;

            _readOnlyParameters = new ReadOnlyCollection<MethodParameterDefinition>(ParametersIntl);

            return _readOnlyParameters;
        }
    }

    /// <summary>
    /// All generic parameter definitions in the method. <see langword="null"/> if no definitions are provided.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/> (parameters not specified).</remarks>
#if !NETFRAMEWORK || NET45_OR_GREATER
    public IReadOnlyList<string>? GenericDefinitions
#else
    public ReadOnlyCollection<string>? GenericDefinitions
#endif
    {
        get
        {
            if (_readOnlyGenDefs != null)
                return _readOnlyGenDefs;

            if (GenDefsIntl == null)
                return null;

            _readOnlyGenDefs = new ReadOnlyCollection<string>(GenDefsIntl);

            return _readOnlyGenDefs;
        }
    }

    /// <summary>
    /// All generic parameter values in the method. <see langword="null"/> if no generic parameter values are provided.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/> (parameters not specified).</remarks>
#if !NETFRAMEWORK || NET45_OR_GREATER
    public IReadOnlyList<Type?>? GenericParameters
#else
    public ReadOnlyCollection<Type?>? GenericParameters
#endif
    {
        get
        {
            if (_readOnlyGenVals != null)
                return _readOnlyGenVals;

            if (GenValsIntl == null)
                return null;

            _readOnlyGenVals = new ReadOnlyCollection<Type?>(GenValsIntl);

            return _readOnlyGenVals;
        }
    }

    /// <summary>
    /// Creates a <see cref="MethodDefinition"/> from a delegate.
    /// </summary>
    /// <param name="methodName">The name of the new method to create.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use, defaulting to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TDelegate">The delegate type to use as the starting point for this <see cref="MethodDefinition"/>.</typeparam>
    public static MethodDefinition FromDelegate<TDelegate>(string methodName, IAccessor? accessor = null) where TDelegate : Delegate
    {
        MethodInfo invokeMethod = (accessor ??= Accessor.Active).GetInvokeMethod<TDelegate>();
        return FromDelegateIntl(invokeMethod, methodName, accessor);
    }

    /// <summary>
    /// Creates a <see cref="MethodDefinition"/> from a delegate.
    /// </summary>
    /// <param name="delegateType">The delegate type to use as the starting point for this <see cref="MethodDefinition"/>. Must be a delegate type</param>
    /// <param name="methodName">The name of the new method to create.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use, defaulting to <see cref="Accessor.Active"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a delegate type.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="delegateType"/> is <see langword="null"/>.</exception>
    public static MethodDefinition FromDelegate(Type delegateType, string methodName, IAccessor? accessor = null)
    {
        if (delegateType == null)
            throw new ArgumentNullException(nameof(delegateType));

        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException("Must be a delegate type.", nameof(delegateType));

        MethodInfo invokeMethod = (accessor ??= Accessor.Active).GetInvokeMethod(delegateType);
        return FromDelegateIntl(invokeMethod, methodName, accessor);
    }

    /// <summary>
    /// Creates a <see cref="MethodDefinition"/> from an already-existing method.
    /// </summary>
    /// <param name="method">The already-existing method.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use, defaulting to <see cref="Accessor.Active"/>.</param>
    public static MethodDefinition FromMethod(MethodBase method, IAccessor? accessor = null)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        accessor ??= Accessor.Active;

        return FromMethodIntl(method, accessor);
    }

    /// <summary>
    /// Create a method definition, starting with a method name.
    /// </summary>
    public MethodDefinition(string methodName)
    {
        Name = methodName;
    }

    /// <summary>
    /// Create a constructor definition, starting with a return type and declaring type.
    /// </summary>
    public MethodDefinition(Type declaringType, bool isTypeInitializer = false)
    {
        ReturnType = declaringType;
        Name = isTypeInitializer ? ".cctor" : ".ctor";
        DeclaringType = declaringType;
        IsConstructor = true;
        IsStatic = isTypeInitializer;
    }

    internal static MethodDefinition FromMethodIntl(MethodBase method, IAccessor accessor, bool dele = false)
    {
        MethodInfo? returnableMethod = method as MethodInfo;
        ConstructorInfo? ctor = method as ConstructorInfo;
        MethodDefinition definition = dele ? new DelegateMethodDefinition() : new MethodDefinition(method.Name);

        if (ctor != null)
        {
            definition.Name = ctor.IsStatic ? ".cctor" : ".ctor";
            definition.ReturnType = ctor.DeclaringType;
            definition.DeclaringType = ctor.DeclaringType;
            definition.IsConstructor = true;
            definition.IsStatic = ctor.IsStatic;
        }
        else if (method.DeclaringType != null)
        {
            definition.DeclaredIn(method.DeclaringType, method.IsStatic);
        }

        Type[]? genericArguments = null;
        bool isDef = false;

        ByRefTypeMode rtnMode = returnableMethod != null && returnableMethod.ReturnType.IsByRef
            ? returnableMethod.ReturnParameter != null && accessor.IsReadOnly(returnableMethod.ReturnParameter)
                ? ByRefTypeMode.RefReadOnly
                : ByRefTypeMode.Ref
            : ByRefTypeMode.Ignore;

        if (returnableMethod != null)
        {
            if (returnableMethod.IsGenericMethodDefinition)
            {
                genericArguments = returnableMethod.GetGenericArguments();
                bool found = false;
                if (!returnableMethod.ReturnType.IsGenericParameter)
                {
                    definition.Returning(returnableMethod.ReturnType, rtnMode);
                    found = true;
                }

                isDef = true;
                Type returnType = returnableMethod.ReturnType;
                if (!found && returnType.HasElementType)
                {
                    for (Type? elementType = returnType.GetElementType(); elementType != null; elementType = elementType.GetElementType())
                    {
                        returnType = elementType;
                    }
                }
                foreach (Type type in genericArguments)
                {
                    definition.WithGenericParameterDefinition(type.Name);
                    if (found || type != returnType)
                        continue;

                    GenericReferencingReturnTypeBuilder builder = definition.ReturningUsingGeneric(definition.GenDefsIntl!.Count - 1, ByRefTypeMode.Ignore);

                    if (returnType != returnableMethod.ReturnType)
                    {
                        for (Type? elementType = type; elementType != null; elementType = type.GetElementType())
                        {
                            if (elementType.IsArray)
                                builder.Array(elementType.GetArrayRank());
                            else if (elementType.IsPointer)
                                builder.Pointer();
                            else if (elementType.IsByRef)
                                builder.ByRefType();
                        }
                    }

                    builder.CompleteReturnType();
                    found = true;
                }
                if (!found)
                {
                    definition.Returning(returnableMethod.ReturnType, rtnMode);
                }
            }
            else if (method.IsGenericMethod)
            {
                genericArguments = method.GetGenericArguments();
                foreach (Type type in genericArguments)
                {
                    definition.WithGenericParameterValue(type);
                }
                definition.Returning(returnableMethod.ReturnType, rtnMode);
            }
            else
            {
                definition.Returning(returnableMethod.ReturnType, rtnMode);
            }
        }

        foreach (ParameterInfo parameter in method.GetParameters())
        {
            ByRefTypeMode mode = ByRefTypeMode.Ignore;
            bool isParams = parameter.ParameterType.IsArray && accessor.IsDefinedSafe<ParamArrayAttribute>(parameter);
            if (parameter.ParameterType.IsByRef)
            {
                bool isScoped = !parameter.IsOut && accessor.IsCompilerAttributeDefinedSafe(parameter, "ScopedRefAttribute");
                if (parameter.IsIn)
                    mode = isScoped ? ByRefTypeMode.ScopedIn : ByRefTypeMode.In;
                else if (parameter.IsOut)
                    mode = ByRefTypeMode.Out;
                else
                    mode = ByRefTypeMode.Ref;
            }
            if (isDef && parameter.ParameterType.IsGenericParameter)
            {
                int index = Array.IndexOf(genericArguments!, parameter.ParameterType);
                if (index != -1)
                {
                    definition.WithParameterUsingGeneric(index, parameter.Name, isParams, mode).CompleteGenericParameter();
                    continue;
                }
            }

            definition.WithParameter(parameter.ParameterType, parameter.Name, mode, isParams);
        }

        return definition;
    }

    private static MethodDefinition FromDelegateIntl(MethodInfo invokeMethod, string methodName, IAccessor accessor)
    {
        MethodDefinition definition = new MethodDefinition(methodName);

        Type delegateType = invokeMethod.DeclaringType!;
        Type[]? genericArguments = null;
        bool isDef = false;

        ByRefTypeMode rtnMode = invokeMethod.ReturnType.IsByRef
            ? invokeMethod.ReturnParameter != null && accessor.IsReadOnly(invokeMethod.ReturnParameter)
                ? ByRefTypeMode.RefReadOnly
                : ByRefTypeMode.Ref
            : ByRefTypeMode.Ignore;

        if (delegateType.IsGenericType)
        {
            genericArguments = delegateType.GetGenericArguments();
            if (delegateType.IsGenericTypeDefinition)
            {
                bool found = false;
                if (!invokeMethod.ReturnType.IsGenericParameter)
                {
                    definition.Returning(invokeMethod.ReturnType, rtnMode);
                    found = true;
                }

                isDef = true;
                Type returnType = invokeMethod.ReturnType;
                if (!found && returnType.HasElementType)
                {
                    for (Type? elementType = returnType.GetElementType(); elementType != null; elementType = elementType.GetElementType())
                    {
                        returnType = elementType;
                    }
                }
                foreach (Type type in genericArguments)
                {
                    definition.WithGenericParameterDefinition(type.Name);
                    if (found || type != returnType)
                        continue;
                    
                    GenericReferencingReturnTypeBuilder builder = definition.ReturningUsingGeneric(definition.GenDefsIntl!.Count - 1, ByRefTypeMode.Ignore);
                    
                    if (returnType != invokeMethod.ReturnType)
                    {
                        for (Type? elementType = type; elementType != null; elementType = type.GetElementType())
                        {
                            if (elementType.IsArray)
                                builder.Array(elementType.GetArrayRank());
                            else if (elementType.IsPointer)
                                builder.Pointer();
                            else if (elementType.IsByRef)
                                builder.ByRefType();
                        }
                    }

                    builder.CompleteReturnType();
                    found = true;
                }
                if (!found)
                {
                    definition.Returning(invokeMethod.ReturnType, rtnMode);
                }
            }
            else
            {
                definition.Returning(invokeMethod.ReturnType, rtnMode);
                foreach (Type type in genericArguments)
                {
                    definition.WithGenericParameterValue(type);
                }
            }
        }
        else
        {
            definition.Returning(invokeMethod.ReturnType, rtnMode);
        }

        foreach (ParameterInfo parameter in invokeMethod.GetParameters())
        {
            ByRefTypeMode mode = ByRefTypeMode.Ignore;
            bool isParams = parameter.ParameterType.IsArray && accessor.IsDefinedSafe<ParamArrayAttribute>(parameter);
            if (parameter.ParameterType.IsByRef)
            {
                bool isScoped = !parameter.IsOut && accessor.IsCompilerAttributeDefinedSafe(parameter, "ScopedRefAttribute");
                if (parameter.IsIn)
                    mode = isScoped ? ByRefTypeMode.ScopedIn : ByRefTypeMode.In;
                else if (parameter.IsOut)
                    mode = ByRefTypeMode.Out;
                else
                    mode = ByRefTypeMode.Ref;
            }
            if (isDef && parameter.ParameterType.IsGenericParameter)
            {
                int index = Array.IndexOf(genericArguments!, parameter.ParameterType);
                if (index != -1)
                {
                    definition.WithParameterUsingGeneric(index, parameter.Name, isParams, mode).CompleteGenericParameter();
                    continue;
                }
            }

            definition.WithParameter(parameter.ParameterType, parameter.Name, mode, isParams);
        }

        return definition;
    }

    /// <summary>
    /// Set this method to an extension method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Can not create a static extension method with a DelegateMethodDefinition.</exception>
    public MethodDefinition AsExtensionMethod()
    {
        IsExtensionMethod = true;
        return this;
    }

    /// <summary>
    /// Set the method's by-ref return type.
    /// </summary>
    public MethodDefinition WithReturnRefMode(ByRefTypeMode returnRefTypeMode)
    {
        ReturnRefTypeMode = returnRefTypeMode;
        if (returnRefTypeMode != ByRefTypeMode.Ignore && ReturnType is { IsByRef: false })
            ReturnType = ReturnType.MakeByRefType();
        else if (returnRefTypeMode == ByRefTypeMode.Ignore)
        {
            while (ReturnType is { IsByRef: true })
                ReturnType = ReturnType.GetElementType()!;
        }
        return this;
    }

    /// <summary>
    /// Set the return type of the method to <see langword="void"/>.
    /// </summary>
    public MethodDefinition ReturningVoid()
    {
        ReturnType = typeof(void);
        return this;
    }

    /// <summary>
    /// Set the return type of the method.
    /// </summary>
    public MethodDefinition Returning<TReturnType>(ByRefTypeMode returnRefTypeMode = ByRefTypeMode.Ignore)
    {
        ReturnType = typeof(TReturnType);
        return WithReturnRefMode(returnRefTypeMode);
    }

    /// <summary>
    /// Set the return type of the method.
    /// </summary>
    public MethodDefinition Returning(Type returnType, ByRefTypeMode returnRefTypeMode = ByRefTypeMode.Ignore)
    {
        ReturnType = returnType;
        return WithReturnRefMode(returnRefTypeMode);
    }

    /// <summary>
    /// Set the return type of the method.
    /// </summary>
    public MethodDefinition Returning(string returnType, ByRefTypeMode returnRefTypeMode = ByRefTypeMode.Ignore)
    {
        ReturnType = Type.GetType(returnType);
        return WithReturnRefMode(returnRefTypeMode);
    }

    /// <summary>
    /// Start a <see cref="GenericReferencingReturnTypeBuilder"/> with the given generic parameter definition to create a return type with the base element type pulled from the generic type definitions.
    /// </summary>
    /// <param name="genericParameterIndex">The zero-based index of the generic parameter definition in <see cref="GenericDefinitions"/>.</param>
    /// <param name="byRefMode">The by-ref keyword of the return type.</param>
    public GenericReferencingReturnTypeBuilder ReturningUsingGeneric(int genericParameterIndex, ByRefTypeMode byRefMode = ByRefTypeMode.Ignore)
    {
        return new GenericReferencingReturnTypeBuilder(this, genericParameterIndex, byRefMode);
    }

    /// <summary>
    /// Start a <see cref="GenericReferencingReturnTypeBuilder"/> with the given generic parameter definition to create a return type with the base element type pulled from the generic type definitions.
    /// </summary>
    /// <param name="genericParameterTypeName">Definition name of a type already declared.</param>
    /// <param name="byRefMode">The by-ref keyword of the return type.</param>
    /// <exception cref="ArgumentException">Unknown generic definition type name: <paramref name="genericParameterTypeName"/>.</exception>
    /// <exception cref="ArgumentNullException" />
    public GenericReferencingReturnTypeBuilder ReturningUsingGeneric(string genericParameterTypeName, ByRefTypeMode byRefMode = ByRefTypeMode.Ignore)
    {
        if (genericParameterTypeName == null)
            throw new ArgumentNullException(nameof(genericParameterTypeName));

        if (GenDefsIntl == null)
            throw new ArgumentException("No generic defitions have been defined yet.", nameof(genericParameterTypeName));

        int index = GenDefsIntl.IndexOf(genericParameterTypeName);
        if (index < 0)
            throw new ArgumentException($"Generic parameter not found: '{genericParameterTypeName}'.", nameof(genericParameterTypeName));

        return ReturningUsingGeneric(index, byRefMode);
    }

    /// <summary>
    /// Set the declaring type of the method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Can not create a static extension method with a DelegateMethodDefinition.</exception>
    public MethodDefinition DeclaredIn<TDeclaringType>(bool isStatic)
    {
        DeclaringType = typeof(TDeclaringType);
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Can not create a static extension method with a DelegateMethodDefinition.</exception>
    public MethodDefinition DeclaredIn(Type declaringType, bool isStatic)
    {
        DeclaringType = declaringType;
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the method.
    /// </summary>
    /// <exception cref="InvalidOperationException">Can not create a static extension method with a DelegateMethodDefinition.</exception>
    public MethodDefinition DeclaredIn(string declaringType, bool isStatic)
    {
        DeclaringType = Type.GetType(declaringType);
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Specify the method is parameterless.
    /// </summary>
    public MethodDefinition WithNoParameters()
    {
        if (ParametersIntl == null)
        {
            ParametersIntl = new List<MethodParameterDefinition>(0);
            _readOnlyParameters = null;
        }
        else
            ParametersIntl.Clear();
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(in MethodParameterDefinition parameter)
    {
        if (ParametersIntl == null)
        {
            ParametersIntl = new List<MethodParameterDefinition>(1);
            _readOnlyParameters = null;
        }

        ParametersIntl.Add(parameter);
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(Type type, string? name)
    {
        return WithParameter(new MethodParameterDefinition(type, name));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(Type type, string? name, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(type, name, isParams));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(Type type, string? name, ByRefTypeMode byRefMode)
    {
        return WithParameter(new MethodParameterDefinition(type, name, byRefMode));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(Type type, string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(type, name, byRefMode, isParams));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter<TParamType>(string? name)
    {
        return WithParameter(new MethodParameterDefinition(typeof(TParamType), name));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter<TParamType>(string? name, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(typeof(TParamType), name, isParams));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter<TParamType>(string? name, ByRefTypeMode byRefMode)
    {
        return WithParameter(new MethodParameterDefinition(typeof(TParamType), name, byRefMode));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter<TParamType>(string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(typeof(TParamType), name, byRefMode, isParams));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(string type, string? name)
    {
        return WithParameter(new MethodParameterDefinition(Type.GetType(type), name));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(string type, string? name, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(Type.GetType(type), name, isParams));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(string type, string? name, ByRefTypeMode byRefMode)
    {
        return WithParameter(new MethodParameterDefinition(Type.GetType(type), name, byRefMode));
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameterUsingGeneric(int,string?,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(string type, string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(Type.GetType(type), name, byRefMode, isParams));
    }

    /// <summary>
    /// Start a <see cref="GenericReferencingParameterBuilder"/> with the given generic parameter definition to create a parameter with the base element type pulled from the generic type definitions.
    /// </summary>
    /// <param name="genericParameterIndex">The zero-based index of the generic parameter definition in <see cref="GenericDefinitions"/>.</param>
    /// <param name="parameterName">The name of the new parameter.</param>
    /// <param name="isParams">If the new parameter is a params (remainder) parameter.</param>
    /// <param name="byRefMode">The by-ref keyword of the parameter.</param>
    public GenericReferencingParameterBuilder WithParameterUsingGeneric(int genericParameterIndex, string? parameterName, bool isParams = false, ByRefTypeMode byRefMode = ByRefTypeMode.Ignore)
    {
        return new GenericReferencingParameterBuilder(this, genericParameterIndex, parameterName, isParams, byRefMode);
    }

    /// <summary>
    /// Start a <see cref="GenericReferencingParameterBuilder"/> with the given generic parameter definition to create a parameter with the base element type pulled from the generic type definitions.
    /// </summary>
    /// <param name="genericParameterTypeName">Definition name of a type already declared.</param>
    /// <param name="parameterName">The name of the new parameter.</param>
    /// <param name="isParams">If the new parameter is a params (remainder) parameter.</param>
    /// <param name="byRefMode">The by-ref keyword of the parameter.</param>
    /// <exception cref="ArgumentException">Unknown generic definition type name: <paramref name="genericParameterTypeName"/>.</exception>
    /// <exception cref="ArgumentNullException" />
    public GenericReferencingParameterBuilder WithParameterUsingGeneric(string genericParameterTypeName, string? parameterName, bool isParams = false, ByRefTypeMode byRefMode = ByRefTypeMode.Ignore)
    {
        if (genericParameterTypeName == null)
            throw new ArgumentNullException(nameof(genericParameterTypeName));

        if (GenDefsIntl == null)
            throw new ArgumentException("No generic defitions have been defined yet.", nameof(genericParameterTypeName));

        int index = GenDefsIntl.IndexOf(genericParameterTypeName);
        if (index < 0)
            throw new ArgumentException($"Generic parameter not found: '{genericParameterTypeName}'.", nameof(genericParameterTypeName));

        return WithParameterUsingGeneric(index, parameterName, isParams, byRefMode);
    }

    /// <summary>
    /// Add a generic parameter definition to the generic definitions list.
    /// </summary>
    public MethodDefinition WithGenericParameterDefinition(string typeParamName)
    {
        if (GenDefsIntl == null)
        {
            GenDefsIntl = new List<string>(1);
            _readOnlyGenDefs = null;
        }

        GenDefsIntl.Add(typeParamName);
        return this;
    }

    /// <summary>
    /// Add a generic parameter value to the generic parameters list.
    /// </summary>
    public MethodDefinition WithGenericParameterValue(Type typeParamValue)
    {
        if (GenValsIntl == null)
        {
            GenValsIntl = new List<Type?>(1);
            _readOnlyGenVals = null;
        }

        GenValsIntl.Add(typeParamValue);
        return this;
    }

    /// <summary>
    /// Add a generic parameter value to the generic parameters list.
    /// </summary>
    public MethodDefinition WithGenericParameterValue<TTypeParamValue>()
    {
        return WithGenericParameterValue(typeof(TTypeParamValue));
    }

    /// <summary>
    /// Add a generic parameter value to the generic parameters list.
    /// </summary>
    public MethodDefinition WithGenericParameterValue(string typeParamValue)
    {
        return WithGenericParameterValue(Type.GetType(typeParamValue)!);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Can not create a static extension method with a DelegateMethodDefinition.</exception>
    IMemberDefinition IMemberDefinition.NestedIn<TDeclaringType>(bool isStatic) => DeclaredIn<TDeclaringType>(isStatic);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Can not create a static extension method with a DelegateMethodDefinition.</exception>
    IMemberDefinition IMemberDefinition.NestedIn(Type declaringType, bool isStatic) => DeclaredIn(declaringType, isStatic);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Can not create a static extension method with a DelegateMethodDefinition.</exception>
    IMemberDefinition IMemberDefinition.NestedIn(string declaringType, bool isStatic) => DeclaredIn(declaringType, isStatic);

    /// <inheritdoc />
    public override string ToString()
    {
        if (this is DelegateMethodDefinition dele)
            return Accessor.ExceptionFormatter.Format(dele.DeclaringType);

        return Accessor.ExceptionFormatter.Format(this);
    }
}

/// <summary>
/// Wrapper for <see cref="MethodDefinition"/> used for creating delegate types with <see cref="DelegateTypeDefinition"/>.
/// </summary>
internal class DelegateMethodDefinition() : MethodDefinition("Invoke")
{
    public new DelegateTypeDefinition DeclaringType { get; internal set; } = null!;
}

/// <summary>
/// Defines a template for a method parameter.
/// </summary>
public struct MethodParameterDefinition
{
    /// <summary>
    /// Parameter type.
    /// </summary>
    public Type? Type { get; set; }

    /// <summary>
    /// Index of this parameter's generic type definition.
    /// </summary>
    public int GenericTypeIndex { get; set; }

    /// <summary>
    /// Array of int values defining how the generic type index is manipulated as an element type.
    /// <para>
    /// Element meanings:
    /// <code>
    /// <br/>* dim = -1 = pointer
    /// <br/>* dim &gt; 0  = array of rank {n}
    /// <br/>* dim &lt; -1 = -(int){ByRefTypeMode} - 1
    /// </code>
    /// </para>
    /// </summary>
    public int[]? GenericTypeElementTypes { get; set; }

    /// <summary>
    /// Actual length of <see cref="GenericTypeElementTypes"/>, as it could be from the underlying array of a list.
    /// </summary>
    public int GenericTypeElementTypesLength { get; set; }

    /// <summary>
    /// Optional parameter name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// What keyword to use for the by-ref passing mode, if any.
    /// </summary>
    public ByRefTypeMode ByRefMode { get; set; }

    /// <summary>
    /// Is this parameter a <see langword="params"/> (remainder) array?
    /// </summary>
    public bool IsParams { get; set; }

    /// <summary>
    /// Create a parameter with just a type and name.
    /// </summary>
    public MethodParameterDefinition(Type? type, string? name)
    {
        Type = type;
        Name = name;
        ByRefMode = ByRefTypeMode.Ignore;
        IsParams = false;
        GenericTypeIndex = -1;
    }

    /// <summary>
    /// Create a parameter with just a type and name, along with if it's a <see langword="params"/> array.
    /// </summary>
    public MethodParameterDefinition(Type? type, string? name, bool isParams)
    {
        Type = type;
        Name = name;
        ByRefMode = ByRefTypeMode.Ignore;
        IsParams = isParams;
        GenericTypeIndex = -1;
    }

    /// <summary>
    /// Create a parameter with just a type and name, along with what <paramref name="byRefMode"/> to use.
    /// </summary>
    public MethodParameterDefinition(Type? type, string? name, ByRefTypeMode byRefMode)
    {
        if (byRefMode != ByRefTypeMode.Ignore && type is { IsByRef: false })
            type = type.MakeByRefType();

        Type = type;
        Name = name;
        ByRefMode = byRefMode;
        IsParams = false;
        GenericTypeIndex = -1;
    }

    /// <summary>
    /// Create a parameter with just a type and name, along with if it's a <see langword="params"/> array and what <paramref name="byRefMode"/> to use.
    /// </summary>
    public MethodParameterDefinition(Type? type, string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        if (byRefMode != ByRefTypeMode.Ignore && type is { IsByRef: false })
            type = type.MakeByRefType();

        Type = type;
        Name = name;
        ByRefMode = byRefMode;
        IsParams = isParams;
        GenericTypeIndex = -1;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        if (IsParams && (Type == null || Type.IsArray))
        {
            sb.Append("params ");
        }
        else if (ByRefMode != ByRefTypeMode.Ignore)
        {
            sb.Append(RefTypeToString(ByRefMode));
        }
        if (Type != null)
        {
            sb.Append(Accessor.ExceptionFormatter.Format(Type));
        }
        else if (GenericTypeIndex >= 0)
        {
            if (GenericTypeElementTypes != null)
            {
                for (int i = 0; i < GenericTypeElementTypesLength; ++i)
                {
                    int dim = GenericTypeElementTypes[GenericTypeElementTypesLength - i - 1];
                    if (dim >= -1)
                        continue;

                    ByRefTypeMode mode = (ByRefTypeMode)(-(dim + 1));
                    sb.Append(RefTypeToString(mode));
                }
            }

            sb.Append("T" + (GenericTypeIndex + 1).ToString(CultureInfo.InvariantCulture));
            if (GenericTypeElementTypes != null)
            {
                for (int i = 0; i < GenericTypeElementTypesLength; ++i)
                {
                    int dim = GenericTypeElementTypes[GenericTypeElementTypesLength - i - 1];
                    if (dim > 0)
                    {
                        sb.Append('[');
                        sb.Append(',', dim - 1);
                        sb.Append(']');
                    }
                    else if (dim == -1)
                    {
                        sb.Append('*');
                    }
                    if (dim >= -1)
                        continue;

                    ByRefTypeMode mode = (ByRefTypeMode)(-(dim + 1));
                    sb.Append(RefTypeToString(mode));
                }
            }
        }
        if (Name != null)
        {
            if (Type != null || GenericTypeIndex >= 0)
                sb.Append(' ');
            sb.Append(Name);
        }

        return sb.ToString(0, sb[sb.Length - 1] == ' ' ? sb.Length - 1 : sb.Length);

        static string RefTypeToString(ByRefTypeMode mode)
        {
            return mode switch
            {
                ByRefTypeMode.Ref => "ref ",
                ByRefTypeMode.In => "in ",
                ByRefTypeMode.RefReadOnly => "in ",
                ByRefTypeMode.Out => "out ",
                ByRefTypeMode.ScopedRef => "scoped ref ",
                ByRefTypeMode.ScopedIn => "scoped in ",
                ByRefTypeMode.ScopedRefReadOnly => "scoped in ",
                _ => string.Empty
            };
        }
    }
}

/// <summary>
/// Builds a <see cref="MethodParameterDefinition"/> that references a generic parameter definition in a <see cref="MethodDefinition"/>.
/// </summary>
public class GenericReferencingParameterBuilder
{
    private readonly List<int> _levels = new List<int>(0);
    private readonly MethodDefinition _method;

    /// <summary>
    /// Index in <see cref="MethodDefinition.GenericDefinitions"/> that this parameter references.
    /// </summary>
    public int GenericParameterIndex { get; set; }

    /// <summary>
    /// Name of the parameter.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// What keyword to use for the by-ref passing mode, if any.
    /// </summary>
    public ByRefTypeMode ByRefMode { get; set; }

    /// <summary>
    /// Is this parameter a <see langword="params"/> (remainder) array?
    /// </summary>
    public bool IsParams { get; set; }

    /// <summary>
    /// Current number of types that are wrapped around the original element type.
    /// </summary>
    public int Depth => _levels.Count;

    /// <summary>
    /// Start a <see cref="GenericReferencingParameterBuilder"/> with the given generic parameter definition.
    /// </summary>
    internal GenericReferencingParameterBuilder(MethodDefinition method, int genericParameterIndex, string? parameterName, bool isParams = false, ByRefTypeMode byRefMode = ByRefTypeMode.Ignore)
    {
        _method = method;
        GenericParameterIndex = genericParameterIndex;
        Name = parameterName;
        IsParams = isParams;
        ByRefMode = byRefMode;
    }

    /// <summary>
    /// Add an array with the specified rank (dimension).
    /// </summary>
    /// <remarks>Keep in mind arrays of arrays are usually defined backwards so groups of sequential arrays should be defined as you would write it in C# (backwards).</remarks>
    /// <param name="rank">Number of dimensions the array has.</param>
    /// <exception cref="ArgumentOutOfRangeException">Rank must be 1 or higher.</exception>
    /// <exception cref="InvalidOperationException">There can only be one by-ref type and it must be the most outer (last) element wrapper.</exception>
    public GenericReferencingParameterBuilder Array(int rank = 1)
    {
        if (rank < 1)
            throw new ArgumentOutOfRangeException(nameof(rank));

        PushElement(rank);
        return this;
    }

    /// <summary>
    /// Add a pointer.
    /// </summary>
    /// <exception cref="InvalidOperationException">There can only be one by-ref type and it must be the most outer (last) element wrapper.</exception>
    public GenericReferencingParameterBuilder Pointer()
    {
        PushElement(-1);
        return this;
    }

    /// <summary>
    /// Add a by-ref type wrapper. Must be done last.
    /// </summary>
    /// <exception cref="InvalidOperationException">There can only be one by-ref type and it must be the most outer (last) element wrapper.</exception>
    public GenericReferencingParameterBuilder ByRefType()
    {
        PushElement(-(int)ByRefTypeMode.Ref - 1);
        return this;
    }

    private void PushElement(int dim)
    {
        if (_levels.Count > 0 && _levels[_levels.Count - 1] == -((int)ByRefTypeMode.Ref - 1))
            throw new FormatException("There can only be one by-ref type and it must be the most outer (last) element wrapper.");

        _levels.Add(dim);
    }

    /// <summary>
    /// Create a <see cref="MethodParameterDefinition"/> from the given information and add it to the method.
    /// </summary>
    public MethodDefinition CompleteGenericParameter()
    {
        for (int i = 0; i < _levels.Count / 2; ++i)
        {
            int tmp = _levels[i];
            int index = _levels.Count - i - 1;
            _levels[i] = _levels[index];
            _levels[index] = tmp;
        }

        if (ByRefMode != ByRefTypeMode.Ignore && (Depth == 0 || _levels[0] != -(int)ByRefTypeMode.Ref - 1))
        {
            throw new FormatException("If a by-ref mode is passed, the most outer level must be ByRefType().");
        }

        MethodParameterDefinition d = default;
        d.GenericTypeIndex = GenericParameterIndex;
        d.IsParams = IsParams;
        d.ByRefMode = ByRefMode;
        d.Name = Name;

        int[] arr = _levels.GetUnderlyingArrayOrCopy();
        d.GenericTypeElementTypes = arr;
        d.GenericTypeElementTypesLength = _levels.Count;

        _method.WithParameter(in d);
        return _method;
    }
}

/// <summary>
/// Builds a return type that references a generic parameter definition in a <see cref="MethodDefinition"/>.
/// </summary>
public class GenericReferencingReturnTypeBuilder
{
    private readonly List<int> _levels = new List<int>(0);
    private readonly MethodDefinition _method;

    /// <summary>
    /// Index in <see cref="MethodDefinition.GenericDefinitions"/> that this parameter references.
    /// </summary>
    public int GenericParameterIndex { get; set; }

    /// <summary>
    /// What keyword to use for the by-ref return mode, if any.
    /// </summary>
    public ByRefTypeMode ByRefMode { get; set; }

    /// <summary>
    /// Current number of types that are wrapped around the original element type.
    /// </summary>
    public int Depth => _levels.Count;

    /// <summary>
    /// Start a <see cref="GenericReferencingReturnTypeBuilder"/> with the given generic parameter definition.
    /// </summary>
    internal GenericReferencingReturnTypeBuilder(MethodDefinition method, int genericParameterIndex, ByRefTypeMode byRefMode = ByRefTypeMode.Ignore)
    {
        _method = method;
        GenericParameterIndex = genericParameterIndex;
        ByRefMode = byRefMode;
    }

    /// <summary>
    /// Add an array with the specified rank (dimension).
    /// </summary>
    /// <remarks>Keep in mind arrays of arrays are usually defined backwards so groups of sequential arrays should be defined as you would write it in C# (backwards).</remarks>
    /// <param name="rank">Number of dimensions the array has.</param>
    /// <exception cref="ArgumentOutOfRangeException">Rank must be 1 or higher.</exception>
    /// <exception cref="InvalidOperationException">There can only be one by-ref type and it must be the most outer (last) element wrapper.</exception>
    public GenericReferencingReturnTypeBuilder Array(int rank = 1)
    {
        if (rank < 1)
            throw new ArgumentOutOfRangeException(nameof(rank));

        PushElement(rank);
        return this;
    }

    /// <summary>
    /// Add a pointer.
    /// </summary>
    /// <exception cref="InvalidOperationException">There can only be one by-ref type and it must be the most outer (last) element wrapper.</exception>
    public GenericReferencingReturnTypeBuilder Pointer()
    {
        PushElement(-1);
        return this;
    }

    /// <summary>
    /// Add a by-ref type wrapper. Must be done last.
    /// </summary>
    /// <exception cref="InvalidOperationException">There can only be one by-ref type and it must be the most outer (last) element wrapper.</exception>
    public GenericReferencingReturnTypeBuilder ByRefType()
    {
        PushElement(-(int)ByRefTypeMode.Ref - 1);
        return this;
    }

    private void PushElement(int dim)
    {
        if (_levels.Count > 0 && _levels[_levels.Count - 1] == -((int)ByRefTypeMode.Ref - 1))
            throw new FormatException("There can only be one by-ref type and it must be the most outer (last) element wrapper.");

        _levels.Add(dim);
    }

    /// <summary>
    /// Set the return type and return the original method definition.
    /// </summary>
    public MethodDefinition CompleteReturnType()
    {
        for (int i = 0; i < _levels.Count / 2; ++i)
        {
            int tmp = _levels[i];
            int index = _levels.Count - i - 1;
            _levels[i] = _levels[index];
            _levels[index] = tmp;
        }

        if (ByRefMode != ByRefTypeMode.Ignore && (Depth == 0 || _levels[0] != -(int)ByRefTypeMode.Ref - 1))
        {
            throw new FormatException("If a by-ref mode is passed, the most outer level must be ByRefType().");
        }

        _method.ReturnTypeGenericIndex = GenericParameterIndex;
        _method.ReturnRefTypeMode = ByRefMode;
        _method.ReturnType = null;

        int[] arr = _levels.GetUnderlyingArrayOrCopy();
        _method.ReturnTypeGenericTypeElementTypes = arr;
        _method.ReturnTypeGenericTypeElementTypesLength = _levels.Count;

        return _method;
    }
}