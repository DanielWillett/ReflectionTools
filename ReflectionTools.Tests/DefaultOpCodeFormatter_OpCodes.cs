using DanielWillett.ReflectionTools.Emit;
using DanielWillett.ReflectionTools.Formatting;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class DefaultOpCodeFormatter_OpCodes
{
    private int _testField;

    [TestMethod]
    [DataRow(0, "0")]
    [DataRow(40, "40")]
    [DataRow(int.MaxValue, "2147483647")]
    public unsafe void WriteLabel(int label, string expectedResult)
    {
        Label lbl = *(Label*)&label;

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(lbl);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(lbl);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(lbl, span)];
        string separateFormat = new string(span);
        
        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("ldarg.1", "ldarg.1")]
    public void WriteOpCode(string opCodeStr, string expectedResult)
    {
        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, span)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("br.s", "br.s lbl.3", OpCodeFormattingContext.InLine)]
    public unsafe void WriteOpCodeWithLabel(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        int lblId = 3;
        Label lbl = *(Label*)&lblId;

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, lbl, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, lbl, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, lbl, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("ldfld", "ldfld int DefaultOpCodeFormatter_OpCodes._testField", OpCodeFormattingContext.InLine)]
    public void WriteOpCodeWithField(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        FieldInfo? field = GetType().GetField("_testField", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field);

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, field, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, field, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, field, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("callvirt", "callvirt void DefaultOpCodeFormatter_OpCodes.WriteOpCodeWithMethod(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)", OpCodeFormattingContext.InLine)]
    public void WriteOpCodeWithMethod(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        MethodInfo? method = Accessor.GetMethod(WriteOpCodeWithMethod);
        Assert.IsNotNull(method);

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, method, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, method, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, method, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("ldc.r8", "ldc.r8 18.102334", OpCodeFormattingContext.InLine)]
    public void WriteOpCodeWithR8(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        const double r8 = 18.102334d;

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, r8, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, r8, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, r8, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("ldc.i8", "ldc.i8 18", OpCodeFormattingContext.InLine)]
    public void WriteOpCodeWithI(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        const long i8 = 18;

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, i8, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, i8, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, i8, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("switch", "switch (lbl.1, lbl.2, lbl.3)", OpCodeFormattingContext.InLine)]
    [DataRow("switch", "switch\r\n{\r\n  0 => lbl.1\r\n  1 => lbl.2\r\n  2 => lbl.3\r\n}", OpCodeFormattingContext.List)]
    public unsafe void WriteOpCodeWithSwitch(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        Label[] lbls = new Label[3];
        fixed (Label* lbl = lbls)
        {
            int l = 1;
            *lbl = *(Label*)&l;
            l = 2;
            lbl[1] = *(Label*)&l;
            l = 3;
            lbl[2] = *(Label*)&l;
        }

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, lbls, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, lbls, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, lbls, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("switch", "switch (lbl.1, lbl.2, lbl.3, lbl.4, lbl.5)", OpCodeFormattingContext.InLine)]
    [DataRow("switch", "switch\r\n{\r\n  0 => lbl.1\r\n  1 => lbl.2\r\n  2 => lbl.3\r\n  3 => lbl.4\r\n  4 => lbl.5\r\n}", OpCodeFormattingContext.List)]
    public unsafe void WriteOpCodeWithSwitch5Elem(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        Label[] lbls = new Label[5];
        fixed (Label* lbl = lbls)
        {
            int l = 1;
            *lbl = *(Label*)&l;
            l = 2;
            lbl[1] = *(Label*)&l;
            l = 3;
            lbl[2] = *(Label*)&l;
            l = 4;
            lbl[3] = *(Label*)&l;
            l = 5;
            lbl[4] = *(Label*)&l;
        }

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, lbls, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, lbls, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, lbls, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("switch", "switch ( )", OpCodeFormattingContext.InLine)]
    [DataRow("switch", "switch { }", OpCodeFormattingContext.List)]
    public unsafe void WriteOpCodeWithSwitch0Elem(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        // ReSharper disable once UseArrayEmptyMethod
        Label[] lbls = new Label[0];

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, lbls, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, lbls, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, lbls, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("ldtoken", "ldtoken DefaultOpCodeFormatter_OpCodes", OpCodeFormattingContext.InLine)]
    [DataRow("isinst", "isinst DefaultOpCodeFormatter_OpCodes", OpCodeFormattingContext.InLine)]
    [DataRow("castclass", "castclass DefaultOpCodeFormatter_OpCodes", OpCodeFormattingContext.InLine)]
    public void WriteOpCodeWithType(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        Type type = GetType();
        Assert.IsNotNull(type);

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, type, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, type, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, type, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("ldstr", "ldstr \"string_test\"", OpCodeFormattingContext.InLine)]
    public void WriteOpCodeWithString(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        const string str = "string_test";

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, str, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, str, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, str, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("ldloc.s", "ldloc.s 1", OpCodeFormattingContext.InLine)]
    public void WriteOpCodeWithLocalIndex(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        const int lclInd = 1;

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, lclInd, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, lclInd, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, lclInd, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }

    [TestMethod]
    [DataRow("ldloc.s", "ldloc.s 6 : Version", OpCodeFormattingContext.InLine)]
    public void WriteOpCodeWithLocalBuilder(string opCodeStr, string expectedResult, OpCodeFormattingContext mode)
    {
        const int lclInd = 6;

        MethodInfo? method = Accessor.GetMethod(WriteOpCodeWithMethod);
        Assert.IsNotNull(method);
        LocalBuilder? lclBuilder = (LocalBuilder?)Activator.CreateInstance(typeof(LocalBuilder),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, [ lclInd, typeof(Version), method, false ],
            CultureInfo.InvariantCulture, null);
        Assert.IsNotNull(lclBuilder);

        OpCode opCode = EmitUtility.ParseOpCode(opCodeStr);

        IOpCodeFormatter formatter = new DefaultOpCodeFormatter();

        string format = formatter.Format(opCode, lclBuilder, mode);

        Console.WriteLine(format);
        Assert.AreEqual(expectedResult, format);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        int formatLength = formatter.GetFormatLength(opCode, lclBuilder, mode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..formatter.Format(opCode, lclBuilder, span, mode)];
        string separateFormat = new string(span);

        Console.WriteLine(separateFormat);
        Assert.AreEqual(expectedResult, separateFormat);
        Assert.AreEqual(formatLength, separateFormat.Length);
#endif
    }
}