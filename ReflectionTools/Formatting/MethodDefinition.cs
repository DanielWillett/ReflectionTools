using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace DanielWillett.ReflectionTools.Formatting;

// todo method return from generics
/// <summary>
/// Represents a shell of a method for formatting purposes.
/// </summary>
public class MethodDefinition
{
    /// <summary>
    /// Name of the method.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// If this method is a constructor for <see cref="DeclaringType"/>.
    /// </summary>
    public bool IsConstructor { get; set; }

    /// <summary>
    /// Type the method is declared in.
    /// </summary>
    public Type? DeclaringType { get; set; }

    /// <summary>
    /// Type the method returns, or <see langword="void"/>.
    /// </summary>
    public Type ReturnType { get; set; }

    /// <summary>
    /// By-ref keyword of the value the method returns.
    /// </summary>
    public ByRefTypeMode ReturnRefTypeMode { get; set; }

    /// <summary>
    /// If the method requires an instance of <see cref="DeclaringType"/> to be invoked.
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// If the method is an extension method (the first parameter is 'this').
    /// </summary>
    public bool IsExtensionMethod { get; set; }

    /// <summary>
    /// All parameters in the method. <see langword="null"/> if no parameters are provided.
    /// </summary>
    public IList<MethodParameterDefinition>? Parameters { get; private set; }

    /// <summary>
    /// All generic parameter definitions in the method. <see langword="null"/> if no definitions are provided.
    /// </summary>
    public IList<string>? GenericDefinitions { get; private set; }

    /// <summary>
    /// All generic parameter values in the method. <see langword="null"/> if no generic parameter values are provided.
    /// </summary>
    public IList<Type?>? GenericParameters { get; private set; }

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
    /// Create a method definition, starting with a return type and method name.
    /// </summary>
    public MethodDefinition(Type returnType, string methodName)
    {
        ReturnType = returnType;
        Name = methodName;
    }

    /// <summary>
    /// Create a constructor definition, starting with a return type and declaring type.
    /// </summary>
    public MethodDefinition(Type declaringType, bool isTypeInitializer = false)
    {
        ReturnType = declaringType;
        Name = ".ctor";
        DeclaringType = declaringType;
        IsConstructor = true;
        IsStatic = isTypeInitializer;
    }

    private static MethodDefinition FromDelegateIntl(MethodInfo invokeMethod, string methodName, IAccessor accessor)
    {
        MethodDefinition definition = new MethodDefinition(invokeMethod.ReturnType, methodName);
        if (definition.ReturnType.IsByRef)
        {
            if (invokeMethod.ReturnParameter != null && accessor.IsReadOnly(invokeMethod.ReturnParameter))
                definition.WithReturnRefMode(ByRefTypeMode.RefReadonly);
            else
                definition.WithReturnRefMode(ByRefTypeMode.Ref);
        }

        Type delegateType = invokeMethod.DeclaringType!;
        Type[]? genericArguments = null;
        bool isDef = false;
        if (delegateType.IsGenericType)
        {
            genericArguments = delegateType.GetGenericArguments();
            if (delegateType.IsGenericTypeDefinition)
            {
                isDef = true;
                foreach (Type type in genericArguments)
                {
                    definition.WithGenericParameterDefinition(type.Name);
                }
            }
            else
            {
                foreach (Type type in genericArguments)
                {
                    definition.WithGenericParameterValue(type);
                }
            }
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
                    definition.WithParameter(index, parameter.Name, isParams, mode);
                    continue;
                }
            }

            definition.WithParameter(parameter.ParameterType, parameter.Name, mode, isParams);
        }

        return definition;
    }

    /// <summary>
    /// Set the method's by-ref return type.
    /// </summary>
    public MethodDefinition WithReturnRefMode(ByRefTypeMode returnRefTypeMode)
    {
        ReturnRefTypeMode = returnRefTypeMode;
        if (returnRefTypeMode != ByRefTypeMode.Ignore && ReturnType is { IsByRef: false })
            ReturnType = ReturnType.MakeByRefType();
        return this;
    }

    /// <summary>
    /// Set this method to an extension method.
    /// </summary>
    public MethodDefinition AsExtensionMethod()
    {
        IsExtensionMethod = true;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the method.
    /// </summary>
    public MethodDefinition DeclaredIn<TDeclaringType>(bool isStatic)
    {
        DeclaringType = typeof(TDeclaringType);
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the method.
    /// </summary>
    public MethodDefinition DeclaredIn(Type declaringType, bool isStatic)
    {
        DeclaringType = declaringType;
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the method.
    /// </summary>
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
        if (Parameters == null)
            Parameters = new List<MethodParameterDefinition>(0);
        else
            Parameters.Clear();
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(in MethodParameterDefinition parameter)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(parameter with { Method = this });
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(Type type, string? name)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(type, name));
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(Type type, string? name, bool isParams)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(type, name, isParams));
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(Type type, string? name, ByRefTypeMode byRefMode)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(type, name, byRefMode));
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(Type type, string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(type, name, byRefMode, isParams));
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter<TParamType>(string? name)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(typeof(TParamType), name)
        {
            Method = this
        });
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter<TParamType>(string? name, bool isParams)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(typeof(TParamType), name, isParams)
        {
            Method = this
        });
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter<TParamType>(string? name, ByRefTypeMode byRefMode)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(typeof(TParamType), name, byRefMode)
        {
            Method = this
        });
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter<TParamType>(string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(typeof(TParamType), name, byRefMode, isParams)
        {
            Method = this
        });
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(string type, string? name)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(Type.GetType(type), name)
        {
            Method = this
        });
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(string type, string? name, bool isParams)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(Type.GetType(type), name, isParams)
        {
            Method = this
        });
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(string type, string? name, ByRefTypeMode byRefMode)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(Type.GetType(type), name, byRefMode)
        {
            Method = this
        });
        return this;
    }

    /// <summary>
    /// Add a parameter to the parameter list.
    /// </summary>
    /// <remarks>See <see cref="WithParameter(int,string,bool,ByRefTypeMode)"/> to reference a generic type parameter definition.</remarks>
    public MethodDefinition WithParameter(string type, string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        Parameters ??= new List<MethodParameterDefinition>(1);
        Parameters.Add(new MethodParameterDefinition(Type.GetType(type), name, byRefMode, isParams)
        {
            Method = this
        });
        return this;
    }

    /// <summary>
    /// Start a <see cref="GenericReferencingParameterBuilder"/> with the given generic parameter definition to create a parameter with the base element type pulled from the generic type definitions.
    /// </summary>
    /// <param name="genericParameterIndex">The zero-based index of the generic parameter definition in <see cref="MethodDefinition.GenericDefinitions"/>.</param>
    /// <param name="parameterName">The name of the new parameter.</param>
    /// <param name="isParams">If the new parameter is a params (remainder) parameter.</param>
    /// <param name="byRefMode">The by-ref keyword of the parameter.</param>
    public GenericReferencingParameterBuilder WithParameter(int genericParameterIndex, string? parameterName, bool isParams = false, ByRefTypeMode byRefMode = ByRefTypeMode.Ignore)
    {
        return new GenericReferencingParameterBuilder(this, genericParameterIndex, parameterName, isParams, byRefMode);
    }

    /// <summary>
    /// Add a generic parameter definition to the generic definitions list.
    /// </summary>
    public MethodDefinition WithGenericParameterDefinition(string typeParamName)
    {
        GenericDefinitions ??= new List<string>(1);
        GenericDefinitions.Add(typeParamName);
        return this;
    }

    /// <summary>
    /// Add a generic parameter value to the generic parameters list.
    /// </summary>
    public MethodDefinition WithGenericParameterValue<TTypeParamValue>()
    {
        GenericParameters ??= new List<Type?>(1);
        GenericParameters.Add(typeof(TTypeParamValue));
        return this;
    }

    /// <summary>
    /// Add a generic parameter value to the generic parameters list.
    /// </summary>
    public MethodDefinition WithGenericParameterValue(Type typeParamValue)
    {
        GenericParameters ??= new List<Type?>(1);
        GenericParameters.Add(typeParamValue);
        return this;
    }

    /// <summary>
    /// Add a generic parameter value to the generic parameters list.
    /// </summary>
    public MethodDefinition WithGenericParameterValue(string typeParamValue)
    {
        GenericParameters ??= new List<Type?>(1);
        GenericParameters.Add(Type.GetType(typeParamValue));
        return this;
    }

    /// <inheritdoc />
    public override string ToString() => Accessor.ExceptionFormatter.Format(this);
}

/// <summary>
/// Defines a template for a method parameter.
/// </summary>
public struct MethodParameterDefinition
{
    internal MethodDefinition? Method;

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

            if (Method == null)
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
                ByRefTypeMode.RefReadonly => "in ",
                ByRefTypeMode.Out => "out ",
                ByRefTypeMode.ScopedRef => "scoped ref ",
                ByRefTypeMode.ScopedIn => "scoped in ",
                ByRefTypeMode.ScopedRefReadonly => "scoped in ",
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