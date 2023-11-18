using DanielWillett.ReflectionTools.Tests.SampleObjects;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GenerateInstanceGetter
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

        InstanceGetter<SampleClass, int> getter = Accessor.GenerateInstanceGetter<SampleClass, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<SampleClass, object> getter = Accessor.GenerateInstanceGetter<SampleClass, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<object, int> getter = Accessor.GenerateInstanceGetter<int>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<SampleClass, int> getter = Accessor.GenerateInstanceGetter<SampleClass, int>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass
        {
            PublicValTypeField = value
        };

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<SampleClass, object> getter = Accessor.GenerateInstanceGetter<SampleClass, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass
        {
            PublicValTypeField = value
        };

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<object, int> getter = Accessor.GenerateInstanceGetter<int>(typeof(SampleClass), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass
        {
            PublicValTypeField = value
        };

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass
        {
            PublicValTypeField = value
        };

        Assert.AreEqual(value, (int)getter(instance));
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
        const int value = 1;

        InstanceGetter<SampleStruct, int> getter = Accessor.GenerateInstanceGetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter((SampleStruct)instance));
    }

    [TestMethod]
    // field: private, non-readonly, value type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as a boxed value type
    public void TestPrivateBoxedValueTypeFieldInValueType()
    {
        const string fieldName = "_privateValTypeField";
        const int value = 1;

        InstanceGetter<SampleStruct, object> getter = Accessor.GenerateInstanceGetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (int)getter((SampleStruct)instance));
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

        InstanceGetter<object, int> getter = Accessor.GenerateInstanceGetter<int>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (int)getter((SampleStruct)instance));
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
        const int value = 1;

        InstanceGetter<SampleStruct, int> getter = Accessor.GenerateInstanceGetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct
        {
            PublicValTypeField = value
        };

        Assert.AreEqual(value, getter(instance));
    }

    [TestMethod]
    // field: public, non-readonly, value type
    // instance: value type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestPublicBoxedValueTypeFieldInValueType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceGetter<SampleStruct, object> getter = Accessor.GenerateInstanceGetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct
        {
            PublicValTypeField = value
        };

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<object, int> getter = Accessor.GenerateInstanceGetter<int>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct
        {
            PublicValTypeField = value
        };

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct
        {
            PublicValTypeField = value
        };

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<SampleClass, string> getter = Accessor.GenerateInstanceGetter<SampleClass, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<SampleClass, object> getter = Accessor.GenerateInstanceGetter<SampleClass, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<object, string> getter = Accessor.GenerateInstanceGetter<string>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<SampleClass, string> getter = Accessor.GenerateInstanceGetter<SampleClass, string>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass
        {
            PublicRefTypeField = value
        };

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<SampleClass, object> getter = Accessor.GenerateInstanceGetter<SampleClass, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass
        {
            PublicRefTypeField = value
        };

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<object, string> getter = Accessor.GenerateInstanceGetter<string>(typeof(SampleClass), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass
        {
            PublicRefTypeField = value
        };

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass
        {
            PublicRefTypeField = value
        };

        Assert.AreEqual(value, (string)getter(instance));
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
        const string value = "test";

        InstanceGetter<SampleStruct, string> getter = Accessor.GenerateInstanceGetter<SampleStruct, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter((SampleStruct)instance));
    }

    [TestMethod]
    // field: private, non-readonly, reference type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as a boxed value type
    public void TestPrivateObjectReferenceTypeFieldInValueType()
    {
        const string fieldName = "_privateRefTypeField";
        const string value = "test";

        InstanceGetter<SampleStruct, object> getter = Accessor.GenerateInstanceGetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (string)getter((SampleStruct)instance));
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

        InstanceGetter<object, string> getter = Accessor.GenerateInstanceGetter<string>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (string)getter((SampleStruct)instance));
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
        const string value = "test";

        InstanceGetter<SampleStruct, string> getter = Accessor.GenerateInstanceGetter<SampleStruct, string>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct
        {
            PublicRefTypeField = value
        };

        Assert.AreEqual(value, getter(instance));
    }

    [TestMethod]
    // field: public, non-readonly, reference type
    // instance: value type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestPublicObjectReferenceTypeFieldInValueType()
    {
        const string fieldName = "PublicRefTypeField";
        const string value = "test";

        InstanceGetter<SampleStruct, object> getter = Accessor.GenerateInstanceGetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct
        {
            PublicRefTypeField = value
        };

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<object, string> getter = Accessor.GenerateInstanceGetter<string>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct
        {
            PublicRefTypeField = value
        };

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct
        {
            PublicRefTypeField = value
        };

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<SampleClass, int> getter = Accessor.GenerateInstanceGetter<SampleClass, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<SampleClass, object> getter = Accessor.GenerateInstanceGetter<SampleClass, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<object, int> getter = Accessor.GenerateInstanceGetter<int>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<SampleClass, int> getter = Accessor.GenerateInstanceGetter<SampleClass, int>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass(value, null);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<SampleClass, object> getter = Accessor.GenerateInstanceGetter<SampleClass, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass(value, null);

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<object, int> getter = Accessor.GenerateInstanceGetter<int>(typeof(SampleClass), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass(value, null);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass(value, null);

        Assert.AreEqual(value, (int)getter(instance));
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
        const int value = 1;

        InstanceGetter<SampleStruct, int> getter = Accessor.GenerateInstanceGetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter((SampleStruct)instance));
    }

    [TestMethod]
    // field: private, readonly, value type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as a boxed value type
    public void TestReadonlyPrivateBoxedValueTypeFieldInValueType()
    {
        const string fieldName = "_privateReadonlyValTypeField";
        const int value = 1;

        InstanceGetter<SampleStruct, object> getter = Accessor.GenerateInstanceGetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (int)getter((SampleStruct)instance));
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

        InstanceGetter<object, int> getter = Accessor.GenerateInstanceGetter<int>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (int)getter((SampleStruct)instance));
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
        const int value = 1;

        InstanceGetter<SampleStruct, int> getter = Accessor.GenerateInstanceGetter<SampleStruct, int>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct(value, null);

        Assert.AreEqual(value, getter(instance));
    }

    [TestMethod]
    // field: public, readonly, value type
    // instance: value type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestReadonlyPublicBoxedValueTypeFieldInValueType()
    {
        const string fieldName = "PublicReadonlyValTypeField";
        const int value = 1;

        InstanceGetter<SampleStruct, object> getter = Accessor.GenerateInstanceGetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct(value, null);

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<object, int> getter = Accessor.GenerateInstanceGetter<int>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct(value, null);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct(value, null);

        Assert.AreEqual(value, (int)getter(instance));
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

        InstanceGetter<SampleClass, string> getter = Accessor.GenerateInstanceGetter<SampleClass, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<SampleClass, object> getter = Accessor.GenerateInstanceGetter<SampleClass, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<object, string> getter = Accessor.GenerateInstanceGetter<string>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleClass).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<SampleClass, string> getter = Accessor.GenerateInstanceGetter<SampleClass, string>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass(0, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<SampleClass, object> getter = Accessor.GenerateInstanceGetter<SampleClass, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass(0, value);

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<object, string> getter = Accessor.GenerateInstanceGetter<string>(typeof(SampleClass), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass(0, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleClass instance = new SampleClass(0, value);

        Assert.AreEqual(value, (string)getter(instance));
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
        const string value = "test";

        InstanceGetter<SampleStruct, string> getter = Accessor.GenerateInstanceGetter<SampleStruct, string>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter((SampleStruct)instance));
    }

    [TestMethod]
    // field: private, readonly, reference type
    // instance: value type
    // instance: passed as-is (value type)
    // value: passed as a boxed value type
    public void TestReadonlyPrivateObjectReferenceTypeFieldInValueType()
    {
        const string fieldName = "_privateReadonlyRefTypeField";
        const string value = "test";

        InstanceGetter<SampleStruct, object> getter = Accessor.GenerateInstanceGetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (string)getter((SampleStruct)instance));
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

        InstanceGetter<object, string> getter = Accessor.GenerateInstanceGetter<string>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        FieldInfo field = typeof(SampleStruct).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!;

        Assert.IsNotNull(getter);

        object instance = new SampleStruct();
        field.SetValue(instance, value);

        Assert.AreEqual(value, (string)getter((SampleStruct)instance));
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
        const string value = "test";

        InstanceGetter<SampleStruct, string> getter = Accessor.GenerateInstanceGetter<SampleStruct, string>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct(0, value);

        Assert.AreEqual(value, getter(instance));
    }

    [TestMethod]
    // field: public, readonly, reference type
    // instance: value type
    // instance: passed as-is
    // value: passed as a boxed value type
    public void TestReadonlyPublicObjectReferenceTypeFieldInValueType()
    {
        const string fieldName = "PublicReadonlyRefTypeField";
        const string value = "test";

        InstanceGetter<SampleStruct, object> getter = Accessor.GenerateInstanceGetter<SampleStruct, object>(fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct(0, value);

        Assert.AreEqual(value, (string)getter(instance));
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

        InstanceGetter<object, string> getter = Accessor.GenerateInstanceGetter<string>(typeof(SampleStruct), fieldName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct(0, value);

        Assert.AreEqual(value, getter(instance));
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

        InstanceGetter<object, object> getter = Accessor.GenerateInstanceGetter<object>(typeof(SampleStruct), fieldName, throwOnError: true)!;

        Assert.IsNotNull(getter);

        SampleStruct instance = new SampleStruct(0, value);

        Assert.AreEqual(value, (string)getter(instance));
    }
}