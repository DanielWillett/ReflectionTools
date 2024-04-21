using DanielWillett.ReflectionTools.Tests.SampleObjects;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GeneratePropertyGetter
{
    [TestMethod]
    public void BasicInstanceGetter()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        InstanceGetter<SampleClass, int> getter = Accessor.GenerateInstancePropertyGetter<SampleClass, int>(propertyName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleClass sampleClass = new SampleClass
        {
            PublicValTypeProperty = value
        };

        Assert.AreEqual(value, getter(sampleClass));
    }
    [TestMethod]
    public void ValueTypeInstanceGetter()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        InstanceGetter<SampleStruct, int> getter = Accessor.GenerateInstancePropertyGetter<SampleStruct, int>(propertyName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStruct sampleStruct = new SampleStruct
        {
            PublicValTypeProperty = value
        };

        Assert.AreEqual(value, getter(sampleStruct));
    }
    [TestMethod]
    public void BoxedValueTypeInstanceGetter()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        InstanceGetter<object, int> getter = Accessor.GenerateInstancePropertyGetter<int>(typeof(SampleStruct), propertyName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        object sampleStruct = new SampleStruct
        {
            PublicValTypeProperty = value
        };

        Assert.AreEqual(value, getter(sampleStruct));
    }
    [TestMethod]
    public void BasicInstanceNoGetterThrowsException()
    {
        const string propertyName = "PublicSetonlyValTypeProperty";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstancePropertyGetter<SampleClass, int>(propertyName, throwOnError: true)!;
        }, "Did not throw exception on missing getter.");
    }
    [TestMethod]
    public void BasicStaticGetter()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        StaticGetter<int> getter = Accessor.GenerateStaticPropertyGetter<SampleStaticMembers, int>(propertyName, throwOnError: true)!;
        
        Assert.IsNotNull(getter);

        SampleStaticMembers.PublicValTypeProperty = value;

        Assert.AreEqual(value, getter());
    }
    [TestMethod]
    public void BasicStaticNoGetterThrowsException()
    {
        const string propertyName = "PublicSetonlyValTypeProperty";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateStaticPropertyGetter<SampleStaticMembers, int>(propertyName, throwOnError: true)!;
        }, "Did not throw exception on missing getter.");
    }
    [TestMethod]
    public void BasicInstanceGetter_IVariable()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        IInstanceVariable<SampleClass, int>? variable = Variables.FindInstance<SampleClass, int>(propertyName);
        Assert.IsNotNull(variable);

        InstanceGetter<SampleClass, int> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

        SampleClass sampleClass = new SampleClass
        {
            PublicValTypeProperty = value
        };

        Assert.AreEqual(value, getter(sampleClass));
    }
    [TestMethod]
    public void ValueTypeInstanceGetter_IVariable()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        IInstanceVariable<SampleStruct, int>? variable = Variables.FindInstance<SampleStruct, int>(propertyName);
        Assert.IsNotNull(variable);

        InstanceGetter<SampleStruct, int> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

        SampleStruct sampleStruct = new SampleStruct
        {
            PublicValTypeProperty = value
        };

        Assert.AreEqual(value, getter(sampleStruct));
    }
    [TestMethod]
    public void BasicStaticGetter_IVariable()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        IStaticVariable<int>? variable = Variables.FindStatic<SampleStaticMembers, int>(propertyName);
        Assert.IsNotNull(variable);

        StaticGetter<int> getter = variable.GenerateGetter(throwOnError: true);

        Assert.IsNotNull(getter);

        SampleStaticMembers.PublicValTypeProperty = value;

        Assert.AreEqual(value, getter());
    }
}
