namespace DanielWillett.ReflectionTools.Tests.SampleObjects;
public abstract class SampleAbstractClass
{
    public virtual int SampleVirtualPropertyValType { get; set; } = 1;
    public virtual string SampleVirtualPropertyRefType { get; set; } = "base";
    public abstract int SampleAbstractPropertyValType { get; set; }
    public abstract string SampleAbstractPropertyRefType { get; set; }
    public virtual int SampleVirtualMethodValType() => 1;
    public virtual string SampleVirtualMethodRefType() => "base";
    public abstract int SampleAbstractMethodValType();
    public abstract string SampleAbstractMethodRefType();
}
