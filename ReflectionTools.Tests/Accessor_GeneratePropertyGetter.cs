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
}
