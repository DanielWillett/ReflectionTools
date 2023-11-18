using DanielWillett.ReflectionTools.Tests.SampleObjects;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_SetterTypeChecking
{
    [TestMethod]
    public void CheckSetStaticValueTypeField()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;
        
        StaticSetter<object?> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object?>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        // does not throw exception
        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicValTypeField);

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            setter(null);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(new object());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(1u);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(3.0f);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(new SampleClass());
        });
    }

    [TestMethod]
    public void CheckSetStaticReferenceTypeField()
    {
        const string fieldName = "PublicBaseClassField";

        StaticSetter<object?> setter = Accessor.GenerateStaticSetter<SampleStaticMembers, object?>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        // does not throw exception
        setter(new SampleBaseClass());

        Assert.AreEqual(typeof(SampleBaseClass), SampleStaticMembers.PublicBaseClassField.GetType());

        // does not throw exception
        setter(new SampleDerivingClass());

        Assert.AreEqual(typeof(SampleDerivingClass), SampleStaticMembers.PublicBaseClassField.GetType());

        // does not throw exception
        setter(new SampleDoubleDerivingClass());

        Assert.AreEqual(typeof(SampleDoubleDerivingClass), SampleStaticMembers.PublicBaseClassField.GetType());

        // does not throw exception
        setter(null);

        Assert.IsNull(SampleStaticMembers.PublicBaseClassField);

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(new object());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(1u);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(3.0f);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(new SampleClass());
        });
    }
    [TestMethod]
    public void CheckSetInstanceValueTypeFieldInReferenceType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceSetter<SampleClass, object?> setter = Accessor.GenerateInstanceSetter<SampleClass, object?>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        SampleClass sampleClass = new SampleClass();

        // does not throw exception
        setter(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            setter(sampleClass, null);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, new object());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, 1u);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, 3.0f);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, new SampleClass());
        });

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            setter(null!, value);
        });
    }

    [TestMethod]
    public void CheckSetInstanceReferenceTypeFieldInReferenceType()
    {
        const string fieldName = "PublicBaseClassField";

        InstanceSetter<SampleClass, object?> setter = Accessor.GenerateInstanceSetter<SampleClass, object?>(fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        SampleClass sampleClass = new SampleClass();

        // does not throw exception
        setter(sampleClass, new SampleBaseClass());

        Assert.AreEqual(typeof(SampleBaseClass), sampleClass.PublicBaseClassField.GetType());

        // does not throw exception
        setter(sampleClass, new SampleDerivingClass());

        Assert.AreEqual(typeof(SampleDerivingClass), sampleClass.PublicBaseClassField.GetType());

        // does not throw exception
        setter(sampleClass, new SampleDoubleDerivingClass());

        Assert.AreEqual(typeof(SampleDoubleDerivingClass), sampleClass.PublicBaseClassField.GetType());

        // does not throw exception
        setter(sampleClass, null);

        Assert.IsNull(sampleClass.PublicBaseClassField);

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, new object());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, 1u);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, 3.0f);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, new SampleClass());
        });

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            setter(null!, new SampleBaseClass());
        });
    }
    [TestMethod]
    public void CheckSetInstanceValueTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicValTypeField";
        const int value = 1;

        InstanceSetter<object?, object?> setter = Accessor.GenerateInstanceSetter<object?>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        SampleClass sampleClass = new SampleClass();

        // does not throw exception
        setter(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeField);

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            setter(sampleClass, null);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, new object());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, 1u);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, 3.0f);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, new SampleClass());
        });

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            setter(null!, value);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(3, value);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter("test", value);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(new SampleBaseClass(), value);
        });
    }

    [TestMethod]
    public void CheckSetInstanceReferenceTypeFieldInObjectReferenceType()
    {
        const string fieldName = "PublicBaseClassField";

        InstanceSetter<object?, object?> setter = Accessor.GenerateInstanceSetter<object?>(typeof(SampleClass), fieldName, throwOnError: true)!;

        Assert.IsNotNull(setter);

        SampleClass sampleClass = new SampleClass();

        // does not throw exception
        setter(sampleClass, new SampleBaseClass());

        Assert.AreEqual(typeof(SampleBaseClass), sampleClass.PublicBaseClassField.GetType());

        // does not throw exception
        setter(sampleClass, new SampleDerivingClass());

        Assert.AreEqual(typeof(SampleDerivingClass), sampleClass.PublicBaseClassField.GetType());

        // does not throw exception
        setter(sampleClass, new SampleDoubleDerivingClass());

        Assert.AreEqual(typeof(SampleDoubleDerivingClass), sampleClass.PublicBaseClassField.GetType());

        // does not throw exception
        setter(sampleClass, null);

        Assert.IsNull(sampleClass.PublicBaseClassField);

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, new object());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, 1u);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, 3.0f);
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(sampleClass, new SampleClass());
        });

        Assert.ThrowsException<NullReferenceException>(() =>
        {
            setter(null!, new SampleBaseClass());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(3, new SampleBaseClass());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter("test", new SampleBaseClass());
        });

        Assert.ThrowsException<InvalidCastException>(() =>
        {
            setter(new SampleBaseClass(), new SampleBaseClass());
        });
    }
}
