using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_Fields
{
    private static readonly int Field1;
    private static readonly Version Field2;
    public unsafe SpinLock** Field3;

    [TestMethod]
    [DataRow(nameof(Field1), "static int DefaultOpCodeFormatter_Fields.Field1")]
    [DataRow(nameof(Field2), "static Version DefaultOpCodeFormatter_Fields.Field2")]
    [DataRow(nameof(Field3), "SpinLock** DefaultOpCodeFormatter_Fields.Field3")]
    public void WriteNormalField(string eventName, string expectedResult)
    {
        FieldInfo? property = typeof(DefaultOpCodeFormatter_Fields).GetField(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
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
    [DataRow(nameof(Field1), "private static readonly int DefaultOpCodeFormatter_Fields.Field1")]
    [DataRow(nameof(Field2), "private static readonly Version DefaultOpCodeFormatter_Fields.Field2")]
    [DataRow(nameof(Field3), "public SpinLock** DefaultOpCodeFormatter_Fields.Field3")]
    public void WriteNormalFieldDeclaritave(string eventName, string expectedResult)
    {
        FieldInfo? property = typeof(DefaultOpCodeFormatter_Fields).GetField(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
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

#if NET7_0_OR_GREATER
    [TestMethod]
    public void WriteRefField()
    {
        const string expectedResult = "ref char DefaultOpCodeFormatter_Fields.TestRefStruct.RefField";

        FieldInfo? property = typeof(TestRefStruct).GetField("RefField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(property);

        Assert.AreEqual(expectedResult, format);

        int formatLength = formatter.GetFormatLength(property);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(property, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
    }
    [TestMethod]
    public void WriteRefFieldDeclaritave()
    {
        const string expectedResult = "public ref char DefaultOpCodeFormatter_Fields.TestRefStruct.RefField";

        FieldInfo? property = typeof(TestRefStruct).GetField("RefField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(property, includeDefinitionKeywords: true);

        Assert.AreEqual(expectedResult, format);

        int formatLength = formatter.GetFormatLength(property, includeDefinitionKeywords: true);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(property, span, includeDefinitionKeywords: true)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
    }
    public ref struct TestRefStruct
    {
        public ref char RefField;
    }
#endif
}