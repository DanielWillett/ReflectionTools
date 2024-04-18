using System;
using System.Reflection;
using DanielWillett.ReflectionTools.Formatting;
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Strongly-typed abstraction for instance <see cref="FieldInfo"/> and <see cref="PropertyInfo"/>.
/// </summary>
/// <remarks>See the <see cref="Variables"/> class for creation and utility methods.</remarks>
/// <typeparam name="TMemberType">The returning type of the member.</typeparam>
/// <typeparam name="TDeclaringType">The type in which this variable is defined in.</typeparam>
public interface IInstanceVariable<TDeclaringType, TMemberType> : IVariable
{
    /// <summary>
    /// Generates a delegate if possible, otherwise a dynamic method, that gets a property or field value. Works for reference or value types.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    /// <exception cref="InvalidOperationException">This member does not implement 'get' functionality when <paramref name="throwOnError"/> is <see langword="true"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    new InstanceGetter<TDeclaringType, TMemberType>? GenerateGetter(bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate if possible, otherwise a dynamic method, that gets a property or field value. For value types use <see cref="GenerateSetter"/>.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    /// <exception cref="InvalidOperationException">This member does not implement 'set' functionality when <paramref name="throwOnError"/> is <see langword="true"/>.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="TDeclaringType"/> is a value type, in which case <see cref="GenerateSetter"/> should be used.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<TDeclaringType, TMemberType>? GenerateReferenceTypeSetter(bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate if possible, otherwise a dynamic method, that sets an instance property value.
    /// When using value types, you have to store the value type in a boxed variable before passing it. This allows you to pass it as a reference type.<br/><br/>
    /// <code>
    /// object instance = new CustomStruct();
    /// SetProperty.Invoke(instance, 3);
    /// CustomStruct result = (CustomStruct)instance;
    /// </code>
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    /// <exception cref="InvalidOperationException">This member does not implement 'set' functionality when <paramref name="throwOnError"/> is <see langword="true"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    new InstanceSetter<object, TMemberType>? GenerateSetter(bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Get the value of the field or property with reflection.
    /// </summary>
    /// <param name="instance">The instance to get the value from.</param>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'get' functionality.</exception>
    TMemberType? GetValue(TDeclaringType instance);

    /// <summary>
    /// Set the value of the field or property with reflection.
    /// </summary>
    /// <param name="instance">The instance to get the value from. This is a <see langword="ref"/> parameter to support value types.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'set' functionality.</exception>
    void SetValue(scoped ref TDeclaringType instance, TMemberType? value);

    /// <summary>
    /// Set the value of the field or property with reflection. For value types use <see cref="SetValue(ref TDeclaringType,TMemberType?)"/>.
    /// </summary>
    /// <param name="instance">The instance to get the value from.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'set' functionality.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="TDeclaringType"/> is a value type, in which case the <see cref="SetValue(ref TDeclaringType,TMemberType?)"/> overload should be used.</exception>
    void SetValue(TDeclaringType instance, TMemberType? value);
}

/// <summary>
/// Strongly-typed abstraction for static <see cref="FieldInfo"/> and <see cref="PropertyInfo"/>.
/// </summary>
/// <remarks>See the <see cref="Variables"/> class for creation and utility methods.</remarks>
/// <typeparam name="TMemberType">The returning type of the member.</typeparam>
public interface IStaticVariable<TMemberType> : IVariable
{
    /// <summary>
    /// Generates a delegate that gets a static property or field value.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    /// <exception cref="InvalidOperationException">This member does not implement 'get' functionality when <paramref name="throwOnError"/> is <see langword="true"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    new StaticGetter<TMemberType>? GenerateGetter(bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate or dynamic method that sets a static property or field value.
    /// </summary>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    /// <exception cref="InvalidOperationException">This member does not implement 'set' functionality when <paramref name="throwOnError"/> is <see langword="true"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    new StaticSetter<TMemberType>? GenerateSetter(bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Get the value of the field or property with reflection.
    /// </summary>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'get' functionality.</exception>
    TMemberType? GetValue();

    /// <summary>
    /// Set the value of the field or property with reflection.
    /// </summary>
    /// <param name="value">Value to set.</param>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'set' functionality.</exception>
    void SetValue(TMemberType? value);
}

/// <summary>
/// Abstraction for <see cref="FieldInfo"/> and <see cref="PropertyInfo"/>.
/// </summary>
/// <remarks>See the <see cref="Variables"/> class for creation and utility methods.</remarks>
public interface IVariable
{
    /// <summary>
    /// If it is safe to get the value.
    /// </summary>
    bool CanGet { get; }

    /// <summary>
    /// If it is safe to set the value.
    /// </summary>
    bool CanSet { get; }

    /// <summary>
    /// If <see cref="Member"/> is <see langword="static"/>.
    /// </summary>
    bool IsStatic { get; }

    /// <summary>
    /// If this variable is a <see cref="PropertyInfo"/>.
    /// </summary>
    bool IsProperty { get; }

    /// <summary>
    /// If this variable is a <see cref="FieldInfo"/>.
    /// </summary>
    bool IsField { get; }

    /// <summary>
    /// The type <see cref="Member"/> is declared in.
    /// </summary>
    Type? DeclaringType { get; }

    /// <summary>
    /// The type <see cref="Member"/> returns (field or property type).
    /// </summary>
    Type MemberType { get; }

    /// <summary>
    /// Backing member, either a <see cref="FieldInfo"/> or <see cref="PropertyInfo"/>.
    /// </summary>
    MemberInfo Member { get; }

    /// <summary>
    /// Get the value of the field or property with reflection.
    /// </summary>
    /// <param name="instance">The instance to get the value from. Pass <see langword="null"/> for static variables.</param>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'get' functionality.</exception>
    object? GetValue(object? instance);

    /// <summary>
    /// Set the value of the field or property with reflection.
    /// </summary>
    /// <param name="instance">The instance to get the value from. Pass <see langword="null"/> for static variables.</param>
    /// <param name="value">Value to set.</param>
    /// <returns>The value of the variable.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'set' functionality.</exception>
    void SetValue(object? instance, object? value);

    /// <summary>
    /// Format this variable into a string representation using <see cref="Accessor.Formatter"/>.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    string Format(bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format this variable into a string representation.
    /// </summary>
    /// <param name="formatter">Implementation of <see cref="IOpCodeFormatter"/> to use.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    string Format(IOpCodeFormatter formatter, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Generates a delegate if possible, otherwise a dynamic method, that gets a property or field value. Works for reference or value types.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    /// <returns>A delegate of type <see cref="InstanceGetter{TInstance,T}"/> or <see cref="StaticGetter{T}"/> depending on if the variable is static or not, with the generic arguments being the same as how they were defined in the field.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'get' functionality when <paramref name="throwOnError"/> is <see langword="true"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateGetter(bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate if possible, otherwise a dynamic method, that sets a property or field value.
    /// When using value types, you have to store the value type in a boxed variable before passing it. This allows you to pass it as a reference type.<br/><br/>
    /// <code>
    /// object instance = new CustomStruct();
    /// SetProperty.Invoke(instance, 3);
    /// CustomStruct result = (CustomStruct)instance;
    /// </code>
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    /// <returns>A delegate of type <see cref="InstanceSetter{TInstance,T}"/> or <see cref="StaticSetter{T}"/> depending on if the variable is static or not, with the generic arguments being the same as how they were defined in the field.</returns>
    /// <exception cref="InvalidOperationException">This member does not implement 'set' functionality when <paramref name="throwOnError"/> is <see langword="true"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateSetter(bool throwOnError = false, bool allowUnsafeTypeBinding = false);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(Span{char}, bool, bool)"/> using <see cref="Accessor.Formatter"/>.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of this variable as a string.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int GetFormatLength(bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format this variable into a string representation using <see cref="Accessor.Formatter"/>. Use <see cref="GetFormatLength(bool, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of this variable as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(Span{char}, bool, bool)"/>.
    /// </summary>
    /// <param name="formatter">Implementation of <see cref="IOpCodeFormatter"/> to use.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of this variable as a string.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int GetFormatLength(IOpCodeFormatter formatter, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format this variable into a string representation. Use <see cref="GetFormatLength(bool, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="formatter">Implementation of <see cref="IOpCodeFormatter"/> to use.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of this variable as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(IOpCodeFormatter formatter, Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false);
#endif
}