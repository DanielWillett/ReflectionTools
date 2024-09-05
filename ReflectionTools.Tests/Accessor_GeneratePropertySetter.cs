using DanielWillett.ReflectionTools.Tests.SampleObjects;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("Accessor")]
public class Accessor_GeneratePropertySetter
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

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
    public void ValTypeInstanceSetter()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        InstanceSetter<object, int> setter = Accessor.GenerateInstancePropertySetter<int>(typeof(SampleStruct), propertyName, throwOnError: true)!;
        
        Assert.IsNotNull(setter);

        object sampleClass = new SampleStruct();

        setter(sampleClass, value);

        Assert.AreEqual(value, ((SampleStruct)sampleClass).PublicValTypeProperty);
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
    [TestMethod]
    public void BasicInstanceSetter_IVariable()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        IInstanceVariable<SampleClass, int>? variable = Variables.FindInstance<SampleClass, int>(propertyName);
        Assert.IsNotNull(variable);

        InstanceSetter<SampleClass, int> setter = variable.GenerateReferenceTypeSetter(throwOnError: true);

        Assert.IsNotNull(setter);

        SampleClass sampleClass = new SampleClass();

        setter(sampleClass, value);

        Assert.AreEqual(value, sampleClass.PublicValTypeProperty);
    }
    [TestMethod]
    public void ValTypeInstanceSetter_IVariable()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        IInstanceVariable<SampleStruct, int>? variable = Variables.FindInstance<SampleStruct, int>(propertyName);
        Assert.IsNotNull(variable);

        InstanceSetter<object, int> setter = variable.GenerateSetter(throwOnError: true);

        Assert.IsNotNull(setter);

        object sampleStruct = new SampleStruct();

        setter(sampleStruct, value);

        Assert.AreEqual(value, ((SampleStruct)sampleStruct).PublicValTypeProperty);
    }
    [TestMethod]
    public void BasicStaticSetter_IVariable()
    {
        const string propertyName = "PublicValTypeProperty";
        const int value = 3;

        IStaticVariable<int>? variable = Variables.FindStatic<SampleStaticMembers, int>(propertyName);
        Assert.IsNotNull(variable);

        StaticSetter<int> setter = variable.GenerateSetter(throwOnError: true);

        Assert.IsNotNull(setter);

        setter(value);

        Assert.AreEqual(value, SampleStaticMembers.PublicValTypeProperty);
    }
}
