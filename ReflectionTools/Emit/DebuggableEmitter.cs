using DanielWillett.ReflectionTools.Formatting;
using System;
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
    private bool _init;
    private bool _lastWasPrefix;
    private readonly IAccessor _accessor;
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

    /// <inheritdoc />
    public string LogSource
    {
        get => _logSource;
        set => _logSource = value ?? nameof(DebuggableEmitter);
    }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from a <see cref="MethodBuilder"/>.
    /// </summary>
    public DebuggableEmitter(MethodBuilder method, IAccessor? accessor = null) : this(method.GetILGenerator().AsEmitter(), method, accessor) { }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from a <see cref="DynamicMethod"/>.
    /// </summary>
    public DebuggableEmitter(DynamicMethod method, IAccessor? accessor = null) : this(method.GetILGenerator().AsEmitter(), method, accessor) { }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from an <see cref="IOpCodeEmitter"/> and <see cref="MethodBase"/> used for logging.
    /// </summary>
    public DebuggableEmitter(IOpCodeEmitter generator, MethodBase? method, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        _accessor = accessor;
        if (generator.GetType() == typeof(DebuggableEmitter))
            generator = ((DebuggableEmitter)generator).Generator;
        Generator = generator;
        Method = method;
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
        if (DebugLog && reflectionToolsLogger != null)
        {
            reflectionToolsLogger.LogDebug(LogSource, ".method " + _accessor.Formatter.Format(Method!));
            if (Breakpointing)
                reflectionToolsLogger.LogDebug(LogSource, " (with breakpointing)");
        }
        if (Breakpointing)
        {
            Generator.Emit(OpCodes.Ldstr, LogSource);
            Generator.Emit(OpCodes.Ldstr, ".method " + _accessor.Formatter.Format(Method!));
            if (LogMethod != null)
                Generator.Emit(_accessor.GetCallRuntime(LogMethod), LogMethod);
        }
    }

    /// <summary>
    /// Run <see cref="Initialize"/> if it hasn't already been ran.
    /// </summary>
    protected void CheckInit()
    {
        if (_init || Method is null) return;
        Initialize();
        _init = true;
    }
    private string GetCommentStarter()
    {
#if NET40_OR_GREATER || !NETFRAMEWORK
        return new string(' ', LogIndent + ILOffset.ToString("X5").Length + 3);
#else
        return new string(' ', LogIndent + 8);
#endif
    }
    private string GetLogStarter()
    {
#if NET40_OR_GREATER || !NETFRAMEWORK
        return "IL" + ILOffset.ToString("X5") + " ";
#else
        return _ilOffsetRepl ??= "IL" + new string(' ', LogIndent + 6);
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
            msg = GetLogStarter() + (LogIndent <= 0 ? string.Empty : new string(' ', LogIndent)) + txt;
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
        string msg = GetLogStarter() + (LogIndent <= 0 ? string.Empty : new string(' ', LogIndent)) + _accessor.Formatter.Format(code, operand, OpCodeFormattingContext.List);
        
        if (DebugLog)
            _accessor.Logger?.LogDebug(LogSource, msg);
        if (Breakpointing && !_lastWasPrefix)
        {
            Generator.Emit(OpCodes.Ldstr, LogSource);
            Generator.Emit(OpCodes.Ldstr, msg);
            Generator.Emit(_accessor.GetCallRuntime(LogMethod!), LogMethod!);
        }
        _lastWasPrefix = code.OpCodeType == OpCodeType.Prefix;
    }

    /// <inheritdoc />
    public virtual void BeginCatchBlock(Type exceptionType)
    {
        --LogIndent;
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("}");
            Log(".catch (" + _accessor.Formatter.Format(exceptionType) + ") {");
        }
        ++LogIndent;
        Generator.BeginCatchBlock(exceptionType);
    }

    /// <inheritdoc />
    public virtual void BeginExceptFilterBlock()
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(".try (filter) {");
        }

        ++LogIndent;
        Generator.BeginExceptFilterBlock();
    }

    /// <inheritdoc />
    public virtual void BeginExceptionBlock()
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(".try {");
        }

        ++LogIndent;
        Generator.BeginExceptionBlock();
    }

    /// <inheritdoc />
    public virtual void BeginFaultBlock()
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(".fault {");
        }

        ++LogIndent;
        Generator.BeginFaultBlock();
    }

    /// <inheritdoc />
    public virtual void BeginFinallyBlock()
    {
        --LogIndent;
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("}");
            Log(".finally {");
        }

        ++LogIndent;
        Generator.BeginFinallyBlock();
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
            Log("// Declared local: # " + lcl.LocalIndex + " " + _accessor.Formatter.Format(lcl.LocalType ?? localType) + " (Pinned: " + lcl.IsPinned + ")");
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
            Log("// Defined label: " + _accessor.Formatter.Format(lbl));
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
    }

    /// <inheritdoc />
    public virtual void EmitWriteLine(FieldInfo fld)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("// Write Line: Field " + _accessor.Formatter.Format(fld));
        }

        Generator.EmitWriteLine(fld);
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
    }

    /// <inheritdoc />
    public virtual void MarkLabel(Label loc)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
#if NET40_OR_GREATER
            Log(".label " + _accessor.Formatter.Format(loc) + ": @ IL" + ILOffset.ToString("X") + ".");
#else
            Log(".label " + _accessor.Formatter.Format(loc) + ".");
#endif
        }
        Generator.MarkLabel(loc);
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
            Log($"// Throw Exception {_accessor.Formatter.Format(excType)}.");
        }
        Generator.ThrowException(excType);
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
