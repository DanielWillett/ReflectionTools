namespace DanielWillett.ReflectionTools.Tests.SampleObjects;
#nullable disable
public class SampleClass
{
    private int _privateValTypeField;
    private readonly int _privateReadonlyValTypeField;

    private string _privateRefTypeField;
    private readonly string _privateReadonlyRefTypeField;

    public string PublicRefTypeField;
    public readonly string PublicReadonlyRefTypeField;

    public int PublicValTypeField;
    public readonly int PublicReadonlyValTypeField;

    public int PublicValTypeProperty { get; set; }
    public int PublicGetonlyValTypeProperty { get; }
    public int PublicSetonlyValTypeProperty { set => _privateValTypeField = value; }

    public string PublicRefTypeProperty { get; set; }
    public string PublicGetonlyRefTypeProperty { get; }
    public string PublicSetonlyRefTypeProperty { set => _privateRefTypeField = value; }

    public SampleBaseClass PublicBaseClassField;

    public SampleClass()
    {

    }
    public SampleClass(int publicReadonlyValType, string publicReadonlyRefType)
    {
        PublicReadonlyRefTypeField = publicReadonlyRefType;
        PublicReadonlyValTypeField = publicReadonlyValType;
        
        PublicGetonlyValTypeProperty = publicReadonlyValType;
        PublicGetonlyRefTypeProperty = publicReadonlyRefType;
    }

    public void NotImplementedNoParams()
    {
        throw new NotImplementedException();
    }
    public void SetRefTypeField(string value)
    {
        PublicRefTypeField = value;
    }
    public void SetValTypeField(int value)
    {
        PublicValTypeField = value;
    }
    public void TestMethodWithRefVTParameter(int value, ref int refValue)
    {
        refValue = value;
    }
    public void TestMethodWithRefRTParameter(string value, ref string refValue)
    {
        refValue = value;
    }
    public void TestMethodWithOutVTParameter(int value, out int outValue)
    {
        outValue = value;
    }
    public void TestMethodWithOutRTParameter(string value, out string outValue)
    {
        outValue = value;
    }
    public void TestMethodWithInVTParameter(int value, in int outValue)
    {

    }
    public void TestMethodWithInRTParameter(string value, in string outValue)
    {
        
    }
}
#nullable restore