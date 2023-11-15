using DanielWillett.ReflectionTools.Tests.SampleObjects;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GenerateStaticGetter
{
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
    }
}
