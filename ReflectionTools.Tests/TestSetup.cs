[assembly: DoNotParallelize]


namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
public class TestSetup
{
    public static event Action<string>? OnLog;
    [AssemblyInitialize]
    public static void Initialize(TestContext testContext)
    {
        Accessor.LogILTraceMessages = true;
        Accessor.LogDebugMessages = true;
        Accessor.LogInfoMessages = true;
        Accessor.LogWarningMessages = true;
        Accessor.LogErrorMessages = true;

        Accessor.Logger = new Logger(testContext);
    }
    private class Logger : IReflectionToolsLogger
    {
        private readonly TestContext _ctx;
        public Logger(TestContext ctx)
        {
            _ctx = ctx;
        }
        public void LogDebug(string source, string message)
        {
            OnLog?.Invoke(message);
            _ctx.WriteLine("[DBG] [" + source + "] " + message);
        }
        public void LogInfo(string source, string message)
        {
            OnLog?.Invoke(message);
            _ctx.WriteLine("[INF] [" + source + "] " + message);
        }
        public void LogWarning(string source, string message)
        {
            OnLog?.Invoke(message);
            _ctx.WriteLine("[WRN] [" + source + "] " + message);
        }
        public void LogError(string source, Exception? ex, string? message)
        {
            if (message != null)
            {
                OnLog?.Invoke(message);
                _ctx.WriteLine("[ERR] [" + source + "] " + message);
            }
            if (ex != null)
                _ctx.WriteLine("[ERR] [" + source + "]" + Environment.NewLine + ex);
        }
    }
}

public class LogListener : IDisposable
{
    public string Text { get; set; }
    public bool Result { get; private set; }
    public LogListener(string text)
    {
        Text = text;
        TestSetup.OnLog += OnLog;
    }

    public void Reset(string? text = null)
    {
        if (text != null)
            Text = text;
        Result = false;
        TestSetup.OnLog += OnLog;
    }
    private void OnLog(string message)
    {
        if (message.IndexOf(Text, StringComparison.InvariantCultureIgnoreCase) != -1)
        {
            Result = true;
            TestSetup.OnLog -= OnLog;
        }
    }

    public void Dispose()
    {
        if (!Result)
            TestSetup.OnLog -= OnLog;
    }
}