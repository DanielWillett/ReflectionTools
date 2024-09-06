using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Utility for laying out exception blocks in a cleaner way.
/// </summary>
public class ExceptionBlockBuilder
{
    private readonly IOpCodeEmitter _emitter;
    private bool _hasHandler;
    private bool _isClosed;

    /// <summary>
    /// The label for the end of the block, if the implementation supports it, otherwise <see langword="null"/>. This will leave you in the correct place to execute finally blocks or to finish the try.
    /// </summary>
    public Label? EndingLabel { get; }

    /// <summary>
    /// Create a new <see cref="ExceptionBlockBuilder"/> and begin an exception block.
    /// </summary>
    public ExceptionBlockBuilder(IOpCodeEmitter emitter, Action<IOpCodeEmitter> tryBlock)
    {
        EmitterWrapper.Reduce(ref emitter);
        _emitter = emitter;
        EndingLabel = emitter.BeginExceptionBlock();

        TryBlockEmitterWrapper wrapper = new TryBlockEmitterWrapper(emitter);
        tryBlock(wrapper);
    }

    private void AssertNotClosed()
    {
        if (_isClosed)
            throw new ObjectDisposedException("This exception block has already been closed.", (Exception?)null);
    }

    /// <summary>
    /// Start a catch handler block for all remaining exceptions. Catch handlers can't co-exist with fault blocks.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public ExceptionBlockBuilder Catch(Action<IOpCodeEmitter> catchHandler)
    {
        CatchIntl(typeof(object), catchHandler);
        return this;
    }

    /// <summary>
    /// Start a catch handler block for exceptions of type <typeparamref name="TException"/>. Catch handlers can't co-exist with fault blocks.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public ExceptionBlockBuilder Catch<TException>(Action<IOpCodeEmitter> catchHandler) where TException : Exception
    {
        return Catch(typeof(TException), catchHandler);
    }

    /// <summary>
    /// Start a catch handler block for exceptions of type <paramref name="baseExceptionType"/>. Catch handlers can't co-exist with fault blocks.
    /// </summary>
    /// <exception cref="ArgumentException">Type is not assignable to <see cref="Exception"/> (or an interface).</exception>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public ExceptionBlockBuilder Catch(Type baseExceptionType, Action<IOpCodeEmitter> catchHandler)
    {
        if (!baseExceptionType.IsInterface && !typeof(Exception).IsAssignableFrom(baseExceptionType))
            throw new ArgumentException($"Expected a type deriving from {Accessor.ExceptionFormatter.Format(typeof(Exception))}.", nameof(baseExceptionType));

        CatchIntl(baseExceptionType, catchHandler);
        return this;
    }
    private void CatchIntl(Type baseExceptionType, Action<IOpCodeEmitter> catchHandler)
    {
        AssertNotClosed();
        _hasHandler = true;

        IOpCodeEmitter emitter = _emitter;
        emitter.BeginCatchBlock(baseExceptionType);
        if (catchHandler == null)
        {
            emitter.Emit(OpCodes.Pop);
        }
        else
        {
            CatchBlockEmitterWrapper wrapper = new CatchBlockEmitterWrapper(emitter);
            catchHandler(wrapper);
            if (!wrapper.HasEmittedStackChanges)
                emitter.Emit(OpCodes.Pop);
        }
    }

    /// <summary>
    /// Start a finally block in the exception block. Finally blocks can't co-exist with fault blocks.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public ExceptionBlockBuilder Finally(Action<IOpCodeEmitter> finallyHandler)
    {
        AssertNotClosed();
        _hasHandler = true;

        IOpCodeEmitter emitter = _emitter;
        emitter.BeginFinallyBlock();
        if (finallyHandler == null)
            return this;

        FinallyBlockEmitterWrapper wrapper = new FinallyBlockEmitterWrapper(emitter);
        finallyHandler.Invoke(wrapper);
        return this;
    }

    /// <summary>
    /// Start a fault block in the exception block. Fault blocks can't exist with any other handlers.
    /// </summary>
    /// <remarks>Note that fault blocks aren't supported for <see cref="DynamicMethod"/>'s in some runtimes including .NET Framework. They are supported in .NET Core.</remarks>
    /// <exception cref="NotSupportedException">Fault blocks aren't supported in <see cref="DynamicMethod"/> in some runtimes. They are supported in .NET Core.</exception>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public ExceptionBlockBuilder Fault(Action<IOpCodeEmitter> faultHandler)
    {
        AssertNotClosed();
        _hasHandler = true;

        IOpCodeEmitter emitter = _emitter;
        emitter.BeginFaultBlock();
        if (faultHandler == null)
            return this;

        FaultBlockEmitterWrapper wrapper = new FaultBlockEmitterWrapper(emitter);
        faultHandler.Invoke(wrapper);
        return this;
    }

    internal void ApplyFilterHandler(Action<IOpCodeEmitter> filterHandler)
    {
        AssertNotClosed();
        _hasHandler = true;

        IOpCodeEmitter emitter = _emitter;
        emitter.BeginCatchBlock(null);
        if (filterHandler == null)
        {
            emitter.Emit(OpCodes.Pop);
        }
        else
        {
            CatchBlockEmitterWrapper wrapper = new CatchBlockEmitterWrapper(emitter);
            filterHandler(wrapper);
            if (!wrapper.HasEmittedStackChanges)
                emitter.Emit(OpCodes.Pop);
        }
    }

    /// <summary>
    /// Adds a filter block (<see langword="when"/> in C#) to the exception block. Filter handlers can't co-exist with fault blocks.
    /// </summary>
    /// <remarks>Note that filter blocks aren't supported for <see cref="DynamicMethod"/>'s in some runtimes including .NET Framework. They are supported in .NET Core.</remarks>
    /// <exception cref="NotSupportedException">Filter blocks aren't supported in <see cref="DynamicMethod"/> in some runtimes. They are supported in .NET Core.</exception>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public ExceptionBlockFilterBuilder CatchWhen(Action<IOpCodeEmitter> exceptionHandler)
    {
        AssertNotClosed();

        IOpCodeEmitter emitter = _emitter;
        emitter.BeginExceptFilterBlock();
        if (exceptionHandler == null)
        {
            emitter.Emit(OpCodes.Pop);
            emitter.Emit(OpCodes.Ldc_I4_0);
        }
        else
        {
            FilterBlockEmitterWrapper wrapper = new FilterBlockEmitterWrapper(emitter);
            exceptionHandler(wrapper);
            if (!wrapper.HasEmittedStackChanges)
            {
                emitter.Emit(OpCodes.Pop);
                emitter.Emit(OpCodes.Ldc_I4_0);
            }
        }
        // endfilter is automatically appended
        return new ExceptionBlockFilterBuilder(this);
    }

    /// <summary>
    /// Adds a filter block (<see langword="when"/> in C#) to the exception block that starts with a type check. Filters can't co-exist with fault blocks.
    /// </summary>
    /// <remarks>Note that filter blocks aren't supported for <see cref="DynamicMethod"/>'s in some runtimes including .NET Framework. They are supported in .NET Core.</remarks>
    /// <exception cref="NotSupportedException">Filter blocks aren't supported in <see cref="DynamicMethod"/> in some runtimes. They are supported in .NET Core.</exception>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public ExceptionBlockFilterBuilder CatchWhen<TExceptionType>(Action<IOpCodeEmitter> exceptionHandler) where TExceptionType : Exception
    {
        return CatchWhen(typeof(TExceptionType), exceptionHandler);
    }

    /// <summary>
    /// Adds a filter block (<see langword="when"/> in C#) to the exception block that starts with a type check. Filters can't co-exist with fault blocks.
    /// </summary>
    /// <remarks>Note that filter blocks aren't supported for <see cref="DynamicMethod"/>'s in some runtimes including .NET Framework. They are supported in .NET Core.</remarks>
    /// <exception cref="NotSupportedException">Filter blocks aren't supported in <see cref="DynamicMethod"/> in some runtimes. They are supported in .NET Core.</exception>
    /// <exception cref="ArgumentException">Type is not assignable to <see cref="Exception"/> (or an interface).</exception>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public ExceptionBlockFilterBuilder CatchWhen(Type baseExceptionType, Action<IOpCodeEmitter> exceptionHandler)
    {
        if (!baseExceptionType.IsInterface && !typeof(Exception).IsAssignableFrom(baseExceptionType))
            throw new ArgumentException($"Expected a type deriving from {Accessor.ExceptionFormatter.Format(typeof(Exception))}.", nameof(baseExceptionType));

        AssertNotClosed();

        IOpCodeEmitter emitter = _emitter;
        emitter.BeginExceptFilterBlock();
        Label lblIncorrectType = emitter.DefineLabel();
        Label lblCorrectType = emitter.DefineLabel();
        if (exceptionHandler == null)
        {
            emitter.Emit(OpCodes.Isinst, baseExceptionType);
            emitter.Emit(OpCodes.Brtrue, lblCorrectType);
            emitter.Emit(OpCodes.Ldc_I4_0);
            emitter.Emit(OpCodes.Br, lblIncorrectType);
            emitter.MarkLabel(lblCorrectType);
            emitter.Emit(OpCodes.Ldc_I4_1);
            emitter.MarkLabel(lblIncorrectType);
        }
        else
        {
            emitter.Emit(OpCodes.Isinst, baseExceptionType);
            emitter.Emit(OpCodes.Dup);
            emitter.Emit(OpCodes.Brtrue, lblCorrectType);
            emitter.Emit(OpCodes.Pop);
            emitter.Emit(OpCodes.Ldc_I4_0);
            emitter.Emit(OpCodes.Br, lblIncorrectType);

            emitter.MarkLabel(lblCorrectType);
            FilterBlockEmitterWrapper wrapper = new FilterBlockEmitterWrapper(emitter);
            exceptionHandler(wrapper);
            if (!wrapper.HasEmittedStackChanges)
            {
                emitter.Emit(OpCodes.Pop);
                emitter.Emit(OpCodes.Ldc_I4_0);
            }

            emitter.MarkLabel(lblIncorrectType);
            // null value on stack = 0
        }

        // endfilter is automatically appended
        return new ExceptionBlockFilterBuilder(this);
    }

    /// <summary>
    /// Close the exception block. It must have at least one handler attached.
    /// </summary>
    /// <exception cref="InvalidOperationException">Did not start a catch, finally, fault, or didn't start and end a filter block.</exception>
    /// <exception cref="ObjectDisposedException">Exception block is already closed.</exception>
    public IOpCodeEmitter End()
    {
        AssertNotClosed();

        if (!_hasHandler)
            throw new InvalidOperationException("A catch, finally, or fault block must be started before ending an exception block.");

        _emitter.EndExceptionBlock();
        _isClosed = true;
        return _emitter;
    }

    /// <summary>
    /// The sub-type of the emitter used in the factory for <see cref="CatchWhen(Action{IOpCodeEmitter})"/>.
    /// </summary>
    public interface IFilterBlockEmitter : IOpCodeEmitter;
    
    /// <summary>
    /// The sub-type of the emitter used in the factory for <see cref="EmitterExtensions.Try"/>.
    /// </summary>
    public interface ITryBlockEmitter : IOpCodeEmitter;

    /// <summary>
    /// The sub-type of the emitter used in the factory for <see cref="Catch(Type,Action{IOpCodeEmitter})"/> or <see cref="ExceptionBlockFilterBuilder.OnPass"/>.
    /// </summary>
    public interface ICatchBlockEmitter : IOpCodeEmitter;

    /// <summary>
    /// The sub-type of the emitter used in the factory for <see cref="Finally"/>.
    /// </summary>
    public interface IFinallyBlockEmitter : IOpCodeEmitter;

    /// <summary>
    /// The sub-type of the emitter used in the factory for <see cref="Fault"/>.
    /// </summary>
    public interface IFaultBlockEmitter : IOpCodeEmitter;
    private class FilterBlockEmitterWrapper(IOpCodeEmitter underlying) : EmitterWrapper(underlying, InvalidOpCodes), IFilterBlockEmitter
    {
        private static readonly HashSet<OpCode> InvalidOpCodes = 
        [
            OpCodes.Ret, OpCodes.Endfilter, OpCodes.Jmp, OpCodes.Endfinally,
            OpCodes.Tailcall, OpCodes.Localloc, OpCodes.Rethrow
        ];

        public override Label? BeginExceptionBlock() => throw new NotSupportedException("Exception blocks can not be started in a filter.");
        public override void BeginCatchBlock(Type? exceptionType) => throw new NotSupportedException("Catch blocks can not be started in a filter.");
        public override void BeginExceptFilterBlock() => throw new NotSupportedException("Filter blocks can not be started in a filter.");
        public override void BeginFaultBlock() => throw new NotSupportedException("Fault blocks can not be started in a filter.");
        public override void BeginFinallyBlock() => throw new NotSupportedException("Finally blocks can not be started in a filter.");
    }
    private class CatchBlockEmitterWrapper(IOpCodeEmitter underlying) : EmitterWrapper(underlying, InvalidOpCodes), ICatchBlockEmitter
    {
        private static readonly HashSet<OpCode> InvalidOpCodes =
        [
            OpCodes.Ret, OpCodes.Tailcall, OpCodes.Jmp, OpCodes.Endfinally, OpCodes.Endfilter, OpCodes.Localloc
        ];
    }
    private class FinallyBlockEmitterWrapper(IOpCodeEmitter underlying) : EmitterWrapper(underlying, InvalidOpCodes), IFinallyBlockEmitter
    {
        private static readonly HashSet<OpCode> InvalidOpCodes =
        [
            OpCodes.Ret, OpCodes.Tailcall, OpCodes.Jmp, OpCodes.Endfilter, OpCodes.Localloc
        ];
    }
    private class FaultBlockEmitterWrapper(IOpCodeEmitter underlying) : EmitterWrapper(underlying, InvalidOpCodes), IFaultBlockEmitter
    {
        private static readonly HashSet<OpCode> InvalidOpCodes =
        [
            OpCodes.Ret, OpCodes.Tailcall, OpCodes.Jmp, OpCodes.Endfilter, OpCodes.Localloc
        ];
    }
    private class TryBlockEmitterWrapper(IOpCodeEmitter underlying) : EmitterWrapper(underlying, InvalidOpCodes), IFilterBlockEmitter
    {
        private static readonly HashSet<OpCode> InvalidOpCodes =
        [
            OpCodes.Endfilter, OpCodes.Endfinally, OpCodes.Rethrow, OpCodes.Tailcall, OpCodes.Ret
        ];

        public override void BeginCatchBlock(Type? exceptionType) => throw new NotSupportedException("Catch blocks can not be started outside an exception block.");
        public override void BeginExceptFilterBlock() => throw new NotSupportedException("Filter blocks can not be started outside an exception block.");
        public override void BeginFaultBlock() => throw new NotSupportedException("Fault blocks can not be started outside an exception block.");
        public override void BeginFinallyBlock() => throw new NotSupportedException("Finally blocks can not be started outside an exception block.");
    }
}

/// <summary>
/// Limits the actions of <see cref="ExceptionBlockBuilder"/> to only handling the last filter block.
/// </summary>
public class ExceptionBlockFilterBuilder
{
    private readonly ExceptionBlockBuilder _builder;
    internal ExceptionBlockFilterBuilder(ExceptionBlockBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>
    /// Handles when the preceding filter passes.
    /// </summary>
    public ExceptionBlockBuilder OnPass(Action<IOpCodeEmitter> filterHandler)
    {
        _builder.ApplyFilterHandler(filterHandler);
        return _builder;
    }
}