using DanielWillett.ReflectionTools.Formatting;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_Parameters
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

    public unsafe void Method(
        int value1,
        scoped in SpinLock*[,]******[,,,,,][][,][][,,,][][][,,][]* sl,
        Version v1,
        int[] arr,
        out int outParam,
        params string[] paramsArray)
    {
        outParam = 0;
    }

    [TestMethod]
    [DataRow(0, "int value1")]
    [DataRow(1, "scoped in SpinLock*[,]******[,,,,,][][,][][,,,][][][,,][]* sl")]
    [DataRow(2, "Version v1")]
    [DataRow(3, "int[] arr")]
    [DataRow(4, "out int outParam")]
    [DataRow(5, "params string[] paramsArray")]
    public unsafe void WriteParameter(int paramIndex, string expectedResult)
    {
        MethodInfo? method = Accessor.GetMethod(Method);
        Assert.IsNotNull(method);
        ParameterInfo[] parameters = method.GetParameters();
        Assert.IsTrue(paramIndex < parameters.Length);
        ParameterInfo parameter = parameters[paramIndex];

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(parameter);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(parameter);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(parameter, span)];
        string separateFormat = new string(span);
        
        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow(0, "this int value1")]
    [DataRow(1, "this scoped in SpinLock*[,]******[,,,,,][][,][][,,,][][][,,][]* sl")]
    [DataRow(2, "this Version v1")]
    [DataRow(3, "this int[] arr")]
    [DataRow(4, "this out int outParam")]
    [DataRow(5, "this params string[] paramsArray")]
    public unsafe void WriteExtParameter(int paramIndex, string expectedResult)
    {
        MethodInfo? method = Accessor.GetMethod(Method);
        Assert.IsNotNull(method);
        ParameterInfo[] parameters = method.GetParameters();
        Assert.IsTrue(paramIndex < parameters.Length);
        ParameterInfo parameter = parameters[paramIndex];

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(parameter, isExtensionThisParameter: true);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(parameter, isExtensionThisParameter: true);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(parameter, span, isExtensionThisParameter: true)];
        string separateFormat = new string(span);
        
        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow(0, "int value1")]
    [DataRow(1, "scoped in System.Threading.SpinLock*[,]******[,,,,,][][,][][,,,][][][,,][]* sl")]
    [DataRow(2, "System.Version v1")]
    [DataRow(3, "int[] arr")]
    [DataRow(4, "out int outParam")]
    [DataRow(5, "params string[] paramsArray")]
    public unsafe void WriteParameterNamespaces(int paramIndex, string expectedResult)
    {
        MethodInfo? method = Accessor.GetMethod(Method);
        Assert.IsNotNull(method);
        ParameterInfo[] parameters = method.GetParameters();
        Assert.IsTrue(paramIndex < parameters.Length);
        ParameterInfo parameter = parameters[paramIndex];

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter
        {
            UseFullTypeNames = true
        };

        string format = formatter.Format(parameter);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(parameter);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(parameter, span)];
        string separateFormat = new string(span);
        
        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
}