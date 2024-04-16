using DanielWillett.ReflectionTools.Formatting;

namespace DanielWillett.ReflectionTools.Tests;

// ReSharper disable UnusedTypeParameter
[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_Types
{
    [DataRow(typeof(int), "int")]
    [DataRow(typeof(IComparable<>), "IComparable<T>")]
    [DataRow(typeof(IComparable<IComparable<IComparable<string>>>), "IComparable<IComparable<IComparable<string>>>")]
    [DataRow(typeof(NestedClass.DblNestedClass), "DefaultOpCodeFormatter_Types.NestedClass.DblNestedClass")]
    [DataRow(typeof(void*), "void*")]
    [DataRow(typeof(int*), "int*")]
    [DataRow(typeof(SpinLock*), "SpinLock*")]
    [DataRow(typeof(int[]), "int[]")]
    [DataRow(typeof(SpinLock[]), "SpinLock[]")]
    [DataRow(typeof(SpinLock*[]), "SpinLock*[]")]
    [DataRow(typeof(SpinLock**[]), "SpinLock**[]")]
    [DataRow(typeof(int*[]), "int*[]")]
    [DataRow(typeof(int**[]), "int**[]")]
    [DataRow(typeof(NestedClass.DblNestedClass.TplNestedType[]), "DefaultOpCodeFormatter_Types.NestedClass.DblNestedClass.TplNestedType[]")]
    [DataRow(typeof(GenericNested1<string>.GenericNested2<int>.NonGenericNested[]), "DefaultOpCodeFormatter_Types.GenericNested1<T>.GenericNested2<T, T2>.NonGenericNested<string, int>[]")]
#if NET || NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DataRow(typeof(ReadOnlySpan<char>), "ReadOnlySpan<char>")]
    [DataRow(typeof(ArraySegment<char>), "ArraySegment<char>")]
    [DataRow(typeof(TestRefStruct), "DefaultOpCodeFormatter_Types.TestRefStruct")]
    [DataRow(typeof(TestRefStruct*), "DefaultOpCodeFormatter_Types.TestRefStruct*")]
#endif
    [TestMethod]
    public void TestFormatTypeBasic(Type type, string expectedResult)
    {
        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string fullFormat = formatter.Format(type);

        Assert.AreEqual(expectedResult, fullFormat);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(type);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(type, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [DataRow(typeof(int), "int")]
    [DataRow(typeof(IComparable<>), "System.IComparable<T>")]
    [DataRow(typeof(IComparable<IComparable<IComparable<string>>>), "System.IComparable<System.IComparable<System.IComparable<string>>>")]
    [DataRow(typeof(NestedClass.DblNestedClass), "DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.NestedClass.DblNestedClass")]
    [DataRow(typeof(void*), "void*")]
    [DataRow(typeof(int*), "int*")]
    [DataRow(typeof(SpinLock*), "System.Threading.SpinLock*")]
    [DataRow(typeof(int[]), "int[]")]
    [DataRow(typeof(SpinLock[]), "System.Threading.SpinLock[]")]
    [DataRow(typeof(SpinLock*[]), "System.Threading.SpinLock*[]")]
    [DataRow(typeof(SpinLock**[]), "System.Threading.SpinLock**[]")]
    [DataRow(typeof(int*[]), "int*[]")]
    [DataRow(typeof(int**[]), "int**[]")]
    [DataRow(typeof(NestedClass.DblNestedClass.TplNestedType[]), "DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.NestedClass.DblNestedClass.TplNestedType[]")]
    [DataRow(typeof(GenericNested1<string>.GenericNested2<int>.NonGenericNested[]), "DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.GenericNested1<T>.GenericNested2<T, T2>.NonGenericNested<string, int>[]")]
#if NET || NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DataRow(typeof(ReadOnlySpan<char>), "System.ReadOnlySpan<char>")]
    [DataRow(typeof(ArraySegment<char>), "System.ArraySegment<char>")]
    [DataRow(typeof(TestRefStruct), "DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestRefStruct")]
    [DataRow(typeof(TestRefStruct*), "DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestRefStruct*")]
#endif
    [TestMethod]
    public void TestFormatTypeNamespaces(Type type, string expectedResult)
    {
        IOpCodeFormatter formatter = new DefaultOpCodeFormatter
        {
            UseFullTypeNames = true
        };

        string fullFormat = formatter.Format(type);

        Assert.AreEqual(expectedResult, fullFormat);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(type);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(type, span)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [DataRow(typeof(int), "int")]
    [DataRow(typeof(int*), "int*")]
    [DataRow(typeof(int**), "int**")]
    [DataRow(typeof(Version), "Version")]
    [TestMethod]
    public void TestFormatTypeRef(Type type, string expectedResult)
    {
        type = type.MakeByRefType();

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        for (ByRefTypeMode mode = ByRefTypeMode.RefReadonly; mode <= ByRefTypeMode.Out; ++mode)
        {
            string refExpectedResult = (mode == ByRefTypeMode.RefReadonly ? "ref readonly" : mode.ToString().ToLower()) + " " + expectedResult;
            string fullFormat = formatter.Format(type, refMode: mode);

            Assert.AreEqual(refExpectedResult, fullFormat);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
            int formatLength = formatter.GetFormatLength(type, refMode: mode);
            // ReSharper disable once StackAllocInsideLoop
            Span<char> span = stackalloc char[formatLength];
            span = span[..formatter.Format(type, span, refMode: mode)];
            string separateFormat = new string(span);

            Assert.AreEqual(refExpectedResult, separateFormat);
            Assert.AreEqual(formatLength, separateFormat.Length);
#endif
        }
    }

    [DataRow(typeof(int), "int")]
    [DataRow(typeof(IComparable<>), "public interface IComparable<T>")]
    [DataRow(typeof(IComparable<IComparable<IComparable<string>>>), "public interface IComparable<IComparable<IComparable<string>>>")]
    [DataRow(typeof(Type), "public abstract class Type")]
    [DataRow(typeof(TaskExtensions), "public static class TaskExtensions")]
    [DataRow(typeof(StringComparison), "public enum StringComparison")]
    [DataRow(typeof(Action<object>), "public delegate void Action<object>")]
    [DataRow(typeof(Func<object, Action<Task>>), "public delegate Action<Task> Func<object, Action<Task>>")]
    [DataRow(typeof(NestedClass.DblNestedClass), "public static class DefaultOpCodeFormatter_Types.NestedClass.DblNestedClass")]
    [DataRow(typeof(NestedClass.DblNestedClass.TplNestedType[]), "public class DefaultOpCodeFormatter_Types.NestedClass.DblNestedClass.TplNestedType[]")]
    [DataRow(typeof(GenericNested1<string>.GenericNested2<int>.NonGenericNested[]), "public class DefaultOpCodeFormatter_Types.GenericNested1<T>.GenericNested2<T, T2>.NonGenericNested<string, int>[]")]
    [DataRow(typeof(void*), "void*")]
    [DataRow(typeof(int*), "int*")]
    [DataRow(typeof(int**), "int**")]
    [DataRow(typeof(TestDelegate3), "public delegate ref int* DefaultOpCodeFormatter_Types.TestDelegate3")]
    [DataRow(typeof(TestDelegate1), "public delegate void DefaultOpCodeFormatter_Types.TestDelegate1")]
    [DataRow(typeof(TestDelegate0), "public delegate int* TestDelegate0")]
#if NET || NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DataRow(typeof(ReadOnlySpan<char>), "public readonly ref struct ReadOnlySpan<char>")]
    [DataRow(typeof(ArraySegment<char>), "public readonly struct ArraySegment<char>")]
    [DataRow(typeof(TestRefStruct), "private ref struct DefaultOpCodeFormatter_Types.TestRefStruct")]
    [DataRow(typeof(TestRefStruct*), "private ref struct DefaultOpCodeFormatter_Types.TestRefStruct*")]
    [DataRow(typeof(TestRefStruct**), "private ref struct DefaultOpCodeFormatter_Types.TestRefStruct**")]
    [DataRow(typeof(TestDelegate2), "private delegate ref DefaultOpCodeFormatter_Types.TestRefStruct DefaultOpCodeFormatter_Types.TestDelegate2")]
#endif
    [TestMethod]
    public void TestFormatTypeDefinitionKeywords(Type type, string expectedResult)
    {
        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(type, includeDefinitionKeywords: true);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(type, includeDefinitionKeywords: true);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(type, span, includeDefinitionKeywords: true)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [DataRow(typeof(int), "int")]
    [DataRow(typeof(IComparable<>), "public interface System.IComparable<T>")]
    [DataRow(typeof(IComparable<IComparable<IComparable<string>>>), "public interface System.IComparable<System.IComparable<System.IComparable<string>>>")]
    [DataRow(typeof(Type), "public abstract class System.Type")]
    [DataRow(typeof(TaskExtensions), "public static class System.Threading.Tasks.TaskExtensions")]
    [DataRow(typeof(StringComparison), "public enum System.StringComparison")]
    [DataRow(typeof(Action<object>), "public delegate void System.Action<object>")]
    [DataRow(typeof(Func<object, Action<Task>>), "public delegate System.Action<System.Threading.Tasks.Task> System.Func<object, System.Action<System.Threading.Tasks.Task>>")]
    [DataRow(typeof(NestedClass.DblNestedClass), "public static class DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.NestedClass.DblNestedClass")]
    [DataRow(typeof(NestedClass.DblNestedClass.TplNestedType[]), "public class DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.NestedClass.DblNestedClass.TplNestedType[]")]
    [DataRow(typeof(GenericNested1<string>.GenericNested2<int>.NonGenericNested[]), "public class DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.GenericNested1<T>.GenericNested2<T, T2>.NonGenericNested<string, int>[]")]
    [DataRow(typeof(void*), "void*")]
    [DataRow(typeof(int*), "int*")]
    [DataRow(typeof(int**), "int**")]
    [DataRow(typeof(TestDelegate3), "public delegate ref int* DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestDelegate3")]
    [DataRow(typeof(TestDelegate1), "public delegate void DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestDelegate1")]
    [DataRow(typeof(TestDelegate0), "public delegate int* DanielWillett.ReflectionTools.Tests.TestDelegate0")]
#if NET || NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DataRow(typeof(ReadOnlySpan<char>), "public readonly ref struct System.ReadOnlySpan<char>")]
    [DataRow(typeof(ArraySegment<char>), "public readonly struct System.ArraySegment<char>")]
    [DataRow(typeof(TestRefStruct), "private ref struct DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestRefStruct")]
    [DataRow(typeof(TestRefStruct*), "private ref struct DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestRefStruct*")]
    [DataRow(typeof(TestRefStruct**), "private ref struct DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestRefStruct**")]
    [DataRow(typeof(TestDelegate2), "private delegate ref DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestRefStruct DanielWillett.ReflectionTools.Tests.DefaultOpCodeFormatter_Types.TestDelegate2")]
#endif
    [TestMethod]
    public void TestFormatTypeDefinitionKeywordsNamespaces(Type type, string expectedResult)
    {
        IOpCodeFormatter formatter = new DefaultOpCodeFormatter
        {
            UseFullTypeNames = true
        };

        string format = formatter.Format(type, includeDefinitionKeywords: true);

        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(type, includeDefinitionKeywords: true);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(type, span, includeDefinitionKeywords: true)];
        string separateFormat = new string(span);

        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

#if NET || NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    private ref struct TestRefStruct;
    private delegate ref readonly TestRefStruct TestDelegate2(int v);
#endif

    public class NestedClass
    {
        public static class DblNestedClass
        {
            public class TplNestedType;
        }
    }

    public class GenericNested1<T>
    {
        public class GenericNested2<T2>
        {
            public class NonGenericNested;
        }
    }

    public delegate void TestDelegate1(int v);
    public unsafe delegate ref int* TestDelegate3(int v);
}

public unsafe delegate int* TestDelegate0(int v);