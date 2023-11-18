using DanielWillett.ReflectionTools.Tests.SampleObjects;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_CallerParameters
{
    private delegate void TestStaticMethodWithRefVTParameter(int value, ref int refValue);
    [TestMethod]
    public void TestBasicDelegateStaticRefVTParameter()
    {
        const string methodName = "TestMethodWithRefVTParameter";
        const int value = 4;

        using LogListener listener = new LogListener("Created basic delegate");

        TestStaticMethodWithRefVTParameter caller = Accessor.GenerateStaticCaller<SampleStaticMembers, TestStaticMethodWithRefVTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a basic delegate.");

        int refValue = 0;
        caller(value, ref refValue);

        Assert.AreEqual(value, refValue);
    }
    private delegate void TestStaticMethodWithRefRTParameter(string value, ref string refValue);
    [TestMethod]
    public void TestBasicDelegateStaticRefRTParameter()
    {
        const string methodName = "TestMethodWithRefRTParameter";
        const string value = "test";

        using LogListener listener = new LogListener("Created basic delegate");

        TestStaticMethodWithRefRTParameter caller = Accessor.GenerateStaticCaller<SampleStaticMembers, TestStaticMethodWithRefRTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a basic delegate.");

        string refValue = "not test";
        caller(value, ref refValue);

        Assert.AreEqual(value, refValue);
    }
    private delegate void TestInstanceMethodWithRefVTParameter(SampleClass instance, int value, ref int refValue);
    [TestMethod]
    public void TestDynamicMethodInstanceRefVTParameter()
    {
        const string methodName = "TestMethodWithRefVTParameter";
        const int value = 4;

        using LogListener listener = new LogListener("Created dynamic method");

        TestInstanceMethodWithRefVTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestInstanceMethodWithRefVTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        int refValue = 0;

        caller(new SampleClass(), value, ref refValue);

        Assert.AreEqual(value, refValue);
    }
    private delegate void TestInstanceMethodWithRefRTParameter(SampleClass instance, string value, ref string refValue);
    [TestMethod]
    public void TestDynamicMethodInstanceRefRTParameter()
    {
        const string methodName = "TestMethodWithRefRTParameter";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        TestInstanceMethodWithRefRTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestInstanceMethodWithRefRTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        string refValue = "not test";

        caller(new SampleClass(), value, ref refValue);

        Assert.AreEqual(value, refValue);
    }
    private delegate void TestUnsafeInstanceMethodWithRefVTParameter(object instance, int value, ref int refValue);
    [TestMethod]
    public void TestUnsafeDelegateInstanceRefVTParameter()
    {
        const string methodName = "TestMethodWithRefVTParameter";
        const int value = 4;

        using LogListener listener = new LogListener("Created unsafely binded delegate");

        TestUnsafeInstanceMethodWithRefVTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestUnsafeInstanceMethodWithRefVTParameter>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

#if NET6_0_OR_GREATER
        Assert.IsFalse(listener.Result, "Method created with an unsafely binded delegate on .NET 6.0 or later.");
#else
        Assert.IsTrue(listener.Result, "Method was not created with a unsafely binded delegate.");
#endif

        int refValue = 0;

        caller(new SampleClass(), value, ref refValue);

        Assert.AreEqual(value, refValue);
    }
    private delegate void TestUnsafeInstanceMethodWithRefRTParameter(object instance, string value, ref string refValue);
    [TestMethod]
    public void TestUnsafeDelegateInstanceRefRTParameter()
    {
        const string methodName = "TestMethodWithRefRTParameter";

        const string value = "test";

        using LogListener listener = new LogListener("Created unsafely binded delegate");

        TestUnsafeInstanceMethodWithRefRTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestUnsafeInstanceMethodWithRefRTParameter>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);
#if NET6_0_OR_GREATER
        Assert.IsFalse(listener.Result, "Method created with an unsafely binded delegate on .NET 6.0 or later.");
#else
        Assert.IsTrue(listener.Result, "Method was not created with a unsafely binded delegate.");
#endif

        string refValue = "not test";

        caller(new SampleClass(), value, ref refValue);

        Assert.AreEqual(value, refValue);

    }
    private delegate void TestStaticMethodWithOutVTParameter(int value, out int outValue);
    [TestMethod]
    public void TestBasicDelegateStaticOutVTParameter()
    {
        const string methodName = "TestMethodWithOutVTParameter";
        const int value = 4;

        using LogListener listener = new LogListener("Created basic delegate");

        TestStaticMethodWithOutVTParameter caller = Accessor.GenerateStaticCaller<SampleStaticMembers, TestStaticMethodWithOutVTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a basic delegate.");

        caller(value, out int outValue);

        Assert.AreEqual(value, outValue);
    }
    private delegate void TestStaticMethodWithOutRTParameter(string value, out string outValue);
    [TestMethod]
    public void TestBasicDelegateStaticOutRTParameter()
    {
        const string methodName = "TestMethodWithOutRTParameter";
        const string value = "test";

        using LogListener listener = new LogListener("Created basic delegate");

        TestStaticMethodWithOutRTParameter caller = Accessor.GenerateStaticCaller<SampleStaticMembers, TestStaticMethodWithOutRTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a basic delegate.");

        caller(value, out string outValue);

        Assert.AreEqual(value, outValue);
    }
    private delegate void TestInstanceMethodWithOutVTParameter(SampleClass instance, int value, out int outValue);
    [TestMethod]
    public void TestDynamicMethodInstanceOutVTParameter()
    {
        const string methodName = "TestMethodWithOutVTParameter";
        const int value = 4;

        using LogListener listener = new LogListener("Created dynamic method");

        TestInstanceMethodWithOutVTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestInstanceMethodWithOutVTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        caller(new SampleClass(), value, out int outValue);

        Assert.AreEqual(value, outValue);
    }
    private delegate void TestInstanceMethodWithOutRTParameter(SampleClass instance, string value, out string outValue);
    [TestMethod]
    public void TestDynamicMethodInstanceOutRTParameter()
    {
        const string methodName = "TestMethodWithOutRTParameter";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        TestInstanceMethodWithOutRTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestInstanceMethodWithOutRTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        caller(new SampleClass(), value, out string outValue);

        Assert.AreEqual(value, outValue);
    }
    private delegate void TestUnsafeInstanceMethodWithOutVTParameter(object instance, int value, out int outValue);
    [TestMethod]
    public void TestUnsafeDelegateInstanceOutVTParameter()
    {
        const string methodName = "TestMethodWithOutVTParameter";
        const int value = 4;

        using LogListener listener = new LogListener("Created unsafely binded delegate");

        TestUnsafeInstanceMethodWithOutVTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestUnsafeInstanceMethodWithOutVTParameter>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

#if NET6_0_OR_GREATER
        Assert.IsFalse(listener.Result, "Method created with an unsafely binded delegate on .NET 6.0 or later.");
#else
        Assert.IsTrue(listener.Result, "Method was not created with a unsafely binded delegate.");
#endif

        caller(new SampleClass(), value, out int outValue);

        Assert.AreEqual(value, outValue);
    }
    private delegate void TestUnsafeInstanceMethodWithOutRTParameter(object instance, string value, out string outValue);
    [TestMethod]
    public void TestUnsafeDelegateInstanceOutRTParameter()
    {
        const string methodName = "TestMethodWithOutRTParameter";
        const string value = "test";

        using LogListener listener = new LogListener("Created unsafely binded delegate");

        TestUnsafeInstanceMethodWithOutRTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestUnsafeInstanceMethodWithOutRTParameter>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

#if NET6_0_OR_GREATER
        Assert.IsFalse(listener.Result, "Method created with an unsafely binded delegate on .NET 6.0 or later.");
#else
        Assert.IsTrue(listener.Result, "Method was not created with a unsafely binded delegate.");
#endif

        caller(new SampleClass(), value, out string outValue);

        Assert.AreEqual(value, outValue);
    }
    private delegate void TestStaticMethodWithInVTParameter(int value, in int inValue);
    [TestMethod]
    public void TestBasicDelegateStaticInVTParameter()
    {
        const string methodName = "TestMethodWithInVTParameter";
        const int value = 4;

        using LogListener listener = new LogListener("Created basic delegate");

        TestStaticMethodWithInVTParameter caller = Accessor.GenerateStaticCaller<SampleStaticMembers, TestStaticMethodWithInVTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a basic delegate.");

        int inValue = value;
        
        caller(value, in inValue);

        Assert.AreEqual(value, inValue);
    }
    private delegate void TestStaticMethodWithInRTParameter(string value, in string inValue);
    [TestMethod]
    public void TestBasicDelegateStaticInRTParameter()
    {
        const string methodName = "TestMethodWithInRTParameter";
        const string value = "test";

        using LogListener listener = new LogListener("Created basic delegate");

        TestStaticMethodWithInRTParameter caller = Accessor.GenerateStaticCaller<SampleStaticMembers, TestStaticMethodWithInRTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a basic delegate.");

        string inValue = value;

        caller(value, in inValue);

        Assert.AreEqual(value, inValue);
    }
    private delegate void TestInstanceMethodWithInVTParameter(SampleClass instance, int value, in int inValue);
    [TestMethod]
    public void TestDynamicMethodInstanceInVTParameter()
    {
        const string methodName = "TestMethodWithInVTParameter";
        const int value = 4;

        using LogListener listener = new LogListener("Created dynamic method");

        TestInstanceMethodWithInVTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestInstanceMethodWithInVTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        int inValue = value;

        caller(new SampleClass(), value, in inValue);

        Assert.AreEqual(value, inValue);
    }
    private delegate void TestInstanceMethodWithInRTParameter(SampleClass instance, string value, in string inValue);
    [TestMethod]
    public void TestDynamicMethodInstanceInRTParameter()
    {
        const string methodName = "TestMethodWithInRTParameter";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        TestInstanceMethodWithInRTParameter caller = Accessor.GenerateInstanceCaller<SampleClass, TestInstanceMethodWithInRTParameter>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        string inValue = value;

        caller(new SampleClass(), value, in inValue);

        Assert.AreEqual(value, inValue);
    }
}
