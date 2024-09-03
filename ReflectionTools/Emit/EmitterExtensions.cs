using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using DanielWillett.ReflectionTools.Formatting;

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
    /// <para><c>ldtoken <typeparamref name="T"/></c> then <c>call <see cref="Type.GetTypeFromHandle"/></c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <see cref="Type"/></remarks>
    public static IOpCodeEmitter LoadTypeOf<T>(this IOpCodeEmitter emitter)
    {
        return emitter.LoadTypeOf(typeof(T));
    }

    /// <summary>
    /// Loads the <see cref="Type"/> object of <paramref name="type"/> onto the stack.
    /// <para><c>ldtoken <paramref name="type"/></c> then <c>call <see cref="Type.GetTypeFromHandle"/></c></para>
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
    /// <para><c>add</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 + value2</remarks>
    public static IOpCodeEmitter Add(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Add);
        return emitter;
    }

    /// <summary>
    /// Adds the top two values on the stack and pushes the result onto the stack, throwing an <see cref="OverflowException"/> if the operation will result in an overflow.
    /// <para><c>add.ovf</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 + value2</remarks>
    public static IOpCodeEmitter AddChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Add_Ovf);
        return emitter;
    }

    /// <summary>
    /// Adds the top two unsigned values on the stack and pushes the result onto the stack, throwing an <see cref="OverflowException"/> if the operation will result in an overflow.
    /// <para><c>add.ovf.un</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 + value2</remarks>
    public static IOpCodeEmitter AddUnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Add_Ovf_Un);
        return emitter;
    }

    /// <summary>
    /// Bitwise and's the top two values on the stack and pushes the result onto the stack.
    /// <para><c>and</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 &amp; value2</remarks>
    public static IOpCodeEmitter And(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.And);
        return emitter;
    }

    /// <summary>
    /// Pushes a pointer to the argument list for a function created with a VARARGS parameter. Same as the <see langword="__arglist"/> keyword in C#.
    /// <para><c>arglist</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <see cref="RuntimeArgumentHandle"/></remarks>
    public static IOpCodeEmitter LoadArgList(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Arglist);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the two top values on the stack are equal.
    /// <para><c>beq</c></para>
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
    /// <para><c>bge</c></para>
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
    /// <para><c>bge.un</c></para>
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
    /// <para><c>bgt</c></para>
    /// </summary>
    /// <param name="forceShort">Use <c>bgt.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &gt; value2), ...</remarks>
    public static IOpCodeEmitter BranchIfGreater(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Bgt_S : OpCodes.Bgt, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top unsigned value is greater than the top unsigned value on the stack, or the second to top value is unordered (meaning <see langword="NaN"/>).
    /// <para><c>bgt.un</c></para>
    /// </summary>
    /// <param name="forceShort">Use <c>bgt.un.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>..., value1, value2 -&gt; (branch if value1 &gt; value2), ...</remarks>
    public static IOpCodeEmitter BranchIfGreaterUnsigned(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Bgt_Un_S : OpCodes.Bgt_Un, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the second to top value is less than or equal to the top value on the stack.
    /// <para><c>ble</c></para>
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
    /// <para><c>ble.un</c></para>
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
    /// <para><c>blt</c></para>
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
    /// <para><c>blt.un</c></para>
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
    /// <para><c>bne.un</c></para>
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
    /// <para><c>box</c></para>
    /// </summary>
    /// <remarks>..., <see langword="struct"/> -&gt; ..., <see cref="object"/></remarks>
    public static IOpCodeEmitter Box(this IOpCodeEmitter emitter, Type valueType)
    {
        emitter.Emit(OpCodes.Box, valueType);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label unconditionally.
    /// <para><c>br</c></para>
    /// </summary>
    /// <param name="forceShort">Use <c>br.s</c>. Only set this to <see langword="true"/> if you know the label will be less than 256 IL bytes.</param>
    /// <remarks>... -&gt; (branch), ...</remarks>
    public static IOpCodeEmitter Branch(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Br_S : OpCodes.Br, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label unconditionally from a try, filter, or catch block and executes finally blocks. Also clears the stack.
    /// <para>Leaving inside a finally block is invalid.</para>
    /// <para><c>leave</c></para>
    /// </summary>
    /// <remarks>... -&gt; (branch), ...</remarks>
    public static IOpCodeEmitter Leave(this IOpCodeEmitter emitter, Label destination, bool forceShort = false)
    {
        emitter.Emit(forceShort ? OpCodes.Leave_S : OpCodes.Leave, destination);
        return emitter;
    }

    /// <summary>
    /// Branches to the given label if the top value on the stack is <see langword="false"/>, zero, or <see langword="null"/>.
    /// <para><c>brfalse</c></para>
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
    /// <para><c>brtrue</c></para>
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
    /// <para><c>constrained.</c> if needed, and <c>call</c> or <c>callvirt</c></para>
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
    /// <para><c>call</c></para>
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
    /// <para><c>castclass</c></para>
    /// </summary>
    /// <remarks>..., <see cref="object"/> -&gt; ..., <typeparamref name="T"/></remarks>
    public static IOpCodeEmitter CastReference<T>(this IOpCodeEmitter emitter)
    {
        return emitter.CastReference(typeof(T));
    }

    /// <summary>
    /// Asserts that the reference type on the stack is assignable to type <paramref name="type"/>, throwing a <see cref="InvalidCastException"/> if not.
    /// <para><c>castclass</c></para>
    /// </summary>
    /// <remarks>..., <see cref="object"/> -&gt; ..., <paramref name="type"/></remarks>
    public static IOpCodeEmitter CastReference(this IOpCodeEmitter emitter, Type type)
    {
        emitter.Emit(OpCodes.Castclass, type);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the two top values on the stack are equal as a 0 or 1.
    /// <para><c>ceq</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 == value2), ...</remarks>
    public static IOpCodeEmitter LoadIfEqual(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Ceq);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the second to top value is greater than the top value on the stack as a 0 or 1.
    /// <para><c>cgt</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 &gt; value2), ...</remarks>
    public static IOpCodeEmitter LoadIfGreater(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Cgt);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the second to top unsigned value is greater than the top unsigned value on the stack, or the second to top value is unordered (meaning <see langword="NaN"/>) as a 0 or 1.
    /// <para><c>cgt.un</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 &gt; value2), ...</remarks>
    public static IOpCodeEmitter LoadIfGreaterUnsigned(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Cgt_Un);
        return emitter;
    }

    /// <summary>
    /// Throws an <see cref="ArithmeticException"/> if value is <see langword="NaN"/>, <see langword="-Infinity"/>, or <see langword="+Infinity"/>, leaving the value on the stack. Behavior is unspecified if the value isn't a floating point value. This won't work with <see cref="decimal"/> values.
    /// <para><c>ckfinite</c></para>
    /// </summary>
    /// <remarks>..., value -&gt; value, ...</remarks>
    public static IOpCodeEmitter CheckFinite(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Ckfinite);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the second to top value is less than the top value on the stack as a 0 or 1.
    /// <para><c>clt</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; int(value1 &lt; value2), ...</remarks>
    public static IOpCodeEmitter LoadIfLess(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Clt);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the second to top unsigned value is less than the top unsigned value on the stack, or the second to top value is unordered (meaning <see langword="NaN"/>) as a 0 or 1.
    /// <para><c>clt.un</c></para>
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
    /// <para><c>conv.i</c></para>
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
    /// <para><c>conv.i1</c></para>
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
    /// <para><c>conv.i2</c></para>
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
    /// <para><c>conv.i4</c></para>
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
    /// <para><c>conv.i8</c></para>
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
    /// <para><c>conv.u</c></para>
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
    /// <para><c>conv.u1</c></para>
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
    /// <para><c>conv.u2</c></para>
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
    /// <para><c>conv.u4</c></para>
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
    /// <para><c>conv.u8</c></para>
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
    /// <para><c>conv.r4</c></para>
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
    /// <para><c>conv.r8</c></para>
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
    /// <para><c>conv.r.un</c></para>
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
    /// <para><c>conv.ovf.i</c></para>
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
    /// <para><c>conv.ovf.i1</c></para>
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
    /// <para><c>conv.ovf.i2</c></para>
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
    /// <para><c>conv.ovf.i4</c></para>
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
    /// <para><c>conv.ovf.i8</c></para>
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
    /// <para><c>conv.ovf.u</c></para>
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
    /// <para><c>conv.ovf.u1</c></para>
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
    /// <para><c>conv.ovf.u2</c></para>
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
    /// <para><c>conv.ovf.u4</c></para>
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
    /// <para><c>conv.ovf.u8</c></para>
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
    /// <para><c>conv.ovf.i.un</c></para>
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
    /// <para><c>conv.ovf.i1.un</c></para>
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
    /// <para><c>conv.ovf.i2.un</c></para>
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
    /// <para><c>conv.ovf.i4.un</c></para>
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
    /// <para><c>conv.ovf.i8.un</c></para>
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
    /// <para><c>conv.ovf.u.un</c></para>
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
    /// <para><c>conv.ovf.u1.un</c></para>
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
    /// <para><c>conv.ovf.u2.un</c></para>
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
    /// <para><c>conv.ovf.u4.un</c></para>
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
    /// <para><c>conv.ovf.u8.un</c></para>
    /// </summary>
    /// <remarks>..., value -&gt; converted value, ...</remarks>
    public static IOpCodeEmitter ConvertToUInt64UnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_Ovf_U8_Un);
        return emitter;
    }

    /// <summary>
    /// Copies some number of bytes from one address to another.
    /// <para><c>cpblk</c></para>
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
    /// <para><c>cpobj</c></para>
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
    /// <para><c>div</c></para>
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
    /// <para><c>div.un</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 / value2</remarks>
    public static IOpCodeEmitter DivideUnsigned(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Div_Un);
        return emitter;
    }

    /// <summary>
    /// Duplicates the top value on the stack.
    /// <para><c>dup</c></para>
    /// </summary>
    /// <remarks>..., value -&gt; ..., value, value</remarks>
    public static IOpCodeEmitter Duplicate(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Dup);
        return emitter;
    }

    /// <summary>
    /// Duplicates the top value on the stack <paramref name="times"/> number of times.
    /// <para><c>dup</c> multiple <paramref name="times"/></para>
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
    /// <para><c>initblk</c></para>
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
    /// <para><c>initobj</c></para>
    /// </summary>
    /// <remarks>..., destinationAddress -&gt; ...</remarks>
    public static IOpCodeEmitter SetDefaultValue<T>(this IOpCodeEmitter emitter)
    {
        return emitter.SetDefaultValue(typeof(T));
    }

    /// <summary>
    /// Sets the default value of a type to a given address. Value types will be initialized to <see langword="default"/>, and reference types to <see langword="null"/>.
    /// <para><c>initobj</c></para>
    /// </summary>
    /// <remarks>..., destinationAddress -&gt; ...</remarks>
    public static IOpCodeEmitter SetDefaultValue(this IOpCodeEmitter emitter, Type type)
    {
        emitter.Emit(OpCodes.Initobj, type);
        return emitter;
    }

    /// <summary>
    /// Loads whether or not the type on the stack is assignable to type <typeparamref name="T"/> as a 0 or 1.
    /// <para>
    /// If <typeparamref name="T"/> is a value type, it should be in it's boxed form before <c>isinst</c> is emitted.
    /// </para>
    /// <para><c>isinst</c></para>
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
    /// <para><c>isinst</c></para>
    /// </summary>
    /// <remarks>..., <see cref="object"/> -&gt; ..., <paramref name="type"/></remarks>
    public static IOpCodeEmitter Is(this IOpCodeEmitter emitter, Type type)
    {
        emitter.Emit(OpCodes.Isinst, type);
        return emitter;
    }

    /// <summary>
    /// Transfers execution of the current method to <paramref name="method"/>. The stack must be empty and any parameters of the current method are transferred to <paramref name="method"/>.
    /// <para><c>jmp</c></para>
    /// </summary>
    /// <remarks>... -&gt; (jump to <paramref name="method"/>) ...</remarks>
    public static IOpCodeEmitter JumpTo(this IOpCodeEmitter emitter, MethodInfo method)
    {
        emitter.Emit(OpCodes.Jmp, method);
        return emitter;
    }

    /// <summary>
    /// Load an argument by it's zero-based index. Note that on non-static methods, 0 denotes the <see langword="this"/> argument.
    /// <para><c>ldarg</c></para>
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
    /// <para><c>ldarga</c></para>
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
    /// <para><c>ldc.i4</c> and <c>conv.i1</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantInt8(this IOpCodeEmitter emitter, sbyte value)
    {
        return emitter.LoadConstantInt32(value)
                      .ConvertToInt8();
    }

    /// <summary>
    /// Load a constant 8-bit unsigned integer.
    /// <para><c>ldc.i4</c> and <c>conv.u1</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantUInt8(this IOpCodeEmitter emitter, byte value)
    {
        return emitter.LoadConstantInt32(value)
                      .ConvertToUInt8();
    }

    /// <summary>
    /// Load a constant 16-bit signed integer.
    /// <para><c>ldc.i4</c> and <c>conv.i2</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantInt16(this IOpCodeEmitter emitter, short value)
    {
        return emitter.LoadConstantInt32(value)
                      .ConvertToInt16();
    }

    /// <summary>
    /// Load a constant 16-bit unsigned integer.
    /// <para><c>ldc.i4</c> and <c>conv.u2</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantUInt16(this IOpCodeEmitter emitter, ushort value)
    {
        return emitter.LoadConstantInt32(value)
                      .ConvertToUInt16();
    }

    /// <summary>
    /// Load a constant 32-bit unsigned integer.
    /// <para><c>ldc.i4</c> and <c>conv.u4</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantUInt32(this IOpCodeEmitter emitter, uint value)
    {
        return emitter.LoadConstantInt32(unchecked( (int)value ))
                      .ConvertToUInt32();
    }

    /// <summary>
    /// Load a constant 32-bit signed integer.
    /// <para><c>ldc.i4</c></para>
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
    /// <para><c>ldc.i8</c> and <c>conv.u8</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantUInt64(this IOpCodeEmitter emitter, ulong value)
    {
        return emitter.LoadConstantInt64(unchecked( (long)value ))
                      .ConvertToUInt64();
    }

    /// <summary>
    /// Load a constant 64-bit signed integer.
    /// <para><c>ldc.i8</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantInt64(this IOpCodeEmitter emitter, long value)
    {
        emitter.Emit(OpCodes.Ldc_I8, value);
        return emitter;
    }

    /// <summary>
    /// Load a constant 32-bit floating point value.
    /// <para><c>ldc.r4</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantSingle(this IOpCodeEmitter emitter, float value)
    {
        emitter.Emit(OpCodes.Ldc_R4, value);
        return emitter;
    }

    /// <summary>
    /// Load a constant 64-bit floating point value.
    /// <para><c>ldc.r8</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantDouble(this IOpCodeEmitter emitter, double value)
    {
        emitter.Emit(OpCodes.Ldc_R8, value);
        return emitter;
    }

    /// <summary>
    /// Load a constant string literal.
    /// <para><c>ldstr</c> or <c>ldnull</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., <paramref name="value"/></remarks>
    public static IOpCodeEmitter LoadConstantString(this IOpCodeEmitter emitter, string? value)
    {
        if (value == null)
            emitter.Emit(OpCodes.Ldnull);
        else
            emitter.Emit(OpCodes.Ldstr, value);
        return emitter;
    }

    /// <summary>
    /// Load an element at the given index in a zero-bound 1-dimensional array.
    /// <para><c>ldelem.[i/u/r][sz]</c> or <c>ldelem <typeparamref name="TElementType"/></c> or <c>ldelem.ref</c></para>
    /// </summary>
    /// <remarks>..., array, index -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadArrayElement<TElementType>(this IOpCodeEmitter emitter, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        if (@volatile || (alignment & MemoryMask) != 0)
        {
            emitter.Emit(OpCodes.Readonly);
            // volatile or unaligned values have to be loaded by address
            emitter.Emit(OpCodes.Ldelema, typeof(TElementType));
            return emitter.LoadAddressValue<TElementType>(alignment, @volatile);
        }

        // benefits from JIT optimization so using typeof literals
        if (typeof(TElementType) == typeof(int))
        {
            emitter.Emit(OpCodes.Ldelem_I4);
        }
        else if (typeof(TElementType) == typeof(byte))
        {
            emitter.Emit(OpCodes.Ldelem_U1);
        }
        else if (typeof(TElementType) == typeof(uint))
        {
            emitter.Emit(OpCodes.Ldelem_U4);
        }
        else if (typeof(TElementType) == typeof(float))
        {
            emitter.Emit(OpCodes.Ldelem_R4);
        }
        else if (typeof(TElementType) == typeof(double))
        {
            emitter.Emit(OpCodes.Ldelem_R8);
        }
        else if (typeof(TElementType) == typeof(long) || typeof(TElementType) == typeof(ulong))
        {
            emitter.Emit(OpCodes.Ldelem_I8);
        }
        else if (typeof(TElementType) == typeof(sbyte))
        {
            emitter.Emit(OpCodes.Ldelem_I1);
        }
        else if (typeof(TElementType) == typeof(nint) || typeof(TElementType) == typeof(nuint))
        {
            emitter.Emit(OpCodes.Ldelem_I);
        }
        else if (typeof(TElementType) == typeof(short))
        {
            emitter.Emit(OpCodes.Ldelem_I2);
        }
        else if (typeof(TElementType) == typeof(ushort))
        {
            emitter.Emit(OpCodes.Ldelem_U2);
        }
        else if (typeof(TElementType).IsValueType)
        {
            emitter.Emit(OpCodes.Ldelem, typeof(TElementType));
        }
        else
        {
            emitter.Emit(OpCodes.Ldelem_Ref);
        }

        return emitter;
    }

    /// <summary>
    /// Load an element at the given index in any array.
    /// <para>Consider using <see cref="LoadArrayElement{TElementType}(IOpCodeEmitter,MemoryAlignment,bool)"/> if you know the array will be a standard ('SZ') array.</para>
    /// <para><c>ldelem.[i/u/r][sz]</c> or <c>ldelem <typeparamref name="TElementType"/></c> or <c>ldelem.ref</c> or <c>callvirt <paramref name="arrayType"/>.Get(params int[] indices)</c></para>
    /// </summary>
    /// <exception cref="ArgumentException"><typeparamref name="TElementType"/> doesn't match <paramref name="arrayType"/>'s element type or <paramref name="arrayType"/> isn't an array type.</exception>
    /// <exception cref="MemberAccessException">Unable to find the expected Get or Address method in <paramref name="arrayType"/>.</exception>
    /// <remarks>..., array[, index] × dimensions -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadArrayElement<TElementType>(this IOpCodeEmitter emitter, Type arrayType, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        if (!arrayType.IsArray)
            throw new ArgumentException("Expected an array type.", nameof(arrayType));

        if (arrayType.GetElementType() != typeof(TElementType))
            throw new ArgumentException("Incorrect element type.", nameof(TElementType));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        if (arrayType.IsSZArray)
#else
        // old check if SZ array
        if (arrayType == typeof(TElementType[]))
#endif
        {
            return emitter.LoadArrayElement<TElementType>(alignment, @volatile);
        }

        if (@volatile || (alignment & MemoryMask) != 0)
        {
            LoadArrayElementAddressIntl(emitter, arrayType, true);
            return emitter.LoadAddressValue<TElementType>(alignment, @volatile);
        }

        LoadArrayElementIntl(emitter, arrayType, typeof(TElementType));
        return emitter;
    }

    /// <summary>
    /// Load an element at the given index in a zero-bound 1-dimensional array.
    /// <para><c>ldelem.[i/u/r][sz]</c> or <c>ldelem <paramref name="elementType"/></c> or <c>ldelem.ref</c></para>
    /// </summary>
    /// <remarks>..., array, index -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadArrayElement(this IOpCodeEmitter emitter, Type elementType, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        if (@volatile || (alignment & MemoryMask) != 0)
        {
            emitter.Emit(OpCodes.Readonly);
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
    /// Load an element at the given index in a zero-bound 1-dimensional array.
    /// <para>Consider using <see cref="LoadArrayElement(IOpCodeEmitter,Type,MemoryAlignment,bool)"/> if you know the array will be a standard ('SZ') array.</para>
    /// <para><c>ldelem.[i/u/r][sz]</c> or <c>ldelem <paramref name="elementType"/></c> or <c>ldelem.ref</c> or <c>callvirt <paramref name="arrayType"/>.Get(params int[] indices)</c></para>
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="elementType"/> doesn't match <paramref name="arrayType"/>'s element type or <paramref name="arrayType"/> isn't an array type.</exception>
    /// <exception cref="MemberAccessException">Unable to find the expected Get or Address method in <paramref name="arrayType"/>.</exception>
    /// <remarks>..., array[, index] × dimensions -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadArrayElement(this IOpCodeEmitter emitter, Type elementType, Type arrayType, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        if (!arrayType.IsArray)
            throw new ArgumentException("Expected an array type.", nameof(arrayType));

        if (arrayType.GetElementType() != elementType)
            throw new ArgumentException("Incorrect element type.", nameof(elementType));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        if (arrayType.IsSZArray)
#else
        // old check if SZ array
        if (arrayType == elementType.MakeArrayType())
#endif
        {
            return emitter.LoadArrayElement(elementType, alignment, @volatile);
        }

        if (@volatile || (alignment & MemoryMask) != 0)
        {
            LoadArrayElementAddressIntl(emitter, arrayType, true);
            return emitter.LoadAddressValue(elementType, alignment, @volatile);
        }

        LoadArrayElementIntl(emitter, arrayType, elementType);
        return emitter;
    }

    private static void LoadArrayElementIntl(IOpCodeEmitter emitter, Type arrayType, Type elementType)
    {
        MethodInfo? getMethod = arrayType.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance);
        if (getMethod != null)
        {
            emitter.Emit(OpCodes.Callvirt, getMethod);
            return;
        }

        MethodDefinition def = new MethodDefinition("Get")
            .DeclaredIn(arrayType, isStatic: false)
            .Returning(elementType);

        int rank = arrayType.GetArrayRank();
        for (int i = 0; i < rank; ++i)
        {
            def.WithParameter(TypeI4, "index" + (i + 1));
        }

        throw new MemberAccessException($"Unable to find {Accessor.ExceptionFormatter.Format(def)}.");
    }

    /// <summary>
    /// Load the address of an element at the given index in a zero-bound 1-dimensional array.
    /// <para><c>ldelema</c></para>
    /// </summary>
    /// <param name="readonly">The given address will be read-only, avoids unnecessary type-checking.</param>
    /// <remarks>..., array, index -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadArrayElementAddress<TElementType>(this IOpCodeEmitter emitter, bool @readonly = false)
    {
        if (@readonly)
            emitter.Emit(OpCodes.Readonly);

        emitter.Emit(OpCodes.Ldelema, typeof(TElementType));
        return emitter;
    }

    /// <summary>
    /// Load the address of an element at the given index in any array.
    /// <para>Consider using <see cref="LoadArrayElementAddress(IOpCodeEmitter,Type,bool)"/> if you know the array will be a standard ('SZ') array.</para>
    /// <para><c>ldelema</c> or <c>callvirt <see cref="arrayType"/>.Address(params int[] indices)</c></para>
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="elementType"/> doesn't match <paramref name="arrayType"/>'s element type or <paramref name="arrayType"/> isn't an array type.</exception>
    /// <exception cref="MemberAccessException">Unable to find the expected Address method in <paramref name="arrayType"/>.</exception>
    /// <param name="readonly">The given address will be read-only, avoids unnecessary type-checking.</param>
    /// <remarks>..., array[, index] × dimensions -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadArrayElementAddress(this IOpCodeEmitter emitter, Type elementType, Type arrayType, bool @readonly = false)
    {
        if (!arrayType.IsArray)
            throw new ArgumentException("Expected an array type.", nameof(arrayType));

        if (arrayType.GetElementType() != elementType)
            throw new ArgumentException("Incorrect element type.", nameof(elementType));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        if (arrayType.IsSZArray)
#else
        // old check if SZ array
        if (arrayType == elementType.MakeArrayType())
#endif
        {
            if (@readonly)
                emitter.Emit(OpCodes.Readonly);

            emitter.Emit(OpCodes.Ldelema, elementType);
            return emitter;
        }

        LoadArrayElementAddressIntl(emitter, arrayType, @readonly);
        return emitter;
    }

    /// <summary>
    /// Load the address of an element at the given index in a zero-bound 1-dimensional array.
    /// <para><c>ldelema</c></para>
    /// </summary>
    /// <param name="readonly">The given address will be read-only, avoids unnecessary type-checking.</param>
    /// <remarks>..., array, index -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadArrayElementAddress(this IOpCodeEmitter emitter, Type elementType, bool @readonly = false)
    {
        if (@readonly)
            emitter.Emit(OpCodes.Readonly);

        emitter.Emit(OpCodes.Ldelema, elementType);
        return emitter;
    }

    /// <summary>
    /// Load the address of an element at the given index in any array.
    /// <para>Consider using <see cref="LoadArrayElementAddress{T}(IOpCodeEmitter,bool)"/> if you know the array will be a standard ('SZ') array.</para>
    /// <para><c>ldelema</c> or <c>callvirt <see cref="arrayType"/>.Address(params int[] indices)</c></para>
    /// </summary>
    /// <exception cref="ArgumentException"><typeparamref name="TElementType"/> doesn't match <paramref name="arrayType"/>'s element type or <paramref name="arrayType"/> isn't an array type.</exception>
    /// <exception cref="MemberAccessException">Unable to find the expected Address method in <paramref name="arrayType"/>.</exception>
    /// <param name="readonly">The given address will be read-only, avoids unnecessary type-checking.</param>
    /// <remarks>..., array[, index] × dimensions -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadArrayElementAddress<TElementType>(this IOpCodeEmitter emitter, Type arrayType, bool @readonly = false)
    {
        if (!arrayType.IsArray)
            throw new ArgumentException("Expected an array type.", nameof(arrayType));

        if (arrayType.GetElementType() != typeof(TElementType))
            throw new ArgumentException("Incorrect element type.", nameof(TElementType));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        if (arrayType.IsSZArray)
#else
        // old check if SZ array
        if (arrayType == typeof(TElementType[]))
#endif
        {
            if (@readonly)
                emitter.Emit(OpCodes.Readonly);

            emitter.Emit(OpCodes.Ldelema, typeof(TElementType));
            return emitter;
        }

        LoadArrayElementAddressIntl(emitter, arrayType, @readonly);
        return emitter;
    }

    private static void LoadArrayElementAddressIntl(IOpCodeEmitter emitter, Type arrayType, bool @readonly)
    {
        MethodInfo? addressMethod = arrayType.GetMethod("Address", BindingFlags.Public | BindingFlags.Instance);
        if (addressMethod != null)
        {
            if (@readonly)
                emitter.Emit(OpCodes.Readonly);

            emitter.Emit(OpCodes.Callvirt, addressMethod);
            return;
        }

        MethodDefinition def = new MethodDefinition("Address")
            .DeclaredIn(arrayType, isStatic: false)
            .Returning(arrayType.GetElementType()!.MakeByRefType(), @readonly ? ByRefTypeMode.RefReadOnly : ByRefTypeMode.Ref);

        int rank = arrayType.GetArrayRank();
        for (int i = 0; i < rank; ++i)
        {
            def.WithParameter(TypeI4, "index" + (i + 1));
        }

        throw new MemberAccessException($"Unable to find {Accessor.ExceptionFormatter.Format(def)}.");
    }

    /// <summary>
    /// Load the value at the given instance field, removing the instance from the top of the stack.
    /// <para><c>ldfld</c></para>
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
    /// <para><c>ldsfld</c></para>
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
    /// Load the value at the given static or instance field.
    /// <para><c>ldfld</c> or <c>ldsfld</c></para>
    /// </summary>
    /// <exception cref="ArgumentException">The expression <paramref name="field"/> doesn't load a field.</exception>
    /// <remarks>...[, instance if non-static] -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadFieldValue<T>(this IOpCodeEmitter emitter, Expression<Func<T>> field, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        Expression expr = field;
        while (expr is LambdaExpression lambda)
        {
            expr = lambda.Body;
        }
#if NET40_OR_GREATER || NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        while (expr.CanReduce)
        {
            expr = expr.Reduce();
        }
#endif

        if (expr.NodeType == ExpressionType.MemberAccess && (expr as MemberExpression)?.Member is FieldInfo fld)
        {
            return fld.IsStatic
                ? emitter.LoadStaticFieldValue(fld, alignment, @volatile)
                : emitter.LoadInstanceFieldValue(fld, alignment, @volatile);
        }

        throw new ArgumentException("Expected a static or instance field (such as '() => _field')", nameof(field));
    }

    /// <summary>
    /// Load the value at the given instance field.
    /// <para><c>ldfld</c> or <c>ldsfld</c></para>
    /// </summary>
    /// <exception cref="ArgumentException">The expression <paramref name="field"/> doesn't load a field.</exception>
    /// <remarks>..., instance -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadFieldValue<TInstance, T>(this IOpCodeEmitter emitter, Expression<Func<TInstance, T>> field, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        Expression expr = field;
        while (expr is LambdaExpression lambda)
        {
            expr = lambda.Body;
        }
#if NET40_OR_GREATER || NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        while (expr.CanReduce)
        {
            expr = expr.Reduce();
        }
#endif

        if (expr.NodeType == ExpressionType.MemberAccess && (expr as MemberExpression)?.Member is FieldInfo fld)
        {
            return fld.IsStatic
                ? emitter.LoadStaticFieldValue(fld, alignment, @volatile)
                : emitter.LoadInstanceFieldValue(fld, alignment, @volatile);
        }

        throw new ArgumentException("Expected a static or instance field (such as '() => _field')", nameof(field));
    }

    /// <summary>
    /// Load the address of the given instance field, removing the instance from the top of the stack.
    /// <para><c>ldflda</c></para>
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
    /// <para><c>ldsflda</c></para>
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
    /// Load the address of the given static or instance field.
    /// <para><c>ldflda</c> or <c>ldsflda</c></para>
    /// </summary>
    /// <exception cref="ArgumentException">The expression <paramref name="field"/> doesn't load a field.</exception>
    /// <remarks>...[, instance if non-static] -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadFieldAddress<T>(this IOpCodeEmitter emitter, Expression<Func<T>> field)
    {
        Expression expr = field;
        while (expr is LambdaExpression lambda)
        {
            expr = lambda.Body;
        }
#if NET40_OR_GREATER || NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        while (expr.CanReduce)
        {
            expr = expr.Reduce();
        }
#endif

        if (expr.NodeType == ExpressionType.MemberAccess && (expr as MemberExpression)?.Member is FieldInfo fld)
        {
            return fld.IsStatic
                ? emitter.LoadStaticFieldAddress(fld)
                : emitter.LoadInstanceFieldAddress(fld);
        }

        throw new ArgumentException("Expected a static or instance field (such as '() => _field')", nameof(field));
    }

    /// <summary>
    /// Load the address of the given instance field.
    /// <para><c>ldflda</c> or <c>ldsflda</c></para>
    /// </summary>
    /// <exception cref="ArgumentException">The expression <paramref name="field"/> doesn't load a field.</exception>
    /// <remarks>..., instance -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadFieldAddress<TInstance, T>(this IOpCodeEmitter emitter, Expression<Func<TInstance, T>> field)
    {
        Expression expr = field;
        while (expr is LambdaExpression lambda)
        {
            expr = lambda.Body;
        }
#if NET40_OR_GREATER || NETSTANDARD1_0_OR_GREATER || NETCOREAPP1_0_OR_GREATER
        while (expr.CanReduce)
        {
            expr = expr.Reduce();
        }
#endif

        if (expr.NodeType == ExpressionType.MemberAccess && (expr as MemberExpression)?.Member is FieldInfo fld)
        {
            return fld.IsStatic
                ? emitter.LoadStaticFieldAddress(fld)
                : emitter.LoadInstanceFieldAddress(fld);
        }

        throw new ArgumentException("Expected a static or instance field (such as '() => _field')", nameof(field));
    }

    /// <summary>
    /// Load the function pointer to a method.
    /// <para><c>ldftn</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadFunctionPointer(this IOpCodeEmitter emitter, MethodInfo method)
    {
        emitter.Emit(OpCodes.Ldftn, method);
        return emitter;
    }

    /// <summary>
    /// Load the function pointer to the implemented method of a virtual or abstract method on an instace.
    /// <para><c>ldvirtftn</c></para>
    /// </summary>
    /// <remarks>..., instance -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadFunctionPointerVirtual(this IOpCodeEmitter emitter, MethodInfo method)
    {
        emitter.Emit(OpCodes.Ldvirtftn, method);
        return emitter;
    }

    /// <summary>
    /// Load the value at the given address.
    /// <para><c>ldind_[i/u/r][sz]</c> or <c>ldobj <paramref name="elementType"/></c> or <c>ldind_ref</c></para>
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
    /// Load the value at the given address.
    /// <para><c>ldind_[i/u/r][sz]</c> or <c>ldobj <typeparamref name="TElementType"/></c> or <c>ldind_ref</c></para>
    /// </summary>
    /// <remarks>..., address -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadAddressValue<TElementType>(this IOpCodeEmitter emitter, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        EmitAlignmentPrefix(emitter, alignment);
        if (@volatile)
            emitter.Emit(OpCodes.Volatile);

        if (typeof(TElementType) == typeof(int))
        {
            emitter.Emit(OpCodes.Ldind_I4);
        }
        else if (typeof(TElementType) == typeof(byte))
        {
            emitter.Emit(OpCodes.Ldind_U1);
        }
        else if (typeof(TElementType) == typeof(uint))
        {
            emitter.Emit(OpCodes.Ldind_U4);
        }
        else if (typeof(TElementType) == typeof(float))
        {
            emitter.Emit(OpCodes.Ldind_R4);
        }
        else if (typeof(TElementType) == typeof(double))
        {
            emitter.Emit(OpCodes.Ldind_R8);
        }
        else if (typeof(TElementType) == typeof(long) || typeof(TElementType) == typeof(ulong))
        {
            emitter.Emit(OpCodes.Ldind_I8);
        }
        else if (typeof(TElementType) == typeof(sbyte))
        {
            emitter.Emit(OpCodes.Ldind_I1);
        }
        else if (typeof(TElementType) == typeof(nint) || typeof(TElementType) == typeof(nuint))
        {
            emitter.Emit(OpCodes.Ldind_I);
        }
        else if (typeof(TElementType) == typeof(short))
        {
            emitter.Emit(OpCodes.Ldind_I2);
        }
        else if (typeof(TElementType) == typeof(ushort))
        {
            emitter.Emit(OpCodes.Ldind_U2);
        }
        else if (typeof(TElementType).IsValueType)
        {
            emitter.Emit(OpCodes.Ldobj, typeof(TElementType));
        }
        else
        {
            emitter.Emit(OpCodes.Ldind_Ref);
        }

        return emitter;
    }

    /// <summary>
    /// Load the length of the zero-bound 1-dimensional array on the stack.
    /// <para><c>ldlen</c></para>
    /// </summary>
    /// <remarks>..., array -&gt; ..., length</remarks>
    public static IOpCodeEmitter LoadArrayLength(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Ldlen);
        return emitter;
    }

    private static MethodInfo? _getArrayLength;

    /// <summary>
    /// Load the length of the array on the stack of any array type.
    /// <para><c>ldlen</c> or <c>callvirt Array.Length</c></para>
    /// <para>Consider using <see cref="LoadArrayLength(IOpCodeEmitter)"/> if you know the array will be a standard ('SZ') array.</para>
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="arrayType"/> isn't an array type.</exception>
    /// <exception cref="MemberAccessException">Unable to find <see cref="Array.Length"/> with reflection.</exception>
    /// <param name="arrayType">The type of the array. Ex: <c>typeof(int[,])</c>.</param>
    /// <remarks>..., array -&gt; ..., length</remarks>
    public static IOpCodeEmitter LoadArrayLength(this IOpCodeEmitter emitter, Type arrayType)
    {
        if (!arrayType.IsArray)
            throw new ArgumentException("Expected an array type.", nameof(arrayType));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        if (arrayType.IsSZArray)
        {
            emitter.Emit(OpCodes.Ldlen);
            return emitter;
        }
#else
        // old check if SZ array
        if (arrayType == arrayType.GetElementType()!.MakeArrayType())
        {
            emitter.Emit(OpCodes.Ldlen);
            return emitter;
        }
#endif
        _getArrayLength ??= typeof(Array).GetProperty("Length", BindingFlags.Instance | BindingFlags.Public)?.GetGetMethod(true);

        if (_getArrayLength == null)
        {
            throw new MemberAccessException($"Unable to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(Array.Length))
                .DeclaredIn<Array>(isStatic: false)
                .WithPropertyType<int>()
                .WithNoSetter())}.Length.");
        }

        emitter.Emit(OpCodes.Callvirt, _getArrayLength);
        return emitter;
    }

    /// <summary>
    /// Load the length of the array on the stack with the given rank and if all indices start at zero or not.
    /// <para><c>ldlen</c> or <c>callvirt Array.Length</c></para>
    /// <para>Consider using <see cref="LoadArrayLength(IOpCodeEmitter)"/> if you know the array will be a standard ('SZ') array.</para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rank"/> is less than one.</exception>
    /// <exception cref="MemberAccessException">Unable to find <see cref="Array.Length"/> with reflection.</exception>
    /// <param name="rank">Number of dimensions in the array. Standard arrays have 1.</param>
    /// <param name="allIndicesStartAtZero">If the lower bound of all dimension's indices are zero. Standard arrays always are.</param>
    /// <remarks>..., array -&gt; ..., length</remarks>
    public static IOpCodeEmitter LoadArrayLength(this IOpCodeEmitter emitter, int rank, bool allIndicesStartAtZero = true)
    {
        if (rank < 1)
            throw new ArgumentOutOfRangeException(nameof(rank));

        if (allIndicesStartAtZero && rank == 1)
        {
            emitter.Emit(OpCodes.Ldlen);
            return emitter;
        }

        _getArrayLength ??= typeof(Array).GetProperty("Length", BindingFlags.Instance | BindingFlags.Public)?.GetGetMethod(true);

        if (_getArrayLength == null)
        {
            throw new MemberAccessException($"Unable to find {Accessor.ExceptionFormatter.Format(new PropertyDefinition(nameof(Array.Length))
                .DeclaredIn<Array>(isStatic: false)
                .WithPropertyType<int>()
                .WithNoSetter())
            }.Length.");
        }

        emitter.Emit(OpCodes.Callvirt, _getArrayLength);
        return emitter;
    }

    /// <summary>
    /// Load the value of a local variable by a reference to it.
    /// <para>
    /// <see cref="LocalBuilder"/> implicitly casts to <see cref="LocalReference"/>.
    /// </para>
    /// <para><c>ldloc</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadLocalValue(this IOpCodeEmitter emitter, LocalReference lclRef)
    {
        int index = lclRef.Index;
        LocalBuilder? bldr = lclRef.Local;
        if (bldr == null && (index < 0 || index > 3))
            throw new ArgumentException("Missing local reference.", nameof(lclRef));

        switch (index)
        {
            case 0:
                emitter.Emit(OpCodes.Ldloc_0);
                return emitter;

            case 1:
                emitter.Emit(OpCodes.Ldloc_1);
                return emitter;

            case 2:
                emitter.Emit(OpCodes.Ldloc_2);
                return emitter;

            case 3:
                emitter.Emit(OpCodes.Ldloc_3);
                return emitter;

            default:
                // ILGenerator will optimize low indices
                emitter.Emit(OpCodes.Ldloc, bldr!);
                return emitter;
        }
    }

    /// <summary>
    /// Load the address of a local variable by a reference to it.
    /// <para>
    /// <see cref="LocalBuilder"/> implicitly casts to <see cref="LocalReference"/>.
    /// </para>
    /// <para><c>ldloca</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadLocalAddress(this IOpCodeEmitter emitter, LocalReference lclRef)
    {
        int index = lclRef.Index;
        LocalBuilder? bldr = lclRef.Local;
        if (bldr == null && (index < 0 || index > 3))
            throw new ArgumentException("Missing local reference.", nameof(lclRef));

        switch (index)
        {
            case <= 3:
                emitter.Emit(OpCodes.Ldloca_S, (byte)index);
                return emitter;

            default:
                // ILGenerator will optimize low indices
                emitter.Emit(OpCodes.Ldloca, bldr!);
                return emitter;
        }
    }

    /// <summary>
    /// Load a <see langword="null"/> reference to the stack.
    /// <para><c>ldnull</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., null</remarks>
    public static IOpCodeEmitter LoadNullValue(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Ldnull);
        return emitter;
    }

    /// <summary>
    /// Load the metadata token for a type to the stack. Consider <see cref="LoadTypeOf"/> instead.
    /// <para><c>ldtoken</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., typeTok</remarks>
    public static IOpCodeEmitter LoadToken(this IOpCodeEmitter emitter, Type type)
    {
        emitter.Emit(OpCodes.Ldtoken, type);
        return emitter;
    }

    /// <summary>
    /// Load the metadata token for a type to the stack. Consider <see cref="LoadTypeOf{T}"/> instead.
    /// <para><c>ldtoken</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., typeToken</remarks>
    public static IOpCodeEmitter LoadToken<T>(this IOpCodeEmitter emitter)
    {
        return emitter.LoadToken(typeof(T));
    }

    /// <summary>
    /// Load the metadata token for a method to the stack.
    /// <para><c>ldtoken</c></para>
    /// </summary>
    /// <exception cref="ArgumentException">Expected <paramref name="method"/> to be a <see cref="ConstructorInfo"/> or <see cref="MethodInfo"/>.</exception>
    /// <remarks>... -&gt; ..., methodToken</remarks>
    public static IOpCodeEmitter LoadToken(this IOpCodeEmitter emitter, MethodBase method)
    {
        switch (method)
        {
            case ConstructorInfo ctor:
                emitter.Emit(OpCodes.Ldtoken, ctor);
                break;

            case MethodInfo mtd:
                emitter.Emit(OpCodes.Ldtoken, mtd);
                break;

            default:
                throw new ArgumentException($"Unexpected method type: {Accessor.ExceptionFormatter.Format(method.GetType())}.", nameof(method));
        }

        return emitter;
    }

    /// <summary>
    /// Load the metadata token for a field to the stack.
    /// <para><c>ldtoken</c></para>
    /// </summary>
    /// <remarks>... -&gt; ..., fieldToken</remarks>
    public static IOpCodeEmitter LoadToken(this IOpCodeEmitter emitter, FieldInfo field)
    {
        emitter.Emit(OpCodes.Ldtoken, field);
        return emitter;
    }

    /// <summary>
    /// Allocate the current number on the stack of bytes.
    /// <para><c>conv.u</c> and <c>localloc</c></para>
    /// </summary>
    /// <remarks>..., size -&gt; ..., address</remarks>
    public static IOpCodeEmitter StackAllocate(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Conv_U);
        emitter.Emit(OpCodes.Localloc);
        return emitter;
    }

    /// <summary>
    /// Allocate the current number on the stack in terms of <typeparamref name="T"/>. The number on the stack will be multiplied by the size of <typeparamref name="T"/>
    /// <para><c>conv.u</c>, <c>ldc.i4</c>, <c>mul.ovf.un</c>, and <c>localloc</c></para>
    /// </summary>
    /// <remarks>..., size -&gt; ..., address</remarks>
    public static unsafe IOpCodeEmitter StackAllocate<T>(this IOpCodeEmitter emitter) where T : unmanaged
    {
        emitter.Emit(OpCodes.Conv_U);

        // not using extension method for generic JIT optimziation
        switch (sizeof(T))
        {
            case 0:
            case 1:
                emitter.Emit(OpCodes.Localloc);
                return emitter;

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
                emitter.Emit(OpCodes.Ldc_I4_S, (byte)sizeof(T));
                break;

            default:
                emitter.Emit(OpCodes.Ldc_I4, sizeof(T));
                break;
        }

        emitter.Emit(OpCodes.Mul_Ovf_Un);
        emitter.Emit(OpCodes.Localloc);
        return emitter;
    }

    /// <summary>
    /// Creates a <see cref="TypedReference"/> of type <paramref name="type"/>.
    /// <para><c>mkrefany</c></para>
    /// </summary>
    /// <remarks>..., address -&gt; ..., <see cref="TypedReference"/></remarks>
    public static IOpCodeEmitter MakeTypedReference(this IOpCodeEmitter emitter, Type type)
    {
        emitter.Emit(OpCodes.Mkrefany, type);
        return emitter;
    }

    /// <summary>
    /// Creates a <see cref="TypedReference"/> of type <typeparamref name="T"/>.
    /// <para><c>mkrefany</c></para>
    /// </summary>
    /// <remarks>..., address -&gt; ..., <see cref="TypedReference"/></remarks>
    public static IOpCodeEmitter MakeTypedReference<T>(this IOpCodeEmitter emitter)
    {
        return emitter.MakeTypedReference(typeof(T));
    }

    /// <summary>
    /// Retreives the token of the type of a <see cref="TypedReference"/>.
    /// <para><c>refanytype</c></para>
    /// </summary>
    /// <remarks>..., <see cref="TypedReference"/> -&gt; ..., typeToken</remarks>
    public static IOpCodeEmitter LoadTypedReferenceTypeToken(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Refanytype);
        return emitter;
    }

    /// <summary>
    /// Retreives the type of a <see cref="TypedReference"/>.
    /// <para><c>refanytype</c> then <c>call <see cref="Type.GetTypeFromHandle"/></c></para>
    /// </summary>
    /// <remarks>..., <see cref="TypedReference"/> -&gt; ..., <see cref="Type"/></remarks>
    public static IOpCodeEmitter LoadTypedReferenceType(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Refanytype);
        emitter.Emit(OpCodes.Call, TokenToTypeMtd);
        return emitter;
    }

    /// <summary>
    /// Retreives the target address of a <see cref="TypedReference"/> which targets <typeparamref name="TReferencedType"/>.
    /// <para><c>refanyval</c></para>
    /// </summary>
    /// <remarks>..., <see cref="TypedReference"/> -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadTypedReferenceAddress<TReferencedType>(this IOpCodeEmitter emitter)
    {
        return emitter.LoadTypedReferenceAddress(typeof(TReferencedType));
    }

    /// <summary>
    /// Retreives the target address of a <see cref="TypedReference"/> which targets <paramref name="referencedType"/>.
    /// <para><c>refanyval</c></para>
    /// </summary>
    /// <remarks>..., <see cref="TypedReference"/> -&gt; ..., address</remarks>
    public static IOpCodeEmitter LoadTypedReferenceAddress(this IOpCodeEmitter emitter, Type referencedType)
    {
        emitter.Emit(OpCodes.Refanyval, referencedType);
        return emitter;
    }

    /// <summary>
    /// Retreives the value of a <see cref="TypedReference"/> which targets <typeparamref name="TReferencedType"/>.
    /// <para><c>refanyval</c> then <c>ldind_[i/u/r][sz]</c> or <c>ldobj <typeparamref name="TReferencedType"/></c> or <c>ldind_ref</c></para>
    /// </summary>
    /// <remarks>..., <see cref="TypedReference"/> -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadTypedReferenceValue<TReferencedType>(this IOpCodeEmitter emitter, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        emitter.Emit(OpCodes.Refanyval, typeof(TReferencedType));
        emitter.LoadAddressValue<TReferencedType>(alignment, @volatile);
        return emitter;
    }

    /// <summary>
    /// Retreives the value of a <see cref="TypedReference"/> which targets <paramref name="referencedType"/>.
    /// <para><c>refanyval</c> then <c>ldind_[i/u/r][sz]</c> or <c>ldobj <paramref name="referencedType"/></c> or <c>ldind_ref</c></para>
    /// </summary>
    /// <remarks>..., <see cref="TypedReference"/> -&gt; ..., value</remarks>
    public static IOpCodeEmitter LoadTypedReferenceValue(this IOpCodeEmitter emitter, Type referencedType, MemoryAlignment alignment = MemoryAlignment.AlignedNative, bool @volatile = false)
    {
        emitter.Emit(OpCodes.Refanyval, referencedType);
        emitter.LoadAddressValue(referencedType, alignment, @volatile);
        return emitter;
    }

    /// <summary>
    /// Multiplies the top two values on the stack and pushes the result onto the stack.
    /// <para><c>mul</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 × value2</remarks>
    public static IOpCodeEmitter Multiply(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Mul);
        return emitter;
    }

    /// <summary>
    /// Multiplies the top two values on the stack and pushes the result onto the stack, throwing an <see cref="OverflowException"/> if the operation will result in an overflow.
    /// <para><c>mul.ovf</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 × value2</remarks>
    public static IOpCodeEmitter MultiplyChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Mul_Ovf);
        return emitter;
    }

    /// <summary>
    /// Multiplies the top two unsigned values on the stack and pushes the result onto the stack, throwing an <see cref="OverflowException"/> if the operation will result in an overflow.
    /// <para><c>mul.ovf.un</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 × value2</remarks>
    public static IOpCodeEmitter MultiplyUnsignedChecked(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Mul_Ovf_Un);
        return emitter;
    }

    /// <summary>
    /// Multiplies the top value on the stack by -1.
    /// <para>This is not the same as <see cref="Not"/> or <see cref="BitwiseNot"/></para>
    /// <para><c>neg</c></para>
    /// </summary>
    /// <remarks>..., value -&gt; ..., -value</remarks>
    public static IOpCodeEmitter Negate(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Neg);
        return emitter;
    }

    /// <summary>
    /// Performs a bitwise not operation on the top value on the stack and pushes the result onto the stack.
    /// <para>This is not the same as <see cref="Not"/> or <see cref="Negate"/></para>
    /// <para><c>not</c></para>
    /// </summary>s
    /// <remarks>..., value -&gt; ..., ~value</remarks>
    public static IOpCodeEmitter BitwiseNot(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Not);
        return emitter;
    }

    /// <summary>
    /// Performs a boolean not operation on the top value on the stack and pushes the result onto the stack. Any zero value becomes one, all other values become zero.
    /// <para>This is not the same as <see cref="BitwiseNot"/> or <see cref="Negate"/></para>
    /// <para><c>ldc.i4.0</c> and <c>ceq</c></para>
    /// </summary>s
    /// <remarks>..., value -&gt; ..., !value</remarks>
    public static IOpCodeEmitter Not(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Ldc_I4_0);
        emitter.Emit(OpCodes.Ceq);
        return emitter;
    }

    /// <summary>
    /// Load a new zero-bound 1-dimensional array on the stack after removing the length from the top of the stack.
    /// <para><c>newarr</c></para>
    /// </summary>
    /// <remarks>..., length -&gt; ..., array</remarks>
    public static IOpCodeEmitter CreateArray(this IOpCodeEmitter emitter, Type elementType)
    {
        emitter.Emit(OpCodes.Newarr, elementType);
        return emitter;
    }

    /// <summary>
    /// Load a new array on the stack after removing the lengths of each dimension (in order) from the top of the stack.
    /// If the array type has any non-zero lower bounds, <paramref name="hasStartIndices"/> should be <see langword="true"/> and arguments should be passed in the following format:
    /// <code>dim1StartIndex, dim1Length, dim2StartIndex, dim2Length, etc..</code>
    /// Otherwise, the lengths of each dimension should be passed in the following format:
    /// <code>dim1Length, dim2Length, etc..</code>
    /// Standard arrays will be simplified to a <c>newarr</c> call.
    /// <para><c>newarr</c> or <c>newobj <paramref name="arrayType"/>(params int[] length)</c> or <c>newobj <paramref name="arrayType"/>(params (int startIndex, int length)[] data)</c></para>
    /// <para>Consider using <see cref="CreateArray(IOpCodeEmitter,Type)"/> if you know the array will be a standard ('SZ') array.</para>
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="elementType"/> doesn't match <paramref name="arrayType"/>'s element type or <paramref name="arrayType"/> isn't an array type.</exception>
    /// <exception cref="MemberAccessException">Unable to find the expected constructor in <paramref name="arrayType"/>.</exception>
    /// <param name="arrayType">The type of the array. Ex: <c>typeof(int[,])</c>.</param>
    /// <param name="hasStartIndices">If start indices should also be consumed from the stack.</param>
    /// <remarks>..., ([startIndex,] length) × rank -&gt; ..., array</remarks>
    public static IOpCodeEmitter CreateArray(this IOpCodeEmitter emitter, Type elementType, Type arrayType, bool hasStartIndices = false)
    {
        if (!arrayType.IsArray)
            throw new ArgumentException("Expected an array type.", nameof(arrayType));

        if (elementType != arrayType.GetElementType()!)
            throw new ArgumentException("Incorrect element type.", nameof(elementType));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        if (arrayType.IsSZArray)
        {
            emitter.Emit(OpCodes.Newarr, elementType);
            return emitter;
        }
#else
        // old check if SZ array
        if (arrayType == elementType.MakeArrayType())
        {
            emitter.Emit(OpCodes.Newarr, elementType);
            return emitter;
        }
#endif

        CreateArrayIntl(emitter, arrayType, hasStartIndices);
        return emitter;
    }

    /// <summary>
    /// Load a new zero-bound 1-dimensional array on the stack after removing the length from the top of the stack.
    /// <para><c>newarr</c></para>
    /// </summary>
    /// <remarks>..., length -&gt; ..., array</remarks>
    public static IOpCodeEmitter CreateArray<TElementType>(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Newarr, typeof(TElementType));
        return emitter;
    }

    /// <summary>
    /// Load a new array on the stack after removing the lengths of each dimension (in order) from the top of the stack.
    /// If the array type has any non-zero lower bounds, <paramref name="hasStartIndices"/> should be <see langword="true"/> and arguments should be passed in the following format:
    /// <code>dim1StartIndex, dim1Length, dim2StartIndex, dim2Length, etc..</code>
    /// Otherwise, the lengths of each dimension should be passed in the following format:
    /// <code>dim1Length, dim2Length, etc..</code>
    /// Standard arrays will be simplified to a <c>newarr</c> call.
    /// <para><c>newarr</c> or <c>newobj <paramref name="arrayType"/>(params int[] length)</c> or <c>newobj <paramref name="arrayType"/>(params (int startIndex, int length)[] data)</c></para>
    /// <para>Consider using <see cref="CreateArray{T}(IOpCodeEmitter)"/> if you know the array will be a standard ('SZ') array.</para>
    /// </summary>
    /// <exception cref="ArgumentException"><typeparamref name="TElementType"/> doesn't match <paramref name="arrayType"/>'s element type or <paramref name="arrayType"/> isn't an array type.</exception>
    /// <exception cref="MemberAccessException">Unable to find the expected constructor in <paramref name="arrayType"/>.</exception>
    /// <param name="arrayType">The type of the array. Ex: <c>typeof(int[,])</c>.</param>
    /// <param name="hasStartIndices">If start indices should also be consumed from the stack.</param>
    /// <remarks>..., ([startIndex,] length) × rank -&gt; ..., array</remarks>
    public static IOpCodeEmitter CreateArray<TElementType>(this IOpCodeEmitter emitter, Type arrayType, bool hasStartIndices = false)
    {
        if (!arrayType.IsArray)
            throw new ArgumentException("Expected an array type.", nameof(arrayType));

        Type elementType2 = arrayType.GetElementType()!;
        if (typeof(TElementType) != elementType2)
            throw new ArgumentException("Incorrect element type.", nameof(TElementType));

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        if (arrayType.IsSZArray)
        {
            emitter.Emit(OpCodes.Newarr, typeof(TElementType));
            return emitter;
        }
#else
        // old check if SZ array
        if (arrayType == typeof(TElementType[]))
        {
            emitter.Emit(OpCodes.Newarr, elementType);
            return emitter;
        }
#endif

        CreateArrayIntl(emitter, arrayType, hasStartIndices);
        return emitter;
    }

    private static void CreateArrayIntl(IOpCodeEmitter emitter, Type arrayType, bool hasStartIndices)
    {
        int rank = arrayType.GetArrayRank();
        Type[] args = new Type[hasStartIndices ? rank * 2 : rank];
        for (int i = 0; i < args.Length; ++i)
            args[i] = TypeI4;

        ConstructorInfo? ctor = arrayType.GetConstructor(args);
        if (ctor != null)
        {
            emitter.Emit(OpCodes.Newobj, ctor);
            return;
        }

        MethodDefinition def = new MethodDefinition(arrayType);
        for (int i = 0; i < rank; ++i)
        {
            string iFmt = (i + 1).ToString();
            if (hasStartIndices)
                def.WithParameter(TypeI4, "startIndex" + iFmt);
            def.WithParameter(TypeI4, "length" + iFmt);
        }

        throw new MemberAccessException($"Unable to find {Accessor.ExceptionFormatter.Format(def)}.");
    }

    /// <summary>
    /// Load a new object created with the parameterless constructor onto the stack.
    /// <para><c>newobj</c></para>
    /// </summary>
    /// <exception cref="MemberAccessException">No parameterless constructor is available for <typeparamref name="TElementType"/>.</exception>
    /// <remarks>... -&gt; ..., object</remarks>
    public static IOpCodeEmitter CreateObject<TElementType>(this IOpCodeEmitter emitter) where TElementType : new()
    {
        ConstructorInfo? ctor = typeof(TElementType).GetConstructor(Type.EmptyTypes);
        if (ctor == null)
        {
            // theoretically this should never happen
            throw new MemberAccessException($"Unable to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(typeof(TElementType), false).WithNoParameters())}.");
        }

        emitter.Emit(OpCodes.Newobj, ctor);
        return emitter;
    }

    /// <summary>
    /// Load a new object created with the parameterless constructor onto the stack.
    /// <para><c>newobj</c></para>
    /// </summary>
    /// <exception cref="MemberAccessException">No parameterless constructor is available for <paramref name="type"/>.</exception>
    /// <remarks>... -&gt; ..., object</remarks>
    public static IOpCodeEmitter CreateObject(this IOpCodeEmitter emitter, Type type)
    {
        ConstructorInfo? ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor == null)
        {
            throw new MemberAccessException($"Unable to find {Accessor.ExceptionFormatter.Format(new MethodDefinition(type, false).WithNoParameters())}.");
        }

        emitter.Emit(OpCodes.Newobj, ctor);
        return emitter;
    }

    /// <summary>
    /// Load a new object created with the given constructor onto the stack.
    /// <para><c>newobj</c></para>
    /// </summary>
    /// <remarks>...[, parameters] -&gt; ..., object</remarks>
    public static IOpCodeEmitter CreateObject(this IOpCodeEmitter emitter, ConstructorInfo constructor)
    {
        emitter.Emit(OpCodes.Newobj, constructor);
        return emitter;
    }

    /// <summary>
    /// Inserts an instruction that fundamentally does nothing.
    /// <para><c>nop</c></para>
    /// </summary>
    /// <remarks>... -&gt; ...</remarks>
    public static IOpCodeEmitter NoOperation(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Nop);
        return emitter;
    }

    /// <summary>
    /// Bitwise or's the top two values on the stack and pushes the result onto the stack.
    /// <para><c>or</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 | value2</remarks>
    public static IOpCodeEmitter Or(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Or);
        return emitter;
    }

    /// <summary>
    /// Removes the top-most value from the stack.
    /// <para><c>pop</c></para>
    /// </summary>
    /// <remarks>..., value -&gt; ...</remarks>
    public static IOpCodeEmitter PopFromStack(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Pop);
        return emitter;
    }

    /// <summary>
    /// Removes the top-most values from the stack <paramref name="times"/> number of times.
    /// <para><c>pop</c> multiple <paramref name="times"/></para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> was less than 0.</exception>
    /// <remarks>..., value -&gt; ...</remarks>
    public static IOpCodeEmitter PopFromStack(this IOpCodeEmitter emitter, int times)
    {
        if (times < 0)
            throw new ArgumentOutOfRangeException(nameof(times), "Expected at least 0 times.");

        for (int i = 0; i < times; ++i)
        {
            emitter.Emit(OpCodes.Pop);
        }

        return emitter;
    }

    /// <summary>
    /// Divides the top two values on the stack and pushes the remainder onto the stack. If both values are integers, the value will be truncated towards zero.
    /// <para>
    /// The result will always be smaller in magnitude than the divisor.
    /// The sign of the result will always equal the sign of the dividend.
    /// </para>
    /// <para>
    /// With floating point numbers:
    /// The remainder of dividing a number by zero will result in <see langword="NaN"/>.
    /// The remainder of dividing infinity by anything will result in <see langword="NaN"/>.
    /// The remainder of dividing a number by infinity will be that number.
    /// </para>
    /// <para>
    /// With integer numbers:
    /// The remainder of dividing a number by zero will result in a <see cref="DivideByZeroException"/>.
    /// It's possible for getting the remainder of division to produce a <see cref="ArithmeticException"/> when the output can't be represented by the operation.
    /// </para>
    /// <para><c>rem</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 % value2</remarks>
    public static IOpCodeEmitter Modulo(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Rem);
        return emitter;
    }

    /// <summary>
    /// Divides the top two unsigned values on the stack and pushes the remainder onto the stack. If both values are integers, the value will be truncated towards zero.
    /// <para>
    /// The result will always be smaller than the divisor.
    /// The result will always be positive or zero.
    /// </para>
    /// <para>
    /// The remainder of dividing a number by zero will result in a <see cref="DivideByZeroException"/>.
    /// </para>
    /// <para><c>rem.un</c></para>
    /// </summary>
    /// <remarks>..., value1, value2 -&gt; ..., value1 % value2</remarks>
    public static IOpCodeEmitter ModuloUnsigned(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Rem_Un);
        return emitter;
    }

    /// <summary>
    /// If the executing method isn't void removes the top value on the stack and returns control to the calling method.
    /// <para>Returning inside a try, filter, catch, or finally block is invalid.</para>
    /// <para><c>ret</c></para>
    /// </summary>
    /// <remarks>..., (return value if not void) -&gt; (jump to caller)</remarks>
    public static IOpCodeEmitter Return(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Ret);
        return emitter;
    }

    /// <summary>
    /// Rethrow the currently caught exception without modifying the stack trace of the exception.
    /// <para>Rethrowing is only valid inside a catch block.</para>
    /// <para><c>rethrow</c></para>
    /// </summary>
    /// <remarks>... -&gt; (throw exception)</remarks>
    public static IOpCodeEmitter Rethrow(this IOpCodeEmitter emitter)
    {
        emitter.Emit(OpCodes.Rethrow);
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