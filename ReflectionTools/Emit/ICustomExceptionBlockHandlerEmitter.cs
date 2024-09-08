namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Represents an <see cref="IOpCodeEmitter"/> that has custom handling for an exception block.
/// </summary>
public interface ICustomExceptionBlockHandlerEmitter : IOpCodeEmitter
{
    /// <summary>
    /// Allows implementations of <see cref="IOpCodeEmitter"/> to create their own types of exception block handlers.
    /// </summary>
    /// <param name="wrapperEmitter">The actual emitter being used. For example this may be a <see cref="DebuggableEmitter"/> even though this type isn't. <paramref name="wrapperEmitter"/> should be used over '<see langword="this"/>'.</param>
    /// <returns></returns>
    IExceptionBlockBuilder CreateExceptionBlockBuilder(IOpCodeEmitter wrapperEmitter);
}