﻿using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Harmony;

/// <summary>
/// Tool for keeping up with the stack size of a transpiler. <c>calli</c> instructions are not supported.
/// </summary>
/// <remarks>It's okay if the given list changes after passing it.</remarks>
public class StackTracker(List<CodeInstruction> instructions, MethodBase method, IAccessor accessor)
{
    private readonly IAccessor _accessor = accessor ?? Accessor.Active;
    private int _lastStackSizeIs0;
    private int _listVersion;

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
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(instructions, out int version) || version != _listVersion)
            lastStack = 0;

        for (int i = lastStack; i < startIndex; ++i)
        {
            CodeInstruction current = instructions[i];

            if (i > 1 && instructions[i - 1].opcode == OpCodes.Ret && current.labels.Count > 0)
            {
                Label lbl = current.labels[0];
                int index = instructions.FindLastIndex(i - 2, x => x.operand is Label lbl2 && lbl2 == lbl);
                if (index != -1 && TryGetStackSizeAtIndex(index + 1, out int stackSize2))
                {
                    stackSizeIntl = stackSize2;
                }
            }

            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSizeIntl != 0 && current.operand is Label lbl3)
            {
                int index = instructions.FindIndex(i, x => x.labels.Contains(lbl3));
                if (index != -1)
                {
                    i = index - 1;
                    continue;
                }
            }
            
            if (stackSizeIntl == 0)
                _lastStackSizeIs0 = i;
            
            stackSizeIntl += GetStackChange(current.opcode, current.operand, method);
            if (current.blocks.Any(x => x.blockType is ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginExceptFilterBlock))
                ++stackSizeIntl;

            if (i == startIndex - 1)
            {
                stackSize = stackSizeIntl;
                return true;
            }
        }

        Accessor.TryGetListVersion(instructions, out _listVersion);
        stackSize = 0;
        return false;
    }

    /// <summary>
    /// Returns the index of the last instruction before <paramref name="startIndex"/> which starts with a stack size of zero.
    /// </summary>
    /// <remarks>This can be useful for isolating and replicating method calls with arguments that could change over time.</remarks>
    /// <param name="startIndex">Index to search backwards from.</param>
    /// <param name="code">Code to match. Use the other overload for a wildcard match.</param>
    /// <param name="operand">Operand to match, or <see langword="null"/> for a wildcard.</param>
    /// <exception cref="InvalidProgramException">At any point your stack size drops below zero.</exception>
    /// <exception cref="NotSupportedException"><c>calli</c> instructions are not supported.</exception>
    public int GetLastUnconsumedIndex(int startIndex, OpCode code, object? operand = null)
    {
        int stackSize = 0;
        int lastStack = _lastStackSizeIs0;
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(instructions, out int version) || version != _listVersion)
            lastStack = 0;

        int last = -1;
        for (int i = lastStack; i < instructions.Count; ++i)
        {
            CodeInstruction current = instructions[i];
            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSize != 0 && current.operand is Label lbl)
            {
                int index = instructions.FindIndex(i, x => x.labels.Contains(lbl));
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
                        Accessor.TryGetListVersion(instructions, out _listVersion);
                        return last;
                    }
                    last = i;
                }
            }
            stackSize += GetStackChange(current.opcode, current.operand, method);
            if (current.blocks.Any(x => x.blockType is ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginExceptFilterBlock))
                ++stackSize;
            if (stackSize < 0)
            {
                if (_accessor.LogErrorMessages)
                {
                    IReflectionToolsLogger? logger = _accessor.Logger;
                    logger?.LogError(nameof(StackTracker), null, "Stack size less than 0 around the following lines of IL: ");

                    for (int j = Math.Max(0, i - 2); j < Math.Min(instructions.Count - 1, i + 2); ++j)
                        logger?.LogError(nameof(StackTracker), null, $"#{j:F4} {instructions[j]}.");
                }

                throw new InvalidProgramException($"Stack size should never be less than zero. There is an issue with your IL code around index {i}.");
            }
        }

        Accessor.TryGetListVersion(instructions, out _listVersion);
        return last;
    }

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
        if (_lastStackSizeIs0 >= startIndex || !Accessor.TryGetListVersion(instructions, out int version) || version != _listVersion)
            lastStack = 0;

        int last = -1;
        for (int i = lastStack; i < instructions.Count; ++i)
        {
            CodeInstruction current = instructions[i];
            if ((current.opcode == OpCodes.Br || current.opcode == OpCodes.Br_S) && stackSize != 0 && current.operand is Label lbl)
            {
                int index = instructions.FindIndex(i, x => x.labels.Contains(lbl));
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
                        Accessor.TryGetListVersion(instructions, out _listVersion);
                        return last;
                    }
                    last = i;
                }
            }
            stackSize += GetStackChange(current.opcode, current.operand, method);
            if (current.blocks.Any(x => x.blockType is ExceptionBlockType.BeginCatchBlock or ExceptionBlockType.BeginExceptFilterBlock))
                ++stackSize;
            if (stackSize < 0)
            {
                if (_accessor.LogErrorMessages)
                {
                    IReflectionToolsLogger? logger = _accessor.Logger;
                    logger?.LogError(nameof(StackTracker), null, "Stack size less than 0 around the following lines of IL: ");

                    for (int j = Math.Max(0, i - 2); j < Math.Min(instructions.Count - 1, i + 2); ++j)
                        logger?.LogError(nameof(StackTracker), null, $"#{j:F4} {instructions[j]}.");
                }

                throw new InvalidProgramException($"Stack size should never be less than zero. There is an issue with your IL code around index {i}.");
            }
        }
        
        Accessor.TryGetListVersion(instructions, out _listVersion);
        return last;
    }

    private static bool OperandsEqual(object? left, object? right)
    {
        if (left == null)
            return right == null;
        return right != null && left.Equals(right);
    }
}