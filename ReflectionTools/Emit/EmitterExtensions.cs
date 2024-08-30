using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Emit;

/// <summary>
/// Contains extensions for common patterns with IL emitters.
/// </summary>
public static class EmitterExtensions
{
    // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
    private const MemoryAlignment MemoryMask = (MemoryAlignment)0b111;

    private static readonly MethodInfo TokenToTypeMtd = Accessor.GetMethod(Type.GetTypeFromHandle)!;
    private static readonly Type TypeI = typeof(nint);
    private static readonly Type TypeU = typeof(nuint);
    private static readonly Type TypeI1 = typeof(sbyte);
    private static readonly Type TypeU1 = typeof(byte);
    private static readonly Type TypeI2 = typeof(short);
    private static readonly Type TypeU2 = typeof(ushort);
    private static readonly Type TypeI4 = typeof(int);
    private static readonly Type TypeU4 = typeof(uint);
    private static readonly Type TypeI8 = typeof(long);
    private static readonly Type TypeU8 = typeof(ulong);
    private static readonly Type TypeR4 = typeof(float);
    private static readonly Type TypeR8 = typeof(double);

    private static void EmitAlignmentPrefix(IOpCodeEmitter emitter, MemoryAlignment alignment)
    {
        switch (alignment)
        {
            case MemoryAlignment.AlignedPerByte:
                emitter.Emit(OpCodes.Unaligned, (byte)1);
                break;
            
            case MemoryAlignment.AlignedPerTwoBytes:
                emitter.Emit(OpCodes.Unaligned, (byte)2);
                break;

            case MemoryAlignment.AlignedPerFourBytes:
                emitter.Emit(OpCodes.Unaligned, (byte)4);
                break;
        }
    }

    /// <summary>
    /// Loads the <see cref="Type"/> object of <typeparamref name="T"/> onto the stack.
    /// </summary>
    /// <remarks>... -&gt; ..., <see cref="Type"/></remarks>
    public static IOpCodeEmitter LoadTypeOf<T>(this IOpCodeEmitter emitter)
    {
        return emitter.LoadTypeOf(typeof(T));
    }

    /// <summary>
    /// Loads the <see cref="Type"/> object of <paramref name="type"/> onto the stack.
    /// </summary>
    /// <remarks>... -&gt; ..., <see cref="Type"/></remarks>
    public static IOpCodeEmitter LoadTypeOf(this IOpCodeEmitter emitter, Type type)
    {
        emitter.Emit(OpCodes.Ldtoken, type);
        emitter.Emit(OpCodes.Call, TokenToTypeMtd);
        
        return emitter;
    }

    /// <summary>
    /// Adds the top two values on the stack and pushes the result onto the stack.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 + value2</remarks>
    public static IOpCodeEmitter Add(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Add);
        return emitter;
    }

    /// <summary>
    /// Adds the top two values on the stack and pushes the result onto the stack, throwing an <see cref="OverflowException"/> if the operation will result in an overflow.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 + value2</remarks>
    public static IOpCodeEmitter AddChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Add_Ovf);
        return emitter;
    }

    /// <summary>
    /// Adds the top two unsigned values on the stack and pushes the result onto the stack, throwing an <see cref="OverflowException"/> if the operation will result in an overflow.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 + value2</remarks>
    public static IOpCodeEmitter AddUnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Add_Ovf_Un);
        return emitter;
    }

    /// <summary>
    /// Bitwise and's the top two values on the stack and pushes the result onto the stack.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 &amp; value2</remarks>
    public static IOpCodeEmitter OperateAnd(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.And);
        return emitter;
    }

    /// <summary>
    /// Pushes a pointer to the argument list for a function created with a VARARGS parameter.
    /// </summary>
    /// <remarks>... -&gt; ..., <see cref="RuntimeArgumentHandle"/></remarks>
    public static IOpCodeEmitter LoadArgList(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Arglist);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the two top values on the stack are equal.
    /// </summary>
    /// <param name="forceShort">Use <c>beq.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 == value2), ...</remarks>
    public static IOpCodeEmitter BranchIfEqual(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Beq_S : OpCodes.Beq, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top value is greater than or equal to the top value on the stack.
    /// </summary>
    /// <param name="forceShort">Use <c>bge.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &gt;= value2), ...</remarks>
    public static IOpCodeEmitter BranchIfGreaterOrEqual(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Bge_S : OpCodes.Bge, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top unsigned value is greater than or equal to the top unsigned value on the stack, or the second to top or both values are unordered (meaning <see langword="NaN"/>).
    /// </summary>
    /// <param name="forceShort">Use <c>bge.un.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &gt;= value2), ...</remarks>
    public static IOpCodeEmitter BranchIfGreaterOrEqualUnsigned(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Bge_Un_S : OpCodes.Bge_Un, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top value is greater than the top value on the stack.
    /// </summary>
    /// <param name="forceShort">Use <c>bgt.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &gt; value2), ...</remarks>
    public static IOpCodeEmitter BranchIfGreater(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Bge_S : OpCodes.Bge, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top unsigned value is greater than the top unsigned value on the stack, or the second to top value is unordered (meaning <see langword="NaN"/>).
    /// </summary>
    /// <param name="forceShort">Use <c>bgt.un.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &gt; value2), ...</remarks>
    public static IOpCodeEmitter BranchIfGreaterUnsigned(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Bge_Un_S : OpCodes.Bge_Un, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top value is less than or equal to the top value on the stack.
    /// </summary>
    /// <param name="forceShort">Use <c>ble.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &lt;= value2), ...</remarks>
    public static IOpCodeEmitter BranchIfLessOrEqual(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Ble_S : OpCodes.Ble, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top unsigned value is less than or equal to the top unsigned value on the stack, or the second to top or both values are unordered (meaning <see langword="NaN"/>).
    /// </summary>
    /// <param name="forceShort">Use <c>ble.un.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &lt;= value2), ...</remarks>
    public static IOpCodeEmitter BranchIfLessOrEqualUnsigned(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Ble_Un_S : OpCodes.Ble_Un, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top value is less than the top value on the stack.
    /// </summary>
    /// <param name="forceShort">Use <c>blt.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &lt; value2), ...</remarks>
    public static IOpCodeEmitter BranchIfLess(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Blt_S : OpCodes.Blt, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top unsigned value is less than the top unsigned value on the stack, or the second to top is unordered (meaning <see langword="NaN"/>).
    /// </summary>
    /// <param name="forceShort">Use <c>blt.un.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &lt; value2), ...</remarks>
    public static IOpCodeEmitter BranchIfLessUnsigned(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Blt_Un_S : OpCodes.Blt_Un, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the two top values on the stack are not equal or one value is unordered (meaning <see langword="NaN"/>).
    /// </summary>
    /// <param name="forceShort">Use <c>bne.un.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 != value2), ...</remarks>
    public static IOpCodeEmitter BranchIfNotEqual(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Bne_Un_S : OpCodes.Bne_Un, destination);
        return emitter;
    }

    /// <summary>
    /// Converts the top value type on the stack to a boxed value (moving it to the heap) and pushes a reference type onto the stack.
    /// </summary>
    /// <remarks>..., <see langword="struct"/> -&gt; ..., <see cref="object"/></remarks>
    public static IOpCodeEmitter Box(this IOpCodeEmitter emitter, Type valueType)
    {
        emitter.Emit(OpCodes.Box, valueType);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label unconditionally.
    /// </summary>
    /// <param name="forceShort">Use <c>br.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>... -&gt; (branch), ...</remarks>
    public static IOpCodeEmitter Branch(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Br_S : OpCodes.Br, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the top value on the stack is <see langword="false"/>, zero, or <see langword="null"/>.
    /// </summary>
    /// <param name="forceShort">Use <c>brfalse.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value -&gt; (branch if !value), ...</remarks>
    public static IOpCodeEmitter BranchIfFalse(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Brfalse_S : OpCodes.Brfalse, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the top value on the stack is <see langword="true"/>, not zero, or not <see langword="null"/>.
    /// </summary>
    /// <param name="forceShort">Use <c>brtrue.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value -&gt; (branch if value), ...</remarks>
    public static IOpCodeEmitter BranchIfTrue(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Brtrue_S : OpCodes.Brtrue, destination);
        return emitter;
    }

    /// <summary>
    /// Invokes the given method, removing all parameters from the stack and pushing the return value if it isn't <see langword="void"/>.
    /// </summary>
    /// <param name="forceValueTypeCallvirt">Use <c>callvirt</c> for methods declared in value types. This will throw an error unless the method is called on a boxed value type.</param>
    /// <param name="constrainingType">The type to constrain the <c>callvirt</c> invocation to. Allows virtually calling functions on value types or reference types. This parameter does nothing if the function is static or can't be <c>callvirt</c>'d.</param>
    /// <remarks>..., (parameters) -&gt; (return value if not void), ...</remarks>
    public static IOpCodeEmitter Invoke(this IOpCodeEmitter emitter, MethodInfo method, bool forceValueTypeCallvirt = false, Type? constrainingType = null)
    {
        bool callvirt = !method.IsStatic && (forceValueTypeCallvirt || method.DeclaringType is { IsValueType: false });
        if (callvirt && constrainingType != null)
        {
            emitter.Emit(OpCodes.Constrained, constrainingType);
            emitter.Emit(OpCodes.Callvirt, method);
        }
        else
        {
            emitter.Emit(callvirt ? OpCodes.Callvirt : OpCodes.Call, method);
        }
        return emitter;
    }

    /// <summary>
    /// Invokes the given static method with no parameters or return value.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="simpleCall"/> is not <see langword="static"/>.</exception>
    /// <exception cref="MemberAccessException">The caller does not have access to the method represented by the delegate (for example, if the method is private).</exception>
    /// <remarks>... -&gt; ...</remarks>
    public static IOpCodeEmitter Invoke(this IOpCodeEmitter emitter, Action simpleCall)
    {
        if (simpleCall.Target is not null)
            throw new ArgumentException("Expected a static function.");

        emitter.Emit(OpCodes.Call, simpleCall.Method);
        return emitter;
    }

    /// <summary>
    /// Asserts that the reference type on the stack is assignable to type <typeparamref name="T"/>, throwing a <see cref="InvalidCastException"/> if not.
    /// </summary>
    /// <remarks>..., <see cref="object"/> -&gt; ..., <typeparamref name="T"/></remarks>
    public static IOpCodeEmitter CastReference<T>(this IOpCodeEmitter emitter)
    {
        return emitter.CastReference(typeof(T));
    }

    /// <summary>
    /// Asserts that the reference type on the stack is assignable to type <paramref name="type"/>, throwing a <see cref="InvalidCastException"/> if not.
    /// </summary>
    /// <remarks>..., <see cref="object"/> -&gt; ..., <paramref name="type"/></remarks>
    public static IOpCodeEmitter CastReference(this IOpCodeEmitter emitter, Type type)
    {
        emitter.Emit(OpCodes.Castclass, type);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the two top values on the stack are equal as a 0 or 1.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 == value2), ...</remarks>
    public static IOpCodeEmitter LoadIfEqual(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Ceq);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the second to top value is greater than the top value on the stack as a 0 or 1.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 &gt; value2), ...</remarks>
    public static IOpCodeEmitter LoadIfGreater(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Cgt);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the second to top unsigned value is greater than the top unsigned value on the stack, or the second to top value is unordered (meaning <see langword="NaN"/>) as a 0 or 1.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 &gt; value2), ...</remarks>
    public static IOpCodeEmitter LoadIfGreaterUnsigned(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Cgt_Un);
        return emitter;
    }

    /// <summary>
    /// Throws an <see cref="ArithmeticException"/> if value is <see langword="NaN"/>, <see langword="-Infinity"/>, or <see langword="+Infinity"/>, leaving the value on the stack. Behavior is unspecified if the value isn't a floating point value. This won't work with <see cref="decimal"/> values.
    /// </summary>
    /// <remarks>..., value -&gt; value, ...</remarks>
    public static IOpCodeEmitter CheckFinite(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Ckfinite);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the second to top value is less than the top value on the stack as a 0 or 1.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 &lt; value2), ...</remarks>
    public static IOpCodeEmitter LoadIfLess(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Clt);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the second to top unsigned value is less than the top unsigned value on the stack, or the second to top value is unordered (meaning <see langword="NaN"/>) as a 0 or 1.
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 &lt; value2), ...</remarks>
    public static IOpCodeEmitter LoadIfLessUnsigned(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Clt_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a native signed integer (<see langword="nint"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToNativeInt(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_I);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a signed 8-bit integer (<see langword="sbyte"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt8(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_I1);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a signed 16-bit integer (<see langword="short"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt16(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_I2);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a signed 32-bit integer (<see langword="int"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt32(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_I4);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a signed 64-bit integer (<see langword="long"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt64(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_I8);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a native unsigned integer (<see langword="nuint"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToNativeUInt(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_U);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a unsigned 8-bit integer (<see langword="byte"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt8(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_U1);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a unsigned 16-bit integer (<see langword="ushort"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt16(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_U2);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a unsigned 32-bit integer (<see langword="uint"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt32(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_U4);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a unsigned 64-bit integer (<see langword="ulong"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt64(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_U8);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a 32-bit floating-point value (<see langword="float"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToSingle(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_R4);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a 64-bit floating-point value (<see langword="double"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToDouble(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_R8);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a 64-bit floating-point value (<see langword="double"/>).
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToDoubleUnsigned(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_R_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a native signed integer (<see langword="nint"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToNativeIntChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a signed 8-bit integer (<see langword="sbyte"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt8Checked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I1);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a signed 16-bit integer (<see langword="short"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt16Checked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I2);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a signed 32-bit integer (<see langword="int"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt32Checked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I4);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a signed 64-bit integer (<see langword="long"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt64Checked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I8);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a native unsigned integer (<see langword="nuint"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToNativeUIntChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a unsigned 8-bit integer (<see langword="byte"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt8Checked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U1);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a unsigned 16-bit integer (<see langword="ushort"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt16Checked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U2);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a unsigned 32-bit integer (<see langword="uint"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt32Checked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U4);
        return emitter;
    }

    /// <summary>
    /// Convert the value on the stack to a unsigned 64-bit integer (<see langword="ulong"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt64Checked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U8);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a native signed integer (<see langword="nint"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToNativeIntUnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a signed 8-bit integer (<see langword="sbyte"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt8UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I1_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a signed 16-bit integer (<see langword="short"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt16UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I2_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a signed 32-bit integer (<see langword="int"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt32UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I4_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a signed 64-bit integer (<see langword="long"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToInt64UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_I8_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a native unsigned integer (<see langword="nuint"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToNativeUIntUnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a unsigned 8-bit integer (<see langword="byte"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt8UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U1_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a unsigned 16-bit integer (<see langword="ushort"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt16UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U2_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a unsigned 32-bit integer (<see langword="uint"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt32UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U4_Un);
        return emitter;
    }

    /// <summary>
    /// Convert the unsigned value on the stack to a unsigned 64-bit integer (<see langword="ulong"/>), throwing an <see cref="OverflowException"/> if the value can't be converted.
    /// <para>
    /// Converting larger integers to smaller integers disposes of the higher bits.
    /// Converting smaller integers to larger integers brings the sign bit to the first bit.
    /// Converting floating points to integers truncates towards zero.
    /// Converting large values to smaller floating points brings overflowing values to their corresponding <see langword="Infinity"/> values.
    /// </para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt64UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U8_Un);
        return emitter;
    }

    /// <summary>
    /// Copies some number of bytes from one address to another.
    /// </summary>
    /// <remarks>..., destinationAddress, sourceAddress, sizeInBytes -&gt; ...</remarks>
    public static IOpCodeEmitter CopyBytes(this IOpCodeEmitter emitter, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        EmitAlignmentPrefix(emitter, alignment);
        if (@volatile)
            emitter.Emit(OpCodes.Volatile);

        emitter.Emit(OpCodes.Cpblk);
        return emitter;
    }

    /// <summary>
    /// Copies the value type or class at one address to another address.
    /// Source type should be assignable to <paramref name="objectType"/> which should be assignable to destination type.
    /// If this is not the case, the behavior is undefined.
    /// </summary>
    /// <remarks>..., destinationAddress, sourceAddress -&gt; ...</remarks>
    public static IOpCodeEmitter CopyValue(this IOpCodeEmitter emitter, Type objectType)
    {
        emitter.Emit(OpCodes.Cpobj, objectType);
        return emitter;
    }

    /// <summary>
    /// Divides the top two values on the stack and pushes the result onto the stack. If both values are integers, the value will be truncated towards zero.
    /// <para>
    /// With floating point numbers:
    /// Dividing a number by zero will result in <see langword="Infinity"/> with the sign of the dividend.
    /// Dividing zero by zero or <see langword="Infinity"/> by <see langword="Infinity"/> will result in <see langword="NaN"/>.
    /// Dividing a number by <see langword="Infinity"/> will result in 0.
    /// </para>
    /// <para>
    /// With integer numbers:
    /// Dividing a number by zero will result in a <see cref="DivideByZeroException"/>.
    /// It's possible for division to produce a <see cref="ArithmeticException"/> when the output can't be represented by the operation.
    /// </para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 / value2</remarks>
    public static IOpCodeEmitter Divide(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Div);
        return emitter;
    }

    /// <summary>
    /// Divides the top two unsigned values on the stack and pushes the result onto the stack. The value will be truncated towards zero.
    /// <para>
    /// Dividing a number by zero will result in a <see cref="DivideByZeroException"/>.
    /// </para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 / value2</remarks>
    public static IOpCodeEmitter DivideUnsigned(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Div_Un);
        return emitter;
    }

    /// <summary>
    /// Duplicates the top value on the stack.
    /// </summary>
    /// <remarks>..., value -&gt; ..., value, value</remarks>
    public static IOpCodeEmitter Duplicate(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Dup);
        return emitter;
    }

    /// <summary>
    /// Duplicates the top value on the stack <paramref name="times"/> number of times.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> was less than 0.</exception>
    /// <remarks>..., value -&gt; ..., value, value</remarks>
    public static IOpCodeEmitter Duplicate(this IOpCodeEmitter emitter, int times)
    {
        if (times < 0)
            throw new ArgumentOutOfRangeException(nameof(times), "Expected at least 0 times.");

        for (int i = 0; i < times; ++i)
        {
            emitter.Emit(OpCodes.Dup);
        }

        return emitter;
    }

    /// <summary>
    /// Sets a number of bytes to a single value.
    /// </summary>
    /// <remarks>..., destinationAddress, value, sizeInBytes -&gt; ...</remarks>
    public static IOpCodeEmitter SetBytes(this IOpCodeEmitter emitter, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        EmitAlignmentPrefix(emitter, alignment);
        if (@volatile)
            emitter.Emit(OpCodes.Volatile);

        emitter.Emit(OpCodes.Initblk);
        return emitter;
    }

    /// <summary>
    /// Sets the default value of a type to a given address. Value types will be initialized to <see langword="default"/>, and reference types to <see langword="null"/>.
    /// </summary>
    /// <remarks>..., destinationAddress -&gt; ...</remarks>
    public static IOpCodeEmitter SetDefaultValue(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Initobj);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the type on the stack is assignable to type <typeparamref name="T"/> as a 0 or 1.
    /// <para>
    /// If <typeparamref name="T"/> is a value type, it should be in it's boxed form before <c>isinst</c> is emitted.
    /// </para>
    /// </summary>
    /// <remarks>..., <see cref="object"/> -&gt; ..., <typeparamref name="T"/></remarks>
    public static IOpCodeEmitter Is<T>(this IOpCodeEmitter emitter)
    {
        return emitter.Is(typeof(T));
    }

    /// <summary>
    /// Asserts that the reference type on the stack is assignable to type <paramref name="type"/>, throwing a <see cref="InvalidCastException"/> if not.
    /// <para>
    /// If <paramref name="type"/> is a value type, it should be in it's boxed form before <c>isinst</c> is emitted.
    /// </para>
    /// </summary>
    /// <remarks>..., <see cref="object"/> -&gt; ..., <paramref name="type"/></remarks>
    public static IOpCodeEmitter Is(this IOpCodeEmitter emitter, Type type)
    {
        emitter.Emit(OpCodes.Isinst, type);
        return emitter;
    }

    /// <summary>
    /// Transfers execution of the current method to <paramref name="method"/>. The stack must be empty and any parameters of the current method are transferred to <paramref name="method"/>.
    /// </summary>
    /// <remarks>... -&gt; (jump to <paramref name="method"/>) ...</remarks>
    public static IOpCodeEmitter JumpTo(this IOpCodeEmitter emitter, MethodInfo method)
    {
        emitter.Emit(OpCodes.Jmp, method);
        return emitter;
    }

    /// <summary>
    /// Load an argument by it's zero-based index. Note that on non-static methods, 0 denotes the <see langword="this"/> argument.
    /// </summary>
    /// <remarks>... -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadArgument(this IOpCodeEmitter emitter, ushort index)
    {
        switch (index)
        {
            case 0:
                emitter.Emit(OpCodes.Ldarg_0);
                break;

            case 1:
                emitter.Emit(OpCodes.Ldarg_1);
                break;

            case 2:
                emitter.Emit(OpCodes.Ldarg_2);
                break;

            case 3:
                emitter.Emit(OpCodes.Ldarg_3);
                break;

            case <= byte.MaxValue:
                emitter.Emit(OpCodes.Ldarg_S, (byte)index);
                break;

            default:
                emitter.Emit(OpCodes.Ldarg, index);
                break;
        }

        return emitter;
    }

    /// <summary>
    /// Load an argument's address by it's zero-based index. Note that on non-static methods, 0 denotes the <see langword="this"/> argument.
    /// </summary>
    /// <remarks>... -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadArgumentAddress(this IOpCodeEmitter emitter, ushort index)
    {
        switch (index)
        {
            case <= byte.MaxValue:
                emitter.Emit(OpCodes.Ldarga_S, (byte)index);
                break;

            default:
                emitter.Emit(OpCodes.Ldarga, index);
                break;
        }

        return emitter;
    }

    /// <summary>
    /// Load a constant 8-bit signed integer.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantInt8(this IOpCodeEmitter emitter, sbyte value)
    {
        return emitter.LoadConstantInt32(value)
                      .ConvertToInt8();
    }
    
    /// <summary>
    /// Load a constant 8-bit unsigned integer.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantUInt8(this IOpCodeEmitter emitter, byte value)
    {
        return emitter.LoadConstantInt32(value)
                      .ConvertToUInt8();
    }
    
    /// <summary>
    /// Load a constant 16-bit signed integer.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantInt16(this IOpCodeEmitter emitter, short value)
    {
        return emitter.LoadConstantInt32(value)
                      .ConvertToInt16();
    }
    
    /// <summary>
    /// Load a constant 16-bit unsigned integer.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantUInt16(this IOpCodeEmitter emitter, ushort value)
    {
        return emitter.LoadConstantInt32(value)
                      .ConvertToUInt16();
    }
    
    /// <summary>
    /// Load a constant 32-bit unsigned integer.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantUInt32(this IOpCodeEmitter emitter, uint value)
    {
        return emitter.LoadConstantInt32(unchecked( (int)value ))
                      .ConvertToUInt32();
    }

    /// <summary>
    /// Load a constant 32-bit signed integer.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantInt32(this IOpCodeEmitter emitter, int value)
    {
        switch (value)
        {
            case -1:
                emitter.Emit(OpCodes.Ldc_I4_M1);
                break;

            case 0:
                emitter.Emit(OpCodes.Ldc_I4_0);
                break;
                
            case 1:
                emitter.Emit(OpCodes.Ldc_I4_1);
                break;
                
            case 2:
                emitter.Emit(OpCodes.Ldc_I4_2);
                break;
                
            case 3:
                emitter.Emit(OpCodes.Ldc_I4_3);
                break;
                
            case 4:
                emitter.Emit(OpCodes.Ldc_I4_4);
                break;
                
            case 5:
                emitter.Emit(OpCodes.Ldc_I4_5);
                break;
                
            case 6:
                emitter.Emit(OpCodes.Ldc_I4_6);
                break;
                
            case 7:
                emitter.Emit(OpCodes.Ldc_I4_7);
                break;
                
            case 8:
                emitter.Emit(OpCodes.Ldc_I4_8);
                break;
                
            case <= byte.MaxValue:
                emitter.Emit(OpCodes.Ldc_I4_S, value);
                break;

            default:
                emitter.Emit(OpCodes.Ldc_I4, value);
                break;
        }

        return emitter;
    }
    
    /// <summary>
    /// Load a constant 64-bit unsigned integer.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantUInt64(this IOpCodeEmitter emitter, ulong value)
    {
        return emitter.LoadConstantInt64(unchecked( (long)value ))
                      .ConvertToUInt64();
    }

    /// <summary>
    /// Load a constant 64-bit signed integer.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantInt64(this IOpCodeEmitter emitter, long value)
    {
        emitter.Emit(OpCodes.Ldc_I8, value);
        return emitter;
    }

    /// <summary>
    /// Load a constant 32-bit floating point value.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantSingle(this IOpCodeEmitter emitter, float value)
    {
        emitter.Emit(OpCodes.Ldc_R4, value);
        return emitter;
    }

    /// <summary>
    /// Load a constant 64-bit floating point value.
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantDouble(this IOpCodeEmitter emitter, double value)
    {
        emitter.Emit(OpCodes.Ldc_R8, value);
        return emitter;
    }

    /// <summary>
    /// Load an element at the given index in an array.
    /// </summary>
    /// <remarks>..., array, index -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadArrayElement(this IOpCodeEmitter emitter, Type elementType, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        if (@volatile || (alignment & MemoryMask) != 0)
        {
            // volatile or unaligned values have to be loaded by address
            emitter.Emit(OpCodes.Ldelema, elementType);
            return emitter.LoadAddressValue(elementType, alignment, @volatile);
        }

        if (elementType == TypeI4)
        {
            emitter.Emit(OpCodes.Ldelem_I4);
        }
        else if (elementType == TypeU1)
        {
            emitter.Emit(OpCodes.Ldelem_U1);
        }
        else if (elementType == TypeU4)
        {
            emitter.Emit(OpCodes.Ldelem_U4);
        }
        else if (elementType == TypeR4)
        {
            emitter.Emit(OpCodes.Ldelem_R4);
        }
        else if (elementType == TypeR8)
        {
            emitter.Emit(OpCodes.Ldelem_R8);
        }
        else if (elementType == TypeI8 || elementType == TypeU8)
        {
            emitter.Emit(OpCodes.Ldelem_I8);
        }
        else if (elementType == TypeI1)
        {
            emitter.Emit(OpCodes.Ldelem_I1);
        }
        else if (elementType == TypeI || elementType == TypeU)
        {
            emitter.Emit(OpCodes.Ldelem_I);
        }
        else if (elementType == TypeI2)
        {
            emitter.Emit(OpCodes.Ldelem_I2);
        }
        else if (elementType == TypeU2)
        {
            emitter.Emit(OpCodes.Ldelem_U2);
        }
        else if (elementType.IsValueType)
        {
            emitter.Emit(OpCodes.Ldelem, elementType);
        }
        else
        {
            emitter.Emit(OpCodes.Ldelem_Ref);
        }

        return emitter;
    }

    /// <summary>
    /// Load the address of an element at the given index in an array.
    /// </summary>
    /// <remarks>..., array, index -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadArrayElementAddress(this IOpCodeEmitter emitter, Type elementType)
    {
        emitter.Emit(OpCodes.Ldelema, elementType);
        return emitter;
    }

    /// <summary>
    /// Load the value at the given address.
    /// </summary>
    /// <remarks>..., address -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadAddressValue(this IOpCodeEmitter emitter, Type elementType, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        EmitAlignmentPrefix(emitter, alignment);
        if (@volatile)
            emitter.Emit(OpCodes.Volatile);

        if (elementType == TypeI4)
        {
            emitter.Emit(OpCodes.Ldind_I4);
        }
        else if (elementType == TypeU1)
        {
            emitter.Emit(OpCodes.Ldind_U1);
        }
        else if (elementType == TypeU4)
        {
            emitter.Emit(OpCodes.Ldind_U4);
        }
        else if (elementType == TypeR4)
        {
            emitter.Emit(OpCodes.Ldind_R4);
        }
        else if (elementType == TypeR8)
        {
            emitter.Emit(OpCodes.Ldind_R8);
        }
        else if (elementType == TypeI8 || elementType == TypeU8)
        {
            emitter.Emit(OpCodes.Ldind_I8);
        }
        else if (elementType == TypeI1)
        {
            emitter.Emit(OpCodes.Ldind_I1);
        }
        else if (elementType == TypeI || elementType == TypeU)
        {
            emitter.Emit(OpCodes.Ldind_I);
        }
        else if (elementType == TypeI2)
        {
            emitter.Emit(OpCodes.Ldind_I2);
        }
        else if (elementType == TypeU2)
        {
            emitter.Emit(OpCodes.Ldind_U2);
        }
        else if (elementType.IsValueType)
        {
            emitter.Emit(OpCodes.Ldobj, elementType);
        }
        else
        {
            emitter.Emit(OpCodes.Ldind_Ref);
        }

        return emitter;
    }

    /// <summary>
    /// Load the value at the given instance field, removing the instance from the top of the stack.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="field"/> is a <see langword="static"/> field.</exception>
    /// <remarks>..., instance -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadInstanceFieldValue(this IOpCodeEmitter emitter, FieldInfo field, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        if (field.IsStatic)
            throw new ArgumentException("Expected instance field.", nameof(field));

        EmitAlignmentPrefix(emitter, alignment);
        if (@volatile)
            emitter.Emit(OpCodes.Volatile);

        emitter.Emit(OpCodes.Ldfld, field);
        return emitter;
    }

    /// <summary>
    /// Load the value at the given static field.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="field"/> is not a <see langword="static"/> field.</exception>
    /// <remarks>... -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadStaticFieldValue(this IOpCodeEmitter emitter, FieldInfo field, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        if (!field.IsStatic)
            throw new ArgumentException("Expected static field.", nameof(field));

        EmitAlignmentPrefix(emitter, alignment);
        if (@volatile)
            emitter.Emit(OpCodes.Volatile);

        emitter.Emit(OpCodes.Ldsfld, field);
        return emitter;
    }

    /// <summary>
    /// Load the address of the given instance field, removing the instance from the top of the stack.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="field"/> is a <see langword="static"/> field.</exception>
    /// <remarks>..., instance -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadInstanceFieldAddress(this IOpCodeEmitter emitter, FieldInfo field)
    {
        if (field.IsStatic)
            throw new ArgumentException("Expected instance field.", nameof(field));

        emitter.Emit(OpCodes.Ldflda, field);
        return emitter;
    }

    /// <summary>
    /// Load the address of the given static field.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="field"/> is not a <see langword="static"/> field.</exception>
    /// <remarks>... -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadStaticFieldAddress(this IOpCodeEmitter emitter, FieldInfo field)
    {
        if (!field.IsStatic)
            throw new ArgumentException("Expected static field.", nameof(field));

        emitter.Emit(OpCodes.Ldsflda, field);
        return emitter;
    }

    /// <summary>
    /// Load the function pointer to a method.
    /// </summary>
    /// <remarks>... -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadFunctionPointer(this IOpCodeEmitter emitter, MethodInfo method)
    {
        emitter.Emit(OpCodes.Ldftn, method);
        return emitter;
    }

    /// <summary>
    /// Load the function pointer to the implemented method of a virtual or abstract method on an instace.
    /// </summary>
    /// <remarks>..., instance -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadFunctionPointerVirtual(this IOpCodeEmitter emitter, MethodInfo method)
    {
        emitter.Emit(OpCodes.Ldftn, method);
        return emitter;
    }
}

/// <summary>
/// Describes if and how the <c>unaligned.</c> prefix is emitted.
/// </summary>
public enum MemoryAlignment : byte
{
    /// <summary>
    /// Data is not unaligned (aligned to the current architecture.
    /// </summary>
    AlignedNative = 0,

    /// <summary>
    /// Data is byte-aligned.
    /// </summary>
    AlignedPerByte = 1,

    /// <summary>
    /// Data is double-byte-aligned.
    /// </summary>
    AlignedPerTwoBytes = 2,

    /// <summary>
    /// Data is quad-byte-aligned.
    /// </summary>
    AlignedPerFourBytes = 4
}