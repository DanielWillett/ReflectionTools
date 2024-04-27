using System;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Handles formatting transpiler logs and writing them to a <see cref="IReflectionToolsLogger"/>.
/// </summary>
public interface ITranspileContextLogger : ICloneable
{
    /// <summary>
    /// If logging should be enabled.
    /// </summary>
    bool Enabled { get; }

    /// <summary>
    /// Log failure to find a reflection member with a message.
    /// </summary>
    /// <param name="context">The transpiler that failed.</param>
    /// <param name="missingMember">A definition of the member that couldn't be found.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use. Defaults to <see cref="Accessor.Active"/>.</param>
    void LogFailure(TranspileContext context, IMemberDefinition missingMember, IAccessor? accessor = null);

    /// <summary>
    /// Log failure with a message.
    /// </summary>
    /// <param name="context">The transpiler that failed.</param>
    /// <param name="message">Human-readable message to accompany the transpile context in the final log.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use. Defaults to <see cref="Accessor.Active"/>.</param>
    void LogFailure(TranspileContext context, string message, IAccessor? accessor = null);

    /// <summary>
    /// Log debug information with a message.
    /// </summary>
    /// <param name="context">The transpiler.</param>
    /// <param name="message">Human-readable message describing the event.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use. Defaults to <see cref="Accessor.Active"/>.</param>
    void LogDebug(TranspileContext context, string message, IAccessor? accessor = null);

    /// <summary>
    /// Log information with a message.
    /// </summary>
    /// <param name="context">The transpiler.</param>
    /// <param name="message">Human-readable message describing the event.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use. Defaults to <see cref="Accessor.Active"/>.</param>
    void LogInfo(TranspileContext context, string message, IAccessor? accessor = null);

    /// <summary>
    /// Log a warning with a message.
    /// </summary>
    /// <param name="context">The transpiler.</param>
    /// <param name="message">Human-readable message describing the event.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use. Defaults to <see cref="Accessor.Active"/>.</param>
    void LogWarning(TranspileContext context, string message, IAccessor? accessor = null);

    /// <summary>
    /// Log an error with a message.
    /// </summary>
    /// <param name="context">The transpiler.</param>
    /// <param name="message">Human-readable message describing the event.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use. Defaults to <see cref="Accessor.Active"/>.</param>
    void LogError(TranspileContext context, string message, IAccessor? accessor = null);

    /// <summary>
    /// Log an error with an exception and message.
    /// </summary>
    /// <param name="context">The transpiler.</param>
    /// <param name="ex">Error detailing what went wrong during the event.</param>
    /// <param name="message">Human-readable message describing the event.</param>
    /// <param name="accessor">Instance of <see cref="IAccessor"/> to use. Defaults to <see cref="Accessor.Active"/>.</param>
    void LogError(TranspileContext context, Exception ex, string message, IAccessor? accessor = null);
}
