namespace DanielWillett.ReflectionTools.Tests.SampleObjects;
public class SampleAbstractDerivingClass : SampleAbstractClass
{
    public override string SampleVirtualPropertyRefType { get; set; } = "override";
    public override int SampleVirtualPropertyValType { get; set; } = 10;
    public override string SampleAbstractPropertyRefType { get; set; } = "override";
    public override int SampleAbstractPropertyValType { get; set; } = 10;
    public override int SampleVirtualMethodValType() => 10;
    public override string SampleVirtualMethodRefType() => "override";
    public override int SampleAbstractMethodValType() => 10;
    public override string SampleAbstractMethodRefType() => "override";
}
