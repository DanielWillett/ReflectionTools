using DanielWillett.ReflectionTools.Formatting;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_FieldDefinition
{
    [TestMethod]
    public void WriteNormalField()
    {
        FieldDefinition field = new FieldDefinition("Field1")
            .DeclaredIn<DefaultOpCodeFormatter_FieldDefinition>(true)
            .WithFieldType(typeof(SpinLock**[,,,][,,][]).MakeByRefType())
            .AsReadOnly();

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "static ref SpinLock**[,,,][,,][] DefaultOpCodeFormatter_FieldDefinition.Field1";

        string format = formatter.Format(field);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(field);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(field, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
    [TestMethod]
    public void WriteNormalFieldConst()
    {
        FieldDefinition field = new FieldDefinition("ConstField")
            .DeclaredIn<DefaultOpCodeFormatter_FieldDefinition>(true)
            .WithFieldType<string>()
            .AsConstant();

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "const string DefaultOpCodeFormatter_FieldDefinition.ConstField";

        string format = formatter.Format(field);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(field);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(field, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
    [TestMethod]
    public void WriteNormalFieldNoDeclType()
    {
        FieldDefinition field = new FieldDefinition("Field1")
            .WithFieldType(typeof(SpinLock**[,,,][,,][]).MakeByRefType())
            .AsReadOnly();

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "ref SpinLock**[,,,][,,][] Field1";

        string format = formatter.Format(field);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(field);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(field, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
    [TestMethod]
    public void WriteNormalFieldNoNameType()
    {
        FieldDefinition field = new FieldDefinition(null)
            .DeclaredIn<DefaultOpCodeFormatter_FieldDefinition>(false)
            .WithFieldType(typeof(SpinLock**[,,,][,,][]))
            .AsReadOnly();

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "SpinLock**[,,,][,,][] DefaultOpCodeFormatter_FieldDefinition";

        string format = formatter.Format(field);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(field);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(field, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
    [TestMethod]
    public void WriteNormalFieldNoNameOrDeclTypeType()
    {
        FieldDefinition field = new FieldDefinition(null)
            .WithFieldType(typeof(Version))
            .AsReadOnly();

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "Version";

        string format = formatter.Format(field);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(field);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(field, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
    [TestMethod]
    public void WriteNormalFieldNoFieldType()
    {
        FieldDefinition field = new FieldDefinition("Field1")
            .DeclaredIn<DefaultOpCodeFormatter_FieldDefinition>(true)
            .AsReadOnly();

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "static DefaultOpCodeFormatter_FieldDefinition.Field1";

        string format = formatter.Format(field);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(field);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(field, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
    [TestMethod]
    public void WriteNormalFieldOnlyStatic()
    {
        FieldDefinition field = new FieldDefinition(null)
        {
            IsStatic = true
        };
        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "static";

        string format = formatter.Format(field);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(field);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(field, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
    [TestMethod]
    public void WriteNormalFieldNone()
    {
        FieldDefinition field = new FieldDefinition(null);
        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "";

        string format = formatter.Format(field);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(field);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(field, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
}