using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;

namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Defines an <see cref="IOpCodeEmitter"/> that's known to be at the top level of the method (not in an exception block).
/// </summary>
public interface IRootOpCodeEmitter : IOpCodeEmitter;

/// <summary>
/// Defines the correct restrictions for an emitter created from the top of a method.
/// </summary>
/// <param name="underlying"></param>
public class RootEmitterWrapper(IOpCodeEmitter underlying) : EmitterWrapper(underlying, DisallowedOpCodes), IRootOpCodeEmitter
{
    private int _exceptions;
    private static readonly HashSet<OpCode> DisallowedOpCodes =
    [
        OpCodes.Endfilter, OpCodes.Endfinally, OpCodes.Prefix1, OpCodes.Prefix2,
        OpCodes.Prefix3, OpCodes.Prefix4, OpCodes.Prefix5, OpCodes.Prefix6,
        OpCodes.Prefix7, OpCodes.Prefixref,
        OpCodes.Rethrow
    ];

    /// <inheritdoc />
    public override Label? BeginExceptionBlock()
    {
        Interlocked.Increment(ref _exceptions);
        return base.BeginExceptionBlock();
    }

    /// <inheritdoc />
    public override void EndExceptionBlock()
    {
        Interlocked.Decrement(ref _exceptions);
        base.EndExceptionBlock();
    }

    /// <inheritdoc />
    public override void BeginCatchBlock(Type? exceptionType)
    {
        if (_exceptions <= 0)
            throw new NotSupportedException("Can only begin a catch block in an exception block.");
    }

    /// <inheritdoc />
    public override void BeginExceptFilterBlock()
    {
        if (_exceptions <= 0)
            throw new NotSupportedException("Can only begin a filter block in an exception block.");
    }

    /// <inheritdoc />
    public override void BeginFinallyBlock()
    {
        if (_exceptions <= 0)
            throw new NotSupportedException("Can only begin a finally block in an exception block.");
    }

    /// <inheritdoc />
    public override void BeginFaultBlock()
    {
        if (_exceptions <= 0)
            throw new NotSupportedException("Can only begin a fault block in an exception block.");
    }
}
