using DanielWillett.ReflectionTools.Tests.SampleObjects;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GenerateStaticCaller
{
    [TestMethod]
    public void TestBasicDelegateAction()
    {
        const string methodName = "TestMethod";

        using LogListener listener = new LogListener("Created basic delegate");

        Action caller = Accessor.GenerateStaticCaller<SampleStaticMembers, Action>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a basic delegate.");

        Assert.ThrowsException<NotImplementedException>(() => caller(), "Method did not run.");
    }
    [TestMethod]
    public void TestBasicDelegateActionPoppedReturnValue()
    {
        const string methodName = "TestMethodWithReturnValue";

        using LogListener listener = new LogListener("Created dynamic method");

        Action caller = Accessor.GenerateStaticCaller<SampleStaticMembers, Action>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        caller();
    }
    [TestMethod]
    public void TestBasicDelegateActionGeneratedReturnValue()
    {
        const string methodName = "TestEmptyMethod";

        using LogListener listener = new LogListener("Created dynamic method");

        Func<string?> caller = Accessor.GenerateStaticCaller<SampleStaticMembers, Func<string?>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        Assert.AreEqual(null, caller(), "Method return value was not created.");
    }
}
