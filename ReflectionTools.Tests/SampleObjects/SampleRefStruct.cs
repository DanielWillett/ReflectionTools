namespace DanielWillett.ReflectionTools.Tests.SampleObjects;
public unsafe ref struct SampleRefStruct
{
    public int* StackValue { get; set; }
    public SampleRefStruct(int* stackValue)
    {
        StackValue = stackValue;
    }
}
