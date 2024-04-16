using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_Properties
{
    public ulong Property5 { private protected get; set; }
    public int Property1 { get; set; }
    public static int StaticProperty1 { protected internal get; set; }
    public Version Property2 { get; }
    private string Property6 { get; set; }
    public unsafe string this[DefaultOpCodeFormatter_Properties otherValue, int** val2, ArraySegment<char> arr]
    {
        get => "";
        private set { }
    }

    [TestMethod]
    [DataRow(nameof(Property1), "int DefaultOpCodeFormatter_Properties.Property1 { get; set; }")]
    [DataRow(nameof(Property2), "Version DefaultOpCodeFormatter_Properties.Property2 { get; }")]
    [DataRow(nameof(Property5), "ulong DefaultOpCodeFormatter_Properties.Property5 { private protected get; set; }")]
    [DataRow(nameof(Property6), "string DefaultOpCodeFormatter_Properties.Property6 { private get; private set; }")]
    [DataRow(nameof(StaticProperty1), "static int DefaultOpCodeFormatter_Properties.StaticProperty1 { protected internal get; set; }")]
    [DataRow("Item", "string DefaultOpCodeFormatter_Properties.this[DefaultOpCodeFormatter_Properties otherValue, int** val2, ArraySegment<char> arr] { get; private set; }")]
    public void WriteNormalProperty(string propertyName, string expectedResult)
    {
        PropertyInfo? property = typeof(DefaultOpCodeFormatter_Properties).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(property);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(property);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(property, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow(nameof(Property1), "int DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Properties.Property1 { get; set; }")]
    [DataRow(nameof(Property2), "System.Version DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Properties.Property2 { get; }")]
    [DataRow(nameof(Property5), "ulong DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Properties.Property5 { private protected get; set; }")]
    [DataRow(nameof(Property6), "string DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Properties.Property6 { private get; private set; }")]
    [DataRow(nameof(StaticProperty1), "static int DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Properties.StaticProperty1 { protected internal get; set; }")]
    [DataRow("Item", "string DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Properties.this[DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Properties otherValue, int** val2, System.ArraySegment<char> arr] { get; private set; }")]
    public void WriteNormalPropertyNamespaces(string propertyName, string expectedResult)
    {
        PropertyInfo? property = typeof(DefaultOpCodeFormatter_Properties).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter
        {
            UseFullTypeNames = true
        };

        string format = formatter.Format(property);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(property);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(property, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow(nameof(Property1), "int DefaultOpCodeFormatter_Properties.Property1")]
    [DataRow(nameof(Property2), "Version DefaultOpCodeFormatter_Properties.Property2")]
    [DataRow(nameof(Property5), "ulong DefaultOpCodeFormatter_Properties.Property5")]
    [DataRow(nameof(Property6), "string DefaultOpCodeFormatter_Properties.Property6")]
    [DataRow(nameof(StaticProperty1), "static int DefaultOpCodeFormatter_Properties.StaticProperty1")]
    [DataRow("Item", "string DefaultOpCodeFormatter_Properties.this[DefaultOpCodeFormatter_Properties otherValue, int** val2, ArraySegment<char> arr]")]
    public void WriteNormalPropertyNoAccessors(string propertyName, string expectedResult)
    {
        PropertyInfo? property = typeof(DefaultOpCodeFormatter_Properties).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(property, includeAccessors: false);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(property, includeAccessors: false);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(property, span, includeAccessors: false)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow(nameof(Property1), "public int DefaultOpCodeFormatter_Properties.Property1 { get; set; }")]
    [DataRow(nameof(Property2), "public Version DefaultOpCodeFormatter_Properties.Property2 { get; }")]
    [DataRow(nameof(Property5), "public ulong DefaultOpCodeFormatter_Properties.Property5 { private protected get; set; }")]
    [DataRow(nameof(Property6), "private string DefaultOpCodeFormatter_Properties.Property6 { get; set; }")]
    [DataRow(nameof(StaticProperty1), "public static int DefaultOpCodeFormatter_Properties.StaticProperty1 { protected internal get; set; }")]
    [DataRow("Item", "public string DefaultOpCodeFormatter_Properties.this[DefaultOpCodeFormatter_Properties otherValue, int** val2, ArraySegment<char> arr] { get; private set; }")]
    public void WriteDeclarativeProperty(string propertyName, string expectedResult)
    {
        PropertyInfo? property = typeof(DefaultOpCodeFormatter_Properties).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(property, includeDefinitionKeywords: true);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(property, includeDefinitionKeywords: true);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(property, span, includeDefinitionKeywords: true)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
}