using DanielWillett.ReflectionTools.Formatting;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_MethodDefinition
{

    [TestMethod]
    public void WriteCtorMethod()
    {
        MethodDefinition method =
            new MethodDefinition(typeof(DefaultOpCodeFormatter_MethodDefinition))
                .WithParameter<int>("integer")
                .WithParameter((Type?)null!, "nullParam")
                .WithParameter((Type?)null!, null)
                .WithParameter(typeof(string[]), null)
                .WithParameter(typeof(string[]), "paramsArray", isParams: true);

        const string expectedResult = "DefaultOpCodeFormatter_MethodDefinition(int integer, nullParam, , string[], params string[] paramsArray)";

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
    public void WriteParameterlessStaticMethod()
    {
        MethodDefinition method = new MethodDefinition(typeof(void), "TestMethod1")
                                    .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: true)
                                    .WithNoParameters();

        Assert.IsNotNull(method);

        const string expectedResult = "static void DefaultOpCodeFormatter_MethodDefinition.TestMethod1()";

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
    [TestMethod]
    public void WriteParameterlessNonStaticMethod()
    {
        MethodDefinition method = new MethodDefinition(typeof(void), "TestMethod2")
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: false)
            .WithNoParameters();

        Assert.IsNotNull(method);

        const string expectedResult = "void DefaultOpCodeFormatter_MethodDefinition.TestMethod2()";

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
    public void WriteGenericMethod1Param()
    {
        MethodDefinition method = new MethodDefinition(typeof(void), "TestGenericMethod1")
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: false)
            .WithGenericParameterDefinition("TParam1")
            .WithParameter(0, "param1")
                .CompleteGenericParameter();

        Assert.IsNotNull(method);

        const string expectedResult = "void DefaultOpCodeFormatter_MethodDefinition.TestGenericMethod1<TParam1>(TParam1 param1)";

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
    public void WriteGenericMethod1ParamWithTypes()
    {
        MethodDefinition method = new MethodDefinition(typeof(void), "TestGenericMethod1")
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: false)
            .WithGenericParameterValue<Version>()
            .WithParameter<Version>("param1");

        Assert.IsNotNull(method);

        const string expectedResult = "void DefaultOpCodeFormatter_MethodDefinition.TestGenericMethod1<Version>(Version param1)";

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
    public void WriteGenericMethod1ParamWithElementTypes()
    {
        MethodDefinition method = new MethodDefinition(typeof(void), "TestGenericMethod2")
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: false)
            .WithGenericParameterValue(typeof(Version**[][,,][,]*[,,,][,][]**[]))
            .WithGenericParameterValue(typeof(int**[,,,][,]*[]**[]))
            .WithParameter(0, "param1")
                .CompleteGenericParameter()
            .WithParameter(1, "param2")
                .CompleteGenericParameter();

        Assert.IsNotNull(method);

        const string expectedResult = "void DefaultOpCodeFormatter_MethodDefinition.TestGenericMethod2<Version**[][,,][,]*[,,,][,][]**[], int**[,,,][,]*[]**[]>(Version**[][,,][,]*[,,,][,][]**[] param1, int**[,,,][,]*[]**[] param2)";

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(method);
        Console.WriteLine(format);

        Assert.AreEqual(expectedResult, format);

        format = formatter.Format(method);
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
    public void WriteGenericMethod2Param()
    {
        MethodDefinition method = new MethodDefinition(typeof(void), "TestGenericMethod2")
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: false)
            .WithGenericParameterDefinition("TParam1")
            .WithGenericParameterDefinition("TParam2")
            .WithParameter(0, "param1", byRefMode: ByRefTypeMode.ScopedIn)
                .Array()
                .ByRefType()
                .CompleteGenericParameter()
            .WithParameter(1, "param2", byRefMode: ByRefTypeMode.Ref)
                .Pointer()
                .Pointer()
                .Array()
                .Array(4)
                .Array(2)
                .Pointer()
                .ByRefType()
                .CompleteGenericParameter()
            .WithParameter<int>("num3")
            .WithParameter(0, "paramsParam", isParams: true)
                .Array()
                .CompleteGenericParameter();

        Assert.IsNotNull(method);

        const string expectedResult = "void DefaultOpCodeFormatter_MethodDefinition.TestGenericMethod2<TParam1, TParam2>(scoped in TParam1[] param1, ref TParam2**[][,,,][,]* param2, int num3, params TParam1[] paramsParam)";

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
    public void Write1ParameterStaticMethod()
    {
        MethodDefinition method = new MethodDefinition(typeof(void), "TestMethod3")
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: true)
            .WithParameter<Version>("p1");

        Assert.IsNotNull(method);

        const string expectedResult = "static void DefaultOpCodeFormatter_MethodDefinition.TestMethod3(Version p1)";

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
    public unsafe void WriteMultiParameterStaticMethod()
    {
        MethodDefinition method = new MethodDefinition(typeof(int**[][,,][,][]), "TestMethod4")
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: true)
            .WithReturnRefMode(ByRefTypeMode.RefReadonly)
            .WithParameter<Version>("p1")
            .WithParameter<string>("s2")
            .WithParameter<SpinLock>("inParam", ByRefTypeMode.In)
            .WithParameter<SpinLock>("outParam", ByRefTypeMode.Out)
            .WithParameter<SpinLock>("refParam", ByRefTypeMode.Ref)
            .WithParameter<object[][]>("jaggedArray")
            .WithParameter<object[][,,][,][]>("jaggedArray2")
            .WithParameter<object[,]>("dimArray")
            .WithParameter<string[,][][,,,,][]>("refDimArray", ByRefTypeMode.ScopedRef)
            .WithParameter(typeof(string[,]**[][,,,,][]*), "refDimPtrArray", ByRefTypeMode.Ref)
            .WithParameter<int**[][,,][,]>("ptrDimArray")
            .WithParameter<string[]>("formattingArgs", isParams: true);

        Assert.IsNotNull(method);

        const string expectedResult = "static ref readonly int**[][,,][,][] DefaultOpCodeFormatter_MethodDefinition.TestMethod4(Version p1, string s2, in SpinLock inParam, out SpinLock outParam, ref SpinLock refParam, object[][] jaggedArray, object[][,,][,][] jaggedArray2, object[,] dimArray, scoped ref string[,][][,,,,][] refDimArray, ref string[,]**[][,,,,][]* refDimPtrArray, int**[][,,][,] ptrDimArray, params string[] formattingArgs)";

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
        MethodDefinition method = new MethodDefinition(typeof(ArraySegment<ArraySegment<int>>), "TestMethod5")
            .DeclaredIn(typeof(TestStruct1), false)
            .WithParameter<ArraySegment<Version>>("arr")
            .WithParameter<string[][,]>("arrays", isParams: true);

        Assert.IsNotNull(method);

        const string expectedResult = "ArraySegment<ArraySegment<int>> DefaultOpCodeFormatter_MethodDefinition.TestStruct1.TestMethod5(ArraySegment<Version> arr, params string[][,] arrays)";

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
    public void WriteExtMethod()
    {
        MethodDefinition method = new MethodDefinition(typeof(string), "Ext1")
            .DeclaredIn(typeof(Extensions), true)
            .WithParameter<SpinLock>("spinlock", ByRefTypeMode.ScopedIn)
            .WithReturnRefMode(ByRefTypeMode.RefReadonly)
            .AsExtensionMethod();

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

    private ref struct TestStruct1;

    public delegate void TestDelegate1(int param1);
    public unsafe delegate TGenType* TestDelegate2<TGenType>(TGenType[]**[,,][,] param1);

    [TestMethod]
    public void WriteFromDelegate()
    {
        MethodDefinition method = MethodDefinition.FromDelegate<TestDelegate1>("MethodName");

        Assert.IsNotNull(method);

        const string expectedResult = "void MethodName(int param1)";

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
    public void WriteFromDelegate2()
    {
        MethodDefinition method = MethodDefinition.FromDelegate(typeof(TestDelegate2<>), "MethodName");

        Assert.IsNotNull(method);

        const string expectedResult = "TGenType* MethodName<TGenType>(TGenType[]**[,,][,] param1)";

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
}