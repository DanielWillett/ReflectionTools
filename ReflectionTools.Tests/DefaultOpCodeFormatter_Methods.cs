using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_Methods
{
    private static void TestMethod1() { }

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

    private static void TestMethod3(Version p1) { }

    [TestMethod]
    public void Write1ParameterStaticMethod()
    {
        MethodInfo? method = Accessor.GetMethod(TestMethod3);
        Assert.IsNotNull(method);

        const string expectedResult = "static void DefaultOpCodeFormatter_Methods.TestMethod3(Version p1)";

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

    private static readonly unsafe int**[][,,][,][] ValTest = null;
    private static unsafe ref readonly int**[][,,][,][] TestMethod4(Version p1, string s2, in SpinLock inParam, out SpinLock outParam, ref SpinLock refParam,
        object[][] jaggedArray, object[][,,][,][] jaggedArray2, object[,] dimArray, ref string[,][][,,,,][] refDimArray,
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

        const string expectedResult = "static ref readonly int**[][,,][,][] TestMethod4(Version p1, string s2, in SpinLock inParam, out SpinLock outParam, ref SpinLock refParam, object[][] jaggedArray, object[][,,][,][] jaggedArray2, object[,] dimArray, ref string[,][][,,,,][] refDimArray, ref string[,]**[][,,,,][]* refDimPtrArray, int**[][,,][,] ptrDimArray, params string[] formattingArgs";

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
}