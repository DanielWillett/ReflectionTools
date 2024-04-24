using DanielWillett.ReflectionTools.Formatting;

namespace DanielWillett.ReflectionTools.Harmony.Formatting;
public interface ITranspileContextLogger
{
    bool Enabled { get; }
    void LogMethodFindError(TranspileContext context, MethodDefinition method);
    void LogFieldFindError(TranspileContext context);
}
