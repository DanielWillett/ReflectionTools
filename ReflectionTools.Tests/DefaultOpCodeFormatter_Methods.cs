using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_Methods
{
    public DefaultOpCodeFormatter_Methods()
    {
        Console.WriteLine("c1");
    }
    static DefaultOpCodeFormatter_Methods()
    {
        Console.WriteLine("sc1");
    }
    public DefaultOpCodeFormatter_Methods(string str1)
    {
        Console.WriteLine("c2");
    }
    private unsafe DefaultOpCodeFormatter_Methods(scoped in Version**[][,,][,] test, SpinLock l)
    {
        Console.WriteLine("c3");
    }
    private static void TestMethod1() { }

    [TestMethod]
    public void WriteStaticCtorMethod()
    {
        ConstructorInfo method = GetType()
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .First();

        const string expectedResult = "static DefaultOpCodeFormatter_Methods()";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow(0, "DefaultOpCodeFormatter_Methods()")]
    [DataRow(1, "DefaultOpCodeFormatter_Methods(string str1)")]
    [DataRow(2, "DefaultOpCodeFormatter_Methods(scoped in Version**[][,,][,] test, SpinLock l)")]
    public void WriteCtorMethod(int paramCt, string expectedResult)
    {
        ConstructorInfo method = GetType()
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.GetParameters().Length == paramCt);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow(0, "public DefaultOpCodeFormatter_Methods()")]
    [DataRow(1, "public DefaultOpCodeFormatter_Methods(string str1)")]
    [DataRow(2, "private DefaultOpCodeFormatter_Methods(scoped in Version**[][,,][,] test, SpinLock l)")]
    public void WriteCtorMethodDeclaritave(int paramCt, string expectedResult)
    {
        ConstructorInfo method = GetType()
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.GetParameters().Length == paramCt);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method, includeDefinitionKeywords: true);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method, includeDefinitionKeywords: true);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span, includeDefinitionKeywords: true)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    public void WriteParameterlessStaticMethod()
    {
        MethodInfo? method = Accessor.GetMethod(TestMethod1);
        Assert.IsNotNull(method);

        const string expectedResult = "static void DefaultOpCodeFormatter_Methods.TestMethod1()";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
    private void TestMethod2() { }

    [TestMethod]
    public void WriteParameterlessNonStaticMethod()
    {
        MethodInfo? method = Accessor.GetMethod(this.TestMethod2);
        Assert.IsNotNull(method);

        const string expectedResult = "void DefaultOpCodeFormatter_Methods.TestMethod2()";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    private static void TestMethod3(Version p1) { }

    [TestMethod]
    public void Write1ParameterStaticMethod()
    {
        MethodInfo? method = Accessor.GetMethod(TestMethod3);
        Assert.IsNotNull(method);

        const string expectedResult = "static void DefaultOpCodeFormatter_Methods.TestMethod3(Version p1)";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    private static readonly unsafe int**[][,,][,][] ValTest = null;
    private static unsafe ref readonly int**[][,,][,][] TestMethod4(Version p1, string s2, in SpinLock inParam, out SpinLock outParam, ref SpinLock refParam,
        object[][] jaggedArray, object[][,,][,][] jaggedArray2, object[,] dimArray, scoped ref string[,][][,,,,][] refDimArray,
        ref string[,]**[][,,,,][]* refDimPtrArray, int**[][,,][,] ptrDimArray,
        params string[] formattingArgs)
    {
        outParam = default;
        return ref ValTest;
    }

    [TestMethod]
    public unsafe void WriteMultiParameterStaticMethod()
    {
        MethodInfo? method = Accessor.GetMethod(TestMethod4);
        Assert.IsNotNull(method);

        const string expectedResult = "static ref readonly int**[][,,][,][] DefaultOpCodeFormatter_Methods.TestMethod4(Version p1, string s2, in SpinLock inParam, out SpinLock outParam, ref SpinLock refParam, object[][] jaggedArray, object[][,,][,][] jaggedArray2, object[,] dimArray, scoped ref string[,][][,,,,][] refDimArray, ref string[,]**[][,,,,][]* refDimPtrArray, int**[][,,][,] ptrDimArray, params string[] formattingArgs)";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    public void WriteInRefStructMethod()
    {
        MethodInfo? method = typeof(TestStruct1).GetMethod(nameof(TestStruct1.TestMethod5), BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        const string expectedResult = "internal ArraySegment<ArraySegment<int>> DefaultOpCodeFormatter_Methods.TestStruct1.TestMethod5(ArraySegment<Version> arr, params string[][,] arrays)";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method, includeDefinitionKeywords: true);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method, includeDefinitionKeywords: true);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span, includeDefinitionKeywords: true)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    public void WriteExtMethod()
    {
        MethodInfo? method = Accessor.GetMethod(Extensions.Ext1);
        Assert.IsNotNull(method);

        const string expectedResult = "static ref readonly string Extensions.Ext1(this scoped in SpinLock spinlock)";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(method);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(method, span)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    private ref struct TestStruct1
    {
        internal ArraySegment<ArraySegment<int>> TestMethod5(ArraySegment<Version> arr, params string[][,] arrays)
        {
            return default;
        }
    }
}
public static class Extensions
{
    private static readonly string Test;
    public static ref readonly string Ext1(this scoped in SpinLock spinlock)
    {
        return ref Test;
    }
}