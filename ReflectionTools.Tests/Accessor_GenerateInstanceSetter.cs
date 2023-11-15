using DanielWillett.ReflectionTools.Tests.SampleObjects;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GenerateInstanceSetter
{
    /*
     * Private value type field in reference type
     */

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: reference type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestPrivateValueTypeFieldInReferenceType()
    {
        const string fieldName = "_privateValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, int> setter = Accessor.GenerateInstanceSetter<SampleClass, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: reference type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestPrivateBoxedValueTypeFieldInReferenceType()
    {
        const string fieldName = "_privateValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, object> setter = Accessor.GenerateInstanceSetter<SampleClass, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: reference type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestPrivateValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "_privateValTypeField";
        const int value = 1;

        InstanceSetter<object, int> setter = Accessor.GenerateInstanceSetter<int>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: reference type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestPrivateBoxedValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "_privateValTypeField";
        const int value = 1;

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    /*
     * Public value type field in reference type
     */

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: reference type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestPublicValueTypeFieldInReferenceType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, int> setter = Accessor.GenerateInstanceSetter<SampleClass, int>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);
        
        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicValTypeField);
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: reference type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestPublicBoxedValueTypeFieldInReferenceType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, object> setter = Accessor.GenerateInstanceSetter<SampleClass, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicValTypeField);
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: reference type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestPublicValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceSetter<object, int> setter = Accessor.GenerateInstanceSetter<int>(typeof(SampleClass), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicValTypeField);
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: reference type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestPublicBoxedValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicValTypeField);
    }

    /*
     * Private value type field in value type
     */

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as-is (value type)
    public void TestPrivateValueTypeFieldInValueType()
    {
        const string fieldName = "_privateValTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as a boxed value type
    public void TestPrivateBoxedValueTypeFieldInValueType()
    {
        const string fieldName = "_privateValTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: value type
    // instance: passed as a boxed value type
    // value: passed as-is (value type)
    public void TestPrivateValueTypeFieldInObjectValueType()
    {
        const string fieldName = "_privateValTypeField";
        const int value = 1;

        InstanceSetter<object, int> setter = Accessor.GenerateInstanceSetter<int>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: value type
    // instance: passed as a boxed value type
    // value: passed as a boxed value type
    public void TestPrivateBoxedValueTypeFieldInObjectValueType()
    {
        const string fieldName = "_privateValTypeField";
        const int value = 1;

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    /*
     * Public value type field in value type
     */

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: value type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestPublicValueTypeFieldInValueType()
    {
        const string fieldName = "PublicValTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: value type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestPublicBoxedValueTypeFieldInValueType()
    {
        const string fieldName = "PublicValTypeField";
        
        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: value type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestPublicValueTypeFieldInObjectValueType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceSetter<object, int> setter = Accessor.GenerateInstanceSetter<int>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, ((SampleStruct)instance).PublicValTypeField);
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: value type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestPublicBoxedValueTypeFieldInObjectValueType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, ((SampleStruct)instance).PublicValTypeField);
    }

    /*
     * Private reference type field in reference type
     */

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: reference type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestPrivateReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "_privateRefTypeField";
        const string value = "test";

        InstanceSetter<SampleClass, string> setter = Accessor.GenerateInstanceSetter<SampleClass, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: reference type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestPrivateObjectReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "_privateRefTypeField";
        const string value = "test";

        InstanceSetter<SampleClass, object> setter = Accessor.GenerateInstanceSetter<SampleClass, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: reference type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestPrivateReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "_privateRefTypeField";
        const string value = "test";

        InstanceSetter<object, string> setter = Accessor.GenerateInstanceSetter<string>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: reference type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestPrivateObjectReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "_privateRefTypeField";
        const string value = "test";

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    /*
     * Public reference type field in reference type
     */

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: reference type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestPublicReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        InstanceSetter<SampleClass, string> setter = Accessor.GenerateInstanceSetter<SampleClass, string>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicRefTypeField);
    }

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: reference type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestPublicObjectReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        InstanceSetter<SampleClass, object> setter = Accessor.GenerateInstanceSetter<SampleClass, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicRefTypeField);
    }

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: reference type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestPublicReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        InstanceSetter<object, string> setter = Accessor.GenerateInstanceSetter<string>(typeof(SampleClass), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicRefTypeField);
    }

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: reference type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestPublicObjectReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicRefTypeField);
    }

    /*
     * Private reference type field in value type
     */

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as-is (value type)
    public void TestPrivateReferenceTypeFieldInValueType()
    {
        const string fieldName = "_privateRefTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as a boxed value type
    public void TestPrivateObjectReferenceTypeFieldInValueType()
    {
        const string fieldName = "_privateRefTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: value type
    // instance: passed as a boxed value type
    // value: passed as-is (value type)
    public void TestPrivateReferenceTypeFieldInObjectValueType()
    {
        const string fieldName = "_privateRefTypeField";
        const string value = "test";

        InstanceSetter<object, string> setter = Accessor.GenerateInstanceSetter<string>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: value type
    // instance: passed as a boxed value type
    // value: passed as a boxed value type
    public void TestPrivateObjectReferenceTypeFieldInObjectValueType()
    {
        const string fieldName = "_privateRefTypeField";
        const string value = "test";

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    /*
     * Public reference type field in value type
     */

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: value type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestPublicReferenceTypeFieldInValueType()
    {
        const string fieldName = "PublicRefTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: value type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestPublicObjectReferenceTypeFieldInValueType()
    {
        const string fieldName = "PublicRefTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: value type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestPublicReferenceTypeFieldInObjectValueType()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        InstanceSetter<object, string> setter = Accessor.GenerateInstanceSetter<string>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, ((SampleStruct)instance).PublicRefTypeField);
    }

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: value type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestPublicObjectReferenceTypeFieldInObjectValueType()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, ((SampleStruct)instance).PublicRefTypeField);
    }

    /*
     * Private readonly value type field in reference type
     */

    [TestMethod]
    // field: private, readonly, value type
    // instance: reference type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestReadonlyPrivateValueTypeFieldInReferenceType()
    {
        const string fieldName = "_privateReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, int> setter = Accessor.GenerateInstanceSetter<SampleClass, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, (int)field.GetValue(instance));
    }

    [TestMethod]
    // field: private, readonly, value type
    // instance: reference type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestReadonlyPrivateBoxedValueTypeFieldInReferenceType()
    {
        const string fieldName = "_privateReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, object> setter = Accessor.GenerateInstanceSetter<SampleClass, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, (int)field.GetValue(instance));
    }

    [TestMethod]
    // field: private, readonly, value type
    // instance: reference type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestReadonlyPrivateValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "_privateReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<object, int> setter = Accessor.GenerateInstanceSetter<int>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, (int)field.GetValue(instance));
    }

    [TestMethod]
    // field: private, readonly, value type
    // instance: reference type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestReadonlyPrivateBoxedValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "_privateReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, (int)field.GetValue(instance));
    }

    /*
     * Public readonly value type field in reference type
     */

    [TestMethod]
    // field: public, readonly, value type
    // instance: reference type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestReadonlyPublicValueTypeFieldInReferenceType()
    {
        const string fieldName = "PublicReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, int> setter = Accessor.GenerateInstanceSetter<SampleClass, int>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicReadonlyValTypeField);
    }

    [TestMethod]
    // field: public, readonly, value type
    // instance: reference type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestReadonlyPublicBoxedValueTypeFieldInReferenceType()
    {
        const string fieldName = "PublicReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, object> setter = Accessor.GenerateInstanceSetter<SampleClass, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicReadonlyValTypeField);
    }

    [TestMethod]
    // field: public, readonly, value type
    // instance: reference type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestReadonlyPublicValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<object, int> setter = Accessor.GenerateInstanceSetter<int>(typeof(SampleClass), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicReadonlyValTypeField);
    }

    [TestMethod]
    // field: public, readonly, value type
    // instance: reference type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestReadonlyPublicBoxedValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicReadonlyValTypeField);
    }

    /*
     * Private readonly value type field in value type
     */

    [TestMethod]
    // field: private, readonly, value type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as-is (value type)
    public void TestReadonlyPrivateValueTypeFieldInValueType()
    {
        const string fieldName = "_privateReadonlyValTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: private, readonly, value type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as a boxed value type
    public void TestReadonlyPrivateBoxedValueTypeFieldInValueType()
    {
        const string fieldName = "_privateReadonlyValTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: private, readonly, value type
    // instance: value type
    // instance: passed as a boxed value type
    // value: passed as-is (value type)
    public void TestReadonlyPrivateValueTypeFieldInObjectValueType()
    {
        const string fieldName = "_privateReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<object, int> setter = Accessor.GenerateInstanceSetter<int>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, readonly, value type
    // instance: value type
    // instance: passed as a boxed value type
    // value: passed as a boxed value type
    public void TestReadonlyPrivateBoxedValueTypeFieldInObjectValueType()
    {
        const string fieldName = "_privateReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, (int)field.GetValue(instance));
    }

    /*
     * Public readonly value type field in value type
     */

    [TestMethod]
    // field: public, readonly, value type
    // instance: value type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestReadonlyPublicValueTypeFieldInValueType()
    {
        const string fieldName = "PublicReadonlyValTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: public, readonly, value type
    // instance: value type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestReadonlyPublicBoxedValueTypeFieldInValueType()
    {
        const string fieldName = "PublicReadonlyValTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: public, readonly, value type
    // instance: value type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestReadonlyPublicValueTypeFieldInObjectValueType()
    {
        const string fieldName = "PublicReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<object, int> setter = Accessor.GenerateInstanceSetter<int>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, ((SampleStruct)instance).PublicReadonlyValTypeField);
    }

    [TestMethod]
    // field: public, readonly, value type
    // instance: value type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestReadonlyPublicBoxedValueTypeFieldInObjectValueType()
    {
        const string fieldName = "PublicReadonlyValTypeField";
        const int value = 1;

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, ((SampleStruct)instance).PublicReadonlyValTypeField);
    }

    /*
     * Private readonly reference type field in reference type
     */

    [TestMethod]
    // field: private, readonly, reference type
    // instance: reference type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestReadonlyPrivateReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<SampleClass, string> setter = Accessor.GenerateInstanceSetter<SampleClass, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, readonly, reference type
    // instance: reference type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestReadonlyPrivateObjectReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<SampleClass, object> setter = Accessor.GenerateInstanceSetter<SampleClass, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, (string)field.GetValue(instance));
    }

    [TestMethod]
    // field: private, readonly, reference type
    // instance: reference type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestReadonlyPrivateReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<object, string> setter = Accessor.GenerateInstanceSetter<string>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, readonly, reference type
    // instance: reference type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestReadonlyPrivateObjectReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, (string)field.GetValue(instance));
    }

    /*
     * Public readonly reference type field in reference type
     */

    [TestMethod]
    // field: public, readonly, reference type
    // instance: reference type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestReadonlyPublicReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<SampleClass, string> setter = Accessor.GenerateInstanceSetter<SampleClass, string>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicReadonlyRefTypeField);
    }

    [TestMethod]
    // field: public, readonly, reference type
    // instance: reference type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestReadonlyPublicObjectReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<SampleClass, object> setter = Accessor.GenerateInstanceSetter<SampleClass, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicReadonlyRefTypeField);
    }

    [TestMethod]
    // field: public, readonly, reference type
    // instance: reference type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestReadonlyPublicReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<object, string> setter = Accessor.GenerateInstanceSetter<string>(typeof(SampleClass), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicReadonlyRefTypeField);
    }

    [TestMethod]
    // field: public, readonly, reference type
    // instance: reference type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestReadonlyPublicObjectReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        SampleClass instance = new SampleClass();
        setter(instance, value);

        Assert.AreEqual(value, instance.PublicReadonlyRefTypeField);
    }

    /*
     * Private readonly reference type field in value type
     */

    [TestMethod]
    // field: private, readonly, reference type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as-is (value type)
    public void TestReadonlyPrivateReferenceTypeFieldInValueType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: private, readonly, reference type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as a boxed value type
    public void TestReadonlyPrivateObjectReferenceTypeFieldInValueType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: private, readonly, reference type
    // instance: value type
    // instance: passed as a boxed value type
    // value: passed as-is (value type)
    public void TestReadonlyPrivateReferenceTypeFieldInObjectValueType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<object, string> setter = Accessor.GenerateInstanceSetter<string>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, field.GetValue(instance));
    }

    [TestMethod]
    // field: private, readonly, reference type
    // instance: value type
    // instance: passed as a boxed value type
    // value: passed as a boxed value type
    public void TestReadonlyPrivateObjectReferenceTypeFieldInObjectValueType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, (string)field.GetValue(instance));
    }

    /*
     * Public readonly reference type field in value type
     */

    [TestMethod]
    // field: public, readonly, reference type
    // instance: value type
    // instance: passed as-is
    // value: passed as-is (value type)
    public void TestReadonlyPublicReferenceTypeFieldInValueType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: public, readonly, reference type
    // instance: value type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestReadonlyPublicObjectReferenceTypeFieldInValueType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstanceSetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        }, "Does not throw exceptions for setting value types.");
    }

    [TestMethod]
    // field: public, readonly, reference type
    // instance: value type
    // instance: passed as an object
    // value: passed as-is (value type)
    public void TestReadonlyPublicReferenceTypeFieldInObjectValueType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<object, string> setter = Accessor.GenerateInstanceSetter<string>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, ((SampleStruct)instance).PublicReadonlyRefTypeField);
    }

    [TestMethod]
    // field: public, readonly, reference type
    // instance: value type
    // instance: passed as an object
    // value: passed as a boxed value type
    public void TestReadonlyPublicObjectReferenceTypeFieldInObjectValueType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        InstanceSetter<object, object> setter = Accessor.GenerateInstanceSetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        object instance = new SampleStruct();
        setter(instance, value);

        Assert.AreEqual(value, ((SampleStruct)instance).PublicReadonlyRefTypeField);
    }
}