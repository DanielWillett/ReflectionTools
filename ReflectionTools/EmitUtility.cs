﻿using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
#if !NETSTANDARD || NETSTANDARD2_1_OR_GREATER
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
#endif

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Utilities for <see cref="ILGenerator"/> and transpiling with Harmony.
/// </summary>
public static class EmitUtility
{
#if !NETSTANDARD || NETSTANDARD2_1_OR_GREATER
    /// <summary>
    /// Returns instructions to throw the provided <typeparamref name="TException"/> with an optional <paramref name="message"/>.
    /// </summary>
    [Pure]
    public static IEnumerable<CodeInstruction> Throw<TException>(string? message = null) where TException : Exception
    {
        ConstructorInfo[] ctors = typeof(TException).GetConstructors(BindingFlags.Instance | BindingFlags.Public);

        ConstructorInfo? info = message == null
            ? ctors.FirstOrDefault(x => x.GetParameters().Length == 0)
            : (ctors.FirstOrDefault(x => x.GetParameters().Length == 1 && x.GetParameters()[0].ParameterType == typeof(string)) ??
               ctors.FirstOrDefault(x => x.GetParameters().Length == 0));

        if (info == null)
            throw new MemberAccessException("Unable to find any constructors for that exception.");

        if (info.GetParameters().Length == 1)
        {
            return new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldstr, message),
                new CodeInstruction(OpCodes.Newobj, info),
                new CodeInstruction(OpCodes.Throw)
            };
        }

        return new CodeInstruction[]
        {
            new CodeInstruction(OpCodes.Newobj, info),
            new CodeInstruction(OpCodes.Throw)
        };
    }

    /// <summary>
    /// Returns <see langword="true"/> if the instruction at <paramref name="index"/> and the following match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    /// <remarks><paramref name="index"/> will be incremented to the next instruction after the match.</remarks>
    [Pure]
    public static bool FollowPattern(IList<CodeInstruction> instructions, ref int index, params PatternMatch?[] matches)
    {
        if (MatchPattern(instructions, index, matches))
        {
            index += matches.Length;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> and removes the instructions at <paramref name="index"/> and the following if they match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    [Pure]
    public static bool RemovePattern(IList<CodeInstruction> instructions, int index, params PatternMatch?[] matches)
    {
        if (MatchPattern(instructions, index, matches))
        {
            if (instructions is List<CodeInstruction> list)
            {
                list.RemoveRange(index, matches.Length);
            }
            else
            {
                for (int i = 0; i < matches.Length; ++i)
                    instructions.RemoveAt(index);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the instruction at <paramref name="index"/> and the following match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    [Pure]
    public static bool MatchPattern(IList<CodeInstruction> instructions, int index, params PatternMatch?[] matches)
    {
        int c = matches.Length;
        if (c <= 0 || index >= instructions.Count - matches.Length)
            return false;
        for (int i = index; i < index + matches.Length; ++i)
        {
            PatternMatch? pattern = matches[i - index];
            if (pattern != null && !pattern.Invoke(instructions[i]))
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// Inserts instructions to execute <paramref name="checker"/> and return (or optionally branch to <paramref name="goto"/>) if it returns <see langword="false"/>.
    /// </summary>
    /// <returns>Amount of instructions inserted.</returns>
    public static int ReturnIfFalse(IList<CodeInstruction> instructions, ILGenerator generator, ref int index, Func<bool> checker, Label? @goto = null)
    {
        if (index < 0)
            throw new ArgumentException($"Unable to add ReturnIfFalse ({checker.Method.Name}), index is too small: {index}.", nameof(index));
        if (index >= instructions.Count)
            throw new ArgumentException($"Unable to add ReturnIfFalse ({checker.Method.Name}), index is too large: {index}.", nameof(index));
        if (@goto.HasValue)
        {
            CodeInstruction instruction = new CodeInstruction(checker.Method.GetCallRuntime(), checker.Method);
            instruction.MoveLabelsFrom(instructions[index]);
            instructions.Insert(index, instruction);
            
            instructions.Insert(index + 1, new CodeInstruction(OpCodes.Brfalse, @goto));
            index += 2;
            return 2;
        }
        else
        {
            Label continueLbl = generator.DefineLabel();
            CodeInstruction instruction = new CodeInstruction(checker.Method.GetCallRuntime(), checker.Method);
            instruction.MoveLabelsFrom(instructions[index]);
            instructions.Insert(index, instruction);

            instructions.Insert(index + 1, new CodeInstruction(OpCodes.Brtrue, continueLbl));
            instructions.Insert(index + 2, @goto.HasValue ? new CodeInstruction(OpCodes.Br, @goto) : new CodeInstruction(OpCodes.Ret));
            index += 3;
            if (instructions.Count > index)
                instructions[index].labels.Add(continueLbl);
            return 3;
        }
    }

    /// <summary>
    /// Increment <paramref name="index"/> until <paramref name="match"/> matches or the function ends.
    /// </summary>
    /// <returns>Amount of instructions skipped.</returns>
    [Pure]
    public static int ContinueUntil(IList<CodeInstruction> instructions, ref int index, PatternMatch match, bool includeMatch = true)
    {
        int amt = 0;
        for (int i = index; i < instructions.Count; ++i)
        {
            ++amt;
            if (match(instructions[i]))
            {
                index = includeMatch ? i : i + 1;
                if (includeMatch)
                    --amt;
                break;
            }
        }
        return amt;
    }

    /// <summary>
    /// Increment <paramref name="index"/> until <paramref name="match"/> fails or the function ends.
    /// </summary>
    /// <returns>Amount of instructions skipped.</returns>
    [Pure]
    public static int ContinueWhile(IList<CodeInstruction> instructions, ref int index, PatternMatch match, bool includeNext = true)
    {
        int amt = 0;
        for (int i = index; i < instructions.Count; ++i)
        {
            ++amt;
            if (!match(instructions[i]))
            {
                index = includeNext ? i : i - 1;
                if (!includeNext)
                    --amt;
                break;
            }
        }
        return amt;
    }

    /// <summary>
    /// Add <paramref name="label"/> to the next instruction that matches <paramref name="match"/>.
    /// </summary>
    [Pure]
    public static bool LabelNext(IList<CodeInstruction> instructions, int index, Label label, PatternMatch match, int shift = 0, bool labelRtnIfFailure = false)
    {
        for (int i = index; i < instructions.Count; ++i)
        {
            if (match(instructions[i]))
            {
                int newIndex = i + shift;
                if (instructions.Count > i)
                {
                    instructions[newIndex].labels.Add(label);
                    return true;
                }
            }
        }
        if (labelRtnIfFailure)
        {
            if (instructions[instructions.Count - 1].opcode == OpCodes.Ret)
                instructions[instructions.Count - 1].labels.Add(label);
            else
            {
                CodeInstruction instruction = new CodeInstruction(OpCodes.Ret);
                instruction.labels.Add(label);
                instructions.Add(instruction);
            }
        }
        return false;
    }

    /// <summary>
    /// Get a label to the next instruction that matches <paramref name="match"/>.
    /// </summary>
    [Pure]
    public static Label? LabelNext(IList<CodeInstruction> instructions, ILGenerator generator, int index, PatternMatch match, int shift = 0)
    {
        for (int i = index; i < instructions.Count; ++i)
        {
            if (match(instructions[i]))
            {
                int newIndex = i + shift;
                if (instructions.Count > i)
                {
                    Label label = generator.DefineLabel();
                    instructions[newIndex].labels.Add(label);
                    return label;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Get a label to the next instruction that matches <paramref name="match"/>, or the end of the function.
    /// </summary>
    [Pure]
    public static Label LabelNextOrReturn(IList<CodeInstruction> instructions, ILGenerator generator, int index, PatternMatch? match, int shift = 0, bool allowUseExisting = true)
    {
        CodeInstruction instruction;
        for (int i = index; i < instructions.Count; ++i)
        {
            if (match == null || match(instructions[i]))
            {
                int newIndex = i + shift;
                if (instructions.Count > i)
                {
                    instruction = instructions[newIndex];
                    if (allowUseExisting && instruction.labels.Count > 0)
                        return instruction.labels[instruction.labels.Count - 1];
                    Label label = generator.DefineLabel();
                    instruction.labels.Add(label);
                    return label;
                }
            }
        }

        instruction = instructions[instructions.Count - 1];
        if (instruction.opcode == OpCodes.Ret)
        {
            if (allowUseExisting && instruction.labels.Count > 0)
                return instruction.labels[instruction.labels.Count - 1];
            Label label = generator.DefineLabel();
            instruction.labels.Add(label);
            return label;
        }
        else
        {
            Label label = generator.DefineLabel();
            instruction = new CodeInstruction(OpCodes.Ret);
            instruction.labels.Add(label);
            instructions.Add(instruction);
            return label;
        }
    }

    /// <summary>
    /// Get the label of the next branch instructino.
    /// </summary>
    [Pure]
    public static Label? GetNextBranchTarget(IList<CodeInstruction> instructions, int index)
    {
        if (index < 0)
            index = 0;
        for (int i = index; i < instructions.Count; ++i)
        {
            if (instructions[i].Branches(out Label? label) && label.HasValue)
                return label;
        }

        return null;
    }

    /// <summary>
    /// Find the index of the code instruction to which the label refers.
    /// </summary>
    [Pure]
    public static int FindLabelDestinationIndex(IList<CodeInstruction> instructions, Label label, int startIndex = 0)
    {
        if (startIndex < 0)
            startIndex = 0;
        for (int i = startIndex; i < instructions.Count; ++i)
        {
            List<Label>? labels = instructions[i].labels;
            if (labels != null)
            {
                for (int j = 0; j < labels.Count; ++j)
                {
                    if (labels[j] == label)
                        return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the index of a local code instruction.
    /// </summary>
    [Pure]
    public static int GetLocalIndex(CodeInstruction code, bool set)
    {
        if (code.opcode.OperandType == OperandType.ShortInlineVar &&
            (set && code.opcode == OpCodes.Stloc_S ||
             !set && code.opcode == OpCodes.Ldloc_S || !set && code.opcode == OpCodes.Ldloca_S))
            return ((LocalBuilder)code.operand).LocalIndex;
        if (code.opcode.OperandType == OperandType.InlineVar &&
            (set && code.opcode == OpCodes.Stloc ||
             !set && code.opcode == OpCodes.Ldloc || !set && code.opcode == OpCodes.Ldloca))
            return ((LocalBuilder)code.operand).LocalIndex;
        if (set)
        {
            if (code.opcode == OpCodes.Stloc_0)
                return 0;
            if (code.opcode == OpCodes.Stloc_1)
                return 1;
            if (code.opcode == OpCodes.Stloc_2)
                return 2;
            if (code.opcode == OpCodes.Stloc_3)
                return 3;
        }
        else
        {
            if (code.opcode == OpCodes.Ldloc_0)
                return 0;
            if (code.opcode == OpCodes.Ldloc_1)
                return 1;
            if (code.opcode == OpCodes.Ldloc_2)
                return 2;
            if (code.opcode == OpCodes.Ldloc_3)
                return 3;
        }

        return -1;
    }

    /// <summary>
    /// Emit an Int32.
    /// </summary>
    [Pure]
    public static CodeInstruction LoadConstantI4(int number)
    {
        return number switch
        {
            -1 => new CodeInstruction(OpCodes.Ldc_I4_M1),
            0 => new CodeInstruction(OpCodes.Ldc_I4_0),
            1 => new CodeInstruction(OpCodes.Ldc_I4_1),
            2 => new CodeInstruction(OpCodes.Ldc_I4_2),
            3 => new CodeInstruction(OpCodes.Ldc_I4_3),
            4 => new CodeInstruction(OpCodes.Ldc_I4_4),
            5 => new CodeInstruction(OpCodes.Ldc_I4_5),
            6 => new CodeInstruction(OpCodes.Ldc_I4_6),
            7 => new CodeInstruction(OpCodes.Ldc_I4_7),
            8 => new CodeInstruction(OpCodes.Ldc_I4_8),
            _ => new CodeInstruction(OpCodes.Ldc_I4, number),
        };
    }

    /// <summary>
    /// Loads a parameter from an index.
    /// </summary>
    [Pure]
    public static CodeInstruction GetParameter(int index)
    {
        return index switch
        {
            0 => new CodeInstruction(OpCodes.Ldarg_0),
            1 => new CodeInstruction(OpCodes.Ldarg_1),
            2 => new CodeInstruction(OpCodes.Ldarg_2),
            3 => new CodeInstruction(OpCodes.Ldarg_3),
            < ushort.MaxValue => new CodeInstruction(OpCodes.Ldarg_S, index),
            _ => new CodeInstruction(OpCodes.Ldarg, index)
        };
    }

    /// <summary>
    /// Get the local builder or index of the instruction.
    /// </summary>
    [Pure]
    public static LocalBuilder? GetLocal(CodeInstruction code, out int index, bool set)
    {
        if (code.opcode.OperandType == OperandType.ShortInlineVar &&
            (set && code.opcode == OpCodes.Stloc_S ||
             !set && code.opcode == OpCodes.Ldloc_S || !set && code.opcode == OpCodes.Ldloca_S))
        {
            LocalBuilder bld = (LocalBuilder)code.operand;
            index = bld.LocalIndex;
            return bld;
        }
        if (code.opcode.OperandType == OperandType.InlineVar &&
            (set && code.opcode == OpCodes.Stloc ||
             !set && code.opcode == OpCodes.Ldloc || !set && code.opcode == OpCodes.Ldloca))
        {
            LocalBuilder bld = (LocalBuilder)code.operand;
            index = bld.LocalIndex;
            return bld;
        }
        if (set)
        {
            if (code.opcode == OpCodes.Stloc_0)
            {
                index = 0;
                return null;
            }
            if (code.opcode == OpCodes.Stloc_1)
            {
                index = 1;
                return null;
            }
            if (code.opcode == OpCodes.Stloc_2)
            {
                index = 2;
                return null;
            }
            if (code.opcode == OpCodes.Stloc_3)
            {
                index = 3;
                return null;
            }
        }
        else
        {
            if (code.opcode == OpCodes.Ldloc_0)
            {
                index = 0;
                return null;
            }
            if (code.opcode == OpCodes.Ldloc_1)
            {
                index = 1;
                return null;
            }
            if (code.opcode == OpCodes.Ldloc_2)
            {
                index = 2;
                return null;
            }
            if (code.opcode == OpCodes.Ldloc_3)
            {
                index = 3;
                return null;
            }
        }

        index = -1;
        return null;
    }

    /// <summary>
    /// Copy an instruction without 
    /// </summary>
    [Pure]
    public static CodeInstruction CopyWithoutSpecial(this CodeInstruction instruction) => new CodeInstruction(instruction.opcode, instruction.operand);

    /// <summary>
    /// Transfers blocks that would be on the last instruction of a block to the target instruction.
    /// </summary>
    public static CodeInstruction WithEndingInstructionNeeds(this CodeInstruction instruction, CodeInstruction other)
    {
        TransferEndingInstructionNeeds(instruction, other);
        return instruction;
    }

    /// <summary>
    /// Transfers all labels and blocks that would be on the first instruction of a block to the target instruction.
    /// </summary>
    public static CodeInstruction WithStartingInstructionNeeds(this CodeInstruction instruction, CodeInstruction other)
    {
        TransferStartingInstructionNeeds(instruction, other);
        return instruction;
    }

    /// <summary>
    /// Transfers blocks that would be on the last instruction of a block to the target instruction.
    /// </summary>
    public static void TransferEndingInstructionNeeds(CodeInstruction originalEnd, CodeInstruction newEnd)
    {
        newEnd.blocks.AddRange(originalEnd.blocks.Where(x => x.blockType.IsEndBlockType()));
        originalEnd.blocks.RemoveAll(x => x.blockType.IsEndBlockType());
    }

    /// <summary>
    /// Transfers all labels and blocks that would be on the first instruction of a block to the target instruction.
    /// </summary>
    public static void TransferStartingInstructionNeeds(CodeInstruction originalStart, CodeInstruction newStart)
    {
        newStart.labels.AddRange(originalStart.labels);
        originalStart.labels.Clear();
        newStart.blocks.AddRange(originalStart.blocks.Where(x => x.blockType.IsBeginBlockType()));
        originalStart.blocks.RemoveAll(x => x.blockType.IsBeginBlockType());
    }

    /// <summary>
    /// Cut and pastes all labels and blocks to the target instruction.
    /// </summary>
    public static void MoveBlocksAndLabels(this CodeInstruction from, CodeInstruction to)
    {
        to.labels.AddRange(from.labels);
        from.labels.Clear();
        to.blocks.AddRange(from.blocks);
        from.blocks.Clear();
    }

    /// <summary>
    /// Would this block type begin a block?
    /// </summary>
    [Pure]
    public static bool IsBeginBlockType(this ExceptionBlockType type) => type is ExceptionBlockType.BeginCatchBlock
        or ExceptionBlockType.BeginExceptFilterBlock or ExceptionBlockType.BeginExceptionBlock
        or ExceptionBlockType.BeginFaultBlock or ExceptionBlockType.BeginFinallyBlock;

    /// <summary>
    /// Would this block type end a block?
    /// </summary>
    [Pure]
    public static bool IsEndBlockType(this ExceptionBlockType type) => type == ExceptionBlockType.EndExceptionBlock;
#endif

    /// <summary>
    /// Get the label ID from a <see cref="Label"/> object.
    /// </summary>
    /// <remarks>Not CLR compliant.</remarks>
    [Pure]
    public static unsafe int GetLabelId(this Label label) => *(int*)&label;

    /// <summary>
    /// Loads an argument from an index.
    /// </summary>
    public static void EmitArgument(ILGenerator il, int index, bool set, bool byref = false)
    {
        if (index > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(index));
        OpCode code;
        if (set)
        {
            code = index > byte.MaxValue ? OpCodes.Starg : OpCodes.Starg_S;
            if (index > byte.MaxValue)
                il.Emit(code, (short)index);
            else
                il.Emit(code, (byte)index);
            return;
        }
        if (byref)
        {
            code = index > byte.MaxValue ? OpCodes.Ldarga : OpCodes.Ldarga_S;
            if (index > byte.MaxValue)
                il.Emit(code, (short)index);
            else
                il.Emit(code, (byte)index);
            return;
        }

        if (index is < 4 and > -1)
        {
            il.Emit(index switch
            {
                0 => OpCodes.Ldarg_0,
                1 => OpCodes.Ldarg_1,
                2 => OpCodes.Ldarg_2,
                _ => OpCodes.Ldarg_3
            });
            return;
        }

        code = index > byte.MaxValue ? OpCodes.Ldarg : OpCodes.Ldarg_S;
        if (index > byte.MaxValue)
            il.Emit(code, (short)index);
        else
            il.Emit(code, (byte)index);
    }

    /// <summary>
    /// Emit an Int32.
    /// </summary>
    public static void LoadConstantI4(ILGenerator generator, int number)
    {
        OpCode code = number switch
        {
            -1 => OpCodes.Ldc_I4_M1,
            0 => OpCodes.Ldc_I4_0,
            1 => OpCodes.Ldc_I4_1,
            2 => OpCodes.Ldc_I4_2,
            3 => OpCodes.Ldc_I4_3,
            4 => OpCodes.Ldc_I4_4,
            5 => OpCodes.Ldc_I4_5,
            6 => OpCodes.Ldc_I4_6,
            7 => OpCodes.Ldc_I4_7,
            8 => OpCodes.Ldc_I4_8,
            _ => OpCodes.Ldc_I4
        };
        if (number is < -1 or > 8)
            generator.Emit(code, number);
        else
            generator.Emit(code);
    }

    /// <summary>
    /// Loads a parameter from an index.
    /// </summary>
    public static void EmitParameter(this ILGenerator generator, int index, bool byref = false, Type? type = null, Type? targetType = null)
        => EmitParameter(generator, index, null, byref, type, targetType);

    /// <summary>
    /// Loads a parameter from an index.
    /// </summary>
    public static void EmitParameter(this ILGenerator generator, int index, string? castErrorMessage, bool byref = false, Type? type = null, Type? targetType = null)
    {
        EmitParameter(generator, null, index, castErrorMessage, byref, type, targetType);
    }
    internal static void EmitParameter(this ILGenerator generator, string? logSource, int index, string? castErrorMessage, bool byref = false, Type? type = null, Type? targetType = null)
    {
        if (index > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (!byref && type != null && targetType != null && type.IsValueType && targetType.IsValueType && type != targetType)
            throw new ArgumentException($"Types not compatible; input type: {type.FullName}, target type: {targetType.FullName}.", nameof(type));

        if (byref)
        {
            OpCode code2 = index > byte.MaxValue ? OpCodes.Ldarga : OpCodes.Ldarga_S;
            if (index > byte.MaxValue)
                generator.Emit(code2, (short)index);
            else
                generator.Emit(code2, (byte)index);
            if (logSource != null)
                Accessor.Logger.LogDebug(logSource, $"IL:  {(index > ushort.MaxValue ? "ldarga" : "ldarga.s")} <{index.ToString(CultureInfo.InvariantCulture)}>");
            return;
        }

        OpCode code = index switch
        {
            0 => OpCodes.Ldarg_0,
            1 => OpCodes.Ldarg_1,
            2 => OpCodes.Ldarg_2,
            3 => OpCodes.Ldarg_3,
            <= byte.MaxValue => OpCodes.Ldarg_S,
            _ => OpCodes.Ldarg
        };
        if (logSource != null)
        {
            Accessor.Logger.LogDebug(logSource, index switch
            {
                0 => "IL:  ldarg.0",
                1 => "IL:  ldarg.1",
                2 => "IL:  ldarg.2",
                3 => "IL:  ldarg.3",
                <= byte.MaxValue => $"IL:  ldarg.s <{index.ToString(CultureInfo.InvariantCulture)}",
                _ => $"IL:  ldarg <{index.ToString(CultureInfo.InvariantCulture)}"
            });
        }
        if (index > 3)
        {
            if (index > byte.MaxValue)
                generator.Emit(code, (ushort)index);
            else
                generator.Emit(code, (byte)index);
        }
        else
            generator.Emit(code);

        if (type == null || targetType == null || type == typeof(void) || targetType == typeof(void))
            return;

        Accessor.CheckExceptionConstructors();
        if (type.IsValueType && !targetType.IsValueType)
        {
            generator.Emit(OpCodes.Box, type);
            if (logSource != null)
                Accessor.Logger.LogDebug(logSource, $"IL:  box <{type.FullName}>");
        }
        else if (!type.IsValueType && targetType.IsValueType)
        {
            generator.Emit(OpCodes.Unbox_Any, targetType);
            if (logSource != null)
                Accessor.Logger.LogDebug(logSource, $"IL:  unbox.any <{targetType.FullName}>");
        }
        else if (!targetType.IsAssignableFrom(type) && (Accessor.CastExCtor != null || Accessor.NreExCtor != null))
        {
            Label lbl = generator.DefineLabel();
            generator.Emit(OpCodes.Isinst, targetType);
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Brtrue, lbl);
            generator.Emit(OpCodes.Pop);
            if (index > 3)
            {
                if (index > byte.MaxValue)
                    generator.Emit(code, (ushort)index);
                else
                    generator.Emit(code, (byte)index);
            }
            else
                generator.Emit(code);
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Brfalse, lbl);
            generator.Emit(OpCodes.Pop);
            castErrorMessage ??= $"Invalid type passed to parameter {index.ToString(CultureInfo.InvariantCulture)}.";
            if (Accessor.CastExCtor != null)
                generator.Emit(OpCodes.Ldstr, castErrorMessage);
            generator.Emit(OpCodes.Newobj, Accessor.CastExCtor ?? Accessor.NreExCtor!);
            generator.Emit(OpCodes.Throw);
            generator.MarkLabel(lbl);
            if (logSource != null)
            {
                string lblId = lbl.GetLabelId().ToString(CultureInfo.InvariantCulture);
                Accessor.Logger.LogDebug(logSource, $"IL:  isinst <{targetType.FullName}>");
                Accessor.Logger.LogDebug(logSource, "IL:  dup");
                Accessor.Logger.LogDebug(logSource, $"IL:  brtrue <lbl_{lblId}>");
                Accessor.Logger.LogDebug(logSource, "IL:  pop");
                Accessor.Logger.LogDebug(logSource, index switch
                {
                    0 => "IL:  ldarg.0",
                    1 => "IL:  ldarg.1",
                    2 => "IL:  ldarg.2",
                    3 => "IL:  ldarg.3",
                    <= byte.MaxValue => $"IL:  ldarg.s <{index.ToString(CultureInfo.InvariantCulture)}",
                    _ => $"IL:  ldarg <{index.ToString(CultureInfo.InvariantCulture)}"
                });
                Accessor.Logger.LogDebug(logSource, "IL:  dup");
                Accessor.Logger.LogDebug(logSource, $"IL:  brfalse <lbl_{lblId}>");
                Accessor.Logger.LogDebug(logSource, "IL:  pop");
                if (Accessor.CastExCtor != null)
                    Accessor.Logger.LogDebug(logSource, $"IL:  ldstr \"{castErrorMessage}\"");
                Accessor.Logger.LogDebug(logSource, $"IL:  newobj <{(Accessor.CastExCtor?.DeclaringType ?? Accessor.NreExCtor!.DeclaringType!).FullName}(System.String)>");
                Accessor.Logger.LogDebug(logSource, "IL:  throw");
                Accessor.Logger.LogDebug(logSource, $"IL: lbl_{lblId}:");
            }
        }
    }

    /// <summary>
    /// Compare <see cref="OpCode"/>s.
    /// </summary>
    /// <param name="opcode">Original <see cref="OpCode"/>.</param>
    /// <param name="comparand"><see cref="OpCode"/> to compare to <paramref name="opcode"/>.</param>
    /// <param name="fuzzy">Changes how similar <see cref="OpCode"/>s are compared (<c>br</c> and <c>ble</c> will match, for example).</param>
    [Pure]
    public static bool IsOfType(this OpCode opcode, OpCode comparand, bool fuzzy = false)
    {
        if (opcode == comparand)
            return true;
        if (opcode.IsStArg())
            return comparand.IsStArg();
        if (opcode.IsStLoc())
            return comparand.IsStLoc();
        if (!fuzzy)
        {
            if (opcode.IsLdArg())
                return comparand.IsLdArg();
            if (opcode.IsLdArg(true))
                return comparand.IsLdArg(true);
            if (opcode.IsLdLoc())
                return comparand.IsLdLoc();
            if (opcode.IsLdLoc(true))
                return comparand.IsLdLoc(true);
            if (opcode.IsLdFld())
                return comparand.IsLdFld();
            if (opcode.IsLdFld(true))
                return comparand.IsLdFld(true);
            if (opcode.IsLdFld(@static: true))
                return comparand.IsLdFld(@static: true);
            if (opcode.IsLdFld(true, @static: true))
                return comparand.IsLdFld(true, @static: true);
            if (opcode.IsLdc())
                return comparand.IsLdc();
            if (opcode.IsLdc(false, true))
                return comparand.IsLdc(false, true);
            if (opcode.IsLdc(false, false, true))
                return comparand.IsLdc(false, false, true);
            if (opcode.IsLdc(false, false, false, true))
                return comparand.IsLdc(false, false, false, true);
            if (opcode.IsLdc(false, false, false, false, true))
                return comparand.IsLdc(false, false, false, false, true);
            if (opcode.IsLdc(false, false, false, false, false, true))
                return comparand.IsLdc(false, false, false, false, false, true);
            if (opcode.IsBr(true))
                return comparand.IsBr(true);
            if (opcode.IsBr(false, true))
                return comparand.IsBr(false, true);
            if (opcode.IsBr(false, false, true))
                return comparand.IsBr(false, false, true);
            if (opcode.IsBr(false, false, false, true))
                return comparand.IsBr(false, false, false, true);
            if (opcode.IsBr(false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, true);
            if (opcode.IsBr(false, false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, false, true);
            if (opcode.IsBr(false, false, false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, false, false, true);
            if (opcode.IsBr(false, false, false, false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, false, false, false, true);
            if (opcode.IsBr(false, false, false, false, false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, false, false, false, false, true);
        }
        else
        {
            if (opcode.IsLdArg(true, true))
                return comparand.IsLdArg(true, true);
            if (opcode.IsLdLoc(true, true))
                return comparand.IsLdLoc(true, true);
            if (opcode.IsLdFld(either: true, staticOrInstance: true))
                return comparand.IsLdFld(either: true, staticOrInstance: true);
            if (opcode.IsLdc(true, true, true, true, true, true))
                return comparand.IsLdc(true, true, true, true, true, true);
            if (opcode.IsBr(true, true, true, true, true, true, true, true, true))
                return comparand.IsBr(true, true, true, true, true, true, true, true, true);
            if (opcode.IsConv(true, false, false, false, false, false, false))
                return comparand.IsConv(true, false, false, false, false, false, false);
            if (opcode.IsConv(false, true, false, false, false, false, false))
                return comparand.IsConv(false, true, false, false, false, false, false);
            if (opcode.IsConv(false, false, true, false, false, false, false))
                return comparand.IsConv(false, false, true, false, false, false, false);
            if (opcode.IsConv(false, false, false, true, false, false, false))
                return comparand.IsConv(false, false, false, true, false, false, false);
            if (opcode.IsConv(false, false, false, false, true, false, false))
                return comparand.IsConv(false, false, false, false, true, false, false);
            if (opcode.IsConv(false, false, false, false, false, true, false))
                return comparand.IsConv(false, false, false, false, false, true, false);
            if (opcode.IsConv(false, false, false, false, false, false, true))
                return comparand.IsConv(false, false, false, false, false, false, true);
        }

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>stloc</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    [Pure]
    public static bool IsStLoc(this OpCode opcode)
    {
        return opcode == OpCodes.Stloc || opcode == OpCodes.Stloc_S || opcode == OpCodes.Stloc_0 || opcode == OpCodes.Stloc_1 || opcode == OpCodes.Stloc_2 || opcode == OpCodes.Stloc_3;
    }

    /// <summary>
    /// Is this opcode any variants of <c>ldloc</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="byRef">Only match instructions that load by address.</param>
    /// <param name="either">Match instructions that load by value or address.</param>
    [Pure]
    public static bool IsLdLoc(this OpCode opcode, bool byRef = false, bool either = false)
    {
        if (opcode == OpCodes.Ldloc_S || opcode == OpCodes.Ldloc_0 || opcode == OpCodes.Ldloc_1 || opcode == OpCodes.Ldloc_2 || opcode == OpCodes.Ldloc_3 || opcode == OpCodes.Ldloc)
            return !byRef || either;
        if (opcode == OpCodes.Ldloca_S || opcode == OpCodes.Ldloca)
            return byRef || either;

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>ldfld</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="byRef">Only match instructions that load by address.</param>
    /// <param name="either">Match instructions that load by value or address.</param>
    /// <param name="static">Only match instructions that load static fields.</param>
    /// <param name="staticOrInstance">Match instructions that load static or instance fields.</param>
    [Pure]
    public static bool IsLdFld(this OpCode opcode, bool byRef = false, bool either = false, bool @static = false, bool staticOrInstance = false)
    {
        if (opcode == OpCodes.Ldfld)
            return (!byRef || either) && (!@static || staticOrInstance);
        if (opcode == OpCodes.Ldflda)
            return (byRef || either) && (!@static || staticOrInstance);
        if (opcode == OpCodes.Ldsfld)
            return (!byRef || either) && (@static || staticOrInstance);
        if (opcode == OpCodes.Ldsflda)
            return (byRef || either) && (@static || staticOrInstance);

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>starg</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    [Pure]
    public static bool IsStArg(this OpCode opcode)
    {
        return opcode == OpCodes.Starg || opcode == OpCodes.Starg_S;
    }

    /// <summary>
    /// Is this opcode any variants of <c>stloc</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="byRef">Only match instructions that load by address.</param>
    /// <param name="either">Match instructions that load by value or address.</param>
    [Pure]
    public static bool IsLdArg(this OpCode opcode, bool byRef = false, bool either = false)
    {
        if (opcode == OpCodes.Ldarg_S || opcode == OpCodes.Ldarg_0 || opcode == OpCodes.Ldarg_1 || opcode == OpCodes.Ldarg_2 || opcode == OpCodes.Ldarg_3 || opcode == OpCodes.Ldarg)
            return !byRef || either;
        if (opcode == OpCodes.Ldarga_S || opcode == OpCodes.Ldarga)
            return byRef || either;

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>br</c>.
    /// </summary>
    /// <remarks>Use <see cref="IsBr"/> for the same check but all parameters default to <see langword="false"/>.</remarks>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="br">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>br</c>.</param>
    /// <param name="brtrue">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>brtrue</c>.</param>
    /// <param name="brfalse">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>brfalse</c>.</param>
    /// <param name="beq">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>beq</c>.</param>
    /// <param name="bne">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bne</c>.</param>
    /// <param name="bge">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bge</c>.</param>
    /// <param name="ble">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ble</c>.</param>
    /// <param name="bgt">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bgt</c>.</param>
    /// <param name="blt">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>blt</c>.</param>
    [Pure]
    public static bool IsBrAny(this OpCode opcode, bool br = true, bool brtrue = true, bool brfalse = true,
        bool beq = true, bool bne = true, bool bge = true, bool ble = true, bool bgt = true, bool blt = true)
        => opcode.IsBr(br, brtrue, brfalse, beq, bne, bge, ble, bgt, blt);

    /// <summary>
    /// Is this opcode any variants of <c>br</c>.
    /// </summary>
    /// <remarks>Use <see cref="IsBrAny"/> for the same check but all parameters default to <see langword="true"/>.</remarks>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="br">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>br</c>.</param>
    /// <param name="brtrue">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>brtrue</c>.</param>
    /// <param name="brfalse">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>brfalse</c>.</param>
    /// <param name="beq">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>beq</c>.</param>
    /// <param name="bne">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bne</c>.</param>
    /// <param name="bge">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bge</c>.</param>
    /// <param name="ble">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ble</c>.</param>
    /// <param name="bgt">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bgt</c>.</param>
    /// <param name="blt">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>blt</c>.</param>
    [Pure]
    public static bool IsBr(this OpCode opcode, bool br = false, bool brtrue = false, bool brfalse = false,
        bool beq = false, bool bne = false, bool bge = false, bool ble = false, bool bgt = false, bool blt = false)
    {
        if (opcode == OpCodes.Br_S || opcode == OpCodes.Br)
            return br;
        if (opcode == OpCodes.Brtrue_S || opcode == OpCodes.Brtrue)
            return brtrue;
        if (opcode == OpCodes.Brfalse_S || opcode == OpCodes.Brfalse)
            return brfalse;
        if (opcode == OpCodes.Beq_S || opcode == OpCodes.Beq)
            return beq;
        if (opcode == OpCodes.Bne_Un_S || opcode == OpCodes.Bne_Un)
            return bne;
        if (opcode == OpCodes.Bge_S || opcode == OpCodes.Bge || opcode == OpCodes.Bge_Un_S || opcode == OpCodes.Bge_Un)
            return bge;
        if (opcode == OpCodes.Ble_S || opcode == OpCodes.Ble || opcode == OpCodes.Ble_Un_S || opcode == OpCodes.Ble_Un)
            return ble;
        if (opcode == OpCodes.Bgt_S || opcode == OpCodes.Bgt || opcode == OpCodes.Bgt_Un_S || opcode == OpCodes.Bgt_Un)
            return bgt;
        if (opcode == OpCodes.Blt_S || opcode == OpCodes.Blt || opcode == OpCodes.Blt_Un_S || opcode == OpCodes.Blt_Un)
            return blt;

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>ldc</c>.
    /// </summary>
    /// <remarks>Use <see cref="IsBrAny"/> for the same check but all parameters default to <see langword="true"/>.</remarks>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="int">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldc.i4</c>.</param>
    /// <param name="long">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldc.i8</c>.</param>
    /// <param name="float">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldc.r4</c>.</param>
    /// <param name="double">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldc.r8</c>.</param>
    /// <param name="string">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldstr</c>.</param>
    /// <param name="null">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldnull</c>.</param>
    [Pure]
    public static bool IsLdc(this OpCode opcode, bool @int = true, bool @long = false, bool @float = false, bool @double = false, bool @string = false, bool @null = false)
    {
        if (opcode == OpCodes.Ldc_I4_0 || opcode == OpCodes.Ldc_I4_1 || opcode == OpCodes.Ldc_I4_S ||
            opcode == OpCodes.Ldc_I4 || opcode == OpCodes.Ldc_I4_2 || opcode == OpCodes.Ldc_I4_3 ||
            opcode == OpCodes.Ldc_I4_4 || opcode == OpCodes.Ldc_I4_5 || opcode == OpCodes.Ldc_I4_6 ||
            opcode == OpCodes.Ldc_I4_7 || opcode == OpCodes.Ldc_I4_8 || opcode == OpCodes.Ldc_I4_M1)
            return @int;
        if (opcode == OpCodes.Ldc_R4)
            return @float;
        if (opcode == OpCodes.Ldc_R8)
            return @double;
        if (opcode == OpCodes.Ldc_I8)
            return @long;
        if (opcode == OpCodes.Ldstr)
            return @string;
        if (opcode == OpCodes.Ldnull)
            return @null;

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>conv</c>.
    /// </summary>
    /// <remarks>Use <see cref="IsBrAny"/> for the same check but all parameters default to <see langword="true"/>.</remarks>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="nint">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i</c> or <c>conv.u</c>.</param>
    /// <param name="byte">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i1</c> or <c>conv.u1</c>.</param>
    /// <param name="short">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i2</c> or <c>conv.u2</c>.</param>
    /// <param name="int">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i4</c> or <c>conv.u4</c>.</param>
    /// <param name="long">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i8</c> or <c>conv.u8</c>.</param>
    /// <param name="float">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.r4</c> or <c>conv.r.un</c>.</param>
    /// <param name="double">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.r8</c>.</param>
    /// <param name="fromUnsigned">Allow converting from unsigned checks.</param>
    /// <param name="toUnsigned">Allow converting to unsigned checks.</param>
    /// <param name="signed">Allow converting to signed checks.</param>
    /// <param name="overflowCheck">Allow overflow checks.</param>
    /// <param name="noOverflowCheck">Allow no overflow checks.</param>
    [Pure]
    public static bool IsConv(this OpCode opcode, bool nint = true, bool @byte = true, bool @short = true, bool @int = true, bool @long = true, bool @float = true, bool @double = true,
        bool fromUnsigned = true, bool toUnsigned = true, bool signed = true, bool overflowCheck = true, bool noOverflowCheck = true)
    {
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I || toUnsigned && opcode == OpCodes.Conv_U) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I || fromUnsigned && opcode == OpCodes.Conv_Ovf_I_Un))
            return nint;
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I1 || toUnsigned && opcode == OpCodes.Conv_U1) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I1 || fromUnsigned && opcode == OpCodes.Conv_Ovf_I1_Un))
            return @byte;
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I2 || toUnsigned && opcode == OpCodes.Conv_U2) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I2 || fromUnsigned && opcode == OpCodes.Conv_Ovf_I2_Un))
            return @short;
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I4 || toUnsigned && opcode == OpCodes.Conv_U4) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I4 || fromUnsigned && opcode == OpCodes.Conv_Ovf_I4_Un))
            return @int;
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I8 || toUnsigned && opcode == OpCodes.Conv_U8) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I8 || fromUnsigned && opcode == OpCodes.Conv_Ovf_I8_Un))
            return @long;
        if (noOverflowCheck && (opcode == OpCodes.Conv_R4 || fromUnsigned && opcode == OpCodes.Conv_R_Un))
            return @float;
        if (noOverflowCheck && opcode == OpCodes.Conv_R8)
            return @double;

        return false;
    }

    /// <summary>
    /// Return the correct call <see cref="OpCode"/> to use depending on the method. Usually you will use <see cref="GetCallRuntime"/> instead as it doesn't account for possible future keyword changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
    [Pure]
    public static OpCode GetCall(this MethodBase method)
    {
        return method.ShouldCallvirt() ? OpCodes.Callvirt : OpCodes.Call;
    }

    /// <summary>
    /// Return the correct call <see cref="OpCode"/> to use depending on the method at runtime. Doesn't account for future changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
    [Pure]
    public static OpCode GetCallRuntime(this MethodBase method)
    {
        return method.ShouldCallvirtRuntime() ? OpCodes.Callvirt : OpCodes.Call;
    }
}

#if !NETSTANDARD || NETSTANDARD2_1_OR_GREATER
/// <summary>
/// Represents a predicate for code instructions.
/// </summary>
/// <param name="instruction">The code instruction to check for a match on.</param>
/// <returns><see langword="true"/> for a match, otherwise <see langword="false"/>.</returns>
public delegate bool PatternMatch(CodeInstruction instruction);
#endif