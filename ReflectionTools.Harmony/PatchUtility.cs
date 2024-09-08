using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using DanielWillett.ReflectionTools.Emit;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Represents a predicate for code instructions.
/// </summary>
/// <param name="instruction">The code instruction to check for a match on.</param>
/// <returns><see langword="true"/> for a match, otherwise <see langword="false"/>.</returns>
public delegate bool PatternMatch(CodeInstruction instruction);

/// <summary>
/// Transpiling utilities.
/// </summary>
public static class PatchUtility
{
    private static ICodeInstructionFormatter _codeInsFormatter = new DefaultCodeInstructionFormatter();

    /// <summary>
    /// Formatter used to format <see cref="CodeInstruction"/>'s.
    /// </summary>
    public static ICodeInstructionFormatter CodeInstructionFormatter
    {
        get => _codeInsFormatter;
        set
        {
            ICodeInstructionFormatter old = Interlocked.Exchange(ref _codeInsFormatter, value);
            if (!ReferenceEquals(old, value) && old is IDisposable disp)
                disp.Dispose();

            if (Accessor.LogDebugMessages)
            {
                Accessor.Logger?.LogDebug("PatchUtility.CodeInstructionFormatter", "Code instruction formatter updated: " +
                    (value == null ? "null" : Accessor.ExceptionFormatter.Format(value.GetType())) + ".");
            }
        }
    }

    /// <summary>
    /// Returns instructions to throw the provided <typeparamref name="TException"/> with an optional <paramref name="message"/>.
    /// </summary>
    /// <exception cref="MemberAccessException">Unable to find any useable constructors for that exception.</exception>
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
            return
            [
                new CodeInstruction(OpCodes.Ldstr, message),
                new CodeInstruction(OpCodes.Newobj, info),
                new CodeInstruction(OpCodes.Throw)
            ];
        }

        return
        [
            new CodeInstruction(OpCodes.Newobj, info),
            new CodeInstruction(OpCodes.Throw)
        ];
    }

    /// <summary>
    /// Returns <see langword="true"/> if the instruction at <paramref name="index"/> and the following match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    /// <remarks><paramref name="index"/> will be incremented to the next instruction after the match.</remarks>
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
    /// Returns <see langword="true"/> if the instruction at the caret and the following match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    /// <remarks>The caret will be incremented to the next instruction after the match.</remarks>
    public static bool FollowPattern(TranspileContext instructions, params PatternMatch?[] matches)
    {
        if (MatchPattern(instructions, matches))
        {
            instructions.CaretIndex += matches.Length;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> and removes the instructions at <paramref name="index"/> and the following if they match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
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
    /// Returns a block with info of the removed instructions and removes the instructions at the caret and the following if they match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    /// <remarks>The return block will be of size 0 if no matches were found.</remarks>
    public static BlockInfo RemovePattern(TranspileContext instructions, params PatternMatch?[] matches)
    {
        if (MatchPattern(instructions, matches))
        {
            return instructions.Remove(matches.Length);
        }

        return new BlockInfo(Array.Empty<InstructionBlockInfo>(), instructions.CaretIndex);
    }

    /// <summary>
    /// Creates a block with info of the removed instructions and removes the instructions at the caret and the following if they match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    /// <remarks>The outputted block will be of size 0 if no matches were found.</remarks>
    public static bool TryRemovePattern(TranspileContext instructions, out BlockInfo block, params PatternMatch?[] matches)
    {
        if (MatchPattern(instructions, matches))
        {
            block = instructions.Remove(matches.Length);
            return true;
        }

        block = new BlockInfo(Array.Empty<InstructionBlockInfo>(), instructions.CaretIndex);
        return false;
    }
    
    /// <summary>
    /// Creates a block with info of the removed instructions and removes the instructions at the caret and the following if they match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    /// <remarks>The outputted block will be of size 0 if no matches were found.</remarks>
    public static bool TryReplacePattern(TranspileContext instructions, Action<IOpCodeEmitter> replaceWith, params PatternMatch?[] matches)
    {
        if (!MatchPattern(instructions, matches))
            return false;

        instructions.Replace(matches.Length, replaceWith);
        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the instruction at <paramref name="index"/> and the following match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    [Pure]
    public static bool MatchPattern(IList<CodeInstruction> instructions, int index, params PatternMatch?[] matches)
    {
        int c = matches.Length;
        if (c <= 0 || index >= instructions.Count - c)
            return false;
        for (int i = index; i < index + c; ++i)
        {
            PatternMatch? pattern = matches[i - index];
            if (pattern != null && !pattern.Invoke(instructions[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the instruction at the caret and the following match <paramref name="matches"/>. Pass <see langword="null"/> as a wildcard match.
    /// </summary>
    [Pure]
    public static bool MatchPattern(TranspileContext instructions, params PatternMatch?[] matches)
    {
        int c = matches.Length;

        if (c <= 0 || instructions.CaretIndex + c > instructions.Count)
            return false;

        int index = instructions.CaretIndex;
        for (int i = index; i < index + c; ++i)
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
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="ArgumentException"><paramref name="checker"/> is not static.</exception>
    /// <returns>Amount of instructions inserted.</returns>
    public static int ReturnIfFalse(IList<CodeInstruction> instructions, ILGenerator generator, ref int index, Func<bool> checker, Label? @goto = null)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException($"Unable to add ReturnIfFalse ({Accessor.ExceptionFormatter.Format(checker.Method)}), index is too small: {index}.", nameof(index));
        if (index >= instructions.Count)
            throw new ArgumentOutOfRangeException($"Unable to add ReturnIfFalse ({Accessor.ExceptionFormatter.Format(checker.Method)}), index is too large: {index}.", nameof(index));
        if (!checker.Method.IsStatic)
            throw new ArgumentException("Checker must be static.", nameof(checker));

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
    /// Inserts instructions to execute <paramref name="checker"/> and return (or optionally branch to <paramref name="goto"/>) if it returns <see langword="false"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="checker"/> is not static.</exception>
    /// <returns>Amount of instructions inserted.</returns>
    public static int ReturnIfFalse(TranspileContext instructions, Func<bool> checker, Label? @goto = null)
    {
        if (!checker.Method.IsStatic)
            throw new ArgumentException("Checker must be static.", nameof(checker));

        if (@goto.HasValue)
        {
            Label lbl = @goto.Value;
            instructions.EmitAbove(emit =>
            {
                emit.Invoke(checker.Method)
                    .BranchIfFalse(lbl);
            });
            return 2;
        }

        Label continueLbl = instructions.DefineLabel();

        instructions.EmitAbove(emit =>
        {
            emit.Invoke(checker.Method)
                .BranchIfTrue(continueLbl)
                .Return();
        });

        if (instructions.Count > instructions.CaretIndex)
            instructions.Instruction.labels.Add(continueLbl);
        else
            instructions.MarkLabel(continueLbl);

        return 3;
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
            if (!match(instructions[i]))
                continue;

            index = includeMatch ? i : i + 1;
            if (includeMatch)
                --amt;
            break;
        }
        return amt;
    }

    /// <summary>
    /// Increment the caret until <paramref name="match"/> matches or the function ends.
    /// </summary>
    /// <returns>Amount of instructions skipped.</returns>
    [Pure]
    public static int ContinueUntil(TranspileContext instructions, PatternMatch match, bool includeMatch = true)
    {
        int amt = 0;
        for (int i = instructions.CaretIndex; i < instructions.Count; ++i)
        {
            ++amt;
            if (!match(instructions[i]))
                continue;

            instructions.CaretIndex = includeMatch ? i : i + 1;
            if (includeMatch)
                --amt;
            break;
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
            if (match(instructions[i]))
                continue;

            index = includeNext ? i : i - 1;
            if (!includeNext)
                --amt;
            break;
        }
        return amt;
    }

    /// <summary>
    /// Increment the caret until <paramref name="match"/> fails or the function ends.
    /// </summary>
    /// <returns>Amount of instructions skipped.</returns>
    [Pure]
    public static int ContinueWhile(TranspileContext instructions, PatternMatch match, bool includeNext = true)
    {
        int amt = 0;
        for (int i = instructions.CaretIndex; i < instructions.Count; ++i)
        {
            ++amt;
            if (match(instructions[i]))
                continue;

            instructions.CaretIndex = includeNext ? i : i - 1;
            if (!includeNext)
                --amt;
            break;
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
            if (!match(instructions[i]))
                continue;

            int newIndex = i + shift;
            if (instructions.Count <= i)
                continue;

            instructions[newIndex].labels.Add(label);
            return true;
        }

        if (!labelRtnIfFailure)
            return false;

        if (instructions[instructions.Count - 1].opcode == OpCodes.Ret)
            instructions[instructions.Count - 1].labels.Add(label);
        else
        {
            CodeInstruction instruction = new CodeInstruction(OpCodes.Ret);
            instruction.labels.Add(label);
            instructions.Add(instruction);
        }
        return false;
    }

    /// <summary>
    /// Add <paramref name="label"/> to the next instruction that matches <paramref name="match"/>.
    /// </summary>
    [Pure]
    public static bool LabelNext(TranspileContext instructions, Label label, PatternMatch match, int shift = 0, bool labelRtnIfFailure = false)
    {
        for (int i = instructions.CaretIndex; i < instructions.Count; ++i)
        {
            if (!match(instructions[i]))
                continue;

            int newIndex = i + shift;
            if (instructions.Count <= i)
                continue;

            instructions[newIndex].labels.Add(label);
            return true;
        }

        if (!labelRtnIfFailure)
            return false;

        if (instructions[instructions.Count - 1].opcode == OpCodes.Ret)
            instructions[instructions.Count - 1].labels.Add(label);
        else
        {
            CodeInstruction instruction = new CodeInstruction(OpCodes.Ret);
            instruction.labels.Add(label);
            
            instructions.Instructions.Add(instruction);
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
            if (!match(instructions[i]))
                continue;

            int newIndex = i + shift;
            if (instructions.Count <= i)
                continue;

            Label label = generator.DefineLabel();
            instructions[newIndex].labels.Add(label);
            return label;
        }
        return null;
    }

    /// <summary>
    /// Get a label to the next instruction that matches <paramref name="match"/>.
    /// </summary>
    [Pure]
    public static Label? LabelNext(TranspileContext instructions, PatternMatch match, int shift = 0)
    {
        for (int i = instructions.CaretIndex; i < instructions.Count; ++i)
        {
            if (!match(instructions[i]))
                continue;

            int newIndex = i + shift;
            if (instructions.Count <= i)
                continue;

            Label label = instructions.DefineLabel();
            instructions[newIndex].labels.Add(label);
            return label;
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
            if (match != null && !match(instructions[i]))
                continue;

            int newIndex = i + shift;
            if (instructions.Count <= i)
                continue;

            instruction = instructions[newIndex];
            if (allowUseExisting && instruction.labels.Count > 0)
                return instruction.labels[instruction.labels.Count - 1];
            Label label = generator.DefineLabel();
            instruction.labels.Add(label);
            return label;
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
    /// Get a label to the next instruction that matches <paramref name="match"/>, or the end of the function.
    /// </summary>
    [Pure]
    public static Label LabelNextOrReturn(TranspileContext instructions, PatternMatch? match, int shift = 0, bool allowUseExisting = true)
    {
        CodeInstruction instruction;
        for (int i = instructions.CaretIndex; i < instructions.Count; ++i)
        {
            if (match != null && !match(instructions[i]))
                continue;

            int newIndex = i + shift;
            if (instructions.Count <= i)
                continue;

            instruction = instructions[newIndex];
            if (allowUseExisting && instruction.labels.Count > 0)
                return instruction.labels[instruction.labels.Count - 1];
            Label label = instructions.DefineLabel();
            instruction.labels.Add(label);
            return label;
        }

        instruction = instructions[instructions.Count - 1];
        if (instruction.opcode == OpCodes.Ret)
        {
            if (allowUseExisting && instruction.labels.Count > 0)
                return instruction.labels[instruction.labels.Count - 1];
            Label label = instructions.DefineLabel();
            instruction.labels.Add(label);
            return label;
        }
        else
        {
            Label label = instructions.DefineLabel();
            instruction = new CodeInstruction(OpCodes.Ret);
            instruction.labels.Add(label);
            instructions.Instructions.Add(instruction);
            return label;
        }
    }

    /// <summary>
    /// Get the label of the next branch instruction.
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
    /// Get the label of the next branch instruction.
    /// </summary>
    [Pure]
    public static Label? GetNextBranchTarget(TranspileContext instructions)
    {
        for (int i = instructions.CaretIndex; i < instructions.Count; ++i)
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
    /// Find the index of the code instruction to which the label refers.
    /// </summary>
    [Pure]
    public static int FindLabelDestinationIndex(TranspileContext instructions, Label label)
    {
        for (int i = instructions.CaretIndex; i < instructions.Count; ++i)
        {
            List<Label>? labels = instructions[i].labels;
            if (labels == null)
                continue;

            for (int j = 0; j < labels.Count; ++j)
            {
                if (labels[j] == label)
                    return i;
            }
        }
        for (int i = 0; i < instructions.CaretIndex; ++i)
        {
            List<Label>? labels = instructions[i].labels;
            if (labels == null)
                continue;

            for (int j = 0; j < labels.Count; ++j)
            {
                if (labels[j] == label)
                    return i;
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
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use WithEndBlocksFrom, note the arguments are switched.")]
    public static CodeInstruction WithEndingInstructionNeeds(this CodeInstruction instruction, CodeInstruction other)
    {
        TransferEndingInstructionNeeds(instruction, other);
        return instruction;
    }

    /// <summary>
    /// Transfers all labels and blocks that would be on the first instruction of a block to the target instruction.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use WithStartBlocksFrom, note the arguments are switched.")]
    public static CodeInstruction WithStartingInstructionNeeds(this CodeInstruction instruction, CodeInstruction other)
    {
        TransferStartingInstructionNeeds(instruction, other);
        return instruction;
    }

    /// <summary>
    /// Transfers blocks that would be on the last instruction of a block from <paramref name="other"/> to <paramref name="instruction"/>.
    /// </summary>
    public static CodeInstruction WithEndBlocksFrom(this CodeInstruction instruction, CodeInstruction other)
    {
        TransferEndingInstructionNeeds(other, instruction);
        return instruction;
    }

    /// <summary>
    /// Transfers blocks that would be on the first instruction of a block from <paramref name="other"/> to <paramref name="instruction"/>.
    /// </summary>
    public static CodeInstruction WithStartBlocksFrom(this CodeInstruction instruction, CodeInstruction other)
    {
        TransferStartingInstructionNeeds(other, instruction);
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
    public static bool IsBeginBlockType(this ExceptionBlockType type) => (int)type is >= 0 and < 5;

    /// <summary>
    /// Would this block type end a block?
    /// </summary>
    [Pure]
    public static bool IsEndBlockType(this ExceptionBlockType type) => type == ExceptionBlockType.EndExceptionBlock;
}