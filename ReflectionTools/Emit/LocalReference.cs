using System;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Allows referencing a local by it's index or <see cref="LocalBuilder"/>.
/// </summary>
public readonly struct LocalReference
{
    /// <summary>
    /// Stored this way so a <see langword="default"/> reference can be detected.
    /// </summary>
    private readonly int _indexPlusOne;

    /// <summary>
    /// A reference to the local variable builder.
    /// </summary>
    public LocalBuilder? Local { get; }

    /// <summary>
    /// The index of the local variable.
    /// </summary>
    public int Index => _indexPlusOne - 1;

    /// <summary>
    /// Reference a local based on it's index.
    /// </summary>
    /// <remarks>The index has to be 0-3, otherwise you need to pass the <see cref="LocalBuilder"/> instead.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index was less than zero.</exception>
    /// <exception cref="ArgumentException">The index was greater than three.</exception>
    public LocalReference(int index)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (index > 3)
            throw new ArgumentException("Can not load a local by index unless it's #0-3.", nameof(index));

        _indexPlusOne = index + 1;
    }

    /// <summary>
    /// Reference a local by it's local variable builder.
    /// </summary>
    public LocalReference(LocalBuilder builder)
    {
        _indexPlusOne = builder.LocalIndex + 1;
        Local = builder;
    }

    /// <summary>
    /// Reference a local by it's local variable builder.
    /// </summary>
    public static implicit operator LocalReference(LocalBuilder local) => new LocalReference(local);

    /// <summary>
    /// Reference a local based on it's index.
    /// </summary>
    /// <remarks>The index has to be 0-3, otherwise you need to pass the <see cref="LocalBuilder"/> instead.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index was less than zero.</exception>
    /// <exception cref="ArgumentException">The index was greater than three.</exception>
    public static explicit operator LocalReference(int index) => new LocalReference(index);
}