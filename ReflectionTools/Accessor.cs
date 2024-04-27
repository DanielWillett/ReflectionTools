using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif


namespace DanielWillett.ReflectionTools;

/// <summary>
/// Reflection utilities for accessing private or internal members.
/// </summary>
public static class Accessor
{
    private static IAccessor _accessor = new DefaultAccessor();

    /// <summary>
    /// Implementation of <see cref="IAccessor"/> currently in use by all static/extension methods.
    /// </summary>
    /// <remarks>By assigning a value to this property, you transfer ownership of the object to this class, meaning it shouldn't be used or disposed outside this class at all.</remarks>
    public static IAccessor Active
    {
        get => _accessor;
        set
        {
            value ??= new DefaultAccessor();
            IAccessor old = Interlocked.Exchange(ref _accessor, value);
            if (!ReferenceEquals(old, value) && old is IDisposable disp)
                disp.Dispose();

            if (value.LogDebugMessages)
                value.Logger?.LogDebug("Accessor.Formatter", $"Accessor implementation updated: {value.Formatter.Format(value.GetType())}.");
        }
    }

    internal static void RemoveActiveIfThis(IAccessor accessor)
    {
        if (_accessor != accessor)
            return;

        Interlocked.CompareExchange(ref _accessor, new DefaultAccessor(), accessor);
    }

    /// <summary>
    /// Should <see cref="Logger"/> log generated IL code (as debug messages)?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    public static bool LogILTraceMessages
    {
        get => _accessor.LogILTraceMessages;
        set => _accessor.LogILTraceMessages = value;
    }

    /// <summary>
    /// Should <see cref="Logger"/> log debug messages?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    public static bool LogDebugMessages
    {
        get => _accessor.LogDebugMessages;
        set => _accessor.LogDebugMessages = value;
    }

    /// <summary>
    /// Should <see cref="Logger"/> log info messages?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    public static bool LogInfoMessages
    {
        get => _accessor.LogInfoMessages;
        set => _accessor.LogInfoMessages = value;
    }

    /// <summary>
    /// Should <see cref="Logger"/> log warning messages?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    public static bool LogWarningMessages
    {
        get => _accessor.LogWarningMessages;
        set => _accessor.LogWarningMessages = value;
    }

    /// <summary>
    /// Should <see cref="Logger"/> log error messages?
    /// </summary>
    /// <remarks>Returns <see langword="false"/> if <see cref="Logger"/> is <see langword="null"/>.</remarks>
    public static bool LogErrorMessages
    {
        get => _accessor.LogErrorMessages;
        set => _accessor.LogErrorMessages = value;
    }

    /// <summary>
    /// Logging IO for all methods in this library.
    /// <para>Assigning a value to this will dispose the previous value if needed.</para>
    /// </summary>
    /// <remarks>Default value is an instance of <see cref="ConsoleReflectionToolsLogger"/>, which outputs to <see cref="Console"/>.
    /// By assigning a value to this property, you transfer ownership of the object to this class, meaning it shouldn't be used or disposed outside this class at all.</remarks>
    public static IReflectionToolsLogger? Logger
    {
        get => _accessor.Logger;
        set => _accessor.Logger = value;
    }

    /// <summary>
    /// Logging IO for all methods in this library for standard output.
    /// <para>Assigning a value to this will dispose the previous value if needed.</para>
    /// </summary>
    /// <remarks>Default value is an instance of <see cref="ConsoleReflectionToolsLogger"/>, which outputs to <see cref="Console"/>.
    /// By assigning a value to this property, you transfer ownership of the object to this class, meaning it shouldn't be used or disposed outside this class at all.</remarks>
    public static IOpCodeFormatter Formatter
    {
        get => _accessor.Formatter;
        set => _accessor.Formatter = value;
    }

    /// <summary>
    /// Logging IO for all methods in this library for exceptions.
    /// <para>Assigning a value to this will dispose the previous value if needed.</para>
    /// </summary>
    /// <remarks>Default value is an instance of <see cref="ConsoleReflectionToolsLogger"/>, which outputs to <see cref="Console"/>.
    /// By assigning a value to this property, you transfer ownership of the object to this class, meaning it shouldn't be used or disposed outside this class at all.</remarks>
    public static IOpCodeFormatter ExceptionFormatter
    {
        get => _accessor.ExceptionFormatter;
        set => _accessor.ExceptionFormatter = value;
    }

    /// <summary>
    /// System primary assembly.
    /// </summary>
    /// <remarks>Lazily cached.</remarks>
    /// <exception cref="TypeLoadException"/>
    public static Assembly MSCoreLib => _accessor.MSCoreLib;

    /// <summary>
    /// Whether or not the <c>Mono.Runtime</c> class is available. Indicates if the current runtime is Mono.
    /// </summary>
    public static bool IsMono => _accessor.IsMono;

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
    public static InstanceSetter<TInstance, TValue>? GenerateInstanceSetter<TInstance, TValue>(string fieldName, bool throwOnError = false) where TInstance : class
        => _accessor.GenerateInstanceSetter<TInstance, TValue>(fieldName, throwOnError);

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
    public static InstanceSetter<TInstance, TValue>? GenerateInstanceSetter<TInstance, TValue>(FieldInfo field, bool throwOnError = false) where TInstance : class
        => _accessor.GenerateInstanceSetter<TInstance, TValue>(field, throwOnError);

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
    public static Delegate? GenerateInstanceSetter(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateInstanceSetter(field, throwOnError);

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
    public static InstanceGetter<TInstance, TValue>? GenerateInstanceGetter<TInstance, TValue>(string fieldName, bool throwOnError = false)
        => _accessor.GenerateInstanceGetter<TInstance, TValue>(fieldName, throwOnError);

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
    public static InstanceGetter<TInstance, TValue>? GenerateInstanceGetter<TInstance, TValue>(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateInstanceGetter<TInstance, TValue>(field, throwOnError);

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
    public static Delegate? GenerateInstanceGetter(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateInstanceGetter(field, throwOnError);

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
    public static InstanceSetter<object, TValue>? GenerateInstanceSetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
        => _accessor.GenerateInstanceSetter<TValue>(declaringType, fieldName, throwOnError);

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
    public static InstanceSetter<object, TValue>? GenerateInstanceSetter<TValue>(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateInstanceSetter<TValue>(field, throwOnError);

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
    public static InstanceGetter<object, TValue>? GenerateInstanceGetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
        => _accessor.GenerateInstanceGetter<TValue>(declaringType, fieldName, throwOnError);

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
    public static InstanceGetter<object, TValue>? GenerateInstanceGetter<TValue>(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateInstanceGetter<TValue>(field, throwOnError);

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
    public static InstanceSetter<TInstance, TValue>? GenerateInstancePropertySetter<TInstance, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TInstance : class
        => _accessor.GenerateInstancePropertySetter<TInstance, TValue>(propertyName, throwOnError, allowUnsafeTypeBinding);

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
    public static InstanceGetter<TInstance, TValue>? GenerateInstancePropertyGetter<TInstance, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstancePropertyGetter<TInstance, TValue>(propertyName, throwOnError, allowUnsafeTypeBinding);

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
    public static InstanceSetter<TInstance, TValue>? GenerateInstancePropertySetter<TInstance, TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TInstance : class
        => _accessor.GenerateInstancePropertySetter<TInstance, TValue>(property, throwOnError, allowUnsafeTypeBinding);

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
    public static InstanceGetter<TInstance, TValue>? GenerateInstancePropertyGetter<TInstance, TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstancePropertyGetter<TInstance, TValue>(property, throwOnError, allowUnsafeTypeBinding);

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
    public static Delegate? GenerateInstancePropertySetter(PropertyInfo property, bool throwOnError = false)
        => _accessor.GenerateInstancePropertySetter(property, throwOnError);

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
    public static Delegate? GenerateInstancePropertyGetter(PropertyInfo property, bool throwOnError = false)
        => _accessor.GenerateInstancePropertyGetter(property, throwOnError);

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
    public static InstanceSetter<object, TValue>? GenerateInstancePropertySetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstancePropertySetter<TValue>(declaringType, propertyName, throwOnError, allowUnsafeTypeBinding);

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
    public static InstanceGetter<object, TValue>? GenerateInstancePropertyGetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstancePropertyGetter<TValue>(declaringType, propertyName, throwOnError, allowUnsafeTypeBinding);

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
    public static InstanceSetter<object, TValue>? GenerateInstancePropertySetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstancePropertySetter<TValue>(property, throwOnError, allowUnsafeTypeBinding);

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
    public static InstanceGetter<object, TValue>? GenerateInstancePropertyGetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstancePropertyGetter<TValue>(property, throwOnError, allowUnsafeTypeBinding);

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
    public static StaticSetter<TValue>? GenerateStaticSetter<TDeclaringType, TValue>(string fieldName, bool throwOnError = false)
        => _accessor.GenerateStaticSetter<TDeclaringType, TValue>(fieldName, throwOnError);

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
    public static StaticGetter<TValue>? GenerateStaticGetter<TDeclaringType, TValue>(string fieldName, bool throwOnError = false)
        => _accessor.GenerateStaticGetter<TDeclaringType, TValue>(fieldName, throwOnError);

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
    public static StaticSetter<TValue>? GenerateStaticSetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
        => _accessor.GenerateStaticSetter<TValue>(declaringType, fieldName, throwOnError);

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
    public static StaticSetter<TValue>? GenerateStaticSetter<TValue>(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateStaticSetter<TValue>(field, throwOnError);

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
    public static StaticGetter<TValue>? GenerateStaticGetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
        => _accessor.GenerateStaticGetter<TValue>(declaringType, fieldName, throwOnError);

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
    public static Delegate? GenerateStaticSetter(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateStaticSetter(field, throwOnError);

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
    public static StaticGetter<TValue>? GenerateStaticGetter<TValue>(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateStaticGetter<TValue>(field, throwOnError);

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
    public static Delegate? GenerateStaticGetter(FieldInfo field, bool throwOnError = false)
        => _accessor.GenerateStaticGetter(field, throwOnError);

    /// <summary>
    /// Generates a delegate or dynamic method that sets a static property value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static StaticSetter<TValue>? GenerateStaticPropertySetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false)
        => _accessor.GenerateStaticPropertySetter<TDeclaringType, TValue>(propertyName, throwOnError);

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
    // ReSharper disable once MethodOverloadWithOptionalParameter
    public static StaticSetter<TValue>? GenerateStaticPropertySetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateStaticPropertySetter<TDeclaringType, TValue>(propertyName, throwOnError, allowUnsafeTypeBinding);

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
    public static StaticGetter<TValue>? GenerateStaticPropertyGetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = true)
        => _accessor.GenerateStaticPropertyGetter<TDeclaringType, TValue>(propertyName, throwOnError, allowUnsafeTypeBinding);

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
    public static StaticSetter<TValue>? GenerateStaticPropertySetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateStaticPropertySetter<TValue>(declaringType, propertyName, throwOnError, allowUnsafeTypeBinding);

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
    public static StaticGetter<TValue>? GenerateStaticPropertyGetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateStaticPropertyGetter<TValue>(declaringType, propertyName, throwOnError, allowUnsafeTypeBinding);

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
    public static StaticSetter<TValue>? GenerateStaticPropertySetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateStaticPropertySetter<TValue>(property, throwOnError, allowUnsafeTypeBinding);

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
    public static StaticGetter<TValue>? GenerateStaticPropertyGetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateStaticPropertyGetter<TValue>(property, throwOnError, allowUnsafeTypeBinding);

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
    public static Delegate? GenerateStaticPropertySetter(PropertyInfo property, bool throwOnError = false)
        => _accessor.GenerateStaticPropertySetter(property, throwOnError);

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
    public static Delegate? GenerateStaticPropertyGetter(PropertyInfo property, bool throwOnError = false)
        => _accessor.GenerateStaticPropertyGetter(property, throwOnError);

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
    public static Delegate? GenerateInstanceCaller<TInstance>(string methodName, Type[]? parameters = null, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstanceCaller<TInstance>(methodName, parameters, throwOnError, allowUnsafeTypeBinding);

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
    public static TDelegate? GenerateInstanceCaller<TInstance, TDelegate>(string methodName, bool throwOnError = false, bool allowUnsafeTypeBinding = false, Type[]? parameters = null) where TDelegate : Delegate
        => _accessor.GenerateInstanceCaller<TInstance, TDelegate>(methodName, throwOnError, allowUnsafeTypeBinding, parameters);

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
    public static Delegate? GenerateInstanceCaller(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstanceCaller(method, throwOnError, allowUnsafeTypeBinding);

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
    public static TDelegate? GenerateInstanceCaller<TDelegate>(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TDelegate : Delegate
        => _accessor.GenerateInstanceCaller<TDelegate>(method, throwOnError, allowUnsafeTypeBinding);

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
    public static Delegate? GenerateInstanceCaller(Type delegateType, MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateInstanceCaller(delegateType, method, throwOnError, allowUnsafeTypeBinding);

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
    public static Delegate? GenerateStaticCaller<TDeclaringType>(string methodName, Type[]? parameters = null, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateStaticCaller<TDeclaringType>(methodName, parameters, throwOnError, allowUnsafeTypeBinding);

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
    public static TDelegate? GenerateStaticCaller<TDeclaringType, TDelegate>(string methodName, bool throwOnError = false, bool allowUnsafeTypeBinding = false, Type[]? parameters = null) where TDelegate : Delegate
        => _accessor.GenerateStaticCaller<TDeclaringType, TDelegate>(methodName, throwOnError, allowUnsafeTypeBinding, parameters);

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
    public static Delegate? GenerateStaticCaller(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateStaticCaller(method, throwOnError, allowUnsafeTypeBinding);

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
    public static TDelegate? GenerateStaticCaller<TDelegate>(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TDelegate : Delegate
        => _accessor.GenerateStaticCaller<TDelegate>(method, throwOnError, allowUnsafeTypeBinding);

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
    public static Delegate? GenerateStaticCaller(Type delegateType, MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => _accessor.GenerateStaticCaller(delegateType, method, throwOnError, allowUnsafeTypeBinding);

    /// <summary>
    /// Gets platform-specific flags for creating dynamic methods.
    /// </summary>
    /// <param name="static">Whether or not the method has no 'instance', only considered when on mono.</param>
    /// <param name="attributes">Method attributes to pass to <see cref="DynamicMethod"/> constructor.</param>
    /// <param name="convention">Method convention to pass to <see cref="DynamicMethod"/> constructor.</param>
    public static void GetDynamicMethodFlags(bool @static, out MethodAttributes attributes, out CallingConventions convention)
        => _accessor.GetDynamicMethodFlags(@static, out attributes, out convention);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of a <paramref name="type"/>.
    /// </summary>
    /// <remarks>Takes nested types into account, returning the lowest visibility in the nested hierarchy
    /// (ex. if an internal class is nested in a private nested type, this method will consider it private).</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MemberVisibility GetVisibility(this Type type)
        => _accessor.GetVisibility(type);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of a <paramref name="method"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MemberVisibility GetVisibility(this MethodBase method)
        => _accessor.GetVisibility(method);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of a <paramref name="field"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MemberVisibility GetVisibility(this FieldInfo field)
        => _accessor.GetVisibility(field);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of a <paramref name="property"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MemberVisibility GetVisibility(this PropertyInfo property)
        => _accessor.GetVisibility(property);

    /// <summary>
    /// Gets a simplified enum representing the visiblity (accessibility) of an <paramref name="event"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MemberVisibility GetVisibility(this EventInfo @event)
        => _accessor.GetVisibility(@event);

    /// <summary>
    /// Get the highest visibilty needed for both of the given methods to be visible. Methods which are <see langword="null"/> are ignored.
    /// </summary>
    /// <remarks>Useful for getting property visiblity manually, will always be at least <see cref="MemberVisibility.Private"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MemberVisibility GetHighestVisibility(MethodBase? method1, MethodBase? method2)
        => _accessor.GetHighestVisibility(method1, method2);

    /// <summary>
    /// Get the highest visibilty needed for all three of the given methods to be visible. Methods which are <see langword="null"/> are ignored.
    /// </summary>
    /// <remarks>Useful for getting event visiblity manually, will always be at least <see cref="MemberVisibility.Private"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MemberVisibility GetHighestVisibility(MethodBase? method1, MethodBase? method2, MethodBase? method3)
        => _accessor.GetHighestVisibility(method1, method2, method3);

    /// <summary>
    /// Get the highest visibilty needed for all of the given methods to be visible. Methods which are <see langword="null"/> are ignored.
    /// </summary>
    /// <remarks>Will always be at least <see cref="MemberVisibility.Private"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MemberVisibility GetHighestVisibility(params MethodBase?[] methods)
        => _accessor.GetHighestVisibility(methods);

    /// <summary>
    /// Checks if <paramref name="assembly"/> has a <see cref="InternalsVisibleToAttribute"/> with the given <paramref name="assemblyName"/>.
    /// The value of the attribute must match <paramref name="assemblyName"/> exactly.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool AssemblyGivesInternalAccess(Assembly assembly, string assemblyName)
        => _accessor.AssemblyGivesInternalAccess(assembly, assemblyName);

    /// <summary>
    /// Checks <paramref name="method"/> for the <see langword="extern"/> flag.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsExtern(this MethodBase method)
        => _accessor.IsExtern(method);

    /// <summary>
    /// Checks <paramref name="field"/> for the <see langword="extern"/> flag.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsExtern(this FieldInfo field)
        => _accessor.IsExtern(field);

    /// <summary>
    /// Checks <paramref name="property"/>'s getter and setter for the <see langword="extern"/> flag.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsExtern(this PropertyInfo property, bool checkGetterFirst = true)
        => _accessor.IsExtern(property, checkGetterFirst);

    /// <summary>
    /// Checks for the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <remarks>Alias of <see cref="HasAttributeSafe{TAttribute}"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsDefinedSafe<TAttribute>(this ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute
        => _accessor.IsDefinedSafe<TAttribute>(member, inherit);

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
    public static bool IsDefinedSafe(this ICustomAttributeProvider member, Type attributeType, bool inherit = false)
        => _accessor.IsDefinedSafe(member, attributeType, inherit);

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
    public static bool IsCompilerAttributeDefinedSafe(this ICustomAttributeProvider member, string typeName, bool inherit = false)
        => _accessor.IsCompilerAttributeDefinedSafe(member, typeName, inherit);

    /// <summary>
    /// Checks for the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <remarks>Alias of <see cref="IsDefinedSafe{TAttribute}"/>.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool HasAttributeSafe<TAttribute>(this ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute
        => _accessor.HasAttributeSafe<TAttribute>(member, inherit);

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
    public static bool HasAttributeSafe(this ICustomAttributeProvider member, Type attributeType, bool inherit = false)
        => _accessor.HasAttributeSafe(member, attributeType, inherit);

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
    public static bool HasCompilerAttributeSafe(this ICustomAttributeProvider member, string typeName, bool inherit = false)
        => _accessor.HasCompilerAttributeSafe(member, typeName, inherit);

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
    public static TAttribute? GetAttributeSafe<TAttribute>(this ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute
        => _accessor.GetAttributeSafe<TAttribute>(member, inherit);

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
    public static Attribute? GetAttributeSafe(this ICustomAttributeProvider member, Type attributeType, bool inherit = false)
        => _accessor.GetAttributeSafe(member, attributeType, inherit);

    /// <summary>
    /// Checks for and returns the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="inherit">Also check parent members.</param>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <typeparam name="TAttribute">Type of the attribute to check for.</typeparam>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static TAttribute[] GetAttributesSafe<TAttribute>(this ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute
        => _accessor.GetAttributesSafe<TAttribute>(member, inherit);

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
    public static Attribute[] GetAttributesSafe(this ICustomAttributeProvider member, Type attributeType, bool inherit = false)
        => _accessor.GetAttributesSafe(member, attributeType, inherit);

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
    public static bool TryGetAttributeSafe<TAttribute>(this ICustomAttributeProvider member, out TAttribute attribute, bool inherit = false) where TAttribute : Attribute
        => _accessor.TryGetAttributeSafe(member, out attribute, inherit);

    /// <summary>
    /// Checks for the <see cref="T:System.Runtime.CompilerServices.IsReadOnlyAttribute"/> on <paramref name="member"/>, which signifies the readonly value.
    /// <remarks>This behavior is overridden on fields to check <see cref="FieldInfo.IsInitOnly"/>.</remarks>
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsReadOnly(this ICustomAttributeProvider member)
        => _accessor.IsReadOnly(member);

    /// <summary>
    /// Checks for the <see cref="T:System.Runtime.CompilerServices.IsByRefLikeAttribute"/> on <paramref name="type"/>, or <see cref="P:System.Type.IsByRefLike"/> on newer platforms.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsByRefLikeType(this Type type)
        => _accessor.IsByRefLikeType(type);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="type"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsIgnored(this Type type)
        => _accessor.IsIgnored(type);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="member"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsIgnored(this MemberInfo member)
        => _accessor.IsIgnored(member);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="assembly"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsIgnored(this Assembly assembly)
        => _accessor.IsIgnored(assembly);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="parameter"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsIgnored(this ParameterInfo parameter)
        => _accessor.IsIgnored(parameter);

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="module"/>.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsIgnored(this Module module)
        => _accessor.IsIgnored(module);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="type"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int GetPriority(this Type type)
        => _accessor.GetPriority(type);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="member"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int GetPriority(this MemberInfo member)
        => _accessor.GetPriority(member);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="assembly"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int GetPriority(this Assembly assembly)
        => _accessor.GetPriority(assembly);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="parameter"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int GetPriority(this ParameterInfo parameter)
        => _accessor.GetPriority(parameter);

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="module"/> and returns the priority (or zero if not found).
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int GetPriority(this Module module)
        => _accessor.GetPriority(module);

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
    public static MethodInfo? GetMethod(Delegate @delegate)
        => _accessor.GetMethod(@delegate);

    /// <param name="returnType">Return type of the method.</param>
    /// <param name="parameters">Method parameters, not including the instance.</param>
    /// <param name="instanceType">The declaring type, or <see langword="null"/> for static methods.</param>
    /// <remarks>The first argument will be the instance.</remarks>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature, or <see langword="null"/> if there are too many parameters.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static Type? GetDefaultDelegate(Type returnType,
#if NET45_OR_GREATER
        IReadOnlyList<ParameterInfo> parameters,
#else
        IList<ParameterInfo> parameters,
#endif
        Type? instanceType)
        => _accessor.GetDefaultDelegate(returnType, parameters, instanceType);

    /// <summary>
    /// Used to perform a repeated <paramref name="action"/> for each base type of a <paramref name="type"/>.
    /// </summary>
    /// <param name="type">Highest (most derived) type in the hierarchy.</param>
    /// <param name="action">Called optionally for <paramref name="type"/>, then for each base type in order from most related to least related.</param>
    /// <param name="includeParent">Call <paramref name="action"/> on <paramref name="type"/>. Overrides <paramref name="excludeSystemBase"/>.</param>
    /// <param name="excludeSystemBase">Excludes calling <paramref name="action"/> for <see cref="object"/> or <see cref="ValueType"/>.</param>
    public static void ForEachBaseType(this Type type, ForEachBaseType action, bool includeParent = true, bool excludeSystemBase = true)
        => _accessor.ForEachBaseType(type, action, includeParent, excludeSystemBase);

    /// <summary>
    /// Used to perform a repeated <paramref name="action"/> for each base type of a <paramref name="type"/>.
    /// </summary>
    /// <remarks>Execution can be broken by returning <see langword="false"/>.</remarks>
    /// <param name="type">Highest (most derived) type in the hierarchy.</param>
    /// <param name="action">Called optionally for <paramref name="type"/>, then for each base type in order from most related to least related.</param>
    /// <param name="includeParent">Call <paramref name="action"/> on <paramref name="type"/>. Overrides <paramref name="excludeSystemBase"/>.</param>
    /// <param name="excludeSystemBase">Excludes calling <paramref name="action"/> for <see cref="object"/> or <see cref="ValueType"/>.</param>
    public static void ForEachBaseType(this Type type, ForEachBaseTypeWhile action, bool includeParent = true, bool excludeSystemBase = true)
        => _accessor.ForEachBaseType(type, action, includeParent, excludeSystemBase);

    /// <returns>Every type defined in the calling assembly.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static List<Type> GetTypesSafe(bool removeIgnored = false)
        => _accessor.GetTypesSafe(Assembly.GetCallingAssembly(), removeIgnored);

    /// <returns>Every type defined in <paramref name="assembly"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static List<Type> GetTypesSafe(Assembly assembly, bool removeIgnored = false)
        => _accessor.GetTypesSafe(assembly, removeIgnored);

    /// <returns>Every type defined in the provided <paramref name="assmeblies"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static List<Type> GetTypesSafe(IEnumerable<Assembly> assmeblies, bool removeIgnored = false)
        => _accessor.GetTypesSafe(assmeblies, removeIgnored);

    /// <summary>
    /// Takes a method declared in an interface and returns an implementation on <paramref name="type"/>. Useful for getting explicit implementations.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="interfaceMethod"/> is not defined in an interface or <paramref name="type"/> does not implement the interface it's defined in.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MethodInfo? GetImplementedMethod(Type type, MethodInfo interfaceMethod)
        => _accessor.GetImplementedMethod(type, interfaceMethod);

    /// <summary>
    /// Gets the (cached) <paramref name="returnType"/> and <paramref name="parameters"/> of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    public static void GetDelegateSignature<TDelegate>(out Type returnType, out ParameterInfo[] parameters) where TDelegate : Delegate
        => _accessor.GetDelegateSignature<TDelegate>(out returnType, out parameters);

    /// <summary>
    /// Gets the (cached) <paramref name="returnParameter"/> and <paramref name="parameters"/> of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    public static void GetDelegateSignature<TDelegate>(out ParameterInfo? returnParameter, out ParameterInfo[] parameters) where TDelegate : Delegate
        => _accessor.GetDelegateSignature<TDelegate>(out returnParameter, out parameters);

    /// <summary>
    /// Gets the (cached) return type of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static Type GetReturnType<TDelegate>() where TDelegate : Delegate
        => _accessor.GetReturnType<TDelegate>();

    /// <summary>
    /// Gets the (cached) return parameter info of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static ParameterInfo? GetReturnParameter<TDelegate>() where TDelegate : Delegate
        => _accessor.GetReturnParameter<TDelegate>();

    /// <summary>
    /// Gets the (cached) parameters of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static ParameterInfo[] GetParameters<TDelegate>() where TDelegate : Delegate
        => _accessor.GetParameters<TDelegate>();

    /// <summary>
    /// Gets the (cached) <see langword="Invoke"/> method of a <typeparamref name="TDelegate"/> delegate type. All delegates have one by default.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MethodInfo GetInvokeMethod<TDelegate>() where TDelegate : Delegate
        => _accessor.GetInvokeMethod<TDelegate>();

    /// <summary>
    /// Gets the <paramref name="returnType"/> and <paramref name="parameters"/> of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    public static void GetDelegateSignature(Type delegateType, out Type returnType, out ParameterInfo[] parameters)
        => _accessor.GetDelegateSignature(delegateType, out returnType, out parameters);

    /// <summary>
    /// Gets the <paramref name="returnParameter"/> and <paramref name="parameters"/> of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    public static void GetDelegateSignature(Type delegateType, out ParameterInfo? returnParameter, out ParameterInfo[] parameters)
        => _accessor.GetDelegateSignature(delegateType, out returnParameter, out parameters);

    /// <summary>
    /// Gets the return type of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a <see langword="delegate"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static Type GetReturnType(Type delegateType)
        => _accessor.GetReturnType(delegateType);

    /// <summary>
    /// Gets the return parameter info of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a <see langword="delegate"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static ParameterInfo? GetReturnParameter(Type delegateType)
        => _accessor.GetReturnParameter(delegateType);

    /// <summary>
    /// Gets the parameters of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a <see langword="delegate"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static ParameterInfo[] GetParameters(Type delegateType)
        => _accessor.GetParameters(delegateType);

    /// <summary>
    /// Gets the <see langword="Invoke"/> method of a <paramref name="delegateType"/>. All delegates have one by default.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentException"><paramref name="delegateType"/> is not a <see langword="delegate"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static MethodInfo GetInvokeMethod(Type delegateType)
        => _accessor.GetInvokeMethod(delegateType);

    /// <summary>
    /// Get the 'type' of a member, returns <see cref="FieldInfo.FieldType"/> or <see cref="PropertyInfo.PropertyType"/> or
    /// <see cref="MethodInfo.ReturnType"/> or <see cref="EventInfo.EventHandlerType"/> or <see cref="MemberInfo.DeclaringType"/> for constructors.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static Type? GetMemberType(this MemberInfo member)
        => _accessor.GetMemberType(member);

    /// <summary>
    /// Check any member for being static.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool GetIsStatic(this MemberInfo member)
        => _accessor.GetIsStatic(member);

    /// <summary>
    /// Decide if a method should be callvirt'd instead of call'd. Usually you will use <see cref="ShouldCallvirtRuntime"/> instead as it doesn't account for possible future keyword changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool ShouldCallvirt(this MethodBase method)
        => _accessor.ShouldCallvirt(method);

    /// <summary>
    /// Decide if a method should be callvirt'd instead of call'd at runtime. Doesn't account for future changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool ShouldCallvirtRuntime(this MethodBase method)
        => _accessor.ShouldCallvirtRuntime(method);

    /// <summary>
    /// Return the correct call <see cref="OpCode"/> to use depending on the method. Usually you will use <see cref="GetCallRuntime"/> instead as it doesn't account for possible future keyword changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static OpCode GetCall(this MethodBase method)
        => _accessor.GetCall(method);

    /// <summary>
    /// Return the correct call <see cref="OpCode"/> to use depending on the method at runtime. Doesn't account for future changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static OpCode GetCallRuntime(this MethodBase method)
        => _accessor.GetCallRuntime(method);

    /// <summary>
    /// Get the underlying array from a list.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static TElementType[] GetUnderlyingArray<TElementType>(this List<TElementType> list)
        => _accessor.GetUnderlyingArray(list);

    /// <summary>
    /// Get the underlying array from a list, or in the case of a reflection failure calls <see cref="List{TElementType}.ToArray"/> on <paramref name="list"/> and returns that.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static TElementType[] GetUnderlyingArrayOrCopy<TElementType>(this List<TElementType> list)
        => _accessor.GetUnderlyingArrayOrCopy(list);

    /// <summary>
    /// Get the version of a list, which is incremented each time the list is updated.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int GetListVersion<TElementType>(this List<TElementType> list)
        => _accessor.GetListVersion(list);

    /// <summary>
    /// Get the underlying array from a list.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public static bool TryGetUnderlyingArray<TElementType>(List<TElementType> list, out TElementType[] underlyingArray)
        => _accessor.TryGetUnderlyingArray(list, out underlyingArray);

    /// <summary>
    /// Get the version of a list, which is incremented each time the list is updated.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public static bool TryGetListVersion<TElementType>(List<TElementType> list, out int version)
        => _accessor.TryGetListVersion(list, out version);

    /// <summary>
    /// Checks if it's possible for a variable of type <paramref name="actualType"/> to have a value of type <paramref name="queriedType"/>. 
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="actualType"/> is assignable from <paramref name="queriedType"/> or if <paramref name="queriedType"/> is assignable from <paramref name="actualType"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool CouldBeAssignedTo(this Type actualType, Type queriedType)
        => _accessor.CouldBeAssignedTo(actualType, queriedType);

    /// <summary>
    /// Checks if it's possible for a variable of type <paramref name="actualType"/> to have a value of type <typeparamref name="T"/>. 
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="actualType"/> is assignable from <typeparamref name="T"/> or if <typeparamref name="T"/> is assignable from <paramref name="actualType"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool CouldBeAssignedTo<T>(this Type actualType)
        => _accessor.CouldBeAssignedTo<T>(actualType);

    /// <summary>
    /// Sort types by their priority, used for sort methods.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int SortTypesByPriorityHandler(Type a, Type b)
    {
        return GetPriority(b).CompareTo(GetPriority(a));
    }

    /// <summary>
    /// Sort members by their priority, used for sort methods.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int SortMembersByPriorityHandler(MemberInfo a, MemberInfo b)
    {
        return GetPriority(b).CompareTo(GetPriority(a));
    }
}

/// <summary>
/// Represents a setter for an instance field or property.
/// </summary>
/// <typeparam name="TInstance">The declaring type of the member.</typeparam>
/// <typeparam name="T">The return type of the member</typeparam>
public delegate void InstanceSetter<in TInstance, in T>(TInstance owner, T value);

/// <summary>
/// Represents a getter for an instance field or property.
/// </summary>
/// <typeparam name="TInstance">The declaring type of the member.</typeparam>
/// <typeparam name="T">The return type of the member</typeparam>
public delegate T InstanceGetter<in TInstance, out T>(TInstance owner);

/// <summary>
/// Represents a setter for a static field or property.
/// </summary>
/// <typeparam name="T">The return type of the member</typeparam>
public delegate void StaticSetter<in T>(T value);

/// <summary>
/// Represents a getter for a static field or property.
/// </summary>
/// <typeparam name="T">The return type of the member</typeparam>
public delegate T StaticGetter<out T>();

/// <summary>
/// Used with <see cref="Accessor.ForEachBaseType(Type, ForEachBaseType, bool, bool)"/>
/// </summary>
/// <param name="type">The current type in the hierarchy.</param>
/// <param name="depth">Number of types below the provided type this base type is. Will be zero if the type returned is the provided type, 1 for its base type, and so on.</param>
public delegate void ForEachBaseType(Type type, int depth);

/// <summary>
/// Used with <see cref="Accessor.ForEachBaseType(Type, ForEachBaseTypeWhile, bool, bool)"/>
/// </summary>
/// <param name="type">The current type in the hierarchy.</param>
/// <param name="depth">Number of types below the provided type this base type is. Will be zero if the type returned is the provided type, 1 for its base type, and so on.</param>
/// <returns><see langword="True"/> to continue, <see langword="false"/> to break.</returns>
public delegate bool ForEachBaseTypeWhile(Type type, int depth);