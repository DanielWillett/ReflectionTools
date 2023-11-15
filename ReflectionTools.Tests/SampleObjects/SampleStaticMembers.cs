namespace DanielWillett.ReflectionTools.Tests.SampleObjects;
public class SampleStaticMembers
{
    public static int PublicValTypeField;
    public static string PublicRefTypeField;
    private static int PrivateValTypeField;
    private static string PrivateRefTypeField;
    
    public static readonly int PublicReadonlyValTypeField;
    public static readonly string PublicReadonlyRefTypeField;
    private static readonly int PrivateReadonlyValTypeField;
    private static readonly string PrivateReadonlyRefTypeField;

    public static SampleBaseClass PublicBaseClassField;

    public static int PublicValTypeProperty { get; set; }
    public static int PublicGetonlyValTypeProperty { get; }
    public static int PublicSetonlyValTypeProperty { set => PrivateValTypeField = value; }

    public static string PublicRefTypeProperty { get; set; }
    public static string PublicGetonlyRefTypeProperty { get; }
    public static string PublicSetonlyRefTypeProperty { set => PrivateRefTypeField = value; }

    public static void TestMethod()
    {
        throw new NotImplementedException();
    }
    public static void TestEmptyMethod()
    {

    }
    public static int TestMethodWithReturnValue()
    {
        return 3;
    }

    public static void TestMethodWithRefVTParameter(int value, ref int refValue)
    {
        refValue = value;
    }
    public static void TestMethodWithRefRTParameter(string value, ref string refValue)
    {
        refValue = value;
    }
    public static void TestMethodWithOutVTParameter(int value, out int outValue)
    {
        outValue = value;
    }
    public static void TestMethodWithOutRTParameter(string value, out string outValue)
    {
        outValue = value;
    }
    public static void TestMethodWithInVTParameter(int value, in int outValue)
    {
    }
    public static void TestMethodWithInRTParameter(string value, in string outValue)
    {
    }
}