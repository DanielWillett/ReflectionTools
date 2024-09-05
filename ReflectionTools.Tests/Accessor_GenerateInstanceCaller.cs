using DanielWillett.ReflectionTools.Tests.SampleObjects;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GenerateInstanceCaller
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

    [TestMethod]
    public void TestRTBasicDelegateAction()
    {
        const string methodName = "NotImplementedNoParams";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        Assert.ThrowsException<NotImplementedException>(() => caller(sampleClass), "Method did not run.");
    }
    [TestMethod]
    public void TestRTBasicDelegateActionUnsafeBinding()
    {
        const string methodName = "NotImplementedNoParams";

        using LogListener listener = new LogListener("Created basic delegate");

        Action<SampleClass> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass>>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a basic delegate.");

        SampleClass sampleClass = new SampleClass();

        Assert.ThrowsException<NotImplementedException>(() => caller(sampleClass), "Method did not run.");
    }
    [TestMethod]
    public void TestRTBasicDelegateActionThrowsNRE()
    {
        const string methodName = "NotImplementedNoParams";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        Assert.ThrowsException<NotImplementedException>(() =>
        {
            caller(sampleClass);
        }, "Method did not run.");

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            caller(null!);
        });
    }
    [TestMethod]
    public void TestRTBasicDelegateRTArgThrowsNRE()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass, string> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, string>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicRefTypeField);

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            caller(null!, value);
        });
    }
    [TestMethod]
    public void TestRTBasicDelegateActionRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass, string> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, string>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicRefTypeField);
    }
    [TestMethod]
    public void TestRTBasicDelegateActionVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass, int> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, int>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);
    }
    [TestMethod]
    public void TestRTUnsafeTypeBindingDelegateActionObjRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created unsafely binded delegate");

        Action<SampleClass, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, object>>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

#if !NET6_0_OR_GREATER
        Assert.IsTrue(listener.Result, "Method was not created with an unsafely binded delegate.");
#else
        Assert.IsFalse(listener.Result, "Method created with an unsafely binded delegate on .NET 6.0 or later.");
#endif

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicRefTypeField);
    }
    [TestMethod]
    public void TestRTNoUnsafeTypeBindingDelegateActionBoxedVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, object>>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);
    }
    [TestMethod]
    public void TestRTDynamicDelegateActionObjRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, object>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicRefTypeField);
    }
    [TestMethod]
    public void TestRTDynamicDelegateActionObjRTArgThrowsExceptions()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass, object?> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, object?>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicRefTypeField);

        caller(sampleClass, null);

        Assert.AreEqual(null, sampleClass.PublicRefTypeField);

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            caller(sampleClass, 3.0f);
        });
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            caller(sampleClass, new SampleClass());
        });
    }
    [TestMethod]
    public void TestRTDynamicDelegateActionBoxedVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, object>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);
    }
    [TestMethod]
    public void TestRTDynamicDelegateActionBoxedVTArgThrowsExceptions()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleClass, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<SampleClass, object>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            caller(sampleClass, null!);
        });
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            caller(sampleClass, 3.0f);
        });
        Assert.ThrowsException<InvalidCastException>(() =>
        {
            caller(sampleClass, new SampleClass());
        });
    }
    [TestMethod]
    public void TestObjectRTBasicDelegateAction()
    {
        const string methodName = "NotImplementedNoParams";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<object>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        Assert.ThrowsException<NotImplementedException>(() => caller(sampleClass), "Method did not run.");
    }
    [TestMethod]
    public void TestObjectRTBasicDelegateActionRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<object, string> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<object, string>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicRefTypeField);
    }
    [TestMethod]
    public void TestObjectRTBasicDelegateActionVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        Action<object, int> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<object, int>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);
    }
    [TestMethod]
    public void TestObjectRTUnsafeTypeBindingDelegateActionObjRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created unsafely binded delegate");

        Action<object, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<object, object>>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

#if !NET6_0_OR_GREATER
        Assert.IsTrue(listener.Result, "Method was not created with an unsafely binded delegate.");
#else
        Assert.IsFalse(listener.Result, "Method created with an unsafely binded delegate on .NET 6.0 or later.");
#endif

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicRefTypeField);
    }
    [TestMethod]
    public void TestObjectRTNoUnsafeTypeBindingDelegateActionBoxedVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        Action<object, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<object, object>>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);
    }
    [TestMethod]
    public void TestObjectRTDynamicDelegateActionObjRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<object, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<object, object>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicRefTypeField);
    }
    [TestMethod]
    public void TestObjectRTDynamicDelegateActionBoxedVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        Action<object, object> caller = Accessor.GenerateInstanceCaller<SampleClass, Action<object, object>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleClass sampleClass = new SampleClass();

        caller(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);
    }
    [TestMethod]
    public void TestVTBasicDelegateAction()
    {
        const string methodName = "NotImplementedNoParams";

        using LogListener listener = new LogListener("Created dynamic method");

        Action<SampleStruct> caller = Accessor.GenerateInstanceCaller<SampleStruct, Action<SampleStruct>>(methodName, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        SampleStruct sampleClass = new SampleStruct();

        Assert.ThrowsException<NotImplementedException>(() => caller(sampleClass), "Method did not run.");
    }
    [TestMethod]
    public void TestVTBasicDelegateActionRTArg()
    {
        const string methodName = "SetRefTypeField";
        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceCaller<SampleStruct, Action<SampleStruct, string>>(methodName, throwOnError: true)!;
        }, "Threw correct exception for non-readonly method as a value type.");
    }
    [TestMethod]
    public void TestVTBasicDelegateActionVTArg()
    {
        const string methodName = "SetValTypeField";
        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceCaller<SampleStruct, Action<SampleStruct, int>>(methodName, throwOnError: true)!;
        }, "Threw correct exception for non-readonly method as a value type.");
    }
    [TestMethod]
    public void TestVTUnsafeTypeBindingDelegateActionObjRTArg()
    {
        const string methodName = "SetRefTypeField";
        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceCaller<SampleStruct, Action<SampleStruct, object>>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;
        }, "Threw correct exception for non-readonly method as a value type.");
    }
    [TestMethod]
    public void TestVTNoUnsafeTypeBindingDelegateActionBoxedVTArg()
    {
        const string methodName = "SetValTypeField";
        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceCaller<SampleStruct, Action<SampleStruct, object>>(methodName, allowUnsafeTypeBinding: true, throwOnError: true)!;
        }, "Threw correct exception for non-readonly method as a value type.");
    }
    [TestMethod]
    public void TestVTDynamicDelegateActionObjRTArg()
    {
        const string methodName = "SetRefTypeField";
        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceCaller<SampleStruct, Action<SampleStruct, object>>(methodName, throwOnError: true)!;
        }, "Threw correct exception for non-readonly method as a value type.");
    }
    [TestMethod]
    public void TestVTDynamicDelegateActionBoxedVTArg()
    {
        const string methodName = "SetValTypeField";
        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceCaller<SampleStruct, Action<SampleStruct, object>>(methodName, throwOnError: true)!;
        }, "Threw correct exception for non-readonly method as a value type.");
    }
    [TestMethod]
    public void TestBoxedVTBasicDelegateAction()
    {
        const string methodName = "NotImplementedNoParams";

        using LogListener listener = new LogListener("Created dynamic method");

        MethodInfo method = typeof(SampleStruct).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Action<object> caller = Accessor.GenerateInstanceCaller<Action<object>>(method, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        object sampleClass = new SampleStruct();

        Assert.ThrowsException<NotImplementedException>(() => caller(sampleClass), "Method did not run.");
    }
    [TestMethod]
    public void TestBoxedVTBasicDelegateActionRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        MethodInfo method = typeof(SampleStruct).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Action<object, string> caller = Accessor.GenerateInstanceCaller<Action<object, string>>(method, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        object sampleClass = new SampleStruct();

        caller(sampleClass, value);

        Assert.AreEqual(value, ((SampleStruct)sampleClass).PublicRefTypeField);
    }
    [TestMethod]
    public void TestBoxedVTBasicDelegateActionVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        MethodInfo method = typeof(SampleStruct).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Action<object, int> caller = Accessor.GenerateInstanceCaller<Action<object, int>>(method, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        object sampleClass = new SampleStruct();

        caller(sampleClass, value);

        Assert.AreEqual(value, ((SampleStruct)sampleClass).PublicValTypeField);
    }
    [TestMethod]
    public void TestBoxedVTUnsafeTypeBindingDelegateActionObjRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        MethodInfo method = typeof(SampleStruct).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Action<object, object> caller = Accessor.GenerateInstanceCaller<Action<object, object>>(method, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);
        
        Assert.IsTrue(listener.Result, "Method was not created with an dynamic method.");

        object sampleClass = new SampleStruct();

        caller(sampleClass, value);

        Assert.AreEqual(value, ((SampleStruct)sampleClass).PublicRefTypeField);
    }
    [TestMethod]
    public void TestBoxedVTNoUnsafeTypeBindingDelegateActionBoxedVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        MethodInfo method = typeof(SampleStruct).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Action<object, object> caller = Accessor.GenerateInstanceCaller<Action<object, object>>(method, allowUnsafeTypeBinding: true, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        object sampleClass = new SampleStruct();

        caller(sampleClass, value);

        Assert.AreEqual(value, ((SampleStruct)sampleClass).PublicValTypeField);
    }
    [TestMethod]
    public void TestBoxedVTDynamicDelegateActionObjRTArg()
    {
        const string methodName = "SetRefTypeField";
        const string value = "test";

        using LogListener listener = new LogListener("Created dynamic method");

        MethodInfo method = typeof(SampleStruct).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Action<object, object> caller = Accessor.GenerateInstanceCaller<Action<object, object>>(method, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        object sampleClass = new SampleStruct();

        caller(sampleClass, value);

        Assert.AreEqual(value, ((SampleStruct)sampleClass).PublicRefTypeField);
    }
    [TestMethod]
    public void TestBoxedVTDynamicDelegateActionBoxedVTArg()
    {
        const string methodName = "SetValTypeField";
        const int value = 3;

        using LogListener listener = new LogListener("Created dynamic method");

        MethodInfo method = typeof(SampleStruct).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        Action<object, object> caller = Accessor.GenerateInstanceCaller<Action<object, object>>(method, throwOnError: true)!;

        Assert.IsNotNull(caller);

        Assert.IsTrue(listener.Result, "Method was not created with a dynamic method.");

        object sampleClass = new SampleStruct();

        caller(sampleClass, value);

        Assert.AreEqual(value, ((SampleStruct)sampleClass).PublicValTypeField);
    }
}
