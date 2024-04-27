using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
#if NETFRAMEWORK
using System.Diagnostics.SymbolStore;
#endif
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Used for logging in a transpiler, along with finding members.
/// </summary>
public class TranspileContext : IOpCodeEmitter, IEnumerable<CodeInstruction>
{
    private static ITranspileContextLogger _defaultTranspileLogger = new DefaultTranspileContextLogger();
    private ITranspileContextLogger _transpileLogger = (ITranspileContextLogger)_defaultTranspileLogger.Clone();

    private readonly IAccessor _accessor;
    private readonly IEnumerable<CodeInstruction>? _originalInstructions;
    internal readonly List<CodeInstruction> Instructions;
    private readonly List<Label> _nextLabels = [];
    private readonly List<ExceptionBlock> _nextBlocks = [];
    private readonly ILGenerator _il;
    private static Type[]? _msgTypeArr;
    private int _caretIndex;
    private int _lastStackSizeIs0;
    private int _listVersion;

    /// <summary>
    /// The default log formatter to use for new <see cref="TranspileContext"/> objects.
    /// </summary>
    /// <remarks>By assigning a value to this property, you transfer ownership of the object to this class, meaning it shouldn't be used or disposed outside this class at all.</remarks>
    public static ITranspileContextLogger DefaultTranspileLogger
    {
        get => _defaultTranspileLogger;
        set
        {
            ITranspileContextLogger old = Interlocked.Exchange(ref _defaultTranspileLogger, value);
            if (!ReferenceEquals(old, value) && old is IDisposable disp)
                disp.Dispose();
        }
    }

    /// <summary>
    /// Formatter to use for formatting transpile loggers.
    /// </summary>
    /// <remarks>You are responsible for cleaning up this object after use, as one object is expected to be used for multiple <see cref="TranspileContext"/> objects.</remarks>
    public ITranspileContextLogger TranspileLogger
    {
        get => _transpileLogger;
        set => _transpileLogger = value ?? (ITranspileContextLogger)_defaultTranspileLogger.Clone();
    }

    /// <summary>
    /// Used for logging in a transpiler, along with finding members.
    /// </summary>
    /// <param name="method">Get by injecting a parameter of type <see cref="MethodBase"/> into the transpiler method.</param>
    /// <param name="generator">Get by injecting a parameter of type <see cref="ILGenerator"/> into the transpiler method.</param>
    /// <param name="instructions">Get by injecting the enumerable of code instructions into the transpiler method.</param>
    /// <param name="accessor">The instance of <see cref="IAccessor"/> to use for accessing members, defaulting to <see cref="Accessor.Active"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> and/or <paramref name="generator"/> are null.</exception>
    public TranspileContext(MethodBase method, ILGenerator generator, IEnumerable<CodeInstruction>? instructions = null, IAccessor? accessor = null)
    {
        _il = generator ?? throw new ArgumentNullException(nameof(generator));
        Method = method ?? throw new ArgumentNullException(nameof(method));
        _accessor = accessor ?? Accessor.Active;
        Instructions = instructions == null ? new List<CodeInstruction>(0) : new List<CodeInstruction>(instructions);
        _originalInstructions = instructions;
        _caretIndex = 0;
    }

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">ILOffset not supported in TranspileContext.</exception>
    public int ILOffset
    {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
        [DoesNotReturn]
#endif
        get { throw new NotSupportedException("ILOffset not supported in TranspileContext."); }
    }

    /// <summary>
    /// Number of instructions in the list.
    /// </summary>
    public int Count => Instructions.Count;

    /// <summary>
    /// The method that's being transpiled.
    /// </summary>
    /// <remarks>Get by injecting a parameter of type <see cref="MethodBase"/> into the transpiler method.</remarks>
    public MethodBase Method { get; }

    /// <summary>
    /// Index in the instruction list where instructions are emitted.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Caret index must be &gt;= 0 and &lt;= instruction count.</exception>
    public int CaretIndex
    {
        get => _caretIndex;
        set
        {
            if (value > Instructions.Count || value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Caret index must be >= 0 and <= instruction count.");

            AssertBlocksAndLabelsApplied();
            _caretIndex = value;
        }
    }

    /// <summary>
    /// A reference to the instruction the caret is currently on.
    /// </summary>
    public CodeInstruction Instruction
    {
        get
        {
            if (_caretIndex > Instructions.Count || _caretIndex < 0)
                throw new InvalidOperationException("Caret is not on an instruction.");

            return Instructions[_caretIndex];
        }
    }
    
    /// <summary>
    /// Move the caret to the next instruction in the instruction list.
    /// </summary>
    /// <returns><see langword="true"/> if the caret is on an instruction, otherwise false.</returns>
    public bool MoveNext()
    {
        if (_caretIndex + 1 >= Instructions.Count)
            return false;

        ++_caretIndex;
        return true;
    }

    /// <summary>
    /// Get the code instruction at the given index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public CodeInstruction this[int index] => Instructions[index];

    /// <summary>
    /// Remove <paramref name="count"/> instructions from the instruction list, returning their label and block information.
    /// </summary>
    public BlockInfo Remove(int count)
    {
        if (count == 0)
            return new BlockInfo(Array.Empty<InstructionBlockInfo>());
        
        if (count < 0 || Instructions == null || _caretIndex + count > Instructions.Count)
            throw new ArgumentOutOfRangeException(nameof(count));

        InstructionBlockInfo[] instructions = new InstructionBlockInfo[count];
        for (int i = _caretIndex; i < _caretIndex + count; ++i)
        {
            CodeInstruction codeInstruction = Instructions[i];
            instructions[i - _caretIndex] = new InstructionBlockInfo(codeInstruction.opcode,
                codeInstruction.operand,
                codeInstruction.labels.Count == 0 ? Array.Empty<Label>() : codeInstruction.labels.ToArray(),
                codeInstruction.blocks.Count == 0 ? Array.Empty<ExceptionBlock>() : codeInstruction.blocks.ToArray()
            );
        }

        Instructions.RemoveRange(_caretIndex, count);

        return new BlockInfo(instructions);
    }

    /// <summary>
    /// Fail the transpiler because a member couldn't be found and log an error to <see cref="TranspileLogger"/>. 
    /// </summary>
    /// <param name="missingMember">A definition of the original member that couldn't be found.</param>
    /// <returns>The original method's instructions.</returns>
    public IEnumerable<CodeInstruction> Fail(IMemberDefinition missingMember)
    {
        ITranspileContextLogger logger = _transpileLogger;
        if (logger.Enabled)
            logger.LogFailure(this, missingMember, _accessor);
        return _originalInstructions ?? Array.Empty<CodeInstruction>();
    }

    /// <summary>
    /// Fail the transpiler with a generic message and log an error to <see cref="TranspileLogger"/>. 
    /// </summary>
    /// <param name="message">A generic human-readable message describing what went wrong.</param>
    /// <returns>The original method's instructions.</returns>
    public IEnumerable<CodeInstruction> Fail(string message)
    {
        ITranspileContextLogger logger = _transpileLogger;
        if (logger.Enabled)
            logger.LogFailure(this, message, _accessor);
        return _originalInstructions ?? Array.Empty<CodeInstruction>();
    }

    /// <summary>
    /// Log debug information to <see cref="TranspileLogger"/>. 
    /// </summary>
    /// <param name="message">A generic human-readable message describing the event.</param>
    public void LogDebug(string message)
    {
        ITranspileContextLogger logger = _transpileLogger;
        if (logger.Enabled)
            logger.LogDebug(this, message, _accessor);
    }

    /// <summary>
    /// Log information to <see cref="TranspileLogger"/>. 
    /// </summary>
    /// <param name="message">A generic human-readable message describing the event.</param>
    public void LogInfo(string message)
    {
        ITranspileContextLogger logger = _transpileLogger;
        if (logger.Enabled)
            logger.LogInfo(this, message, _accessor);
    }

    /// <summary>
    /// Log a warning to <see cref="TranspileLogger"/>. 
    /// </summary>
    /// <param name="message">A generic human-readable message describing the event.</param>
    public void LogWarning(string message)
    {
        ITranspileContextLogger logger = _transpileLogger;
        if (logger.Enabled)
            logger.LogWarning(this, message, _accessor);
    }

    /// <summary>
    /// Log an error to <see cref="TranspileLogger"/>. 
    /// </summary>
    /// <param name="message">A generic human-readable message describing the event.</param>
    public void LogError(string message)
    {
        ITranspileContextLogger logger = _transpileLogger;
        if (logger.Enabled)
            logger.LogError(this, message, _accessor);
    }

    /// <summary>
    /// Log an error to <see cref="TranspileLogger"/>. 
    /// </summary>
    /// <param name="message">A generic human-readable message describing the event.</param>
    public void LogError(Exception ex, string message)
    {
        ITranspileContextLogger logger = _transpileLogger;
        if (logger.Enabled)
            logger.LogError(this, ex, message, _accessor);
    }

    /// <summary>
    /// Not supported by <see cref="TranspileContext"/>, will be ignored.
    /// </summary>
    /// <remarks>Removed when not compiled for <c>DEBUG</c> mode.</remarks>
    [Conditional("DEBUG")]
    public void Comment(string comment) { }

    void IOpCodeEmitter.Comment(string comment) { }

    /// <inheritdoc />
    public void BeginCatchBlock(Type exceptionType)
    {
        _nextBlocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, exceptionType));
    }

    /// <summary>
    /// Applies any marked labels to the instruction at index <see cref="CaretIndex"/>. This happens automatically when you emit a new instruction.
    /// </summary>
    public void ApplyBlocksAndLabels()
    {
        if (Instructions.Count == 0)
            throw new InvalidOperationException("No instructions loaded.");

        CodeInstruction instruction = Instructions[_caretIndex];
        foreach (Label lbl in _nextLabels)
            instruction.labels.Add(lbl);

        _nextLabels.Clear();

        foreach (ExceptionBlock block in _nextBlocks)
            instruction.blocks.Add(block);

        _nextBlocks.Clear();
    }
    private void AssertBlocksAndLabelsApplied()
    {
        if (_nextBlocks.Count != 0 || _nextLabels.Count != 0)
            throw new InvalidOperationException("Blocks and labels must be applied first, either by emitting an instruction or by calling ApplyBlocksAndLabels.");
    }

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Harmony does not support filter blocks.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    void IOpCodeEmitter.BeginExceptFilterBlock()
    {
        throw new NotSupportedException("Harmony does not support filter blocks.");
    }

    /// <inheritdoc />
    public Label? BeginExceptionBlock()
    {
        _nextBlocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
        return null;
    }

    /// <inheritdoc />
    public void BeginFaultBlock()
    {
        _nextBlocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFaultBlock));
    }

    /// <inheritdoc />
    public void BeginFinallyBlock()
    {
        _nextBlocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
    }

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Harmony does not support scopes.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    void IOpCodeEmitter.BeginScope()
    {
        throw new NotSupportedException("Harmony does not support scopes.");
    }

    /// <inheritdoc />
    public LocalBuilder DeclareLocal(Type localType) => _il.DeclareLocal(localType);

    /// <inheritdoc />
    public LocalBuilder DeclareLocal(Type localType, bool pinned) => _il.DeclareLocal(localType, pinned);

    /// <inheritdoc />
    public Label DefineLabel() => _il.DefineLabel();

    /// <summary>Puts the specified instruction onto the stream of instructions.</summary>
    /// <param name="instruction">The Microsoft Intermediate Language (MSIL) instruction to be put onto the stream, with an optional operand and label list.</param>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(CodeInstruction instruction)
    {
        Instructions.Insert(_caretIndex, instruction);
        ApplyBlocksAndLabels();
        ++_caretIndex;
        return instruction;
    }

    void IOpCodeEmitter.Emit(OpCode opcode)
    {
        Emit(new CodeInstruction(opcode));
    }

    void IOpCodeEmitter.Emit(OpCode opcode, byte arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    void IOpCodeEmitter.Emit(OpCode opcode, double arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    void IOpCodeEmitter.Emit(OpCode opcode, float arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    void IOpCodeEmitter.Emit(OpCode opcode, int arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    void IOpCodeEmitter.Emit(OpCode opcode, long arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    void IOpCodeEmitter.Emit(OpCode opcode, sbyte arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, short arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, string str)
    {
        Emit(new CodeInstruction(opcode, str));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, ConstructorInfo con)
    {
        Emit(new CodeInstruction(opcode, con));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, Label label)
    {
        Emit(new CodeInstruction(opcode, label));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, Label[] labels)
    {
        Emit(new CodeInstruction(opcode, labels));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, LocalBuilder local)
    {
        Emit(new CodeInstruction(opcode, local));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, SignatureHelper signature)
    {
        Emit(new CodeInstruction(opcode, signature));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, FieldInfo field)
    {
        Emit(new CodeInstruction(opcode, field));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, MethodInfo meth)
    {
        Emit(new CodeInstruction(opcode, meth));
    }

    /// <inheritdoc />
    void IOpCodeEmitter.Emit(OpCode opcode, Type cls)
    {
        Emit(new CodeInstruction(opcode, cls));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode)
    {
        return Emit(new CodeInstruction(opcode));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,byte)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, byte arg)
    {
        return Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,double)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, double arg)
    {
        return Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,float)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, float arg)
    {
        return Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,int)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, int arg)
    {
        return Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,long)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, long arg)
    {
        return Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,sbyte)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, sbyte arg)
    {
        return Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,short)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, short arg)
    {
        return Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,string)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, string str)
    {
        return Emit(new CodeInstruction(opcode, str));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,ConstructorInfo)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, ConstructorInfo con)
    {
        return Emit(new CodeInstruction(opcode, con));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,Label)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, Label label)
    {
        return Emit(new CodeInstruction(opcode, label));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,Label[])"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, Label[] labels)
    {
        return Emit(new CodeInstruction(opcode, labels));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,LocalBuilder)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, LocalBuilder local)
    {
        return Emit(new CodeInstruction(opcode, local));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,SignatureHelper)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, SignatureHelper signature)
    {
        return Emit(new CodeInstruction(opcode, signature));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,FieldInfo)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, FieldInfo field)
    {
        return Emit(new CodeInstruction(opcode, field));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,MethodInfo)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, MethodInfo meth)
    {
        return Emit(new CodeInstruction(opcode, meth));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,Type)"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction Emit(OpCode opcode, Type cls)
    {
        return Emit(new CodeInstruction(opcode, cls));
    }

    /// <inheritdoc cref="Emit(HarmonyLib.CodeInstruction)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(CodeInstruction instruction)
    {
        CodeInstruction current = Instructions[_caretIndex];
        Instructions.Insert(_caretIndex, instruction);
        ApplyBlocksAndLabels();
        ++_caretIndex;
        return instruction.WithStartBlocksFrom(current);
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode)
    {
        return EmitAbove(new CodeInstruction(opcode));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,byte)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, byte arg)
    {
        return EmitAbove(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,double)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, double arg)
    {
        return EmitAbove(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,float)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, float arg)
    {
        return EmitAbove(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,int)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, int arg)
    {
        return EmitAbove(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,long)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, long arg)
    {
        return EmitAbove(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,sbyte)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, sbyte arg)
    {
        return EmitAbove(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,short)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, short arg)
    {
        return EmitAbove(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,string)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, string str)
    {
        return EmitAbove(new CodeInstruction(opcode, str));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,ConstructorInfo)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, ConstructorInfo con)
    {
        return EmitAbove(new CodeInstruction(opcode, con));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,Label)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, Label label)
    {
        return EmitAbove(new CodeInstruction(opcode, label));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,Label[])"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, Label[] labels)
    {
        return EmitAbove(new CodeInstruction(opcode, labels));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,LocalBuilder)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, LocalBuilder local)
    {
        return EmitAbove(new CodeInstruction(opcode, local));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,SignatureHelper)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, SignatureHelper signature)
    {
        return EmitAbove(new CodeInstruction(opcode, signature));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,FieldInfo)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, FieldInfo field)
    {
        return EmitAbove(new CodeInstruction(opcode, field));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,MethodInfo)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, MethodInfo meth)
    {
        return EmitAbove(new CodeInstruction(opcode, meth));
    }

    /// <inheritdoc cref="IOpCodeEmitter.Emit(OpCode,Type)"/>
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitAbove(OpCode opcode, Type cls)
    {
        return EmitAbove(new CodeInstruction(opcode, cls));
    }

    /// <inheritdoc />
    /// <remarks>Using <paramref name="optionalParameterTypes"/> is not supported in <see cref="TranspileContext"/>.</remarks>
    /// <exception cref="NotSupportedException">Optional parameters are not supported.</exception>
    void IOpCodeEmitter.EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        if (optionalParameterTypes is { Length: > 0 })
            throw new NotSupportedException("Optional parameters are not supported.");

        Emit(new CodeInstruction(opcode, methodInfo));
    }

    /// <inheritdoc cref="IOpCodeEmitter.EmitCall(OpCode,MethodInfo,Type[])"/>
    /// <returns>An instance of the added instruction, allowing you to modify the reference to the one added to the instruction set.</returns>
    public CodeInstruction EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        if (optionalParameterTypes is { Length: > 0 })
            throw new NotSupportedException("Optional parameters are not supported.");

        return Emit(new CodeInstruction(opcode, methodInfo));
    }

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Calli is not supported.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    void IOpCodeEmitter.EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[]? optionalParameterTypes)
    {
        throw new NotSupportedException("Calli is not supported.");
    }

#if NETSTANDARD2_1_OR_GREATER || !NETSTANDARD
    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Calli is not supported.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    void IOpCodeEmitter.EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
    {
        throw new NotSupportedException("Calli is not supported.");
    }
#endif

    /// <inheritdoc />
    public void EmitWriteLine(string value)
    {
        MethodInfo method = new Action<string>(Console.WriteLine).Method;
        Emit(OpCodes.Ldstr, value);
        Emit(OpCodes.Call, method);
    }

    /// <inheritdoc cref="IOpCodeEmitter.EmitWriteLine(string)" />
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    public void EmitWriteLineAbove(string value)
    {
        MethodInfo method = new Action<string>(Console.WriteLine).Method;
        CodeInstruction st = Instructions[_caretIndex];
        Emit(OpCodes.Ldstr, value).WithStartBlocksFrom(st);
        Emit(OpCodes.Call, method);
    }

    private static MethodInfo GetConsoleWriteLineMethod(Type? type, out bool box)
    {
        MethodInfo? method = type == null ? null : typeof(Console).GetMethod(nameof(Console.WriteLine), BindingFlags.Public | BindingFlags.Static, null, [ type ], null);
        method ??= new Action<object>(Console.WriteLine).Method;
        ParameterInfo[] parameters = method.GetParameters();
        box = type != null && parameters.Length > 0 && !parameters[0].ParameterType.IsValueType && type.IsValueType;
        return method;
    }

    /// <inheritdoc />
    public void EmitWriteLine(LocalBuilder localBuilder)
    {
        MethodInfo method = GetConsoleWriteLineMethod(localBuilder.LocalType, out bool box);
        Emit(OpCodes.Ldloc, localBuilder);

        if (box)
            Emit(OpCodes.Box, localBuilder.LocalType!);

        Emit(OpCodes.Call, method);
    }

    /// <inheritdoc cref="IOpCodeEmitter.EmitWriteLine(LocalBuilder)" />
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    public void EmitWriteLineAbove(LocalBuilder localBuilder)
    {
        MethodInfo method = GetConsoleWriteLineMethod(localBuilder.LocalType, out bool box);
        CodeInstruction st = Instructions[_caretIndex];
        Emit(OpCodes.Ldloc, localBuilder).WithStartBlocksFrom(st);

        if (box)
            Emit(OpCodes.Box, localBuilder.LocalType!);

        Emit(OpCodes.Call, method);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Field must be static, or the same instance as the current method.</exception>
    public void EmitWriteLine(FieldInfo fld)
    {
        MethodInfo method = GetConsoleWriteLineMethod(fld.FieldType, out bool box);
        if (fld.IsStatic)
            Emit(OpCodes.Ldsfld, fld);
        else if (fld.DeclaringType == null || fld.DeclaringType != Method.DeclaringType || Method.IsStatic)
            throw new ArgumentException("Field must be static, or the same instance as the current method.", nameof(fld));
        else
        {
            Emit(OpCodes.Ldarg_0);
            Emit(OpCodes.Ldfld, fld);
        }

        if (box)
            Emit(OpCodes.Box, fld.FieldType);

        Emit(OpCodes.Call, method);
    }

    /// <inheritdoc cref="IOpCodeEmitter.EmitWriteLine(FieldInfo)" />
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    /// <exception cref="ArgumentException">Field must be static, or the same instance as the current method.</exception>
    public void EmitWriteLineAbove(FieldInfo fld)
    {
        MethodInfo method = GetConsoleWriteLineMethod(fld.FieldType, out bool box);
        CodeInstruction st = Instructions[_caretIndex];
        if (fld.IsStatic)
            Emit(OpCodes.Ldsfld, fld).WithStartBlocksFrom(st);
        else if (fld.DeclaringType == null || fld.DeclaringType != Method.DeclaringType || Method.IsStatic)
            throw new ArgumentException("Field must be static, or the same instance as the current method.", nameof(fld));
        else
        {
            Emit(OpCodes.Ldarg_0).WithStartBlocksFrom(st);
            Emit(OpCodes.Ldfld, fld);
        }

        if (box)
            Emit(OpCodes.Box, fld.FieldType);

        Emit(OpCodes.Call, method);
    }

    /// <inheritdoc />
    public void EndExceptionBlock()
    {
        _nextBlocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
    }

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Harmony does not support scopes.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    public void EndScope()
    {
        throw new NotSupportedException("Harmony does not support scopes.");
    }

    /// <inheritdoc />
    public void MarkLabel(Label loc)
    {
        _nextLabels.Add(loc);
    }

#if NETFRAMEWORK
    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">MarkSequencePoint is not supported.</exception>
    public void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
    {
        throw new NotSupportedException("MarkSequencePoint is not supported.");
    }
#endif

    /// <inheritdoc />
    public void ThrowException(Type excType)
    {
        if (excType == null)
            throw new ArgumentNullException(nameof(excType));

        ConstructorInfo? con = excType.IsSubclassOf(typeof(Exception)) || !(excType != typeof(Exception))
                                   ? excType.GetConstructor(Type.EmptyTypes)
                                   : throw new ArgumentException("Not a valid exception type.");

        if (con == null)
            throw new ArgumentException("Exception does not have a parameterless constructor.");

        Emit(OpCodes.Newobj, con);
        Emit(OpCodes.Throw);
    }

    /// <inheritdoc cref="IOpCodeEmitter.ThrowException(Type)" />
    /// <remarks>Emits above the active instruction, pushing it down the execution list and taking it's labels and blocks.</remarks>
    public void ThrowExceptionAbove(Type excType)
    {
        if (excType == null)
            throw new ArgumentNullException(nameof(excType));

        ConstructorInfo? con = excType.IsSubclassOf(typeof(Exception)) || !(excType != typeof(Exception))
                                   ? excType.GetConstructor(Type.EmptyTypes)
                                   : throw new ArgumentException("Not a valid exception type.");

        if (con == null)
            throw new ArgumentException("Exception does not have a parameterless constructor.");

        CodeInstruction st = Instructions[_caretIndex];
        Emit(OpCodes.Newobj, con).WithStartBlocksFrom(st);
        Emit(OpCodes.Throw);
    }

    /// <summary>Emits an instruction to throw an exception.</summary>
    /// <param name="excType">The class of the type of exception to throw.</param>
    /// <param name="message">The message add to the exception if there's a constructor for a message present.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="excType" /> is not the <see cref="Exception" /> class or a derived class of <see cref="Exception" />.
    /// -or-
    /// The type does not have a default constructor or a constructor with a string argument.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="excType" /> is <see langword="null" />.</exception>
    public void ThrowException(Type excType, string message)
    {
        if (excType == null)
            throw new ArgumentNullException(nameof(excType));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        _msgTypeArr ??= [ typeof(string) ];
        ConstructorInfo? con = excType.IsSubclassOf(typeof(Exception)) || !(excType != typeof(Exception))
                                   ? excType.GetConstructor(_msgTypeArr)
                                   : throw new ArgumentException("Not a valid exception type.");

        con ??= excType.GetConstructor(Type.EmptyTypes);

        if (con == null)
            throw new ArgumentException("Exception does not have a constructor with one string argument or a parameterless constructor.");

        Emit(OpCodes.Ldstr, message);
        Emit(OpCodes.Newobj, con);
        Emit(OpCodes.Throw);
    }

    /// <summary>Emits an instruction to throw an exception.</summary>
    /// <param name="excType">The class of the type of exception to throw.</param>
    /// <param name="message">The message add to the exception if there's a constructor for a message present.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="excType" /> is not the <see cref="Exception" /> class or a derived class of <see cref="Exception" />.
    /// -or-
    /// The type does not have a default constructor or a constructor with a string argument.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="excType" /> is <see langword="null" />.</exception>
    public void ThrowExceptionAbove(Type excType, string message)
    {
        if (excType == null)
            throw new ArgumentNullException(nameof(excType));
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        _msgTypeArr ??= [ typeof(string) ];
        ConstructorInfo? con = excType.IsSubclassOf(typeof(Exception)) || !(excType != typeof(Exception))
                                   ? excType.GetConstructor(_msgTypeArr)
                                   : throw new ArgumentException("Not a valid exception type.");

        con ??= excType.GetConstructor(Type.EmptyTypes);

        if (con == null)
            throw new ArgumentException("Exception does not have a constructor with one string argument or a parameterless constructor.");

        CodeInstruction st = Instructions[_caretIndex];
        Emit(OpCodes.Ldstr, message).WithStartBlocksFrom(st);
        Emit(OpCodes.Newobj, con);
        Emit(OpCodes.Throw);
    }

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">UsingNamespace is not supported.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    void IOpCodeEmitter.UsingNamespace(string usingNamespace)
    {
        throw new NotSupportedException("UsingNamespace is not supported.");
    }

    /// <summary>
    /// Get the stack size change of this <see cref="OpCode"/> with the given operand and method.
    /// </summary>
    /// <exception cref="NotSupportedException"><c>calli</c> instructions are not supported.</exception>
    /// <remarks>This does not take into account catch blocks, so if one is present one must be added to the stack change.</remarks>
    public int GetStackChange(OpCode code, object? operand)
        => GetStackChange(code, operand, Method);

    /// <summary>
    /// Get the stack size change of this <see cref="OpCode"/> with the given operand and method.
    /// </summary>
    /// <exception cref="NotSupportedException"><c>calli</c> instructions are not supported.</exception>
    /// <remarks>This does not take into account catch blocks, so if one is present one must be added to the stack change.</remarks>
    public static int GetStackChange(OpCode code, object? operand, MethodBase owningMethod)
    {
        int pop;
        if (code.StackBehaviourPop == StackBehaviour.Varpop)
        {
            switch (operand)
            {
                case MethodBase method when code == OpCodes.Call || code == OpCodes.Callvirt || code == OpCodes.Newobj:
                    pop = method.GetParameters().Length;
                    if (!method.IsStatic && method is MethodInfo)
                        ++pop;
                    break;
                case SignatureHelper when code == OpCodes.Calli:
                    throw new NotSupportedException("Calli not supported.");
                default:
                    if (code == OpCodes.Ret && owningMethod is MethodInfo m && m.ReturnType != typeof(void))
                        pop = 1;
                    else pop = 0;
                    break;
            }
        }
        else pop = Get(code.StackBehaviourPop);

        int push;
        if (code.StackBehaviourPush == StackBehaviour.Varpush)
        {
            switch (operand)
            {
                case MethodBase method when code == OpCodes.Call || code == OpCodes.Callvirt || code == OpCodes.Newobj:
                    if (method is ConstructorInfo || method is MethodInfo m2 && m2.ReturnType != typeof(void))
                        push = 1;
                    else push = 0;
                    break;
                case SignatureHelper when code == OpCodes.Calli:
                    throw new NotSupportedException("Calli not supported.");
                default:
                    push = 0;
                    break;
            }
        }
        else push = Get(code.StackBehaviourPush);

        return push - pop;

        int Get(StackBehaviour b)
        {
            switch (b)
            {
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Popref:
                case StackBehaviour.Pushref:
                    return 1;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                case StackBehaviour.Push1_push1:
                    return 2;
                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_pop1:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    return 3;
                default: return 0;
            }
        }
    }

    /// <summary>
    /// Tries to calculate the stack size before <see cref="CaretIndex"/>.
    /// </summary>
    /// <exception cref="NotSupportedException"><c>calli</c> instructions are not supported.</exception>
    public bool TryGetStackSize(out int stackSize) => TryGetStackSizeAtIndex(_caretIndex, out stackSize);

    /// <summary>
    /// Tries to calculate the stack size before the given instruction index.
    /// </summary>
    /// <exception cref="NotSupportedException"><c>calli</c> instructions are not supported.</exception>
    public bool TryGetStackSizeAtIndex(int startIndex, out int stackSize)
    {
        if (startIndex == 0)
        {
            stackSize = 0;
            return true;
        }

        int stackSizeIntl = 0;
        int lastStack = _lastStackSizeIs0;
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(Instructions, out int version) || version != _listVersion)
            lastStack = 0;

        for (int i = lastStack; i < startIndex; ++i)
        {
            CodeInstruction current = Instructions[i];

            if (i > 1 && Instructions[i - 1].opcode == OpCodes.Ret && current.labels.Count > 0)
            {
                Label lbl = current.labels[0];
                int index = Instructions.FindLastIndex(i - 2, x => x.operand is Label lbl2 && lbl2 == lbl);
                if (index != -1 && TryGetStackSizeAtIndex(index + 1, out int stackSize2))
                {
                    stackSizeIntl = stackSize2;
                }
            }

            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSizeIntl != 0 && current.operand is Label lbl3)
            {
                int index = Instructions.FindIndex(i, x => x.labels.Contains(lbl3));
                if (index != -1)
                {
                    i = index - 1;
                    continue;
                }
            }

            if (stackSizeIntl == 0)
                _lastStackSizeIs0 = i;

            stackSizeIntl += GetStackChange(current.opcode, current.operand, Method);
            stackSizeIntl += current.blocks.Count(x => x.blockType is ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginExceptFilterBlock);

            if (i == startIndex - 1)
            {
                stackSize = stackSizeIntl;
                return true;
            }
        }

        Accessor.TryGetListVersion(Instructions, out _listVersion);
        stackSize = 0;
        return false;
    }

    /// <summary>
    /// Returns the index of the last instruction before <see cref="CaretIndex"/> which starts with a stack size of zero.
    /// </summary>
    /// <remarks>This can be useful for isolating and replicating method calls with arguments that could change over time.</remarks>
    /// <param name="code">Code to match. Use the other overload for a wildcard match.</param>
    /// <param name="operand">Operand to match, or <see langword="null"/> for a wildcard.</param>
    /// <exception cref="InvalidProgramException">At any point your stack size drops below zero.</exception>
    /// <exception cref="NotSupportedException"><c>calli</c> _instructions are not supported.</exception>
    public int GetLastUnconsumedIndex(OpCode code, object? operand = null) => GetLastUnconsumedIndex(_caretIndex, code, operand);

    /// <summary>
    /// Returns the index of the last instruction before <paramref name="startIndex"/> which starts with a stack size of zero.
    /// </summary>
    /// <remarks>This can be useful for isolating and replicating method calls with arguments that could change over time.</remarks>
    /// <param name="startIndex">Index to search backwards from.</param>
    /// <param name="code">Code to match. Use the other overload for a wildcard match.</param>
    /// <param name="operand">Operand to match, or <see langword="null"/> for a wildcard.</param>
    /// <exception cref="InvalidProgramException">At any point your stack size drops below zero.</exception>
    /// <exception cref="NotSupportedException"><c>calli</c> _instructions are not supported.</exception>
    public int GetLastUnconsumedIndex(int startIndex, OpCode code, object? operand = null)
    {
        int stackSize = 0;
        int lastStack = _lastStackSizeIs0;
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(Instructions, out int version) || version != _listVersion)
            lastStack = 0;

        int last = -1;
        for (int i = lastStack; i < Instructions.Count; ++i)
        {
            CodeInstruction current = Instructions[i];
            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSize != 0 && current.operand is Label lbl)
            {
                int index = Instructions.FindIndex(i, x => x.labels.Contains(lbl));
                if (index != -1)
                {
                    i = index - 1;
                    continue;
                }
            }
            if (stackSize == 0)
            {
                lastStack = _lastStackSizeIs0;
                _lastStackSizeIs0 = i;
                if (current.opcode == code && (operand == null || OperandsEqual(current.operand, operand)))
                {
                    if (i >= startIndex)
                    {
                        _lastStackSizeIs0 = lastStack;
                        Accessor.TryGetListVersion(Instructions, out _listVersion);
                        return last;
                    }
                    last = i;
                }
            }
            stackSize += GetStackChange(current.opcode, current.operand, Method);
            stackSize += current.blocks.Count(x => x.blockType is ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginExceptFilterBlock);

            if (stackSize < 0)
            {
                if (_accessor.LogErrorMessages)
                {
                    IReflectionToolsLogger? logger = _accessor.Logger;
                    logger?.LogError(nameof(TranspileContext), null, "Stack size less than 0 around the following lines of IL: ");

                    for (int j = Math.Max(0, i - 2); j < Math.Min(Instructions.Count - 1, i + 2); ++j)
                        logger?.LogError(nameof(TranspileContext), null, $"#{j:F4} {Instructions[j]}.");
                }

                throw new InvalidProgramException($"Stack size should never be less than zero. There is an issue with your IL code around index {i}.");
            }
        }

        Accessor.TryGetListVersion(Instructions, out _listVersion);
        return last;
    }

    /// <summary>
    /// Returns the index of the last instruction before <see cref="CaretIndex"/> which starts with a stack size of zero.
    /// </summary>
    /// <remarks>This can be useful for isolating and replicating method calls with arguments that could change over time.</remarks>
    /// <param name="codeFilter">Predicate for <see cref="CodeInstruction"/> to match, or <see langword="null"/> for a wildcard.</param>
    /// <exception cref="InvalidProgramException">At any point your stack size drops below zero.</exception>
    /// <exception cref="NotSupportedException"><c>calli</c> instructions are not supported.</exception>
    public int GetLastUnconsumedIndex(PatternMatch? codeFilter) => GetLastUnconsumedIndex(_caretIndex, codeFilter);

    /// <summary>
    /// Returns the index of the last instruction before <paramref name="startIndex"/> which starts with a stack size of zero.
    /// </summary>
    /// <remarks>This can be useful for isolating and replicating method calls with arguments that could change over time.</remarks>
    /// <param name="startIndex">Index to search backwards from.</param>
    /// <param name="codeFilter">Predicate for <see cref="CodeInstruction"/> to match, or <see langword="null"/> for a wildcard.</param>
    /// <exception cref="InvalidProgramException">At any point your stack size drops below zero.</exception>
    /// <exception cref="NotSupportedException"><c>calli</c> instructions are not supported.</exception>
    public int GetLastUnconsumedIndex(int startIndex, PatternMatch? codeFilter)
    {
        int stackSize = 0;
        int lastStack = _lastStackSizeIs0;
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(Instructions, out int version) || version != _listVersion)
            lastStack = 0;

        int last = -1;
        for (int i = lastStack; i < Instructions.Count; ++i)
        {
            CodeInstruction current = Instructions[i];
            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSize != 0 && current.operand is Label lbl)
            {
                int index = Instructions.FindIndex(i, x => x.labels.Contains(lbl));
                if (index != -1)
                {
                    i = index - 1;
                    continue;
                }
            }

            if (stackSize == 0)
            {
                lastStack = _lastStackSizeIs0;
                _lastStackSizeIs0 = i;
                if (codeFilter == null || codeFilter(current))
                {
                    if (i >= startIndex)
                    {
                        _lastStackSizeIs0 = lastStack;
                        Accessor.TryGetListVersion(Instructions, out _listVersion);
                        return last;
                    }
                    last = i;
                }
            }
            stackSize += GetStackChange(current.opcode, current.operand, Method);
            if (current.blocks.Any(x => x.blockType is ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginExceptFilterBlock))
                ++stackSize;
            if (stackSize < 0)
            {
                if (_accessor.LogErrorMessages)
                {
                    IReflectionToolsLogger? logger = _accessor.Logger;
                    logger?.LogError(nameof(TranspileContext), null, "Stack size less than 0 around the following lines of IL: ");

                    for (int j = Math.Max(0, i - 2); j < Math.Min(Instructions.Count - 1, i + 2); ++j)
                        logger?.LogError(nameof(TranspileContext), null, $"#{j:F4} {Instructions[j]}.");
                }

                throw new InvalidProgramException($"Stack size should never be less than zero. There is an issue with your IL code around index {i}.");
            }
        }

        Accessor.TryGetListVersion(Instructions, out _listVersion);
        return last;
    }

    private static bool OperandsEqual(object? left, object? right)
    {
        if (left == null)
            return right == null;
        return right != null && left.Equals(right);
    }

    /// <inheritdoc/>
    public IEnumerator<CodeInstruction> GetEnumerator() => Instructions.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => Instructions.GetEnumerator();

#if NETFRAMEWORK
    /// <inheritdoc/>
    void _ILGenerator.GetTypeInfoCount(out uint pcTInfo)
    {
        ((_ILGenerator)_il).GetTypeInfoCount(out pcTInfo);
    }

    /// <inheritdoc/>
    void _ILGenerator.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
    {
        ((_ILGenerator)_il).GetTypeInfo(iTInfo, lcid, ppTInfo);
    }

    /// <inheritdoc/>
    void _ILGenerator.GetIDsOfNames(ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
    {
        ((_ILGenerator)_il).GetIDsOfNames(ref riid, rgszNames, cNames, lcid, rgDispId);
    }

    /// <inheritdoc/>
    void _ILGenerator.Invoke(uint dispIdMember, ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult,
        IntPtr pExcepInfo, IntPtr puArgErr)
    {
        ((_ILGenerator)_il).Invoke(dispIdMember, ref riid, lcid, wFlags, pDispParams, pVarResult, pExcepInfo, puArgErr);
    }
#endif
}
