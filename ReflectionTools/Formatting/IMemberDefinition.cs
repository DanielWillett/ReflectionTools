using System;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Abstracted interface for all member defintion builders.
/// </summary>
public interface IMemberDefinition
{
    /// <summary>
    /// Name of the member.
    /// </summary>
    string? Name { get; set; }

    /// <summary>
    /// Type the member is declared in.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    Type? DeclaringType { get; }

    /// <summary>
    /// If the member requires an instance of <see cref="DeclaringType"/> to be accessed.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    bool IsStatic { get; }

    /// <summary>
    /// Set the declaring type of the member.
    /// </summary>
    IMemberDefinition NestedIn<TDeclaringType>(bool isStatic);

    /// <summary>
    /// Set the declaring type of the member.
    /// </summary>
    IMemberDefinition NestedIn(Type declaringType, bool isStatic);

    /// <summary>
    /// Set the declaring type of the member.
    /// </summary>
    IMemberDefinition NestedIn(string declaringType, bool isStatic);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(IOpCodeFormatter,Span{char})"/>.
    /// </summary>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    /// <returns>The length in characters of this as a string.</returns>
    int GetFormatLength(IOpCodeFormatter formatter);

    /// <summary>
    /// Format this into a string representation. Use <see cref="GetFormatLength(IOpCodeFormatter)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    /// <returns>The length in characters of this as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(IOpCodeFormatter formatter, Span<char> output);
#endif

    /// <summary>
    /// Format this into a string representation.
    /// </summary>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    string Format(IOpCodeFormatter formatter);
}