using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;
using System.Runtime.CompilerServices;
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local

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
        MethodDefinition method = new MethodDefinition("TestMethod1")
                                    .ReturningVoid()
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
        MethodDefinition method = new MethodDefinition("TestMethod2")
            .ReturningVoid()
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
        MethodDefinition method = new MethodDefinition("TestGenericMethod1")
            .ReturningVoid()
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: false)
            .WithGenericParameterDefinition("TParam1")
            .WithParameterUsingGeneric("TParam1", "param1")
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
        MethodDefinition method = new MethodDefinition("TestGenericMethod1")
            .ReturningVoid()
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
        MethodDefinition method = new MethodDefinition("TestGenericMethod2")
            .ReturningVoid()
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: false)
            .WithGenericParameterValue(typeof(Version**[][,,][,]*[,,,][,][]**[]))
            .WithGenericParameterValue(typeof(int**[,,,][,]*[]**[]))
            .WithParameter(typeof(Version**[][,,][,]*[,,,][,][]**[]), "param1")
            .WithParameter(typeof(int**[,,,][,]*[]**[]), "param2");

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
        MethodDefinition method = new MethodDefinition("TestGenericMethod2")
            .ReturningVoid()
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: false)
            .WithGenericParameterDefinition("TParam1")
            .WithGenericParameterDefinition("TParam2")
            .WithParameterUsingGeneric("TParam1", "param1", byRefMode: ByRefTypeMode.ScopedIn)
                .Array()
                .ByRefType()
                .CompleteGenericParameter()
            .WithParameterUsingGeneric(1, "param2", byRefMode: ByRefTypeMode.Ref)
                .Pointer()
                .Pointer()
                .Array()
                .Array(4)
                .Array(2)
                .Pointer()
                .ByRefType()
                .CompleteGenericParameter()
            .WithParameter<int>("num3")
            .WithParameterUsingGeneric(0, "paramsParam", isParams: true)
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
        MethodDefinition method = new MethodDefinition("TestMethod3")
            .ReturningVoid()
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
        MethodDefinition method = new MethodDefinition("TestMethod4")
            .Returning<int**[][,,][,][]>()
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: true)
            .WithReturnRefMode(ByRefTypeMode.RefReadOnly)
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
        MethodDefinition method = new MethodDefinition("TestMethod5")
            .Returning<ArraySegment<ArraySegment<int>>>()
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
        MethodDefinition method = new MethodDefinition("Ext1")
            .Returning<string>()
            .DeclaredIn(typeof(Extensions), true)
            .WithParameter<SpinLock>("spinlock", ByRefTypeMode.ScopedIn)
            .WithReturnRefMode(ByRefTypeMode.RefReadOnly)
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

    [TestMethod]
    public void WriteGenericReturnMethod()
    {
        MethodDefinition method = new MethodDefinition("TestMethod7")
            .WithGenericParameterDefinition("TParam1")
            .ReturningUsingGeneric(0, ByRefTypeMode.RefReadOnly)
                .Array(2)
                .Array()
                .Array(4)
                .Pointer()
                .ByRefType()
                .CompleteReturnType()
            .DeclaredIn<DefaultOpCodeFormatter_MethodDefinition>(isStatic: true)
            .WithNoParameters();

        Assert.IsNotNull(method);

        const string expectedResult = "static ref readonly TParam1[,][][,,,]* DefaultOpCodeFormatter_MethodDefinition.TestMethod7<TParam1>()";

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
    // ReSharper disable once TypeParameterCanBeVariant
    public unsafe delegate ref readonly TGenType1**[][,,,][,,] TestDelegate3<TGenType1, TGenType2>(TGenType2[]**[,,][,] param1, out TGenType1[,]*[,,,,][][,] outParam, params TGenType2[] paramsArray);

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

    [TestMethod]
    public void WriteFromDelegate2WithType()
    {
        MethodDefinition method = MethodDefinition.FromDelegate(typeof(TestDelegate2<int>), "MethodName");

        Assert.IsNotNull(method);

        const string expectedResult = "int* MethodName<int>(int[]**[,,][,] param1)";

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
    public void WriteFromDelegate3()
    {
        MethodDefinition method = MethodDefinition.FromDelegate(typeof(TestDelegate3<,>), "MethodName");

        Assert.IsNotNull(method);

        const string expectedResult = "ref readonly TGenType1**[][,,,][,,] MethodName<TGenType1, TGenType2>(TGenType2[]**[,,][,] param1, out TGenType1[,]*[,,,,][][,] outParam, params TGenType2[] paramsArray)";

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

    private void MethodName(int param1) { }
    private unsafe TGenType* MethodName2<TGenType>(TGenType[]**[,,][,] param1) => default;
#if NET461_OR_GREATER || !NETFRAMEWORK
    private unsafe ref readonly TGenType1**[][,,,][,,] MethodName3<TGenType1, TGenType2>(TGenType2[]**[,,][,] param1, out TGenType1[,]*[,,,,][][,] outParam, params TGenType2[] paramsArray)
    {
        outParam = null;
        ref TGenType1**[][,,,][,,] v = ref Unsafe.AsRef<TGenType1**[][,,,][,,]>(null);
        return ref v;
    }
#endif

    [TestMethod]
    public void WriteFromMethod()
    {
        MethodDefinition method = MethodDefinition.FromMethod(new Action<int>(MethodName).Method);

        Assert.IsNotNull(method);

        const string expectedResult = "void DefaultOpCodeFormatter_MethodDefinition.MethodName(int param1)";

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
    public void WriteFromMethod2()
    {
        MethodDefinition method = MethodDefinition.FromMethod(GetType().GetMethod("MethodName2", BindingFlags.NonPublic | BindingFlags.Instance)!);

        Assert.IsNotNull(method);

        const string expectedResult = "TGenType* DefaultOpCodeFormatter_MethodDefinition.MethodName2<TGenType>(TGenType[]**[,,][,] param1)";

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
    public void WriteFromCtor()
    {
        MethodDefinition method = MethodDefinition.FromMethod(typeof(Exception).GetConstructor([ typeof(string), typeof(Exception) ])!);

        Assert.IsNotNull(method);

        const string expectedResult = "Exception(string message, Exception innerException)";

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
    public void WriteFromGenericCtor()
    {
        MethodDefinition method = MethodDefinition.FromMethod(typeof(ArraySegment<>).GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 3)!);

        Assert.IsNotNull(method);

        const string expectedResult = "ArraySegment<T>(T[] array, int offset, int count)";

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
    public void WriteFromMethod2WithType()
    {
        MethodDefinition method = MethodDefinition.FromMethod(GetType().GetMethod("MethodName2", BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(typeof(int)));

        Assert.IsNotNull(method);

        const string expectedResult = "int* DefaultOpCodeFormatter_MethodDefinition.MethodName2<int>(int[]**[,,][,] param1)";

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

#if NET461_OR_GREATER || !NETFRAMEWORK
    [TestMethod]
    public void WriteFromMethod3()
    {
        MethodDefinition method = MethodDefinition.FromMethod(GetType().GetMethod("MethodName3", BindingFlags.NonPublic | BindingFlags.Instance)!);

        Assert.IsNotNull(method);

        const string expectedResult = "ref readonly TGenType1**[][,,,][,,] DefaultOpCodeFormatter_MethodDefinition.MethodName3<TGenType1, TGenType2>(TGenType2[]**[,,][,] param1, out TGenType1[,]*[,,,,][][,] outParam, params TGenType2[] paramsArray)";

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
#endif
}