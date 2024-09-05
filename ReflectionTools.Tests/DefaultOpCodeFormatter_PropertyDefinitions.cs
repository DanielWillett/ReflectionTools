using DanielWillett.ReflectionTools.Formatting;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_PropertyDefinitions
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

    [TestMethod]
    public void WriteNormalProperty()
    {
        PropertyDefinition property = new PropertyDefinition("Property1")
            .WithPropertyType<int>()
            .DeclaredIn<DefaultOpCodeFormatter_PropertyDefinitions>(true);

        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "static int DefaultOpCodeFormatter_PropertyDefinitions.Property1 { get; set; }";

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
    public void WriteNormalPropertyNoAccessors()
    {
        PropertyDefinition property = new PropertyDefinition("Property1")
            .WithPropertyType<int>()
            .DeclaredIn<DefaultOpCodeFormatter_PropertyDefinitions>(true);

        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "static int DefaultOpCodeFormatter_PropertyDefinitions.Property1";

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
    public void WriteNormalNoNameProperty()
    {
        PropertyDefinition property = new PropertyDefinition(null)
            .WithPropertyType<int>()
            .DeclaredIn<DefaultOpCodeFormatter_PropertyDefinitions>(true);

        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "static int DefaultOpCodeFormatter_PropertyDefinitions { get; set; }";

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
    public void WriteNormalNoDeclProperty()
    {
        PropertyDefinition property = new PropertyDefinition("Property1")
            .WithPropertyType<int>();

        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "int Property1 { get; set; }";

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
    public void WriteNormalNoRtnTypeProperty()
    {
        PropertyDefinition property = new PropertyDefinition("Property1")
            .DeclaredIn<DefaultOpCodeFormatter_PropertyDefinitions>(true);

        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "static DefaultOpCodeFormatter_PropertyDefinitions.Property1 { get; set; }";

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
    public void WriteNormalNoNothingProperty()
    {
        PropertyDefinition property = new PropertyDefinition(null);

        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "{ get; set; }";

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
    public void WriteNormalOnlyStaticProperty()
    {
        PropertyDefinition property = new PropertyDefinition(null)
        {
            IsStatic = true
        };

        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "static";

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
    public void WriteNormalOnlyNonStaticProperty()
    {
        PropertyDefinition property = new PropertyDefinition(null);
        Assert.IsNotNull(property);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        const string expectedResult = "";

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

}