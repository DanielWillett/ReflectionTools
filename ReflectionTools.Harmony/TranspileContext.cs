using System.Reflection;
using DanielWillett.ReflectionTools.Formatting;

namespace DanielWillett.ReflectionTools.Harmony;

/// <summary>
/// Used for logging in a transpiler, along with finding members.
/// </summary>
public class TranspileContext(MethodBase method, IReflectionToolsLogger? logger = null, IOpCodeFormatter? formatter = null)
{
    
}
