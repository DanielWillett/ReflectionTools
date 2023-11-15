// ReSharper disable UnassignedReadonlyField
// ReSharper disable UnassignedGetOnlyAutoProperty
// ReSharper disable UnusedAutoPropertyAccessor.Local

using System.Globalization;

#pragma warning disable CS0169
#nullable disable

namespace DanielWillett.ReflectionTools.Tests.SampleObjects;
public struct SampleStruct
{
    private int _privateValTypeField;
    private readonly int _privateReadonlyValTypeField;

    private string _privateRefTypeField;
    private readonly string _privateReadonlyRefTypeField;

    public string PublicRefTypeField;
    public readonly string PublicReadonlyRefTypeField;

    public int PublicValTypeField;
    public readonly int PublicReadonlyValTypeField;

    public SampleStruct()
    {
        
    }
    public SampleStruct(int publicReadonlyValType, string publicReadonlyRefType)
    {
        PublicReadonlyRefTypeField = publicReadonlyRefType;
        PublicReadonlyValTypeField = publicReadonlyValType;
        
        PublicReadonlyValTypeProperty = publicReadonlyValType;
        PublicReadonlyRefTypeProperty = publicReadonlyRefType;
    }

    public int PublicValTypeProperty { get; set; }
    public int PublicReadonlyValTypeProperty { get; }

    public string PublicRefTypeProperty { get; set; }
    public string PublicReadonlyRefTypeProperty { get; }

    private int PrivateValTypeProperty { get; set; }
    private int PrivateReadonlyValTypeProperty { get; }

    private string PrivateRefTypeProperty { get; set; }
    private string PrivateReadonlyRefTypeProperty { get; }

    public readonly void NotImplementedNoParams()
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
}

// ReSharper restore UnassignedReadonlyField
// ReSharper restore UnassignedGetOnlyAutoProperty
// ReSharper restore UnusedAutoPropertyAccessor.Local
#pragma warning restore CS0169
#nullable restore