using System.Diagnostics;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Provides extension methods for <see cref="Stopwatch"/>.
/// </summary>
public static class StopwatchExtensions
{
    /// <summary>
    /// Gets the amount of milliseconds elapsed from a <see cref="Stopwatch"/> as a decimal instead of an integer.
    /// </summary>
    public static double GetElapsedMilliseconds(this Stopwatch stopwatch) => stopwatch.ElapsedTicks / (double)Stopwatch.Frequency * 1000d;
}
