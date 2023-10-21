using System;

namespace DanielWillett.ReflectionTools;


/// <summary>
/// Order members highest to lowest (where supported).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PriorityAttribute : Attribute
{
    /// <summary>
    /// Higher numbers come before lower numbers. Zero is neutral.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Set the priority of a member.
    /// </summary>
    /// <param name="priority">Higher numbers come before lower numbers. Zero is neutral.</param>
    public PriorityAttribute(int priority)
    {
        Priority = priority;
    }
}