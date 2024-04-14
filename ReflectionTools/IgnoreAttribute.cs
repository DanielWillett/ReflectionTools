using System;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Ignore members from being checked by reflection (where supported).
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false)]
public sealed class IgnoreAttribute : Attribute;