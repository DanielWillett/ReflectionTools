using DanielWillett.ReflectionTools.Formatting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
#if !NETSTANDARD || NETSTANDARD2_1_OR_GREATER
using System.Runtime.InteropServices;
#endif
#if NETFRAMEWORK
using System.Diagnostics.SymbolStore;
#endif

namespace DanielWillett.ReflectionTools.Emit;
/// <summary>
/// Wrapper for an <see cref="ILGenerator"/> which has support for logging, both while creating the method or while executing the method.
/// </summary>
/// <remarks>See <see cref="DebugLog"/> and <see cref="Breakpointing"/>.</remarks>
public class DebuggableEmitter : IOpCodeEmitterLogSource
{
    private readonly DebuggableEmitter? _owner;
    private bool _init;
    private bool _lastWasPrefix;
    private readonly IAccessor _accessor;
    private List<string>? _prefixQueue;
    private static readonly MethodInfo? LogMethod = typeof(DebuggableEmitter).GetMethod("TryLogDebug", BindingFlags.Instance | BindingFlags.NonPublic);
    private string _logSource = nameof(DebuggableEmitter);
#if !(NET40_OR_GREATER || !NETFRAMEWORK)
    private int _logIndent;
    private string _ilOffsetRepl = string.Empty;
#endif

    /// <summary>
    /// Actively editing method.
    /// </summary>
    public MethodBase? Method { get; }

    /// <summary>
    /// Underlying <see cref="ILGenerator"/>.
    /// </summary>
    public IOpCodeEmitter Generator { get; }

#if NET40_OR_GREATER || !NETFRAMEWORK
    /// <inheritdoc />
    public int ILOffset => Generator.ILOffset;
#endif

    /// <summary>
    /// Indent level of debug logging.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    public int LogIndent { get; private set; }
#else
    public int LogIndent
    {
        get => _logIndent;
        private set
        {
            _logIndent = value;
            _ilOffsetRepl = null!;
        }
    }
#endif

    /// <summary>
    /// Enable debug logging while emitting instructions.
    /// </summary>
    public bool DebugLog { get; set; }

    /// <summary>
    /// Enable debug logging while calling instructions.
    /// </summary>
    /// <remarks>This is done by inserting log calls for each instruction.</remarks>
    public bool Breakpointing { get; set; }

    /// <summary>
    /// Number of whitespaces per indent.
    /// </summary>
    /// <remarks>Default is 4.</remarks>
    public int IndentSpacing { get; set; } = 4;

    /// <inheritdoc />
    public string LogSource
    {
        get => _logSource;
        set => _logSource = value ?? nameof(DebuggableEmitter);
    }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from a <see cref="MethodBuilder"/>.
    /// </summary>
    public DebuggableEmitter(MethodBuilder method, IAccessor? accessor = null)
        : this(method.GetILGenerator().AsEmitter(), method, accessor) { }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from a <see cref="ConstructorBuilder"/>.
    /// </summary>
    public DebuggableEmitter(ConstructorBuilder constructor, IAccessor? accessor = null)
        : this(constructor.GetILGenerator().AsEmitter(), constructor, accessor) { }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from a <see cref="DynamicMethod"/>.
    /// </summary>
    public DebuggableEmitter(DynamicMethod method, IAccessor? accessor = null)
        : this(method.GetILGenerator().AsEmitter(), method, accessor) { }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from an <see cref="IOpCodeEmitter"/> and <see cref="MethodBase"/> used for logging.
    /// </summary>
    public DebuggableEmitter(IOpCodeEmitter generator, MethodBase? method, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        _accessor = accessor;
        if (generator is DebuggableEmitter emitter && emitter.GetType() == typeof(DebuggableEmitter))
            generator = emitter.Generator;
        Generator = generator;
        Method = method;
    }

    private DebuggableEmitter(DebuggableEmitter other, IOpCodeEmitter newEmitter)
    {
        _init = other._init;
        _owner = other;

        if (newEmitter is DebuggableEmitter emitter && emitter.GetType() == typeof(DebuggableEmitter))
            newEmitter = emitter.Generator;
        Generator = newEmitter;
        Method = other.Method;
        _lastWasPrefix = other._lastWasPrefix;
        _accessor = other._accessor;
        _logSource = other._logSource;
#if !(NET40_OR_GREATER || !NETFRAMEWORK)
        _logIndent = other._logIndent;
        _ilOffsetRepl = other._ilOffsetRepl;
#else
        LogIndent = other.LogIndent;
#endif
        DebugLog = other.DebugLog;
        Breakpointing = other.Breakpointing;
        IndentSpacing = other.IndentSpacing;
    }

    internal DebuggableEmitter GetEmitterWithDifferentGenerator(IOpCodeEmitter generator)
    {
        return new DebuggableEmitter(this, generator);
    }

    // ReSharper disable once UnusedMember.Local
    private void TryLogDebug(string logSource, string message)
    {
        _accessor.Logger?.LogDebug(logSource, message);
    }

    /// <summary>
    /// Write any starter logging and initialize before the first log.
    /// </summary>
    protected virtual void Initialize()
    {
        IReflectionToolsLogger? reflectionToolsLogger = _accessor.Logger;
        MethodDefinition? def = null;
        if (DebugLog && reflectionToolsLogger != null)
        {
            if (Method is not null)
            {
                string txt;
                try
                {
                    txt = ".method " + _accessor.Formatter.Format(Method!);
                }
                // type builders, etc can throw NotSupportedException in a lot of cases
                catch (NotSupportedException)
                {
                    def = Method is ConstructorBuilder { DeclaringType: not null } ctor ? new MethodDefinition(ctor.DeclaringType, ctor.IsStatic) : new MethodDefinition(Method.Name);
                    try
                    {
                        txt = ".method " + _accessor.Formatter.Format(def) + " {in type builder}";
                    }
                    catch (Exception ex)
                    {
                        txt = ".method " + def + " {in type builder}";
                        if (Accessor.LogErrorMessages)
                        {
                            _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format type: {def}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    txt = ".method " + Method;
                    if (Accessor.LogErrorMessages)
                    {
                        _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format method: {Method}.");
                    }
                }

                reflectionToolsLogger.LogDebug(LogSource, txt);
            }
            else
            {
                string fmt;
                try
                {
                    fmt = _accessor.Formatter.Format(typeof(ILGenerator));
                }
                catch (Exception ex)
                {
                    fmt = nameof(ILGenerator);
                    if (Accessor.LogErrorMessages)
                    {
                        _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, "Failed to format type: ILGenerator.");
                    }
                }
                reflectionToolsLogger.LogDebug(LogSource, ".method <" + fmt + ">");
            }
            if (Breakpointing)
                reflectionToolsLogger.LogDebug(LogSource, " (with breakpointing)");
        }
        if (Breakpointing)
        {
            Generator.Emit(OpCodes.Ldstr, LogSource);
            if (Method is not null)
            {
                string txt;
                try
                {
                    txt = ".method " + _accessor.Formatter.Format(Method!);
                }
                // type builders, etc can throw NotSupportedException in a lot of cases
                catch (NotSupportedException)
                {
                    def = Method is ConstructorBuilder { DeclaringType: not null } ctor ? new MethodDefinition(ctor.DeclaringType, ctor.IsStatic) : new MethodDefinition(Method.Name);
                    try
                    {
                        txt = ".method " + _accessor.Formatter.Format(def) + " {in type builder}";
                    }
                    catch (Exception ex)
                    {
                        txt = ".method " + def + " {in type builder}";
                        if (Accessor.LogErrorMessages)
                        {
                            _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format type: {def}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    txt = ".method " + Method;
                    if (Accessor.LogErrorMessages)
                    {
                        _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format method: {Method}.");
                    }
                }
                Generator.Emit(OpCodes.Ldstr, txt);
            }
            else
            {
                string fmt;
                try
                {
                    fmt = _accessor.Formatter.Format(typeof(ILGenerator));
                }
                catch (Exception ex)
                {
                    fmt = nameof(ILGenerator);
                    if (Accessor.LogErrorMessages)
                    {
                        _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, "Failed to format type: ILGenerator.");
                    }
                }
                Generator.Emit(OpCodes.Ldstr, ".method <" + fmt + ">");
            }

            if (LogMethod != null)
                Generator.Emit(_accessor.GetCallRuntime(LogMethod), LogMethod);
        }
    }

    /// <summary>
    /// Run <see cref="Initialize"/> if it hasn't already been ran.
    /// </summary>
    protected void CheckInit()
    {
        if (_init) return;
        Initialize();
        _init = true;
        if (_owner != null)
            _owner._init = true;
    }
    private string GetCommentStarter()
    {
#if NET40_OR_GREATER || !NETFRAMEWORK
        try
        {
            return new string(' ', LogIndent * IndentSpacing + ILOffset.ToString("X5").Length + 3);
        }
        catch (NotSupportedException)
        {
            return new string(' ', LogIndent * IndentSpacing + 8);
        }
#else
        return new string(' ', LogIndent * IndentSpacing + 8);
#endif
    }
    private string GetLogStarter()
    {
#if NET40_OR_GREATER || !NETFRAMEWORK
        try
        {
            return "IL" + ILOffset.ToString("X5") + " ";
        }
        catch (NotSupportedException)
        {
            return "IL" + new string(' ', LogIndent * IndentSpacing + 6);
        }
#else
        return _ilOffsetRepl ??= "IL" + new string(' ', LogIndent * IndentSpacing + 6);
#endif
    }

    /// <summary>
    /// Log a comment/verbose message.
    /// </summary>
    protected virtual void Log(string txt)
    {
        string msg;
        bool comment = txt.StartsWith("//", StringComparison.Ordinal);
        if (comment)
        {
            msg = GetCommentStarter() + txt;
        }
        else
        {
            msg = GetLogStarter() + (LogIndent <= 0 ? string.Empty : new string(' ', LogIndent * IndentSpacing)) + txt;
        }
        if (DebugLog)
            _accessor.Logger?.LogDebug(LogSource, msg);
        if (Breakpointing)
        {
            Generator.Emit(OpCodes.Ldstr, LogSource);
            Generator.Emit(OpCodes.Ldstr, msg);
            Generator.Emit(_accessor.GetCallRuntime(LogMethod!), LogMethod!);
        }
    }

    /// <summary>
    /// Log the given <see cref="OpCode"/> and it's operand if it exists.
    /// </summary>
    protected virtual void Log(OpCode code, object? operand)
    {
        CheckInit();

        string msg = GetLogStarter() + (LogIndent <= 0 ? string.Empty : new string(' ', LogIndent * IndentSpacing));
        try
        {
            msg += _accessor.Formatter.Format(code, operand, OpCodeFormattingContext.List);
        }
        catch (Exception ex)
        {
            if (Accessor.LogErrorMessages)
            {
                _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format opcode: {code} {operand}.");
            }
        }
        
        if (DebugLog)
            _accessor.Logger?.LogDebug(LogSource, msg);
        if (Breakpointing)
        {
            if (!_lastWasPrefix)
            {
                Generator.Emit(OpCodes.Ldstr, LogSource);
                Generator.Emit(OpCodes.Ldstr, msg);
                Generator.Emit(_accessor.GetCallRuntime(LogMethod!), LogMethod!);
            }
            else
            {
                (_prefixQueue ??= new List<string>()).Add(msg);
            }
        }
        _lastWasPrefix = code.OpCodeType == OpCodeType.Prefix;
    }

    private void LogPrefixQueue()
    {
        if (_lastWasPrefix || !Breakpointing || _prefixQueue == null)
            return;

        for (int i = 0; i < _prefixQueue.Count; ++i)
        {
            string msg = _prefixQueue[i];
            Generator.Emit(OpCodes.Ldstr, LogSource);
            Generator.Emit(OpCodes.Ldstr, msg);
            Generator.Emit(_accessor.GetCallRuntime(LogMethod!), LogMethod!);
        }

        _prefixQueue.Clear();
    }

    /// <inheritdoc />
    public virtual void BeginCatchBlock(Type? exceptionType)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            --LogIndent;
            Log("}");
            if (exceptionType == null)
            {
                Log(".filter handler {");
            }
            else
            {
                try
                {
                    Log(".catch (" + _accessor.Formatter.Format(exceptionType) + ") {");
                }
                catch (Exception ex)
                {
                    Log(".catch (" + exceptionType + ") {");
                    if (Accessor.LogErrorMessages)
                    {
                        _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format type: {exceptionType}.");
                    }
                }
            }
            ++LogIndent;
        }
        Generator.BeginCatchBlock(exceptionType);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void BeginExceptFilterBlock()
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            --LogIndent;
            Log("}");
            Log(".exception filter {");
            ++LogIndent;
        }

        Generator.BeginExceptFilterBlock();
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual Label? BeginExceptionBlock()
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(".try {");
        }

        ++LogIndent;
        Label? lbl = Generator.BeginExceptionBlock();
        LogPrefixQueue();
        return lbl;
    }

    /// <inheritdoc />
    public virtual void BeginFaultBlock()
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            --LogIndent;
            Log("}");
            Log(".fault {");
            ++LogIndent;
        }

        Generator.BeginFaultBlock();
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void BeginFinallyBlock()
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            --LogIndent;
            Log("}");
            Log(".finally {");
            ++LogIndent;
        }

        Generator.BeginFinallyBlock();
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void BeginScope()
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(".scope {");
        }

        ++LogIndent;
        Generator.BeginScope();
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Comment(string comment)
    {
        CheckInit();
        if (DebugLog)
        {
            _accessor.Logger?.LogDebug(LogSource, GetCommentStarter() + "// " + comment);
        }
    }
    /// <inheritdoc />
    public virtual LocalBuilder DeclareLocal(Type localType) => DeclareLocal(localType, false);

    /// <inheritdoc />
    public virtual LocalBuilder DeclareLocal(Type localType, bool pinned)
    {
        LocalBuilder lcl = Generator.DeclareLocal(localType, pinned);
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            try
            {
                Log("// Declared local: # " + lcl.LocalIndex + " " + _accessor.Formatter.Format(lcl.LocalType ?? localType) + " (Pinned: " + lcl.IsPinned + ")");
            }
            catch (Exception ex)
            {
                Log("// Declared local: # " + lcl.LocalIndex + " " + (lcl.LocalType ?? localType) + " (Pinned: " + lcl.IsPinned + ")");
                if (Accessor.LogErrorMessages)
                {
                    _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format type: {lcl.LocalType ?? localType}.");
                }
            }
        }

        return lcl;
    }

    /// <inheritdoc />
    public virtual Label DefineLabel()
    {
        Label lbl = Generator.DefineLabel();
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            try
            {
                Log("// Defined label: " + _accessor.Formatter.Format(lbl));
            }
            catch (Exception ex)
            {
                Log("// Defined label: " + lbl);
                if (Accessor.LogErrorMessages)
                {
                    _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format label: {lbl}.");
                }
            }
        }

        return lbl;
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, null);
        }

        Generator.Emit(opcode);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, byte arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, double arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, float arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, int arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, long arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, sbyte arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, short arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, string str)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, str);
        }

        Generator.Emit(opcode, str);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, ConstructorInfo con)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, con);
        }

        Generator.Emit(opcode, con);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, Label label)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, label);
        }

        Generator.Emit(opcode, label);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, Label[] labels)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, labels);
        }

        Generator.Emit(opcode, labels);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, LocalBuilder local)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, local);
        }

        Generator.Emit(opcode, local);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, SignatureHelper signature)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, signature);
        }

        Generator.Emit(opcode, signature);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, FieldInfo field)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, field);
        }

        Generator.Emit(opcode, field);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, MethodInfo meth)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, meth);
        }

        Generator.Emit(opcode, meth);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void Emit(OpCode opcode, Type cls)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, cls);
        }

        Generator.Emit(opcode, cls);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, methodInfo);
        }

        Generator.EmitCall(opcode, methodInfo, optionalParameterTypes);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[]? optionalParameterTypes)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, null);
        }

        Generator.EmitCalli(opcode, callingConvention, returnType, parameterTypes, optionalParameterTypes);
        LogPrefixQueue();
    }

#if !NETSTANDARD || NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public virtual void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, null);
        }

        Generator.EmitCalli(opcode, unmanagedCallConv, returnType, parameterTypes);
        LogPrefixQueue();
    }
#endif

    /// <inheritdoc />
    public virtual void EmitWriteLine(string value)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("// Write Line: \"" + value + "\".");
        }

        Generator.EmitWriteLine(value);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void EmitWriteLine(LocalBuilder localBuilder)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("// Write Line: lcl." + localBuilder.LocalIndex);
        }

        Generator.EmitWriteLine(localBuilder);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void EmitWriteLine(FieldInfo fld)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            try
            {
                Log("// Write Line: Field " + _accessor.Formatter.Format(fld));
            }
            catch (Exception ex)
            {
                Log("// Write Line: Field " + fld);
                if (Accessor.LogErrorMessages)
                {
                    _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format field: {fld}.");
                }
            }
        }

        Generator.EmitWriteLine(fld);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void EndExceptionBlock()
    {
        --LogIndent;
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("}");
        }

        Generator.EndExceptionBlock();
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void EndScope()
    {
        --LogIndent;
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("}");
        }

        Generator.EndScope();
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void MarkLabel(Label loc)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            try
            {
#if NET40_OR_GREATER
                Log(".label " + _accessor.Formatter.Format(loc) + ": @ IL" + ILOffset.ToString("X") + ".");
#else
                Log(".label " + _accessor.Formatter.Format(loc) + ".");
#endif
            }
            catch (Exception ex)
            {
#if NET40_OR_GREATER
                Log(".label " + loc + ": @ IL" + ILOffset.ToString("X") + ".");
#else
                Log(".label " + loc + ".");
#endif
                if (Accessor.LogErrorMessages)
                {
                    _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format label: {loc}.");
                }
            }
        }
        Generator.MarkLabel(loc);
        LogPrefixQueue();
    }

#if NETFRAMEWORK
    /// <inheritdoc />
    public virtual void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log($"// Sequence Point: (line, column) Start: {startLine}, {startColumn}, End: {endLine}, {endColumn}.");
        }
        Generator.MarkSequencePoint(document, startLine, startColumn, endLine, endColumn);
    }
#endif

    /// <inheritdoc />
    public virtual void ThrowException(Type excType)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            try
            {
                Log($"// Throw Exception {_accessor.Formatter.Format(excType)}.");
            }
            catch (Exception ex)
            {
                Log($"// Throw Exception {excType}.");
                if (Accessor.LogErrorMessages)
                {
                    _accessor.Logger?.LogError(nameof(DebuggableEmitter), ex, $"Failed to format type: {excType}.");
                }
            }
        }
        Generator.ThrowException(excType);
        LogPrefixQueue();
    }

    /// <inheritdoc />
    public virtual void UsingNamespace(string usingNamespace)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log($".using \"{usingNamespace}\";");
        }

        Generator.UsingNamespace(usingNamespace);
        LogPrefixQueue();
    }

#if NETFRAMEWORK
    void _ILGenerator.GetIDsOfNames(ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        => Generator.GetIDsOfNames(ref riid, rgszNames, cNames, lcid, rgDispId);
    void _ILGenerator.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        => Generator.GetTypeInfo(iTInfo, lcid, ppTInfo);
    void _ILGenerator.GetTypeInfoCount(out uint pcTInfo)
        => Generator.GetTypeInfoCount(out pcTInfo);
    void _ILGenerator.Invoke(uint dispIdMember, ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        => Generator.Invoke(dispIdMember, ref riid, lcid, wFlags, pDispParams, pVarResult, pExcepInfo, puArgErr);
#endif
}
