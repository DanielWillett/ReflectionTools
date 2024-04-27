using HarmonyLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Path = System.IO.Path;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Manages <see cref="FileLog"/>, with a <see cref="Reset"/> method to clear the file that can be called on startup, along with setting a specific log path.
/// </summary>
public static class HarmonyLog
{
    private static readonly object Sync = new object();

    /// <summary>
    /// If the log has already been started, clear the file, then update the log location for harmony to the given file.
    /// </summary>
    /// <remarks>Will only compile if <c>REFLECTION_TOOLS_ENABLE_HARMONY_LOG</c> is defined, otherwise does the same thing as <see cref="Reset"/>.</remarks>
    /// <param name="logPath">The path to create the log file at. If this is <see langword="null"/>, an attempt will be made to get it from the environment variable.</param>
    /// <param name="enableDebug">Should harmony debug logging be enabled (<see cref="Harmony.DEBUG"/>).</param>
    /// <exception cref="InvalidOperationException">Log path is not set, neither is the environment variable 'HARMONY_LOG_FILE'.</exception>
    [Conditional("REFLECTION_TOOLS_ENABLE_HARMONY_LOG")]
    public static void ResetConditional(string? logPath, bool enableDebug = true)
    {
        Reset(logPath, enableDebug);
    }

    /// <summary>
    /// If the log has already been started, clear the file, then update the log location for harmony to the given file.
    /// </summary>
    /// <param name="logPath">The path to create the log file at. If this is <see langword="null"/>, an attempt will be made to get it from the environment variable.</param>
    /// <param name="enableDebug">Should harmony debug logging be enabled (<see cref="Harmony.DEBUG"/>).</param>
    /// <exception cref="InvalidOperationException">Log path is not set, neither is the environment variable 'HARMONY_LOG_FILE'.</exception>
    public static void Reset(string? logPath, bool enableDebug = true)
    {
        logPath ??= Environment.GetEnvironmentVariable("HARMONY_LOG_FILE", EnvironmentVariableTarget.Process);

        if (logPath == null)
            throw new InvalidOperationException("Log path is not set, neither is the environment variable 'HARMONY_LOG_FILE'.");

        string? dir = Path.GetDirectoryName(logPath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        // lock private sync lock for harmony's file log before clearing file.
        FieldInfo? harmonyLockObjectField = typeof(FileLog).GetField("fileLock", BindingFlags.NonPublic | BindingFlags.Static);
        object? harmonyLockObject = harmonyLockObjectField?.GetValue(null);
        if (harmonyLockObject != null)
            Monitor.Enter(harmonyLockObject);

        try
        {
            lock (Sync)
            {
                try
                {
                    using FileStream str = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    byte[] bytes = Encoding.UTF8.GetBytes(DateTimeOffset.UtcNow.ToString("R") + Environment.NewLine);
                    str.Write(bytes, 0, bytes.Length);
                    str.Flush();
                }
                catch (Exception ex)
                {
                    IReflectionToolsLogger? reflectionToolsLogger = Accessor.Logger;
                    if (reflectionToolsLogger != null && Accessor.LogErrorMessages)
                    {
                        reflectionToolsLogger.LogError(nameof(HarmonyLog), ex, $"Unable to clear previous harmony log: {logPath}");
                    }
                }

                Environment.SetEnvironmentVariable("HARMONY_LOG_FILE", logPath, EnvironmentVariableTarget.Process);

                // set private flag to recheck the environment variable
                FieldInfo? logPathField = typeof(FileLog).GetField("_logPathInited", BindingFlags.NonPublic | BindingFlags.Static);
                if (logPathField != null && logPathField.FieldType == typeof(bool))
                    logPathField.SetValue(null, false);

                Environment.SetEnvironmentVariable("HARMONY_DEBUG", enableDebug ? "1" : "0", EnvironmentVariableTarget.Process);
                Harmony.DEBUG = enableDebug;
            }
        }
        finally
        {
            if (harmonyLockObject != null)
                Monitor.Exit(harmonyLockObject);
        }
    }
}
