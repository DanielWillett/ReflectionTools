using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Trace and error logger for any reflection tools.
/// </summary>
public interface IReflectionToolsLogger
{
    /// <summary>
    /// Logs a verbose message meant for debugging.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    void LogDebug(string source, string message);

    /// <summary>
    /// Logs information that may be useful.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    void LogInfo(string source, string message);

    /// <summary>
    /// Logs warnings that could cause errors but may be okay.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    void LogWarning(string source, string message);

    /// <summary>
    /// Logs errors and/or <see cref="Exception"/>s.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    void LogError(string source, Exception? ex, string? message);
}

/// <summary>
/// Implement a <see cref="ILogger"/> through <see cref="IReflectionToolsLogger"/>.
/// </summary>
[Ignore]
public class ReflectionToolsLoggerProxy : IReflectionToolsLogger, IDisposable
{
    private Dictionary<string, ILogger>? _loggers;

    /// <summary>
    /// Factory to create loggers from.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Should <see cref="LoggerFactory"/> be disposed when this object gets disposed?
    /// </summary>
    public bool DisposeFactoryOnDispose { get; }

    /// <summary>
    /// Creates a proxy to implement <see cref="IReflectionToolsLogger"/> with <see cref="ILogger"/>.
    /// </summary>
    /// <param name="disposeFactoryOnDispose">Should <see cref="LoggerFactory"/> be disposed when this object gets disposed?</param>
    /// <param name="loggerFactory">Factory to create loggers from.</param>
    public ReflectionToolsLoggerProxy(ILoggerFactory loggerFactory, bool disposeFactoryOnDispose = false)
    {
        LoggerFactory = loggerFactory;
        DisposeFactoryOnDispose = disposeFactoryOnDispose;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogDebug(string source, string message)
    {
        ILogger logger = GetOrAddLogger(source);
        logger.LogDebug(message);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogInfo(string source, string message)
    {
        ILogger logger = GetOrAddLogger(source);
        logger.LogInformation(message);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogWarning(string source, string message)
    {
        ILogger logger = GetOrAddLogger(source);
        logger.LogWarning(message);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogError(string source, Exception? ex, string? message)
    {
        ILogger logger = GetOrAddLogger(source);
        logger.LogError(ex, message);
    }

    private ILogger GetOrAddLogger(string? source)
    {
        if (source == null)
            return LoggerFactory.CreateLogger("DanielWillett.ReflectionTools");

        lock (this)
        {
            _loggers ??= new Dictionary<string, ILogger>(4);
            if (!_loggers.TryGetValue(source, out ILogger logger))
                _loggers.Add(source, logger = LoggerFactory.CreateLogger("DanielWillett.ReflectionTools::" + source));

            return logger;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (DisposeFactoryOnDispose)
            LoggerFactory.Dispose();
    }
}

/// <summary>
/// Logs messages to the <see cref="Console"/>.
/// </summary>
public class ConsoleReflectionToolsLogger : IReflectionToolsLogger
{
    /// <summary>
    /// Color of debug messages in the console.
    /// </summary>
    /// <remarks>Default value: <see cref="ConsoleColor.DarkGray"/>.</remarks>
    public ConsoleColor DebugColor { get; set; } = ConsoleColor.DarkGray;

    /// <summary>
    /// Color of info messages in the console.
    /// </summary>
    /// <remarks>Default value: <see cref="ConsoleColor.Gray"/>.</remarks>
    public ConsoleColor InfoColor { get; set; } = ConsoleColor.Gray;

    /// <summary>
    /// Color of warning messages in the console.
    /// </summary>
    /// <remarks>Default value: <see cref="ConsoleColor.Yellow"/>.</remarks>
    public ConsoleColor WarningColor { get; set; } = ConsoleColor.Yellow;

    /// <summary>
    /// Color of error messages in the console.
    /// </summary>
    /// <remarks>Default value: <see cref="ConsoleColor.Red"/>.</remarks>
    public ConsoleColor ErrorColor { get; set; } = ConsoleColor.Red;

    /// <summary>
    /// Should stack traces be logged for errors. Stack traces are always logged for exceptions.
    /// </summary>
    /// <remarks>Default value: <see langword="true"/>.</remarks>
    public bool LogErrorStackTrace { get; set; } = true;

    /// <summary>
    /// Should stack traces be logged for warnings.
    /// </summary>
    /// <remarks>Default value: <see langword="false"/>.</remarks>
    public bool LogWarningStackTrace { get; set; } = false;

    /// <summary>
    /// Should stack traces be logged for info messages.
    /// </summary>
    /// <remarks>Default value: <see langword="false"/>.</remarks>
    public bool LogInfoStackTrace { get; set; } = false;

    /// <summary>
    /// Should stack traces be logged for debug messages.
    /// </summary>
    /// <remarks>Default value: <see langword="false"/>.</remarks>
    public bool LogDebugStackTrace { get; set; } = false;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogDebug(string source, string? message)
    {
        ConsoleColor color = Console.ForegroundColor;
        Console.ForegroundColor = DebugColor;

        if (!string.IsNullOrEmpty(message))
            Console.WriteLine("[DBG] [" + source + "] " + message + ".");
        else
            Console.WriteLine("[DBG] [" + source + "]");

        if (LogDebugStackTrace)
            Console.WriteLine(new StackTrace(1).ToString());

        Console.ForegroundColor = color;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogInfo(string source, string? message)
    {
        ConsoleColor color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Gray;

        if (!string.IsNullOrEmpty(message))
            Console.WriteLine("[INF] [" + source + "] " + message + ".");
        else
            Console.WriteLine("[INF] [" + source + "]");

        if (LogInfoStackTrace)
            Console.WriteLine(new StackTrace(1).ToString());

        Console.ForegroundColor = color;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogWarning(string source, string? message)
    {
        ConsoleColor color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;

        if (!string.IsNullOrEmpty(message))
            Console.WriteLine("[WRN] [" + source + "] " + message + ".");
        else
            Console.WriteLine("[WRN] [" + source + "]");

        if (LogWarningStackTrace)
            Console.WriteLine(new StackTrace(1).ToString());

        Console.ForegroundColor = color;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void LogError(string source, Exception? ex, string? message)
    {
        ConsoleColor color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        if (!string.IsNullOrEmpty(message))
            Console.WriteLine("[ERR] [" + source + "] " + message + ".");

        if (ex != null)
            Console.WriteLine(ex.ToString());
        
        if (ex == null && string.IsNullOrEmpty(message))
            Console.WriteLine("[ERR] [" + source + "]");
        else if (LogErrorStackTrace)
            Console.WriteLine(new StackTrace(1).ToString());

        Console.ForegroundColor = color;
    }
}