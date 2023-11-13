using Microsoft.Extensions.Logging;

namespace DanielWillett.ReflectionTools.IoC;

/// <summary>
/// Provides extension methods for <see cref="ILoggerFactory"/>.
/// </summary>
[Ignore]
public static class LoggerFactoryExtensions
{
    /// <summary>
    /// Creates a logger proxy for <see cref="Accessor.Logger"/>
    /// </summary>
    /// <param name="disposeFactoryOnDispose">Should <paramref name="loggerFactory"/> be disposed when this object gets disposed?</param>
    /// <param name="loggerFactory">Factory to create loggers from.</param>
    public static IReflectionToolsLogger CreateReflectionToolsLogger(this ILoggerFactory loggerFactory, bool disposeFactoryOnDispose = false)
    {
        return new ReflectionToolsLoggerProxy(loggerFactory, disposeFactoryOnDispose);
    }
}