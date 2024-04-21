using System.Runtime.CompilerServices;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Simplified enumeration for defining member visibilities (accessibility levels).
/// </summary>
public enum MemberVisibility
{
    /// <summary>
    /// Unknown visibility, usually meaning an invalid member definition.
    /// </summary>
    Unknown,

    /// <summary>
    /// Accessible only to the declaring type and it's nested types.
    /// </summary>
    /// <remarks>Designated <see langword="Private"/> in the CLR.</remarks>
    Private,

    /// <summary>
    /// Accessible to all types deriving from the declaring type or types nested within those types
    /// that are also within the defining assembly (or within assemblies that have been given access with the <see cref="InternalsVisibleToAttribute"/>).
    /// </summary>
    /// <remarks>Designated <see langword="FamANDAssem"/> in the CLR.</remarks>
    PrivateProtected,

    /// <summary>
    /// Accessible to all types deriving from the declaring type or types nested within those types.
    /// </summary>
    /// <remarks>Designated <see langword="Family"/> in the CLR.</remarks>
    Protected,

    /// <summary>
    /// Accessible to all types deriving from the declaring type or types nested within those types,
    /// or within the defining assembly (or within assemblies that have been given access with the <see cref="InternalsVisibleToAttribute"/>).
    /// </summary>
    /// <remarks>Designated <see langword="FamORAssem"/> in the CLR.</remarks>
    ProtectedInternal,

    /// <summary>
    /// Accessible to all types within the defining assembly (or within assemblies that have been given access with the <see cref="InternalsVisibleToAttribute"/>).
    /// </summary>
    /// <remarks>Designated <see langword="Assembly"/> in the CLR.</remarks>
    Internal,

    /// <summary>
    /// Accessible to all types.
    /// </summary>
    /// <remarks>Designated <see langword="Public"/> in the CLR.</remarks>
    Public
}