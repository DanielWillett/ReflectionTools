using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Reflection utilities for accessing private or internal members. Interface for <see cref="Accessor"/>.
/// </summary>
public interface IAccessor
{
    /// <summary>
    /// Should <see cref="Logger"/> log generated IL code (as debug messages)?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    bool LogILTraceMessages { get; set; }

    /// <summary>
    /// Should <see cref="Logger"/> log debug messages?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    bool LogDebugMessages { get; set; }

    /// <summary>
    /// Should <see cref="Logger"/> log info messages?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    bool LogInfoMessages { get; set; }

    /// <summary>
    /// Should <see cref="Logger"/> log warning messages?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    bool LogWarningMessages { get; set; }

    /// <summary>
    /// Should <see cref="Logger"/> log error messages?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    bool LogErrorMessages { get; set; }

    /// <summary>
    /// Logging IO for all methods in library.
    /// <para>Assigning a value to will dispose the previous value if needed.</para>
    /// </summary>
    /// <remarks>Default value is an instance of <see cref="ConsoleReflectionToolsLogger"/>, which outputs to <see cref="Console"/>.
    /// By assigning a value to this property, you transfer ownership of the object to this class, meaning it shouldn't be used or disposed outside this class at all.</remarks>
    IReflectionToolsLogger? Logger { get; set; }

    /// <summary>
    /// Logging IO for all methods in library for standard output.
    /// <para>Assigning a value to will dispose the previous value if needed.</para>
    /// </summary>
    /// <remarks>Default value is an instance of <see cref="ConsoleReflectionToolsLogger"/>, which outputs to <see cref="Console"/>.
    /// By assigning a value to this property, you transfer ownership of the object to this class, meaning it shouldn't be used or disposed outside this class at all.</remarks>
    IOpCodeFormatter Formatter { get; set; }

    /// <summary>
    /// Logging IO for all methods in library for exceptions.
    /// <para>Assigning a value to will dispose the previous value if needed.</para>
    /// </summary>
    /// <remarks>Default value is an instance of <see cref="ConsoleReflectionToolsLogger"/>, which outputs to <see cref="Console"/>.
    /// By assigning a value to this property, you transfer ownership of the object to this class, meaning it shouldn't be used or disposed outside this class at all.</remarks>
    IOpCodeFormatter ExceptionFormatter { get; set; }

    /// <summary>
    /// System primary assembly.
    /// </summary>
    /// <remarks>Lazily cached.</remarks>
    /// <exception cref="TypeLoadException"/>
    Assembly MSCoreLib { get; }

    /// <summary>
    /// Whether or not the <c>Mono.Runtime</c> class is available. Indicates if the current runtime is Mono.
    /// </summary>
    bool IsMono { get; }

    /// <summary>
    /// Generates a dynamic method that sets an instance field value. For value types use <see cref="GenerateInstanceSetter{TValue}(Type,string,bool)"/> instead.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<TInstance, TValue>? GenerateInstanceSetter<TInstance, TValue>(string fieldName, bool throwOnError = false) where TInstance : class;
    /// <summary>
    /// Generates a dynamic method that sets an instance field value. For value types use <see cref="GenerateInstanceSetter{TValue}(FieldInfo,bool)"/> instead.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<TInstance, TValue>? GenerateInstanceSetter<TInstance, TValue>(FieldInfo field, bool throwOnError = false) where TInstance : class;

    /// <summary>
    /// Generates a dynamic method that sets an instance field value.
    /// </summary>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    /// <returns>A delegate of type <see cref="InstanceSetter{TInstance,T}"/> with the generic arguments being the same as how they were defined in the field. If <paramref name="field"/>'s declaring type is a value type, <c>TInstance</c> will be <see cref="object"/> instead of the declaring type to allow setting of value type fields via boxing.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateInstanceSetter(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets an instance field value. Works for reference or value types.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceGetter<TInstance, TValue>? GenerateInstanceGetter<TInstance, TValue>(string fieldName, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets an instance field value. Works for reference or value types.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceGetter<TInstance, TValue>? GenerateInstanceGetter<TInstance, TValue>(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets an instance field value. Works for reference or value types.
    /// </summary>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <returns>A delegate of type <see cref="InstanceGetter{TInstance,T}"/> with the generic arguments being the same as how they were defined in the field.</returns>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateInstanceGetter(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that sets an instance field value.
    /// When using value types, you have to store the value type in a boxed variable before passing it. This allows you to pass it as a reference type.<br/><br/>
    /// <code>
    /// object instance = new CustomStruct();
    /// SetField.Invoke(instance, 3);
    /// CustomStruct result = (CustomStruct)instance;
    /// </code>
    /// </summary>
    /// <param name="declaringType">Declaring type of the field.</param>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<object, TValue>? GenerateInstanceSetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that sets an instance field value.
    /// When using value types, you have to store the value type in a boxed variable before passing it. This allows you to pass it as a reference type.<br/><br/>
    /// <code>
    /// object instance = new CustomStruct();
    /// SetField.Invoke(instance, 3);
    /// CustomStruct result = (CustomStruct)instance;
    /// </code>
    /// </summary>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<object, TValue>? GenerateInstanceSetter<TValue>(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets an instance field value. Works for reference or value types.
    /// </summary>
    /// <param name="declaringType">Declaring type of the field.</param>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceGetter<object, TValue>? GenerateInstanceGetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets an instance field value. Works for reference or value types.
    /// </summary>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceGetter<object, TValue>? GenerateInstanceGetter<TValue>(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a delegate that sets an instance property value. For value types use <see cref="GenerateInstancePropertySetter{TValue}(Type,string,bool,bool)"/> instead.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <typeparam name="TInstance">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<TInstance, TValue>? GenerateInstancePropertySetter<TInstance, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TInstance : class;

    /// <summary>
    /// Generates a delegate that gets an instance property value. Works for reference or value types.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <typeparam name="TInstance">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceGetter<TInstance, TValue>? GenerateInstancePropertyGetter<TInstance, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate that sets an instance property value. For value types use <see cref="GenerateInstancePropertySetter{TValue}(PropertyInfo,bool,bool)"/> instead.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<TInstance, TValue>? GenerateInstancePropertySetter<TInstance, TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TInstance : class;

    /// <summary>
    /// Generates a delegate that gets an instance property value. Works for reference or value types.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <typeparam name="TInstance">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceGetter<TInstance, TValue>? GenerateInstancePropertyGetter<TInstance, TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate that sets an instance property value.
    /// </summary>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <returns>A delegate of type <see cref="InstanceSetter{TInstance,T}"/> with the generic arguments being the same as how they were defined in the field. If <paramref name="property"/>'s declaring type is a value type, <c>TInstance</c> will be <see cref="object"/> instead of the declaring type to allow setting of value type fields via boxing.</returns>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateInstancePropertySetter(PropertyInfo property, bool throwOnError = false);

    /// <summary>
    /// Generates a delegate that gets an instance property value. Works for reference or value types.
    /// </summary>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <returns>A delegate of type <see cref="InstanceGetter{TInstance,T}"/> with the generic arguments being the same as how they were defined in the property.</returns>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateInstancePropertyGetter(PropertyInfo property, bool throwOnError = false);

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
    /// <param name="declaringType">Declaring type of the property.</param>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<object, TValue>? GenerateInstancePropertySetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate if possible, otherwise a dynamic method, that gets an instance property value. Works for reference or value types.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="declaringType">Declaring type of the property.</param>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceGetter<object, TValue>? GenerateInstancePropertyGetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

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
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceSetter<object, TValue>? GenerateInstancePropertySetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate if possible, otherwise a dynamic method, that gets an instance property value. Works for reference or value types.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    InstanceGetter<object, TValue>? GenerateInstancePropertyGetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a dynamic method that sets a static field value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of the field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticSetter<TValue>? GenerateStaticSetter<TDeclaringType, TValue>(string fieldName, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets a static field value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of the field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticGetter<TValue>? GenerateStaticGetter<TDeclaringType, TValue>(string fieldName, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that sets a static field value.
    /// </summary>
    /// <param name="declaringType">Declaring type of the field.</param>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of the field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticSetter<TValue>? GenerateStaticSetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that sets a static field value.
    /// </summary>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticSetter<TValue>? GenerateStaticSetter<TValue>(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets a static field value.
    /// </summary>
    /// <param name="declaringType">Declaring type of the field.</param>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of the field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticGetter<TValue>? GenerateStaticGetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that sets a static field value.
    /// </summary>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <returns>A delegate of type <see cref="StaticSetter{T}"/> with the generic argument being the type of the field.</returns>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateStaticSetter(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets a static field value.
    /// </summary>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticGetter<TValue>? GenerateStaticGetter<TValue>(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a dynamic method that gets a static field value.
    /// </summary>
    /// <param name="field">Field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <returns>A delegate of type <see cref="StaticGetter{T}"/> with the generic argument being the type of the field.</returns>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateStaticGetter(FieldInfo field, bool throwOnError = false);

    /// <summary>
    /// Generates a delegate or dynamic method that sets a static property value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticSetter<TValue>? GenerateStaticPropertySetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate that gets a static property value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticGetter<TValue>? GenerateStaticPropertyGetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false);

    /// <summary>
    /// Generates a delegate that gets a static property value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    // ReSharper disable once MethodOverloadWithOptionalParameter
    StaticGetter<TValue>? GenerateStaticPropertyGetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = true);

    /// <summary>
    /// Generates a delegate or dynamic method that sets a static property value.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="declaringType">Declaring type of the property.</param>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticSetter<TValue>? GenerateStaticPropertySetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate that gets a static property value.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="declaringType">Declaring type of the property.</param>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticGetter<TValue>? GenerateStaticPropertyGetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate or dynamic method that sets a static property value.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticSetter<TValue>? GenerateStaticPropertySetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate that gets a static property value.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    StaticGetter<TValue>? GenerateStaticPropertyGetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate or dynamic method that sets a static property value.
    /// </summary>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <returns>A delegate of type <see cref="StaticSetter{T}"/> with the generic argument being the type of the property.</returns>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateStaticPropertySetter(PropertyInfo property, bool throwOnError = false);

    /// <summary>
    /// Generates a delegate that gets a static property value.
    /// </summary>
    /// <param name="property">Property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <returns>A delegate of type <see cref="StaticGetter{T}"/> with the generic argument being the type of the property.</returns>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateStaticPropertyGetter(PropertyInfo property, bool throwOnError = false);

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method. For non-readonly methods with value types, the instance must be boxed (passed as <see cref="object"/>) to keep any changes made.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature. The first parameter will be the instance.</returns>
    /// <param name="methodName">Name of method that will be called.</param>
    /// <param name="parameters">Optional parameter list for resolving ambiguous methods.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateInstanceCaller<TInstance>(string methodName, Type[]? parameters = null, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method. For non-readonly methods with value types, the instance must be boxed (passed as <see cref="object"/>) to keep any changes made.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="methodName">Name of method that will be called.</param>
    /// <param name="parameters">Optional parameter list for resolving ambiguous methods.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    TDelegate? GenerateInstanceCaller<TInstance, TDelegate>(string methodName, bool throwOnError = false, bool allowUnsafeTypeBinding = false, Type[]? parameters = null) where TDelegate : Delegate;

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method. For non-readonly methods with value types, the instance must be boxed (passed as <see cref="object"/>) to keep any changes made.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature. The first parameter will be the instance.</returns>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateInstanceCaller(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method. For non-readonly methods with value types, the instance must be boxed (passed as <see cref="object"/>) to keep any changes made.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    TDelegate? GenerateInstanceCaller<TDelegate>(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TDelegate : Delegate;

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method. For non-readonly methods with value types, the instance must be boxed (passed as <see cref="object"/>) to keep any changes made.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <param name="delegateType">Type of delegate to return.</param>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateInstanceCaller(Type delegateType, MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature.</returns>
    /// <param name="methodName">Name of method that will be called.</param>
    /// <param name="parameters">Optional parameter list for resolving ambiguous methods.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateStaticCaller<TDeclaringType>(string methodName, Type[]? parameters = null, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <param name="methodName">Name of method that will be called.</param>
    /// <param name="parameters">Optional parameter list for resolving ambiguous methods.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    TDelegate? GenerateStaticCaller<TDeclaringType, TDelegate>(string methodName, bool throwOnError = false, bool allowUnsafeTypeBinding = false, Type[]? parameters = null) where TDelegate : Delegate;

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature.</returns>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateStaticCaller(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    TDelegate? GenerateStaticCaller<TDelegate>(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TDelegate : Delegate;

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="delegateType">Type of delegate to return.</param>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Delegate? GenerateStaticCaller(Type delegateType, MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false);

    /// <summary>
    /// Gets platform-specific flags for creating dynamic methods.
    /// </summary>
    /// <param name="static">Whether or not the method has no 'instance', only considered when on mono.</param>
    /// <param name="attributes">Method attributes to pass to <see cref="DynamicMethod"/> constructor.</param>
    /// <param name="convention">Method convention to pass to <see cref="DynamicMethod"/> constructor.</param>
    void GetDynamicMethodFlags(bool @static, out MethodAttributes attributes, out CallingConventions convention);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of a <paramref name="type"/>.
    /// </summary>
    /// <remarks>Takes nested types into account, returning the lowest visibility in the nested hierarchy
    /// (ex. if an internal class is nested in a private nested type, method will consider it private).</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetVisibility(Type type);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of a <paramref name="method"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetVisibility(MethodBase method);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of a <paramref name="field"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetVisibility(FieldInfo field);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of a <paramref name="property"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetVisibility(PropertyInfo property);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of an <paramref name="event"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetVisibility(EventInfo @event);

    /// <summary>
    /// Get the highest visibilty needed for both of the given methods to be visible. Methods which are <see langword="null"/> are ignored.
    /// </summary>
    /// <remarks>Useful for getting property visiblity manually, will always be at least <see cref="MemberVisibility.Private"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetHighestVisibility(MethodBase? method1, MethodBase? method2);

    /// <summary>
    /// Get the highest visibilty needed for all three of the given methods to be visible. Methods which are <see langword="null"/> are ignored.
    /// </summary>
    /// <remarks>Useful for getting event visiblity manually, will always be at least <see cref="MemberVisibility.Private"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetHighestVisibility(MethodBase? method1, MethodBase? method2, MethodBase? method3);

    /// <summary>
    /// Get the highest visibilty needed for all of the given methods to be visible. Methods which are <see langword="null"/> are ignored.
    /// </summary>
    /// <remarks>Will always be at least <see cref="MemberVisibility.Private"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetHighestVisibility(params MethodBase?[] methods);

#if !NETSTANDARD
    /// <summary>
    /// Get the highest visibilty needed for all of the given methods to be visible. Methods which are <see langword="null"/> are ignored.
    /// </summary>
    /// <remarks>Will always be at least <see cref="MemberVisibility.Private"/>. All parameters should be at least of type <see cref="MethodBase"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MemberVisibility GetHighestVisibility(__arglist);
#endif

    /// <summary>
    /// Checks if <paramref name="assembly"/> has a <see cref="InternalsVisibleToAttribute"/> with the given <paramref name="assemblyName"/>.
    /// The value of the attribute must match <paramref name="assemblyName"/> exactly.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool AssemblyGivesInternalAccess(Assembly assembly, string assemblyName);

    /// <summary>
    /// Checks <paramref name="method"/> for the <see langword="extern"/> flag.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsExtern(MethodBase method);

    /// <summary>
    /// Checks <paramref name="field"/> for the <see langword="extern"/> flag.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsExtern(FieldInfo field);

    /// <summary>
    /// Checks <paramref name="property"/>'s getter and setter for the <see langword="extern"/> flag.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsExtern(PropertyInfo property, bool checkGetterFirst = true);

    /// <summary>
    /// Checks for the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <remarks>Alias of <see cref="HasAttributeSafe{TAttribute}"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsDefinedSafe<TAttribute>(ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute;

    /// <summary>
    /// Checks for the attribute of type <paramref name="attributeType"/> on <paramref name="member"/>.
    /// </summary>
    /// <remarks>Alias of <see cref="HasAttributeSafe"/>.</remarks>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="attributeType">Type of the attribute to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <exception cref="ArgumentException"><paramref name="attributeType"/> did not derive from <see cref="Attribute"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsDefinedSafe(ICustomAttributeProvider member, Type attributeType, bool inherit = false);

    /// <summary>
    /// Checks for the attribute of type <c>System.Runtime.CompilerServices.<paramref name="typeName"/></c> on <paramref name="member"/>.
    /// </summary>
    /// <remarks>Alias of <see cref="HasCompilerAttributeSafe"/>. In some older versions of .NET Framework, attributes not available in the API at that version will be added to the assembly on compile. This checks for those.</remarks>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="typeName">Type name of the attribute in <c>System.Runtime.CompilerServices</c> to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsCompilerAttributeDefinedSafe(ICustomAttributeProvider member, string typeName, bool inherit = false);

    /// <summary>
    /// Checks for the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <remarks>Alias of <see cref="IsDefinedSafe{TAttribute}"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool HasAttributeSafe<TAttribute>(ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute;

    /// <summary>
    /// Checks for the attribute of type <paramref name="attributeType"/> on <paramref name="member"/>.
    /// </summary>
    /// <remarks>Alias of <see cref="IsDefinedSafe"/>.</remarks>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="attributeType">Type of the attribute to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <exception cref="ArgumentException"><paramref name="attributeType"/> did not derive from <see cref="Attribute"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool HasAttributeSafe(ICustomAttributeProvider member, Type attributeType, bool inherit = false);

    /// <summary>
    /// Checks for the attribute of type <c>System.Runtime.CompilerServices.<paramref name="typeName"/></c> on <paramref name="member"/>.
    /// </summary>
    /// <remarks>Alias of <see cref="IsCompilerAttributeDefinedSafe"/>. In some older versions of .NET Framework, attributes not available in the API at that version will be added to the assembly on compile. This checks for those.</remarks>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="typeName">Type name of the attribute in <c>System.Runtime.CompilerServices</c> to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool HasCompilerAttributeSafe(ICustomAttributeProvider member, string typeName, bool inherit = false);

    /// <summary>
    /// Checks for and returns the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="inherit">Also check parent members.</param>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <typeparam name="TAttribute">Type of the attribute to check for.</typeparam>
    /// <exception cref="AmbiguousMatchException">There are more than one attributes of type <typeparamref name="TAttribute"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    TAttribute? GetAttributeSafe<TAttribute>(ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute;

    /// <summary>
    /// Checks for and returns the attribute of type <paramref name="attributeType"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="attributeType">Type of the attribute to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <exception cref="ArgumentException"><paramref name="attributeType"/> did not derive from <see cref="Attribute"/>.</exception>
    /// <exception cref="AmbiguousMatchException">There are more than one attributes of type <paramref name="attributeType"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Attribute? GetAttributeSafe(ICustomAttributeProvider member, Type attributeType, bool inherit = false);

    /// <summary>
    /// Checks for and returns the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="inherit">Also check parent members.</param>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <typeparam name="TAttribute">Type of the attribute to check for.</typeparam>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    TAttribute[] GetAttributesSafe<TAttribute>(ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute;

    /// <summary>
    /// Checks for and returns the attribute of type <paramref name="attributeType"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="attributeType">Type of the attribute to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <exception cref="ArgumentException"><paramref name="attributeType"/> did not derive from <see cref="Attribute"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Attribute[] GetAttributesSafe(ICustomAttributeProvider member, Type attributeType, bool inherit = false);

    /// <summary>
    /// Checks for and outputs the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="attribute">Found attribute, or <see langword="null"/> if it's not found (the function will return <see langword="false"/>).</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <typeparam name="TAttribute">Type of the attribute to check for.</typeparam>
    /// <returns><see langword="true"/> if the attribute was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool TryGetAttributeSafe<TAttribute>(ICustomAttributeProvider member, out TAttribute attribute, bool inherit = false) where TAttribute : Attribute;

    /// <summary>
    /// Checks for the <see cref="T:System.Runtime.CompilerServices.IsReadOnlyAttribute"/> on <paramref name="member"/>, which signifies the readonly value.
    /// <remarks>This behavior is overridden on fields to check <see cref="FieldInfo.IsInitOnly"/>.</remarks>
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsReadOnly(ICustomAttributeProvider member);

    /// <summary>
    /// Checks for the <see cref="T:System.Runtime.CompilerServices.IsByRefLikeAttribute"/> on <paramref name="type"/>, or <see cref="P:System.Type.IsByRefLike"/> on newer platforms.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsByRefLikeType(Type type);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="type"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsIgnored(Type type);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="member"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsIgnored(MemberInfo member);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="assembly"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsIgnored(Assembly assembly);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="parameter"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsIgnored(ParameterInfo parameter);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="module"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool IsIgnored(Module module);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="type"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int GetPriority(Type type);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="member"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int GetPriority(MemberInfo member);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="assembly"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int GetPriority(Assembly assembly);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="parameter"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int GetPriority(ParameterInfo parameter);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="module"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int GetPriority(Module module);

    /// <summary>
    /// Created for <see cref="List{T}.Sort(Comparison{T})"/> to order by priority (highest to lowest).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int SortTypesByPriorityHandler(Type a, Type b);

    /// <summary>
    /// Created for <see cref="List{T}.Sort(Comparison{T})"/> to order by priority (highest to lowest).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int SortMembersByPriorityHandler(MemberInfo a, MemberInfo b);

    /// <summary>
    /// Safely gets the reflection method info of the passed method. Works best with static methods.<br/><br/>
    /// <code>
    /// MethodInfo? method = Accessor.GetMethod(Guid.Parse);
    /// </code>
    /// </summary>
    /// <returns>A method info of a passed delegate</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MethodInfo? GetMethod(Delegate @delegate);

    /// <param name="returnType">Return type of the method.</param>
    /// <param name="parameters">Method parameters, not including the instance.</param>
    /// <param name="instanceType">The declaring type, or <see langword="null"/> for static methods.</param>
    /// <remarks>The first argument will be the instance.</remarks>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature, or <see langword="null"/> if there are too many parameters.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Type? GetDefaultDelegate(Type returnType,
#if NET45_OR_GREATER
        IReadOnlyList<ParameterInfo> parameters,
#else
        IList<ParameterInfo> parameters,
#endif
        Type? instanceType);

    /// <summary>
    /// Used to perform a repeated <paramref name="action"/> for each base type of a <paramref name="type"/>.
    /// </summary>
    /// <param name="type">Highest (most derived) type in the hierarchy.</param>
    /// <param name="action">Called optionally for <paramref name="type"/>, then for each base type in order from most related to least related.</param>
    /// <param name="includeParent">Call <paramref name="action"/> on <paramref name="type"/>. Overrides <paramref name="excludeSystemBase"/>.</param>
    /// <param name="excludeSystemBase">Excludes calling <paramref name="action"/> for <see cref="object"/> or <see cref="ValueType"/>.</param>
    void ForEachBaseType(Type type, ForEachBaseType action, bool includeParent = true, bool excludeSystemBase = true);

    /// <summary>
    /// Used to perform a repeated <paramref name="action"/> for each base type of a <paramref name="type"/>.
    /// </summary>
    /// <remarks>Execution can be broken by returning <see langword="false"/>.</remarks>
    /// <param name="type">Highest (most derived) type in the hierarchy.</param>
    /// <param name="action">Called optionally for <paramref name="type"/>, then for each base type in order from most related to least related.</param>
    /// <param name="includeParent">Call <paramref name="action"/> on <paramref name="type"/>. Overrides <paramref name="excludeSystemBase"/>.</param>
    /// <param name="excludeSystemBase">Excludes calling <paramref name="action"/> for <see cref="object"/> or <see cref="ValueType"/>.</param>
    void ForEachBaseType(Type type, ForEachBaseTypeWhile action, bool includeParent = true, bool excludeSystemBase = true);

    /// <returns>Every type defined in the calling assembly.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    List<Type> GetTypesSafe(bool removeIgnored = false);

    /// <returns>Every type defined in <paramref name="assembly"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    List<Type> GetTypesSafe(Assembly assembly, bool removeIgnored = false);

    /// <returns>Every type defined in the provided <paramref name="assmeblies"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    List<Type> GetTypesSafe(IEnumerable<Assembly> assmeblies, bool removeIgnored = false);

    /// <summary>
    /// Takes a method declared in an interface and returns an implementation on <paramref name="type"/>. Useful for getting explicit implementations.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="interfaceMethod"/> is not defined in an interface or <paramref name="type"/> does not implement the interface it's defined in.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MethodInfo? GetImplementedMethod(Type type, MethodInfo interfaceMethod);

    /// <summary>
    /// Gets the (cached) <paramref name="returnType"/> and <paramref name="parameters"/> of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    void GetDelegateSignature<TDelegate>(out Type returnType, out ParameterInfo[] parameters) where TDelegate : Delegate;

    /// <summary>
    /// Gets the (cached) <paramref name="returnParameter"/> and <paramref name="parameters"/> of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    void GetDelegateSignature<TDelegate>(out ParameterInfo? returnParameter, out ParameterInfo[] parameters) where TDelegate : Delegate;

    /// <summary>
    /// Gets the (cached) return type of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Type GetReturnType<TDelegate>() where TDelegate : Delegate;

    /// <summary>
    /// Gets the (cached) return parameter info of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    ParameterInfo? GetReturnParameter<TDelegate>() where TDelegate : Delegate;

    /// <summary>
    /// Gets the (cached) parameters of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    ParameterInfo[] GetParameters<TDelegate>() where TDelegate : Delegate;

    /// <summary>
    /// Gets the (cached) <see langword="Invoke"/> method of a <typeparamref name="TDelegate"/> delegate type. All delegates have one by default.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MethodInfo GetInvokeMethod<TDelegate>() where TDelegate : Delegate;

    /// <summary>
    /// Gets the <paramref name="returnType"/> and <paramref name="parameters"/> of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    void GetDelegateSignature(Type delegateType, out Type returnType, out ParameterInfo[] parameters);

    /// <summary>
    /// Gets the <paramref name="returnParameter"/> and <paramref name="parameters"/> of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    void GetDelegateSignature(Type delegateType, out ParameterInfo? returnParameter, out ParameterInfo[] parameters);

    /// <summary>
    /// Gets the return type of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a <see langword="delegate"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Type GetReturnType(Type delegateType);

    /// <summary>
    /// Gets the return parameter info of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a <see langword="delegate"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    ParameterInfo? GetReturnParameter(Type delegateType);

    /// <summary>
    /// Gets the parameters of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a <see langword="delegate"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    ParameterInfo[] GetParameters(Type delegateType);

    /// <summary>
    /// Gets the <see langword="Invoke"/> method of a <paramref name="delegateType"/>. All delegates have one by default.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a <see langword="delegate"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    MethodInfo GetInvokeMethod(Type delegateType);

    /// <summary>
    /// Get the 'type' of a member, returns <see cref="FieldInfo.FieldType"/> or <see cref="PropertyInfo.PropertyType"/> or
    /// <see cref="MethodInfo.ReturnType"/> or <see cref="EventInfo.EventHandlerType"/> or <see cref="MemberInfo.DeclaringType"/> for constructors.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    Type? GetMemberType(MemberInfo member);


    /// <summary>
    /// Check any member for being static.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool GetIsStatic(MemberInfo member);

    /// <summary>
    /// Decide if a method should be callvirt'd instead of call'd. Usually you will use <see cref="ShouldCallvirtRuntime"/> instead as it doesn't account for possible future keyword changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool ShouldCallvirt(MethodBase method);

    /// <summary>
    /// Decide if a method should be callvirt'd instead of call'd at runtime. Doesn't account for future changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool ShouldCallvirtRuntime(MethodBase method);

    /// <summary>
    /// Return the correct call <see cref="OpCode"/> to use depending on the method. Usually you will use <see cref="GetCallRuntime"/> instead as it doesn't account for possible future keyword changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    OpCode GetCall(MethodBase method);

    /// <summary>
    /// Return the correct call <see cref="OpCode"/> to use depending on the method at runtime. Doesn't account for future changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    OpCode GetCallRuntime(MethodBase method);

    /// <summary>
    /// Get the underlying array from a list.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    TElementType[] GetUnderlyingArray<TElementType>(List<TElementType> list);

    /// <summary>
    /// Get the underlying array from a list, or in the case of a reflection failure calls <see cref="List{TElementType}.ToArray"/> on <paramref name="list"/> and returns that.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    TElementType[] GetUnderlyingArrayOrCopy<TElementType>(List<TElementType> list);

    /// <summary>
    /// Get the version of a list, which is incremented each time the list is updated.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    int GetListVersion<TElementType>(List<TElementType> list);

    /// <summary>
    /// Get the underlying array from a list.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    bool TryGetUnderlyingArray<TElementType>(List<TElementType> list, out TElementType[] underlyingArray);

    /// <summary>
    /// Get the version of a list, which is incremented each time the list is updated.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    bool TryGetListVersion<TElementType>(List<TElementType> list, out int version);

    /// <summary>
    /// Checks if it's possible for a variable of type <paramref name="actualType"/> to have a value of type <paramref name="queriedType"/>. 
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="actualType"/> is assignable from <paramref name="queriedType"/> or if <paramref name="queriedType"/> is assignable from <paramref name="actualType"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool CouldBeAssignedTo(Type actualType, Type queriedType);

    /// <summary>
    /// Checks if it's possible for a variable of type <paramref name="actualType"/> to have a value of type <typeparamref name="T"/>. 
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="actualType"/> is assignable from <typeparamref name="T"/> or if <typeparamref name="T"/> is assignable from <paramref name="actualType"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool CouldBeAssignedTo<T>(Type actualType);

    /// <summary>
    /// Nethod to get a <see cref="IOpCodeEmitter"/> from an existing <see cref="ILGenerator"/>.
    /// </summary>
    /// <param name="generator"><see cref="ILGenerator"/> to wrap.</param>
    /// <param name="debuggable">Shows debug logging as the method generates.</param>
    /// <param name="addBreakpoints">Shows debug logging as the method executes.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IOpCodeEmitter AsEmitter(ILGenerator generator, bool debuggable = false, bool addBreakpoints = false);

    /// <summary>
    /// Method to get a <see cref="IOpCodeEmitter"/> from an existing <see cref="DynamicMethod"/>.
    /// </summary>
    /// <param name="dynMethod">Dynamic method.</param>
    /// <param name="debuggable">Shows debug logging as the method generates.</param>
    /// <param name="addBreakpoints">Shows debug logging as the method executes.</param>
    /// <param name="streamSize">The size of the MSIL stream, in bytes.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IOpCodeEmitter AsEmitter(DynamicMethod dynMethod, bool debuggable = false, bool addBreakpoints = false, int streamSize = 64);

    /// <summary>
    /// Method to get a <see cref="IOpCodeEmitter"/> from an existing <see cref="MethodBuilder"/>.
    /// </summary>
    /// <param name="methodBuilder">Dynamic method builder.</param>
    /// <param name="debuggable">Shows debug logging as the method generates.</param>
    /// <param name="addBreakpoints">Shows debug logging as the method executes.</param>
    /// <param name="streamSize">The size of the MSIL stream, in bytes.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IOpCodeEmitter AsEmitter(MethodBuilder methodBuilder, bool debuggable = false, bool addBreakpoints = false, int streamSize = 64);

    /// <summary>
    /// Creates an abstracted <see cref="IVariable"/> for <paramref name="field"/>.
    /// </summary>
    /// <param name="field">The underlying field.</param>
    /// <returns>An abstracted variable with <paramref name="field"/> as it's underlying field.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IVariable AsVariable(FieldInfo field);

    /// <summary>
    /// Creates an abstracted <see cref="IVariable"/> for <paramref name="property"/>.
    /// </summary>
    /// <param name="property">The underlying property.</param>
    /// <returns>An abstracted variable with <paramref name="property"/> as it's underlying property.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IVariable AsVariable(PropertyInfo property);

    /// <summary>
    /// Creates an abstracted <see cref="IVariable"/> for <paramref name="member"/>, a field or property.
    /// </summary>
    /// <param name="member">The underlying field or property. Must be of type <see cref="FieldInfo"/> or <see cref="PropertyInfo"/>.</param>
    /// <returns>An abstracted variable with <paramref name="member"/> as it's underlying field or property.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="member"/> is not a field or property.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IVariable AsVariable(MemberInfo member);

    /// <summary>
    /// Creates a static, strongly-typed, abstracted <see cref="IVariable"/> for <paramref name="field"/>.
    /// </summary>
    /// <param name="field">The underlying field.</param>
    /// <typeparam name="TMemberType">The field type of <paramref name="field"/>.</typeparam>
    /// <returns>An abstracted variable with <paramref name="field"/> as it's underlying field.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IStaticVariable<TMemberType> AsStaticVariable<TMemberType>(FieldInfo field);

    /// <summary>
    /// Creates a static, strongly-typed, abstracted <see cref="IVariable"/> for <paramref name="property"/>.
    /// </summary>
    /// <param name="property">The underlying property.</param>
    /// <typeparam name="TMemberType">The property type of <paramref name="property"/>.</typeparam>
    /// <returns>An abstracted variable with <paramref name="property"/> as it's underlying property.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IStaticVariable<TMemberType> AsStaticVariable<TMemberType>(PropertyInfo property);

    /// <summary>
    /// Creates an instance, strongly-typed, abstracted <see cref="IVariable"/> for <paramref name="field"/>.
    /// </summary>
    /// <param name="field">The underlying field.</param>
    /// <typeparam name="TMemberType">The field type of <paramref name="field"/>.</typeparam>
    /// <typeparam name="TDeclaringType">The type that <paramref name="field"/> is declared in.</typeparam>
    /// <returns>An abstracted variable with <paramref name="field"/> as it's underlying field.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IInstanceVariable<TDeclaringType, TMemberType> AsInstanceVariable<TDeclaringType, TMemberType>(FieldInfo field);

    /// <summary>
    /// Creates an instance, strongly-typed, abstracted <see cref="IVariable"/> for <paramref name="property"/>.
    /// </summary>
    /// <param name="property">The underlying property.</param>
    /// <typeparam name="TMemberType">The property type of <paramref name="property"/>.</typeparam>
    /// <typeparam name="TDeclaringType">The type that <paramref name="property"/> is declared in.</typeparam>
    /// <returns>An abstracted variable with <paramref name="property"/> as it's underlying property.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IInstanceVariable<TDeclaringType, TMemberType> AsInstanceVariable<TDeclaringType, TMemberType>(PropertyInfo property);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="variable">The found variable, or <see langword="null"/>.</param>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool TryFind<TDeclaringType>(string name, out IVariable? variable, bool ignoreCase = false);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="declaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</param>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="variable">The found variable, or <see langword="null"/>.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool TryFind(Type declaringType, string name, out IVariable? variable, bool ignoreCase = false);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="variable">The found variable, or <see langword="null"/>.</param>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool TryFindStatic<TDeclaringType, TMemberType>(string name, out IStaticVariable<TMemberType>? variable, bool ignoreCase = false);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="variable">The found variable, or <see langword="null"/>.</param>
    /// <param name="declaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</param>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    bool TryFindStatic<TMemberType>(Type declaringType, string name, out IStaticVariable<TMemberType>? variable, bool ignoreCase = false);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IVariable? Find<TDeclaringType>(string name, bool ignoreCase = false);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IStaticVariable<TMemberType>? FindStatic<TDeclaringType, TMemberType>(string name, bool ignoreCase = false);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="declaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</param>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IVariable? Find(Type declaringType, string name, bool ignoreCase = false);

    /// <summary>
    /// Find a static variable by declaring type and name.
    /// </summary>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <param name="declaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</param>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IStaticVariable<TMemberType>? FindStatic<TMemberType>(Type declaringType, string name, bool ignoreCase = false);

    /// <summary>
    /// Find an instance variable by declaring type and name.
    /// </summary>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    IInstanceVariable<TDeclaringType, TMemberType>? FindInstance<TDeclaringType, TMemberType>(string name, bool ignoreCase = false);
}