using DanielWillett.ReflectionTools.Tests.SampleObjects;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GeneratePropertySetter
{
    [TestMethod]
    public void BasicInstanceSetter()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        InstanceSetter<SampleClass, int> setter = Accessor.GenerateInstancePropertySetter<SampleClass, int>(propertyName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        SampleClass sampleClass = new SampleClass();

        setter(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeProperty);
    }
    [TestMethod]
    public void BasicInstanceNoSetterThrowsException()
    {
        const string propertyName = "PublicGetonlyValTypeProperty";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateInstancePropertySetter<SampleClass, int>(propertyName, throwOnError: true)!;
        }, "Did not throw exception on missing setter.");
    }
    [TestMethod]
    public void BasicStaticSetter()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        StaticSetter<int> setter = Accessor.GenerateStaticPropertySetter<SampleStaticMembers, int>(propertyName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicValTypeProperty);
    }
    [TestMethod]
    public void BasicStaticNoSetterThrowsException()
    {
        const string propertyName = "PublicGetonlyValTypeProperty";

        Assert.ThrowsException<Exception>(() =>
        {
            _ = Accessor.GenerateStaticPropertySetter<SampleStaticMembers, int>(propertyName, throwOnError: true)!;
        }, "Did not throw exception on missing setter.");
    }
}
