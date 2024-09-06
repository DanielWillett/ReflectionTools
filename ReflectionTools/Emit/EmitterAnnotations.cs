using System;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Defines how an extension method for <see cref="IOpCodeEmitter"/> changes the stack.
/// </summary>
/// <remarks>Can be used by analyzers.</remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EmitBehaviorAttribute : Attribute
{
    /// <summary>
    /// How this function removes elements from the stack.
    /// </summary>
    public StackBehaviour PopBehavior { get; }

    /// <summary>
    /// How this function adds elements to the stack.
    /// </summary>
    public StackBehaviour PushBehavior { get; }

    /// <summary>
    /// Defines a preset special behavior
    /// </summary>
    public EmitSpecialBehavior SpecialBehavior { get; set; }

    /// <summary>
    /// Defines how an extension method for <see cref="IOpCodeEmitter"/> changes the stack.
    /// </summary>
    public EmitBehaviorAttribute(StackBehaviour popBehavior = StackBehaviour.Pop0, StackBehaviour pushBehavior = StackBehaviour.Push0)
    {
        PopBehavior = popBehavior;
        PushBehavior = pushBehavior;
    }
}

/// <summary>
/// Marks that the return value of a method, property, or field starts a new <see cref="IOpCodeEmitter"/>.
/// </summary>
/// <remarks>Can be used by analyzers.</remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class StartsEmitterAttribute : Attribute;

/// <summary>
/// Marks that this function ends the emitter attached to the dynamic method this references.
/// </summary>
/// <remarks>Can be used by analyzers.</remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EndsEmitterAttribute : Attribute;

/// <summary>
/// Defines special cases with <see cref="EmitBehaviorAttribute"/>.
/// </summary>
[Flags]
public enum EmitSpecialBehavior
{
    /// <summary>
    /// Expects parameter (int times). If push is a multiple push, only the last push is repeated (ex. push1_push1, only the last push1 will be repeated).
    /// </summary>
    RepeatPush = 1 << 0,

    /// <summary>
    /// Expects parameter (int times). If pop is a multiple pop, only the last pop is repeated (ex. pop1_pop1, only the last pop1 will be repeated).
    /// </summary>
    RepeatPop = 1 << 1,

    /// <summary>
    /// Expects parameter (int times).
    /// </summary>
    RepeatPushAndPop = RepeatPush | RepeatPop,

    /// <summary>
    /// Expects parameter (Type arrayType) and for pop behavior to be Popref_popi or Popref_popi_pop1. The popi will be repeated (arrayType's rank) times.
    /// </summary>
    /// <remarks>Popi will occur the same amount of times as the arrayType's rank.</remarks>
    PopIndices = 1 << 2,

    /// <summary>
    /// Expects parameters (Type arrayType, bool hasStartIndices) and for pop behavior to be Popref_popi.
    /// </summary>
    /// <remarks>Popi will occur the same amount of times as the arrayType's rank, or double if hasStartIndices is <see langword="true"/>.</remarks>
    PopIndexBoundsAndLengths = 1 << 3,

    /// <summary>
    /// Expects parameters (Expression field) with Popref pop behavior.
    /// </summary>
    /// <remarks>Popref will only occur if field points at a non-static field.</remarks>
    PopRefIfNotStaticExpression = 1 << 4,

    /// <summary>
    /// The last pop in this pop will be transformed to whatever the last generic argument is.
    /// </summary>
    PopGenericTypeLast = 1 << 5,

    /// <summary>
    /// The last pop in this pop will be transformed to whatever the first <see cref="Type"/> parameter is.
    /// </summary>
    PopArgumentTypeLast = 1 << 6,

    /// <summary>
    /// The last push in this push will be transformed to whatever the last generic argument is.
    /// </summary>
    PushGenericTypeLast = 1 << 7,

    /// <summary>
    /// The last push in this push will be transformed to whatever the first <see cref="Type"/> parameter is.
    /// </summary>
    PushArgumentTypeLast = 1 << 8,

    /// <summary>
    /// Marks the first label in parameters if it isn't null (<see cref="Nullable{T}"/>).
    /// </summary>
    MarksLabel = 1 << 9,

    /// <summary>
    /// Indicates that this function will end the current branch's execution.
    /// </summary>
    TerminatesBranch = 1 << 10,

    /// <summary>
    /// Requires that this function be ran inside a catch block.
    /// </summary>
    RequireBlockCatch = 1 << 11,

    /// <summary>
    /// Requires that this function not be ran inside a try, filter, catch, or finally block.
    /// </summary>
    RequireNotInNoJumpBlock = RequireNotInFinallyBlock | (1 << 12),

    /// <summary>
    /// Requires that this function not be ran inside a finally block.
    /// </summary>
    RequireNotInFinallyBlock = 1 << 13,

    /// <summary>
    /// Indicates that this instruction branches to the first <see cref="Label"/> parameter.
    /// </summary>
    Branches = 1 << 14,

    /// <summary>
    /// Indicates that this instruction empties the stack.
    /// </summary>
    ClearsStack = 1 << 15
}