using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
#if !NET6_0_OR_GREATER
using System.Runtime.Serialization;
#endif
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif

namespace DanielWillett.ReflectionTools;
/// <summary>
/// Default implementation of <see cref="IAccessor"/>.
/// </summary>
public class DefaultAccessor : IAccessor, IDisposable
{
    private IReflectionToolsLogger? _logger;
    private IOpCodeFormatter _formatter;
    private IOpCodeFormatter _exceptionFormatter;

    private static bool _isMonoCached;
    private static bool _isMono;
    private static Assembly? _mscorlibAssembly;

    internal static ConstructorInfo? CastExCtor;
    internal static ConstructorInfo? NreExCtor;

    internal static Type[]? FuncTypes;
    internal static Type[]? ActionTypes;
    private static Type? _ignoreAttribute;
    private static Type? _priorityAttribute;
    private static bool _castExCtorCalc;
    private static bool _nreExCtorCalc;
    private static bool _logILTraceMessages;
    private static bool _logDebugMessages;
    private static bool _logInfoMessages;
    private static bool _logWarningMessages;
    private static bool _logErrorMessages;

    void IDisposable.Dispose()
    {
        Accessor.RemoveActiveIfThis(this);
    }

    /// <summary>
    /// Create an instance of <see cref="DefaultAccessor"/> with a <see cref="ConsoleReflectionToolsLogger"/> logger.
    /// </summary>
    public DefaultAccessor()
    {
        _logger = new ConsoleReflectionToolsLogger();
        _formatter = new DefaultOpCodeFormatter(this);
        _exceptionFormatter = new DefaultOpCodeFormatter(this);
    }

    /// <summary>
    /// Create an instance of <see cref="DefaultAccessor"/> with the specified logger.
    /// </summary>
#if NET461_OR_GREATER || !NETFRAMEWORK
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
#endif
    public DefaultAccessor(IReflectionToolsLogger logger)
    {
        _logger = logger;
        _formatter = new DefaultOpCodeFormatter(this);
        _exceptionFormatter = new DefaultOpCodeFormatter(this);
    }

    /// <summary>
    /// Create an instance of <see cref="DefaultAccessor"/> with the specified logger, formatters, and configuration method.
    /// </summary>
    public DefaultAccessor(IReflectionToolsLogger logger, IOpCodeFormatter? formatter, IOpCodeFormatter? exceptionFormatter, Action<IAccessor>? configureAccessor, bool isStaticDefault)
    {
        _logger = logger;
        if (formatter is DefaultOpCodeFormatter f)
            f.Accessor = this;
        if (exceptionFormatter is DefaultOpCodeFormatter ef)
            ef.Accessor = this;
        _formatter = formatter ?? new DefaultOpCodeFormatter(this);
        _exceptionFormatter = exceptionFormatter ?? new DefaultOpCodeFormatter(this);

        configureAccessor?.Invoke(this);
        if (isStaticDefault)
            Accessor.Active = this;
    }

    /// <inheritdoc />
    public virtual bool LogILTraceMessages
    {
        get => _logger != null && _logILTraceMessages;
        set => _logILTraceMessages = value;
    }

    /// <inheritdoc />
    public virtual bool LogDebugMessages
    {
        get => _logger != null && _logDebugMessages;
        set => _logDebugMessages = value;
    }

    /// <inheritdoc />
    public virtual bool LogInfoMessages
    {
        get => _logger != null && _logInfoMessages;
        set => _logInfoMessages = value;
    }

    /// <inheritdoc />
    public virtual bool LogWarningMessages
    {
        get => _logger != null && _logWarningMessages;
        set => _logWarningMessages = value;
    }

    /// <inheritdoc />
    public virtual bool LogErrorMessages
    {
        get => _logger != null && _logErrorMessages;
        set => _logErrorMessages = value;
    }

    /// <inheritdoc />
    public virtual IReflectionToolsLogger? Logger
    {
        get => _logger;
        set
        {
            IReflectionToolsLogger? old = Interlocked.Exchange(ref _logger, value);
            if (!ReferenceEquals(old, value) && old is IDisposable disp)
                disp.Dispose();

            if (LogInfoMessages)
                value?.LogInfo("Accessor.Logger", $"Logger updated: {Formatter.Format(value.GetType())}.");
        }
    }

    /// <inheritdoc />
    public virtual IOpCodeFormatter Formatter
    {
        get => _formatter;
        set
        {
            if (value is DefaultOpCodeFormatter f)
                f.Accessor = this;
            else
                value ??= new DefaultOpCodeFormatter(this);
            IOpCodeFormatter old = Interlocked.Exchange(ref _formatter, value);
            if (!ReferenceEquals(old, value) && old is IDisposable disp)
                disp.Dispose();

            if (LogDebugMessages)
                Logger?.LogDebug("Accessor.Formatter", $"Formatter updated: {Formatter.Format(value.GetType())}.");
        }
    }

    /// <inheritdoc />
    public virtual IOpCodeFormatter ExceptionFormatter
    {
        get => _exceptionFormatter;
        set
        {
            if (value is DefaultOpCodeFormatter f)
                f.Accessor = this;
            else
                value ??= new DefaultOpCodeFormatter(this);
            IOpCodeFormatter old = Interlocked.Exchange(ref _exceptionFormatter, value);
            if (!ReferenceEquals(old, value) && old is IDisposable disp)
                disp.Dispose();

            if (LogDebugMessages)
                Logger?.LogDebug("Accessor.ExceptionFormatter", $"Exception formatter updated: {Formatter.Format(value.GetType())}.");
        }
    }

    /// <inheritdoc />
    public Assembly MSCoreLib => _mscorlibAssembly ??= typeof(object).Assembly;

    /// <inheritdoc />
    public virtual bool IsMono
    {
        get
        {
            if (_isMonoCached)
                return _isMono;

            _isMono = Type.GetType("Mono.Runtime", false, false) != null;
            _isMonoCached = true;
            return _isMono;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceSetter<TInstance, TValue>? GenerateInstanceSetter<TInstance, TValue>(string fieldName, bool throwOnError = false) where TInstance : class
    {
        const string source = "Accessor.GenerateInstanceSetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (typeof(TInstance).IsValueType)
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance setter for {ExceptionFormatter.Format(typeof(TInstance))}.{fieldName}, you must pass structs ({ExceptionFormatter.Format(typeof(TInstance))}) as a boxed object.");

            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Unable to create instance setter for {Formatter.Format(typeof(TInstance))}.{fieldName}, you must pass structs ({Formatter.Format(typeof(TInstance))}) as a boxed object.");
            return null;
        }

        FieldInfo? field = typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field ??= typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (field is not null && !field.IsStatic && (field.FieldType.IsAssignableFrom(typeof(TValue)) || typeof(TValue) == typeof(object)))
        {
            return GenerateInstanceSetter<TInstance, TValue>(field, throwOnError);
        }

        if (throwOnError)
            throw new Exception($"Unable to find matching field: {ExceptionFormatter.Format(typeof(TInstance))}.{fieldName}.");
        if (LogErrorMessages)
            reflectionToolsLogger?.LogError(source, null, $"Unable to find matching field {Formatter.Format(typeof(TInstance))}.{fieldName}.");

        return null;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceSetter<TInstance, TValue>? GenerateInstanceSetter<TInstance, TValue>(FieldInfo field, bool throwOnError = false) where TInstance : class
    {
        const string source = "Accessor.GenerateInstanceSetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        string fieldName = field.Name;

        if (field.DeclaringType == null || field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)}. It must be a non-static field with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)}. It must be a non-static field with a declaring type.");
            return null;
        }

        if (!CouldBeAssignedTo(field.DeclaringType, typeof(TInstance)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)} with invalid instance type {ExceptionFormatter.Format(typeof(TInstance))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)} with invalid instance type {Formatter.Format(typeof(TInstance))}.");
            return null;
        }

        if (!CouldBeAssignedTo(field.FieldType, typeof(TValue)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)} with invalid value type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)} with invalid value type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, convention, typeof(void), new Type[] { typeof(TInstance), typeof(TValue) }, typeof(TInstance), true);
            method.DefineParameter(1, ParameterAttributes.None, "this");
            method.DefineParameter(2, ParameterAttributes.None, "value");

            IOpCodeEmitter il =
                AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating instance setter for {Formatter.Format(field)}:");

            EmitInstanceSetterIntl(typeof(TValue), field, il);

            InstanceSetter<TInstance, TValue> setter = (InstanceSetter<TInstance, TValue>)method.CreateDelegate(typeof(InstanceSetter<TInstance, TValue>));

            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method instance setter for {Formatter.Format(field)}.");
            return setter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating instance getter for {Formatter.Format(field)}.");
            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateInstanceSetter(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceSetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        string fieldName = field.Name;

        if (field.DeclaringType == null || field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)}. It must be a non-static field with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)}. It must be a non-static field with a declaring type.");
            return null;
        }

        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            Type instanceType = field.DeclaringType.IsValueType ? typeof(object) : field.DeclaringType;
            DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, convention, typeof(void), new Type[] { instanceType, field.FieldType }, field.DeclaringType, true);
            method.DefineParameter(1, ParameterAttributes.None, "this");
            method.DefineParameter(2, ParameterAttributes.None, "value");

            IOpCodeEmitter il =
                AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating instance setter for {Formatter.Format(field)}:");

            EmitInstanceSetterIntl(field.FieldType, field, il);

            Type delegateType = typeof(InstanceSetter<,>).MakeGenericType(instanceType, field.FieldType);
            Delegate setter = method.CreateDelegate(delegateType);

            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method instance setter for {Formatter.Format(field)}.");
            return setter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating instance getter for {Formatter.Format(field)}.");
            return null;
        }
    }

    private void EmitInstanceSetterIntl(Type valueType, FieldInfo field, IOpCodeEmitter il)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        Label? typeLbl = null;

        if (!valueType.IsValueType && field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, field.FieldType);
        }
        else if (valueType.IsValueType && !field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Box, valueType);
        }
        else if (!field.FieldType.IsAssignableFrom(valueType) && (CastExCtor != null || NreExCtor != null))
        {
            typeLbl = il.DefineLabel();
            il.Emit(OpCodes.Isinst, field.FieldType);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue_S, typeLbl.Value);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brfalse_S, typeLbl.Value);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Pop);
            if (CastExCtor != null)
                il.Emit(OpCodes.Ldstr, "Invalid argument type passed to setter for " + field.Name + ". Expected " + ExceptionFormatter.Format(field.FieldType) + ".");
            il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
            il.Emit(OpCodes.Throw);
        }

        if (typeLbl.HasValue)
        {
            il.MarkLabel(typeLbl.Value);
        }

        il.Emit(OpCodes.Stfld, field);
        il.Emit(OpCodes.Ret);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceGetter<TInstance, TValue>? GenerateInstanceGetter<TInstance, TValue>(string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceGetter";

        FieldInfo? field = typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field ??= typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

        if (field is not null && !field.IsStatic && (typeof(TValue).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(object)))
        {
            return GenerateInstanceGetter<TInstance, TValue>(field, throwOnError);
        }

        if (throwOnError)
            throw new Exception($"Unable to find matching field: {ExceptionFormatter.Format(typeof(TInstance))}.{fieldName}.");

        if (LogErrorMessages)
            Logger?.LogError(source, null, $"Unable to find matching property {Formatter.Format(typeof(TInstance))}.{fieldName}.");

        return null;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceGetter<TInstance, TValue>? GenerateInstanceGetter<TInstance, TValue>(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceGetter";
        if (field == null)
        {
            if (throwOnError)
                throw new Exception("Field not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Field not found.");
            return null;
        }

        if (field.DeclaringType == null || field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)}. It must be a non-static field with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)}. It must be a non-static field with a declaring type.");
            return null;
        }

        if (!CouldBeAssignedTo(field.DeclaringType, typeof(TInstance)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)} with invalid instance type {ExceptionFormatter.Format(typeof(TInstance))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)} with invalid instance type {Formatter.Format(typeof(TInstance))}.");
            return null;
        }

        if (!CouldBeAssignedTo(field.FieldType, typeof(TValue)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)} with invalid return type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)} with invalid return type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        string fieldName = field.Name;
        IReflectionToolsLogger? reflectionToolsLogger = Logger;

        try
        {
            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, convention, typeof(TValue), new Type[] { typeof(TInstance) }, typeof(TInstance), true);
            method.DefineParameter(1, ParameterAttributes.None, "this");

            IOpCodeEmitter il =
                AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating instance getter for {Formatter.Format(field)}:");

            EmitInstanceGetterIntl(typeof(TValue), field, il);

            InstanceGetter<TInstance, TValue> getter = (InstanceGetter<TInstance, TValue>)method.CreateDelegate(typeof(InstanceGetter<TInstance, TValue>));

            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method instance getter for {Formatter.Format(field)}.");
            return getter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating instance getter for {Formatter.Format(field)}.");
            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateInstanceGetter(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceGetter";
        if (field.DeclaringType == null || field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)}. It must be a non-static field with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)}. It must be a non-static field with a declaring type.");
            return null;
        }

        string fieldName = field.Name;
        IReflectionToolsLogger? reflectionToolsLogger = Logger;

        try
        {
            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, convention, field.FieldType, new Type[] { field.DeclaringType }, field.DeclaringType, true);
            method.DefineParameter(1, ParameterAttributes.None, "this");

            IOpCodeEmitter il =
                AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating instance getter for {Formatter.Format(field)}:");

            EmitInstanceGetterIntl(field.FieldType, field, il);

            Type delegateType = typeof(InstanceGetter<,>).MakeGenericType(field.DeclaringType, field.FieldType);
            Delegate getter = method.CreateDelegate(delegateType);

            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method instance getter for {Formatter.Format(field)}.");
            return getter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating instance getter for {Formatter.Format(field)}.");
            return null;
        }
    }

    private static void EmitInstanceGetterIntl(Type valueType, FieldInfo field, IOpCodeEmitter il)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);

        if (valueType.IsValueType && !field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, valueType);
        }
        else if (!valueType.IsValueType && field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Box, field.FieldType);
        }

        il.Emit(OpCodes.Ret);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceSetter<object, TValue>? GenerateInstanceSetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceSetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance setter for <unknown>.{fieldName}. Declaring type not found.");
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Error generating instance setter for <unknown>.{fieldName}. Declaring type not found.");
            return null;
        }

        FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field ??= declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (field is not null && !field.IsStatic && (field.FieldType.IsAssignableFrom(typeof(TValue)) || typeof(TValue) == typeof(object)))
        {
            return GenerateInstanceSetter<TValue>(field, throwOnError);
        }

        if (throwOnError)
            throw new Exception($"Unable to find matching field: {ExceptionFormatter.Format(declaringType)}.{fieldName}.");
        if (LogErrorMessages)
            reflectionToolsLogger?.LogError(source, null, $"Unable to find matching property {Formatter.Format(declaringType)}.{fieldName}.");

        return null;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceSetter<object, TValue>? GenerateInstanceSetter<TValue>(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceSetter";
        if (field == null)
        {
            if (throwOnError)
                throw new Exception("Field not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Field not found.");
            return null;
        }

        if (field.DeclaringType == null || field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)}. It must be a non-static field with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)}. It must be a non-static field with a declaring type.");
            return null;
        }


        if (!CouldBeAssignedTo(field.FieldType, typeof(TValue)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)} with invalid value type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)} with invalid value type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        string fieldName = field.Name;
        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, convention, typeof(void), new Type[] { typeof(object), typeof(TValue) }, field.DeclaringType, true);
            method.DefineParameter(1, ParameterAttributes.None, "this");
            method.DefineParameter(2, ParameterAttributes.None, "value");
            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating instance setter for {Formatter.Format(field)}:");

            IOpCodeEmitter il =
                 AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            Label lbl = il.DefineLabel();
            Label? typeLbl = null;
            il.Emit(OpCodes.Ldarg_0);

            bool isValueType = field.DeclaringType.IsValueType;
            if (CastExCtor != null || !isValueType && NreExCtor != null)
            {
                Label lbl2 = il.DefineLabel();
                il.Emit(OpCodes.Isinst, field.DeclaringType);
                if (!isValueType)
                    il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue_S, lbl);
                if (!isValueType)
                    il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Brfalse_S, lbl2);

                if (CastExCtor != null)
                    il.Emit(OpCodes.Ldstr, $"Invalid instance type passed to getter for {fieldName}. Expected {ExceptionFormatter.Format(field.DeclaringType)}.");
                il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                il.Emit(OpCodes.Throw);
                il.MarkLabel(lbl2);
                ConstructorInfo ctor = NreExCtor ?? CastExCtor!;
                if (ctor == CastExCtor)
                    il.Emit(OpCodes.Ldstr, $"Null passed to getter for {fieldName}. Expected {ExceptionFormatter.Format(field.DeclaringType)}.");
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Throw);
            }
            il.MarkLabel(lbl);
            if (isValueType)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox, field.DeclaringType);
                il.Emit(OpCodes.Ldflda, field);
                il.Emit(OpCodes.Ldarg_1);

                if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, field.FieldType);
                }
                else if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, typeof(TValue));
                }
                else if (!field.FieldType.IsAssignableFrom(typeof(TValue)) && (CastExCtor != null || NreExCtor != null))
                {
                    typeLbl = il.DefineLabel();
                    il.Emit(OpCodes.Isinst, field.FieldType);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brtrue_S, typeLbl.Value);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brfalse_S, typeLbl.Value);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Pop);
                    if (CastExCtor != null)
                        il.Emit(OpCodes.Ldstr, "Invalid argument type passed to setter for " + fieldName + ". Expected " + ExceptionFormatter.Format(field.FieldType) + ".");
                    il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                    il.Emit(OpCodes.Throw);
                }

                if (typeLbl.HasValue)
                {
                    il.MarkLabel(typeLbl.Value);
                }

                il.Emit(OpCodes.Stobj, field.FieldType);
            }
            else
            {
                il.Emit(OpCodes.Ldarg_1);

                if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, field.FieldType);
                }
                else if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, typeof(TValue));
                }
                else if (!field.FieldType.IsAssignableFrom(typeof(TValue)) && (CastExCtor != null || NreExCtor != null))
                {
                    typeLbl = il.DefineLabel();
                    il.Emit(OpCodes.Isinst, field.FieldType);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brtrue_S, typeLbl.Value);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brfalse_S, typeLbl.Value);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Pop);
                    if (CastExCtor != null)
                        il.Emit(OpCodes.Ldstr, "Invalid argument type passed to setter for " + fieldName + ". Expected " + ExceptionFormatter.Format(field.FieldType) + ".");
                    il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                    il.Emit(OpCodes.Throw);
                }

                if (typeLbl.HasValue)
                {
                    il.MarkLabel(typeLbl.Value);
                }

                il.Emit(OpCodes.Stfld, field);
            }
            il.Emit(OpCodes.Ret);
            InstanceSetter<object, TValue> setter = (InstanceSetter<object, TValue>)method.CreateDelegate(typeof(InstanceSetter<object, TValue>));
            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method instance setter for {Formatter.Format(field)}.");
            return setter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance setter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating instance setter for {Formatter.Format(field)}.");
            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceGetter<object, TValue>? GenerateInstanceGetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceGetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for <unknown>.{fieldName}. Declaring type not found.");
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Error generating instance getter for <unknown>.{fieldName}. Declaring type not found.");
            return null;
        }
        FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field ??= declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (field is not null && !field.IsStatic && (typeof(TValue).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(object)))
        {
            return GenerateInstanceGetter<TValue>(field, throwOnError);
        }

        if (throwOnError)
            throw new Exception($"Unable to find matching field: {ExceptionFormatter.Format(declaringType)}.{fieldName}.");
        if (LogErrorMessages)
            reflectionToolsLogger?.LogError(source, null, $"Unable to find matching property {Formatter.Format(declaringType)}.{fieldName}.");

        return null;

    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceGetter<object, TValue>? GenerateInstanceGetter<TValue>(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceGetter";
        if (field == null)
        {
            if (throwOnError)
                throw new Exception("Field not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Field not found.");
            return null;
        }

        if (field.DeclaringType == null || field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)}. It must be a non-static field with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)}. It must be a non-static field with a declaring type.");
            return null;
        }


        if (!CouldBeAssignedTo(field.FieldType, typeof(TValue)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(field)} with invalid return type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(field)} with invalid return type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        string fieldName = field.Name;
        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, convention, typeof(TValue), new Type[] { typeof(object) }, field.DeclaringType, true);
            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating instance getter for {Formatter.Format(field)}:");
            method.DefineParameter(1, ParameterAttributes.None, "this");

            IOpCodeEmitter il =
                 AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            il.Emit(OpCodes.Ldarg_0);
            Label? lbl = null;

            bool isValueType = field.DeclaringType.IsValueType;
            if (CastExCtor != null || !isValueType && NreExCtor != null)
            {
                lbl = il.DefineLabel();
                Label lbl2 = il.DefineLabel();
                il.Emit(OpCodes.Isinst, field.DeclaringType);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue_S, lbl.Value);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Brfalse_S, lbl2);
                if (CastExCtor != null)
                    il.Emit(OpCodes.Ldstr, "Invalid instance type passed to getter for " + fieldName + ". Expected " + ExceptionFormatter.Format(field.DeclaringType) + ".");
                il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                il.Emit(OpCodes.Throw);
                il.MarkLabel(lbl2);
                ConstructorInfo ctor = NreExCtor ?? CastExCtor!;
                if (ctor == CastExCtor)
                    il.Emit(OpCodes.Ldstr, $"Null passed to getter for {fieldName}. Expected {ExceptionFormatter.Format(field.DeclaringType)}.");
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Throw);
            }
            if (lbl.HasValue)
            {
                il.MarkLabel(lbl.Value);
            }
            if (isValueType)
            {
                il.Emit(OpCodes.Unbox, field.DeclaringType);
            }
            il.Emit(OpCodes.Ldfld, field);

            if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, typeof(TValue));
            }
            else if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
            }

            il.Emit(OpCodes.Ret);
            InstanceGetter<object, TValue> getter = (InstanceGetter<object, TValue>)method.CreateDelegate(typeof(InstanceGetter<object, TValue>));
            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method instance getter for {Formatter.Format(field)}.");
            return getter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating instance getter for {Formatter.Format(field)}.");
            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceSetter<TInstance, TValue>? GenerateInstancePropertySetter<TInstance, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TInstance : class
    {
        if (typeof(TInstance).IsValueType)
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance setter for {ExceptionFormatter.Format(typeof(TInstance))}.{propertyName}, you must pass structs ({ExceptionFormatter.Format(typeof(TInstance))}) as a boxed object.");

            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateInstancePropertySetter", null, $"Unable to create instance setter for {Formatter.Format(typeof(TInstance))}.{propertyName}, you must pass structs ({Formatter.Format(typeof(TInstance))}) as a boxed object.");
            return null;
        }

        PropertyInfo? property = typeof(TInstance).GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        MethodInfo? setter = property?.GetSetMethod(true);
        if (setter is null || setter.IsStatic || setter.GetParameters() is not { Length: 1 } parameters || (!parameters[0].ParameterType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
        {
            property = typeof(TInstance).GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            setter = property?.GetSetMethod(true);

            if (setter is null || setter.IsStatic || setter.GetParameters() is not { Length: 1 } parameters2 || (!parameters2[0].ParameterType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
            {
                if (throwOnError)
                    throw new Exception($"Unable to find matching property: {ExceptionFormatter.Format(typeof(TInstance))}.{propertyName} with a setter.");
                if (LogErrorMessages)
                    Logger?.LogError("Accessor.GenerateInstancePropertySetter", null, $"Unable to find matching property {Formatter.Format(typeof(TInstance))}.{propertyName} with a setter.");
                return null;
            }
        }

        return GenerateInstanceCaller<InstanceSetter<TInstance, TValue>>(setter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceGetter<TInstance, TValue>? GenerateInstancePropertyGetter<TInstance, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        PropertyInfo? property = typeof(TInstance).GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        MethodInfo? getter = property?.GetGetMethod(true);
        if (getter is null || getter.IsStatic || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
        {
            property = typeof(TInstance).GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            getter = property?.GetGetMethod(true);

            if (getter is null || getter.IsStatic || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
            {
                if (throwOnError)
                    throw new Exception($"Unable to find matching property: {ExceptionFormatter.Format(typeof(TInstance))}.{propertyName} with a getter.");
                if (LogErrorMessages)
                    Logger?.LogError("Accessor.GenerateInstancePropertyGetter", null, $"Unable to find matching property {Formatter.Format(typeof(TInstance))}.{propertyName} with a getter.");
                return null;
            }
        }

        return GenerateInstanceCaller<InstanceGetter<TInstance, TValue>>(getter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceSetter<TInstance, TValue>? GenerateInstancePropertySetter<TInstance, TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TInstance : class
    {
        const string source = "Accessor.GenerateInstancePropertySetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");

            return null;
        }

        MethodInfo? setter = property.GetSetMethod(true);
        if (property.DeclaringType == null || setter != null && setter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance setter for {ExceptionFormatter.Format(property)}. It must be a non-static property with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance setter for {Formatter.Format(property)}. It must be a non-static property with a declaring type.");
            return null;
        }

        if (!CouldBeAssignedTo(typeof(TInstance), property.DeclaringType))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)} with invalid instance type {ExceptionFormatter.Format(typeof(TInstance))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)} with invalid instance type {Formatter.Format(typeof(TInstance))}.");
            return null;
        }

        if (!CouldBeAssignedTo(typeof(TValue), property.PropertyType))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance setter for {ExceptionFormatter.Format(property)} with invalid value type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance setter for {Formatter.Format(property)} with invalid value type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        if (setter is null || (!typeof(TValue).IsAssignableFrom(setter.ReturnType) && setter.ReturnType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance setter for {ExceptionFormatter.Format(property)}. It must have a defined 'set' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance setter for {Formatter.Format(property)}. It must have a defined 'set' method.");
            return null;
        }

        return GenerateInstanceCaller<InstanceSetter<TInstance, TValue>>(setter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceGetter<TInstance, TValue>? GenerateInstancePropertyGetter<TInstance, TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        const string source = "Accessor.GenerateInstancePropertyGetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");

            return null;
        }

        MethodInfo? getter = property.GetGetMethod(true);
        if (property.DeclaringType == null || getter != null && getter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)}. It must be a non-static property with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)}. It must be a non-static property with a declaring type.");
            return null;
        }

        if (!CouldBeAssignedTo(typeof(TInstance), property.DeclaringType))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)} with invalid instance type {ExceptionFormatter.Format(typeof(TInstance))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)} with invalid instance type {Formatter.Format(typeof(TInstance))}.");
            return null;
        }

        if (!CouldBeAssignedTo(property.PropertyType, typeof(TValue)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)} with invalid return type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)} with invalid return type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        if (getter is null || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)}. It must have a defined 'get' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)}. It must have a defined 'get' method.");
            return null;
        }

        return GenerateInstanceCaller<InstanceGetter<TInstance, TValue>>(getter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateInstancePropertySetter(PropertyInfo property, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstancePropertySetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");

            return null;
        }

        MethodInfo? setter = property.GetSetMethod(true);
        if (property.DeclaringType == null || setter != null && setter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance setter for {ExceptionFormatter.Format(property)}. It must be a non-static property with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance setter for {Formatter.Format(property)}. It must be a non-static property with a declaring type.");
            return null;
        }

        if (setter is null)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance setter for {ExceptionFormatter.Format(property)}. It must have a defined 'set' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance setter for {Formatter.Format(property)}. It must have a defined 'set' method.");
            return null;
        }

        Type delegateType = typeof(InstanceSetter<,>).MakeGenericType(property.DeclaringType.IsValueType ? typeof(object) : property.DeclaringType, property.PropertyType);
        return GenerateInstanceCaller(delegateType, setter, throwOnError, true);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateInstancePropertyGetter(PropertyInfo property, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstancePropertyGetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");

            return null;
        }

        MethodInfo? getter = property.GetGetMethod(true);
        if (property.DeclaringType == null || getter != null && getter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)}. It must be a non-static property with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)}. It must be a non-static property with a declaring type.");
            return null;
        }

        if (getter is null)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)}. It must have a defined 'get' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)}. It must have a defined 'get' method.");
            return null;
        }

        Type delegateType = typeof(InstanceGetter<,>).MakeGenericType(property.DeclaringType, property.PropertyType);
        return GenerateInstanceCaller(delegateType, getter, throwOnError, true);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceSetter<object, TValue>? GenerateInstancePropertySetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance setter for <unknown>.{propertyName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateInstancePropertySetter", null, $"Error generating instance setter for <unknown>.{propertyName}. Declaring type not found.");
            return null;
        }
        PropertyInfo? property = declaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        MethodInfo? setter = property?.GetSetMethod(true);
        if (setter is null || setter.IsStatic || setter.GetParameters() is not { Length: 1 } parameters || (!parameters[0].ParameterType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
        {
            property = declaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            setter = property?.GetSetMethod(true);

            if (setter is null || setter.IsStatic || setter.GetParameters() is not { Length: 1 } parameters2 || (!parameters2[0].ParameterType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
            {
                if (throwOnError)
                    throw new Exception($"Unable to find matching property: {ExceptionFormatter.Format(declaringType)}.{propertyName} with a setter.");
                if (LogErrorMessages)
                    Logger?.LogError("Accessor.GenerateInstancePropertySetter", null, $"Unable to find matching property {Formatter.Format(declaringType)}.{propertyName} with a setter.");
                return null;
            }
        }

        return GenerateInstanceCaller<InstanceSetter<object, TValue>>(setter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceGetter<object, TValue>? GenerateInstancePropertyGetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for <unknown>.{propertyName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateInstancePropertyGetter", null, $"Error generating instance getter for <unknown>.{propertyName}. Declaring type not found.");
            return null;
        }

        PropertyInfo? property = declaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        MethodInfo? getter = property?.GetGetMethod(true);
        if (getter is null || getter.IsStatic || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
        {
            property = declaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            getter = property?.GetGetMethod(true);

            if (getter is null || getter.IsStatic || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
            {
                if (throwOnError)
                    throw new Exception($"Unable to find matching property: {ExceptionFormatter.Format(declaringType)}.{propertyName} with a getter.");
                if (LogErrorMessages)
                    Logger?.LogError("Accessor.GenerateInstancePropertyGetter", null, $"Unable to find matching property {Formatter.Format(declaringType)}.{propertyName} with a getter.");
                return null;
            }
        }

        return GenerateInstanceCaller<InstanceGetter<object, TValue>>(getter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceSetter<object, TValue>? GenerateInstancePropertySetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        const string source = "Accessor.GenerateInstancePropertySetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");
            return null;
        }

        MethodInfo? setter = property.GetSetMethod(true);
        if (property.DeclaringType == null || setter != null && setter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance setter for {ExceptionFormatter.Format(property)}. It must be a non-static property with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance setter for {Formatter.Format(property)}. It must be a non-static property with a declaring type.");
            return null;
        }

        if (setter is null)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance setter for {ExceptionFormatter.Format(property)}. It must have a defined 'set' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance setter for {Formatter.Format(property)}. It must have a defined 'set' method.");
            return null;
        }

        return GenerateInstanceCaller<InstanceSetter<object, TValue>>(setter, throwOnError, true);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual InstanceGetter<object, TValue>? GenerateInstancePropertyGetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        const string source = "Accessor.GenerateInstancePropertyGetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");
            return null;
        }

        MethodInfo? getter = property.GetGetMethod(true);
        if (property.DeclaringType == null || getter != null && getter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)}. It must be a non-static property with a declaring type.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)}. It must be a non-static property with a declaring type.");
            return null;
        }

        if (!CouldBeAssignedTo(property.PropertyType, typeof(TValue)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)} with invalid return type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)} with invalid return type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        if (getter is null || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate an instance getter for {ExceptionFormatter.Format(property)}. It must have a defined 'get' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate an instance getter for {Formatter.Format(property)}. It must have a defined 'get' method.");
            return null;
        }

        return GenerateInstanceCaller<InstanceGetter<object, TValue>>(getter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticSetter<TValue>? GenerateStaticSetter<TDeclaringType, TValue>(string fieldName, bool throwOnError = false)
        => GenerateStaticSetter<TValue>(typeof(TDeclaringType), fieldName, throwOnError);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticGetter<TValue>? GenerateStaticGetter<TDeclaringType, TValue>(string fieldName, bool throwOnError = false)
        => GenerateStaticGetter<TValue>(typeof(TDeclaringType), fieldName, throwOnError);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticSetter<TValue>? GenerateStaticSetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticSetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating static setter for <unknown>.{fieldName}. Declaring type not found.");
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Error generating static setter for <unknown>.{fieldName}. Declaring type not found.");
            return null;
        }
        FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        if (field is not null && field.IsStatic && (field.FieldType.IsAssignableFrom(typeof(TValue)) || typeof(TValue) == typeof(object)))
        {
            return GenerateStaticSetter<TValue>(field, throwOnError);
        }

        if (throwOnError)
            throw new Exception($"Unable to find matching field: {ExceptionFormatter.Format(declaringType)}.{fieldName}.");
        if (LogErrorMessages)
            reflectionToolsLogger?.LogError(source, null, $"Unable to find matching field {Formatter.Format(declaringType)}.{fieldName}.");

        return null;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticSetter<TValue>? GenerateStaticSetter<TValue>(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticSetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (field == null)
        {
            if (throwOnError)
                throw new Exception("Field not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Field not found.");
            return null;
        }

        string fieldName = field.Name;

        if (!field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(field)}. It must be a static field.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(field)}. It must be a static field.");
            return null;
        }

        if (!CouldBeAssignedTo(typeof(TValue), field.FieldType))
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(field)} with invalid value type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(field)} with invalid value type {Formatter.Format(typeof(TValue))}.");
            return null;
        }


#if NETFRAMEWORK
        if (field.IsPublic)
#endif
            if (field.IsInitOnly && field.FieldType.IsValueType)
            {
                if (throwOnError)
                    throw new NotSupportedException($"Field {ExceptionFormatter.Format(field)} is a static readonly value type field, which is not settable in the current runtime.");
                if (LogErrorMessages)
                    reflectionToolsLogger?.LogError(source, null, $"Field {Formatter.Format(field)} is a static readonly value type field, which is not settable in the current runtime.");
                return null;
            }

        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, convention, typeof(void), new Type[] { typeof(TValue) }, field.DeclaringType ?? typeof(Accessor), true);
            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating static setter for {Formatter.Format(field)}:");
            method.DefineParameter(1, ParameterAttributes.None, "value");

            IOpCodeEmitter il =
                 AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            EmitStaticSetterIntl(typeof(TValue), field, il);

            StaticSetter<TValue> setter = (StaticSetter<TValue>)method.CreateDelegate(typeof(StaticSetter<TValue>));
            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method static setter for {Formatter.Format(field)}.");
            return setter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating static setter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating static setter for {Formatter.Format(field)}.");
            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticGetter<TValue>? GenerateStaticGetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticGetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating static getter for <unknown>.{fieldName}. Declaring type not found.");
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Error generating static getter for <unknown>.{fieldName}. Declaring type not found.");
            return null;
        }
        FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        if (field is not null && field.IsStatic && (typeof(TValue).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(object)))
        {
            return GenerateStaticGetter<TValue>(field, throwOnError);
        }

        if (throwOnError)
            throw new Exception($"Unable to find matching field: {ExceptionFormatter.Format(declaringType)}.{fieldName}.");
        if (LogErrorMessages)
            reflectionToolsLogger?.LogError(source, null, $"Unable to find matching property {Formatter.Format(declaringType)}.{fieldName}.");
        return null;

    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateStaticSetter(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticSetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (field == null)
        {
            if (throwOnError)
                throw new Exception("Field not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Field not found.");
            return null;
        }

        string fieldName = field.Name;

        if (!field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(field)}. It must be a static field.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(field)}. It must be a static field.");
            return null;
        }

#if NETFRAMEWORK
        if (field.IsPublic)
#endif
            if (field.IsInitOnly && field.FieldType.IsValueType)
            {
                if (throwOnError)
                    throw new NotSupportedException($"Field {ExceptionFormatter.Format(field)} is a static readonly value type field, which is not settable in the current runtime.");
                if (LogErrorMessages)
                    reflectionToolsLogger?.LogError(source, null, $"Field {Formatter.Format(field)} is a static readonly value type field, which is not settable in the current runtime.");
                return null;
            }

        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, convention, typeof(void), new Type[] { field.FieldType }, field.DeclaringType ?? typeof(Accessor), true);
            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating static setter for {Formatter.Format(field)}:");
            method.DefineParameter(1, ParameterAttributes.None, "value");

            IOpCodeEmitter il =
                 AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            EmitStaticSetterIntl(field.FieldType, field, il);

            Type delegateType = typeof(StaticSetter<>).MakeGenericType(field.FieldType);
            Delegate setter = method.CreateDelegate(delegateType);
            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method static setter for {Formatter.Format(field)}.");
            return setter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating static setter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating static setter for {Formatter.Format(field)}.");
            return null;
        }
    }

    private void EmitStaticSetterIntl(Type valueType, FieldInfo field, IOpCodeEmitter il)
    {
        il.Emit(OpCodes.Ldarg_0);

        Label? lbl = null;

        if (!valueType.IsValueType && field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, field.FieldType);
        }
        else if (valueType.IsValueType && !field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Box, valueType);
        }
        else if (!field.FieldType.IsAssignableFrom(valueType) && (CastExCtor != null || NreExCtor != null))
        {
            lbl = il.DefineLabel();
            il.Emit(OpCodes.Isinst, field.FieldType);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue_S, lbl.Value);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brfalse_S, lbl.Value);
            il.Emit(OpCodes.Pop);
            if (CastExCtor != null)
                il.Emit(OpCodes.Ldstr, "Invalid argument type passed to getter for " + field.Name + ". Expected " + ExceptionFormatter.Format(field.FieldType) + ".");
            il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
            il.Emit(OpCodes.Throw);
        }

        if (lbl.HasValue)
        {
            il.MarkLabel(lbl.Value);
        }

        il.Emit(OpCodes.Stsfld, field);
        il.Emit(OpCodes.Ret);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticGetter<TValue>? GenerateStaticGetter<TValue>(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticGetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (field == null)
        {
            if (throwOnError)
                throw new Exception("Field not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Field not found.");
            return null;
        }
        string fieldName = field.Name;

        if (!field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(field)}. It must be a static field.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(field)}. It must be a static field.");
            return null;
        }

        if (!CouldBeAssignedTo(field.FieldType, typeof(TValue)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(field)} with invalid value type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(field)} with invalid value type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        try
        {
            GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, convention, typeof(TValue), Type.EmptyTypes, field.DeclaringType ?? typeof(Accessor), true);
            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating static getter for {Formatter.Format(field)}:");

            IOpCodeEmitter il =
                 AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            EmitStaticGetterIntl(typeof(TValue), field, il);
            StaticGetter<TValue> getter = (StaticGetter<TValue>)method.CreateDelegate(typeof(StaticGetter<TValue>));
            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method static getter for {Formatter.Format(field)}.");
            return getter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating static getter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating static getter for {Formatter.Format(field)}.");
            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateStaticGetter(FieldInfo field, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticGetter";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (field == null)
        {
            if (throwOnError)
                throw new Exception("Field not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Field not found.");
            return null;
        }
        string fieldName = field.Name;

        if (!field.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(field)}. It must be a static field.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(field)}. It must be a static field.");
            return null;
        }

        try
        {
            GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, convention, field.FieldType, Type.EmptyTypes, field.DeclaringType ?? typeof(Accessor), true);
            bool logIl = LogILTraceMessages;
            if (logIl)
                reflectionToolsLogger?.LogDebug(source, $"IL: Generating static getter for {Formatter.Format(field)}:");

            IOpCodeEmitter il =
                 AsEmitter(method, LogILTraceMessages && reflectionToolsLogger != null)
                .WithLogSource(source);

            EmitStaticGetterIntl(field.FieldType, field, il);

            Type delegateType = typeof(StaticGetter<>).MakeGenericType(field.FieldType);
            Delegate getter = method.CreateDelegate(delegateType);
            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method static getter for {Formatter.Format(field)}.");
            return getter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating static getter for {ExceptionFormatter.Format(field)}.", ex);
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Error generating static getter for {Formatter.Format(field)}.");
            return null;
        }
    }

    private static void EmitStaticGetterIntl(Type valueType, FieldInfo field, IOpCodeEmitter il)
    {
        il.Emit(OpCodes.Ldsfld, field);

        if (valueType.IsValueType && !field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Unbox_Any, valueType);
        }
        else if (!valueType.IsValueType && field.FieldType.IsValueType)
        {
            il.Emit(OpCodes.Box, field.FieldType);
        }

        il.Emit(OpCodes.Ret);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticSetter<TValue>? GenerateStaticPropertySetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => GenerateStaticPropertySetter<TValue>(typeof(TDeclaringType), propertyName, throwOnError, allowUnsafeTypeBinding);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticGetter<TValue>? GenerateStaticPropertyGetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false)
        => GenerateStaticPropertyGetter<TValue>(typeof(TDeclaringType), propertyName, throwOnError, true);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    // ReSharper disable once MethodOverloadWithOptionalParameter
    public virtual StaticGetter<TValue>? GenerateStaticPropertyGetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = true)
        => GenerateStaticPropertyGetter<TValue>(typeof(TDeclaringType), propertyName, throwOnError, allowUnsafeTypeBinding);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticSetter<TValue>? GenerateStaticPropertySetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating static setter for <unknown>.{propertyName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateStaticPropertySetter", null, $"Error generating static setter for <unknown>.{propertyName}. Declaring type not found.");
            return null;
        }
        PropertyInfo? property = declaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        MethodInfo? setter = property?.GetSetMethod(true);

        if (setter is null || !setter.IsStatic || setter.GetParameters() is not { Length: 1 } parameters || (!parameters[0].ParameterType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching property: {ExceptionFormatter.Format(declaringType)}.{propertyName} with a setter.");
            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateStaticPropertySetter", null, $"Unable to find matching property {Formatter.Format(declaringType)}.{propertyName} with a setter.");
            return null;
        }

        return GenerateStaticCaller<StaticSetter<TValue>>(setter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticGetter<TValue>? GenerateStaticPropertyGetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating static getter for <unknown>.{propertyName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateStaticPropertyGetter", null, $"Error generating static getter for <unknown>.{propertyName}. Declaring type not found.");
            return null;
        }
        PropertyInfo? property = declaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        MethodInfo? getter = property?.GetGetMethod(true);

        if (getter is null || !getter.IsStatic || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching property: {ExceptionFormatter.Format(declaringType)}.{propertyName} with a getter.");
            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateStaticPropertyGetter", null, $"Unable to find matching property {Formatter.Format(declaringType)}.{propertyName} with a getter.");
            return null;
        }

        return GenerateStaticCaller<StaticGetter<TValue>>(getter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticSetter<TValue>? GenerateStaticPropertySetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        const string source = "Accessor.GenerateStaticPropertySetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");
            return null;
        }

        MethodInfo? setter = property.GetSetMethod(true);
        if (setter != null && !setter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static setter for {ExceptionFormatter.Format(property)}. It must be a static property.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static setter for {Formatter.Format(property)}. It must be a static property.");
            return null;
        }


        if (!CouldBeAssignedTo(typeof(TValue), property.PropertyType))
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static setter for {ExceptionFormatter.Format(property)} with invalid value type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static setter for {Formatter.Format(property)} with invalid value type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        if (setter is null || (!typeof(TValue).IsAssignableFrom(setter.ReturnType) && setter.ReturnType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static setter for {ExceptionFormatter.Format(property)}. It must have a defined 'set' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static setter for {Formatter.Format(property)}. It must have a defined 'set' method.");
            return null;
        }

        return GenerateStaticCaller<StaticSetter<TValue>>(setter, throwOnError, true);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual StaticGetter<TValue>? GenerateStaticPropertyGetter<TValue>(PropertyInfo property, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        const string source = "Accessor.GenerateStaticPropertySetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");
            return null;
        }

        MethodInfo? getter = property.GetSetMethod(true);
        if (getter != null && !getter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(property)}. It must be a static property.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(property)}. It must be a static property.");
            return null;
        }


        if (!CouldBeAssignedTo(typeof(TValue), property.PropertyType))
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(property)} with invalid value type {ExceptionFormatter.Format(typeof(TValue))}.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(property)} with invalid value type {Formatter.Format(typeof(TValue))}.");
            return null;
        }

        if (getter is null || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(property)}. It must have a defined 'get' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(property)}. It must have a defined 'get' method.");
            return null;
        }

        return GenerateStaticCaller<StaticGetter<TValue>>(getter, throwOnError, true);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateStaticPropertySetter(PropertyInfo property, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticPropertySetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");
            return null;
        }

        MethodInfo? setter = property.GetSetMethod(true);
        if (setter != null && !setter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static setter for {ExceptionFormatter.Format(property)}. It must be a static property.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static setter for {Formatter.Format(property)}. It must be a static property.");
            return null;
        }

        if (setter is null)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static setter for {ExceptionFormatter.Format(property)}. It must have a defined 'get' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static setter for {Formatter.Format(property)}. It must have a defined 'get' method.");
            return null;
        }

        Type delegateType = typeof(StaticSetter<>).MakeGenericType(property.PropertyType);
        return GenerateStaticCaller(delegateType, setter, throwOnError, true);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateStaticPropertyGetter(PropertyInfo property, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticPropertyGetter";
        if (property == null)
        {
            if (throwOnError)
                throw new Exception("Property not found.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, "Property not found.");
            return null;
        }

        MethodInfo? getter = property.GetGetMethod(true);
        if (getter != null && !getter.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(property)}. It must be a static property.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(property)}. It must be a static property.");
            return null;
        }

        if (getter is null)
        {
            if (throwOnError)
                throw new Exception($"Can't generate a static getter for {ExceptionFormatter.Format(property)}. It must have a defined 'get' method.");

            if (LogErrorMessages)
                Logger?.LogError(source, null, $"Can't generate a static getter for {Formatter.Format(property)}. It must have a defined 'get' method.");
            return null;
        }

        Type delegateType = typeof(StaticGetter<>).MakeGenericType(getter.ReturnType);
        return GenerateStaticCaller(delegateType, getter, throwOnError, true);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateInstanceCaller<TInstance>(string methodName, Type[]? parameters = null, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        MethodInfo? method = null;
        bool noneByName = false;
        try
        {
            method = typeof(TInstance).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
                     typeof(TInstance).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            noneByName = true;
        }
        catch (AmbiguousMatchException)
        {
            // ignored
        }
        if (method == null)
        {
            if (!noneByName && parameters != null)
            {
                method = typeof(TInstance).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                             null, CallingConventions.Any, parameters, null) ??
                         typeof(TInstance).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,
                             null, CallingConventions.Any, parameters, null);
            }
            if (method == null)
            {
                if (throwOnError)
                    throw new Exception($"Unable to find matching instance method: {ExceptionFormatter.Format(typeof(TInstance))}.{methodName}.");

                if (LogErrorMessages)
                    Logger?.LogError("Accessor.GenerateInstanceCaller", null, $"Unable to find matching instance method: {Formatter.Format(typeof(TInstance))}.{methodName}.");
                return null;
            }
        }

        return GenerateInstanceCaller(method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual TDelegate? GenerateInstanceCaller<TInstance, TDelegate>(string methodName, bool throwOnError = false, bool allowUnsafeTypeBinding = false, Type[]? parameters = null) where TDelegate : Delegate
    {
        MethodInfo? method = null;
        bool noneByName = false;
        try
        {
            method = typeof(TInstance).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) ??
                     typeof(TInstance).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            noneByName = true;
        }
        catch (AmbiguousMatchException)
        {
            // ignored
        }
        if (method == null)
        {
            if (!noneByName)
            {
                if (parameters == null)
                {
                    ParameterInfo[] paramInfo = GetParameters<TDelegate>();
                    parameters = paramInfo.Length < 2 ? Type.EmptyTypes : new Type[paramInfo.Length - 1];
                    for (int i = 0; i < parameters.Length; ++i)
                        parameters[i] = paramInfo[i + 1].ParameterType;
                }
                method = typeof(TInstance).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                             null, CallingConventions.Any, parameters, null) ??
                         typeof(TInstance).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy,
                             null, CallingConventions.Any, parameters, null);
            }
            if (method == null)
            {
                if (throwOnError)
                    throw new Exception($"Unable to find matching instance method: {ExceptionFormatter.Format(typeof(TInstance))}.{methodName}.");

                if (LogErrorMessages)
                    Logger?.LogError("Accessor.GenerateInstanceCaller", null, $"Unable to find matching instance method: {Formatter.Format(typeof(TInstance))}.{methodName}.");
                return null;
            }
        }

        return GenerateInstanceCaller<TDelegate>(method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateInstanceCaller(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (method == null || method.IsStatic || method.DeclaringType == null)
        {
            if (throwOnError)
                throw new Exception("Unable to find instance method.");
            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateInstanceCaller", null, "Unable to find instance method.");
            return null;
        }

        CheckFuncArrays();

        bool rtn = method.ReturnType != typeof(void);
        ParameterInfo[] p = method.GetParameters();
        int maxArgs = rtn ? FuncTypes!.Length : ActionTypes!.Length;
        if (p.Length + 1 > maxArgs)
        {
            if (throwOnError)
                throw new ArgumentException($"Method {ExceptionFormatter.Format(method)} can not have more than {maxArgs} arguments!", nameof(method));

            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateInstanceCaller", null, $"Method {Formatter.Format(method)} can not have more than {maxArgs} arguments!");
            return null;
        }

        Type deleType = GetDefaultDelegate(method.ReturnType, p, method.DeclaringType)!;
        return GenerateInstanceCaller(deleType, method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual TDelegate? GenerateInstanceCaller<TDelegate>(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TDelegate : Delegate
    {
        return (TDelegate?)GenerateInstanceCaller(typeof(TDelegate), method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateInstanceCaller(Type delegateType, MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        const string source = "Accessor.GenerateInstanceCaller";
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(ExceptionFormatter.Format(delegateType) + " is not a delegate.", nameof(delegateType));
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (method == null || method.IsStatic || method.DeclaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Unable to find instance method for delegate: {ExceptionFormatter.Format(delegateType)}.");
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Unable to find instance method for delegate: {Formatter.Format(delegateType)}.");
            return null;
        }

        ParameterInfo[] p = method.GetParameters();
        Type instance = method.DeclaringType;

        MethodInfo invokeMethod = delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public)!;
        ParameterInfo[] delegateParameters = invokeMethod.GetParameters();
        Type delegateReturnType = invokeMethod.ReturnType;
        bool shouldCallvirt = ShouldCallvirtRuntime(method);
        bool needsDynamicMethod = shouldCallvirt || (!instance.IsValueType && !allowUnsafeTypeBinding) || method.ReturnType != typeof(void) && delegateReturnType == typeof(void);
        bool isInstanceForValueType = method is { DeclaringType.IsValueType: true };
        shouldCallvirt |= !instance.IsValueType;

        // for some reason invoking a delegate with zero parameters and a null instance does not throw an exception.
        // Adding parameters changes this behavior.
        if (!isInstanceForValueType && p.Length == 0 && !allowUnsafeTypeBinding)
            needsDynamicMethod = true;

        if (p.Length != delegateParameters.Length - 1)
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance caller for {ExceptionFormatter.Format(method)}: incompatable delegate type: {ExceptionFormatter.Format(delegateType)}.");

            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Unable to create instance caller for {Formatter.Format(method)}: incompatable delegate type: {Formatter.Format(delegateType)}.");
            return null;
        }

        if (needsDynamicMethod && instance.IsInterface && !delegateParameters[0].ParameterType.IsInterface)
            needsDynamicMethod = false;

#if NET45_OR_GREATER || !NETFRAMEWORK
        // exact match
        if (!isInstanceForValueType && !needsDynamicMethod && delegateReturnType == method.ReturnType && instance.IsAssignableFrom(delegateParameters[0].ParameterType))
        {
            bool mismatch = false;
            for (int i = 1; i < delegateParameters.Length; ++i)
            {
                if (delegateParameters[i].ParameterType != p[i - 1].ParameterType)
                {
                    mismatch = true;
                    break;
                }
            }
            if (!mismatch)
            {
                try
                {
                    Delegate basicDelegate = method.CreateDelegate(delegateType);
                    if (LogDebugMessages || LogILTraceMessages)
                        reflectionToolsLogger?.LogDebug(source, $"Created basic delegate binding instance caller for {instance.FullName}.{method.Name}.");
                    return basicDelegate;
                }
                catch (Exception ex)
                {
                    if ((LogDebugMessages || LogILTraceMessages) && reflectionToolsLogger != null)
                    {
                        reflectionToolsLogger.LogDebug(source, $"Unable to create basic delegate binding instance caller for {instance.FullName}.{method.Name}.");
                        reflectionToolsLogger.LogDebug(source, ex.GetType() + " - " + ex.Message);
                    }
                }
            }
        }
#endif

        if (isInstanceForValueType && delegateParameters[0].ParameterType != typeof(object) && !IsReadOnly(method))
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance caller for {ExceptionFormatter.Format(method)} (non-readonly), you must pass structs ({ExceptionFormatter.Format(instance)}) as a boxed object (in {ExceptionFormatter.Format(delegateType)}).");

            if (LogErrorMessages || LogILTraceMessages)
                reflectionToolsLogger?.LogError(source, null, $"Unable to create instance caller for {Formatter.Format(method)} (non-readonly), you must pass structs ({Formatter.Format(instance)}) as a boxed object (in {Formatter.Format(delegateType)}).");
            return null;
        }

#if !NET6_0_OR_GREATER // unsafe type binding doesn't work past .NET 5.0
        // rough match, can unsafely cast to actual function arguments.
        if (allowUnsafeTypeBinding && !isInstanceForValueType && !needsDynamicMethod && !instance.IsValueType && delegateParameters[0].ParameterType.IsAssignableFrom(instance) && (method.ReturnType == typeof(void) && delegateReturnType == typeof(void) || !method.ReturnType.IsValueType && delegateReturnType.IsAssignableFrom(method.ReturnType)))
        {
            bool foundIncompatibleConversion = false;
            for (int i = 0; i < p.Length; ++i)
            {
                if (p[i].ParameterType.IsValueType && delegateParameters[i + 1].ParameterType != p[i].ParameterType)
                {
                    foundIncompatibleConversion = true;
                    break;
                }
            }
            if (!foundIncompatibleConversion)
            {
#line hidden
                try
                {
                    IntPtr ptr = method.MethodHandle.GetFunctionPointer();
                    // running the debugger here will crash the program so... don't.
                    object d2 = FormatterServices.GetUninitializedObject(delegateType);
                    delegateType.GetConstructors()[0].Invoke(d2, new object[] { null!, ptr });
                    if (LogDebugMessages || LogILTraceMessages)
                        reflectionToolsLogger?.LogDebug(source, $"Created unsafely binded delegate binding instance caller for {Formatter.Format(method)}.");
                    return (Delegate)d2;
                }
                catch (Exception ex)
                {
                    if ((LogDebugMessages || LogILTraceMessages) && reflectionToolsLogger != null)
                    {
                        reflectionToolsLogger.LogDebug(source, $"Unable to create unsafely binded delegate binding instance caller for {Formatter.Format(method)}.");
                        reflectionToolsLogger.LogDebug(source, ex.GetType() + " - " + ex.Message);
                    }
                }
#line default
            }
        }
#endif

        // generate dynamic method as a worst-case scenerio
        Type[] parameterTypes = new Type[delegateParameters.Length];
        for (int i = 0; i < delegateParameters.Length; ++i)
            parameterTypes[i] = delegateParameters[i].ParameterType;

        GetDynamicMethodFlags(false, out MethodAttributes attributes, out CallingConventions convention);
        DynamicMethod dynMethod = new DynamicMethod("Invoke" + method.Name, attributes, convention, delegateReturnType, parameterTypes, instance.IsInterface ? typeof(Accessor) : instance, true);
        bool logIl = LogILTraceMessages;
        if (logIl)
            reflectionToolsLogger?.LogDebug(source, $"IL: Generating instance caller for {Formatter.Format(method)}:");
        dynMethod.DefineParameter(1, ParameterAttributes.None, "this");

        for (int i = 0; i < p.Length; ++i)
            dynMethod.DefineParameter(i + 2, p[i].Attributes, p[i].Name);

        IOpCodeEmitter generator =
             AsEmitter(dynMethod, LogILTraceMessages && reflectionToolsLogger != null)
            .WithLogSource(source);

        if (instance.IsValueType)
        {
            if (!delegateParameters[0].ParameterType.IsValueType)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Unbox, instance);
            }
            else
            {
                generator.Emit(OpCodes.Ldarga_S, 0);
            }
        }
        else
        {
            generator.Emit(OpCodes.Ldarg_0);
        }

        for (int i = 0; i < p.Length; ++i)
            generator.EmitParameter(i + 1, $"Invalid argument type passed to instance caller for {ExceptionFormatter.Format(method)} at parameter {i.ToString(CultureInfo.InvariantCulture)} ({p[i].Name}). Expected {ExceptionFormatter.Format(p[i].ParameterType)}.", false, type: parameterTypes[i + 1], p[i].ParameterType);

        OpCode call = shouldCallvirt ? OpCodes.Callvirt : OpCodes.Call;
        generator.Emit(call, method);
        if (method.ReturnType != typeof(void) && delegateReturnType == typeof(void))
        {
            generator.Emit(OpCodes.Pop);
        }
        else if (method.ReturnType != typeof(void))
        {
            if (method.ReturnType.IsValueType && !delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Box, method.ReturnType);
            }
            else if (!method.ReturnType.IsValueType && delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, delegateReturnType);
            }
        }
        else if (delegateReturnType != typeof(void))
        {
            if (!delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Ldnull);
            }
            else
            {
                generator.DeclareLocal(delegateReturnType);
                generator.Emit(OpCodes.Ldloca_S, 0);
                generator.Emit(OpCodes.Initobj, delegateReturnType);
                generator.Emit(OpCodes.Ldloc_0);
            }
        }
        generator.Emit(OpCodes.Ret);

        try
        {
            Delegate dynamicDelegate = dynMethod.CreateDelegate(delegateType);
            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method instance caller for {Formatter.Format(method)}.");
            return dynamicDelegate;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance caller for {ExceptionFormatter.Format(method)}.", ex);

            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Unable to create instance caller for {Formatter.Format(method)}.");
            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateStaticCaller<TDeclaringType>(string methodName, Type[]? parameters = null, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        MethodInfo? method = null;
        try
        {
            method = typeof(TDeclaringType).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }
        catch (AmbiguousMatchException)
        {
            // ignored
        }
        if (method == null)
        {
            if (parameters != null)
            {
                method = typeof(TDeclaringType).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null, CallingConventions.Any, parameters, null);
            }
            if (method == null)
            {
                if (throwOnError)
                    throw new Exception($"Unable to find matching static method: {ExceptionFormatter.Format(typeof(TDeclaringType))}.{methodName}.");

                if (LogErrorMessages)
                    Logger?.LogError("Accessor.GenerateStaticCaller", null, $"Unable to find matching static method: {Formatter.Format(typeof(TDeclaringType))}.{methodName}.");
                return null;
            }
        }

        return GenerateStaticCaller(method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual TDelegate? GenerateStaticCaller<TDeclaringType, TDelegate>(string methodName, bool throwOnError = false, bool allowUnsafeTypeBinding = false, Type[]? parameters = null) where TDelegate : Delegate
    {
        MethodInfo? method = null;
        try
        {
            method = typeof(TDeclaringType).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        }
        catch (AmbiguousMatchException)
        {
            // ignored
        }
        if (method == null)
        {
            if (parameters == null)
            {
                ParameterInfo[] paramInfo = GetParameters<TDelegate>();
                parameters = new Type[paramInfo.Length];
                for (int i = 0; i < paramInfo.Length; ++i)
                    parameters[i] = paramInfo[i].ParameterType;
            }
            method = typeof(TDeclaringType).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null, CallingConventions.Any, parameters, null);
            if (method == null)
            {
                if (throwOnError)
                    throw new Exception($"Unable to find matching static method: {ExceptionFormatter.Format(typeof(TDeclaringType))}.{methodName}.");

                if (LogErrorMessages)
                    Logger?.LogError("Accessor.GenerateStaticCaller", null, $"Unable to find matching static method: {Formatter.Format(typeof(TDeclaringType))}.{methodName}.");
                return null;
            }
        }

        return GenerateStaticCaller<TDelegate>(method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateStaticCaller(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (method == null || !method.IsStatic)
        {
            if (throwOnError)
                throw new Exception("Unable to find static method.");
            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateStaticCaller", null, "Unable to find static method.");
            return null;
        }

        CheckFuncArrays();

        bool rtn = method.ReturnType != typeof(void);
        ParameterInfo[] p = method.GetParameters();
        int maxArgs = rtn ? FuncTypes!.Length : ActionTypes!.Length;
        if (p.Length > maxArgs)
        {
            if (throwOnError)
                throw new ArgumentException($"Method {ExceptionFormatter.Format(method)} can not have more than {maxArgs} arguments!", nameof(method));

            if (LogErrorMessages)
                Logger?.LogError("Accessor.GenerateStaticCaller", null, $"Method {Formatter.Format(method)} can not have more than {maxArgs} arguments!");
            return null;
        }

        Type deleType = GetDefaultDelegate(method.ReturnType, p, null)!;
        return GenerateStaticCaller(deleType, method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual TDelegate? GenerateStaticCaller<TDelegate>(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TDelegate : Delegate
    {
        return (TDelegate?)GenerateStaticCaller(typeof(TDelegate), method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Delegate? GenerateStaticCaller(Type delegateType, MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(ExceptionFormatter.Format(delegateType) + " is not a delegate.", nameof(delegateType));

        const string source = "Accessor.GenerateStaticCaller";
        IReflectionToolsLogger? reflectionToolsLogger = Logger;
        if (method == null || !method.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Unable to find static method for delegate: {ExceptionFormatter.Format(delegateType)}.");
            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Unable to find static method for delegate: {Formatter.Format(delegateType)}.");
            return null;
        }

        ParameterInfo[] p = method.GetParameters();

        MethodInfo invokeMethod = delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public)!;
        ParameterInfo[] delegateParameters = invokeMethod.GetParameters();
        Type delegateReturnType = invokeMethod.ReturnType;
        bool needsDynamicMethod = method.ReturnType != typeof(void) && delegateReturnType == typeof(void);
        if (p.Length != delegateParameters.Length)
        {
            if (throwOnError)
                throw new Exception($"Unable to create static caller for {ExceptionFormatter.Format(method)}: incompatable delegate type: {ExceptionFormatter.Format(delegateType)}.");

            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, null, $"Unable to create static caller for {Formatter.Format(method)}: incompatable delegate type: {Formatter.Format(delegateType)}.");
            return null;
        }

#if NET45_OR_GREATER || !NETFRAMEWORK
        // exact match
        if (!needsDynamicMethod && delegateReturnType == method.ReturnType)
        {
            bool mismatch = false;
            for (int i = 0; i < delegateParameters.Length; ++i)
            {
                if (delegateParameters[i].ParameterType != p[i].ParameterType)
                {
                    mismatch = true;
                    break;
                }
            }
            if (!mismatch)
            {
                try
                {
                    Delegate basicDelegateCaller = method.CreateDelegate(delegateType);
                    if (LogDebugMessages || LogILTraceMessages)
                        reflectionToolsLogger?.LogDebug(source, $"Created basic delegate binding static caller for {Formatter.Format(method)}.");
                    return basicDelegateCaller;
                }
                catch (Exception ex)
                {
                    if ((LogDebugMessages || LogILTraceMessages) && reflectionToolsLogger != null)
                    {
                        reflectionToolsLogger.LogDebug(source, $"Unable to create basic delegate binding static caller for {Formatter.Format(method)}.");
                        reflectionToolsLogger.LogDebug(source, ex.GetType() + " - " + ex.Message);
                    }
                }
            }
        }
#endif

#if !NET6_0_OR_GREATER // unsafe type binding doesn't work past .NET 5.0
        // rough match, can unsafely cast to actual function arguments.
        if (!needsDynamicMethod && allowUnsafeTypeBinding && (method.ReturnType == typeof(void) && delegateReturnType == typeof(void) || !method.ReturnType.IsValueType && delegateReturnType.IsAssignableFrom(method.ReturnType)))
        {
            bool foundIncompatibleConversion = false;
            for (int i = 0; i < p.Length; ++i)
            {
                if (p[i].ParameterType.IsValueType && delegateParameters[i].ParameterType != p[i].ParameterType)
                {
                    foundIncompatibleConversion = true;
                    break;
                }
            }
            if (!foundIncompatibleConversion)
            {
#line hidden
                try
                {
                    IntPtr ptr = method.MethodHandle.GetFunctionPointer();
                    // running the debugger here will crash the program so... don't.
                    object d2 = FormatterServices.GetUninitializedObject(delegateType);
                    delegateType.GetConstructors()[0].Invoke(d2, new object[] { null!, ptr });
                    if (LogDebugMessages || LogILTraceMessages)
                        reflectionToolsLogger?.LogDebug(source, $"Created unsafely binded delegate binding static caller for {Formatter.Format(method)}.");
                    return (Delegate)d2;
                }
                catch (Exception ex)
                {
                    if ((LogDebugMessages || LogILTraceMessages) && reflectionToolsLogger != null)
                    {
                        reflectionToolsLogger.LogDebug(source, $"Unable to create unsafely binded delegate binding static caller for {Formatter.Format(method)}.");
                        reflectionToolsLogger.LogDebug(source, ex.GetType() + " - " + ex.Message);
                    }
                }
#line default
            }
        }
#endif

        // generate dynamic method as a worst-case scenerio
        Type[] parameterTypes = new Type[delegateParameters.Length];
        for (int i = 0; i < delegateParameters.Length; ++i)
            parameterTypes[i] = delegateParameters[i].ParameterType;

        GetDynamicMethodFlags(true, out MethodAttributes attributes, out CallingConventions convention);
        DynamicMethod dynMethod = new DynamicMethod("Invoke" + method.Name, attributes, convention, delegateReturnType, parameterTypes, method.DeclaringType is not { IsInterface: false } ? typeof(Accessor) : method.DeclaringType, true);
        bool logIl = LogILTraceMessages;
        if (logIl)
            reflectionToolsLogger?.LogDebug(source, $"IL: Generating static caller for {Formatter.Format(method)}:");

        for (int i = 0; i < p.Length; ++i)
            dynMethod.DefineParameter(i + 1, p[i].Attributes, p[i].Name);

        IOpCodeEmitter generator =
            AsEmitter(dynMethod, LogILTraceMessages && reflectionToolsLogger != null)
            .WithLogSource(source);

        for (int i = 0; i < p.Length; ++i)
            generator.EmitParameter(i, $"Invalid argument type passed to static caller for {ExceptionFormatter.Format(method)} at parameter {i.ToString(CultureInfo.InvariantCulture)} ({p[i].Name}). Expected {ExceptionFormatter.Format(p[i].ParameterType)}.", false, type: parameterTypes[i], p[i].ParameterType);

        generator.Emit(OpCodes.Call, method);
        if (method.ReturnType != typeof(void) && delegateReturnType == typeof(void))
        {
            generator.Emit(OpCodes.Pop);
        }
        else if (method.ReturnType != typeof(void))
        {
            if (method.ReturnType.IsValueType && !delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Box, method.ReturnType);
            }
            else if (!method.ReturnType.IsValueType && delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, delegateReturnType);
            }
        }
        else if (delegateReturnType != typeof(void))
        {
            if (!delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Ldnull);
            }
            else
            {
                generator.DeclareLocal(delegateReturnType);
                generator.Emit(OpCodes.Ldloca_S, 0);
                generator.Emit(OpCodes.Initobj, delegateReturnType);
                generator.Emit(OpCodes.Ldloc_0);
            }
        }

        generator.Emit(OpCodes.Ret);

        try
        {
            Delegate dynamicDelegate = dynMethod.CreateDelegate(delegateType);
            if (LogDebugMessages || logIl)
                reflectionToolsLogger?.LogDebug(source, $"Created dynamic method static caller for {Formatter.Format(method)}.");
            return dynamicDelegate;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Unable to create static caller for {ExceptionFormatter.Format(method)}.", ex);

            if (LogErrorMessages)
                reflectionToolsLogger?.LogError(source, ex, $"Unable to create static caller for {Formatter.Format(method)}.");
            return null;
        }
    }

    /// <inheritdoc />
    public virtual void GetDynamicMethodFlags(bool @static, out MethodAttributes attributes, out CallingConventions convention)
    {
        // mono has less restrictions on dynamic method attributes and conventions
        if (IsMono)
        {
            attributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | (@static ? MethodAttributes.Static : 0);
            convention = !@static ? CallingConventions.HasThis : CallingConventions.Standard;
        }
        else
        {
            attributes = MethodAttributes.Public | MethodAttributes.Static;
            convention = CallingConventions.Standard;
        }
    }
    internal static void CheckFuncArrays()
    {
        FuncTypes ??=
        [
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>),
#if NET40_OR_GREATER || !NETFRAMEWORK
            typeof(Func<,,,,,>),
            typeof(Func<,,,,,,>),
            typeof(Func<,,,,,,,>),
            typeof(Func<,,,,,,,,>),
            typeof(Func<,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,>),
            typeof(Func<,,,,,,,,,,,,,,,,>)
#endif
        ];
        ActionTypes ??=
        [
            typeof(Action<>),
            typeof(Action<,>),
            typeof(Action<,,>),
            typeof(Action<,,,>),
#if NET40_OR_GREATER || !NETFRAMEWORK
            typeof(Action<,,,,>),
            typeof(Action<,,,,,>),
            typeof(Action<,,,,,,>),
            typeof(Action<,,,,,,,>),
            typeof(Action<,,,,,,,,>),
            typeof(Action<,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,>),
            typeof(Action<,,,,,,,,,,,,,,,>)
#endif
        ];
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetVisibility(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        MemberVisibility lowest = MemberVisibility.Public;
        bool isFamily = false, isAssembly = false;
        for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
        {
            TypeAttributes attr = nestingType.Attributes;
            if (nestingType.IsNested)
            {
                if ((attr & TypeAttributes.NestedFamORAssem) == TypeAttributes.NestedFamORAssem)
                {
                    if (lowest > MemberVisibility.ProtectedInternal)
                        lowest = MemberVisibility.ProtectedInternal;
                }
                if ((attr & TypeAttributes.NestedFamANDAssem) == TypeAttributes.NestedFamANDAssem)
                {
                    if (lowest > MemberVisibility.PrivateProtected)
                        lowest = MemberVisibility.PrivateProtected;
                }
                if ((attr & TypeAttributes.NestedAssembly) == TypeAttributes.NestedAssembly)
                {
                    isAssembly = true;
                    MemberVisibility newVis = isFamily ? MemberVisibility.PrivateProtected : MemberVisibility.Internal;
                    if (lowest > newVis)
                        lowest = newVis;
                }
                if ((attr & TypeAttributes.NestedFamily) != 0)
                {
                    isFamily = true;
                    MemberVisibility newVis = isAssembly ? MemberVisibility.PrivateProtected : MemberVisibility.Protected;
                    if (lowest > newVis)
                        lowest = newVis;
                }

                if ((attr & TypeAttributes.NestedPrivate) == TypeAttributes.NestedPrivate)
                    return MemberVisibility.Private;

            }
            else if (nestingType.IsNotPublic)
            {
                return lowest == MemberVisibility.Public ? MemberVisibility.Internal : lowest;
            }
            else return lowest;
        }

        return MemberVisibility.Unknown;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetVisibility(MethodBase method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        MethodAttributes attr = method.Attributes;
        if ((attr & MethodAttributes.Public) == MethodAttributes.Public)
            return MemberVisibility.Public;
        if ((attr & MethodAttributes.Assembly) == MethodAttributes.Assembly)
            return MemberVisibility.Internal;
        if ((attr & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem)
            return MemberVisibility.ProtectedInternal;
        if ((attr & MethodAttributes.FamANDAssem) != 0)
            return MemberVisibility.PrivateProtected;
        if ((attr & MethodAttributes.Family) != 0)
            return MemberVisibility.Protected;
        if ((attr & MethodAttributes.Private) != 0)
            return MemberVisibility.Private;

        return MemberVisibility.Unknown;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetVisibility(FieldInfo field)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        FieldAttributes attr = field.Attributes;
        if ((attr & FieldAttributes.Public) == FieldAttributes.Public)
            return MemberVisibility.Public;
        if ((attr & FieldAttributes.Assembly) == FieldAttributes.Assembly)
            return MemberVisibility.Internal;
        if ((attr & FieldAttributes.FamORAssem) == FieldAttributes.FamORAssem)
            return MemberVisibility.ProtectedInternal;
        if ((attr & FieldAttributes.FamANDAssem) != 0)
            return MemberVisibility.PrivateProtected;
        if ((attr & FieldAttributes.Family) != 0)
            return MemberVisibility.Protected;
        if ((attr & FieldAttributes.Private) != 0)
            return MemberVisibility.Private;

        return MemberVisibility.Unknown;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetVisibility(PropertyInfo property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        MethodInfo? getMethod = property.GetGetMethod(true);
        if (getMethod is { IsPublic: true })
            return MemberVisibility.Public;

        MethodInfo? setMethod = property.GetSetMethod(true);
        return GetHighestVisibility(setMethod, getMethod);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetVisibility(EventInfo @event)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        MethodInfo? getMethod = @event.GetAddMethod(true);
        if (getMethod is { IsPublic: true })
            return MemberVisibility.Public;

        MethodInfo? removeMethod = @event.GetRemoveMethod(true);
        if (removeMethod is { IsPublic: true })
            return MemberVisibility.Public;

        MethodInfo? raiseMethod = @event.GetRaiseMethod(true);
        return GetHighestVisibility(raiseMethod, removeMethod, getMethod);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetHighestVisibility(MethodBase? method1, MethodBase? method2)
    {
        MemberVisibility highest = MemberVisibility.Private;

        CheckHighest(method1, ref highest);
        if (highest == MemberVisibility.Public)
            return MemberVisibility.Public;

        CheckHighest(method2, ref highest);

        return highest;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetHighestVisibility(MethodBase? method1, MethodBase? method2, MethodBase? method3)
    {
        MemberVisibility highest = MemberVisibility.Private;

        CheckHighest(method1, ref highest);
        if (highest == MemberVisibility.Public)
            return MemberVisibility.Public;

        CheckHighest(method2, ref highest);
        if (highest == MemberVisibility.Public)
            return MemberVisibility.Public;

        CheckHighest(method3, ref highest);

        return highest;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetHighestVisibility(params MethodBase?[] methods)
    {
        MemberVisibility highest = MemberVisibility.Private;

        for (int i = 0; i < methods.Length; ++i)
        {
            CheckHighest(methods[i], ref highest);
            if (highest == MemberVisibility.Public)
                return MemberVisibility.Public;
        }

        return highest;
    }
#if !NETSTANDARD
    /// <summary>
    /// Get the highest visibilty needed for all of the given methods to be visible. Methods which are <see langword="null"/> are ignored.
    /// </summary>
    /// <remarks>Will always be at least <see cref="MemberVisibility.Private"/>. All parameters should be at least of type <see cref="MethodBase"/>.</remarks>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="InvalidCastException">A parameter was passed to the arglist that is not a <see cref="MethodBase"/>.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MemberVisibility GetHighestVisibility(__arglist)
    {
        ArgIterator iterator = new ArgIterator(__arglist);
        int ct = iterator.GetRemainingCount();

        MemberVisibility highest = MemberVisibility.Private;
        for (int i = 0; i < ct; ++i)
        {
            CheckHighest(__refvalue(iterator.GetNextArg(), MethodBase), ref highest);
            if (highest == MemberVisibility.Public)
                return MemberVisibility.Public;
        }

        return highest;
    }
#endif

    private static void CheckHighest(MethodBase? method, ref MemberVisibility highest)
    {
        if (method == null)
            return;

        MethodAttributes attr = method.Attributes;
        if ((attr & MethodAttributes.Public) == MethodAttributes.Public)
        {
            highest = MemberVisibility.Public;
            return;
        }
        if ((attr & MethodAttributes.Assembly) == MethodAttributes.Assembly)
        {
            if (highest is MemberVisibility.ProtectedInternal or MemberVisibility.Protected)
                highest = MemberVisibility.Public;
            else if (highest is MemberVisibility.Private or MemberVisibility.PrivateProtected)
                highest = MemberVisibility.Internal;
            return;
        }
        if ((attr & MethodAttributes.Private) != 0)
            return;
        if ((attr & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem)
        {
            if (highest is MemberVisibility.Private or MemberVisibility.Protected or MemberVisibility.Internal)
                highest = MemberVisibility.ProtectedInternal;
            return;
        }
        if ((attr & MethodAttributes.Family) != 0)
        {
            if (highest is MemberVisibility.ProtectedInternal or MemberVisibility.Internal)
                highest = MemberVisibility.Public;
            else if (highest is MemberVisibility.Private or MemberVisibility.PrivateProtected)
                highest = MemberVisibility.Protected;
            return;
        }
        if ((attr & MethodAttributes.FamANDAssem) != 0)
        {
            if (highest == MemberVisibility.Private)
                highest = MemberVisibility.PrivateProtected;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool AssemblyGivesInternalAccess(Assembly assembly, string assemblyName)
    {
        InternalsVisibleToAttribute[] internalsVisibleTo = GetAttributesSafe<InternalsVisibleToAttribute>(assembly);
        for (int i = 0; i < internalsVisibleTo.Length; ++i)
        {
            if (internalsVisibleTo[i].AssemblyName.Equals(assemblyName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsExtern(MethodBase method)
    {
        if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
            return true;

        if (method.IsAbstract || method.IsVirtual || method.DeclaringType is { IsInterface: true })
            return false;

        try
        {
            method.GetMethodBody();
            return false;
        }
        catch
        {
            return true;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsExtern(FieldInfo field) => field.IsPinvokeImpl;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsExtern(PropertyInfo property, bool checkGetterFirst = true)
    {
        MethodInfo? method = checkGetterFirst ? property.GetGetMethod(true) : property.GetSetMethod(true);
        if (method == null)
        {
            method = checkGetterFirst ? property.GetSetMethod(true) : property.GetGetMethod(true);
            if (method == null)
                return false;
        }

        return IsExtern(method);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public bool IsDefinedSafe<TAttribute>(ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute => IsDefinedSafe(member, typeof(TAttribute), inherit);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsDefinedSafe(ICustomAttributeProvider member, Type attributeType, bool inherit = false)
    {
        try
        {
            return member.IsDefined(attributeType, inherit);
        }
        catch (TypeLoadException ex)
        {
            if (LogDebugMessages)
                LogTypeLoadException(ex, "Accessor.IsDefinedSafe", $"Failed to check {member} for attribute {attributeType.Name}.");

            return false;
        }
        catch (FileNotFoundException ex)
        {
            if (LogDebugMessages)
                LogFileNotFoundException(ex, "Accessor.IsDefinedSafe", $"Failed to check {member} for attribute {attributeType.Name}.");

            return false;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsCompilerAttributeDefinedSafe(ICustomAttributeProvider member, string typeName, bool inherit = false)
    {
        typeName = "System.Runtime.CompilerServices." + typeName;
        try
        {
            object[] attributes = member.GetCustomAttributes(inherit);
            for (int i = 0; i < attributes.Length; ++i)
            {
                object attribute = attributes[i];
                if (attribute == null)
                    continue;

                Type type = attribute.GetType();
                if (string.Equals(type.FullName, typeName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
        catch (TypeLoadException ex)
        {
            if (LogDebugMessages)
                LogTypeLoadException(ex, "Accessor.IsCompilerAttributeDefinedSafe", $"Failed to check {member} for attribute {typeName}.");

            return false;
        }
        catch (FileNotFoundException ex)
        {
            if (LogDebugMessages)
                LogFileNotFoundException(ex, "Accessor.IsCompilerAttributeDefinedSafe", $"Failed to check {member} for attribute {typeName}.");

            return false;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public bool HasAttributeSafe<TAttribute>(ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute => HasAttributeSafe(member, typeof(TAttribute), inherit);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool HasAttributeSafe(ICustomAttributeProvider member, Type attributeType, bool inherit = false)
    {
        try
        {
            return member.IsDefined(attributeType, inherit);
        }
        catch (TypeLoadException ex)
        {
            if (LogDebugMessages)
                LogTypeLoadException(ex, "Accessor.HasAttributeSafe", $"Failed to check {member} for attribute {attributeType.Name}.");

            return false;
        }
        catch (FileNotFoundException ex)
        {
            if (LogDebugMessages)
                LogFileNotFoundException(ex, "Accessor.HasAttributeSafe", $"Failed to check {member} for attribute {attributeType.Name}.");

            return false;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool HasCompilerAttributeSafe(ICustomAttributeProvider member, string typeName, bool inherit = false)
    {
        typeName = "System.Runtime.CompilerServices." + typeName;
        try
        {
            object[] attributes = member.GetCustomAttributes(inherit);
            for (int i = 0; i < attributes.Length; ++i)
            {
                object attribute = attributes[i];
                if (attribute == null)
                    continue;

                Type type = attribute.GetType();
                if (string.Equals(type.FullName, typeName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
        catch (TypeLoadException ex)
        {
            if (LogDebugMessages)
                LogTypeLoadException(ex, "Accessor.HasCompilerAttributeSafe", $"Failed to check {member} for attribute {typeName}.");

            return false;
        }
        catch (FileNotFoundException ex)
        {
            if (LogDebugMessages)
                LogFileNotFoundException(ex, "Accessor.HasCompilerAttributeSafe", $"Failed to check {member} for attribute {typeName}.");

            return false;
        }
    }

    private void LogTypeLoadException(TypeLoadException ex, string source, string context)
    {
        string msg = context + $" Can't load type: {ex.TypeName}.";
        if (ex.InnerException != null)
            msg += "(" + ex.InnerException.GetType().Name + " | " + ex.InnerException.Message + ")";
        Logger?.LogDebug(source, msg);
    }
    private void LogFileNotFoundException(FileNotFoundException ex, string source, string context)
    {
        string msg = context + $" Missing assembly: {ex.FileName}.";
        Logger?.LogDebug(source, msg);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public TAttribute? GetAttributeSafe<TAttribute>(ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute
        => GetAttributeSafe(member, typeof(TAttribute), inherit) as TAttribute;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Attribute? GetAttributeSafe(ICustomAttributeProvider member, Type attributeType, bool inherit = false)
    {
        try
        {
            switch (member)
            {
                case MemberInfo memberInfo:
                    return Attribute.GetCustomAttribute(memberInfo, attributeType, inherit);
                case Module module:
                    return Attribute.GetCustomAttribute(module, attributeType, inherit);
                case Assembly assembly:
                    return Attribute.GetCustomAttribute(assembly, attributeType, inherit);
                case ParameterInfo parameterInfo:
                    return Attribute.GetCustomAttribute(parameterInfo, attributeType, inherit);
                default:
                    object[] attributes = member.GetCustomAttributes(attributeType, inherit);
                    if (attributes is not { Length: > 0 })
                        return null;
                    if (attributes.Length > 1)
                        throw new AmbiguousMatchException($"Multiple attributes of type {attributeType.FullName}.");

                    return attributes[0] as Attribute;
            }
        }
        catch (TypeLoadException ex)
        {
            if (LogDebugMessages)
                LogTypeLoadException(ex, "Accessor.GetAttributeSafe", $"Failed to get an attribute of type {attributeType.Name} from {member}.");

            return null;
        }
        catch (FileNotFoundException ex)
        {
            if (LogDebugMessages)
                LogFileNotFoundException(ex, "Accessor.GetAttributeSafe", $"Failed to get an attribute of type {attributeType.Name} from {member}.");

            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public TAttribute[] GetAttributesSafe<TAttribute>(ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute
        => (TAttribute[])GetAttributesSafe(member, typeof(TAttribute), inherit);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Attribute[] GetAttributesSafe(ICustomAttributeProvider member, Type attributeType, bool inherit = false)
    {
        try
        {
            object[] array = member.GetCustomAttributes(attributeType, inherit);
            if (array is { Length: > 0 })
            {
                if (array.GetType().GetElementType() == attributeType)
                    return (Attribute[])array;

                int ct = 0;
                for (int i = 0; i < array.Length; ++i)
                {
                    if (array[i] is Attribute attr && attributeType.IsInstanceOfType(attr))
                        ++ct;
                }

                Attribute[] array2 = (Attribute[])Array.CreateInstance(attributeType, ct);
                for (int i = 0; i < array.Length; ++i)
                {
                    if (array[i] is Attribute attr && attributeType.IsInstanceOfType(attr))
                        array2[array2.Length - --ct - 1] = attr;
                }

                return array2;
            }
        }
        catch (TypeLoadException ex)
        {
            if (LogDebugMessages)
                LogTypeLoadException(ex, "Accessor.GetAttributesSafe", $"Failed to get attributes of type {attributeType.Name} from {member}.");
        }
        catch (FileNotFoundException ex)
        {
            if (LogDebugMessages)
                LogFileNotFoundException(ex, "Accessor.GetAttributesSafe", $"Failed to get attributes of type {attributeType.Name} from {member}.");
        }

        return attributeType == typeof(Attribute)
#if NET461_OR_GREATER || !NETFRAMEWORK
            ? Array.Empty<Attribute>()
#else
            ? new Attribute[0]
#endif
            : (Attribute[])Array.CreateInstance(attributeType, 0);
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public bool TryGetAttributeSafe<TAttribute>(ICustomAttributeProvider member, out TAttribute attribute, bool inherit = false) where TAttribute : Attribute
    {
        attribute = (GetAttributeSafe(member, typeof(TAttribute), inherit) as TAttribute)!;
        return attribute != null;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsReadOnly(ICustomAttributeProvider member)
    {
        if (member is FieldInfo field)
            return field.IsInitOnly;

        if (member is MethodBase)
        {
            if (member is not MethodInfo { DeclaringType.IsValueType: true })
                return false;
        }
        else if (member is Type { IsValueType: false })
            return false;

        if (HasCompilerAttributeSafe(member, "IsReadOnlyAttribute", inherit: false))
            return true;

        if (member is MethodInfo { DeclaringType: not null } method && IsReadOnly(method.DeclaringType))
            return true;

        return false;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsByRefLikeType(Type type)
    {
#if NETCOREAPP2_1_OR_GREATER || NET || NETSTANDARD2_1_OR_GREATER
        return type.IsByRefLike;
#else
        return HasCompilerAttributeSafe(type, "IsByRefLikeAttribute", inherit: false);
#endif
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsIgnored(Type type) => IsDefinedSafe(type, _ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsIgnored(MemberInfo member) => IsDefinedSafe(member, _ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsIgnored(Assembly assembly) => IsDefinedSafe(assembly, _ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsIgnored(ParameterInfo parameter) => IsDefinedSafe(parameter, _ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool IsIgnored(Module module) => IsDefinedSafe(module, _ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual int GetPriority(Type type) => GetAttributeSafe(type, _priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual int GetPriority(MemberInfo member) => GetAttributeSafe(member, _priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual int GetPriority(Assembly assembly) => GetAttributeSafe(assembly, _priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual int GetPriority(ParameterInfo parameter) => GetAttributeSafe(parameter, _priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual int GetPriority(Module module) => GetAttributeSafe(module, _priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;
    
    private int SortTypesByPriorityHandler(Type a, Type b)
    {
        return GetPriority(b).CompareTo(GetPriority(a));
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MethodInfo? GetMethod(Delegate @delegate)
    {
        try
        {
            return @delegate.Method;
        }
        catch (MemberAccessException)
        {
            if (LogDebugMessages)
                Logger?.LogDebug("Accessor.GetMethod", $"Failed to get a method from a delegate of type {@delegate.GetType().Name}.");

            return null;
        }
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Type? GetDefaultDelegate(Type returnType,
#if NET45_OR_GREATER || !NETFRAMEWORK
        IReadOnlyList<ParameterInfo> parameters,
#else
        IList<ParameterInfo> parameters,
#endif
        Type? instanceType)
    {
        CheckFuncArrays();

        if (instanceType == null)
        {
            if (returnType != typeof(void))
            {
                if (FuncTypes!.Length <= parameters.Count)
                    return null;
                Type[] p2 = new Type[parameters.Count + 1];
                for (int i = 1; i < p2.Length - 1; ++i)
                    p2[i] = parameters[i].ParameterType;
                p2[p2.Length - 1] = returnType;
                return FuncTypes[parameters.Count].MakeGenericType(p2);
            }
            else
            {
                if (ActionTypes!.Length <= parameters.Count)
                    return null;
                Type[] p2 = new Type[parameters.Count];
                for (int i = 1; i < p2.Length; ++i)
                    p2[i] = parameters[i].ParameterType;
                return ActionTypes[parameters.Count].MakeGenericType(p2);
            }
        }
        else
        {
            if (returnType != typeof(void))
            {
                if (FuncTypes!.Length <= parameters.Count)
                    return null;
                Type[] p2 = new Type[parameters.Count + 2];
                p2[0] = instanceType;
                for (int i = 1; i < p2.Length - 1; ++i)
                    p2[i] = parameters[i - 1].ParameterType;
                p2[p2.Length - 1] = returnType;
                return FuncTypes[parameters.Count].MakeGenericType(p2);
            }
            else
            {
                if (ActionTypes!.Length <= parameters.Count)
                    return null;
                Type[] p2 = new Type[parameters.Count + 1];
                p2[0] = instanceType;
                for (int i = 1; i < p2.Length; ++i)
                    p2[i] = parameters[i - 1].ParameterType;
                return ActionTypes[parameters.Count].MakeGenericType(p2);
            }
        }
    }

    /// <inheritdoc />
    public virtual void ForEachBaseType(Type type, ForEachBaseType action, bool includeParent = true, bool excludeSystemBase = true)
    {
        Type? type2 = type;
        if (includeParent)
        {
            action(type2, 0);
        }

        type2 = type.BaseType;

        int level = 0;
        for (; type2 != null && (!excludeSystemBase || type2 != typeof(object) && type2 != typeof(ValueType)); type2 = type2.BaseType)
        {
            ++level;
            action(type2, level);
        }
    }

    /// <inheritdoc />
    public virtual void ForEachBaseType(Type type, ForEachBaseTypeWhile action, bool includeParent = true, bool excludeSystemBase = true)
    {
        Type? type2 = type;
        if (includeParent)
        {
            if (!action(type2, 0))
                return;
        }

        type2 = type.BaseType;

        int level = 0;
        for (; type2 != null && (!excludeSystemBase || type2 != typeof(object) && type2 != typeof(ValueType)); type2 = type2.BaseType)
        {
            ++level;
            if (!action(type2, level))
                return;
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public List<Type> GetTypesSafe(bool removeIgnored = false) => GetTypesSafe(Assembly.GetCallingAssembly(), removeIgnored);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual List<Type> GetTypesSafe(Assembly assembly, bool removeIgnored = false)
    {
        List<Type?> types;
        bool removeNulls = false;
        try
        {
            types = [ ..assembly.GetTypes() ];
        }
        catch (FileNotFoundException ex)
        {
            if (LogDebugMessages)
                LogFileNotFoundException(ex, "Accessor.GetTypesSafe", $"Unable to get any types from assembly \"{assembly.FullName}\".");

            return [ ];
        }
        catch (ReflectionTypeLoadException ex)
        {
            if (LogDebugMessages && ex.LoaderExceptions != null)
            {
                for (int i = 0; i < ex.LoaderExceptions.Length; ++i)
                {
                    if (ex.LoaderExceptions[i] is not TypeLoadException tle)
                        continue;

                    LogTypeLoadException(tle, "Accessor.GetTypesSafe", "Skipped type.");
                }
            }
            types = ex.Types == null ? [ ] : [ ..ex.Types ];
            removeNulls = true;
        }

        if (removeNulls)
        {
            if (removeIgnored)
                types.RemoveAll(x => x is null || IsIgnored(x));
            else
                types.RemoveAll(x => x is null);
        }
        else if (removeIgnored)
            types.RemoveAll(IsIgnored!);

        types.Sort(SortTypesByPriorityHandler!);
        return types!;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual List<Type> GetTypesSafe(IEnumerable<Assembly> assmeblies, bool removeIgnored = false)
    {
        List<Type?> types = new List<Type?>();
        bool removeNulls = false;
        foreach (Assembly assembly in assmeblies)
        {
            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (FileNotFoundException ex)
            {
                if (LogDebugMessages)
                    LogFileNotFoundException(ex, "Accessor.GetTypesSafe", $"Unable to get any types from assembly \"{assembly.FullName}\".");
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (LogDebugMessages && ex.LoaderExceptions != null)
                {
                    for (int i = 0; i < ex.LoaderExceptions.Length; ++i)
                    {
                        if (ex.LoaderExceptions[i] is not TypeLoadException tle)
                            continue;

                        LogTypeLoadException(tle, "Accessor.GetTypesSafe", "Skipped type.");
                    }
                }
                if (ex.Types != null)
                    types.AddRange(ex.Types);
                removeNulls = true;
            }
        }

        if (removeNulls)
        {
            if (removeIgnored)
                types.RemoveAll(x => x is null || IsIgnored(x));
            else
                types.RemoveAll(x => x is null);
        }
        else if (removeIgnored)
            types.RemoveAll(IsIgnored!);

        types.Sort(SortTypesByPriorityHandler!);
        return types!;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MethodInfo? GetImplementedMethod(Type type, MethodInfo interfaceMethod)
    {
        if (interfaceMethod is not { DeclaringType.IsInterface: true })
            throw new ArgumentException("Interface method is not defined within an interface.", nameof(interfaceMethod));
        if (!interfaceMethod.DeclaringType.IsAssignableFrom(type))
            throw new ArgumentException("Type does not implement the interface this interface method is defined in.", nameof(interfaceMethod));

        InterfaceMapping mapping = type.GetInterfaceMap(interfaceMethod.DeclaringType!);
        for (int i = 0; i < mapping.InterfaceMethods.Length; ++i)
        {
            MethodInfo explictlyImplementedMethod = mapping.InterfaceMethods[i];
            if (explictlyImplementedMethod.Equals(interfaceMethod))
            {
                return mapping.TargetMethods[i];
            }
        }

        // default implementation
        if (interfaceMethod.IsVirtual)
            return interfaceMethod;

        return null;
    }

    /// <inheritdoc />
    public virtual void GetDelegateSignature<TDelegate>(out Type returnType, out ParameterInfo[] parameters) where TDelegate : Delegate
    {
        returnType = DelegateInfo<TDelegate>.ReturnType;
        parameters = DelegateInfo<TDelegate>.Parameters;
    }

    /// <inheritdoc />
    public virtual void GetDelegateSignature<TDelegate>(out ParameterInfo? returnParameter, out ParameterInfo[] parameters) where TDelegate : Delegate
    {
        returnParameter = DelegateInfo<TDelegate>.ReturnParameter;
        parameters = DelegateInfo<TDelegate>.Parameters;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Type GetReturnType<TDelegate>() where TDelegate : Delegate => DelegateInfo<TDelegate>.ReturnType;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual ParameterInfo? GetReturnParameter<TDelegate>() where TDelegate : Delegate => DelegateInfo<TDelegate>.ReturnParameter;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual ParameterInfo[] GetParameters<TDelegate>() where TDelegate : Delegate => DelegateInfo<TDelegate>.Parameters;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MethodInfo GetInvokeMethod<TDelegate>() where TDelegate : Delegate => DelegateInfo<TDelegate>.InvokeMethod;

    /// <inheritdoc />
    public virtual void GetDelegateSignature(Type delegateType, out Type returnType, out ParameterInfo[] parameters)
    {
        MethodInfo invokeMethod = GetInvokeMethod(delegateType);
        returnType = invokeMethod.ReturnType;
        parameters = invokeMethod.GetParameters();
    }

    /// <inheritdoc />
    public virtual void GetDelegateSignature(Type delegateType, out ParameterInfo? returnParameter, out ParameterInfo[] parameters)
    {
        MethodInfo invokeMethod = GetInvokeMethod(delegateType);
        returnParameter = invokeMethod.ReturnParameter;
        parameters = invokeMethod.GetParameters();
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Type GetReturnType(Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.Name + " is not a delegate type.", nameof(delegateType));

        return delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.ReturnType
               ?? throw new NotSupportedException($"Unable to find Invoke method in delegate {delegateType.Name}.");
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    // ReSharper disable once ReturnTypeCanBeNotNullable
    public virtual ParameterInfo? GetReturnParameter(Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.Name + " is not a delegate type.", nameof(delegateType));

        return (delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new NotSupportedException($"Unable to find Invoke method in delegate {delegateType.Name}."))
            .ReturnParameter;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual ParameterInfo[] GetParameters(Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.Name + " is not a delegate type.", nameof(delegateType));

        return delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetParameters()
               ?? throw new NotSupportedException($"Unable to find Invoke method in delegate {delegateType.Name}.");
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual MethodInfo GetInvokeMethod(Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.Name + " is not a delegate type.", nameof(delegateType));

        return delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
               ?? throw new NotSupportedException($"Unable to find Invoke method in delegate {delegateType.Name}.");
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual Type? GetMemberType(MemberInfo member) => member switch
    {
        MethodInfo a => a.ReturnType,
        FieldInfo a => a.FieldType,
        PropertyInfo a => a.PropertyType,
        ConstructorInfo a => a.DeclaringType,
        EventInfo a => a.EventHandlerType,
        _ => throw new ArgumentException($"Member type {member.GetType().Name} does not have a member type.", nameof(member))
    };

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool GetIsStatic(MemberInfo member) => member switch
    {
        MethodBase a => a.IsStatic,
        FieldInfo a => a.IsStatic,
        PropertyInfo a => a.GetGetMethod(true) is { } getter ? getter.IsStatic : a.GetSetMethod(true) is { IsStatic: true },
        EventInfo a => a.GetAddMethod(true) is { } adder ? adder.IsStatic : (a.GetRemoveMethod(true) is { } remover ? remover.IsStatic : a.GetRaiseMethod(true) is { IsStatic: true }),
        Type a => (a.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class)) == (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class),
        _ => throw new ArgumentException($"Member type {member.GetType().Name} is not static-able.", nameof(member))
    };

    /// <inheritdoc />s>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool ShouldCallvirt(MethodBase method)
    {
        return method is { IsFinal: false, IsVirtual: true } || method.IsAbstract || method is { IsStatic: false, DeclaringType: not { IsValueType: true }, IsFinal: false } || method.DeclaringType is { IsInterface: true };
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool ShouldCallvirtRuntime(MethodBase method)
    {
        return method is { IsFinal: false, IsVirtual: true } || method.IsAbstract || method.DeclaringType is { IsInterface: true };
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual OpCode GetCall(MethodBase method)
    {
        return ShouldCallvirt(method) ? OpCodes.Callvirt : OpCodes.Call;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual OpCode GetCallRuntime(MethodBase method)
    {
        return ShouldCallvirtRuntime(method) ? OpCodes.Callvirt : OpCodes.Call;
    }

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual TElementType[] GetUnderlyingArray<TElementType>(List<TElementType> list) => ListInfo<TElementType>.GetUnderlyingArray(list);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual TElementType[] GetUnderlyingArrayOrCopy<TElementType>(List<TElementType> list) => ListInfo<TElementType>.TryGetUnderlyingArray(list, out TElementType[] array) ? array : list.ToArray();

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual int GetListVersion<TElementType>(List<TElementType> list) => ListInfo<TElementType>.GetListVersion(list);

    /// <inheritdoc />
    public virtual void SetUnderlyingArray<TElementType>(List<TElementType> list, TElementType[] underlyingArray, int size) => ListInfo<TElementType>.SetUnderlyingArray(list, underlyingArray, size);

    /// <inheritdoc />
    public virtual bool TryGetUnderlyingArray<TElementType>(List<TElementType> list, out TElementType[] underlyingArray) => ListInfo<TElementType>.TryGetUnderlyingArray(list, out underlyingArray);

    /// <inheritdoc />
    public virtual bool TryGetListVersion<TElementType>(List<TElementType> list, out int version) => ListInfo<TElementType>.TryGetListVersion(list, out version);

    /// <inheritdoc />
    public virtual bool TrySetUnderlyingArray<TElementType>(List<TElementType> list, TElementType[] underlyingArray, int size) => ListInfo<TElementType>.TrySetUnderlyingArray(list, underlyingArray, size);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool CouldBeAssignedTo(Type actualType, Type queriedType) => actualType.IsAssignableFrom(queriedType) || queriedType.IsAssignableFrom(actualType);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool CouldBeAssignedTo<T>(Type actualType) => CouldBeAssignedTo(actualType, typeof(T));

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public virtual IOpCodeEmitter AsEmitter(ILGenerator generator, bool debuggable = false, bool addBreakpoints = false)
        => debuggable || addBreakpoints
            ? new DebuggableEmitter((ILGeneratorEmitter)generator, null, accessor: this) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : (ILGeneratorEmitter)generator;

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public virtual IOpCodeEmitter AsEmitter(DynamicMethod dynMethod, bool debuggable = false, bool addBreakpoints = false, int streamSize = 64)
        => debuggable || addBreakpoints
            ? new DebuggableEmitter(dynMethod, accessor: this) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : (ILGeneratorEmitter)dynMethod.GetILGenerator(streamSize);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public virtual IOpCodeEmitter AsEmitter(MethodBuilder methodBuilder, bool debuggable = false, bool addBreakpoints = false, int streamSize = 64)
        => debuggable || addBreakpoints
            ? new DebuggableEmitter(methodBuilder, accessor: this) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : (ILGeneratorEmitter)methodBuilder.GetILGenerator(streamSize);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    [StartsEmitter]
    public virtual IOpCodeEmitter AsEmitter(ConstructorBuilder constructorBuilder, bool debuggable = false, bool addBreakpoints = false, int streamSize = 64)
        => debuggable || addBreakpoints
            ? new DebuggableEmitter(constructorBuilder, accessor: this) { DebugLog = debuggable, Breakpointing = addBreakpoints }
            : (ILGeneratorEmitter)constructorBuilder.GetILGenerator(streamSize);

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IVariable AsVariable(FieldInfo field)
        => field.AsVariable(this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IVariable AsVariable(PropertyInfo property)
        => property.AsVariable(this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IVariable AsVariable(MemberInfo member)
        => Variables.AsVariable(member, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IStaticVariable<TMemberType> AsStaticVariable<TMemberType>(FieldInfo field)
        => field.AsStaticVariable<TMemberType>(this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IStaticVariable<TMemberType> AsStaticVariable<TMemberType>(PropertyInfo property)
        => property.AsStaticVariable<TMemberType>(this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IInstanceVariable<TDeclaringType, TMemberType> AsInstanceVariable<TDeclaringType, TMemberType>(FieldInfo field)
        => field.AsInstanceVariable<TDeclaringType, TMemberType>(this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IInstanceVariable<TDeclaringType, TMemberType> AsInstanceVariable<TDeclaringType, TMemberType>(PropertyInfo property)
        => property.AsInstanceVariable<TDeclaringType, TMemberType>(this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool TryFind<TDeclaringType>(string name, out IVariable? variable, bool ignoreCase = false)
        => Variables.TryFind<TDeclaringType>(name, out variable, ignoreCase, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool TryFind(Type declaringType, string name, out IVariable? variable, bool ignoreCase = false)
        => Variables.TryFind(declaringType, name, out variable, ignoreCase, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool TryFindStatic<TDeclaringType, TMemberType>(string name, out IStaticVariable<TMemberType>? variable, bool ignoreCase = false)
        => Variables.TryFindStatic<TDeclaringType, TMemberType>(name, out variable, ignoreCase, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual bool TryFindStatic<TMemberType>(Type declaringType, string name, out IStaticVariable<TMemberType>? variable, bool ignoreCase = false)
        => Variables.TryFindStatic(declaringType, name, out variable, ignoreCase, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IVariable? Find<TDeclaringType>(string name, bool ignoreCase = false)
        => Variables.Find<TDeclaringType>(name, ignoreCase, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IStaticVariable<TMemberType>? FindStatic<TDeclaringType, TMemberType>(string name, bool ignoreCase = false)
        => Variables.FindStatic<TDeclaringType, TMemberType>(name, ignoreCase, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IVariable? Find(Type declaringType, string name, bool ignoreCase = false)
        => Variables.Find(declaringType, name, ignoreCase, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IStaticVariable<TMemberType>? FindStatic<TMemberType>(Type declaringType, string name, bool ignoreCase = false)
        => Variables.FindStatic<TMemberType>(declaringType, name, ignoreCase, this);

    /// <inheritdoc />
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public virtual IInstanceVariable<TDeclaringType, TMemberType>? FindInstance<TDeclaringType, TMemberType>(string name, bool ignoreCase = false)
        => Variables.FindInstance<TDeclaringType, TMemberType>(name, ignoreCase, this);

    /// <summary>
    /// Static caching class with info about a delegate type.
    /// </summary>
    protected class DelegateInfo<TDelegate> where TDelegate : Delegate
    {
        /// <summary>
        /// The compiler-generated .Invoke method for the delegate.
        /// </summary>
        public static MethodInfo InvokeMethod { get; }

        /// <summary>
        /// Array of all parameters in the delegate's invoke method.
        /// </summary>
        public static ParameterInfo[] Parameters { get; }

        /// <summary>
        /// The type the invoke method returns.
        /// </summary>
        public static Type ReturnType { get; }

        /// <summary>
        /// Parameter info for the return parameter.
        /// </summary>
        public static ParameterInfo? ReturnParameter { get; }
        static DelegateInfo()
        {
            InvokeMethod = typeof(TDelegate).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            if (InvokeMethod == null)
                throw new NotSupportedException($"Unable to find Invoke method in delegate {typeof(TDelegate).Name}.");

            Parameters = InvokeMethod.GetParameters();
            ReturnType = InvokeMethod.ReturnType;
            ReturnParameter = InvokeMethod.ReturnParameter;
        }
    }

    /// <summary>
    /// Static caching class with info about a list.
    /// </summary>
    protected class ListInfo<TElementType>
    {
        private static InstanceGetter<List<TElementType>, TElementType[]>? _underlyingArrayGetter;
        private static InstanceSetter<List<TElementType>, TElementType[]>? _underlyingArraySetter;
        private static InstanceGetter<List<TElementType>, int>? _versionGetter;
        private static InstanceSetter<List<TElementType>, int>? _sizeSetter;
        private static bool _checkUndArr;
        private static bool _checkVer;
        private static bool _checkSize;
        private static bool CheckSize()
        {
            if (_checkSize)
            {
                return _sizeSetter != null;
            }

            try
            {
                FieldInfo? field = typeof(List<TElementType>).GetField("_size", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                {
                    _checkSize = true;
                    return false;
                }

                MethodAttributes attr;
                CallingConventions convention;
                if (Type.GetType("Mono.Runtime", false, false) != null)
                {
                    attr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                    convention = CallingConventions.HasThis;
                }
                else
                {
                    attr = MethodAttributes.Public | MethodAttributes.Static;
                    convention = CallingConventions.Standard;
                }

                DynamicMethod method = new DynamicMethod("set__size", attr, convention, typeof(void), [ typeof(List<TElementType>), typeof(int) ], typeof(List<TElementType>), true);
                method.DefineParameter(1, ParameterAttributes.None, "this");
                method.DefineParameter(2, ParameterAttributes.None, "value");

                IOpCodeEmitter il = method.AsEmitter();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);

                _sizeSetter = (InstanceSetter<List<TElementType>, int>)method.CreateDelegate(typeof(InstanceSetter<List<TElementType>, int>));
            }
            catch
            {
                // ignored.
            }
            finally
            {
                _checkSize = true;
            }

            return _sizeSetter != null;
        }
        private static bool CheckUndArr()
        {
            if (_checkUndArr)
            {
                return _underlyingArrayGetter != null && _underlyingArraySetter != null;
            }

            try
            {
                FieldInfo? field = typeof(List<TElementType>).GetField("_items", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                {
                    _checkUndArr = true;
                    return false;
                }

                MethodAttributes attr;
                CallingConventions convention;
                if (Type.GetType("Mono.Runtime", false, false) != null)
                {
                    attr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                    convention = CallingConventions.HasThis;
                }
                else
                {
                    attr = MethodAttributes.Public | MethodAttributes.Static;
                    convention = CallingConventions.Standard;
                }

                DynamicMethod method = new DynamicMethod("get__items", attr, convention, typeof(TElementType[]), [ typeof(List<TElementType>) ], typeof(List<TElementType>), true);
                method.DefineParameter(1, ParameterAttributes.None, "this");

                IOpCodeEmitter il = method.AsEmitter();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);

                _underlyingArrayGetter = (InstanceGetter<List<TElementType>, TElementType[]>)method.CreateDelegate(typeof(InstanceGetter<List<TElementType>, TElementType[]>));

                method = new DynamicMethod("set__items", attr, convention, typeof(void), [ typeof(List<TElementType>), typeof(TElementType[]) ], typeof(List<TElementType>), true);
                method.DefineParameter(1, ParameterAttributes.None, "this");
                method.DefineParameter(2, ParameterAttributes.None, "value");

                il = method.AsEmitter();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);

                _underlyingArraySetter = (InstanceSetter<List<TElementType>, TElementType[]>)method.CreateDelegate(typeof(InstanceSetter<List<TElementType>, TElementType[]>));
            }
            catch
            {
                // ignored.
            }
            finally
            {
                _checkUndArr = true;
            }

            return _underlyingArrayGetter != null && _underlyingArraySetter != null;
        }
        private static bool CheckVer()
        {
            if (_checkVer)
                return _versionGetter != null;

            try
            {
                FieldInfo? field = typeof(List<TElementType>).GetField("_version", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field == null)
                {
                    _checkVer = true;
                    return false;
                }

                MethodAttributes attr;
                CallingConventions convention;
                if (Type.GetType("Mono.Runtime", false, false) != null)
                {
                    attr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
                    convention = CallingConventions.HasThis;
                }
                else
                {
                    attr = MethodAttributes.Public | MethodAttributes.Static;
                    convention = CallingConventions.Standard;
                }

                DynamicMethod method = new DynamicMethod("get__version", attr, convention, typeof(int), [ typeof(List<TElementType>) ], typeof(List<TElementType>), true);
                method.DefineParameter(1, ParameterAttributes.None, "this");

                IOpCodeEmitter il = method.AsEmitter();

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);

                _versionGetter = (InstanceGetter<List<TElementType>, int>)method.CreateDelegate(typeof(InstanceGetter<List<TElementType>, int>));
            }
            catch
            {
                // ignored.
            }
            finally
            {
                _checkVer = true;
            }

            return _versionGetter != null;
        }

        /// <summary>
        /// Gets the underlying array from the list.
        /// </summary>
        /// <returns>Underlying array in the list.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="NotSupportedException">Unable to find '_items' field in list.</exception>
        public static TElementType[] GetUnderlyingArray(List<TElementType> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (_underlyingArrayGetter != null || CheckUndArr())
                return _underlyingArrayGetter!(list);

            throw new NotSupportedException($"Unable to find '_items' in list of {typeof(TElementType).Name}.");
        }

        /// <summary>
        /// Gets the updating version number from the list.
        /// </summary>
        /// <returns>Version number in the list.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="NotSupportedException">Unable to find '_version' field in list.</exception>
        public static int GetListVersion(List<TElementType> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (_versionGetter != null || CheckVer())
                return _versionGetter!(list);

            throw new NotSupportedException($"Unable to find '_version' in list of {typeof(TElementType).Name}.");
        }

        /// <summary>
        /// Sets the underlying array and size of the list.
        /// </summary>
        public static void SetUnderlyingArray(List<TElementType> list, TElementType[] underlyingArray, int size)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (_sizeSetter == null && !CheckSize())
                throw new NotSupportedException($"Unable to find '_size' in list of {typeof(TElementType).Name}.");

            if (_underlyingArraySetter == null && !CheckUndArr())
                throw new NotSupportedException($"Unable to find '_items' in list of {typeof(TElementType).Name}.");

            _sizeSetter!(list, size);
            _underlyingArraySetter!(list, underlyingArray);
        }

        /// <summary>
        /// Gets the underlying array from the list.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static bool TryGetUnderlyingArray(List<TElementType> list, out TElementType[] underlyingArray)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (_underlyingArrayGetter == null && !CheckUndArr())
            {
                underlyingArray = null!;
                return false;
            }

            underlyingArray = _underlyingArrayGetter!(list);
            return true;
        }

        /// <summary>
        /// Gets the updating version number from the list.
        /// </summary>
        public static bool TryGetListVersion(List<TElementType> list, out int version)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (_versionGetter == null && !CheckVer())
            {
                version = 0;
                return false;
            }

            version = _versionGetter!(list);
            return true;
        }

        /// <summary>
        /// Sets the underlying array and size of the list.
        /// </summary>
        public static bool TrySetUnderlyingArray(List<TElementType> list, TElementType[] underlyingArray, int size)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (_sizeSetter == null && !CheckSize() || _underlyingArraySetter == null && !CheckUndArr())
                return false;

            _sizeSetter!(list, size);
            _underlyingArraySetter!(list, underlyingArray);
            return true;
        }
    }
    internal static void CheckExceptionConstructors()
    {
        if (!_castExCtorCalc)
        {
            _castExCtorCalc = true;
            CastExCtor = typeof(InvalidCastException).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(string) }, null)!;
        }
        if (!_nreExCtorCalc)
        {
            _nreExCtorCalc = true;
            NreExCtor = typeof(NullReferenceException).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
        }
    }
    internal static Type? TryGetElementType(Type type)
    {
        return type.HasElementType ? type.GetElementType() : null;
    }
}
