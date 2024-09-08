using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Contains info about user-overloaded operator types.
/// </summary>
public static class Operators
{
    private const OperatorType MaxOperatorType = OperatorType.DivisionAssignment;

    /// <summary>
    /// The name of the function of an <see langword="implicit"/> conversion operator.
    /// </summary>
    public const string ImplicitConversionOperatorMethodName = "op_Implicit";

    /// <summary>
    /// The name of the function of an <see langword="explicit"/> conversion operator.
    /// </summary>
    public const string ExplicitConversionOperatorMethodName = "op_Explicit";

    /// <summary>
    /// The name of the function of a <see langword="checked"/> <see langword="explicit"/> conversion operator.
    /// </summary>
    public const string ExplicitCheckedConversionOperatorMethodName = "op_CheckedExplicit";

    private static readonly Operator[] OperatorsIntl =
    [
        /* unary */
        new Operator(OperatorType.Decrement, OperatorType.None, 0),
        new Operator(OperatorType.Increment, OperatorType.None, 1),
        new Operator(OperatorType.UnaryNegation, OperatorType.None, 2),
        new Operator(OperatorType.UnaryPlus, OperatorType.None),
        new Operator(OperatorType.LogicalNot, OperatorType.None),
        new Operator(OperatorType.True, OperatorType.False),
        new Operator(OperatorType.False, OperatorType.True),
        new Operator(OperatorType.AddressOf, OperatorType.None),
        new Operator(OperatorType.OnesComplement, OperatorType.None),
        new Operator(OperatorType.PointerDereference, OperatorType.None),

        /* binary */
        new Operator(OperatorType.Addition, OperatorType.None, 3),
        new Operator(OperatorType.Subtraction, OperatorType.None, 4),
        new Operator(OperatorType.Multiply, OperatorType.None, 5),
        new Operator(OperatorType.Division, OperatorType.None, 6),
        new Operator(OperatorType.Modulus, OperatorType.None),
        new Operator(OperatorType.ExclusiveOr, OperatorType.None),
        new Operator(OperatorType.BitwiseAnd, OperatorType.None),
        new Operator(OperatorType.BitwiseOr, OperatorType.None),
        new Operator(OperatorType.LogicalAnd, OperatorType.None),
        new Operator(OperatorType.LogicalOr, OperatorType.None),
        new Operator(OperatorType.Assign, OperatorType.None),
        new Operator(OperatorType.LeftShift, OperatorType.None),
        new Operator(OperatorType.RightShift, OperatorType.None),
        new Operator(OperatorType.SignedRightShift, OperatorType.None),
        new Operator(OperatorType.UnsignedRightShift, OperatorType.None),
        new Operator(OperatorType.Equality, OperatorType.Inequality),
        new Operator(OperatorType.GreaterThan, OperatorType.LessThan),
        new Operator(OperatorType.LessThan, OperatorType.GreaterThan),
        new Operator(OperatorType.Inequality, OperatorType.Equality),
        new Operator(OperatorType.GreaterThanOrEqual, OperatorType.LessThanOrEqual),
        new Operator(OperatorType.LessThanOrEqual, OperatorType.GreaterThanOrEqual),
        new Operator(OperatorType.UnsignedRightShiftAssignment, OperatorType.None),
        new Operator(OperatorType.MemberSelection, OperatorType.None),
        new Operator(OperatorType.RightShiftAssignment, OperatorType.None),
        new Operator(OperatorType.MultiplicationAssignment, OperatorType.None, 7),
        new Operator(OperatorType.PointerToMemberSelection, OperatorType.None),
        new Operator(OperatorType.SubtractionAssignment, OperatorType.None, 8),
        new Operator(OperatorType.ExclusiveOrAssignment, OperatorType.None),
        new Operator(OperatorType.LeftShiftAssignment, OperatorType.None),
        new Operator(OperatorType.ModulusAssignment, OperatorType.None),
        new Operator(OperatorType.AdditionAssignment, OperatorType.None, 9),
        new Operator(OperatorType.BitwiseAndAssignment, OperatorType.None),
        new Operator(OperatorType.BitwiseOrAssignment, OperatorType.None),
        new Operator(OperatorType.Comma, OperatorType.None),
        new Operator(OperatorType.DivisionAssignment, OperatorType.None, 10)
    ];

    internal static readonly string[] MethodNamesIntl =
    [
        /* unary */
        "op_Decrement",
        "op_Increment",
        "op_UnaryNegation",
        "op_UnaryPlus",
        "op_LogicalNot",
        "op_True",
        "op_False",
        "op_AddressOf",
        "op_OnesComplement",
        "op_PointerDereference",
        
        /* binary */
        "op_Addition",
        "op_Subtraction",
        "op_Multiply",
        "op_Division",
        "op_Modulus",
        "op_ExclusiveOr",
        "op_BitwiseAnd",
        "op_BitwiseOr",
        "op_LogicalAnd",
        "op_LogicalOr",
        "op_Assign",
        "op_LeftShift",
        "op_RightShift",
        "op_SignedRightShift",
        "op_UnsignedRightShift",
        "op_Equality",
        "op_GreaterThan",
        "op_LessThan",
        "op_Inequality",
        "op_GreaterThanOrEqual",
        "op_LessThanOrEqual",
        "op_UnsignedRightShiftAssignment",
        "op_MemberSelection",
        "op_RightShiftAssignment",
        "op_MultiplicationAssignment",
        "op_PointerToMemberSelection",
        "op_SubtractionAssignment",
        "op_ExclusiveOrAssignment",
        "op_LeftShiftAssignment",
        "op_ModulusAssignment",
        "op_AdditionAssignment",
        "op_BitwiseAndAssignment",
        "op_BitwiseOrAssignment",
        "op_Comma",
        "op_DivisionAssignment"
    ];
    internal static readonly string[] CheckedMethodNamesIntl =
    [
        /* unary */
        "op_CheckedDecrement",
        "op_CheckedIncrement",
        "op_CheckedUnaryNegation",
        
        /* binary */
        "op_CheckedAddition",
        "op_CheckedSubtraction",
        "op_CheckedMultiply",
        "op_CheckedDivision",
        "op_CheckedMultiplicationAssignment",
        "op_CheckedSubtractionAssignment",
        "op_CheckedAdditionAssignment",
        "op_CheckedDivisionAssignment"
    ];

    /// <summary>
    /// List of all unary and binary operators in the CLI.
    /// </summary>
    /// <remarks>Conversion operators are not included.</remarks>
#if NET45_OR_GREATER || !NETFRAMEWORK
    public static IReadOnlyList<Operator> AllOperators { get; }
#else
    public static IList<Operator> AllOperators { get; }
#endif

    static Operators()
    {
        AllOperators = new ReadOnlyCollection<Operator>(OperatorsIntl);
    }

    /// <summary>
    /// <para><c>value--</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> --(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary, Checkable</remarks>
    public static ref readonly Operator Decrement => ref OperatorsIntl[((int)OperatorType.Decrement >> 3) - 1];

    /// <summary>
    /// <para><c>value++</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> ++(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary, Checkable</remarks>
    public static ref readonly Operator Increment => ref OperatorsIntl[((int)OperatorType.Increment >> 3) - 1];

    /// <summary>
    /// <para><c>-value</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> -(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary, Checkable</remarks>
    public static ref readonly Operator UnaryNegation => ref OperatorsIntl[((int)OperatorType.UnaryNegation >> 3) - 1];

    /// <summary>
    /// <para><c>+value</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> +(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    public static ref readonly Operator UnaryPlus => ref OperatorsIntl[((int)OperatorType.UnaryPlus >> 3) - 1];

    /// <summary>
    /// <para><c>!value</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> !(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    public static ref readonly Operator LogicalNot => ref OperatorsIntl[((int)OperatorType.LogicalNot >> 3) - 1];

    /// <summary>
    /// <c><see langword="static"/> <see cref="bool"/> <see langword="operator"/> <see langword="true"/>(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    public static ref readonly Operator True => ref OperatorsIntl[((int)OperatorType.True >> 3) - 1];

    /// <summary>
    /// <c><see langword="static"/> <see cref="bool"/> <see langword="operator"/> <see langword="false"/>(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    public static ref readonly Operator False => ref OperatorsIntl[((int)OperatorType.False >> 3) - 1];

    /// <summary>
    /// <para><c>&amp;value</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Unary</remarks>
    public static ref readonly Operator AddressOf => ref OperatorsIntl[((int)OperatorType.AddressOf >> 3) - 1];

    /// <summary>
    /// <para><c>~value</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> ~(<see langword="T"/> v)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    public static ref readonly Operator OnesComplement => ref OperatorsIntl[((int)OperatorType.OnesComplement >> 3) - 1];

    /// <summary>
    /// <para><c>*value</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Unary</remarks>
    public static ref readonly Operator PointerDereference => ref OperatorsIntl[((int)OperatorType.PointerDereference >> 3) - 1];

    /// <summary>
    /// <para><c>left + right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> +(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    public static ref readonly Operator Addition => ref OperatorsIntl[((int)OperatorType.Addition >> 3) - 1];

    /// <summary>
    /// <para><c>left - right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> -(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    public static ref readonly Operator Subtraction => ref OperatorsIntl[((int)OperatorType.Subtraction >> 3) - 1];

    /// <summary>
    /// <para><c>left * right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> *(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    public static ref readonly Operator Multiply => ref OperatorsIntl[((int)OperatorType.Multiply >> 3) - 1];

    /// <summary>
    /// <para><c>left / right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> /(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    public static ref readonly Operator Division => ref OperatorsIntl[((int)OperatorType.Division >> 3) - 1];

    /// <summary>
    /// <para><c>left % right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> %(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator Modulus => ref OperatorsIntl[((int)OperatorType.Modulus >> 3) - 1];

    /// <summary>
    /// <para><c>left ^ right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> ^(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator ExclusiveOr => ref OperatorsIntl[((int)OperatorType.ExclusiveOr >> 3) - 1];

    /// <summary>
    /// <para><c>left &amp; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &amp;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator BitwiseAnd => ref OperatorsIntl[((int)OperatorType.BitwiseAnd >> 3) - 1];

    /// <summary>
    /// <para><c>left | right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> |(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator BitwiseOr => ref OperatorsIntl[((int)OperatorType.BitwiseOr >> 3) - 1];

    /// <summary>
    /// <para><c>left &amp;&amp; right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator LogicalAnd => ref OperatorsIntl[((int)OperatorType.LogicalAnd >> 3) - 1];

    /// <summary>
    /// <para><c>left || right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator LogicalOr => ref OperatorsIntl[((int)OperatorType.LogicalOr >> 3) - 1];

    /// <summary>
    /// <para><c>left = right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator Assign => ref OperatorsIntl[((int)OperatorType.Assign >> 3) - 1];

    /// <summary>
    /// <para><c>left &lt;&lt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &lt;&lt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator LeftShift => ref OperatorsIntl[((int)OperatorType.LeftShift >> 3) - 1];

    /// <summary>
    /// <para><c>left &gt;&gt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &gt;&gt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator RightShift => ref OperatorsIntl[((int)OperatorType.RightShift >> 3) - 1];

    /// <summary>
    /// <para><c>left &gt;&gt; right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator SignedRightShift => ref OperatorsIntl[((int)OperatorType.SignedRightShift >> 3) - 1];

    /// <summary>
    /// <para><c>left &gt;&gt;&gt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &gt;&gt;&gt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator UnsignedRightShift => ref OperatorsIntl[((int)OperatorType.UnsignedRightShift >> 3) - 1];

    /// <summary>
    /// <para><c>left == right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> ==(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator Equality => ref OperatorsIntl[((int)OperatorType.Equality >> 3) - 1];

    /// <summary>
    /// <para><c>left &gt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &gt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator GreaterThan => ref OperatorsIntl[((int)OperatorType.GreaterThan >> 3) - 1];

    /// <summary>
    /// <para><c>left &lt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &lt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator LessThan => ref OperatorsIntl[((int)OperatorType.LessThan >> 3) - 1];

    /// <summary>
    /// <para><c>left != right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> !=(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator Inequality => ref OperatorsIntl[((int)OperatorType.Inequality >> 3) - 1];

    /// <summary>
    /// <para><c>left &gt;= right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &gt;=(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator GreaterThanOrEqual => ref OperatorsIntl[((int)OperatorType.GreaterThanOrEqual >> 3) - 1];

    /// <summary>
    /// <para><c>left &lt;= right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &lt;=(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator LessThanOrEqual => ref OperatorsIntl[((int)OperatorType.LessThanOrEqual >> 3) - 1];

    /// <summary>
    /// <para><c>left &gt;&gt;&gt;= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator UnsignedRightShiftAssignment => ref OperatorsIntl[((int)OperatorType.UnsignedRightShiftAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left-&gt;right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator MemberSelection => ref OperatorsIntl[((int)OperatorType.MemberSelection >> 3) - 1];

    /// <summary>
    /// <para><c>left &gt;&gt;= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator RightShiftAssignment => ref OperatorsIntl[((int)OperatorType.RightShiftAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left *= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    public static ref readonly Operator MultiplicationAssignment => ref OperatorsIntl[((int)OperatorType.MultiplicationAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left-&gt;*right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator PointerToMemberSelection => ref OperatorsIntl[((int)OperatorType.PointerToMemberSelection >> 3) - 1];

    /// <summary>
    /// <para><c>left -= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    public static ref readonly Operator SubtractionAssignment => ref OperatorsIntl[((int)OperatorType.SubtractionAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left ^= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator ExclusiveOrAssignment => ref OperatorsIntl[((int)OperatorType.ExclusiveOrAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left &lt;&lt;= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator LeftShiftAssignment => ref OperatorsIntl[((int)OperatorType.LeftShiftAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left %= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator ModulusAssignment => ref OperatorsIntl[((int)OperatorType.ModulusAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left += right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    public static ref readonly Operator AdditionAssignment => ref OperatorsIntl[((int)OperatorType.AdditionAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left &amp;= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator BitwiseAndAssignment => ref OperatorsIntl[((int)OperatorType.BitwiseAndAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left |= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator BitwiseOrAssignment => ref OperatorsIntl[((int)OperatorType.BitwiseOrAssignment >> 3) - 1];

    /// <summary>
    /// <para><c>left, right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    public static ref readonly Operator Comma => ref OperatorsIntl[((int)OperatorType.Comma >> 3) - 1];

    /// <summary>
    /// <para><c>left /= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    public static ref readonly Operator DivisionAssignment => ref OperatorsIntl[((int)OperatorType.DivisionAssignment >> 3) - 1];

    /// <summary>
    /// Get info about an operator from it's enum value.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Not a valid operator type.</exception>
    public static ref readonly Operator GetOperator(OperatorType type)
    {
        if ((int)type <= 0b111 || type > MaxOperatorType)
            throw new ArgumentOutOfRangeException(nameof(type));

        return ref OperatorsIntl[((int)type >> 3) - 1];
    }

    /// <summary>
    /// Look for an operator of type <paramref name="op"/> where all parameters are <typeparamref name="TDeclaringType"/>.
    /// </summary>
    /// <param name="preferCheckedOperator">If there is a <see langword="checked"/> variant of an operator, return that one instead.</param>
    /// <exception cref="ArgumentOutOfRangeException">Not a valid operator type.</exception>
    public static MethodInfo? Find<TDeclaringType>(OperatorType op, bool preferCheckedOperator = false)
    {
        return Find(typeof(TDeclaringType), op, preferCheckedOperator);
    }

    /// <summary>
    /// Look for an operator of type <paramref name="op"/> where all parameters are <paramref name="declaringType"/>.
    /// </summary>
    /// <param name="preferCheckedOperator">If there is a <see langword="checked"/> variant of an operator, return that one instead.</param>
    /// <exception cref="ArgumentOutOfRangeException">Not a valid operator type.</exception>
    public static MethodInfo? Find(Type declaringType, OperatorType op, bool preferCheckedOperator = false)
    {
        Operator opInfo = GetOperator(op);

        string methodName = opInfo.GetMethodName(preferCheckedOperator);

        return declaringType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
            null,
            CallingConventions.Any,
            opInfo.IsUnary ? [ declaringType ] : [ declaringType, declaringType ],
            null
        );
    }

    /// <summary>
    /// Look for a binary operator of type <paramref name="op"/> where the first argument is <typeparamref name="TLeft"/> and the second is <typeparamref name="TRight"/>.
    /// </summary>
    /// <remarks>In the event of an ambiguous result, the operator declared in <typeparamref name="TLeft"/> will be returned.</remarks>
    /// <param name="preferCheckedOperator">If there is a <see langword="checked"/> variant of an operator, return that one instead.</param>
    /// <exception cref="ArgumentOutOfRangeException">Not a valid operator type.</exception>
    public static MethodInfo? Find<TLeft, TRight>(OperatorType op, bool preferCheckedOperator = false)
    {
        return Find(typeof(TLeft), op, typeof(TRight), preferCheckedOperator);
    }

    /// <summary>
    /// Look for a binary operator of type <paramref name="op"/> where the first argument is <paramref name="left"/> and the second is <paramref name="right"/>.
    /// </summary>
    /// <remarks>In the event of an ambiguous result, the operator declared in <paramref name="left"/> will be returned.</remarks>
    /// <param name="preferCheckedOperator">If there is a <see langword="checked"/> variant of an operator, return that one instead.</param>
    /// <exception cref="ArgumentOutOfRangeException">Not a valid operator type.</exception>
    public static MethodInfo? Find(Type left, OperatorType op, Type right, bool preferCheckedOperator = false)
    {
        Operator opInfo = GetOperator(op);

        string methodName = opInfo.GetMethodName(preferCheckedOperator);

        MethodInfo? first = left.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
            null,
            CallingConventions.Any,
            [left, right],
            null
        );

        if (first != null)
            return first;

        return right.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
            null,
            CallingConventions.Any,
            [left, right],
            null
        );
    }

    /// <summary>
    /// Look for a conversion operator that converts from a value of type <typeparamref name="TFrom"/> to a value of type <typeparamref name="TTo"/>.
    /// </summary>
    /// <remarks>In the event of an ambiguous result, the first implicit operator is returned, then the operator declared in <typeparamref name="TFrom"/> will be returned.</remarks>
    /// <param name="preferCheckedOperator">If there is a <see langword="checked"/> variant of an operator, return that one instead.</param>
    /// <exception cref="ArgumentOutOfRangeException">Not a valid operator type.</exception>
    public static MethodInfo? FindCast<TFrom, TTo>(bool preferCheckedOperator = false)
    {
        return FindCast(typeof(TFrom), typeof(TTo), preferCheckedOperator);
    }

    /// <summary>
    /// Look for a conversion operator that converts from a value of type <paramref name="from"/> to a value of type <paramref name="to"/>.
    /// </summary>
    /// <remarks>In the event of an ambiguous result, the first implicit operator is returned, then the operator declared in <paramref name="from"/> will be returned.</remarks>
    /// <param name="preferCheckedOperator">If there is a <see langword="checked"/> variant of an operator, return that one instead.</param>
    /// <exception cref="ArgumentOutOfRangeException">Not a valid operator type.</exception>
    public static MethodInfo? FindCast(Type from, Type to, bool preferCheckedOperator = false)
    {
        MethodInfo[] fromMethods = from.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        MethodInfo[] toMethods = to.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        MethodInfo? candidate = null;
        int candidateOrder = 0;

        int ct = fromMethods.Length + toMethods.Length;
        for (int i = 0; i < ct; ++i)
        {
            bool isFromSub = i < fromMethods.Length;
            MethodInfo mtd = isFromSub ? fromMethods[i] : toMethods[i - fromMethods.Length];

            int order = GetIntlOrderingConversionMethod(mtd, from, to, isFromSub, preferCheckedOperator);

            if (order == 23)
                return mtd;

            if (candidateOrder < order)
                candidate = mtd;
        }

        return candidate;
    }

    internal static int GetIntlOrderingConversionMethod(MethodInfo mtd, Type from, Type to, bool isDeclaredInFromSub, bool preferCheckedOperator)
    {
        // cases:

        // find order, higher numbers are returned first
        // fromsub/tosub means the base type of 'from'/'to'

        // prefer checked:                              - order
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
        //  - fall through \/
        // dont prefer checked:
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
        //  - doesnt match                              - -1

        bool isExplicit, isChecked;
        if (preferCheckedOperator && mtd.Name.Equals(ExplicitCheckedConversionOperatorMethodName, StringComparison.Ordinal))
        {
            isExplicit = true;
            isChecked = true;
        }
        else if (mtd.Name.Equals(ImplicitConversionOperatorMethodName, StringComparison.Ordinal))
        {
            isExplicit = false;
            isChecked = false;
        }
        else if (mtd.Name.Equals(ExplicitConversionOperatorMethodName, StringComparison.Ordinal))
        {
            isExplicit = true;
            isChecked = false;
        }
        else return -1;

        if (!to.IsAssignableFrom(mtd.ReturnType))
            return -1;

        ParameterInfo[] parameters = mtd.GetParameters();
        if (parameters.Length != 1 || !parameters[0].ParameterType.IsAssignableFrom(from))
            return -1;

        bool isExactMatch = parameters[0].ParameterType == from && mtd.ReturnType == to;

        bool isFrom = mtd.DeclaringType == from;
        bool isTo = !isFrom && mtd.DeclaringType == to;

        // bit order (most to least significant)
        //  * is checked
        //  * are params and return type exact matches (instead of assignable matches)
        //  * is implicit
        //  * is declared in the 'from' type or one of it's base types
        //  * is declared in the 'from' or 'to' type

        return (isTo ? 1 : 0) | (isFrom ? 1 : 0) | ((isDeclaredInFromSub ? 1 : 0) << 1) | ((isExplicit ? 0 : 1) << 2) |
               ((isExactMatch ? 1 : 0) << 3) | ((isChecked ? 1 : 0) << 4);
    }
}

/// <summary>
/// Represents information about a unary or binary operator type.
/// </summary>
public readonly struct Operator : IEquatable<Operator>, IEquatable<OperatorType>, IComparable<Operator>
{
    private readonly OperatorType _type;

    /// <summary>
    /// The enum type of this operator.
    /// </summary>
    public OperatorType Type => (OperatorType)(ushort)_type;

    /// <summary>
    /// The other operator that must also exist, if any.
    /// </summary>
    public OperatorType RequiredPair => (OperatorType)(((int)_type & 0x0FFFFFFF) >> 16);

    /// <summary>
    /// If the operator takes only one argument (which has to be the same as the type it's in).
    /// </summary>
    public bool IsUnary => (_type & OperatorType.UnaryMask) != 0;

    /// <summary>
    /// If the operator takes two arguments (one of which has to be the same as the type it's in).
    /// </summary>
    public bool IsBinary => (_type & OperatorType.BinaryMask) != 0;

    /// <summary>
    /// If the operator is able to define itself as checked.
    /// </summary>
    public bool CanDefineCheckedVariant => (_type & OperatorType.CheckableMask) != 0;

    /// <summary>
    /// The special name of the function for the operator.
    /// </summary>
    public string MethodName => Operators.MethodNamesIntl[((ushort)_type >> 3) - 1];

    /// <summary>
    /// Get the method name for this operator. If supported and <paramref name="preferChecked"/> is <see langword="true"/>, the checked name will be returned instead.
    /// </summary>
    public string GetMethodName(bool preferChecked)
    {
        if (!preferChecked || (_type & OperatorType.CheckableMask) == 0)
            return Operators.MethodNamesIntl[((ushort)_type >> 3) - 1];

        return Operators.CheckedMethodNamesIntl[(int)_type >>> 28];
    }

    internal Operator(OperatorType type, OperatorType requiredPair, int checkedNameIndex = 0)
    {
        _type = (OperatorType)((int)type | ((int)requiredPair << 16) | (checkedNameIndex << 28));
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj switch
    {
        Operator o => o._type == _type,
        OperatorType t => t == _type,
        _ => false
    };

    /// <inheritdoc />
    public override int GetHashCode() => (int)_type;

    /// <inheritdoc />
    public bool Equals(Operator other) => other._type == _type;

    /// <inheritdoc />
    public bool Equals(OperatorType other) => other == _type;

    /// <inheritdoc />
    public int CompareTo(Operator other)
    {
        return _type < other._type ? -1 : _type == other._type ? 0 : 1;
    }

    /// <summary>
    /// Check if two <see cref="Operator"/> values are equal.
    /// </summary>
    public static bool operator ==(Operator left, Operator right)
    {
        return left._type == right._type;
    }

    /// <summary>
    /// Check if two <see cref="Operator"/> values are not equal.
    /// </summary>
    public static bool operator !=(Operator left, Operator right)
    {
        return left._type == right._type;
    }
}

/// <summary>
/// Enum for all the values of <see cref="Operator"/>.
/// </summary>
[Flags]
public enum OperatorType
{
    /// <summary>
    /// No operator type.
    /// </summary>
    None = 0,

    /// <summary>
    /// All unary operators have this bit enabled.
    /// </summary>
    UnaryMask = 0b001,

    /// <summary>
    /// All binary operators have this bit enabled.
    /// </summary>
    BinaryMask = 0b010,

    /// <summary>
    /// All operators that can be defined as <see langword="checked"/> have this bit enabled.
    /// </summary>
    CheckableMask = 0b100,

    /* Unary Operators */

    /// <summary>
    /// <para><c>value--</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> --(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary, Checkable</remarks>
    Decrement = (1 << 3) | UnaryMask | CheckableMask,

    /// <summary>
    /// <para><c>value++</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> ++(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary, Checkable</remarks>
    Increment = (2 << 3) | UnaryMask | CheckableMask,

    /// <summary>
    /// <para><c>-value</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> -(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary, Checkable</remarks>
    UnaryNegation = (3 << 3) | UnaryMask | CheckableMask,

    /// <summary>
    /// <para><c>+value</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> +(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    UnaryPlus = (4 << 3) | UnaryMask,

    /// <summary>
    /// <para><c>!value</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> !(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    LogicalNot = (5 << 3) | UnaryMask,

    /// <summary>
    /// <c><see langword="static"/> <see cref="bool"/> <see langword="operator"/> <see langword="true"/>(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    True = (6 << 3) | UnaryMask,

    /// <summary>
    /// <c><see langword="static"/> <see cref="bool"/> <see langword="operator"/> <see langword="false"/>(<see langword="T"/> value)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    False = (7 << 3) | UnaryMask,

    /// <summary>
    /// <para><c>&amp;value</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Unary</remarks>
    AddressOf = (8 << 3) | UnaryMask,

    /// <summary>
    /// <para><c>~value</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> ~(<see langword="T"/> v)</c>
    /// </summary>
    /// <remarks>Unary</remarks>
    OnesComplement = (9 << 3) | UnaryMask,

    /// <summary>
    /// <para><c>*value</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Unary</remarks>
    PointerDereference = (10 << 3) | UnaryMask,

    /* Binary Operators */

    /// <summary>
    /// <para><c>left + right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> +(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    Addition = (11 << 3) | BinaryMask | CheckableMask,

    /// <summary>
    /// <para><c>left - right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> -(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    Subtraction = (12 << 3) | BinaryMask | CheckableMask,

    /// <summary>
    /// <para><c>left * right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> *(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    Multiply = (13 << 3) | BinaryMask | CheckableMask,

    /// <summary>
    /// <para><c>left / right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> /(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    Division = (14 << 3) | BinaryMask | CheckableMask,

    /// <summary>
    /// <para><c>left % right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> %(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    Modulus = (15 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left ^ right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> ^(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    ExclusiveOr = (16 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &amp; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &amp;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    BitwiseAnd = (17 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left | right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> |(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    BitwiseOr = (18 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &amp;&amp; right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    LogicalAnd = (19 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left || right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    LogicalOr = (20 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left = right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    Assign = (21 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &lt;&lt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &lt;&lt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    LeftShift = (22 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &gt;&gt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &gt;&gt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    RightShift = (23 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &gt;&gt; right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    SignedRightShift = (24 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &gt;&gt;&gt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &gt;&gt;&gt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    UnsignedRightShift = (25 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left == right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> ==(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    Equality = (26 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &gt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &gt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    GreaterThan = (27 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &lt; right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &lt;(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    LessThan = (28 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left != right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> !=(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    Inequality = (29 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &gt;= right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &gt;=(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    GreaterThanOrEqual = (30 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &lt;= right</c></para>
    /// <c><see langword="static"/> <see langword="T"/> <see langword="operator"/> &lt;=(<see langword="T"/> left, <see langword="T"/> right)</c>
    /// </summary>
    /// <remarks>Binary</remarks>
    LessThanOrEqual = (31 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &gt;&gt;&gt;= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    UnsignedRightShiftAssignment = (32 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left-&gt;right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    MemberSelection = (33 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &gt;&gt;= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    RightShiftAssignment = (34 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left *= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    MultiplicationAssignment = (35 << 3) | BinaryMask | CheckableMask,

    /// <summary>
    /// <para><c>left-&gt;*right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    PointerToMemberSelection = (36 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left -= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    SubtractionAssignment = (37 << 3) | BinaryMask | CheckableMask,

    /// <summary>
    /// <para><c>left ^= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    ExclusiveOrAssignment = (38 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left &lt;&lt;= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    LeftShiftAssignment = (39 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left %= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    ModulusAssignment = (40 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left += right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    AdditionAssignment = (41 << 3) | BinaryMask | CheckableMask,

    /// <summary>
    /// <para><c>left &amp;= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    BitwiseAndAssignment = (42 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left |= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    BitwiseOrAssignment = (43 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left, right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary</remarks>
    Comma = (44 << 3) | BinaryMask,

    /// <summary>
    /// <para><c>left /= right</c></para>
    /// This operator can not be overloaded in C#.
    /// </summary>
    /// <remarks>Binary, Checkable</remarks>
    DivisionAssignment = (45 << 3) | BinaryMask | CheckableMask
}