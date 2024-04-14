using DanielWillett.ReflectionTools.Formatting;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_Types
{
    [DataRow(typeof(int), "int")]
    [DataRow(typeof(IComparable<>), "IComparable<T>")]
    [DataRow(typeof(IComparable<IComparable<IComparable<string>>>), "IComparable<IComparable<IComparable<string>>>")]
    [TestMethod]
    public void TestFormatTypeBasic(Type type, string expectedResult)
    {
        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        Assert.AreEqual(expectedResult, formatter.Format(type));
    }
    [DataRow(typeof(int), "int")]
    [DataRow(typeof(IComparable<>), "public interface IComparable<T>")]
    [DataRow(typeof(IComparable<IComparable<IComparable<string>>>), "public interface IComparable<IComparable<IComparable<string>>>")]
    [DataRow(typeof(Type), "public abstract class Type")]
    [DataRow(typeof(TaskExtensions), "public static class TaskExtensions")]
    [DataRow(typeof(StringComparison), "public enum StringComparison")]
    [DataRow(typeof(Action<object>), "public delegate void Action<object>")]
    [DataRow(typeof(Func<object, Action<Task>>), "public delegate Action<Task> Func<object, Action<Task>>")]
#if NET || NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    [DataRow(typeof(ReadOnlySpan<char>), "public readonly ref struct ReadOnlySpan<char>")]
    [DataRow(typeof(ArraySegment<char>), "public readonly struct ArraySegment<char>")]
    [DataRow(typeof(TestRefStruct), "private ref struct DefaultOpCodeFormatter_Types.TestRefStruct")]
#endif
    [TestMethod]
    public void TestFormatTypeDefinitionKeywords(Type type, string expectedResult)
    {
        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        Assert.AreEqual(expectedResult, formatter.Format(type, includeDefinitionKeywords: true));
    }
#if NET || NETCOREAPP || NETSTANDARD2_1_OR_GREATER
    private ref struct TestRefStruct;
#endif
}
