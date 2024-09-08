using DanielWillett.ReflectionTools.Emit;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Tests.EmitterTests;

[TestClass]
public class EmitterExtensionTests
{
    private static readonly Type[] PrimitiveTypes =
    [
        typeof(bool), typeof(byte), typeof(sbyte), typeof(char), typeof(double), typeof(float), typeof(int), typeof(uint), typeof(nint), 
        typeof(nuint), typeof(long), typeof(ulong), typeof(short), typeof(ushort) 
    ];

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

    private delegate void SetSZArrayHandler<in T>(int index, T value, T[] vector);
    private delegate void SetVariableArrayHandler<in T>(int x, int y, T value, T[,] variableArray);
    private delegate T LoadSZArrayHandler<T>(int index, T[] vector);
    private delegate T LoadVariableArrayHandler<T>(int x, int y, T[,] variableArray);

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
    public void TestLoadDecimalConstant(string valStr)
    {
        decimal value = decimal.Parse(valStr);

        DynamicMethodInfo<GetDecimalHandler> dynMethod = DynamicMethodHelper.Create<GetDecimalHandler>(nameof(TestLoadDecimalConstant));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadConstantDecimal(value)
            .Return();

        GetDecimalHandler loadDec = dynMethod.CreateDelegate();

        Assert.AreEqual(value, loadDec());
    }

    [TestMethod]
    public void TestLoadTypeOf()
    {
        DynamicMethodInfo<GetTypeHandler> dynMethod = DynamicMethodHelper.Create<GetTypeHandler>(nameof(TestLoadTypeOf));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

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
        DynamicMethodInfo<Action> dynMethod = DynamicMethodHelper.Create<Action>(nameof(TestSetDefaultValue));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

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
                emit.LoadLocalAddress(emit.AddLocal<T>())
                    .SetDefaultValue<T>(unaligned, @volatile);
            }
            else
            {
                emit.LoadLocalAddress(emit.AddLocal<T>())
                    .SetDefaultValue(typeof(T), unaligned, @volatile);
            }
        }
    }

    [TestMethod]
    public void TestSetLoadArguments()
    {
        DynamicMethodInfo<Over256ArgsHandler> dynMethod = DynamicMethodHelper.Create<Over256ArgsHandler>(nameof(TestSetLoadArguments));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

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
        DynamicMethodInfo<Over256ArgsHandler> dynMethod = DynamicMethodHelper.Create<Over256ArgsHandler>(nameof(TestLoadArgumentAddresses));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

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
            DynamicMethodInfo<SetSZArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<SetSZArrayHandler<T>>(nameof(TestSetSZArrayElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

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
            DynamicMethodInfo<SetSZArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<SetSZArrayHandler<T>>(nameof(TestSetSZAsVariableArrayElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

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
            DynamicMethodInfo<SetVariableArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<SetVariableArrayHandler<T>>(nameof(TestSetVariableArrayElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

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
    
    [TestMethod]
    [DataRow(MemoryAlignment.AlignedNative, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, false, false)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, false)]
    public void TestLoadSZArrayElement(MemoryAlignment unaligned, bool @volatile, bool generic)
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
            DynamicMethodInfo<LoadSZArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<LoadSZArrayHandler<T>>(nameof(TestLoadSZArrayElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

            emit.LoadArgument(1);
            emit.LoadArgument(0);

            if (generic)
            {
                emit.LoadArrayElement<T>(unaligned, @volatile);
            }
            else
            {
                emit.LoadArrayElement(typeof(T), unaligned, @volatile);
            }

            emit.Return();

            LoadSZArrayHandler<T> getter = dynMethod.CreateDelegate();

            T[] array = new T[257];
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = factory(i);
                T val = getter(i, array);

                Assert.AreEqual(array[i], val);
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
    public void TestLoadSZAsVariableArrayElement(MemoryAlignment unaligned, bool @volatile, bool generic)
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
            DynamicMethodInfo<LoadSZArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<LoadSZArrayHandler<T>>(nameof(TestLoadSZAsVariableArrayElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

            emit.LoadArgument(1);
            emit.LoadArgument(0);

            if (generic)
            {
                emit.LoadArrayElement<T>(typeof(T[]), unaligned, @volatile);
            }
            else
            {
                emit.LoadArrayElement(typeof(T), typeof(T[]), unaligned, @volatile);
            }

            emit.Return();

            LoadSZArrayHandler<T> getter = dynMethod.CreateDelegate();

            T[] array = new T[257];
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = factory(i);
                T val = getter(i, array);

                Assert.AreEqual(array[i], val);
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
    public void TestLoadVariableArrayElement(MemoryAlignment unaligned, bool @volatile, bool generic)
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
            DynamicMethodInfo<LoadVariableArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<LoadVariableArrayHandler<T>>(nameof(TestLoadVariableArrayElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

            emit.LoadArgument(2);
            emit.LoadArgument(0);
            emit.LoadArgument(1);

            if (generic)
            {
                emit.LoadArrayElement<T>(typeof(T[,]), unaligned, @volatile);
            }
            else
            {
                emit.LoadArrayElement(typeof(T), typeof(T[,]), unaligned, @volatile);
            }

            emit.Return();

            LoadVariableArrayHandler<T> getter = dynMethod.CreateDelegate();

            T[,] array = new T[12,12];
            for (int x = 0; x < array.GetLength(0); ++x)
            {
                for (int y = 0; y < array.GetLength(1); ++y)
                {
                    array[x, y] = factory(x, y);
                    T val = getter(x, y, array);

                    Assert.AreEqual(array[x, y], val);
                }
            }
        }
    }
    
    
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestLoadSZArrayAddressElement(bool generic)
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
            DynamicMethodInfo<LoadSZArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<LoadSZArrayHandler<T>>(nameof(TestLoadSZArrayAddressElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

            emit.LoadArgument(1);
            emit.LoadArgument(0);

            if (generic)
            {
                emit.LoadArrayElementAddress<T>();
                emit.LoadAddressValue<T>();
            }
            else
            {
                emit.LoadArrayElementAddress(typeof(T));
                emit.LoadAddressValue(typeof(T));
            }

            emit.Return();

            LoadSZArrayHandler<T> getter = dynMethod.CreateDelegate();

            T[] array = new T[257];
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = factory(i);
                T val = getter(i, array);

                Assert.AreEqual(array[i], val);
            }
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestLoadSZAsVariableArrayAddressElement(bool generic)
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
            DynamicMethodInfo<LoadSZArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<LoadSZArrayHandler<T>>(nameof(TestLoadSZAsVariableArrayAddressElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

            emit.LoadArgument(1);
            emit.LoadArgument(0);

            if (generic)
            {
                emit.LoadArrayElementAddress<T>(typeof(T[]));
                emit.LoadAddressValue<T>();
            }
            else
            {
                emit.LoadArrayElementAddress(typeof(T), typeof(T[]));
                emit.LoadAddressValue(typeof(T));
            }

            emit.Return();

            LoadSZArrayHandler<T> getter = dynMethod.CreateDelegate();

            T[] array = new T[257];
            for (int i = 0; i < array.Length; ++i)
            {
                array[i] = factory(i);
                T val = getter(i, array);

                Assert.AreEqual(array[i], val);
            }
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestLoadVariableArrayAddressElement(bool generic)
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
            DynamicMethodInfo<LoadVariableArrayHandler<T>> dynMethod = DynamicMethodHelper.Create<LoadVariableArrayHandler<T>>(nameof(TestLoadVariableArrayAddressElement));

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

            emit.LoadArgument(2);
            emit.LoadArgument(0);
            emit.LoadArgument(1);

            if (generic)
            {
                emit.LoadArrayElementAddress<T>(typeof(T[,]));
                emit.LoadAddressValue<T>();
            }
            else
            {
                emit.LoadArrayElementAddress(typeof(T), typeof(T[,]));
                emit.LoadAddressValue(typeof(T));
            }

            emit.Return();

            LoadVariableArrayHandler<T> getter = dynMethod.CreateDelegate();

            T[,] array = new T[12,12];
            for (int x = 0; x < array.GetLength(0); ++x)
            {
                for (int y = 0; y < array.GetLength(1); ++y)
                {
                    array[x, y] = factory(x, y);
                    T val = getter(x, y, array);

                    Assert.AreEqual(array[x, y], val);
                }
            }
        }
    }

    [TestMethod]
    public void TestLoadSZArrayLength()
    {
        const int length = 12;

        DynamicMethodInfo<Func<int[], int>> dynMethod = DynamicMethodHelper.Create<Func<int[], int>>(nameof(TestLoadSZArrayLength));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadArgument(0)
            .LoadArrayLength()
            .Return();

        Func<int[], int> func = dynMethod.CreateDelegate();

        int len = func(new int[length]);

        Assert.AreEqual(length, len);
    }

    [TestMethod]
    public void TestExceptionBlockSimple()
    {
        DynamicMethodInfo<Func<int, int, int>> dynMethod = DynamicMethodHelper.Create<Func<int, int, int>>("TryDivide");

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.AddLocal<int>(out LocalBuilder lclResult)
            .Try(emit =>
            {
                emit.LoadArgument(0)
                    .LoadArgument(1)
                    .Divide()
                    .SetLocalValue(lclResult);
            })
            .Catch<DivideByZeroException>(emit =>
            {
                emit.PopFromStack() // pop exception object
                    .SetLocalToDefaultValue<int>(lclResult);
            })
            .End()
            .LoadLocalValue(lclResult)
            .Return();

        Func<int, int, int> tryDivide = dynMethod.CreateDelegate();

        int quotient = tryDivide(10, 0);
        Assert.AreEqual(0, quotient);

        quotient = tryDivide(10, 2);
        Assert.AreEqual(5, quotient);
    }

    [TestMethod]
    public void TestLoadSZAsVariableArrayLength()
    {
        const int length = 12;

        DynamicMethodInfo<Func<int[], int>> dynMethod = DynamicMethodHelper.Create<Func<int[], int>>(nameof(TestLoadSZAsVariableArrayLength));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadArgument(0)
            .LoadArrayLength(typeof(int[]))
            .Return();

        Func<int[], int> func = dynMethod.CreateDelegate();

        int len = func(new int[length]);

        Assert.AreEqual(length, len);
    }

    [TestMethod]
    public void TestLoadSZAsVariableArrayLengthSpecified()
    {
        const int length = 12;

        DynamicMethodInfo<Func<int[], int>> dynMethod = DynamicMethodHelper.Create<Func<int[], int>>(nameof(TestLoadSZAsVariableArrayLengthSpecified));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadArgument(0)
            .LoadArrayLength(1)
            .Return();

        Func<int[], int> func = dynMethod.CreateDelegate();

        int len = func(new int[length]);

        Assert.AreEqual(length, len);
    }

    [TestMethod]
    public void TestLoadVariableArrayLength()
    {
        const int size = 12;

        DynamicMethodInfo<Func<int[,], int>> dynMethod = DynamicMethodHelper.Create<Func<int[,], int>>(nameof(TestLoadVariableArrayLength));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadArgument(0)
            .LoadArrayLength(typeof(int[,]))
            .Return();

        Func<int[,], int> func = dynMethod.CreateDelegate();

        int len = func(new int[size, size]);

        Assert.AreEqual(size * size, len);
    }

    [TestMethod]
    public void TestLoadVariableArrayLengthSpecified()
    {
        const int size = 12;

        DynamicMethodInfo<Func<int[,], int>> dynMethod = DynamicMethodHelper.Create<Func<int[,], int>>(nameof(TestLoadVariableArrayLengthSpecified));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadArgument(0)
            .LoadArrayLength(2)
            .Return();

        Func<int[,], int> func = dynMethod.CreateDelegate();

        int len = func(new int[size, size]);

        Assert.AreEqual(size * size, len);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestCreateSZArray(bool generic)
    {
        const int length = 12;

        DynamicMethodInfo<Func<Array>> dynMethod = DynamicMethodHelper.Create<Func<Array>>(nameof(TestCreateSZAsVariableArray));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadConstantInt32(length);
        if (generic)
        {
            emit.CreateArray<int>();
        }
        else
        {
            emit.CreateArray(typeof(int));
        }

        emit.Return();

        Func<Array> func = dynMethod.CreateDelegate();

        Array array = func();

        Assert.AreEqual(typeof(int[]), array.GetType());
        Assert.AreEqual(0, array.GetLowerBound(0));
        Assert.AreEqual(length, array.Length);
        Assert.AreEqual(length, array.GetLength(0));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestCreateSZAsVariableArray(bool generic)
    {
        const int length = 12;

        DynamicMethodInfo<Func<Array>> dynMethod = DynamicMethodHelper.Create<Func<Array>>(nameof(TestCreateSZAsVariableArray));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadConstantInt32(length);
        if (generic)
        {
            emit.CreateArray<int>(typeof(int[]), hasStartIndices: false);
        }
        else
        {
            emit.CreateArray(typeof(int), typeof(int[]), hasStartIndices: false);
        }

        emit.Return();

        Func<Array> func = dynMethod.CreateDelegate();

        Array array = func();

        Assert.AreEqual(typeof(int[]), array.GetType());
        Assert.AreEqual(0, array.GetLowerBound(0));
        Assert.AreEqual(length, array.Length);
        Assert.AreEqual(length, array.GetLength(0));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestCreateVariableArrayZeroBound(bool generic)
    {
        const int size = 12;
        Type arrayType = typeof(int[,]);

        DynamicMethodInfo<Func<Array>> dynMethod = DynamicMethodHelper.Create<Func<Array>>(nameof(TestCreateVariableArrayZeroBound));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadConstantInt32(size)
            .LoadConstantInt32(size);

        if (generic)
        {
            emit.CreateArray<int>(typeof(int[,]), hasStartIndices: false);
        }
        else
        {
            emit.CreateArray(typeof(int), typeof(int[,]), hasStartIndices: false);
        }
        
        emit.Return();

        Func<Array> func = dynMethod.CreateDelegate();

        Array array = func();

        Assert.AreEqual(arrayType, array.GetType());
        Assert.AreEqual(0, array.GetLowerBound(0));
        Assert.AreEqual(0, array.GetLowerBound(1));
        Assert.AreEqual(size, array.GetLength(0));
        Assert.AreEqual(size, array.GetLength(1));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestCreateVariableArrayNonZeroBound(bool generic)
    {
        const int size = 12;
        Type arrayType = Array.CreateInstance(typeof(int), [ size, size ], [ 1, 1 ]).GetType();

        DynamicMethodInfo<Func<Array>> dynMethod = DynamicMethodHelper.Create<Func<Array>>(nameof(TestCreateVariableArrayNonZeroBound));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadConstantInt32(1)
            .LoadConstantInt32(size)
            .LoadConstantInt32(1)
            .LoadConstantInt32(size);

        if (generic)
        {
            emit.CreateArray<int>(arrayType, hasStartIndices: true);
        }
        else
        {
            emit.CreateArray(typeof(int), arrayType, hasStartIndices: true);
        }

        emit.Return();

        Func<Array> func = dynMethod.CreateDelegate();

        Array array = func();

        Assert.AreEqual(arrayType, array.GetType());
        Assert.AreEqual(1, array.GetLowerBound(0));
        Assert.AreEqual(1, array.GetLowerBound(1));
        Assert.AreEqual(size, array.GetLength(0));
        Assert.AreEqual(size, array.GetLength(1));
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

    private static int _fieldStatic;
    private int _fieldInstance;

#pragma warning restore CS0649

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestSetLoadFieldValueStatic(bool @volatile)
    {
        const int value = 1;

        _fieldStatic = 0;

        DynamicMethodInfo<Func<int>> dynMethod = DynamicMethodHelper.Create<Func<int>>(nameof(TestSetLoadFieldValueStatic));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadConstantInt32(value)
            .SetFieldValue(() => _fieldStatic, @volatile: @volatile)
            .LoadFieldValue(() => _fieldStatic, @volatile: @volatile)
            .Return();

        Func<int> loadDec = dynMethod.CreateDelegate();
        Assert.AreEqual(loadDec(), value);
    }

    [TestMethod]
    [DataRow(MemoryAlignment.AlignedNative, false)]
    [DataRow(MemoryAlignment.AlignedNative, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, false)]
    public void TestSetLoadFieldValueInstance(MemoryAlignment unaligned, bool @volatile)
    {
        const int value = 1;

        DynamicMethodInfo<Func<int>> dynMethod = DynamicMethodHelper.Create<Func<int>>(nameof(TestSetLoadFieldValueInstance));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.CreateObject<EmitterExtensionTests>()
            .Duplicate()
            .LoadConstantInt32(value)
            .SetFieldValue<EmitterExtensionTests, int>(x => x._fieldInstance, unaligned, @volatile)
            .LoadFieldValue<EmitterExtensionTests, int>(x => x._fieldInstance, unaligned, @volatile)
            .Return();

        Func<int> loadDec = dynMethod.CreateDelegate();
        Assert.AreEqual(loadDec(), value);
    }
    
    [TestMethod]
    public void TestSetLoadFieldAddressStatic()
    {
        const int value = 1;

        _fieldStatic = 0;

        DynamicMethodInfo<Func<int>> dynMethod = DynamicMethodHelper.Create<Func<int>>(nameof(TestSetLoadFieldAddressStatic));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.LoadFieldAddress(() => _fieldStatic)
            .LoadConstantInt32(value)
            .SetAddressValue<int>()
            .LoadFieldValue(() => _fieldStatic)
            .Return();

        Func<int> loadDec = dynMethod.CreateDelegate();
        Assert.AreEqual(loadDec(), value);
    }

    [TestMethod]
    public void TestSetLoadFieldAddressInstance()
    {
        const int value = 1;

        DynamicMethodInfo<Func<int>> dynMethod = DynamicMethodHelper.Create<Func<int>>(nameof(TestSetLoadFieldAddressInstance));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.CreateObject<EmitterExtensionTests>()
            .Duplicate()
            .LoadFieldAddress<EmitterExtensionTests, int>(x => x._fieldInstance)
            .LoadConstantInt32(value)
            .SetAddressValue<int>()
            .LoadFieldValue<EmitterExtensionTests, int>(x => x._fieldInstance)
            .Return();

        Func<int> loadDec = dynMethod.CreateDelegate();
        Assert.AreEqual(loadDec(), value);
    }
    
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestSizeOf(bool generic)
    {
        IEnumerable<Type> types = PrimitiveTypes.Concat([ typeof(decimal), typeof(Guid), typeof(string), typeof(DateTimeOffset) ]);

        foreach (Type type in types)
        {
            DynamicMethodInfo<Func<int>> dynMethod = DynamicMethodHelper.Create<Func<int>>(nameof(TestSizeOf) + type.Name);

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

            if (generic)
            {
                if (!type.IsValueType)
                    continue;

                MethodInfo mtd =
                    typeof(EmitterExtensions).GetMethod(nameof(EmitterExtensions.LoadSizeOf),
                        [ typeof(IOpCodeEmitter) ])!.MakeGenericMethod(type);

                mtd.Invoke(null, [ emit ]);
            }
            else
            {
                emit.LoadSizeOf(type);
            }

            emit.Return();

            Func<int> getSize = dynMethod.CreateDelegate();

            Array arr = Array.CreateInstance(type, 4);

            DynamicMethodInfo<Func<Array, int>> dynMethod2 = DynamicMethodHelper.Create<Func<Array, int>>(nameof(TestSizeOf) + "ArrayDist" + type.Name);
            emit = dynMethod2.GetEmitter(debuggable: true);

            // makeshift sizeof, gets num bytes between address of arr[0] and arr[1]
            emit.LoadArgument(0)
                .LoadConstantInt32(0)
                .LoadArrayElementAddress(type, @readonly: true)
                .PopToLocal(type.MakeByRefType(), out LocalBuilder ind0Addr)
                .LoadArgument(0)
                .LoadConstantInt32(1)
                .LoadArrayElementAddress(type, @readonly: true)
                .LoadLocalValue(ind0Addr)
                .Subtract()
                .Return();

            Assert.AreEqual(getSize(), dynMethod2.CreateDelegate()(arr));
        }
    }
    
    [TestMethod]
    [DataRow(MemoryAlignment.AlignedNative, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, true)]
    [DataRow(MemoryAlignment.AlignedPerByte, false, true)]
    [DataRow(MemoryAlignment.AlignedNative, false, false)]
    [DataRow(MemoryAlignment.AlignedPerByte, true, false)]
    public void TestLoadSetAddressValue(MemoryAlignment unaligned, bool @volatile, bool generic)
    {
        IEnumerable<Type> types = PrimitiveTypes.Concat([ typeof(decimal), typeof(Guid), typeof(string), typeof(DateTimeOffset) ]);

        foreach (Type type in types)
        {
            DynamicMethodInfo<Func<object>> dynMethod = DynamicMethodHelper.Create<Func<object>>(nameof(TestSizeOf) + type.Name);

            IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

            LocalBuilder lcl2 = emit.DeclareLocal(type);
            LocalBuilder lcl = emit.DeclareLocal(type);
            if (generic)
            {
                if (!type.IsValueType)
                    continue;

                MethodInfo setMtd =
                    typeof(EmitterExtensions).GetMethod(nameof(EmitterExtensions.SetAddressValue),
                        [ typeof(IOpCodeEmitter), typeof(MemoryAlignment), typeof(bool) ])!.MakeGenericMethod(type);
                MethodInfo loadMtd =
                    typeof(EmitterExtensions).GetMethod(nameof(EmitterExtensions.LoadAddressValue),
                        [ typeof(IOpCodeEmitter), typeof(MemoryAlignment), typeof(bool) ])!.MakeGenericMethod(type);
                MethodInfo setDefMtd =
                    typeof(EmitterExtensions).GetMethod(nameof(EmitterExtensions.SetLocalToDefaultValue),
                        [ typeof(IOpCodeEmitter), typeof(LocalReference) ])!.MakeGenericMethod(type);
                MethodInfo? boxMtd = type.IsValueType ?
                    typeof(EmitterExtensions).GetMethod(nameof(EmitterExtensions.Box),
                        [typeof(IOpCodeEmitter) ])!.MakeGenericMethod(type) : null;

                if (type == typeof(string))
                {
                    emit.LoadConstantString("test");
                    emit.SetLocalValue(lcl2);
                }
                else
                {
                    setDefMtd.Invoke(null, [ emit, (LocalReference)lcl2 ]);
                }
                emit.LoadLocalAddress(lcl);
                emit.LoadLocalValue(lcl2);

                setMtd.Invoke(null, [ emit, unaligned, @volatile ]);
                emit.LoadLocalAddress(lcl);
                loadMtd.Invoke(null, [ emit, unaligned, @volatile ]);

                if (type.IsValueType)
                    boxMtd!.Invoke(null, [ emit ]);
            }
            else
            {
                if (type == typeof(string))
                {
                    emit.LoadConstantString("test");
                    emit.SetLocalValue(lcl2);
                }
                else
                {
                    emit.SetLocalToDefaultValue(lcl2, type);
                }

                emit.LoadLocalAddress(lcl);
                emit.LoadLocalValue(lcl2);

                emit.SetAddressValue(type, unaligned, @volatile);
                emit.LoadLocalAddress(lcl);
                emit.LoadAddressValue(type, unaligned, @volatile);

                if (type.IsValueType)
                    emit.Box(type);
            }

            emit.Return();

            Func<object> getValue = dynMethod.CreateDelegate();

            Assert.AreEqual(
                type == typeof(string) ? "test" : (type.IsValueType ? Activator.CreateInstance(type) : null),
                getValue()
            );
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestRethrowThrowsInRoot(bool debuggable)
    {
        DynamicMethodInfo<Action> dynMethod = DynamicMethodHelper.Create<Action>(nameof(TestNonFaultExceptionBlocksDynamicMethod));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: debuggable);

        Assert.ThrowsException<InvalidOperationException>(emit.Rethrow);
    }


#if !NETFRAMEWORK
    [TestMethod]
    public void TestNonFaultExceptionBlocksDynamicMethod()
    {
        DynamicMethodInfo<Action> dynMethod = DynamicMethodHelper.Create<Action>(nameof(TestNonFaultExceptionBlocksDynamicMethod));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.EmitWriteLine("init");

        emit.Try(emit =>
        {
            emit.EmitWriteLine("try");

            emit.Try(emit =>
            {
                emit.EmitWriteLine("try");
                emit.ThrowException(typeof(IOException));

            }).Catch<FormatException>(emit =>
            {
                emit.EmitWriteLine("typed catch handler");
                emit.PopFromStack()
                    .Rethrow();
            }).Catch(emit =>
            {
                emit.PopFromStack()
                    .EmitWriteLine("catch handler");
            }).Finally(emit =>
            {
                emit.EmitWriteLine("finally");
            }).End();

            emit.ThrowException(typeof(IOException));

        }).CatchWhen<IOException>(emit =>
        {
            emit.EmitWriteLine("filter");
        
            emit.PopFromStack()
                .FailFilter();
        
        }).OnPass(emit =>
        {
            emit.PopFromStack()
                .EmitWriteLine("filter handler");
        
        }).Catch<FormatException>(emit =>
        {
            emit.EmitWriteLine("typed catch handler");
            emit.PopFromStack()
                .Rethrow();
        }).Catch(emit =>
        {
            emit.PopFromStack()
                .EmitWriteLine("catch handler");
        }).Finally(emit =>
        {
            emit.EmitWriteLine("finally");
        }).End();

        emit.EmitWriteLine("done");

        emit.Return();

        Action action = dynMethod.CreateDelegate();

        action();
    }

    [TestMethod]
    public void TestFaultExceptionBlocks()
    {
        DynamicMethodInfo<Action> dynMethod = DynamicMethodHelper.Create<Action>(nameof(TestNonFaultExceptionBlocksDynamicMethod));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.EmitWriteLine("init");

        emit.Try(emit =>
        {
            emit.EmitWriteLine("try");
            emit.ThrowException(typeof(IOException));

        }).Fault(emit =>
        {
            emit.EmitWriteLine("fault");
        }).End();

        emit.EmitWriteLine("done");

        emit.Return();

        Action action = dynMethod.CreateDelegate();

        Assert.ThrowsException<IOException>(() => action());
    }

#else

    [TestMethod]
    public void TestNonFaultExceptionBlocksDynamicMethod()
    {
        DynamicMethodInfo<Action> dynMethod = DynamicMethodHelper.Create<Action>(nameof(TestNonFaultExceptionBlocksDynamicMethod));

        IOpCodeEmitter emit = dynMethod.GetEmitter(debuggable: true);

        emit.EmitWriteLine("init");

        emit.Try(emit =>
        {
            emit.EmitWriteLine("try");

            emit.Try(emit =>
            {
                emit.EmitWriteLine("try");
                emit.ThrowException(typeof(IOException));

            }).Catch<FormatException>(emit =>
            {
                emit.EmitWriteLine("typed catch handler");
                emit.PopFromStack()
                    .Rethrow();
            }).Catch(emit =>
            {
                emit.PopFromStack()
                    .EmitWriteLine("catch handler");
            }).Finally(emit =>
            {
                emit.EmitWriteLine("finally");
            }).End();

            emit.ThrowException(typeof(IOException));

        }).Catch<FormatException>(emit =>
        {
            emit.EmitWriteLine("typed catch handler");
            emit.PopFromStack()
                .Rethrow();
        }).Catch(emit =>
        {
            emit.PopFromStack()
                .EmitWriteLine("catch handler");
        }).Finally(emit =>
        {
            emit.EmitWriteLine("finally");
        }).End();

        emit.EmitWriteLine("done");

        emit.Return();

        Action action = dynMethod.CreateDelegate();

        action();
    }
#endif
}