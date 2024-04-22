using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;

namespace DanielWillett.ReflectionTools.Harmony.Formatting;

/// <summary>
/// Converts a code instruction into a string representation
/// </summary>
public interface ICodeInstructionFormatter
{
    /// <summary>
    /// Convert a code instruction to a string representation.
    /// </summary>
    string FormatCodeInstruction(CodeInstruction codeInstruction, OpCodeFormattingContext usageContext);
}