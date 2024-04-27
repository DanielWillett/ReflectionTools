using System;

namespace DanielWillett.ReflectionTools.Formatting;
internal class DefaultTranspileContextLogger : ITranspileContextLogger
{
    /// <summary>
    /// If this <see cref="DefaultTranspileContextLogger"/> is enabled or not.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void LogFailure(TranspileContext context, IMemberDefinition missingMember, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        IReflectionToolsLogger? logger = accessor.Logger;
        if (!accessor.LogErrorMessages || logger == null)
            return;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        const int additionalLength = 21;
        int len = missingMember.GetFormatLength(accessor.Formatter);
        int contextLen = accessor.Formatter.GetFormatLength(context.Method);
        StringDataMissingMemberState state = default;
        state.Context = context;
        state.MissingMember = missingMember;
        state.Accessor = accessor;
        string str = string.Create(contextLen + additionalLength + len, state, static (span, state) =>
        {
            int pos = 0;
            pos += state.Accessor.Formatter.Format(state.Context.Method, span);
            WriteFailedToFind(ref pos, span);
            pos += state.MissingMember.Format(state.Accessor.Formatter, span[pos..]);
            span[pos] = '"';
            span[pos + 1] = '.';
        });
#else
        string missingMemberFmt = missingMember.Format(accessor.Formatter);
        string methodFmt = accessor.Formatter.Format(context.Method);
        string str = $"{methodFmt} - Failed to find \"{missingMemberFmt}\".";
#endif

        logger.LogError("Transpiler", null, str);
    }

    /// <inheritdoc />
    public void LogFailure(TranspileContext context, string message, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        IReflectionToolsLogger? logger = accessor.Logger;
        if (!accessor.LogErrorMessages || logger == null)
            return;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        const int additionalLength = 27;
        int contextLen = accessor.Formatter.GetFormatLength(context.Method);
        StringDataState state = default;
        state.Context = context;
        state.Message = message;
        state.Accessor = accessor;
        string str = string.Create(contextLen + additionalLength + message.Length, state, static (span, state) =>
        {
            int pos = 0;
            pos += state.Accessor.Formatter.Format(state.Context.Method, span);
            WriteFailedToTranspile(ref pos, span);

            ReadOnlySpan<char> message = state.Message;
            if (message.Length > 1 && message[^1] == '.')
                message = message[..^1];

            message.CopyTo(span[pos..]);
            pos += message.Length;
            span[pos] = '"';
            span[pos + 1] = '.';
        });
#else
        string methodFmt = accessor.Formatter.Format(context.Method);
        if (message.Length > 1 && message[message.Length - 1] == '.')
            message = message.Substring(0, message.Length - 1);
        string str = $"{methodFmt} - Failed to transpile: \"{message}\".";
#endif

        logger.LogError("Transpiler", null, str);
    }

    /// <inheritdoc />
    public void LogDebug(TranspileContext context, string message, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        IReflectionToolsLogger? logger = accessor.Logger;
        if (!accessor.LogDebugMessages || logger == null)
            return;

        string? log = MakeLog(context, message, accessor, logger);
        if (log != null)
            logger.LogDebug("Transpiler", log);
    }

    /// <inheritdoc />
    public void LogInfo(TranspileContext context, string message, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        IReflectionToolsLogger? logger = accessor.Logger;
        if (!accessor.LogInfoMessages || logger == null)
            return;

        string? log = MakeLog(context, message, accessor, logger);
        if (log != null)
            logger.LogInfo("Transpiler", log);
    }

    /// <inheritdoc />
    public void LogWarning(TranspileContext context, string message, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        IReflectionToolsLogger? logger = accessor.Logger;
        if (!accessor.LogWarningMessages || logger == null)
            return;

        string? log = MakeLog(context, message, accessor, logger);
        if (log != null)
            logger.LogWarning("Transpiler", log);
    }

    /// <inheritdoc />
    public void LogError(TranspileContext context, string message, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        IReflectionToolsLogger? logger = accessor.Logger;
        if (!accessor.LogErrorMessages || logger == null)
            return;

        string? log = MakeLog(context, message, accessor, logger);
        if (log != null)
            logger.LogError("Transpiler", null, log);
    }

    /// <inheritdoc />
    public void LogError(TranspileContext context, Exception ex, string message, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        IReflectionToolsLogger? logger = accessor.Logger;
        if (!accessor.LogErrorMessages || logger == null)
            return;

        string? log = MakeLog(context, message, accessor, logger);
        if (log != null)
            logger.LogError("Transpiler", ex, log);
    }

    private static string MakeLog(TranspileContext context, string message, IAccessor accessor, IReflectionToolsLogger logger)
    {
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        int additionalLength = 3;
        StringDataState state = default;
        state.HasPeriod = message.EndsWith('.');
        if (!state.HasPeriod)
            ++additionalLength;
        int contextLen = accessor.Formatter.GetFormatLength(context.Method);
        state.Context = context;
        state.Message = message;
        state.Accessor = accessor;
        string str = string.Create(contextLen + additionalLength + message.Length, state, static (span, state) =>
        {
            int pos = 0;
            pos += state.Accessor.Formatter.Format(state.Context.Method, span);
            WriteOtherMessages(ref pos, span);
            state.Message.AsSpan().CopyTo(span[pos..]);
            pos += state.Message.Length;
            if (!state.HasPeriod)
                span[pos] = '.';
        });
#else
        string methodFmt = accessor.Formatter.Format(context.Method);
        string str = $"{methodFmt} - {message}";
        if (message.Length > 0 && message[message.Length - 1] != '.')
            str += ".";
#endif
        return str;
    }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private const string FailedToFind = " - Failed to find \"";
    private const string FailedToTranspile = " - Failed to transpile: ";
    private const string OtherMessages = " - ";
    private static void WriteFailedToFind(ref int pos, Span<char> span)
    {
        FailedToFind.AsSpan().CopyTo(span[pos..]);
        pos += FailedToFind.Length;
    }
    private static void WriteFailedToTranspile(ref int pos, Span<char> span)
    {
        FailedToTranspile.AsSpan().CopyTo(span[pos..]);
        pos += FailedToTranspile.Length;
    }
    private static void WriteOtherMessages(ref int pos, Span<char> span)
    {
        OtherMessages.AsSpan().CopyTo(span[pos..]);
        pos += OtherMessages.Length;
    }
#endif
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private struct StringDataMissingMemberState
    {
        public TranspileContext Context;
        public IMemberDefinition MissingMember;
        public IAccessor Accessor;
    }
    private struct StringDataState
    {
        public TranspileContext Context;
        public string Message;
        public bool HasPeriod;
        public IAccessor Accessor;
    }
#endif
    public object Clone()
    {
        return new DefaultTranspileContextLogger
        {
            Enabled = Enabled
        };
    }
}
