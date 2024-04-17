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
    private static MethodInfo? _logMethod;
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
    public DebuggableEmitter(MethodBuilder method) : this(method.GetILGenerator().AsEmitter(), method) { }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from a <see cref="DynamicMethod"/>.
    /// </summary>
    public DebuggableEmitter(DynamicMethod method) : this(method.GetILGenerator().AsEmitter(), method) { }

    /// <summary>
    /// Create a <see cref="DebuggableEmitter"/> from an <see cref="IOpCodeEmitter"/> and <see cref="MethodBase"/> used for logging.
    /// </summary>
    public DebuggableEmitter(IOpCodeEmitter generator, MethodBase? method)
    {
        if (generator.GetType() == typeof(DebuggableEmitter))
            generator = ((DebuggableEmitter)generator).Generator;
        Generator = generator;
        Method = method;
    }
    private static void TryLogDebug(string logSource, string message)
    {
        Accessor.Logger?.LogDebug(logSource, message);
    }
    private void CheckInit()
    {
        if (_init || Method is null) return;
        _logMethod ??= Accessor.GetMethod(TryLogDebug);
        IReflectionToolsLogger? reflectionToolsLogger = Accessor.Logger;
        if (DebugLog && reflectionToolsLogger != null)
        {
            reflectionToolsLogger.LogDebug(LogSource, ".method " + Accessor.Formatter.Format(Method));
            if (Breakpointing)
                reflectionToolsLogger.LogDebug(LogSource, " (with breakpointing)");
        }
        if (Breakpointing)
        {
            Generator.Emit(OpCodes.Ldstr, LogSource);
            Generator.Emit(OpCodes.Ldstr, ".method " + Accessor.Formatter.Format(Method));
            if (_logMethod != null)
                Generator.Emit(_logMethod.GetCallRuntime(), _logMethod);
        }
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
    private void Log(string txt)
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
            Accessor.Logger?.LogDebug(LogSource, msg);
        if (Breakpointing)
        {
            Generator.Emit(OpCodes.Ldstr, LogSource);
            Generator.Emit(OpCodes.Ldstr, msg);
            Generator.Emit(_logMethod!.GetCallRuntime(), _logMethod!);
        }
    }
    private void Log(OpCode code, object? operand)
    {
        CheckInit();
        string msg = GetLogStarter() + (LogIndent <= 0 ? string.Empty : new string(' ', LogIndent)) + Accessor.Formatter.Format(code, operand, OpCodeFormattingContext.List);
        
        if (DebugLog)
            Accessor.Logger?.LogDebug(LogSource, msg);
        if (Breakpointing && !_lastWasPrefix)
        {
            Generator.Emit(OpCodes.Ldstr, LogSource);
            Generator.Emit(OpCodes.Ldstr, msg);
            Generator.Emit(_logMethod!.GetCallRuntime(), _logMethod!);
        }
        _lastWasPrefix = code.OpCodeType == OpCodeType.Prefix;
    }

    /// <inheritdoc />
    public void BeginCatchBlock(Type exceptionType)
    {
        --LogIndent;
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("}");
            Log(".catch (" + Accessor.Formatter.Format(exceptionType) + ") {");
        }
        ++LogIndent;
        Generator.BeginCatchBlock(exceptionType);
    }

    /// <inheritdoc />
    public void BeginExceptFilterBlock()
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
    public void BeginExceptionBlock()
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
    public void BeginFaultBlock()
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
    public void BeginFinallyBlock()
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
    public void BeginScope()
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
    public void Comment(string comment)
    {
        CheckInit();
        if (DebugLog)
        {
            Accessor.Logger?.LogDebug(LogSource, GetCommentStarter() + "// " + comment);
        }
    }
    /// <inheritdoc />
    public LocalBuilder DeclareLocal(Type localType) => DeclareLocal(localType, false);

    /// <inheritdoc />
    public LocalBuilder DeclareLocal(Type localType, bool pinned)
    {
        LocalBuilder lcl = Generator.DeclareLocal(localType, pinned);
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("// Declared local: # " + lcl.LocalIndex + " " + Accessor.Formatter.Format(lcl.LocalType ?? localType) + " (Pinned: " + lcl.IsPinned + ")");
        }

        return lcl;
    }

    /// <inheritdoc />
    public Label DefineLabel()
    {
        Label lbl = Generator.DefineLabel();
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("// Defined label: " + Accessor.Formatter.Format(lbl));
        }

        return lbl;
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, null);
        }

        Generator.Emit(opcode);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, byte arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, double arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, float arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, int arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, long arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, sbyte arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, short arg)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, arg);
        }

        Generator.Emit(opcode, arg);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, string str)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, str);
        }

        Generator.Emit(opcode, str);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, ConstructorInfo con)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, con);
        }

        Generator.Emit(opcode, con);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, Label label)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, label);
        }

        Generator.Emit(opcode, label);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, Label[] labels)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, labels);
        }

        Generator.Emit(opcode, labels);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, LocalBuilder local)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, local);
        }

        Generator.Emit(opcode, local);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, SignatureHelper signature)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, signature);
        }

        Generator.Emit(opcode, signature);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, FieldInfo field)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, field);
        }

        Generator.Emit(opcode, field);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, MethodInfo meth)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, meth);
        }

        Generator.Emit(opcode, meth);
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, Type cls)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, cls);
        }

        Generator.Emit(opcode, cls);
    }

    /// <inheritdoc />
    public void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log(opcode, methodInfo);
        }

        Generator.EmitCall(opcode, methodInfo, optionalParameterTypes);
    }

    /// <inheritdoc />
    public void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[]? optionalParameterTypes)
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
    public void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
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
    public void EmitWriteLine(string value)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("// Write Line: \"" + value + "\".");
        }

        Generator.EmitWriteLine(value);
    }

    /// <inheritdoc />
    public void EmitWriteLine(LocalBuilder localBuilder)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("// Write Line: lcl." + localBuilder.LocalIndex);
        }

        Generator.EmitWriteLine(localBuilder);
    }

    /// <inheritdoc />
    public void EmitWriteLine(FieldInfo fld)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log("// Write Line: Field " + Accessor.Formatter.Format(fld));
        }

        Generator.EmitWriteLine(fld);
    }

    /// <inheritdoc />
    public void EndExceptionBlock()
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
    public void EndScope()
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
    public void MarkLabel(Label loc)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
#if NET40_OR_GREATER
            Log(".label " + Accessor.Formatter.Format(loc) + ": @ IL" + ILOffset.ToString("X") + ".");
#else
            Log(".label " + Accessor.Formatter.Format(loc) + ".");
#endif
        }
        Generator.MarkLabel(loc);
    }

#if NETFRAMEWORK
    /// <inheritdoc />
    public void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
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
    public void ThrowException(Type excType)
    {
        if (DebugLog || Breakpointing)
        {
            CheckInit();
            Log($"// Throw Exception {Accessor.Formatter.Format(excType)}.");
        }
        Generator.ThrowException(excType);
    }

    /// <inheritdoc />
    public void UsingNamespace(string usingNamespace)
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
