using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Represents a shell of a type for formatting purposes.
/// </summary>
public class TypeDefinition : IMemberDefinition
{
    internal string? KeywordIntl;
    private protected bool IsDelegateIntl;
    private bool _isValueType;
    private bool _isEnum;
    private bool _isReadOnly;
    private bool _isByRefLike;
    private bool _isStatic;
    private bool _isInterface;
    internal List<string>? GenDefsIntl;
    internal List<Type?>? GenValsIntl;
    private ReadOnlyCollection<string>? _readOnlyGenDefs;
    private ReadOnlyCollection<Type?>? _readOnlyGenVals;
    private ReadOnlyCollection<int>? _readOnlyElementVals;
    internal List<int>? ElementTypesIntl;

    /// <summary>
    /// Name of the type.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Type the type is nested in.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public Type? DeclaringType { get; set; }

    /// <summary>
    /// If the type is an <see langword="static"/> reference type.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will never be <see langword="true"/> if <see cref="IsValueType"/> is <see langword="true"/>.</remarks>
    public bool IsStatic
    {
        get => _isStatic;
        set
        {
            if (value)
                IsValueType = false;

            _isStatic = value;
        }
    }

    /// <summary>
    /// If the type is a <see langword="readonly"/> value type.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will always be <see langword="false"/> if <see cref="IsValueType"/> is <see langword="false"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not create a read-only value type on a DelegateTypeBuilder.</exception>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (value && this is DelegateTypeDefinition)
                throw new InvalidOperationException("Can not create a read-only value type on a DelegateTypeBuilder.");

            if (value)
                _isValueType = true;

            _isReadOnly = value;
        }
    }

    /// <summary>
    /// If the type is a <see langword="ref"/> value type.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will always be <see langword="false"/> if <see cref="IsValueType"/> is <see langword="false"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not create a by-ref-like value type on a DelegateTypeBuilder.</exception>
    public bool IsByRefLike
    {
        get => _isByRefLike;
        set
        {
            if (value && this is DelegateTypeDefinition)
                throw new InvalidOperationException("Can not create a by-ref-like value type on a DelegateTypeBuilder.");

            if (value)
                _isValueType = true;

            _isByRefLike = value;
        }
    }

    /// <summary>
    /// If the type is an <see langword="enum"/> value type.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will always be <see langword="false"/> if <see cref="IsValueType"/> is <see langword="false"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not create an enum on a DelegateTypeBuilder.</exception>
    public bool IsEnum
    {
        get => _isEnum;
        set
        {
            if (value && this is DelegateTypeDefinition)
                throw new InvalidOperationException("Can not create an enum on a DelegateTypeBuilder.");

            if (value)
                _isValueType = true;

            _isEnum = value;
        }
    }

    /// <summary>
    /// If the type is a value type <see langword="struct"/> instead of a reference type <see langword="class"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will always be <see langword="true"/> if <see cref="IsEnum"/> or <see cref="IsReadOnly"/> is <see langword="true"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not create a value type on a DelegateTypeBuilder.</exception>
    public bool IsValueType
    {
        get => _isValueType;
        set
        {
            if (value && this is DelegateTypeDefinition)
                throw new InvalidOperationException("Can not create a value type on a DelegateTypeBuilder.");

            if (!value)
            {
                _isEnum = false;
                _isReadOnly = false;
                _isByRefLike = false;
            }
            else
            {
                _isStatic = false;
            }

            _isValueType = value;
        }
    }

    /// <summary>
    /// If the type is an <see langword="static"/> reference type.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will never be <see langword="true"/> if <see cref="IsValueType"/> is <see langword="true"/>.</remarks>
    /// <exception cref="InvalidOperationException">Can not create an interface on a DelegateTypeBuilder.</exception>
    public bool IsInterface
    {
        get => _isInterface;
        set
        {
            if (value && this is DelegateTypeDefinition)
                throw new InvalidOperationException("Can not create an interface on a DelegateTypeBuilder.");

            if (value)
            {
                IsValueType = false;
                _isStatic = false;
            }

            _isInterface = value;
        }
    }

    /// <summary>
    /// If the type is a <see langword="delegate"/> reference type.
    /// </summary>
    /// <remarks>To create a delegate type, use <see cref="DefineDelegate"/> or <see cref="FromDelegateType"/>.</remarks>
    public bool IsDelegate => IsDelegateIntl;

    /// <summary>
    /// Array of int values defining how the type index is manipulated as an element type.
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
#if !NETFRAMEWORK || NET45_OR_GREATER
    public IReadOnlyList<int>? ElementTypes
#else
    public ReadOnlyCollection<int>? ElementTypes
#endif
    {
        get
        {
            if (_readOnlyElementVals != null)
                return _readOnlyElementVals;

            if (ElementTypesIntl == null)
                return null;

            _readOnlyElementVals = new ReadOnlyCollection<int>(ElementTypesIntl);

            return _readOnlyElementVals;
        }
    }

    /// <summary>
    /// All generic parameter definitions in the type. <see langword="null"/> if no definitions are provided. If this is a <see cref="DelegateTypeDefinition"/>, use <see cref="DelegateTypeDefinition.InvokeMethod"/> to define generic parameters.
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
    /// All generic parameter values in the type. <see langword="null"/> if no generic parameter values are provided. If this is a <see cref="DelegateTypeDefinition"/>, use <see cref="DelegateTypeDefinition.InvokeMethod"/> to define generic parameters.
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
    /// Create a type definition, starting with a type name.
    /// </summary>
    public TypeDefinition(string typeName)
    {
        Name = typeName;
    }

    /// <summary>
    /// Creates a new <see cref="TypeDefinition"/> made for creating delegate types.
    /// </summary>
    public static DelegateTypeDefinition DefineDelegate(string typeName)
    {
        DelegateMethodDefinition methodDef = new DelegateMethodDefinition();
        DelegateTypeDefinition definition = new DelegateTypeDefinition(methodDef, typeName);
        methodDef.DeclaringType = definition;
        return definition;
    }

    /// <summary>
    /// Create a type definition from an existing type.
    /// </summary>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use, defaulting to <see cref="Accessor.Active"/>.</param>
    /// <remarks>For <see langword="delegate"/> types, consider <see cref="FromDelegateType{TDelegateType}"/>.</remarks>
    public static TypeDefinition FromType<T>(IAccessor? accessor = null) => FromType(typeof(T), accessor);

    /// <summary>
    /// Create a type definition from an existing type.
    /// </summary>
    /// <param name="type">Type to model this <see cref="TypeDefinition"/> after.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use, defaulting to <see cref="Accessor.Active"/>.</param>
    /// <remarks>For <see langword="delegate"/> types, consider <see cref="FromDelegateType"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
    public static TypeDefinition FromType(Type type, IAccessor? accessor = null)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        accessor ??= Accessor.Active;
        List<int>? elemTypes = null;
        InternalUtil.GetElementTypes(ref elemTypes, ref type);
        
        if (type.IsSubclassOf(typeof(Delegate)))
        {
            DelegateTypeDefinition def = FromDelegateType(type, accessor);
            if (elemTypes != null)
            {
                def.ElementTypesIntl = elemTypes;
            }
            return def;
        }

        string name = type.Name;
        if (type.IsGenericType)
        {
            int graveIndex = name.IndexOf('`');
            if (graveIndex != -1)
                name = name.Substring(0, graveIndex);
        }

        TypeDefinition definition = new TypeDefinition(name)
        {
            DeclaringType = type.DeclaringType
        };

        definition.KeywordIntl = InternalUtil.GetKeyword(type);

        if (type.IsValueType)
        {
            definition.IsValueType = true;
            definition._isReadOnly = accessor.IsReadOnly(type);
            definition._isByRefLike = accessor.IsByRefLikeType(type);
            definition._isEnum = type.IsEnum;
        }
        else
        {
            definition.IsStatic = accessor.GetIsStatic(type);
        }

        if (type.IsGenericTypeDefinition)
        {
            Type[] genericParameters = type.GetGenericArguments();
            foreach (Type param in genericParameters)
            {
                definition.WithGenericParameterDefinition(param.Name);
            }
        }
        else if (type.IsGenericType)
        {
            Type[] genericParameters = type.GetGenericArguments();
            foreach (Type param in genericParameters)
            {
                definition.WithGenericParameterValue(param);
            }
        }

        return definition;
    }

    /// <summary>
    /// Create a type definition from an existing type.
    /// </summary>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use, defaulting to <see cref="Accessor.Active"/>.</param>
    /// <exception cref="NotSupportedException">Reflection failure getting Invoke method from delegate type.</exception>
    public static DelegateTypeDefinition FromDelegateType<TDelegateType>(IAccessor? accessor = null) where TDelegateType : Delegate
    {
        accessor ??= Accessor.Active;

        MethodInfo invokeMethod = accessor.GetInvokeMethod<TDelegateType>();
        return FromDelegateTypeIntl(invokeMethod, typeof(TDelegateType), accessor);
    }

    /// <summary>
    /// Create a type definition from an existing delegate type or element chain of types ending in a delegate type.
    /// </summary>
    /// <exception cref="ArgumentException">Type must be a delegate type.</exception>
    /// <exception cref="NotSupportedException">Reflection failure getting Invoke method from delegate type.</exception>
    /// <exception cref="ArgumentNullException"/>
    public static DelegateTypeDefinition FromDelegateType(Type delegateType, IAccessor? accessor = null)
    {
        if (delegateType == null)
            throw new ArgumentNullException(nameof(delegateType));

        List<int>? elemTypes = null;
        InternalUtil.GetElementTypes(ref elemTypes, ref delegateType);

        if (!delegateType.IsSubclassOf(typeof(Delegate)))
            throw new ArgumentException("Type must be a delegate type.", nameof(delegateType));

        accessor ??= Accessor.Active;

        MethodInfo invokeMethod = accessor.GetInvokeMethod(delegateType);
        DelegateTypeDefinition def = FromDelegateTypeIntl(invokeMethod, delegateType, accessor);
        if (elemTypes != null)
        {
            def.ElementTypesIntl = elemTypes;
        }
        return def;
    }

    private static DelegateTypeDefinition FromDelegateTypeIntl(MethodInfo invokeMethod, Type delegateType, IAccessor accessor)
    {
        DelegateMethodDefinition methodDefinition = (DelegateMethodDefinition)MethodDefinition.FromMethodIntl(invokeMethod, accessor, true);

        string name = delegateType.Name;
        if (delegateType.IsGenericType)
        {
            int graveIndex = name.IndexOf('`');
            if (graveIndex != -1)
                name = name.Substring(0, graveIndex);
        }

        DelegateTypeDefinition definition = new DelegateTypeDefinition(methodDefinition, name)
        {
            DeclaringType = delegateType.DeclaringType
        };

        methodDefinition.DeclaringType = definition;

        return definition;
    }

    /// <summary>
    /// Set <see cref="IsReadOnly"/> and <see cref="IsValueType"/> to <see langword="true"/>.
    /// </summary>
    public TypeDefinition AsReadOnlyValueType()
    {
        IsReadOnly = true;
        return this;
    }

    /// <summary>
    /// Set <see cref="IsByRefLike"/> and <see cref="IsValueType"/> to <see langword="true"/>.
    /// </summary>
    public TypeDefinition AsByRefLikeValueType()
    {
        IsByRefLike = true;
        return this;
    }

    /// <summary>
    /// Set <see cref="IsEnum"/> and <see cref="IsValueType"/> to <see langword="true"/>.
    /// </summary>
    public TypeDefinition AsEnum()
    {
        IsByRefLike = true;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the type.
    /// </summary>
    public TypeDefinition NestedIn<TDeclaringType>()
    {
        DeclaringType = typeof(TDeclaringType);
        return this;
    }

    /// <summary>
    /// Set the declaring type of the type.
    /// </summary>
    public TypeDefinition NestedIn(Type declaringType)
    {
        DeclaringType = declaringType;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the type.
    /// </summary>
    public TypeDefinition NestedIn(string declaringType)
    {
        DeclaringType = Type.GetType(declaringType);
        return this;
    }

    /// <summary>
    /// Add a generic parameter definition to the generic definitions list.
    /// </summary>
    /// <exception cref="InvalidOperationException">Generic types should be defined in this delegate type's InvokeMethod instead.</exception>
    public TypeDefinition WithGenericParameterDefinition(string typeParamName)
    {
        if (this is DelegateTypeDefinition)
            throw new InvalidOperationException("Generic types should be defined in this delegate type's InvokeMethod instead.");

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
    /// <exception cref="InvalidOperationException">Generic types should be defined in this delegate type's InvokeMethod instead.</exception>
    public TypeDefinition WithGenericParameterValue(Type typeParamValue)
    {
        if (this is DelegateTypeDefinition)
            throw new InvalidOperationException("Generic types should be defined in this delegate type's InvokeMethod instead.");

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
    /// <exception cref="InvalidOperationException">Generic types should be defined in this delegate type's InvokeMethod instead.</exception>
    public TypeDefinition WithGenericParameterValue<TTypeParamValue>()
    {
        return WithGenericParameterValue(typeof(TTypeParamValue));
    }

    /// <summary>
    /// Add a generic parameter value to the generic parameters list.
    /// </summary>
    /// <exception cref="InvalidOperationException">Generic types should be defined in this delegate type's InvokeMethod instead.</exception>
    public TypeDefinition WithGenericParameterValue(string typeParamValue)
    {
        return WithGenericParameterValue(Type.GetType(typeParamValue)!);
    }

    /// <summary>
    /// Add an array with the specified rank (dimension).
    /// </summary>
    /// <remarks>Keep in mind arrays of arrays are usually defined backwards so groups of sequential arrays should be defined as you would write it in C# (backwards).</remarks>
    /// <param name="rank">Number of dimensions the array has.</param>
    /// <exception cref="ArgumentOutOfRangeException">Rank must be 1 or higher.</exception>
    /// <exception cref="InvalidOperationException">There can only be one by-ref type and it must be the most outer (last) element wrapper.</exception>
    public TypeDefinition Array(int rank = 1)
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
    public TypeDefinition Pointer()
    {
        PushElement(-1);
        return this;
    }

    /// <summary>
    /// Add a by-ref type wrapper. Must be done last.
    /// </summary>
    /// <exception cref="InvalidOperationException">There can only be one by-ref type and it must be the most outer (last) element wrapper.</exception>
    public TypeDefinition ByRefType()
    {
        PushElement(-(int)ByRefTypeMode.Ref - 1);
        return this;
    }

    private void PushElement(int dim)
    {
        if (ElementTypesIntl == null)
        {
            ElementTypesIntl = new List<int>(1);
            _readOnlyElementVals = null;
        }

        if (ElementTypesIntl.Count > 0 && ElementTypesIntl[0] == -((int)ByRefTypeMode.Ref - 1))
            throw new FormatException("There can only be one by-ref type and it must be the most outer (last) element wrapper.");

        ElementTypesIntl.Insert(0, dim);
    }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    /// <inheritdoc />
    public int GetFormatLength(IOpCodeFormatter formatter) => formatter.GetFormatLength(this);

    /// <inheritdoc />
    public int Format(IOpCodeFormatter formatter, Span<char> output) => formatter.Format(this, output);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(IOpCodeFormatter,Span{char},ByRefTypeMode)"/>.
    /// </summary>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <returns>The length in characters of this as a string.</returns>
    public int GetFormatLength(IOpCodeFormatter formatter, ByRefTypeMode refMode) => formatter.GetFormatLength(this, refMode);

    /// <summary>
    /// Format this into a string representation. Use <see cref="GetFormatLength(IOpCodeFormatter,ByRefTypeMode)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <returns>The length in characters of this as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    public int Format(IOpCodeFormatter formatter, Span<char> output, ByRefTypeMode refMode) => formatter.Format(this, output, refMode);
#endif

    /// <inheritdoc />
    public string Format(IOpCodeFormatter formatter) => formatter.Format(this);

    /// <summary>
    /// Format this into a string representation.
    /// </summary>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    public string Format(IOpCodeFormatter formatter, ByRefTypeMode refMode) => formatter.Format(this, refMode);

    IMemberDefinition IMemberDefinition.NestedIn<TDeclaringType>(bool isStatic)
    {
        IsStatic = isStatic;
        return NestedIn<TDeclaringType>();
    }
    IMemberDefinition IMemberDefinition.NestedIn(Type declaringType, bool isStatic)
    {
        IsStatic = isStatic;
        return NestedIn(declaringType);
    }
    IMemberDefinition IMemberDefinition.NestedIn(string declaringType, bool isStatic)
    {
        IsStatic = isStatic;
        return NestedIn(declaringType);
    }

    /// <inheritdoc />
    public override string ToString() => Accessor.ExceptionFormatter.Format(this);
}

/// <summary>
/// Represents a <see cref="TypeDefinition"/> for a delegate type.
/// <para>
/// Note that many properties and methods will throw an <see cref="InvalidOperationException"/> on this object, as denoted in the XML documentation.
/// </para>
/// </summary>
/// <remarks>Create using <see cref="TypeDefinition.DefineDelegate"/> or <see cref="TypeDefinition.FromDelegateType"/>.</remarks>
public class DelegateTypeDefinition : TypeDefinition
{
    /// <summary>
    /// A <see cref="MethodDefinition"/> object used to define generic types and parameters.
    /// </summary>
    /// <remarks>Note that many properties and methods will throw an <see cref="InvalidOperationException"/> on this object, as denoted in the XML documentation.</remarks>
    public MethodDefinition InvokeMethod { get; }
    internal DelegateTypeDefinition(MethodDefinition invokeMethod, string typeName) : base(typeName)
    {
        IsDelegateIntl = true;
        InvokeMethod = invokeMethod;
    }
}