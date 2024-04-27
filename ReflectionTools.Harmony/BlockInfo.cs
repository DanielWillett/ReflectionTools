using HarmonyLib;
using System;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Extension methods relating to <see cref="BlockInfo"/>
/// </summary>
public static class BlockInfoExtensions
{
    /// <summary>
    /// Moves any labels and needed blocks that were on the start of the previous block to the given instruction, which should start the new block.
    /// </summary>
    /// <returns>The instance of the same instruction for method chaining.</returns>
    public static CodeInstruction SetupBlockStart(this CodeInstruction instruction, in BlockInfo block)
    {
        return block.SetupBlockStart(instruction);
    }

    /// <summary>
    /// Moves any labels and needed blocks that were on the end of the previous block to the given instruction, which should end the new block.
    /// </summary>
    /// <returns>The instance of the same instruction for method chaining.</returns>
    public static CodeInstruction SetupBlockEnd(this CodeInstruction instruction, in BlockInfo block)
    {
        return block.SetupBlockEnd(instruction);
    }
}

/// <summary>
/// Information about the labels and exception blocks in a group of instructions.
/// </summary>
public readonly struct BlockInfo
{
    /// <summary>
    /// All instructions in the block.
    /// </summary>
    public readonly InstructionBlockInfo[] Instructions;

    /// <summary>
    /// The first instruction in the block
    /// </summary>
    public ref readonly InstructionBlockInfo First => ref Instructions[0];

    /// <summary>
    /// The last instruction in the block.
    /// </summary>
    public ref readonly InstructionBlockInfo Last => ref Instructions[Instructions.Length - 1];

    /// <summary>
    /// Get the instruction at the given index in this block.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException"/>
    public ref readonly InstructionBlockInfo this[int index] => ref Instructions[index];

    /// <summary>
    /// Size of the block in instructions.
    /// </summary>
    public int Length => Instructions.Length;
    internal BlockInfo(InstructionBlockInfo[] instructions)
    {
        Instructions = instructions;
    }

    /// <summary>
    /// Moves any labels and needed blocks that were on the start of the previous block to the given instruction, which should start the new block.
    /// </summary>
    /// <returns>The instance of the same instruction for method chaining.</returns>
    public CodeInstruction SetupBlockStart(CodeInstruction instruction)
    {
        ref readonly InstructionBlockInfo startInfo = ref First;
        instruction.labels.AddRange(startInfo.Labels);
        for (int i = 0; i < startInfo.ExceptionBlocks.Length; ++i)
        {
            ExceptionBlock block = startInfo.ExceptionBlocks[i];
            if (!block.blockType.IsBeginBlockType())
                continue;

            instruction.blocks.Add(block);
        }

        return instruction;
    }

    /// <summary>
    /// Moves any labels and needed blocks that were on the end of the previous block to the given instruction, which should end the new block.
    /// </summary>
    /// <returns>The instance of the same instruction for method chaining.</returns>
    public CodeInstruction SetupBlockEnd(CodeInstruction instruction)
    {
        ref readonly InstructionBlockInfo endInfo = ref Last;
        for (int i = 0; i < endInfo.ExceptionBlocks.Length; ++i)
        {
            ExceptionBlock block = endInfo.ExceptionBlocks[i];
            if (!block.blockType.IsEndBlockType())
                continue;

            instruction.blocks.Add(block);
        }

        return instruction;
    }
}

/// <summary>
/// Information about the labels and exception blocks in one instruction.
/// </summary>
public readonly struct InstructionBlockInfo
{
    /// <summary>
    /// OpCode of the instruction.
    /// </summary>
    public readonly OpCode OpCode;
    
    /// <summary>
    /// Operand of the instruction.
    /// </summary>
    public readonly object? Operand;

    /// <summary>
    /// Array of all labels in the instruction.
    /// </summary>
    public readonly Label[] Labels;

    /// <summary>
    /// Array of all exception blocks in the instruction.
    /// </summary>
    public readonly ExceptionBlock[] ExceptionBlocks;
    internal InstructionBlockInfo(OpCode opCode, object operand, Label[] labels, ExceptionBlock[] exceptionBlocks)
    {
        OpCode = opCode;
        Operand = operand;
        Labels = labels;
        ExceptionBlocks = exceptionBlocks;
    }
}