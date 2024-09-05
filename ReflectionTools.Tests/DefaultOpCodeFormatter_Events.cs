using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_Events
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
    [DataRow(nameof(Event1), "event Action DefaultOpCodeFormatter_Events.Event1 { add; remove; }")]
    [DataRow(nameof(Event2), "static event Action DefaultOpCodeFormatter_Events.Event2 { add; remove; }")]
    [DataRow(nameof(Event3), "event Action DefaultOpCodeFormatter_Events.Event3 { add; remove; }")]
    [DataRow(nameof(Event7), "event DefaultOpCodeFormatter_Events.GenericDelegate<int> DefaultOpCodeFormatter_Events.Event7 { add; remove; }")]
    public void WriteNormalEvent(string eventName, string expectedResult)
    {
        EventInfo? property = typeof(DefaultOpCodeFormatter_Events).GetEvent(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
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
    [DataRow(nameof(Event1), "Action DefaultOpCodeFormatter_Events.Event1 { add; remove; }")]
    [DataRow(nameof(Event2), "static Action DefaultOpCodeFormatter_Events.Event2 { add; remove; }")]
    [DataRow(nameof(Event3), "Action DefaultOpCodeFormatter_Events.Event3 { add; remove; }")]
    [DataRow(nameof(Event7), "DefaultOpCodeFormatter_Events.GenericDelegate<int> DefaultOpCodeFormatter_Events.Event7 { add; remove; }")]
    public void WriteNormalEventNoEventKeyword(string eventName, string expectedResult)
    {
        EventInfo? property = typeof(DefaultOpCodeFormatter_Events).GetEvent(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(property);

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
    [DataRow(nameof(Event1), "event System.Action DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Events.Event1 { add; remove; }")]
    [DataRow(nameof(Event2), "static event System.Action DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Events.Event2 { add; remove; }")]
    [DataRow(nameof(Event3), "event System.Action DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Events.Event3 { add; remove; }")]
    [DataRow(nameof(Event7), "event DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Events.GenericDelegate<int> DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Events.Event7 { add; remove; }")]
    public void WriteNormalEventNamespaces(string eventName, string expectedResult)
    {
        EventInfo? property = typeof(DefaultOpCodeFormatter_Events).GetEvent(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
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
    [DataRow(nameof(Event1), "event Action DefaultOpCodeFormatter_Events.Event1")]
    [DataRow(nameof(Event2), "static event Action DefaultOpCodeFormatter_Events.Event2")]
    [DataRow(nameof(Event3), "event Action DefaultOpCodeFormatter_Events.Event3")]
    [DataRow(nameof(Event7), "event DefaultOpCodeFormatter_Events.GenericDelegate<int> DefaultOpCodeFormatter_Events.Event7")]
    public void WriteNormalEventNoAccessors(string eventName, string expectedResult)
    {
        EventInfo? property = typeof(DefaultOpCodeFormatter_Events).GetEvent(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
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
    [DataRow(nameof(Event1), "public event Action DefaultOpCodeFormatter_Events.Event1 { add; remove; }")]
    [DataRow(nameof(Event2), "public static event Action DefaultOpCodeFormatter_Events.Event2 { add; remove; }")]
    [DataRow(nameof(Event3), "public event Action DefaultOpCodeFormatter_Events.Event3 { add; remove; }")]
    [DataRow(nameof(Event4), "protected event Action DefaultOpCodeFormatter_Events.Event4 { add; remove; }")]
    [DataRow(nameof(Event5), "private protected static event Func<int, string> DefaultOpCodeFormatter_Events.Event5 { add; remove; }")]
    [DataRow(nameof(Event6), "internal event Func<int, Version, string> DefaultOpCodeFormatter_Events.Event6 { add; remove; }")]
    [DataRow(nameof(Event7), "public event DefaultOpCodeFormatter_Events.GenericDelegate<int> DefaultOpCodeFormatter_Events.Event7 { add; remove; }")]
    public void WriteDeclarativeEvent(string eventName, string expectedResult)
    {
        EventInfo? property = typeof(DefaultOpCodeFormatter_Events).GetEvent(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
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

    public delegate T GenericDelegate<T>(T val);
}