using DanielWillett.ReflectionTools.Formatting;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_EventDefinition
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

    public event Action? Event1;
    public static event Action? Event2;
    public event Action? Event3 { add { } remove { } }
    protected event Action? Event4;
    private protected static event Func<int, string>? Event5;
    internal event Func<int, Version, string>? Event6 { add { } remove { } }

    public event GenericDelegate<int>? Event7;

    [TestMethod]
    public void WriteNormalEvent()
    {
        EventDefinition property = new EventDefinition("Event1")
            .DeclaredIn<DefaultOpCodeFormatter_EventDefinition>(false)
            .WithHandlerType<Action>();

        const string expectedResult = "event Action DefaultOpCodeFormatter_EventDefinition.Event1 { add; remove; }";

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
    public void WriteNormalEventNoEventKeyword()
    {
        EventDefinition property = new EventDefinition("Event1")
            .DeclaredIn<DefaultOpCodeFormatter_EventDefinition>(false)
            .WithHandlerType<Action>();

        const string expectedResult = "Action DefaultOpCodeFormatter_EventDefinition.Event1 { add; remove; }";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(property, includeEventKeyword: false);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(property, includeEventKeyword: false);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(property, span, includeEventKeyword: false)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    public void WriteNormalEventNoAccessors()
    {
        EventDefinition property = new EventDefinition("Event1")
            .DeclaredIn<DefaultOpCodeFormatter_EventDefinition>(false)
            .WithHandlerType<Action>();

        const string expectedResult = "event Action DefaultOpCodeFormatter_EventDefinition.Event1";

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

    public delegate T GenericDelegate<T>(T val);
}