using DanielWillett.ReflectionTools.Emit;

namespace DanielWillett.ReflectionTools.Tests.EmitterTests;

[TestClass]
public class EmitterExtensionTests
{
    private delegate decimal GetDecimalHandler();
    private delegate Type GetTypeHandler();
    private delegate void Over256ArgsHandler(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6, int arg7,
        int arg8, int arg9, int arg10, int arg11, int arg12, int arg13, int arg14, int arg15, int arg16, int arg17,
        int arg18, int arg19, int arg20, int arg21, int arg22, int arg23, int arg24, int arg25, int arg26, int arg27,
        int arg28, int arg29, int arg30, int arg31, int arg32, int arg33, int arg34, int arg35, int arg36, int arg37,
        int arg38, int arg39, int arg40, int arg41, int arg42, int arg43, int arg44, int arg45, int arg46, int arg47,
        int arg48, int arg49, int arg50, int arg51, int arg52, int arg53, int arg54, int arg55, int arg56, int arg57,
        int arg58, int arg59, int arg60, int arg61, int arg62, int arg63, int arg64, int arg65, int arg66, int arg67,
        int arg68, int arg69, int arg70, int arg71, int arg72, int arg73, int arg74, int arg75, int arg76, int arg77,
        int arg78, int arg79, int arg80, int arg81, int arg82, int arg83, int arg84, int arg85, int arg86, int arg87,
        int arg88, int arg89, int arg90, int arg91, int arg92, int arg93, int arg94, int arg95, int arg96, int arg97,
        int arg98, int arg99, int arg100, int arg101, int arg102, int arg103, int arg104, int arg105, int arg106,
        int arg107, int arg108, int arg109, int arg110, int arg111, int arg112, int arg113, int arg114, int arg115,
        int arg116, int arg117, int arg118, int arg119, int arg120, int arg121, int arg122, int arg123, int arg124,
        int arg125, int arg126, int arg127, int arg128, int arg129, int arg130, int arg131, int arg132, int arg133,
        int arg134, int arg135, int arg136, int arg137, int arg138, int arg139, int arg140, int arg141, int arg142,
        int arg143, int arg144, int arg145, int arg146, int arg147, int arg148, int arg149, int arg150, int arg151,
        int arg152, int arg153, int arg154, int arg155, int arg156, int arg157, int arg158, int arg159, int arg160,
        int arg161, int arg162, int arg163, int arg164, int arg165, int arg166, int arg167, int arg168, int arg169,
        int arg170, int arg171, int arg172, int arg173, int arg174, int arg175, int arg176, int arg177, int arg178,
        int arg179, int arg180, int arg181, int arg182, int arg183, int arg184, int arg185, int arg186, int arg187,
        int arg188, int arg189, int arg190, int arg191, int arg192, int arg193, int arg194, int arg195, int arg196,
        int arg197, int arg198, int arg199, int arg200, int arg201, int arg202, int arg203, int arg204, int arg205,
        int arg206, int arg207, int arg208, int arg209, int arg210, int arg211, int arg212, int arg213, int arg214,
        int arg215, int arg216, int arg217, int arg218, int arg219, int arg220, int arg221, int arg222, int arg223,
        int arg224, int arg225, int arg226, int arg227, int arg228, int arg229, int arg230, int arg231, int arg232,
        int arg233, int arg234, int arg235, int arg236, int arg237, int arg238, int arg239, int arg240, int arg241,
        int arg242, int arg243, int arg244, int arg245, int arg246, int arg247, int arg248, int arg249, int arg250,
        int arg251, int arg252, int arg253, int arg254, int arg255, int arg256, int arg257);

    private delegate void SetSZArrayHandler<T>(int index, T value, T[] vector);
    private delegate void SetVariableArrayHandler<T>(int x, int y, T value, T[,] variableArray);

    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("1.0")]
    [DataRow("0.00000000000000000000000000000000000")]
    [DataRow("-1474535732890753085.3957395936503750")]
    [DataRow("-1")]
    public void TestWriteDecimalConstant(string valStr)
    {
        decimal value = decimal.Parse(valStr);

        DynamicMethodInfo<GetDecimalHandler> dynMethod = DynamicMethodHelper.Create<GetDecimalHandler>(nameof(TestWriteDecimalConstant));

        IOpCodeEmitter emit = dynMethod.GetILGenerator(debuggable: true);

        emit.LoadConstantDecimal(value)
            .Return();

        GetDecimalHandler loadDec = dynMethod.CreateDelegate();

        Assert.AreEqual(value, loadDec());
    }

    [TestMethod]
    public void TestLoadTypeOf()
    {
        DynamicMethodInfo<GetTypeHandler> dynMethod = DynamicMethodHelper.Create<GetTypeHandler>(nameof(TestWriteDecimalConstant));

        IOpCodeEmitter emit = dynMethod.GetILGenerator(debuggable: true);

        emit.LoadTypeOf<int>()
            .Return();

        GetTypeHandler loadDec = dynMethod.CreateDelegate();

        Assert.AreEqual(typeof(int), loadDec());
    }

    [TestMethod]
    [DataRow(MemoryAlignment.AlignedNative, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, false, false)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, false)]
    public void TestSetDefaultValue(MemoryAlignment unaligned, bool @volatile, bool generic)
    {
        DynamicMethodInfo<Action> dynMethod = DynamicMethodHelper.Create<Action>(nameof(TestWriteDecimalConstant));

        IOpCodeEmitter emit = dynMethod.GetILGenerator(debuggable: true);

        EmitTest<byte>(emit);
        EmitTest<sbyte>(emit);
        EmitTest<bool>(emit);
        EmitTest<ushort>(emit);
        EmitTest<short>(emit);
        EmitTest<char>(emit);
        EmitTest<uint>(emit);
        EmitTest<int>(emit);
        EmitTest<ulong>(emit);
        EmitTest<long>(emit);
        EmitTest<string>(emit);
        EmitTest<Guid>(emit);

        emit.Return();

        Action loadDec = dynMethod.CreateDelegate();

        loadDec();
        return;

        void EmitTest<T>(IOpCodeEmitter emit)
        {
            if (generic)
            {
                emit.LoadLocalAddress(emit.DeclareLocal<T>())
                    .SetDefaultValue<T>(unaligned, @volatile);
            }
            else
            {
                emit.LoadLocalAddress(emit.DeclareLocal<T>())
                    .SetDefaultValue(typeof(T), unaligned, @volatile);
            }
        }
    }

    [TestMethod]
    public void TestSetLoadArguments()
    {
        DynamicMethodInfo<Over256ArgsHandler> dynMethod = DynamicMethodHelper.Create<Over256ArgsHandler>(nameof(TestWriteDecimalConstant));

        IOpCodeEmitter emit = dynMethod.GetILGenerator(debuggable: true);

        for (int i = 0; i < 257; ++i)
        {
            emit.LoadArgument(i)
                .PopFromStack()
                .LoadConstantInt32(i)
                .SetArgument(i);
        }

        emit.Return();

        Over256ArgsHandler loadDec = dynMethod.CreateDelegate();
        loadDec(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    [TestMethod]
    public void TestLoadArgumentAddresses()
    {
        DynamicMethodInfo<Over256ArgsHandler> dynMethod = DynamicMethodHelper.Create<Over256ArgsHandler>(nameof(TestWriteDecimalConstant));

        IOpCodeEmitter emit = dynMethod.GetILGenerator(debuggable: true);

        for (int i = 0; i < 257; ++i)
        {
            emit.LoadArgumentAddress(i)
                .LoadConstantInt32(i)
                .SetAddressValue<int>();
        }

        emit.Return();

        Over256ArgsHandler loadDec = dynMethod.CreateDelegate();
        loadDec(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    [TestMethod]
    [DataRow(MemoryAlignment.AlignedNative, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, false, false)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, false)]
    public void TestSetSZArrayElement(MemoryAlignment unaligned, bool @volatile, bool generic)
    {
        Test(index => (byte)index);
        Test(index => (sbyte)index);
        Test(index => index % 2 == 1);
        Test(index => (ushort)index);
        Test(index => (short)index);
        Test(index => (char)index);
        Test(index => (uint)index);
        Test(index => index);
        Test(index => (ulong)index);
        Test(index => (long)index);
        Test(index => index.ToString());
        Test(index => new Guid(index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));


        void Test<T>(Func<int, T> factory)
        {
            DynamicMethodInfo<SetSZArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<SetSZArrayHandler<T>>(nameof(TestWriteDecimalConstant));

            IOpCodeEmitter emit = dynMethod.GetILGenerator(debuggable: true);

            emit.LoadArgument(2);
            emit.LoadArgument(0);
            emit.LoadArgument(1);

            if (generic)
            {
                emit.SetArrayElement<T>(unaligned, @volatile);
            }
            else
            {
                emit.SetArrayElement(typeof(T), unaligned, @volatile);
            }

            emit.Return();

            SetSZArrayHandler<T> setter = dynMethod.CreateDelegate();

            T[] array = new T[257];
            for (int i = 0; i < array.Length; ++i)
            {
                T val = factory(i);
                setter(i, val, array);

                Assert.AreEqual(val, array[i]);
            }
        }
    }
    

    [TestMethod]
    [DataRow(MemoryAlignment.AlignedNative, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, false, false)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, false)]
    public void TestSetSZAsVariableArrayElement(MemoryAlignment unaligned, bool @volatile, bool generic)
    {
        Test(index => (byte)index);
        Test(index => (sbyte)index);
        Test(index => index % 2 == 1);
        Test(index => (ushort)index);
        Test(index => (short)index);
        Test(index => (char)index);
        Test(index => (uint)index);
        Test(index => index);
        Test(index => (ulong)index);
        Test(index => (long)index);
        Test(index => index.ToString());
        Test(index => new Guid(index, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));


        void Test<T>(Func<int, T> factory)
        {
            DynamicMethodInfo<SetSZArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<SetSZArrayHandler<T>>(nameof(TestWriteDecimalConstant));

            IOpCodeEmitter emit = dynMethod.GetILGenerator(debuggable: true);

            emit.LoadArgument(2);
            emit.LoadArgument(0);
            emit.LoadArgument(1);

            if (generic)
            {
                emit.SetArrayElement<T>(typeof(T[]), unaligned, @volatile);
            }
            else
            {
                emit.SetArrayElement(typeof(T), typeof(T[]), unaligned, @volatile);
            }

            emit.Return();

            SetSZArrayHandler<T> setter = dynMethod.CreateDelegate();

            T[] array = new T[257];
            for (int i = 0; i < array.Length; ++i)
            {
                T val = factory(i);
                setter(i, val, array);

                Assert.AreEqual(val, array[i]);
            }
        }
    }

    [TestMethod]
    [DataRow(MemoryAlignment.AlignedNative, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, false, false)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, false)]
    public void TestSetVariableArrayElement(MemoryAlignment unaligned, bool @volatile, bool generic)
    {
        Test((x, y) => (byte)(x + y));
        Test((x, y) => (sbyte)(x + y));
        Test((x, y) => (x + y) % 2 == 1);
        Test((x, y) => (ushort)(x + y));
        Test((x, y) => (short)(x + y));
        Test((x, y) => (char)(x + y));
        Test((x, y) => (uint)(x + y));
        Test((x, y) => x + y);
        Test((x, y) => (ulong)(x + y));
        Test((x, y) => (long)(x + y));
        Test((x, y) => x + "," + y);
        Test((x, y) => new Guid(x, (short)(y >> 16), (short)y, 0, 0, 0, 0, 0, 0, 0, 0));


        void Test<T>(Func<int, int, T> factory)
        {
            DynamicMethodInfo<SetVariableArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<SetVariableArrayHandler<T>>(nameof(TestWriteDecimalConstant));

            IOpCodeEmitter emit = dynMethod.GetILGenerator(debuggable: true);

            emit.LoadArgument(3);
            emit.LoadArgument(0);
            emit.LoadArgument(1);
            emit.LoadArgument(2);

            if (generic)
            {
                emit.SetArrayElement<T>(typeof(T[,]), unaligned, @volatile);
            }
            else
            {
                emit.SetArrayElement(typeof(T), typeof(T[,]), unaligned, @volatile);
            }

            emit.Return();

            SetVariableArrayHandler<T> setter = dynMethod.CreateDelegate();

            T[,] array = new T[12,12];
            for (int x = 0; x < array.GetLength(0); ++x)
            {
                for (int y = 0; y < array.GetLength(1); ++y)
                {
                    T val = factory(x, y);
                    setter(x, y, val, array);

                    Assert.AreEqual(val, array[x, y]);
                }
            }
        }
    }

}
