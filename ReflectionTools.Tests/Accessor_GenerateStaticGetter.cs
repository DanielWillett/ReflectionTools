using DanielWillett.ReflectionTools.Tests.SampleObjects;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GenerateStaticGetter
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

    /*
     * Private value type
     */

    [TestMethod]
    // field: private, non-readonly, value type
    // value: passed as-is (value type)
    public void TestPrivateValueTypeField()
    {
        const string fieldName = "PrivateValTypeField";
        const int value = 1;

        StaticGetter<int> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(getter);

        field.SetValue(null, value);

        Assert.AreEqual(value, getter());

        field.SetValue(null, 0);
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // value: passed as boxed value type
    public void TestPrivateBoxedValueTypeField()
    {
        const string fieldName = "PrivateValTypeField";
        const int value = 1;

        StaticGetter<object> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(getter);

        field.SetValue(null, value);

        Assert.AreEqual(value, (int)getter());

        field.SetValue(null, 0);
    }

    /*
     * Private reference type
     */

    [TestMethod]
    // field: private, non-readonly, value type
    // value: passed as-is (value type)
    public void TestPrivateReferenceTypeField()
    {
        const string fieldName = "PrivateRefTypeField";
        const string value = "test";

        StaticGetter<string> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(getter);

        field.SetValue(null, value);

        Assert.AreEqual(value, getter());

        field.SetValue(null, null);
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // value: passed as boxed value type
    public void TestPrivateObjectReferenceTypeField()
    {
        const string fieldName = "PrivateRefTypeField";
        const string value = "test";

        StaticGetter<object> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(getter);

        field.SetValue(null, value);

        Assert.AreEqual(value, (string)getter());

        field.SetValue(null, null);
    }

    /*
     * Public value type
     */

    [TestMethod]
    // field: public, non-readonly, value type
    // value: passed as-is (value type)
    public void TestPublicValueTypeField()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        StaticGetter<int> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleStaticMembers.PublicValTypeField = value;

        Assert.AreEqual(value, getter());

        SampleStaticMembers.PublicValTypeField = 0;
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // value: passed as boxed value type
    public void TestPublicBoxedValueTypeField()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        StaticGetter<object> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleStaticMembers.PublicValTypeField = value;

        Assert.AreEqual(value, (int)getter());

        SampleStaticMembers.PublicValTypeField = 0;
    }

    /*
     * Public reference type
     */

    [TestMethod]
    // field: public, non-readonly, value type
    // value: passed as-is (value type)
    public void TestPublicReferenceTypeField()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        StaticGetter<string> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, string>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleStaticMembers.PublicRefTypeField = value;

        Assert.AreEqual(value, getter());

        SampleStaticMembers.PublicRefTypeField = null;
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // value: passed as boxed value type
    public void TestPublicObjectReferenceTypeField()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        StaticGetter<object> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleStaticMembers.PublicRefTypeField = value;

        Assert.AreEqual(value, (string)getter());

        SampleStaticMembers.PublicRefTypeField = null;
    }

    /*
     * Private readonly value type
     */

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as-is (value type)
    public void TestReadonlyPrivateValueTypeField()
    {
        const string fieldName = "PrivateReadonlyValTypeField";
#if NET || NETCOREAPP
        const int value = 0;
#else
        const int value = 1;
#endif

        StaticGetter<int> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as boxed value type
    public void TestReadonlyPrivateBoxedValueTypeField()
    {
        const string fieldName = "PrivateReadonlyValTypeField";
#if NET || NETCOREAPP
        const int value = 0;
#else
        const int value = 1;
#endif

        StaticGetter<object> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, (int)getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    /*
     * Private readonly reference type
     */

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as-is (value type)
    public void TestReadonlyPrivateReferenceTypeField()
    {
        const string fieldName = "PrivateReadonlyRefTypeField";
#if NET || NETCOREAPP
        const string? value = null;
#else
        const string? value = "test";
#endif

        StaticGetter<string> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, string>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as boxed value type
    public void TestReadonlyPrivateObjectReferenceTypeField()
    {
        const string fieldName = "PrivateReadonlyRefTypeField";
#if NET || NETCOREAPP
        const string? value = null;
#else
        const string? value = "test";
#endif

        StaticGetter<object> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, (string)getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    /*
     * Public readonly value type
     */

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as-is (value type)
    public void TestReadonlyPublicValueTypeField()
    {
        const string fieldName = "PublicReadonlyValTypeField";
#if NET || NETCOREAPP
        const int value = 0;
#else
        const int value = 1;
#endif

        StaticGetter<int> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as boxed value type
    public void TestReadonlyPublicBoxedValueTypeField()
    {
        const string fieldName = "PublicReadonlyValTypeField";
#if NET || NETCOREAPP
        const int value = 0;
#else
        const int value = 1;
#endif

        StaticGetter<object> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, (int)getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    /*
     * Public readonly reference type
     */

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as-is (value type)
    public void TestReadonlyPublicReferenceTypeField()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
#if NET || NETCOREAPP
        const string? value = null;
#else
        const string? value = "test";
#endif

        StaticGetter<string> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, string>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as boxed value type
    public void TestReadonlyPublicObjectReferenceTypeField()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
#if NET || NETCOREAPP
        const string? value = null;
#else
        const string? value = "test";
#endif

        StaticGetter<object> getter = Accessor.GenerateStaticGetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, (string)getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }
    /*
     * Private value type
     */

    [TestMethod]
    // field: private, non-readonly, value type
    // value: passed as-is (value type)
    public void TestPrivateValueTypeField_IVariable()
    {
        const string fieldName = "PrivateValTypeField";
        const int value = 1;

        IStaticVariable<int>? variable = Variables.FindStatic<SampleStaticMembers, int>(fieldName);
        Assert.IsNotNull(variable);

        StaticGetter<int> getter = variable.GenerateGetter(throwOnError: true);
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(getter);

        field.SetValue(null, value);

        Assert.AreEqual(value, getter());

        field.SetValue(null, 0);
    }

    /*
     * Private reference type
     */

    [TestMethod]
    // field: private, non-readonly, value type
    // value: passed as-is (value type)
    public void TestPrivateReferenceTypeField_IVariable()
    {
        const string fieldName = "PrivateRefTypeField";
        const string value = "test";

        IStaticVariable<string>? variable = Variables.FindStatic<SampleStaticMembers, string>(fieldName);
        Assert.IsNotNull(variable);

        StaticGetter<string> getter = variable.GenerateGetter(throwOnError: true);
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(getter);

        field.SetValue(null, value);

        Assert.AreEqual(value, getter());

        field.SetValue(null, null);
    }

    /*
     * Public value type
     */

    [TestMethod]
    // field: public, non-readonly, value type
    // value: passed as-is (value type)
    public void TestPublicValueTypeField_IVariable()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        IStaticVariable<int>? variable = Variables.FindStatic<SampleStaticMembers, int>(fieldName);
        Assert.IsNotNull(variable);

        StaticGetter<int> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

        SampleStaticMembers.PublicValTypeField = value;

        Assert.AreEqual(value, getter());

        SampleStaticMembers.PublicValTypeField = 0;
    }

    /*
     * Public reference type
     */

    [TestMethod]
    // field: public, non-readonly, value type
    // value: passed as-is (value type)
    public void TestPublicReferenceTypeField_IVariable()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        IStaticVariable<string>? variable = Variables.FindStatic<SampleStaticMembers, string>(fieldName);
        Assert.IsNotNull(variable);

        StaticGetter<string> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

        SampleStaticMembers.PublicRefTypeField = value;

        Assert.AreEqual(value, getter());

        SampleStaticMembers.PublicRefTypeField = null;
    }

    /*
     * Private readonly value type
     */

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as-is (value type)
    public void TestReadonlyPrivateValueTypeField_IVariable()
    {
        const string fieldName = "PrivateReadonlyValTypeField";
#if NET || NETCOREAPP
        const int value = 0;
#else
        const int value = 1;
#endif

        IStaticVariable<int>? variable = Variables.FindStatic<SampleStaticMembers, int>(fieldName);
        Assert.IsNotNull(variable);

        StaticGetter<int> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    /*
     * Private readonly reference type
     */

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as-is (value type)
    public void TestReadonlyPrivateReferenceTypeField_IVariable()
    {
        const string fieldName = "PrivateReadonlyRefTypeField";
#if NET || NETCOREAPP
        const string? value = null;
#else
        const string? value = "test";
#endif

        IStaticVariable<string>? variable = Variables.FindStatic<SampleStaticMembers, string>(fieldName);
        Assert.IsNotNull(variable);

        StaticGetter<string> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    /*
     * Public readonly value type
     */

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as-is (value type)
    public void TestReadonlyPublicValueTypeField_IVariable()
    {
        const string fieldName = "PublicReadonlyValTypeField";
#if NET || NETCOREAPP
        const int value = 0;
#else
        const int value = 1;
#endif

        IStaticVariable<int>? variable = Variables.FindStatic<SampleStaticMembers, int>(fieldName);
        Assert.IsNotNull(variable);

        StaticGetter<int> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }

    /*
     * Public readonly reference type
     */

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as-is (value type)
    public void TestReadonlyPublicReferenceTypeField_IVariable()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
#if NET || NETCOREAPP
        const string? value = null;
#else
        const string? value = "test";
#endif

        IStaticVariable<string>? variable = Variables.FindStatic<SampleStaticMembers, string>(fieldName);
        Assert.IsNotNull(variable);

        StaticGetter<string> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

#if !NET && !NETCOREAPP
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)!;
        field.SetValue(null, value);
#endif

        Assert.AreEqual(value, getter());

#if !NET && !NETCOREAPP
        field.SetValue(null, null);
#endif
    }
}
