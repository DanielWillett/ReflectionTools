using System.Reflection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
[TestCategory("DefaultOpCodeFormatter")]
public class OperatorTests
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

    [TestMethod]
    public void TestOperatorNames()
    {
        foreach (OperatorType type in typeof(OperatorType).GetEnumValues())
        {
            if (type is OperatorType.None or OperatorType.UnaryMask or OperatorType.BinaryMask or OperatorType.CheckableMask)
                continue;

            Operator op = Operators.GetOperator(type);
            Assert.AreEqual(type, op.Type);
            Assert.AreEqual("op_" + type, op.MethodName);
            if (op.CanDefineCheckedVariant)
            {
                Assert.AreEqual("op_Checked" + type, op.GetMethodName(true));
            }
        }
    }

    private delegate ref readonly Operator GetOperatorFromProperty();

    [TestMethod]
    public void TestOperatorPropertyValues()
    {
        foreach (OperatorType type in typeof(OperatorType).GetEnumValues())
        {
            if (type is OperatorType.None or OperatorType.UnaryMask or OperatorType.BinaryMask or OperatorType.CheckableMask)
                continue;

            GetOperatorFromProperty getter =
                (GetOperatorFromProperty)typeof(Operators).GetProperty(type.ToString())
                .GetMethod
                .CreateDelegate(typeof(GetOperatorFromProperty));

            Operator op = getter();
            Assert.AreEqual(type, op.Type);
        }
    }

    [TestMethod]
    public void TestOperatorPairs()
    {
        Assert.AreEqual(OperatorType.False, Operators.GetOperator(OperatorType.True).RequiredPair);
        Assert.AreEqual(OperatorType.True, Operators.GetOperator(OperatorType.False).RequiredPair);

        Assert.AreEqual(OperatorType.Equality, Operators.GetOperator(OperatorType.Inequality).RequiredPair);
        Assert.AreEqual(OperatorType.Inequality, Operators.GetOperator(OperatorType.Equality).RequiredPair);

        Assert.AreEqual(OperatorType.GreaterThan, Operators.GetOperator(OperatorType.LessThan).RequiredPair);
        Assert.AreEqual(OperatorType.LessThan, Operators.GetOperator(OperatorType.GreaterThan).RequiredPair);

        Assert.AreEqual(OperatorType.GreaterThanOrEqual, Operators.GetOperator(OperatorType.LessThanOrEqual).RequiredPair);
        Assert.AreEqual(OperatorType.LessThanOrEqual, Operators.GetOperator(OperatorType.GreaterThanOrEqual).RequiredPair);
    }

    [TestMethod]
    public void TestAmbiguousResultsReturnsLeftType()
    {
        MethodInfo? op = Operators.Find<OpTestStruct1, OpTestStruct2>(OperatorType.Addition);

        Assert.IsNotNull(op);
        Assert.AreEqual(typeof(OpTestStruct1), op.DeclaringType);
    }

    [TestMethod]
    public void TestFindUncheckedBinaryOperator()
    {
        MethodInfo? op = Operators.Find<OpTestStruct1, OpTestStruct2>(OperatorType.Addition);

        Assert.IsNotNull(op);
        Assert.AreEqual("op_Addition", op.Name);
    }

    [TestMethod]
    public void TestFindCheckedBinaryOperator()
    {
        MethodInfo? op = Operators.Find<OpTestStruct1, OpTestStruct2>(OperatorType.Addition, preferCheckedOperator: true);

        Assert.IsNotNull(op);
        Assert.AreEqual("op_CheckedAddition", op.Name);
    }
    
    [TestMethod]
    public void TestFindBinaryOperatorNeitherChecked()
    {
        MethodInfo? op = Operators.Find<OpTestStruct1, OpTestStruct1>(OperatorType.BitwiseAnd);

        Assert.IsNotNull(op);
        Assert.AreEqual("op_BitwiseAnd", op.Name);
    }
    
    [TestMethod]
    public void TestFindBinaryOperatorAsUnary()
    {
        MethodInfo? op = Operators.Find<OpTestStruct1>(OperatorType.BitwiseAnd);

        Assert.IsNotNull(op);
        Assert.AreEqual("op_BitwiseAnd", op.Name);
    }

    [TestMethod]
    public void TestFindUncheckedUnaryOperator()
    {
        MethodInfo? op = Operators.Find<OpTestStruct1>(OperatorType.UnaryNegation);

        Assert.IsNotNull(op);
        Assert.AreEqual("op_UnaryNegation", op.Name);
    }

    [TestMethod]
    public void TestFindCheckedUnaryOperator()
    {
        MethodInfo? op = Operators.Find<OpTestStruct1>(OperatorType.UnaryNegation, preferCheckedOperator: true);

        Assert.IsNotNull(op);
        Assert.AreEqual("op_CheckedUnaryNegation", op.Name);
    }

    [TestMethod]
    public void TestFindUnaryOperatorNeitherChecked()
    {
        MethodInfo? op = Operators.Find<OpTestStruct1>(OperatorType.LogicalNot);

        Assert.IsNotNull(op);
        Assert.AreEqual("op_LogicalNot", op.Name);
    }

    [TestMethod]
    public void TestFindCastUnchecked()
    {
        MethodInfo? op = Operators.FindCast<OpTestStruct1, OpTestStruct2>();

        Assert.IsNotNull(op);
        Assert.AreEqual("op_Explicit", op.Name);
    }

    [TestMethod]
    public void TestFindCastChecked()
    {
        MethodInfo? op = Operators.FindCast<OpTestStruct1, OpTestStruct2>(preferCheckedOperator: true);

        Assert.IsNotNull(op);
        Assert.AreEqual("op_CheckedExplicit", op.Name);
    }

    public struct OpTestStruct1
    {
        public static OpTestStruct1 operator -(OpTestStruct1 left) => throw null!;
        public static OpTestStruct1 operator checked -(OpTestStruct1 left) => throw null!;
        public static OpTestStruct1 operator +(OpTestStruct1 left, OpTestStruct2 right) => throw null!;
        public static OpTestStruct1 operator checked +(OpTestStruct1 left, OpTestStruct2 right) => throw null!;
        public static OpTestStruct1 operator &(OpTestStruct1 left, OpTestStruct1 right) => throw null!;
        public static OpTestStruct1 operator !(OpTestStruct1 left) => throw null!;

        public static implicit operator OpTestStruct2(OpTestStruct1 v)
        {
            return default;
        }
        public static explicit operator int(OpTestStruct1 v)
        {
            return default;
        }
        public static explicit operator checked int(OpTestStruct1 v)
        {
            return default;
        }
    }

    public struct OpTestStruct2
    {
        public static OpTestStruct1 operator +(OpTestStruct1 left, OpTestStruct2 right) => throw null!;
        public static explicit operator OpTestStruct2(OpTestStruct1 v)
        {
            return default;
        }
        public static explicit operator checked OpTestStruct2(OpTestStruct1 v)
        {
            return default;
        }
    }

#if DEBUG
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestConversionOperatorReturnOrderExplicit(bool preferCheckedOperator)
    {
        Type from = typeof(OpTestClass1);
        Type to = typeof(OpTestClass2);
        MethodInfo[] fromMethods = from.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        MethodInfo[] toMethods = to.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        int ct = fromMethods.Length + toMethods.Length;
        for (int i = 0; i < ct; ++i)
        {
            bool isFromSub = i < fromMethods.Length;
            MethodInfo mtd = isFromSub ? fromMethods[i] : toMethods[i - fromMethods.Length];
            OpOrderAttribute? attr = mtd.GetAttributeSafe<OpOrderAttribute>();
            if (attr == null)
                continue;

            int order = Operators.GetIntlOrderingConversionMethod(mtd, from, to, isFromSub, preferCheckedOperator);

            int expectedOrder = attr.ExpectedOrder;
            if (!preferCheckedOperator && expectedOrder > 15)
                expectedOrder = -1;

            Assert.AreEqual(expectedOrder, order);
        }
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void TestConversionOperatorReturnOrderImplicit(bool preferCheckedOperator)
    {
        Type from = typeof(OpTestClass3);
        Type to = typeof(OpTestClass4);
        MethodInfo[] fromMethods = from.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        MethodInfo[] toMethods = to.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        int ct = fromMethods.Length + toMethods.Length;
        for (int i = 0; i < ct; ++i)
        {
            bool isFromSub = i < fromMethods.Length;
            MethodInfo mtd = isFromSub ? fromMethods[i] : toMethods[i - fromMethods.Length];
            OpOrderAttribute? attr = mtd.GetAttributeSafe<OpOrderAttribute>();
            if (attr == null)
                continue;

            int order = Operators.GetIntlOrderingConversionMethod(mtd, from, to, isFromSub, preferCheckedOperator);

            int expectedOrder = attr.ExpectedOrder;
            if (!preferCheckedOperator && expectedOrder > 15)
                expectedOrder = -1;

            Assert.AreEqual(expectedOrder, order);
        }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    private sealed class OpOrderAttribute : Attribute
    {
        public int ExpectedOrder { get; }

        public OpOrderAttribute(int expectedOrder)
        {
            ExpectedOrder = expectedOrder;
        }
    }

    // ReSharper disable UnusedParameter.Local

    // find order, higher numbers are returned first
    // fromsub/tosub means the base type of from/to
    //  * implicit checked conversions aren't allowed
    //  - explicit exact match checked in from      - 27
    //  - explicit exact match checked in fromsub   - 26
    //  - explicit exact match checked in to        - 25
    //  - explicit exact match checked in tosub     - 24
    //  * implicit checked conversions aren't allowed
    //  - explicit match checked in from            - 19
    //  - explicit match checked in fromsub         - 18
    //  - explicit match checked in to              - 17
    //  - explicit match checked in tosub           - 16
    //  - implicit exact match unchecked in from    - 15
    //  - implicit exact match unchecked in fromsub - 14
    //  - implicit exact match unchecked in to      - 13
    //  - implicit exact match unchecked in tosub   - 12
    //  - explicit exact match unchecked in from    - 11
    //  - explicit exact match unchecked in fromsub - 10
    //  - explicit exact match unchecked in to      -  9
    //  - explicit exact match unchecked in tosub   -  8
    //  - implicit match unchecked in from          -  7
    //  - implicit match unchecked in fromsub       -  6
    //  - implicit match unchecked in to            -  5
    //  - implicit match unchecked in tosub         -  4
    //  - explicit match unchecked in from          -  3
    //  - explicit match unchecked in fromsub       -  2
    //  - explicit match unchecked in to            -  1
    //  - explicit match unchecked in tosub         -  0

    // explicit

    // fromsub
    private class OpTestClass1Sub
    {
        [OpOrder(-1)]
        public static explicit operator OpTestClass2Sub(OpTestClass1Sub c) => throw null!;

        [OpOrder(2)]
        public static explicit operator OpTestClass2(OpTestClass1Sub c) => throw null!;

        [OpOrder(18)]
        public static explicit operator checked OpTestClass2(OpTestClass1Sub c) => throw null!;
    }

    // from
    private class OpTestClass1 : OpTestClass1Sub
    {
        [OpOrder(-1)]
        public static explicit operator OpTestClass2Sub(OpTestClass1 c) => throw null!;

        [OpOrder(11)]
        public static explicit operator OpTestClass2(OpTestClass1 c) => throw null!;

        [OpOrder(27)]
        public static explicit operator checked OpTestClass2(OpTestClass1 c) => throw null!;
    }

    // tosub
    private class OpTestClass2Sub
    {
        [OpOrder(-1)]
        public static explicit operator OpTestClass2Sub(OpTestClass1Sub c) => throw null!;
    }

    // to
    private class OpTestClass2 : OpTestClass2Sub
    {
        [OpOrder(1)]
        public static explicit operator OpTestClass2(OpTestClass1Sub c) => throw null!;
        
        [OpOrder(17)]
        public static explicit operator checked OpTestClass2(OpTestClass1Sub c) => throw null!;

        [OpOrder(9)]
        public static explicit operator OpTestClass2(OpTestClass1 c) => throw null!;

        [OpOrder(25)]
        public static explicit operator checked OpTestClass2(OpTestClass1 c) => throw null!;
    }

    // implicit 

    // fromsub
    private class OpTestClass3Sub
    {
        [OpOrder(-1)]
        public static implicit operator OpTestClass4Sub(OpTestClass3Sub c) => throw null!;

        [OpOrder(6)]
        public static implicit operator OpTestClass4(OpTestClass3Sub c) => throw null!;
    }

    // from
    private class OpTestClass3 : OpTestClass3Sub
    {
        [OpOrder(-1)]
        public static implicit operator OpTestClass4Sub(OpTestClass3 c) => throw null!;

        [OpOrder(15)]
        public static implicit operator OpTestClass4(OpTestClass3 c) => throw null!;
    }

    // tosub
    private class OpTestClass4Sub
    {
        [OpOrder(-1)]
        public static implicit operator OpTestClass4Sub(OpTestClass3Sub c) => throw null!;
    }

    // to
    private class OpTestClass4 : OpTestClass4Sub
    {
        [OpOrder(5)]
        public static implicit operator OpTestClass4(OpTestClass3Sub c) => throw null!;

        [OpOrder(13)]
        public static implicit operator OpTestClass4(OpTestClass3 c) => throw null!;
    }

    // ReSharper restore UnusedParameter.Local
#endif
}