#if NET461_OR_GREATER || !NETFRAMEWORK
using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Tests.Harmony;

[TestClass]
public class TranspileContextTests
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
        HarmonyLog.Reset(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "harmony.log"));
    }
    public void TranspileMethod(string methodName)
    {
        HarmonyLib.Harmony h = new HarmonyLib.Harmony("ReflectionTools.Tests");
        MethodInfo? patch = typeof(TranspileContextTests).GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        MethodInfo? target = typeof(TranspileContextTests).GetMethod(methodName + "Target", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        Assert.IsNotNull(patch);
        Assert.IsNotNull(target);

        h.Patch(target, transpiler: patch);
    }

    [TestMethod]
    public void TestNotFoundMember()
    {
        using LogListener listener = new LogListener("Failed to find \"static string.Format(string format, params object[] args)\".");
        
        TranspileMethod("NotFoundMember");

        Assert.IsTrue(listener.Result, "Did not log the missing string.Format method.");
    }
    
    [TestMethod]
    public void TestWriteInstructions()
    {
        using LogListener listener = new LogListener("Patched arguments to LogInfo.");
        
        TranspileMethod("WriteInstructions");

        Assert.IsTrue(listener.Result, "Did not log the patch debug message.");
        listener.Reset("[INF] [test source] test message");

        WriteInstructionsTarget();
    }

    public static IEnumerable<CodeInstruction> NotFoundMember(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? toStringMethod = typeof(string).GetMethod("XYZ", BindingFlags.Public | BindingFlags.Static, null, [ typeof(string), typeof(object[]) ], null);
        if (toStringMethod == null)
        {
            return ctx.Fail(new MethodDefinition("Format")
                .DeclaredIn<string>(true)
                .WithParameter<string>("format")
                .WithParameter<object[]>("args", isParams: true)
            );
        }

        return ctx;
    }

    public void NotFoundMemberTarget()
    {
        Console.WriteLine("Test");
    }
    public static IEnumerable<CodeInstruction> WriteInstructions(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
    {
        TranspileContext ctx = new TranspileContext(method, generator, instructions);

        MethodInfo? logInfo = typeof(IReflectionToolsLogger).GetMethod("LogInfo", BindingFlags.Public | BindingFlags.Instance, null, [ typeof(string), typeof(string) ], null);
        if (logInfo == null)
        {
            return ctx.Fail(new MethodDefinition("LogInfo")
                .DeclaredIn<IReflectionToolsLogger>(false)
                .WithParameter<string>("source")
                .WithParameter<string>("message")
            );
        }

        MethodInfo? getLogger = typeof(Accessor).GetProperty("Logger", BindingFlags.Public | BindingFlags.Static)?.GetMethod;
        if (getLogger == null)
        {
            return ctx.Fail(new PropertyDefinition("Logger")
                .DeclaredIn(typeof(Accessor), true)
                .WithNoSetter()
            );
        }

        while (ctx.MoveNext())
        {
            if (PatchUtility.TryReplacePattern(ctx,
                emit =>
                {
                    emit.Try(emit =>
                    {
                        emit.Invoke(getLogger)
                            .LoadConstantString("test source")
                            .LoadConstantString("test message")
                            .Invoke(logInfo);
                    })
                    .Catch<Exception>(emit =>
                    {
                        emit.PopFromStack()
                            .EmitWriteLine("catch");
                    }).End();
                },
                new PatternMatch[]
                {
                    x => x.LoadsConstant("Test1{0}"),
                    x => x.LoadsConstant("Test2"),
                    null
                }
            ))
            {
                ctx.LogDebug("Patched arguments to LogInfo.");
            }
        }

        return ctx;
    }
    public void WriteInstructionsTarget()
    {
        if (1 == int.Parse("2"))
            return;
        if (int.Parse("1") == 1)
            goto lbl;
        try
        {
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        lbl:
        Console.WriteLine("Test1{0}", "Test2");
        try
        {
            try
            {
                Console.WriteLine("Test1{0}", "Test2");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Test1{0}", "Test2");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Test1{0}", "Test2");
            Console.WriteLine(ex);
        }
    }
}
#endif