using DanielWillett.ReflectionTools.Tests.SampleObjects;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GenerateStaticSetter
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

        StaticSetter<int> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

        field.SetValue(null, 0);
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // value: passed as boxed value type
    public void TestPrivateBoxedValueTypeField()
    {
        const string fieldName = "PrivateValTypeField";
        const int value = 1;

        StaticSetter<object> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

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

        StaticSetter<string> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);
        
        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

        field.SetValue(null, null);
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // value: passed as boxed value type
    public void TestPrivateObjectReferenceTypeField()
    {
        const string fieldName = "PrivateRefTypeField";
        const string value = "test";

        StaticSetter<object> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

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

        StaticSetter<int> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicValTypeField);

        SampleStaticMembers.PublicValTypeField = 0;
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // value: passed as boxed value type
    public void TestPublicBoxedValueTypeField()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        StaticSetter<object> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicValTypeField);

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

        StaticSetter<string> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, string>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicRefTypeField);

        SampleStaticMembers.PublicRefTypeField = null;
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // value: passed as boxed value type
    public void TestPublicObjectReferenceTypeField()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        StaticSetter<object> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicRefTypeField);

        SampleStaticMembers.PublicRefTypeField = null;
    }

    /*
     * Private readonly value type
     */

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as-is (value type)
    public void TestPrivateReadonlyValueTypeField()
    {
        const string fieldName = "PrivateReadonlyValTypeField";
#if !NET && !NETCOREAPP
        const int value = 1;

        StaticSetter<int> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

        field.SetValue(null, 0);
#else
        Assert.ThrowsException<NotSupportedException>(() =>
        {
            _ = Accessor.GenerateStaticSetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;
        }, "Didn't throw NotSupportedException for readonly static value type field.");
#endif
    }

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as boxed value type
    public void TestPrivateReadonlyBoxedValueTypeField()
    {
        const string fieldName = "PrivateReadonlyValTypeField";
#if !NET && !NETCOREAPP
        const int value = 1;

        StaticSetter<object> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

        field.SetValue(null, 0);
#else
        Assert.ThrowsException<NotSupportedException>(() =>
        {
            _ = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        }, "Didn't throw NotSupportedException for readonly static value type field.");
#endif
    }

    /*
     * Private readonly reference type
     */

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as-is (value type)
    public void TestPrivateReadonlyReferenceTypeField()
    {
        const string fieldName = "PrivateReadonlyRefTypeField";
        const string value = "test";

        StaticSetter<string> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);
        
        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

        setter(null);
    }

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as boxed value type
    public void TestPrivateReadonlyObjectReferenceTypeField()
    {
        const string fieldName = "PrivateReadonlyRefTypeField";
        const string value = "test";

        StaticSetter<object> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

        setter(null);
    }

    /*
     * Public readonly value type
     */

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as-is (value type)
    public void TestPublicReadonlyValueTypeField()
    {
        const string fieldName = "PublicReadonlyValTypeField";

        Assert.ThrowsException<NotSupportedException>(() =>
        {
            _ = Accessor.GenerateStaticSetter<SampleStaticMembers, int>(fieldName, throwOnError: true)!;
        }, "Didn't throw NotSupportedException for readonly static value type field.");
    }

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as boxed value type
    public void TestPublicReadonlyBoxedValueTypeField()
    {
        const string fieldName = "PublicReadonlyValTypeField";

        Assert.ThrowsException<NotSupportedException>(() =>
        {
            _ = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;
        }, "Didn't throw NotSupportedException for readonly static value type field.");
    }

    /*
     * Public readonly reference type
     */

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as-is (value type)
    public void TestPublicReadonlyReferenceTypeField()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        StaticSetter<string> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, string>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicReadonlyRefTypeField);

        setter(null);
    }

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as boxed value type
    public void TestPublicReadonlyObjectReferenceTypeField()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        StaticSetter<object> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicReadonlyRefTypeField);

        setter(null);
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

        StaticSetter<int> setter = variable.GenerateSetter(throwOnError: true);

        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

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

        StaticSetter<string> setter = variable.GenerateSetter(throwOnError: true);
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);
        
        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

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

        StaticSetter<int> setter = variable.GenerateSetter(throwOnError: true);

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicValTypeField);

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

        StaticSetter<string> setter = variable.GenerateSetter(throwOnError: true);

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicRefTypeField);

        SampleStaticMembers.PublicRefTypeField = null;
    }

    /*
     * Private readonly value type
     */

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as-is (value type)
    public void TestPrivateReadonlyValueTypeField_IVariable()
    {
        const string fieldName = "PrivateReadonlyValTypeField";
        IStaticVariable<int>? variable = Variables.FindStatic<SampleStaticMembers, int>(fieldName);
        Assert.IsNotNull(variable);

#if !NET && !NETCOREAPP
        const int value = 1;

        StaticSetter<int> setter = variable.GenerateSetter(throwOnError: true);
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

        field.SetValue(null, 0);
#else
        Assert.ThrowsException<NotSupportedException>(() =>
        {
            _ = variable.GenerateSetter(throwOnError: true);
        }, "Didn't throw NotSupportedException for readonly static value type field.");
#endif
    }

    /*
     * Private readonly reference type
     */

    [TestMethod]
    // field: private, readonly, value type
    // value: passed as-is (value type)
    public void TestPrivateReadonlyReferenceTypeField_IVariable()
    {
        const string fieldName = "PrivateReadonlyRefTypeField";
        const string value = "test";

        IStaticVariable<string>? variable = Variables.FindStatic<SampleStaticMembers, string>(fieldName);
        Assert.IsNotNull(variable);

        StaticSetter<string> setter = variable.GenerateSetter(throwOnError: true);
        FieldInfo field = typeof(SampleStaticMembers).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.IsNotNull(setter);
        
        setter(value);

        Assert.AreEqual(value, field.GetValue(null));

        setter(null);
    }

    /*
     * Public readonly value type
     */

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as-is (value type)
    public void TestPublicReadonlyValueTypeField_IVariable()
    {
        const string fieldName = "PublicReadonlyValTypeField";
        IStaticVariable<int>? variable = Variables.FindStatic<SampleStaticMembers, int>(fieldName);
        Assert.IsNotNull(variable);

        Assert.ThrowsException<NotSupportedException>(() =>
        {
            _ = variable.GenerateSetter(throwOnError: true);
        }, "Didn't throw NotSupportedException for readonly static value type field.");
    }

    /*
     * Public readonly reference type
     */

    [TestMethod]
    // field: public, readonly, value type
    // value: passed as-is (value type)
    public void TestPublicReadonlyReferenceTypeField_IVariable()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        IStaticVariable<string>? variable = Variables.FindStatic<SampleStaticMembers, string>(fieldName);
        Assert.IsNotNull(variable);

        StaticSetter<string> setter = variable.GenerateSetter(throwOnError: true);

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicReadonlyRefTypeField);

        setter(null);
    }
}
