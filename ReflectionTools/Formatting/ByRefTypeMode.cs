namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Describes the way a by-ref type is passed as a parameter.
/// </summary>
public enum ByRefTypeMode
{
    /// <summary>
    /// Don't write a keyword in front, even if it is a by-ref type.
    /// </summary>
    Ignore,

    /// <summary>
    /// Represents a parameter or return type passed with <see langword="ref"/>.
    /// </summary>
    Ref,

    /// <summary>
    /// Represents a parameter passed with <see langword="in"/>.
    /// </summary>
    In,

    /// <summary>
    /// Represents a return type passed with <see langword="ref readonly"/>.
    /// </summary>
    RefReadonly,

    /// <summary>
    /// Represents a parameter passed with <see langword="out"/>.
    /// </summary>
    Out
}