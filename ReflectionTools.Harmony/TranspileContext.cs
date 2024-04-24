using DanielWillett.ReflectionTools.Emit;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
#if NETFRAMEWORK
using System.Diagnostics.SymbolStore;
#endif
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DanielWillett.ReflectionTools.Harmony;

/// <summary>
/// Used for logging in a transpiler, along with finding members.
/// </summary>
public class TranspileContext : IOpCodeEmitter, IEnumerable<CodeInstruction>
{
    private readonly IAccessor _accessor;
    private readonly List<CodeInstruction> _instructions;
    private readonly List<Label> _nextLabels = [];
    private readonly List<ExceptionBlock> _nextBlocks = [];
    private readonly ILGenerator _il;
    private static Type[]? _msgTypeArr;
    private int _caretIndex;
    private int _lastStackSizeIs0;
    private int _listVersion;

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
        _instructions = instructions == null ? new List<CodeInstruction>(0) : new List<CodeInstruction>(instructions);
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
    public int Count => _instructions.Count;

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
            if (_caretIndex > _instructions.Count || _caretIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Caret index must be >= 0 and <= instruction count.");

            AssertBlocksAndLabelsApplied();
            _caretIndex = value;
        }
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
        if (_instructions.Count == 0)
            throw new InvalidOperationException("No instructions loaded.");

        CodeInstruction instruction = _instructions[CaretIndex];
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
    public void BeginExceptFilterBlock()
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
    public void BeginScope()
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
    public void Emit(CodeInstruction instruction)
    {
        _instructions.Insert(CaretIndex, instruction);
        ApplyBlocksAndLabels();
        ++CaretIndex;
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode)
    {
        Emit(new CodeInstruction(opcode));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, byte arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, double arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, float arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, int arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, long arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, sbyte arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, short arg)
    {
        Emit(new CodeInstruction(opcode, arg));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, string str)
    {
        Emit(new CodeInstruction(opcode, str));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, ConstructorInfo con)
    {
        Emit(new CodeInstruction(opcode, con));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, Label label)
    {
        Emit(new CodeInstruction(opcode, label));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, Label[] labels)
    {
        Emit(new CodeInstruction(opcode, labels));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, LocalBuilder local)
    {
        Emit(new CodeInstruction(opcode, local));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, SignatureHelper signature)
    {
        Emit(new CodeInstruction(opcode, signature));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, FieldInfo field)
    {
        Emit(new CodeInstruction(opcode, field));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, MethodInfo meth)
    {
        Emit(new CodeInstruction(opcode, meth));
    }

    /// <inheritdoc />
    public void Emit(OpCode opcode, Type cls)
    {
        Emit(new CodeInstruction(opcode, cls));
    }

    /// <inheritdoc />
    /// <remarks>Using <paramref name="optionalParameterTypes"/> is not supported in <see cref="TranspileContext"/>.</remarks>
    /// <exception cref="NotSupportedException">Optional parameters are not supported.</exception>
    public void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
    {
        if (optionalParameterTypes is { Length: > 0 })
            throw new NotSupportedException("Optional parameters are not supported.");

        Emit(new CodeInstruction(opcode, methodInfo));
    }

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Calli is not supported.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    public void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[] parameterTypes, Type[]? optionalParameterTypes)
    {
        throw new NotSupportedException("Calli is not supported.");
    }

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Calli is not supported.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    public void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
    {
        throw new NotSupportedException("Calli is not supported.");
    }

    /// <inheritdoc />
    public void EmitWriteLine(string value)
    {
        MethodInfo method = new Action<string>(Console.WriteLine).Method;
        Emit(OpCodes.Ldstr, value);
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

    /// <summary>
    /// Not supported in <see cref="TranspileContext"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">UsingNamespace is not supported.</exception>
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DoesNotReturn]
#endif
    public void UsingNamespace(string usingNamespace)
    {
        throw new NotSupportedException("UsingNamespace is not supported.");
    }

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
    public bool TryGetStackSize(out int stackSize) => TryGetStackSizeAtIndex(CaretIndex, out stackSize);

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
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(_instructions, out int version) || version != _listVersion)
            lastStack = 0;

        for (int i = lastStack; i < startIndex; ++i)
        {
            CodeInstruction current = _instructions[i];

            if (i > 1 && _instructions[i - 1].opcode == OpCodes.Ret && current.labels.Count > 0)
            {
                Label lbl = current.labels[0];
                int index = _instructions.FindLastIndex(i - 2, x => x.operand is Label lbl2 && lbl2 == lbl);
                if (index != -1 && TryGetStackSizeAtIndex(index + 1, out int stackSize2))
                {
                    stackSizeIntl = stackSize2;
                }
            }

            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSizeIntl != 0 && current.operand is Label lbl3)
            {
                int index = _instructions.FindIndex(i, x => x.labels.Contains(lbl3));
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

        Accessor.TryGetListVersion(_instructions, out _listVersion);
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
    public int GetLastUnconsumedIndex(OpCode code, object? operand = null) => GetLastUnconsumedIndex(CaretIndex, code, operand);

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
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(_instructions, out int version) || version != _listVersion)
            lastStack = 0;

        int last = -1;
        for (int i = lastStack; i < _instructions.Count; ++i)
        {
            CodeInstruction current = _instructions[i];
            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSize != 0 && current.operand is Label lbl)
            {
                int index = _instructions.FindIndex(i, x => x.labels.Contains(lbl));
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
                        Accessor.TryGetListVersion(_instructions, out _listVersion);
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

                    for (int j = Math.Max(0, i - 2); j < Math.Min(_instructions.Count - 1, i + 2); ++j)
                        logger?.LogError(nameof(TranspileContext), null, $"#{j:F4} {_instructions[j]}.");
                }

                throw new InvalidProgramException($"Stack size should never be less than zero. There is an issue with your IL code around index {i}.");
            }
        }

        Accessor.TryGetListVersion(_instructions, out _listVersion);
        return last;
    }

    /// <summary>
    /// Returns the index of the last instruction before <see cref="CaretIndex"/> which starts with a stack size of zero.
    /// </summary>
    /// <remarks>This can be useful for isolating and replicating method calls with arguments that could change over time.</remarks>
    /// <param name="codeFilter">Predicate for <see cref="CodeInstruction"/> to match, or <see langword="null"/> for a wildcard.</param>
    /// <exception cref="InvalidProgramException">At any point your stack size drops below zero.</exception>
    /// <exception cref="NotSupportedException"><c>calli</c> instructions are not supported.</exception>
    public int GetLastUnconsumedIndex(PatternMatch? codeFilter) => GetLastUnconsumedIndex(CaretIndex, codeFilter);

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
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(_instructions, out int version) || version != _listVersion)
            lastStack = 0;

        int last = -1;
        for (int i = lastStack; i < _instructions.Count; ++i)
        {
            CodeInstruction current = _instructions[i];
            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSize != 0 && current.operand is Label lbl)
            {
                int index = _instructions.FindIndex(i, x => x.labels.Contains(lbl));
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
                        Accessor.TryGetListVersion(_instructions, out _listVersion);
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

                    for (int j = Math.Max(0, i - 2); j < Math.Min(_instructions.Count - 1, i + 2); ++j)
                        logger?.LogError(nameof(TranspileContext), null, $"#{j:F4} {_instructions[j]}.");
                }

                throw new InvalidProgramException($"Stack size should never be less than zero. There is an issue with your IL code around index {i}.");
            }
        }

        Accessor.TryGetListVersion(_instructions, out _listVersion);
        return last;
    }

    private static bool OperandsEqual(object? left, object? right)
    {
        if (left == null)
            return right == null;
        return right != null && left.Equals(right);
    }

    /// <inheritdoc/>
    public IEnumerator<CodeInstruction> GetEnumerator() => _instructions.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => _instructions.GetEnumerator();

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
