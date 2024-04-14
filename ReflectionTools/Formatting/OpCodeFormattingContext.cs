namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Defines how a set of an op code and operand formatting result will be used, which may influence formatting.
/// </summary>
public enum OpCodeFormattingContext
{
    /// <summary>
    /// Used in-line in a log message, etc.
    /// </summary>
    InLine,

    /// <summary>
    /// Used in a list of code instructions, i.e. listing all code instructions in a method. Each instruction will be put on a new line.
    /// </summary>
    List
}