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
    RefReadOnly,

    /// <summary>
    /// Represents a parameter passed with <see langword="out"/>.
    /// </summary>
    Out,

    /// <summary>
    /// Represents a parameter passed with <see langword="scoped ref"/>.
    /// </summary>
    ScopedRef,

    /// <summary>
    /// Represents a parameter passed with <see langword="scoped in"/>.
    /// </summary>
    ScopedIn,

    /// <summary>
    /// Represents a parameter passed with <see langword="scoped ref readonly"/>.
    /// </summary>
    ScopedRefReadOnly
}