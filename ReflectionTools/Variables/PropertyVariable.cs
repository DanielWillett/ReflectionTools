using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Reflection;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// See <see cref="Variables.AsInstanceVariable{TDeclaringType,TMemberType}(PropertyInfo,IAccessor)"/>.
/// </summary>
internal sealed class InstancePropertyVariable<TDeclaringType, TMemberType> : PropertyVariable, IInstanceVariable<TDeclaringType, TMemberType>, IEquatable<IInstanceVariable<TDeclaringType, TMemberType>>
{
    private readonly bool _isInstanceValueType;
    internal InstancePropertyVariable(PropertyInfo property, IAccessor accessor) : base(property, accessor)
    {
        if (property.PropertyType != typeof(TMemberType))
            throw new ArgumentException($"Member type is {accessor.ExceptionFormatter.Format(property.PropertyType)} but expected {accessor.ExceptionFormatter.Format(typeof(TMemberType))}.", nameof(property));

        if (property.DeclaringType == null)
            throw new ArgumentException($"Declaring type is null but expected {accessor.ExceptionFormatter.Format(typeof(TDeclaringType))}.", nameof(property));

        if (!property.DeclaringType.IsAssignableFrom(typeof(TDeclaringType)))
            throw new ArgumentException($"Declaring type is {accessor.ExceptionFormatter.Format(property.DeclaringType)} but expected {accessor.ExceptionFormatter.Format(typeof(TDeclaringType))} or one of it's parents.", nameof(property));

        if (IsStatic)
            throw new ArgumentException("Property is static but expected instance.", nameof(property));

        _isInstanceValueType = typeof(TDeclaringType).IsValueType;
    }
    public bool Equals(IInstanceVariable<TDeclaringType, TMemberType>? other) => ((IVariable)this).Equals(other);
    public TMemberType? GetValue(TDeclaringType instance) => (TMemberType?)((IVariable)this).GetValue(instance);
    public void SetValue(TDeclaringType instance, TMemberType? value)
    {
        if (_isInstanceValueType)
            throw new ArgumentException($"{Accessor.ExceptionFormatter.Format(typeof(TDeclaringType))} is a value type. Use the by-ref overload for set-value instead.");
        
        ((IVariable)this).SetValue(instance, value);
    }
    public void SetValue(scoped ref TDeclaringType instance, TMemberType? value)
    {
        if (_isInstanceValueType)
        {
            object? boxed = instance;

            SetValue(boxed, value);

            instance = (TDeclaringType?)boxed!;
        }
        else
        {
            ((IVariable)this).SetValue(instance, value);
        }
    }
    public new InstanceGetter<TDeclaringType, TMemberType>? GenerateGetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return Accessor.GenerateInstancePropertyGetter<TDeclaringType, TMemberType>(Property, throwOnError, allowUnsafeTypeBinding);
    }
    public InstanceSetter<TDeclaringType, TMemberType>? GenerateReferenceTypeSetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        if (_isInstanceValueType)
            throw new ArgumentException($"{Accessor.ExceptionFormatter.Format(typeof(TDeclaringType))} is a value type. Use 'GenerateSetter' which returns a setter with a boxed instance argument.");

        return (InstanceSetter<TDeclaringType, TMemberType>?)Accessor.GenerateInstancePropertySetter(Property, throwOnError);
    }
    public new InstanceSetter<object, TMemberType>? GenerateSetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return Accessor.GenerateInstancePropertySetter<TMemberType>(Property, throwOnError);
    }
}

/// <summary>
/// See <see cref="Variables.AsStaticVariable{TMemberType}(PropertyInfo,IAccessor)"/>.
/// </summary>
internal sealed class StaticPropertyVariable<TMemberType> : PropertyVariable, IStaticVariable<TMemberType>, IEquatable<IStaticVariable<TMemberType>>
{
    internal StaticPropertyVariable(PropertyInfo property, IAccessor accessor) : base(property, accessor)
    {
        if (property.PropertyType != typeof(TMemberType))
            throw new ArgumentException($"Member type is {accessor.ExceptionFormatter.Format(property.PropertyType)} but expected {accessor.ExceptionFormatter.Format(typeof(TMemberType))}.", nameof(property));

        if (!IsStatic)
            throw new ArgumentException("Property is instance but expected static.", nameof(property));
    }
    public bool Equals(IStaticVariable<TMemberType>? other) => ((IVariable)this).Equals(other);
    public TMemberType? GetValue() => (TMemberType?)GetValue(null);
    public void SetValue(TMemberType? value) => SetValue(null, value);
    public new StaticGetter<TMemberType>? GenerateGetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return (StaticGetter<TMemberType>?)((IVariable)this).GenerateGetter(throwOnError, allowUnsafeTypeBinding);
    }
    public new StaticSetter<TMemberType>? GenerateSetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return (StaticSetter<TMemberType>?)((IVariable)this).GenerateSetter(throwOnError, allowUnsafeTypeBinding);
    }
}

/// <summary>
/// <see cref="Variables"/>
/// </summary>
internal class PropertyVariable : IVariable, IEquatable<IVariable>
{
#if !NET461_OR_GREATER && NETFRAMEWORK
    private static readonly object[] EmptyObjArray = [ ];
#endif
    private protected readonly PropertyInfo Property;
    private protected readonly IAccessor Accessor;
    private readonly MethodInfo? _getter;
    private readonly MethodInfo? _setter;
    public bool CanGet { get; }
    public bool CanSet { get; }
    public bool IsStatic { get; }
    public bool IsProperty => true;
    public bool IsField => false;
    public Type? DeclaringType => Property.DeclaringType;
    public Type MemberType => Property.PropertyType;
    public MemberInfo Member => Property;
    internal PropertyVariable(PropertyInfo property, IAccessor accessor)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        _getter = property.GetGetMethod();
        _setter = property.GetSetMethod();
        CanGet = _getter != null && _getter.GetParameters().Length == 0;
        CanSet = _setter != null && _setter.GetParameters().Length == 1;
        IsStatic = _getter == null ? _setter != null && _setter.IsStatic : _getter.IsStatic;
        Accessor = accessor;
    }
    public object? GetValue(object? instance)
    {
        if (!CanGet)
            throw new InvalidOperationException("Property \"" + Accessor.Formatter.Format(Property) + "\" does not define a getter.");

#if NET461_OR_GREATER || !NETFRAMEWORK
        return _getter!.Invoke(instance, Array.Empty<object>());
#else
        return _getter!.Invoke(instance, EmptyObjArray);
#endif
    }
    public void SetValue(object? instance, object? value)
    {
        if (!CanSet)
            throw new InvalidOperationException("Property \"" + Accessor.Formatter.Format(Property) + "\" does not define a setter.");

        _setter!.Invoke(instance, [ value ]);
    }
    public Delegate? GenerateGetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return IsStatic ? Accessor.GenerateStaticPropertyGetter(Property, throwOnError) : Accessor.GenerateInstancePropertyGetter(Property, throwOnError);
    }
    public Delegate? GenerateSetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return IsStatic ? Accessor.GenerateStaticPropertySetter(Property, throwOnError) : Accessor.GenerateInstancePropertySetter(Property, throwOnError);
    }
    public override string ToString() => Property.ToString()!;
    public bool Equals(IVariable? other) => Property.Equals(other?.Member);
    public override bool Equals(object? obj) => obj switch
    {
        PropertyInfo property => Property.Equals(property),
        IVariable variable => Property.Equals(variable.Member),
        _ => false
    };
    public override int GetHashCode() => Property.GetHashCode();
    public string Format(bool includeAccessors = true, bool includeDefinitionKeywords = false) => Accessor.Formatter.Format(Property, includeDefinitionKeywords);
    public string Format(IOpCodeFormatter formatter, bool includeAccessors = true, bool includeDefinitionKeywords = false) => formatter.Format(Property, includeDefinitionKeywords);
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    public int GetFormatLength(bool includeAccessors = true, bool includeDefinitionKeywords = false) => Accessor.Formatter.GetFormatLength(Property, includeDefinitionKeywords);
    public int Format(Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false) => Accessor.Formatter.Format(Property, output, includeDefinitionKeywords);
    public int GetFormatLength(IOpCodeFormatter formatter, bool includeAccessors = true, bool includeDefinitionKeywords = false) => formatter.GetFormatLength(Property, includeDefinitionKeywords);
    public int Format(IOpCodeFormatter formatter, Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false) => formatter.Format(Property, output, includeDefinitionKeywords);
#endif
}