using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Reflection;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// See <see cref="Variables.AsInstanceVariable{TDeclaringType,TMemberType}(FieldInfo)"/>.
/// </summary>
internal sealed class InstanceFieldVariable<TDeclaringType, TMemberType> : FieldVariable, IInstanceVariable<TDeclaringType, TMemberType>, IEquatable<IInstanceVariable<TDeclaringType, TMemberType>>
{
    private readonly bool _isInstanceValueType;
    public InstanceFieldVariable(FieldInfo field) : base(field)
    {
        if (field.FieldType != typeof(TMemberType))
            throw new ArgumentException($"Member type is {Accessor.ExceptionFormatter.Format(field.FieldType)} but expected {Accessor.ExceptionFormatter.Format(typeof(TMemberType))}.", nameof(field));

        if (field.DeclaringType == null)
            throw new ArgumentException($"Declaring type is null but expected {Accessor.ExceptionFormatter.Format(typeof(TDeclaringType))}.", nameof(field));

        if (!field.DeclaringType.IsAssignableFrom(typeof(TDeclaringType)))
            throw new ArgumentException($"Declaring type is {Accessor.ExceptionFormatter.Format(field.DeclaringType)} but expected {Accessor.ExceptionFormatter.Format(typeof(TDeclaringType))} or one of it's parents.", nameof(field));

        if (IsStatic)
            throw new ArgumentException("Property is static but expected instance.", nameof(field));

        _isInstanceValueType = field.DeclaringType.IsValueType;
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
        return (InstanceGetter<TDeclaringType, TMemberType>?)((IVariable)this).GenerateGetter(throwOnError, allowUnsafeTypeBinding);
    }
    public InstanceSetter<TDeclaringType, TMemberType>? GenerateReferenceTypeSetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        if (_isInstanceValueType)
            throw new ArgumentException($"{Accessor.ExceptionFormatter.Format(typeof(TDeclaringType))} is a value type. Use 'GenerateSetter' which returns a setter with a boxed instance argument.");

        return (InstanceSetter<TDeclaringType, TMemberType>?)((IVariable)this).GenerateSetter(throwOnError, allowUnsafeTypeBinding);
    }
    public new InstanceSetter<object, TMemberType>? GenerateSetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return Accessor.GenerateInstanceSetter<TMemberType>(Field, throwOnError);
    }
}

/// <summary>
/// See <see cref="Variables.AsStaticVariable{TMemberType}(FieldInfo)"/>.
/// </summary>
internal sealed class StaticFieldVariable<TMemberType> : FieldVariable, IStaticVariable<TMemberType>, IEquatable<IStaticVariable<TMemberType>>
{
    public StaticFieldVariable(FieldInfo field) : base(field)
    {
        if (field.FieldType != typeof(TMemberType))
            throw new ArgumentException($"Member type is {Accessor.ExceptionFormatter.Format(field.FieldType)} but expected {Accessor.ExceptionFormatter.Format(typeof(TMemberType))}.", nameof(field));

        if (!IsStatic)
            throw new ArgumentException("Property is instance but expected static.", nameof(field));
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
/// See <see cref="Variables.AsVariable(FieldInfo)"/>.
/// </summary>
internal class FieldVariable : IVariable, IEquatable<IVariable>
{
    private protected readonly FieldInfo Field;
    public bool CanGet => true;
    public bool CanSet => true;
    public bool IsProperty => false;
    public bool IsField => true;
    public Type? DeclaringType => Field.DeclaringType;
    public Type MemberType => Field.FieldType;
    public bool IsStatic => Field.IsStatic;
    public MemberInfo Member => Field;
    public FieldVariable(FieldInfo field)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
    }
    public object? GetValue(object? instance) => Field.GetValue(instance);
    public void SetValue(object? instance, object? value) => Field.SetValue(instance, value);
    public override string ToString() => Field.ToString()!;
    public bool Equals(IVariable? other) => Field.Equals(other?.Member);
    public override bool Equals(object? obj) => obj switch
    {
        FieldInfo field => Field.Equals(field),
        IVariable variable => Field.Equals(variable.Member),
        _ => false
    };
    public override int GetHashCode() => Field.GetHashCode();
    public string Format(bool includeAccessors = true, bool includeDefinitionKeywords = false) => Accessor.Formatter.Format(Field, includeDefinitionKeywords);
    public string Format(IOpCodeFormatter formatter, bool includeAccessors = true, bool includeDefinitionKeywords = false) => formatter.Format(Field, includeDefinitionKeywords);
    public Delegate? GenerateGetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return Field.IsStatic ? Accessor.GenerateStaticGetter(Field, throwOnError) : Accessor.GenerateInstanceGetter(Field, throwOnError);
    }
    public Delegate? GenerateSetter(bool throwOnError = true, bool allowUnsafeTypeBinding = false)
    {
        return Field.IsStatic ? Accessor.GenerateStaticSetter(Field, throwOnError) : Accessor.GenerateInstanceSetter(Field, throwOnError);
    }
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    public int GetFormatLength(bool includeAccessors = true, bool includeDefinitionKeywords = false) => Accessor.Formatter.GetFormatLength(Field, includeDefinitionKeywords);
    public int Format(Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false) => Accessor.Formatter.Format(Field, output, includeDefinitionKeywords);
    public int GetFormatLength(IOpCodeFormatter formatter, bool includeAccessors = true, bool includeDefinitionKeywords = false) => formatter.GetFormatLength(Field, includeDefinitionKeywords);
    public int Format(IOpCodeFormatter formatter, Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false) => formatter.Format(Field, output, includeDefinitionKeywords);
#endif
}