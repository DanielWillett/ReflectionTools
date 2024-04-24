using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Represents a shell of a property for formatting purposes.
/// </summary>
public class PropertyDefinition : IMemberDefinition
{
    internal List<MethodParameterDefinition>? IndexParametersIntl;
    private ReadOnlyCollection<MethodParameterDefinition>? _readOnlyParameters;

    /// <summary>
    /// All parameters in the property if it's an indexer. <see langword="null"/> if no parameters are provided (making this property not an indexer).
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/> (parameters not specified).</remarks>
#if !NETFRAMEWORK || NET45_OR_GREATER
    public IReadOnlyList<MethodParameterDefinition>? IndexParameters
#else
    public ReadOnlyCollection<MethodParameterDefinition>? IndexParameters
#endif
    {
        get
        {
            if (_readOnlyParameters != null)
                return _readOnlyParameters;

            if (IndexParametersIntl == null)
                return null;

            _readOnlyParameters = new ReadOnlyCollection<MethodParameterDefinition>(IndexParametersIntl);

            return _readOnlyParameters;
        }
    }

    /// <summary>
    /// Name of the property.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Type the property is declared in.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public Type? DeclaringType { get; set; }

    /// <summary>
    /// Type the property stores.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public Type? PropertyType { get; set; }

    /// <summary>
    /// If the method requires an instance of <see cref="DeclaringType"/> to be accessed.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    public bool IsStatic { get; set; }

    /// <summary>
    /// If this property has a <see langword="get"/> accessor.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool HasGetter { get; set; } = true;

    /// <summary>
    /// If this property has a <see langword="set"/> accessor.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool HasSetter { get; set; } = true;

    /// <summary>
    /// Create a property definition, starting with a property name.
    /// </summary>
    public PropertyDefinition(string propertyName)
    {
        Name = propertyName;
    }

    /// <summary>
    /// Create a property definition from an existing property.
    /// </summary>
    public static PropertyDefinition FromProperty(PropertyInfo property)
    {
        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetGetMethod(true);

        return new PropertyDefinition(property.Name)
        {
            IsStatic = getMethod != null && getMethod.IsStatic || setMethod != null && setMethod.IsStatic,
            DeclaringType = property.DeclaringType,
            HasGetter = getMethod != null,
            HasSetter = setMethod != null,
            PropertyType = property.PropertyType
        };
    }

    /// <summary>
    /// Specify that this property has no <see langword="get"/> accessor.
    /// </summary>
    public PropertyDefinition WithNoGetter()
    {
        HasGetter = false;
        return this;
    }

    /// <summary>
    /// Specify that this property has no <see langword="set"/> accessor.
    /// </summary>
    public PropertyDefinition WithNoSetter()
    {
        HasSetter = false;
        return this;
    }

    /// <summary>
    /// Set the type stored in the property.
    /// </summary>
    public PropertyDefinition WithPropertyType<TPropertyType>()
    {
        PropertyType = typeof(TPropertyType);
        return this;
    }

    /// <summary>
    /// Set the type stored in the property.
    /// </summary>
    public PropertyDefinition WithPropertyType(Type propertyType)
    {
        PropertyType = propertyType;
        return this;
    }

    /// <summary>
    /// Set the type stored in the property.
    /// </summary>
    public PropertyDefinition WithPropertyType(string propertyType)
    {
        PropertyType = Type.GetType(propertyType);
        return this;
    }

    /// <summary>
    /// Set the declaring type of the property.
    /// </summary>
    public PropertyDefinition DeclaredIn<TDeclaringType>(bool isStatic)
    {
        DeclaringType = typeof(TDeclaringType);
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the property.
    /// </summary>
    public PropertyDefinition DeclaredIn(Type declaringType, bool isStatic)
    {
        DeclaringType = declaringType;
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the property.
    /// </summary>
    public PropertyDefinition DeclaredIn(string declaringType, bool isStatic)
    {
        DeclaringType = Type.GetType(declaringType);
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    /// <exception cref="ArgumentException">Generic types are not supported in PropertyDefinitions.</exception>
    public PropertyDefinition WithParameter(in MethodParameterDefinition parameter)
    {
        if (parameter.GenericTypeIndex >= 0)
            throw new ArgumentException("Generic types are not supported in PropertyDefinitions.", nameof(parameter));

        if (IndexParametersIntl == null)
        {
            IndexParametersIntl = new List<MethodParameterDefinition>(1);
            _readOnlyParameters = null;
        }

        IndexParametersIntl.Add(parameter);
        return this;
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter(Type type, string? name)
    {
        return WithParameter(new MethodParameterDefinition(type, name));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter(Type type, string? name, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(type, name, isParams));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter(Type type, string? name, ByRefTypeMode byRefMode)
    {
        return WithParameter(new MethodParameterDefinition(type, name, byRefMode));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter(Type type, string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(type, name, byRefMode, isParams));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter<TParamType>(string? name)
    {
        return WithParameter(new MethodParameterDefinition(typeof(TParamType), name));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter<TParamType>(string? name, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(typeof(TParamType), name, isParams));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter<TParamType>(string? name, ByRefTypeMode byRefMode)
    {
        return WithParameter(new MethodParameterDefinition(typeof(TParamType), name, byRefMode));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter<TParamType>(string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(typeof(TParamType), name, byRefMode, isParams));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter(string type, string? name)
    {
        return WithParameter(new MethodParameterDefinition(Type.GetType(type), name));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter(string type, string? name, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(Type.GetType(type), name, isParams));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter(string type, string? name, ByRefTypeMode byRefMode)
    {
        return WithParameter(new MethodParameterDefinition(Type.GetType(type), name, byRefMode));
    }

    /// <summary>
    /// Add a parameter to the index parameter list.
    /// </summary>
    public PropertyDefinition WithParameter(string type, string? name, ByRefTypeMode byRefMode, bool isParams)
    {
        return WithParameter(new MethodParameterDefinition(Type.GetType(type), name, byRefMode, isParams));
    }

    IMemberDefinition IMemberDefinition.NestedIn<TDeclaringType>(bool isStatic) => DeclaredIn<TDeclaringType>(isStatic);
    IMemberDefinition IMemberDefinition.NestedIn(Type declaringType, bool isStatic) => DeclaredIn(declaringType, isStatic);
    IMemberDefinition IMemberDefinition.NestedIn(string declaringType, bool isStatic) => DeclaredIn(declaringType, isStatic);

    /// <inheritdoc />
    public override string ToString() => Accessor.ExceptionFormatter.Format(this);
}