using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Reflection utilities for accessing private or internal members.
/// </summary>
public static class Accessor
{
    private static bool _isMonoCached;
    private static bool _isMono;
    private static IReflectionToolsLogger _logger = new ConsoleReflectionToolsLogger();

    private static Assembly? _mscorlibAssembly;

    internal static ConstructorInfo? CastExCtor;
    internal static ConstructorInfo? NreExCtor;

    internal static Type[]? FuncTypes;
    internal static Type[]? ActionTypes;
#if NET471_OR_GREATER || NET || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1
    private static Type? _readonlyAttribute;
#endif
    private static Type? _ignoreAttribute;
    private static Type? _priorityAttribute;
    private static bool _castExCtorCalc;
    private static bool _nreExCtorCalc;

    /// <summary>
    /// Should <see cref="Logger"/> log generated IL code (as debug messages)?
    /// </summary>
    public static bool LogILTraceMessages { get; set; }

    /// <summary>
    /// Should <see cref="Logger"/> log debug messages?
    /// </summary>
    public static bool LogDebugMessages { get; set; }

    /// <summary>
    /// Should <see cref="Logger"/> log info messages?
    /// </summary>
    public static bool LogInfoMessages { get; set; }

    /// <summary>
    /// Should <see cref="Logger"/> log warning messages?
    /// </summary>
    public static bool LogWarningMessages { get; set; }

    /// <summary>
    /// Should <see cref="Logger"/> log error messages?
    /// </summary>
    public static bool LogErrorMessages { get; set; }

    /// <summary>
    /// Logging IO for all methods in this library.
    /// <para>Assigning a value to this will dispose the previous value if needed.</para>
    /// </summary>
    /// <remarks>Default value is an instance of <see cref="ConsoleReflectionToolsLogger"/>, which outputs to <see cref="Console"/>.</remarks>
    public static IReflectionToolsLogger Logger
    {
        get => _logger;
        set
        {
            IReflectionToolsLogger old = Interlocked.Exchange(ref _logger, value);
            if (!ReferenceEquals(old, value) && old is IDisposable disp)
                disp.Dispose();

            if (LogInfoMessages)
                value?.LogInfo("Accessor.Logger", $"Logger updated: {value.GetType().FullName}.");
        }
    }

    /// <summary>
    /// System primary assembly.
    /// </summary>
    /// <remarks>Lazily cached.</remarks>
    /// <exception cref="TypeLoadException"/>
    public static Assembly MSCoreLib => _mscorlibAssembly ??= typeof(object).Assembly;

    /// <summary>
    /// Whether or not the <c>Mono.Runtime</c> class is available. Indicates if the current runtime is Mono.
    /// </summary>
    public static bool IsMono
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

    /// <summary>
    /// Generates a dynamic method that sets an instance field value. For value types use <see cref="GenerateInstanceSetter{TValue}"/> instead.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static InstanceSetter<TInstance, TValue>? GenerateInstanceSetter<TInstance, TValue>(string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceSetter";
        if (typeof(TInstance).IsValueType)
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance setter for {typeof(TInstance).FullName}.{fieldName}, you must pass structs ({typeof(TInstance).Name}) as a boxed object.");

            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to create instance setter for {typeof(TInstance).FullName}.{fieldName}, you must pass structs ({typeof(TInstance).Name}) as a boxed object.");
            return null;
        }

        FieldInfo? field = typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field ??= typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (field is null || field.IsStatic || (!field.FieldType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching field: {typeof(TInstance).FullName}.{fieldName}.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to find matching field {typeof(TInstance).FullName}.{fieldName}.");
            return null;
        }

        fieldName = field.Name;

        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, convention, typeof(void), new Type[] { typeof(TInstance), typeof(TValue) }, typeof(TInstance), true);
            method.DefineParameter(1, ParameterAttributes.None, "this");
            method.DefineParameter(2, ParameterAttributes.None, "value");
            ILGenerator il = method.GetILGenerator();
            bool logIl = LogILTraceMessages;
            if (logIl)
                Logger.LogDebug(source, $"IL: Generating instance setter for {field.DeclaringType!.FullName}.{field.Name}:");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            Label? typeLbl = null;
            if (logIl)
            {
                Logger.LogDebug(source, "IL:  ldarg.0");
                Logger.LogDebug(source, "IL:  ldarg.1");
            }

            if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, field.FieldType);
                if (logIl)
                    Logger.LogDebug(source, "IL:  unbox.any <" + field.FieldType.FullName + ">");
            }
            else if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, typeof(TValue));
                if (logIl)
                    Logger.LogDebug(source, "IL:  box <" + typeof(TValue).FullName + ">");
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
                string errMsg = "Invalid argument type passed to setter for " + fieldName + ". Expected " + field.FieldType.FullName + ".";
                if (CastExCtor != null)
                    il.Emit(OpCodes.Ldstr, errMsg);
                il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                il.Emit(OpCodes.Throw);
                if (logIl)
                {
                    Logger.LogDebug(source, $"IL:  isinst <{field.FieldType.FullName}>");
                    Logger.LogDebug(source, "IL:  dup");
                    Logger.LogDebug(source, "IL:  brtrue.s <lbl_0>");
                    Logger.LogDebug(source, "IL:  pop");
                    Logger.LogDebug(source, "IL:  ldarg.1");
                    Logger.LogDebug(source, "IL:  dup");
                    Logger.LogDebug(source, "IL:  brfalse.s <lbl_0>");
                    Logger.LogDebug(source, "IL:  pop");
                    Logger.LogDebug(source, "IL:  pop");
                    if (CastExCtor != null)
                        Logger.LogDebug(source, $"IL:  ldstr \"{errMsg}\"");
                    Logger.LogDebug(source, $"IL:  newobj <{(CastExCtor?.DeclaringType ?? NreExCtor!.DeclaringType!).FullName}(System.String)>");
                    Logger.LogDebug(source, "IL:  throw");
                }
            }

            if (typeLbl.HasValue)
            {
                il.MarkLabel(typeLbl.Value);
                if (logIl)
                    Logger.LogDebug(source, "IL: lbl_0:");
            }

            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            if (logIl)
            {
                Logger.LogDebug(source, $"IL:  stfld <{field.DeclaringType!.FullName}.{fieldName}>");
                Logger.LogDebug(source, "IL:  ret");
            }
            InstanceSetter<TInstance, TValue> setter = (InstanceSetter<TInstance, TValue>)method.CreateDelegate(typeof(InstanceSetter<TInstance, TValue>));

            if (LogDebugMessages || logIl)
                Logger.LogDebug(source, $"Created dynamic method instance setter for {field.DeclaringType!.Name}.{fieldName}.");
            return setter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for {field.DeclaringType!.FullName}.{fieldName}.", ex);
            if (LogErrorMessages)
                Logger.LogError(source, ex, $"Error generating instance getter for {field.DeclaringType!.FullName}.{fieldName}.");
            return null;
        }
    }

    /// <summary>
    /// Generates a dynamic method that gets an instance field value. Works for reference or value types.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static InstanceGetter<TInstance, TValue>? GenerateInstanceGetter<TInstance, TValue>(string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceGetter";
        FieldInfo? field = typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field ??= typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (field is null || field.IsStatic || (!typeof(TValue).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching field: {typeof(TInstance).FullName}.{fieldName}.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to find matching property {typeof(TInstance).FullName}.{fieldName}.");
            return null;
        }

        fieldName = field.Name;

        try
        {
            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, convention, typeof(TValue), new Type[] { typeof(TInstance) }, typeof(TInstance), true);
            method.DefineParameter(1, ParameterAttributes.None, "this");
            ILGenerator il = method.GetILGenerator();
            bool logIl = LogILTraceMessages;
            if (logIl)
                Logger.LogDebug(source, $"IL: Generating instance getter for {field.DeclaringType!.FullName}.{field.Name}:");
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            if (logIl)
            {
                Logger.LogDebug(source, "IL:  ldarg.0");
                Logger.LogDebug(source, $"IL:  ldfld <{field.DeclaringType!.FullName}.{field.Name}>");
            }

            if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, typeof(TValue));
                if (logIl)
                    Logger.LogDebug(source, "IL:  unbox.any <" + typeof(TValue).FullName + ">");
            }
            else if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
                if (logIl)
                    Logger.LogDebug(source, "IL:  box <" + field.FieldType.FullName + ">");
            }

            il.Emit(OpCodes.Ret);
            if (logIl)
                Logger.LogDebug(source, "IL:  ret");
            
            InstanceGetter<TInstance, TValue> getter = (InstanceGetter<TInstance, TValue>)method.CreateDelegate(typeof(InstanceGetter<TInstance, TValue>));

            if (LogDebugMessages || logIl)
                Logger.LogDebug(source, $"Created dynamic method instance getter for {typeof(TInstance).Name}.{fieldName}.");
            return getter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for {typeof(TInstance).FullName}.{fieldName}.", ex);
            if (LogErrorMessages)
                Logger.LogError(source, ex, $"Error generating instance getter for {typeof(TInstance).FullName}.{fieldName}.");
            return null;
        }
    }

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
    [Pure]
    public static InstanceSetter<object, TValue>? GenerateInstanceSetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceSetter";
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance setter for <unknown>.{fieldName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Error generating instance setter for <unknown>.{fieldName}. Declaring type not found.");
            return null;
        }
        FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field ??= declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (field is null || field.IsStatic || (!field.FieldType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching field: {declaringType.FullName}.{fieldName}.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to find matching property {declaringType.FullName}.{fieldName}.");
            return null;
        }

        fieldName = field.Name;

        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, convention, typeof(void), new Type[] { typeof(object), typeof(TValue) }, declaringType, true);
            method.DefineParameter(1, ParameterAttributes.None, "this");
            method.DefineParameter(2, ParameterAttributes.None, "value");
            bool logIl = LogILTraceMessages;
            if (logIl)
                Logger.LogDebug(source, $"IL: Generating instance setter for {field.DeclaringType!.FullName}.{field.Name}:");
            ILGenerator il = method.GetILGenerator();
            Label lbl = il.DefineLabel();
            Label? typeLbl = null;
            il.Emit(OpCodes.Ldarg_0);
            if (logIl)
                Logger.LogDebug(source, "IL:  ldarg.0");

            bool isValueType = declaringType.IsValueType;
            bool lbl1Exists = false;
            if (CastExCtor != null || !isValueType && NreExCtor != null)
            {
                Label lbl2 = il.DefineLabel();
                lbl1Exists = true;
                il.Emit(OpCodes.Isinst, declaringType);
                if (!isValueType)
                    il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue_S, lbl);
                if (!isValueType)
                    il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Brfalse_S, lbl2);

                string castError = $"Invalid instance type passed to getter for {fieldName}. Expected {declaringType.FullName}.";
                if (CastExCtor != null)
                    il.Emit(OpCodes.Ldstr, castError);
                il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                il.Emit(OpCodes.Throw);
                il.MarkLabel(lbl2);
                if (logIl)
                {
                    Logger.LogDebug(source, $"IL:  isinst <{declaringType.FullName}>");
                    if (!isValueType)
                        Logger.LogDebug(source, "IL:  dup");
                    Logger.LogDebug(source, "IL:  brtrue.s <lbl_0>");
                    if (!isValueType)
                        Logger.LogDebug(source, "IL:  pop");
                    Logger.LogDebug(source, "IL:  ldarg.0");
                    Logger.LogDebug(source, "IL:  brfalse.s <lbl_1>");
                    if (CastExCtor != null)
                        Logger.LogDebug(source, $"IL:  ldstr \"{castError}\"");
                    Logger.LogDebug(source, $"IL:  newobj <{(CastExCtor?.DeclaringType ?? NreExCtor!.DeclaringType!).FullName}(System.String)>");
                    Logger.LogDebug(source, "IL:  throw");
                    Logger.LogDebug(source, "IL: lbl_1:");
                }
                ConstructorInfo ctor = NreExCtor ?? CastExCtor!;
                if (ctor == CastExCtor)
                {
                    string nullError = $"Null passed to getter for {fieldName}. Expected {declaringType.FullName}.";
                    il.Emit(OpCodes.Ldstr, nullError);
                    if (logIl)
                        Logger.LogDebug(source, $"IL:  ldstr \"{nullError}\"");
                }
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Throw);
                if (logIl)
                {
                    Logger.LogDebug(source, $"IL:  newobj <{(NreExCtor?.DeclaringType ?? CastExCtor!.DeclaringType!).FullName}({(ctor == CastExCtor ? "System.String" : string.Empty)})>");
                    Logger.LogDebug(source, "IL:  throw");
                }
            }
            il.MarkLabel(lbl);
            if (logIl)
                Logger.LogDebug(source, "IL: lbl_0:");
            if (isValueType)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Unbox, declaringType);
                il.Emit(OpCodes.Ldflda, field);
                il.Emit(OpCodes.Ldarg_1);

                if (logIl)
                {
                    Logger.LogDebug(source, "IL:  ldarg.0");
                    Logger.LogDebug(source, $"IL:  unbox <{declaringType.FullName}>");
                    Logger.LogDebug(source, $"IL:  ldflda <{field.DeclaringType!.FullName}.{field.Name}>");
                    Logger.LogDebug(source, "IL:  ldarg.1");
                }

                if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, field.FieldType);
                    if (logIl)
                        Logger.LogDebug(source, $"IL:  unbox.any <{field.FieldType.FullName}>");
                }
                else if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, typeof(TValue));
                    if (logIl)
                        Logger.LogDebug(source, $"IL:  box <{typeof(TValue).FullName}>");
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
                    string errMsg = "Invalid argument type passed to setter for " + fieldName + ". Expected " + field.FieldType.FullName + ".";
                    if (CastExCtor != null)
                        il.Emit(OpCodes.Ldstr, errMsg);
                    il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                    il.Emit(OpCodes.Throw);
                    if (logIl)
                    {
                        Logger.LogDebug(source, $"IL:  isinst <{field.FieldType.FullName}>");
                        Logger.LogDebug(source, "IL:  dup");
                        Logger.LogDebug(source, $"IL:  brtrue.s <lbl_{(lbl1Exists ? "2" : "1")}>");
                        Logger.LogDebug(source, "IL:  pop");
                        Logger.LogDebug(source, "IL:  ldarg.1");
                        Logger.LogDebug(source, "IL:  dup");
                        Logger.LogDebug(source, $"IL:  brfalse.s <lbl_{(lbl1Exists ? "2" : "1")}>");
                        Logger.LogDebug(source, "IL:  pop");
                        Logger.LogDebug(source, "IL:  pop");
                        if (CastExCtor != null)
                            Logger.LogDebug(source, $"IL:  ldstr \"{errMsg}\"");
                        Logger.LogDebug(source, $"IL:  newobj <{(CastExCtor?.DeclaringType ?? NreExCtor!.DeclaringType!).FullName}(System.String)>");
                        Logger.LogDebug(source, "IL:  throw");
                    }
                }

                if (typeLbl.HasValue)
                {
                    il.MarkLabel(typeLbl.Value);
                    if (logIl)
                        Logger.LogDebug(source, $"IL: lbl_{(lbl1Exists ? "2" : "1")}:");
                }

                il.Emit(OpCodes.Stobj, field.FieldType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  stobj <{field.FieldType.FullName}>");
            }
            else
            {
                il.Emit(OpCodes.Ldarg_1);
                if (logIl)
                    Logger.LogDebug(source, "IL:  ldarg.1");

                if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, field.FieldType);
                    if (logIl)
                        Logger.LogDebug(source, $"IL:  unbox.any <{field.FieldType.FullName}>");
                }
                else if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Box, typeof(TValue));
                    if (logIl)
                        Logger.LogDebug(source, $"IL:  box <{typeof(TValue).FullName}>");
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
                    string errMsg = "Invalid argument type passed to setter for " + fieldName + ". Expected " + field.FieldType.FullName + ".";
                    if (CastExCtor != null)
                        il.Emit(OpCodes.Ldstr, errMsg);
                    il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                    il.Emit(OpCodes.Throw);
                    if (logIl)
                    {
                        Logger.LogDebug(source, $"IL:  isinst <{field.FieldType.FullName}>");
                        Logger.LogDebug(source, "IL:  dup");
                        Logger.LogDebug(source, $"IL:  brtrue.s <lbl_{(lbl1Exists ? "2" : "1")}>");
                        Logger.LogDebug(source, "IL:  pop");
                        Logger.LogDebug(source, "IL:  ldarg.1");
                        Logger.LogDebug(source, "IL:  dup");
                        Logger.LogDebug(source, $"IL:  brfalse.s <lbl_{(lbl1Exists ? "2" : "1")}>");
                        Logger.LogDebug(source, "IL:  pop");
                        Logger.LogDebug(source, "IL:  pop");
                        if (CastExCtor != null)
                            Logger.LogDebug(source, $"IL:  ldstr \"{errMsg}\"");
                        Logger.LogDebug(source, $"IL:  newobj <{(CastExCtor?.DeclaringType ?? NreExCtor!.DeclaringType!).FullName}(System.String)>");
                        Logger.LogDebug(source, "IL:  throw");
                    }
                }

                if (typeLbl.HasValue)
                {
                    il.MarkLabel(typeLbl.Value);
                    if (logIl)
                        Logger.LogDebug(source, $"IL: lbl_{(lbl1Exists ? "2" : "1")}:");
                }

                il.Emit(OpCodes.Stfld, field);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  stobj <{field.DeclaringType!.FullName}.{field.Name}>");
            }
            il.Emit(OpCodes.Ret);
            if (logIl)
                Logger.LogDebug(source, "IL:  ret");
            InstanceSetter<object, TValue> setter = (InstanceSetter<object, TValue>)method.CreateDelegate(typeof(InstanceSetter<object, TValue>));
            if (LogDebugMessages || logIl)
                Logger.LogDebug(source, $"Created dynamic method instance setter for {declaringType.Name}.{fieldName}.");
            return setter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance setter for {declaringType.FullName}.{fieldName}.", ex);
            if (LogErrorMessages)
                Logger.LogError(source, ex, $"Error generating instance setter for {declaringType.FullName}.{fieldName}.");
            return null;
        }
    }

    /// <summary>
    /// Generates a dynamic method that gets an instance field value. Works for reference or value types.
    /// </summary>
    /// <param name="declaringType">Declaring type of the field.</param>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static InstanceGetter<object, TValue>? GenerateInstanceGetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateInstanceGetter";
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for <unknown>.{fieldName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Error generating instance getter for <unknown>.{fieldName}. Declaring type not found.");
            return null;
        }
        FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field ??= declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
        if (field is null || field.IsStatic || (!typeof(TValue).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching field: {declaringType.FullName}.{fieldName}.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to find matching property {declaringType.FullName}.{fieldName}.");
            return null;
        }

        fieldName = field.Name;

        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(false, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, convention, typeof(TValue), new Type[] { typeof(object) }, declaringType, true);
            bool logIl = LogILTraceMessages;
            if (logIl)
                Logger.LogDebug(source, $"IL: Generating instance getter for {field.DeclaringType!.FullName}.{field.Name}:");
            method.DefineParameter(1, ParameterAttributes.None, "this");
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            Label? lbl = null;
            if (logIl)
                Logger.LogDebug(source, "IL:  ldarg.0");

            bool isValueType = declaringType.IsValueType;
            if (CastExCtor != null || !isValueType && NreExCtor != null)
            {
                lbl = il.DefineLabel();
                Label lbl2 = il.DefineLabel();
                il.Emit(OpCodes.Isinst, declaringType);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Brtrue_S, lbl.Value);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Brfalse_S, lbl2);
                string castError = "Invalid instance type passed to getter for " + fieldName + ". Expected " + declaringType.FullName + ".";
                if (CastExCtor != null)
                    il.Emit(OpCodes.Ldstr, castError);
                il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                il.Emit(OpCodes.Throw);
                il.MarkLabel(lbl2);
                if (logIl)
                {
                    Logger.LogDebug(source, $"IL:  isinst <{declaringType.FullName}>");
                    Logger.LogDebug(source, "IL:  dup");
                    Logger.LogDebug(source, "IL:  brtrue.s <lbl_0>");
                    Logger.LogDebug(source, "IL:  pop");
                    Logger.LogDebug(source, "IL:  ldarg.0");
                    Logger.LogDebug(source, "IL:  brfalse.s <lbl_1>");
                    if (CastExCtor != null)
                        Logger.LogDebug(source, $"IL:  ldstr \"{castError}\"");
                    Logger.LogDebug(source, $"IL:  newobj <{(CastExCtor?.DeclaringType ?? NreExCtor!.DeclaringType!).FullName}(System.String)>");
                    Logger.LogDebug(source, "IL:  throw");
                    Logger.LogDebug(source, "IL: lbl_1:");
                }
                ConstructorInfo ctor = NreExCtor ?? CastExCtor!;
                if (ctor == CastExCtor)
                {
                    string nullError = $"Null passed to getter for {fieldName}. Expected {declaringType.FullName}.";
                    il.Emit(OpCodes.Ldstr, nullError);
                    if (logIl)
                        Logger.LogDebug(source, $"IL:  ldstr \"{nullError}\"");
                }
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Throw);
                if (logIl)
                {
                    Logger.LogDebug(source, $"IL:  newobj <{(NreExCtor?.DeclaringType ?? CastExCtor!.DeclaringType!).FullName}({(ctor == CastExCtor ? "System.String" : string.Empty)})>");
                    Logger.LogDebug(source, "IL:  throw");
                }
            }
            if (lbl.HasValue)
            {
                il.MarkLabel(lbl.Value);
                if (logIl)
                    Logger.LogDebug(source, "IL: lbl_0:");
            }
            if (isValueType)
            {
                il.Emit(OpCodes.Unbox, declaringType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  unbox <{declaringType.FullName}>");
            }
            il.Emit(OpCodes.Ldfld, field);
            if (logIl)
                Logger.LogDebug(source, $"IL:  ldfld <{field.DeclaringType!.FullName}.{field.Name}>");

            if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, typeof(TValue));
                if (logIl)
                    Logger.LogDebug(source, $"IL:  unbox.any <{typeof(TValue).FullName}>");
            }
            else if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  box <{field.FieldType.FullName}>");
            }

            il.Emit(OpCodes.Ret);
            if (logIl)
                Logger.LogDebug(source, "IL:  ret");
            InstanceGetter<object, TValue> getter = (InstanceGetter<object, TValue>)method.CreateDelegate(typeof(InstanceGetter<object, TValue>));
            if (LogDebugMessages || logIl)
                Logger.LogDebug(source, $"Created dynamic method instance getter for {declaringType.Name}.{fieldName}.");
            return getter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for {declaringType.FullName}.{fieldName}.", ex);
            if (LogErrorMessages)
                Logger.LogError(source, ex, $"Error generating instance getter for {declaringType.FullName}.{fieldName}.");
            return null;
        }
    }

    /// <summary>
    /// Generates a delegate that sets an instance property value. For value types use <see cref="GenerateInstancePropertySetter{TValue}"/> instead.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static InstanceSetter<TInstance, TValue>? GenerateInstancePropertySetter<TInstance, TValue>(string propertyName, bool throwOnError = false)
    {
        if (typeof(TInstance).IsValueType)
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance setter for {typeof(TInstance).FullName}.{propertyName}, you must pass structs ({typeof(TInstance).Name}) as a boxed object.");

            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateInstancePropertySetter", null, $"Unable to create instance setter for {typeof(TInstance).FullName}.{propertyName}, you must pass structs ({typeof(TInstance).Name}) as a boxed object.");
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
                    throw new Exception($"Unable to find matching property: {typeof(TInstance).FullName}.{propertyName} with a setter.");
                if (LogErrorMessages)
                    Logger.LogError("Accessor.GenerateInstancePropertySetter", null, $"Unable to find matching property {typeof(TInstance).FullName}.{propertyName} with a setter.");
                return null;
            }
        }

        return GenerateInstanceCaller<InstanceSetter<TInstance, TValue>>(setter, throwOnError, true);
    }

    /// <summary>
    /// Generates a delegate that gets an instance property value. Works for reference or value types.
    /// </summary>
    /// <typeparam name="TInstance">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static InstanceGetter<TInstance, TValue>? GenerateInstancePropertyGetter<TInstance, TValue>(string propertyName, bool throwOnError = false)
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
                    throw new Exception($"Unable to find matching property: {typeof(TInstance).FullName}.{propertyName} with a getter.");
                if (LogErrorMessages)
                    Logger.LogError("Accessor.GenerateInstancePropertyGetter", null, $"Unable to find matching property {typeof(TInstance).FullName}.{propertyName} with a getter.");
                return null;
            }
        }

        return GenerateInstanceCaller<InstanceGetter<TInstance, TValue>>(getter, throwOnError, true);
    }

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
    [Pure]
    public static InstanceSetter<object, TValue>? GenerateInstancePropertySetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance setter for <unknown>.{propertyName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateInstancePropertySetter", null, $"Error generating instance setter for <unknown>.{propertyName}. Declaring type not found.");
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
                    throw new Exception($"Unable to find matching property: {declaringType.FullName}.{propertyName} with a setter.");
                if (LogErrorMessages)
                    Logger.LogError("Accessor.GenerateInstancePropertySetter", null, $"Unable to find matching property {declaringType.FullName}.{propertyName} with a setter.");
                return null;
            }
        }

        return GenerateInstanceCaller<InstanceSetter<object, TValue>>(setter, throwOnError, allowUnsafeTypeBinding);
    }

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
    [Pure]
    public static InstanceGetter<object, TValue>? GenerateInstancePropertyGetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating instance getter for <unknown>.{propertyName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateInstancePropertyGetter", null, $"Error generating instance getter for <unknown>.{propertyName}. Declaring type not found.");
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
                    throw new Exception($"Unable to find matching property: {declaringType.FullName}.{propertyName} with a getter.");
                if (LogErrorMessages)
                    Logger.LogError("Accessor.GenerateInstancePropertyGetter", null, $"Unable to find matching property {declaringType.FullName}.{propertyName} with a getter.");
                return null;
            }
        }

        return GenerateInstanceCaller<InstanceGetter<object, TValue>>(getter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a dynamic method that sets a static field value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of the field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static StaticSetter<TValue>? GenerateStaticSetter<TDeclaringType, TValue>(string fieldName, bool throwOnError = false)
        => GenerateStaticSetter<TValue>(typeof(TDeclaringType), fieldName, throwOnError);

    /// <summary>
    /// Generates a dynamic method that gets a static field value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the field.</typeparam>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of the field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static StaticGetter<TValue>? GenerateStaticGetter<TDeclaringType, TValue>(string fieldName, bool throwOnError = false)
        => GenerateStaticGetter<TValue>(typeof(TDeclaringType), fieldName, throwOnError);

    /// <summary>
    /// Generates a dynamic method that sets a static field value.
    /// </summary>
    /// <param name="declaringType">Declaring type of the field.</param>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of the field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static StaticSetter<TValue>? GenerateStaticSetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticSetter";
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating static setter for <unknown>.{fieldName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Error generating static setter for <unknown>.{fieldName}. Declaring type not found.");
            return null;
        }
        FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        if (field is null || !field.IsStatic || (!field.FieldType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching field: {declaringType.FullName}.{fieldName}.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to find matching field {declaringType.FullName}.{fieldName}.");
            return null;
        }

        fieldName = field.Name;
#if NET || NETCOREAPP
        if (field.IsInitOnly && field.FieldType.IsValueType)
        {
            if (throwOnError)
                throw new NotSupportedException($"Field {declaringType.FullName}.{fieldName} is a static readonly value type field, which is not settable in the current runtime.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Field {declaringType.FullName}.{fieldName} is a static readonly value type field, which is not settable in the current runtime.");
            return null;
        }
#endif
        try
        {
            CheckExceptionConstructors();

            GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("set_" + fieldName, attr, convention, typeof(void), new Type[] { typeof(TValue) }, declaringType, true);
            bool logIl = LogILTraceMessages;
            if (logIl)
                Logger.LogDebug(source, $"IL: Generating static setter for {field.DeclaringType!.FullName}.{fieldName}:");
            method.DefineParameter(1, ParameterAttributes.None, "value");
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (logIl)
                Logger.LogDebug(source, "IL:  ldarg.0");

            Label? lbl = null;

            if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, field.FieldType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  unbox.any <{field.FieldType.FullName}>");
            }
            else if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, typeof(TValue));
                if (logIl)
                    Logger.LogDebug(source, $"IL:  box <{typeof(TValue).FullName}>");
            }
            else if (!field.FieldType.IsAssignableFrom(typeof(TValue)) && (CastExCtor != null || NreExCtor != null))
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
                string errMsg = "Invalid argument type passed to getter for " + fieldName + ". Expected " + field.FieldType.FullName + ".";
                if (CastExCtor != null)
                    il.Emit(OpCodes.Ldstr, errMsg);
                il.Emit(OpCodes.Newobj, CastExCtor ?? NreExCtor!);
                il.Emit(OpCodes.Throw);
                if (logIl)
                {
                    Logger.LogDebug(source, $"IL:  isinst <{field.FieldType.FullName}>");
                    Logger.LogDebug(source, "IL:  dup");
                    Logger.LogDebug(source, "IL:  brtrue.s <lbl_0>");
                    Logger.LogDebug(source, "IL:  pop");
                    Logger.LogDebug(source, "IL:  ldarg.0");
                    Logger.LogDebug(source, "IL:  dup");
                    Logger.LogDebug(source, "IL:  brfalse.s <lbl_0>");
                    Logger.LogDebug(source, "IL:  pop");
                    if (CastExCtor != null)
                        Logger.LogDebug(source, $"IL:  ldstr \"{errMsg}\"");
                    Logger.LogDebug(source, $"IL:  newobj <{(CastExCtor?.DeclaringType ?? NreExCtor!.DeclaringType!).FullName}(System.String)>");
                    Logger.LogDebug(source, "IL:  throw");
                }
            }

            if (lbl.HasValue)
            {
                il.MarkLabel(lbl.Value);
                if (logIl)
                    Logger.LogDebug(source, "IL: lbl_0:");
            }

            il.Emit(OpCodes.Stsfld, field);
            il.Emit(OpCodes.Ret);
            if (logIl)
            {
                Logger.LogDebug(source, $"IL:  stsfld <{field.DeclaringType!.FullName}.{fieldName}");
                Logger.LogDebug(source, "IL:  ret");
            }
            StaticSetter<TValue> setter = (StaticSetter<TValue>)method.CreateDelegate(typeof(StaticSetter<TValue>));
            if (LogDebugMessages || logIl)
                Logger.LogDebug(source, $"Created dynamic method static setter for {declaringType.Name}.{fieldName}.");
            return setter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating static setter for {declaringType.FullName}.{fieldName}.", ex);
            if (LogErrorMessages)
                Logger.LogError(source, ex, $"Error generating static setter for {declaringType.FullName}.{fieldName}.");
            return null;
        }
    }

    /// <summary>
    /// Generates a dynamic method that gets a static field value.
    /// </summary>
    /// <param name="declaringType">Declaring type of the field.</param>
    /// <typeparam name="TValue">Field return type.</typeparam>
    /// <param name="fieldName">Name of the field that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static StaticGetter<TValue>? GenerateStaticGetter<TValue>(Type declaringType, string fieldName, bool throwOnError = false)
    {
        const string source = "Accessor.GenerateStaticGetter";
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating static getter for <unknown>.{fieldName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Error generating static getter for <unknown>.{fieldName}. Declaring type not found.");
            return null;
        }
        FieldInfo? field = declaringType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

        if (field is null || !field.IsStatic || (!typeof(TValue).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching field: {declaringType.FullName}.{fieldName}.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to find matching property {declaringType.FullName}.{fieldName}.");
            return null;
        }

        fieldName = field.Name;

        try
        {
            GetDynamicMethodFlags(true, out MethodAttributes attr, out CallingConventions convention);
            DynamicMethod method = new DynamicMethod("get_" + fieldName, attr, convention, typeof(TValue), Type.EmptyTypes, declaringType, true);
            bool logIl = LogILTraceMessages;
            if (logIl)
                Logger.LogDebug(source, $"IL: Generating static getter for {field.DeclaringType!.FullName}.{fieldName}:");
            ILGenerator il = method.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, field);
            if (logIl)
                Logger.LogDebug(source, "IL:  ldsfld");

            if (typeof(TValue).IsValueType && !field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Unbox_Any, typeof(TValue));
                if (logIl)
                    Logger.LogDebug(source, $"IL:  unbox.any <{typeof(TValue).FullName}>");
            }
            else if (!typeof(TValue).IsValueType && field.FieldType.IsValueType)
            {
                il.Emit(OpCodes.Box, field.FieldType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  box <{field.FieldType.FullName}>");
            }

            if (logIl)
                Logger.LogDebug(source, "IL:  ret");
            il.Emit(OpCodes.Ret);
            StaticGetter<TValue> getter = (StaticGetter<TValue>)method.CreateDelegate(typeof(StaticGetter<TValue>));
            if (LogDebugMessages || logIl)
                Logger.LogDebug(source, $"Created dynamic method static getter for {declaringType.Name}.{fieldName}.");
            return getter;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Error generating static getter for {declaringType.FullName}.{fieldName}.", ex);
            if (LogErrorMessages)
                Logger.LogError(source, ex, $"Error generating static getter for {declaringType.FullName}.{fieldName}.");
            return null;
        }
    }

    /// <summary>
    /// Generates a delegate or dynamic method that sets a static property value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static StaticSetter<TValue>? GenerateStaticPropertySetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false)
        => GenerateStaticPropertySetter<TValue>(typeof(TDeclaringType), propertyName, throwOnError, false);

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
    [Pure]
    // ReSharper disable once MethodOverloadWithOptionalParameter
    public static StaticSetter<TValue>? GenerateStaticPropertySetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
        => GenerateStaticPropertySetter<TValue>(typeof(TDeclaringType), propertyName, throwOnError, allowUnsafeTypeBinding);

    /// <summary>
    /// Generates a delegate that gets a static property value.
    /// </summary>
    /// <typeparam name="TDeclaringType">Declaring type of the property.</typeparam>
    /// <typeparam name="TValue">Property return type.</typeparam>
    /// <param name="propertyName">Name of property that will be referenced.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static StaticGetter<TValue>? GenerateStaticPropertyGetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false)
        => GenerateStaticPropertyGetter<TValue>(typeof(TDeclaringType), propertyName, throwOnError, true);

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
    [Pure]
    // ReSharper disable once MethodOverloadWithOptionalParameter
    public static StaticGetter<TValue>? GenerateStaticPropertyGetter<TDeclaringType, TValue>(string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = true)
        => GenerateStaticPropertyGetter<TValue>(typeof(TDeclaringType), propertyName, throwOnError, allowUnsafeTypeBinding);

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
    [Pure]
    public static StaticSetter<TValue>? GenerateStaticPropertySetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating static setter for <unknown>.{propertyName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateStaticPropertySetter", null, $"Error generating static setter for <unknown>.{propertyName}. Declaring type not found.");
            return null;
        }
        PropertyInfo? property = declaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        MethodInfo? setter = property?.GetSetMethod(true);

        if (setter is null || !setter.IsStatic || setter.GetParameters() is not { Length: 1 } parameters || (!parameters[0].ParameterType.IsAssignableFrom(typeof(TValue)) && typeof(TValue) != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching property: {declaringType.FullName}.{propertyName} with a setter.");
            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateStaticPropertySetter", null, $"Unable to find matching property {declaringType.FullName}.{propertyName} with a setter.");
            return null;
        }

        return GenerateStaticCaller<StaticSetter<TValue>>(setter, throwOnError, allowUnsafeTypeBinding);
    }

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
    [Pure]
    public static StaticGetter<TValue>? GenerateStaticPropertyGetter<TValue>(Type declaringType, string propertyName, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (declaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Error generating static getter for <unknown>.{propertyName}. Declaring type not found.");
            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateStaticPropertyGetter", null, $"Error generating static getter for <unknown>.{propertyName}. Declaring type not found.");
            return null;
        }
        PropertyInfo? property = declaringType.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        MethodInfo? getter = property?.GetGetMethod(true);

        if (getter is null || !getter.IsStatic || (!typeof(TValue).IsAssignableFrom(getter.ReturnType) && getter.ReturnType != typeof(object)))
        {
            if (throwOnError)
                throw new Exception($"Unable to find matching property: {declaringType.FullName}.{propertyName} with a getter.");
            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateStaticPropertyGetter", null, $"Unable to find matching property {declaringType.FullName}.{propertyName} with a getter.");
            return null;
        }

        return GenerateStaticCaller<StaticGetter<TValue>>(getter, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method.
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
    [Pure]
    public static Delegate? GenerateInstanceCaller<TInstance>(string methodName, Type[]? parameters = null, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
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
                    throw new Exception($"Unable to find matching instance method: {typeof(TInstance).FullName}.{methodName}.");

                if (LogErrorMessages)
                    Logger.LogError("Accessor.GenerateInstanceCaller", null, $"Unable to find matching instance method: {typeof(TInstance).FullName}.{methodName}.");
                return null;
            }
        }

        return GenerateInstanceCaller(method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="methodName">Name of method that will be called.</param>
    /// <param name="parameters">Optional parameter list for resolving ambiguous methods.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static TDelegate? GenerateInstanceCaller<TInstance, TDelegate>(string methodName, bool throwOnError = false, bool allowUnsafeTypeBinding = false, Type[]? parameters = null) where TDelegate : Delegate
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
                    throw new Exception($"Unable to find matching instance method: {typeof(TInstance).FullName}.{methodName}.");

                if (LogErrorMessages)
                    Logger.LogError("Accessor.GenerateInstanceCaller", null, $"Unable to find matching instance method: {typeof(TInstance).FullName}.{methodName}.");
                return null;
            }
        }

        return GenerateInstanceCaller<TDelegate>(method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature. The first parameter will be the instance.</returns>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static Delegate? GenerateInstanceCaller(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (method == null || method.IsStatic || method.DeclaringType == null)
        {
            if (throwOnError)
                throw new Exception("Unable to find instance method.");
            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateInstanceCaller", null, "Unable to find instance method.");
            return null;
        }

        CheckFuncArrays();

        bool rtn = method.ReturnType != typeof(void);
        ParameterInfo[] p = method.GetParameters();
        int maxArgs = rtn ? FuncTypes!.Length : ActionTypes!.Length;
        if (p.Length + 1 > maxArgs)
        {
            if (throwOnError)
                throw new ArgumentException($"Method {method.DeclaringType.FullName}.{method.Name} can not have more than {maxArgs} arguments!", nameof(method));

            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateInstanceCaller", null, $"Method {method.DeclaringType.FullName}.{method.Name} can not have more than {maxArgs} arguments!");
            return null;
        }

        Type deleType = GetDefaultDelegate(method.ReturnType, p, method.DeclaringType)!;
        return GenerateInstanceCaller(deleType, method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static TDelegate? GenerateInstanceCaller<TDelegate>(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TDelegate : Delegate
    {
        return (TDelegate?)GenerateInstanceCaller(typeof(TDelegate), method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls an instance method.
    /// </summary>
    /// <remarks>The first parameter will be the instance.</remarks>
    /// <param name="delegateType">Type of delegate to return.</param>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).
    /// This also must be <see langword="true"/> to not null-check instance methods of parameter-less reference types with a dynamic method.</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static Delegate? GenerateInstanceCaller(Type delegateType, MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        const string source = "Accessor.GenerateInstanceCaller";
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.Name + " is not a delegate.", nameof(delegateType));
        if (method == null || method.IsStatic || method.DeclaringType == null)
        {
            if (throwOnError)
                throw new Exception($"Unable to find instance method for delegate: {delegateType.Name}.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to find instance method for delegate: {delegateType.Name}.");
            return null;
        }

        ParameterInfo[] p = method.GetParameters();
        Type instance = method.DeclaringType;

        MethodInfo invokeMethod = delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public)!;
        ParameterInfo[] delegateParameters = invokeMethod.GetParameters();
        Type delegateReturnType = invokeMethod.ReturnType;
        bool shouldCallvirt = method.ShouldCallvirtRuntime();
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
                throw new Exception("Unable to create instance caller for " + instance.FullName + "." + (method.Name ?? "<unknown-name>") + $": incompatable delegate type: {delegateType.FullName}.");

            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to create instance caller for {instance.FullName}.{method.Name}: incompatable delegate type: {delegateType.FullName}.");
            return null;
        }

        if (needsDynamicMethod && instance.IsInterface && !delegateParameters[0].ParameterType.IsInterface)
            needsDynamicMethod = false;

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
                        Logger.LogDebug(source, $"Created basic delegate binding instance caller for {instance.FullName}.{method.Name}.");
                    return basicDelegate;
                }
                catch (Exception ex)
                {
                    if (LogDebugMessages || LogILTraceMessages)
                    {
                        Logger.LogDebug(source, $"Unable to create basic delegate binding instance caller for {instance.FullName}.{method.Name}.");
                        Logger.LogDebug(source, ex.GetType() + " - " + ex.Message);
                    }
                }
            }
        }

        if (isInstanceForValueType && delegateParameters[0].ParameterType != typeof(object) && !method.IsReadOnly())
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance caller for {instance.FullName}.{method.Name} (non-readonly), you must pass structs ({instance.Name}) as a boxed object (in {delegateType.FullName}).");

            if (LogErrorMessages || LogILTraceMessages)
                Logger.LogError(source, null, $"Unable to create instance caller for {instance.FullName}.{method.Name} (non-readonly), you must pass structs ({instance.Name}) as a boxed object (in {delegateType.FullName}).");
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
                        Logger.LogDebug(source, $"Created unsafely binded delegate binding instance caller for {instance.Name}.{method.Name}.");
                    return (Delegate)d2;
                }
                catch (Exception ex)
                {
                    if (LogDebugMessages || LogILTraceMessages)
                    {
                        Logger.LogDebug(source, $"Unable to create unsafely binded delegate binding instance caller for {instance.Name}.{method.Name}.");
                        Logger.LogDebug(source, ex.GetType() + " - " + ex.Message);
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
            Logger.LogDebug(source, $"IL: Generating instance caller for {instance.FullName}.{method.Name}:");
        dynMethod.DefineParameter(1, ParameterAttributes.None, "this");

        for (int i = 0; i < p.Length; ++i)
            dynMethod.DefineParameter(i + 2, p[i].Attributes, p[i].Name);

        ILGenerator generator = dynMethod.GetILGenerator();

        if (instance.IsValueType)
        {
            if (!delegateParameters[0].ParameterType.IsValueType)
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Unbox, instance);
                if (logIl)
                {
                    Logger.LogDebug(source, "IL:  ldarg.0");
                    Logger.LogDebug(source, $"IL:  unbox <{instance.FullName}>");
                }
            }
            else
            {
                generator.Emit(OpCodes.Ldarga_S, 0);
                if (logIl)
                    Logger.LogDebug(source, "IL:  ldarga.s <0>");
            }
        }
        else
        {
            generator.Emit(OpCodes.Ldarg_0);
            if (logIl)
                Logger.LogDebug(source, "IL:  ldarg.0");
        }

        for (int i = 0; i < p.Length; ++i)
            generator.EmitParameter(source, i + 1, $"Invalid argument type passed to instance caller for {instance.FullName}.{method.Name} at parameter {i.ToString(CultureInfo.InvariantCulture)} ({p[i].Name}). Expected {p[i].ParameterType.FullName}.", false, type: parameterTypes[i + 1], p[i].ParameterType);

        OpCode call = shouldCallvirt ? OpCodes.Callvirt : OpCodes.Call;
        generator.Emit(call, method);
        if (logIl)
            Logger.LogDebug(source, $"IL:  {(shouldCallvirt ? "callvirt" : "call")} <{instance.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(x => x.ParameterType.FullName))})>");
        if (method.ReturnType != typeof(void) && delegateReturnType == typeof(void))
        {
            generator.Emit(OpCodes.Pop);
            if (logIl)
                Logger.LogDebug(source, "IL:  pop");
        }
        else if (method.ReturnType != typeof(void))
        {
            if (method.ReturnType.IsValueType && !delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Box, method.ReturnType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  box <{method.ReturnType.FullName}>");
            }
            else if (!method.ReturnType.IsValueType && delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, delegateReturnType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  unbox.any <{delegateReturnType.FullName}>");
            }
        }
        else if (delegateReturnType != typeof(void))
        {
            if (!delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Ldnull);
                if (logIl)
                    Logger.LogDebug(source, "IL:  ldnull");
            }
            else
            {
                generator.DeclareLocal(delegateReturnType);
                generator.Emit(OpCodes.Ldloca_S, 0);
                generator.Emit(OpCodes.Initobj, delegateReturnType);
                generator.Emit(OpCodes.Ldloc_0);
                if (logIl)
                {
                    Logger.LogDebug(source, "IL:  ldloca.s <0>");
                    Logger.LogDebug(source, $"IL:  initobj <{delegateReturnType.FullName}>");
                    Logger.LogDebug(source, "IL:  ldloc.0");
                }
            }
        }
        generator.Emit(OpCodes.Ret);
        if (logIl)
            Logger.LogDebug(source, "IL:  ret");

        try
        {
            Delegate dynamicDelegate = dynMethod.CreateDelegate(delegateType);
            if (LogDebugMessages || logIl)
                Logger.LogDebug(source, $"Created dynamic method instance caller for {instance.FullName}.{method.Name}.");
            return dynamicDelegate;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Unable to create instance caller for {instance.FullName}.{method.Name}.", ex);

            if (LogErrorMessages)
                Logger.LogError(source, ex, $"Unable to create instance caller for {instance.FullName}.{method.Name}.");
            return null;
        }
    }

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
    [Pure]
    public static Delegate? GenerateStaticCaller<TDeclaringType>(string methodName, Type[]? parameters = null, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
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
                    throw new Exception($"Unable to find matching static method: {typeof(TDeclaringType).FullName}.{methodName}.");

                if (LogErrorMessages)
                    Logger.LogError("Accessor.GenerateStaticCaller", null, $"Unable to find matching static method: {typeof(TDeclaringType).FullName}.{methodName}.");
                return null;
            }
        }

        return GenerateStaticCaller(method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <param name="methodName">Name of method that will be called.</param>
    /// <param name="parameters">Optional parameter list for resolving ambiguous methods.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static TDelegate? GenerateStaticCaller<TDeclaringType, TDelegate>(string methodName, bool throwOnError = false, bool allowUnsafeTypeBinding = false, Type[]? parameters = null) where TDelegate : Delegate
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
                    throw new Exception($"Unable to find matching static method: {typeof(TDeclaringType).FullName}.{methodName}.");

                if (LogErrorMessages)
                    Logger.LogError("Accessor.GenerateStaticCaller", null, $"Unable to find matching static method: {typeof(TDeclaringType).FullName}.{methodName}.");
                return null;
            }
        }

        return GenerateStaticCaller<TDelegate>(method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature.</returns>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static Delegate? GenerateStaticCaller(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (method == null || !method.IsStatic)
        {
            if (throwOnError)
                throw new Exception("Unable to find static method.");
            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateStaticCaller", null, "Unable to find static method.");
            return null;
        }

        CheckFuncArrays();

        bool rtn = method.ReturnType != typeof(void);
        ParameterInfo[] p = method.GetParameters();
        int maxArgs = rtn ? FuncTypes!.Length : ActionTypes!.Length;
        if (p.Length > maxArgs)
        {
            if (throwOnError)
                throw new ArgumentException("Method can not have more than " + maxArgs + " arguments!", nameof(method));

            if (LogErrorMessages)
                Logger.LogError("Accessor.GenerateStaticCaller", null, "Method " + method + " can not have more than " + maxArgs + " arguments!");
            return null;
        }

        Type deleType = GetDefaultDelegate(method.ReturnType, p, null)!;
        return GenerateStaticCaller(deleType, method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static TDelegate? GenerateStaticCaller<TDelegate>(MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false) where TDelegate : Delegate
    {
        return (TDelegate?)GenerateStaticCaller(typeof(TDelegate), method, throwOnError, allowUnsafeTypeBinding);
    }

    /// <summary>
    /// Generates a delegate or dynamic method that calls a static method.
    /// </summary>
    /// <param name="delegateType">Type of delegate to return.</param>
    /// <param name="allowUnsafeTypeBinding">Enables unsafe type binding to non-matching delegates, meaning classes of different
    /// types can be passed as parameters and an exception will not be thrown (may cause unintended behavior if the wrong type is passed).</param>
    /// <param name="method">Method that will be called.</param>
    /// <param name="throwOnError">Throw an error instead of writing to console and returning <see langword="null"/>.</param>
    /// <remarks>Will never return <see langword="null"/> if <paramref name="throwOnError"/> is <see langword="true"/>.</remarks>
    [Pure]
    public static Delegate? GenerateStaticCaller(Type delegateType, MethodInfo method, bool throwOnError = false, bool allowUnsafeTypeBinding = false)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.FullName + " is not a delegate.", nameof(delegateType));

        const string source = "Accessor.GenerateStaticCaller";
        if (method == null || !method.IsStatic)
        {
            if (throwOnError)
                throw new Exception($"Unable to find static method for delegate: {delegateType.FullName}.");
            if (LogErrorMessages)
                Logger.LogError(source, null, $"Unable to find static method for delegate: {delegateType.FullName}.");
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
                throw new Exception("Unable to create static caller for " + (method.DeclaringType?.FullName ?? "<unknown-type>") + "." + (method.Name ?? "<unknown-name>") + $": incompatable delegate type: {delegateType.FullName}.");

            if (LogErrorMessages)
                Logger.LogError(source, null, "Unable to create static caller for " + (method.DeclaringType?.FullName ?? "<unknown-type>") + "." + (method.Name ?? "<unknown-name>") + $": incompatable delegate type: {delegateType.FullName}.");
            return null;
        }

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
                        Logger.LogDebug(source, $"Created basic delegate binding static caller for {method.DeclaringType?.FullName ?? "<unknown-type>"}.{method.Name}.");
                    return basicDelegateCaller;
                }
                catch (Exception ex)
                {
                    if (LogDebugMessages || LogILTraceMessages)
                    {
                        Logger.LogDebug(source, $"Unable to create basic delegate binding static caller for {method.DeclaringType?.FullName ?? "<unknown-type>"}.{method.Name}.");
                        Logger.LogDebug(source, ex.GetType() + " - " + ex.Message);
                    }
                }
            }
        }

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
                        Logger.LogDebug(source, $"Created unsafely binded delegate binding static caller for {method.DeclaringType?.Name ?? "<unknown-type>"}.{method.Name}.");
                    return (Delegate)d2;
                }
                catch (Exception ex)
                {
                    if (LogDebugMessages || LogILTraceMessages)
                    {
                        Logger.LogDebug(source, $"Unable to create unsafely binded delegate binding static caller for {method.DeclaringType?.Name ?? "<unknown-type>"}.{method.Name}.");
                        Logger.LogDebug(source, ex.GetType() + " - " + ex.Message);
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
            Logger.LogDebug(source, $"IL: Generating static caller for {method.DeclaringType?.FullName ?? "<unknown type>"}.{method.Name}:");

        for (int i = 0; i < p.Length; ++i)
            dynMethod.DefineParameter(i + 1, p[i].Attributes, p[i].Name);

        ILGenerator generator = dynMethod.GetILGenerator();

        for (int i = 0; i < p.Length; ++i)
            generator.EmitParameter(source, i, $"Invalid argument type passed to static caller for {method.DeclaringType?.Name ?? "<unknown-type>"}.{method.Name} at parameter {i.ToString(CultureInfo.InvariantCulture)} ({p[i].Name}). Expected {p[i].ParameterType.FullName}.", false, type: parameterTypes[i], p[i].ParameterType);

        generator.Emit(OpCodes.Call, method);
        if (logIl)
            Logger.LogDebug(source, $"IL:  call <{method.DeclaringType?.FullName ?? "<unknown type>"}.{method.Name}({string.Join(", ", method.GetParameters().Select(x => x.ParameterType.FullName))})>");
        if (method.ReturnType != typeof(void) && delegateReturnType == typeof(void))
        {
            generator.Emit(OpCodes.Pop);
            if (logIl)
                Logger.LogDebug(source, "IL:  pop");
        }
        else if (method.ReturnType != typeof(void))
        {
            if (method.ReturnType.IsValueType && !delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Box, method.ReturnType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  box <{method.ReturnType.FullName}>");
            }
            else if (!method.ReturnType.IsValueType && delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, delegateReturnType);
                if (logIl)
                    Logger.LogDebug(source, $"IL:  unbox.any <{delegateReturnType.FullName}>");
            }
        }
        else if (delegateReturnType != typeof(void))
        {
            if (!delegateReturnType.IsValueType)
            {
                generator.Emit(OpCodes.Ldnull);
                if (logIl)
                    Logger.LogDebug(source, "IL:  ldnull");
            }
            else
            {
                generator.DeclareLocal(delegateReturnType);
                generator.Emit(OpCodes.Ldloca_S, 0);
                generator.Emit(OpCodes.Initobj, delegateReturnType);
                generator.Emit(OpCodes.Ldloc_0);
                if (logIl)
                {
                    Logger.LogDebug(source, "IL:  ldloca.s <0>");
                    Logger.LogDebug(source, $"IL:  initobj <{delegateReturnType.FullName}>");
                    Logger.LogDebug(source, "IL:  ldloc.0");
                }
            }
        }

        generator.Emit(OpCodes.Ret);
        if (logIl)
            Logger.LogDebug(source, "IL:  ret");

        try
        {
            Delegate dynamicDelegate = dynMethod.CreateDelegate(delegateType);
            if (LogDebugMessages || logIl)
                Logger.LogDebug(source, $"Created dynamic method static caller for {method.DeclaringType?.Name ?? "<unknown-type>"}.{method.Name}.");
            return dynamicDelegate;
        }
        catch (Exception ex)
        {
            if (throwOnError)
                throw new Exception($"Unable to create static caller for {method.DeclaringType?.FullName ?? "<unknown-type>"}.{method.Name}.", ex);

            if (LogErrorMessages)
                Logger.LogError(source, ex, $"Unable to create static caller for {method.DeclaringType?.FullName ?? "<unknown-type>"}.{method.Name}.");
            return null;
        }
    }

    /// <summary>
    /// Gets platform-specific flags for creating dynamic methods.
    /// </summary>
    /// <param name="static">Whether or not the method has no 'instance', only considered when on mono.</param>
    /// <param name="attributes">Method attributes to pass to <see cref="DynamicMethod"/> constructor.</param>
    /// <param name="convention">Method convention to pass to <see cref="DynamicMethod"/> constructor.</param>
    public static void GetDynamicMethodFlags(bool @static, out MethodAttributes attributes, out CallingConventions convention)
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
        FuncTypes ??= new Type[]
        {
            typeof(Func<,>),
            typeof(Func<,,>),
            typeof(Func<,,,>),
            typeof(Func<,,,,>),
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
        };
        ActionTypes ??= new Type[]
        {
            typeof(Action<>),
            typeof(Action<,>),
            typeof(Action<,,>),
            typeof(Action<,,,>),
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
        };
    }

    /// <summary>
    /// Checks <paramref name="method"/> for the <see langword="extern"/> flag.
    /// </summary>
    [Pure]
    public static bool IsExtern(this MethodBase method)
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

    /// <summary>
    /// Checks <paramref name="field"/> for the <see langword="extern"/> flag.
    /// </summary>
    [Pure]
    public static bool IsExtern(this FieldInfo field) => field.IsPinvokeImpl;

    /// <summary>
    /// Checks <paramref name="property"/>'s getter and setter for the <see langword="extern"/> flag.
    /// </summary>
    [Pure]
    public static bool IsExtern(this PropertyInfo property, bool checkGetterFirst = true)
    {
        MethodInfo? method = checkGetterFirst ? property.GetGetMethod(true) : property.GetSetMethod(true);
        if (method == null)
        {
            method = checkGetterFirst ? property.GetSetMethod(true) : property.GetGetMethod(true);
            if (method == null)
                return false;
        }

        return method.IsExtern();
    }

    /// <summary>
    /// Checks for the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    [Pure]
    public static bool IsDefinedSafe<TAttribute>(this ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute => member.IsDefinedSafe(typeof(TAttribute), inherit);

    /// <summary>
    /// Checks for the attribute of type <paramref name="attributeType"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="attributeType">Type of the attribute to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <exception cref="ArgumentException"><paramref name="attributeType"/> did not derive from <see cref="Attribute"/>.</exception>
    [Pure]
    public static bool IsDefinedSafe(this ICustomAttributeProvider member, Type attributeType, bool inherit = false)
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
    
    /// <summary>
    /// Checks for the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    [Pure]
    public static bool HasAttributeSafe<TAttribute>(this ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute => member.HasAttributeSafe(typeof(TAttribute), inherit);

    /// <summary>
    /// Checks for the attribute of type <paramref name="attributeType"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="attributeType">Type of the attribute to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <exception cref="ArgumentException"><paramref name="attributeType"/> did not derive from <see cref="Attribute"/>.</exception>
    [Pure]
    public static bool HasAttributeSafe(this ICustomAttributeProvider member, Type attributeType, bool inherit = false)
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

    private static void LogTypeLoadException(TypeLoadException ex, string source, string context)
    {
        string msg = context + $" Can't load type: {ex.TypeName}.";
        if (ex.InnerException != null)
            msg += "(" + ex.InnerException.GetType().Name + " | " + ex.InnerException.Message + ")";
        Logger.LogDebug(source, msg);
    }
    private static void LogFileNotFoundException(FileNotFoundException ex, string source, string context)
    {
        string msg = context + $" Missing assembly: {ex.FileName}.";
        Logger.LogDebug(source, msg);
    }

    /// <summary>
    /// Checks for and returns the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="inherit">Also check parent members.</param>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <typeparam name="TAttribute">Type of the attribute to check for.</typeparam>
    /// <exception cref="AmbiguousMatchException">There are more than one attributes of type <typeparamref name="TAttribute"/>.</exception>
    [Pure]
    public static TAttribute? GetAttributeSafe<TAttribute>(this ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute
        => member.GetAttributeSafe(typeof(TAttribute), inherit) as TAttribute;

    /// <summary>
    /// Checks for and returns the attribute of type <paramref name="attributeType"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="attributeType">Type of the attribute to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <exception cref="ArgumentException"><paramref name="attributeType"/> did not derive from <see cref="Attribute"/>.</exception>
    /// <exception cref="AmbiguousMatchException">There are more than one attributes of type <paramref name="attributeType"/>.</exception>
    [Pure]
    public static Attribute? GetAttributeSafe(this ICustomAttributeProvider member, Type attributeType, bool inherit = false)
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

    /// <summary>
    /// Checks for and returns the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="inherit">Also check parent members.</param>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <typeparam name="TAttribute">Type of the attribute to check for.</typeparam>
    [Pure]
    public static TAttribute[] GetAttributesSafe<TAttribute>(this ICustomAttributeProvider member, bool inherit = false) where TAttribute : Attribute
        => (TAttribute[])member.GetAttributesSafe(typeof(TAttribute), inherit);

    /// <summary>
    /// Checks for and returns the attribute of type <paramref name="attributeType"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <param name="attributeType">Type of the attribute to check for.</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <exception cref="ArgumentException"><paramref name="attributeType"/> did not derive from <see cref="Attribute"/>.</exception>
    [Pure]
    public static Attribute[] GetAttributesSafe(this ICustomAttributeProvider member, Type attributeType, bool inherit = false)
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

    /// <summary>
    /// Checks for and outputs the the attribute of type <typeparamref name="TAttribute"/> on <paramref name="member"/>.
    /// </summary>
    /// <param name="attribute">Found attribute, or <see langword="null"/> if it's not found (the function will return <see langword="false"/>).</param>
    /// <param name="inherit">Also check parent members.</param>
    /// <param name="member">Member to check for attributes. This can be <see cref="Module"/>, <see cref="Assembly"/>, <see cref="MemberInfo"/>, or <see cref="ParameterInfo"/>.</param>
    /// <typeparam name="TAttribute">Type of the attribute to check for.</typeparam>
    /// <returns><see langword="true"/> if the attribute was found, otherwise <see langword="false"/>.</returns>
    [Pure]
    public static bool TryGetAttributeSafe<TAttribute>(this ICustomAttributeProvider member, out TAttribute attribute, bool inherit = false) where TAttribute : Attribute
    {
        attribute = (member.GetAttributeSafe(typeof(TAttribute), inherit) as TAttribute)!;
        return attribute != null;
    }
#if NET471_OR_GREATER || NET || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1
    /// <summary>
    /// Checks for the <see cref="IsReadOnlyAttribute"/> on <paramref name="member"/>, which signifies the readonly value.
    /// <remarks>This behavior is overridden on fields to check <see cref="FieldInfo.IsInitOnly"/>.</remarks>
    /// </summary>
    [Pure]
    public static bool IsReadOnly(this ICustomAttributeProvider member)
    {
        if (member is FieldInfo field)
        {
            return field.IsInitOnly;
        }
        return member.IsDefinedSafe(_readonlyAttribute ??= typeof(IsReadOnlyAttribute));
    }
#else
    /// <summary>
    /// Checks for <see cref="FieldInfo.IsInitOnly"/> on <paramref name="member"/> if it is a field.
    /// </summary>
    /// <remarks>.NET Framework 4.7.1 and lower, .NET Standard 2.0, or .NET Core 2.0 and lower do not support the <c>IsReadOnlyAttribute</c> to check for readonly methods and structs.</remarks>
    [Pure]
    public static bool IsReadOnly(this ICustomAttributeProvider member)
    {
        if (member is FieldInfo field)
        {
            return field.IsInitOnly;
        }
        return false;
    }
#endif

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="type"/>.
    /// </summary>
    [Pure]
    public static bool IsIgnored(this Type type) => type.IsDefinedSafe(_ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="member"/>.
    /// </summary>
    [Pure]
    public static bool IsIgnored(this MemberInfo member) => member.IsDefinedSafe(_ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="assembly"/>.
    /// </summary>
    [Pure]
    public static bool IsIgnored(this Assembly assembly) => assembly.IsDefinedSafe(_ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="parameter"/>.
    /// </summary>
    [Pure]
    public static bool IsIgnored(this ParameterInfo parameter) => parameter.IsDefinedSafe(_ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <summary>
    /// Checks for the <see cref="IgnoreAttribute"/> on <paramref name="module"/>.
    /// </summary>
    [Pure]
    public static bool IsIgnored(this Module module) => module.IsDefinedSafe(_ignoreAttribute ??= typeof(IgnoreAttribute));

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="type"/> and returns the priority (or zero if not found).
    /// </summary>
    [Pure]
    public static int GetPriority(this Type type) => type.GetAttributeSafe(_priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="member"/> and returns the priority (or zero if not found).
    /// </summary>
    [Pure]
    public static int GetPriority(this MemberInfo member) => member.GetAttributeSafe(_priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="assembly"/> and returns the priority (or zero if not found).
    /// </summary>
    [Pure]
    public static int GetPriority(this Assembly assembly) => assembly.GetAttributeSafe(_priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="parameter"/> and returns the priority (or zero if not found).
    /// </summary>
    [Pure]
    public static int GetPriority(this ParameterInfo parameter) => parameter.GetAttributeSafe(_priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <summary>
    /// Checks for the <see cref="PriorityAttribute"/> on <paramref name="module"/> and returns the priority (or zero if not found).
    /// </summary>
    [Pure]
    public static int GetPriority(this Module module) => module.GetAttributeSafe(_priorityAttribute ??= typeof(PriorityAttribute), true) is PriorityAttribute attr ? attr.Priority : 0;

    /// <summary>
    /// Created for <see cref="List{T}.Sort(Comparison{T})"/> to order by priority (highest to lowest).
    /// </summary>
    [Pure]
    public static int SortTypesByPriorityHandler(Type a, Type b)
    {
        return b.GetPriority().CompareTo(a.GetPriority());
    }

    /// <summary>
    /// Created for <see cref="List{T}.Sort(Comparison{T})"/> to order by priority (highest to lowest).
    /// </summary>
    [Pure]
    public static int SortMembersByPriorityHandler(MemberInfo a, MemberInfo b)
    {
        return b.GetPriority().CompareTo(a.GetPriority());
    }

    /// <summary>
    /// Safely gets the reflection method info of the passed method. Works best with static methods.<br/><br/>
    /// <code>
    /// MethodInfo? method = Accessor.GetMethod(Guid.Parse);
    /// </code>
    /// </summary>
    /// <returns>A method info of a passed delegate</returns>
    [Pure]
    public static MethodInfo? GetMethod(Delegate @delegate)
    {
        try
        {
            return @delegate.Method;
        }
        catch (MemberAccessException)
        {
            if (LogDebugMessages)
                Logger.LogDebug("Accessor.GetMethod", $"Failed to get a method from a delegate of type {@delegate.GetType().Name}.");

            return null;
        }
    }

    /// <param name="returnType">Return type of the method.</param>
    /// <param name="parameters">Method parameters, not including the instance.</param>
    /// <param name="instanceType">The declaring type, or <see langword="null"/> for static methods.</param>
    /// <remarks>The first argument will be the instance.</remarks>
    /// <returns>A delegate of type <see cref="Action"/> or <see cref="Func{T}"/> (or one of their generic counterparts), depending on the method signature, or <see langword="null"/> if there are too many parameters.</returns>
    [Pure]
    public static Type? GetDefaultDelegate(Type returnType, IReadOnlyList<ParameterInfo> parameters, Type? instanceType)
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

    /// <summary>
    /// Used to perform a repeated <paramref name="action"/> for each base type of a <paramref name="type"/>.
    /// </summary>
    /// <param name="type">Highest (most derived) type in the hierarchy.</param>
    /// <param name="action">Called optionally for <paramref name="type"/>, then for each base type in order from most related to least related.</param>
    /// <param name="includeParent">Call <paramref name="action"/> on <paramref name="type"/>. Overrides <paramref name="excludeSystemBase"/>.</param>
    /// <param name="excludeSystemBase">Excludes calling <paramref name="action"/> for <see cref="object"/> or <see cref="ValueType"/>.</param>
    public static void ForEachBaseType(this Type type, ForEachBaseType action, bool includeParent = true, bool excludeSystemBase = true)
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

    /// <summary>
    /// Used to perform a repeated <paramref name="action"/> for each base type of a <paramref name="type"/>.
    /// </summary>
    /// <remarks>Execution can be broken by returning <see langword="false"/>.</remarks>
    /// <param name="type">Highest (most derived) type in the hierarchy.</param>
    /// <param name="action">Called optionally for <paramref name="type"/>, then for each base type in order from most related to least related.</param>
    /// <param name="includeParent">Call <paramref name="action"/> on <paramref name="type"/>. Overrides <paramref name="excludeSystemBase"/>.</param>
    /// <param name="excludeSystemBase">Excludes calling <paramref name="action"/> for <see cref="object"/> or <see cref="ValueType"/>.</param>
    public static void ForEachBaseType(this Type type, ForEachBaseTypeWhile action, bool includeParent = true, bool excludeSystemBase = true)
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

    /// <returns>Every type defined in the calling assembly.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [Pure]
    public static List<Type> GetTypesSafe(bool removeIgnored = false) => GetTypesSafe(Assembly.GetCallingAssembly(), removeIgnored);

    /// <returns>Every type defined in <paramref name="assembly"/>.</returns>
    [Pure]
    public static List<Type> GetTypesSafe(Assembly assembly, bool removeIgnored = false)
    {
        List<Type?> types;
        bool removeNulls = false;
        try
        {
            types = new List<Type?>(assembly.GetTypes());
        }
        catch (FileNotFoundException ex)
        {
            if (LogDebugMessages)
                LogFileNotFoundException(ex, "Accessor.GetTypesSafe", $"Unable to get any types from assembly \"{assembly.FullName}\".");

            return new List<Type>(0);
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
            types = ex.Types == null ? new List<Type?>(0) : new List<Type?>(ex.Types);
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

    /// <returns>Every type defined in the provided <paramref name="assmeblies"/>.</returns>
    [Pure]
    public static List<Type> GetTypesSafe(IEnumerable<Assembly> assmeblies, bool removeIgnored = false)
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

    /// <summary>
    /// Takes a method declared in an interface and returns an implementation on <paramref name="type"/>. Useful for getting explicit implementations.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="interfaceMethod"/> is not defined in an interface or <paramref name="type"/> does not implement the interface it's defined in.</exception>
    [Pure]
    public static MethodInfo? GetImplementedMethod(Type type, MethodInfo interfaceMethod)
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

    /// <summary>
    /// Gets the (cached) <paramref name="returnType"/> and <paramref name="parameters"/> of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    public static void GetDelegateSignature<TDelegate>(out Type returnType, out ParameterInfo[] parameters) where TDelegate : Delegate
    {
        returnType = DelegateInfo<TDelegate>.ReturnType;
        parameters = DelegateInfo<TDelegate>.Parameters;
    }

    /// <summary>
    /// Gets the (cached) return type of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    [Pure]
    public static Type GetReturnType<TDelegate>() where TDelegate : Delegate => DelegateInfo<TDelegate>.ReturnType;

    /// <summary>
    /// Gets the (cached) parameters of a <typeparamref name="TDelegate"/> delegate type.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    [Pure]
    public static ParameterInfo[] GetParameters<TDelegate>() where TDelegate : Delegate => DelegateInfo<TDelegate>.Parameters;

    /// <summary>
    /// Gets the (cached) <see langword="Invoke"/> method of a <typeparamref name="TDelegate"/> delegate type. All delegates have one by default.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    [Pure]
    public static MethodInfo GetInvokeMethod<TDelegate>() where TDelegate : Delegate => DelegateInfo<TDelegate>.InvokeMethod;

    /// <summary>
    /// Gets the <paramref name="returnType"/> and <paramref name="parameters"/> of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    public static void GetDelegateSignature(Type delegateType, out Type returnType, out ParameterInfo[] parameters)
    {
        MethodInfo invokeMethod = GetInvokeMethod(delegateType);
        returnType = invokeMethod.ReturnType;
        parameters = invokeMethod.GetParameters();
    }

    /// <summary>
    /// Gets the return type of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    [Pure]
    public static Type GetReturnType(Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.Name + " is not a delegate type.", nameof(delegateType));

        return delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.ReturnType
               ?? throw new NotSupportedException($"Unable to find Invoke method in delegate {delegateType.Name}.");
    }

    /// <summary>
    /// Gets the parameters of a <paramref name="delegateType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    [Pure]
    public static ParameterInfo[] GetParameters(Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.Name + " is not a delegate type.", nameof(delegateType));

        return delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetParameters()
               ?? throw new NotSupportedException($"Unable to find Invoke method in delegate {delegateType.Name}.");
    }

    /// <summary>
    /// Gets the (cached) <see langword="Invoke"/> method of a <paramref name="delegateType"/>. All delegates have one by default.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    [Pure]
    public static MethodInfo GetInvokeMethod(Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
            throw new ArgumentException(delegateType.Name + " is not a delegate type.", nameof(delegateType));

        return delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
               ?? throw new NotSupportedException($"Unable to find Invoke method in delegate {delegateType.Name}.");
    }

    /// <summary>
    /// Get the 'type' of a member, returns <see cref="FieldInfo.FieldType"/> or <see cref="PropertyInfo.PropertyType"/> or
    /// <see cref="MethodInfo.ReturnType"/> or <see cref="EventInfo.EventHandlerType"/> or <see cref="MemberInfo.DeclaringType"/> for constructors.
    /// </summary>
    [Pure]
    public static Type? GetMemberType(this MemberInfo member) => member switch
    {
        MethodInfo a => a.ReturnType,
        FieldInfo a => a.FieldType,
        PropertyInfo a => a.PropertyType,
        ConstructorInfo a => a.DeclaringType,
        EventInfo a => a.EventHandlerType,
        _ => throw new ArgumentException($"Member type {member.GetType().Name} does not have a member type.", nameof(member))
    };

    /// <summary>
    /// Check any member for being static.
    /// </summary>
    [Pure]
    public static bool GetIsStatic(this MemberInfo member) => member switch
    {
        MethodBase a => a.IsStatic,
        FieldInfo a => a.IsStatic,
        PropertyInfo a => a.GetGetMethod(true) is { } getter ? getter.IsStatic : (a.GetSetMethod(true) is { } setter && setter.IsStatic),
        EventInfo a => a.GetAddMethod(true) is { } adder ? adder.IsStatic : (a.GetRemoveMethod(true) is { } remover ? remover.IsStatic : (a.GetRaiseMethod(true) is { } raiser && raiser.IsStatic)),
        Type a => (a.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.Class)) != 0,
        _ => throw new ArgumentException($"Member type {member.GetType().Name} is not static-able.", nameof(member))
    };

    /// <summary>
    /// Decide if a method should be callvirt'd instead of call'd. Usually you will use <see cref="ShouldCallvirtRuntime"/> instead as it doesn't account for possible future keyword changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
    [Pure]
    public static bool ShouldCallvirt(this MethodBase method)
    {
        return method is { IsFinal: false, IsVirtual: true } || method.IsAbstract || method is { IsStatic: false, DeclaringType: not { IsValueType: true }, IsFinal: false } || method.DeclaringType is { IsInterface: true };
    }

    /// <summary>
    /// Decide if a method should be callvirt'd instead of call'd at runtime. Doesn't account for future changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
    [Pure]
    public static bool ShouldCallvirtRuntime(this MethodBase method)
    {
        return method is { IsFinal: false, IsVirtual: true } || method.IsAbstract || method.DeclaringType is { IsInterface: true };
    }

    /// <summary>
    /// Get the underlying array from a list.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentNullException"/>
    [Pure]
    public static TElementType[] GetUnderlyingArray<TElementType>(this List<TElementType> list) => ListInfo<TElementType>.GetUnderlyingArray(list);

    /// <summary>
    /// Get the underlying array from a list, or in the case of a reflection failure calls <see cref="List{TElementType}.ToArray"/> on <paramref name="list"/> and returns that.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    [Pure]
    public static TElementType[] GetUnderlyingArrayOrCopy<TElementType>(this List<TElementType> list) => ListInfo<TElementType>.TryGetUnderlyingArray(list, out TElementType[] array) ? array : list.ToArray();

    /// <summary>
    /// Get the version of a list, which is incremented each time the list is updated.
    /// </summary>
    /// <exception cref="NotSupportedException">Reflection failure.</exception>
    /// <exception cref="ArgumentNullException"/>
    [Pure]
    public static int GetListVersion<TElementType>(this List<TElementType> list) => ListInfo<TElementType>.GetListVersion(list);

    /// <summary>
    /// Get the underlying array from a list.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public static bool TryGetUnderlyingArray<TElementType>(List<TElementType> list, out TElementType[] underlyingArray) => ListInfo<TElementType>.TryGetUnderlyingArray(list, out underlyingArray);

    /// <summary>
    /// Get the version of a list, which is incremented each time the list is updated.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    public static bool TryGetListVersion<TElementType>(List<TElementType> list, out int version) => ListInfo<TElementType>.TryGetListVersion(list, out version);

    /// <summary>
    /// Checks if it's possible for a variable of type <paramref name="actualType"/> to have a value of type <paramref name="queriedType"/>. 
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="actualType"/> is assignable from <paramref name="queriedType"/> or if <paramref name="queriedType"/> is assignable from <paramref name="actualType"/>.</returns>
    [Pure]
    public static bool CouldBeAssignedTo(this Type actualType, Type queriedType) => actualType.IsAssignableFrom(queriedType) || queriedType.IsAssignableFrom(actualType);

    /// <summary>
    /// Checks if it's possible for a variable of type <paramref name="actualType"/> to have a value of type <typeparamref name="T"/>. 
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="actualType"/> is assignable from <typeparamref name="T"/> or if <typeparamref name="T"/> is assignable from <paramref name="actualType"/>.</returns>
    [Pure]
    public static bool CouldBeAssignedTo<T>(this Type actualType) => actualType.CouldBeAssignedTo(typeof(T));
    private static class DelegateInfo<TDelegate> where TDelegate : Delegate
    {
        public static MethodInfo InvokeMethod { get; }
        public static ParameterInfo[] Parameters { get; }
        public static Type ReturnType { get; }
        static DelegateInfo()
        {
            InvokeMethod = typeof(TDelegate).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
            if (InvokeMethod == null)
                throw new NotSupportedException($"Unable to find Invoke method in delegate {typeof(TDelegate).Name}.");

            Parameters = InvokeMethod.GetParameters();
            ReturnType = InvokeMethod.ReturnType;
        }
    }
    private static class ListInfo<TElementType>
    {
        private static InstanceGetter<List<TElementType>, TElementType[]>? _underlyingArrayGetter;
        private static InstanceGetter<List<TElementType>, int>? _versionGetter;
        private static bool _checkUndArr;
        private static bool _checkVer;
        private static bool CheckUndArr()
        {
            if (!_checkUndArr)
            {
                _underlyingArrayGetter = GenerateInstanceGetter<List<TElementType>, TElementType[]>("_items", false);
                _checkUndArr = true;
            }
            return _underlyingArrayGetter != null;
        }
        private static bool CheckVer()
        {
            if (!_checkVer)
            {
                _versionGetter = GenerateInstanceGetter<List<TElementType>, int>("_version", true);
                _checkVer = true;
            }
            return _versionGetter != null;
        }
        public static TElementType[] GetUnderlyingArray(List<TElementType> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (_underlyingArrayGetter != null || CheckUndArr())
                return _underlyingArrayGetter!(list);

            throw new NotSupportedException($"Unable to find '_items' in list of {typeof(TElementType).Name}.");
        }
        public static int GetListVersion(List<TElementType> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (_versionGetter != null || CheckVer())
                return _versionGetter!(list);

            throw new NotSupportedException($"Unable to find '_version' in list of {typeof(TElementType).Name}.");
        }
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
/// <param name="depth">Number of types below the provided type this base type is. Will be zero if the type returned is the provided type, 1 for its base type, and so on.</param>
public delegate void ForEachBaseType(Type type, int depth);

/// <summary>
/// Used with <see cref="Accessor.ForEachBaseType(Type, ForEachBaseTypeWhile, bool, bool)"/>
/// </summary>
/// <param name="depth">Number of types below the provided type this base type is. Will be zero if the type returned is the provided type, 1 for its base type, and so on.</param>
/// <returns><see langword="True"/> to continue, <see langword="false"/> to break.</returns>
public delegate bool ForEachBaseTypeWhile(Type type, int depth);