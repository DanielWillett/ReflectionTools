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
}