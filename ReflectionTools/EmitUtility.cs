using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif
#if NET45_OR_GREATER || !NETFRAMEWORK
using System.Collections.Generic;
#endif

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Utilities for creating dynamically generated code.
/// </summary>
public static class EmitUtility
{
    private static Type? _opCodeEnumType;
    private static OpCode[]? _allOpCodes;
    private static OpCode[]? _opCodesByValue;
    private static ReadOnlyCollection<OpCode>? _allOpCodesReadonly;

    /// <summary>
    /// A list of all op-codes sorted by their value code.
    /// </summary>
#if NET45_OR_GREATER || !NETFRAMEWORK
    public static IReadOnlyList<OpCode> AllOpCodes
#else
    public static ReadOnlyCollection<OpCode> AllOpCodes
#endif
    {
        get
        {
            if (_allOpCodesReadonly == null)
                SetupOpCodeInfo();

            return _allOpCodesReadonly!;
        }
    }

    /// <summary>
    /// Get an op-code from it's value code, or <see langword="null"/> if the value is invalid.
    /// </summary>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static OpCode? GetOpCodeFromValue(short opCodeValue)
    {
        if (_opCodesByValue == null)
            SetupOpCodeInfo();

        ushort index = unchecked( (ushort)opCodeValue );

        if (index >= _opCodesByValue!.Length)
        {
            return null;
        }

        return _opCodesByValue[index];
    }

    /// <summary>
    /// Get the label ID from a <see cref="Label"/> object.
    /// </summary>
    /// <remarks>Uses an unsafe cast to an integer, may not work in some non-standard .NET implementations.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static unsafe int GetLabelId(this Label label) => *(int*)&label;

    /// <summary>
    /// Loads an argument from an index.
    /// </summary>
    public static void EmitArgument(ILGenerator il, int index, bool set, bool byref = false)
    {
        if (index > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(index));
        OpCode code;
        if (set)
        {
            code = index > byte.MaxValue ? OpCodes.Starg : OpCodes.Starg_S;
            if (index > byte.MaxValue)
                il.Emit(code, (short)index);
            else
                il.Emit(code, (byte)index);
            return;
        }
        if (byref)
        {
            code = index > byte.MaxValue ? OpCodes.Ldarga : OpCodes.Ldarga_S;
            if (index > byte.MaxValue)
                il.Emit(code, (short)index);
            else
                il.Emit(code, (byte)index);
            return;
        }

        if (index is < 4 and > -1)
        {
            il.Emit(index switch
            {
                0 => OpCodes.Ldarg_0,
                1 => OpCodes.Ldarg_1,
                2 => OpCodes.Ldarg_2,
                _ => OpCodes.Ldarg_3
            });
            return;
        }

        code = index > byte.MaxValue ? OpCodes.Ldarg : OpCodes.Ldarg_S;
        if (index > byte.MaxValue)
            il.Emit(code, (short)index);
        else
            il.Emit(code, (byte)index);
    }

    /// <summary>
    /// Emit an Int32.
    /// </summary>
    public static void LoadConstantI4(ILGenerator generator, int number)
    {
        OpCode code = number switch
        {
            -1 => OpCodes.Ldc_I4_M1,
            0 => OpCodes.Ldc_I4_0,
            1 => OpCodes.Ldc_I4_1,
            2 => OpCodes.Ldc_I4_2,
            3 => OpCodes.Ldc_I4_3,
            4 => OpCodes.Ldc_I4_4,
            5 => OpCodes.Ldc_I4_5,
            6 => OpCodes.Ldc_I4_6,
            7 => OpCodes.Ldc_I4_7,
            8 => OpCodes.Ldc_I4_8,
            _ => OpCodes.Ldc_I4
        };
        if (number is < -1 or > 8)
            generator.Emit(code, number);
        else
            generator.Emit(code);
    }

    /// <summary>
    /// Loads a parameter from an index.
    /// </summary>
    public static void EmitParameter(this ILGenerator generator, int index, bool byref = false, Type? type = null, Type? targetType = null)
        => EmitParameter(generator, index, null, byref, type, targetType);

    /// <summary>
    /// Loads a parameter from an index.
    /// </summary>
    public static void EmitParameter(this ILGenerator generator, int index, string? castErrorMessage, bool byref = false, Type? type = null, Type? targetType = null)
    {
        EmitParameter(generator, null, index, castErrorMessage, byref, type, targetType);
    }
    internal static void EmitParameter(this ILGenerator generator, string? logSource, int index, string? castErrorMessage, bool byref = false, Type? type = null, Type? targetType = null)
    {
        if (index > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (!byref && type != null && targetType != null && type.IsValueType && targetType.IsValueType && type != targetType)
            throw new ArgumentException($"Types not compatible; input type: {type.FullName}, target type: {targetType.FullName}.", nameof(type));

        if (byref)
        {
            OpCode code2 = index > byte.MaxValue ? OpCodes.Ldarga : OpCodes.Ldarga_S;
            if (index > byte.MaxValue)
                generator.Emit(code2, (short)index);
            else
                generator.Emit(code2, (byte)index);
            if (logSource != null)
                Accessor.Logger.LogDebug(logSource, $"IL:  {(index > ushort.MaxValue ? "ldarga" : "ldarga.s")} <{index.ToString(CultureInfo.InvariantCulture)}>");
            return;
        }

        OpCode code = index switch
        {
            0 => OpCodes.Ldarg_0,
            1 => OpCodes.Ldarg_1,
            2 => OpCodes.Ldarg_2,
            3 => OpCodes.Ldarg_3,
            <= byte.MaxValue => OpCodes.Ldarg_S,
            _ => OpCodes.Ldarg
        };
        if (logSource != null)
        {
            Accessor.Logger.LogDebug(logSource, index switch
            {
                0 => "IL:  ldarg.0",
                1 => "IL:  ldarg.1",
                2 => "IL:  ldarg.2",
                3 => "IL:  ldarg.3",
                <= byte.MaxValue => $"IL:  ldarg.s <{index.ToString(CultureInfo.InvariantCulture)}",
                _ => $"IL:  ldarg <{index.ToString(CultureInfo.InvariantCulture)}"
            });
        }
        if (index > 3)
        {
            if (index > byte.MaxValue)
                generator.Emit(code, (ushort)index);
            else
                generator.Emit(code, (byte)index);
        }
        else
            generator.Emit(code);

        if (type == null || targetType == null || type == typeof(void) || targetType == typeof(void))
            return;

        Accessor.CheckExceptionConstructors();
        if (type.IsValueType && !targetType.IsValueType)
        {
            generator.Emit(OpCodes.Box, type);
            if (logSource != null)
                Accessor.Logger.LogDebug(logSource, $"IL:  box <{type.FullName}>");
        }
        else if (!type.IsValueType && targetType.IsValueType)
        {
            generator.Emit(OpCodes.Unbox_Any, targetType);
            if (logSource != null)
                Accessor.Logger.LogDebug(logSource, $"IL:  unbox.any <{targetType.FullName}>");
        }
        else if (!targetType.IsAssignableFrom(type) && (Accessor.CastExCtor != null || Accessor.NreExCtor != null))
        {
            Label lbl = generator.DefineLabel();
            generator.Emit(OpCodes.Isinst, targetType);
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Brtrue, lbl);
            generator.Emit(OpCodes.Pop);
            if (index > 3)
            {
                if (index > byte.MaxValue)
                    generator.Emit(code, (ushort)index);
                else
                    generator.Emit(code, (byte)index);
            }
            else
                generator.Emit(code);
            generator.Emit(OpCodes.Dup);
            generator.Emit(OpCodes.Brfalse, lbl);
            generator.Emit(OpCodes.Pop);
            castErrorMessage ??= $"Invalid type passed to parameter {index.ToString(CultureInfo.InvariantCulture)}.";
            if (Accessor.CastExCtor != null)
                generator.Emit(OpCodes.Ldstr, castErrorMessage);
            generator.Emit(OpCodes.Newobj, Accessor.CastExCtor ?? Accessor.NreExCtor!);
            generator.Emit(OpCodes.Throw);
            generator.MarkLabel(lbl);
            if (logSource != null)
            {
                string lblId = lbl.GetLabelId().ToString(CultureInfo.InvariantCulture);
                Accessor.Logger.LogDebug(logSource, $"IL:  isinst <{targetType.FullName}>");
                Accessor.Logger.LogDebug(logSource, "IL:  dup");
                Accessor.Logger.LogDebug(logSource, $"IL:  brtrue <lbl_{lblId}>");
                Accessor.Logger.LogDebug(logSource, "IL:  pop");
                Accessor.Logger.LogDebug(logSource, index switch
                {
                    0 => "IL:  ldarg.0",
                    1 => "IL:  ldarg.1",
                    2 => "IL:  ldarg.2",
                    3 => "IL:  ldarg.3",
                    <= byte.MaxValue => $"IL:  ldarg.s <{index.ToString(CultureInfo.InvariantCulture)}",
                    _ => $"IL:  ldarg <{index.ToString(CultureInfo.InvariantCulture)}"
                });
                Accessor.Logger.LogDebug(logSource, "IL:  dup");
                Accessor.Logger.LogDebug(logSource, $"IL:  brfalse <lbl_{lblId}>");
                Accessor.Logger.LogDebug(logSource, "IL:  pop");
                if (Accessor.CastExCtor != null)
                    Accessor.Logger.LogDebug(logSource, $"IL:  ldstr \"{castErrorMessage}\"");
                Accessor.Logger.LogDebug(logSource, $"IL:  newobj <{(Accessor.CastExCtor?.DeclaringType ?? Accessor.NreExCtor!.DeclaringType!).FullName}(System.String)>");
                Accessor.Logger.LogDebug(logSource, "IL:  throw");
                Accessor.Logger.LogDebug(logSource, $"IL: lbl_{lblId}:");
            }
        }
    }

    /// <summary>
    /// Compare <see cref="OpCode"/>s.
    /// </summary>
    /// <param name="opcode">Original <see cref="OpCode"/>.</param>
    /// <param name="comparand"><see cref="OpCode"/> to compare to <paramref name="opcode"/>.</param>
    /// <param name="fuzzy">Changes how similar <see cref="OpCode"/>s are compared (<c>br</c> and <c>ble</c> will match, for example).</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsOfType(this OpCode opcode, OpCode comparand, bool fuzzy = false)
    {
        if (opcode == comparand)
            return true;
        if (opcode.IsStArg())
            return comparand.IsStArg();
        if (opcode.IsStLoc())
            return comparand.IsStLoc();
        if (!fuzzy)
        {
            if (opcode.IsLdArg())
                return comparand.IsLdArg();
            if (opcode.IsLdArg(true))
                return comparand.IsLdArg(true);
            if (opcode.IsLdLoc())
                return comparand.IsLdLoc();
            if (opcode.IsLdLoc(true))
                return comparand.IsLdLoc(true);
            if (opcode.IsLdFld())
                return comparand.IsLdFld();
            if (opcode.IsLdFld(true))
                return comparand.IsLdFld(true);
            if (opcode.IsLdFld(@static: true))
                return comparand.IsLdFld(@static: true);
            if (opcode.IsLdFld(true, @static: true))
                return comparand.IsLdFld(true, @static: true);
            if (opcode.IsLdc())
                return comparand.IsLdc();
            if (opcode.IsLdc(false, true))
                return comparand.IsLdc(false, true);
            if (opcode.IsLdc(false, false, true))
                return comparand.IsLdc(false, false, true);
            if (opcode.IsLdc(false, false, false, true))
                return comparand.IsLdc(false, false, false, true);
            if (opcode.IsLdc(false, false, false, false, true))
                return comparand.IsLdc(false, false, false, false, true);
            if (opcode.IsLdc(false, false, false, false, false, true))
                return comparand.IsLdc(false, false, false, false, false, true);
            if (opcode.IsBr(true))
                return comparand.IsBr(true);
            if (opcode.IsBr(false, true))
                return comparand.IsBr(false, true);
            if (opcode.IsBr(false, false, true))
                return comparand.IsBr(false, false, true);
            if (opcode.IsBr(false, false, false, true))
                return comparand.IsBr(false, false, false, true);
            if (opcode.IsBr(false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, true);
            if (opcode.IsBr(false, false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, false, true);
            if (opcode.IsBr(false, false, false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, false, false, true);
            if (opcode.IsBr(false, false, false, false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, false, false, false, true);
            if (opcode.IsBr(false, false, false, false, false, false, false, false, true))
                return comparand.IsBr(false, false, false, false, false, false, false, false, true);
        }
        else
        {
            if (opcode.IsLdArg(true, true))
                return comparand.IsLdArg(true, true);
            if (opcode.IsLdLoc(true, true))
                return comparand.IsLdLoc(true, true);
            if (opcode.IsLdFld(either: true, staticOrInstance: true))
                return comparand.IsLdFld(either: true, staticOrInstance: true);
            if (opcode.IsLdc(true, true, true, true, true, true))
                return comparand.IsLdc(true, true, true, true, true, true);
            if (opcode.IsBr(true, true, true, true, true, true, true, true, true))
                return comparand.IsBr(true, true, true, true, true, true, true, true, true);
            if (opcode.IsConv(true, false, false, false, false, false, false))
                return comparand.IsConv(true, false, false, false, false, false, false);
            if (opcode.IsConv(false, true, false, false, false, false, false))
                return comparand.IsConv(false, true, false, false, false, false, false);
            if (opcode.IsConv(false, false, true, false, false, false, false))
                return comparand.IsConv(false, false, true, false, false, false, false);
            if (opcode.IsConv(false, false, false, true, false, false, false))
                return comparand.IsConv(false, false, false, true, false, false, false);
            if (opcode.IsConv(false, false, false, false, true, false, false))
                return comparand.IsConv(false, false, false, false, true, false, false);
            if (opcode.IsConv(false, false, false, false, false, true, false))
                return comparand.IsConv(false, false, false, false, false, true, false);
            if (opcode.IsConv(false, false, false, false, false, false, true))
                return comparand.IsConv(false, false, false, false, false, false, true);
        }

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>stloc</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsStLoc(this OpCode opcode)
    {
        return opcode == OpCodes.Stloc || opcode == OpCodes.Stloc_S || opcode == OpCodes.Stloc_0 || opcode == OpCodes.Stloc_1 || opcode == OpCodes.Stloc_2 || opcode == OpCodes.Stloc_3;
    }

    /// <summary>
    /// Is this opcode any variants of <c>ldloc</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="byRef">Only match instructions that load by address.</param>
    /// <param name="either">Match instructions that load by value or address.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsLdLoc(this OpCode opcode, bool byRef = false, bool either = false)
    {
        if (opcode == OpCodes.Ldloc_S || opcode == OpCodes.Ldloc_0 || opcode == OpCodes.Ldloc_1 || opcode == OpCodes.Ldloc_2 || opcode == OpCodes.Ldloc_3 || opcode == OpCodes.Ldloc)
            return !byRef || either;
        if (opcode == OpCodes.Ldloca_S || opcode == OpCodes.Ldloca)
            return byRef || either;

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>ldfld</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="byRef">Only match instructions that load by address.</param>
    /// <param name="either">Match instructions that load by value or address.</param>
    /// <param name="static">Only match instructions that load static fields.</param>
    /// <param name="staticOrInstance">Match instructions that load static or instance fields.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsLdFld(this OpCode opcode, bool byRef = false, bool either = false, bool @static = false, bool staticOrInstance = false)
    {
        if (opcode == OpCodes.Ldfld)
            return (!byRef || either) && (!@static || staticOrInstance);
        if (opcode == OpCodes.Ldflda)
            return (byRef || either) && (!@static || staticOrInstance);
        if (opcode == OpCodes.Ldsfld)
            return (!byRef || either) && (@static || staticOrInstance);
        if (opcode == OpCodes.Ldsflda)
            return (byRef || either) && (@static || staticOrInstance);

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>starg</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsStArg(this OpCode opcode)
    {
        return opcode == OpCodes.Starg || opcode == OpCodes.Starg_S;
    }

    /// <summary>
    /// Is this opcode any variants of <c>stloc</c>.
    /// </summary>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="byRef">Only match instructions that load by address.</param>
    /// <param name="either">Match instructions that load by value or address.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsLdArg(this OpCode opcode, bool byRef = false, bool either = false)
    {
        if (opcode == OpCodes.Ldarg_S || opcode == OpCodes.Ldarg_0 || opcode == OpCodes.Ldarg_1 || opcode == OpCodes.Ldarg_2 || opcode == OpCodes.Ldarg_3 || opcode == OpCodes.Ldarg)
            return !byRef || either;
        if (opcode == OpCodes.Ldarga_S || opcode == OpCodes.Ldarga)
            return byRef || either;

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>br</c>.
    /// </summary>
    /// <remarks>Use <see cref="IsBr"/> for the same check but all parameters default to <see langword="false"/>.</remarks>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="br">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>br</c>.</param>
    /// <param name="brtrue">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>brtrue</c>.</param>
    /// <param name="brfalse">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>brfalse</c>.</param>
    /// <param name="beq">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>beq</c>.</param>
    /// <param name="bne">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bne</c>.</param>
    /// <param name="bge">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bge</c>.</param>
    /// <param name="ble">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ble</c>.</param>
    /// <param name="bgt">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bgt</c>.</param>
    /// <param name="blt">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>blt</c>.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsBrAny(this OpCode opcode, bool br = true, bool brtrue = true, bool brfalse = true,
        bool beq = true, bool bne = true, bool bge = true, bool ble = true, bool bgt = true, bool blt = true)
        => opcode.IsBr(br, brtrue, brfalse, beq, bne, bge, ble, bgt, blt);

    /// <summary>
    /// Is this opcode any variants of <c>br</c>.
    /// </summary>
    /// <remarks>Use <see cref="IsBrAny"/> for the same check but all parameters default to <see langword="true"/>.</remarks>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="br">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>br</c>.</param>
    /// <param name="brtrue">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>brtrue</c>.</param>
    /// <param name="brfalse">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>brfalse</c>.</param>
    /// <param name="beq">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>beq</c>.</param>
    /// <param name="bne">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bne</c>.</param>
    /// <param name="bge">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bge</c>.</param>
    /// <param name="ble">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ble</c>.</param>
    /// <param name="bgt">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>bgt</c>.</param>
    /// <param name="blt">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>blt</c>.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsBr(this OpCode opcode, bool br = false, bool brtrue = false, bool brfalse = false,
        bool beq = false, bool bne = false, bool bge = false, bool ble = false, bool bgt = false, bool blt = false)
    {
        if (opcode == OpCodes.Br_S || opcode == OpCodes.Br)
            return br;
        if (opcode == OpCodes.Brtrue_S || opcode == OpCodes.Brtrue)
            return brtrue;
        if (opcode == OpCodes.Brfalse_S || opcode == OpCodes.Brfalse)
            return brfalse;
        if (opcode == OpCodes.Beq_S || opcode == OpCodes.Beq)
            return beq;
        if (opcode == OpCodes.Bne_Un_S || opcode == OpCodes.Bne_Un)
            return bne;
        if (opcode == OpCodes.Bge_S || opcode == OpCodes.Bge || opcode == OpCodes.Bge_Un_S || opcode == OpCodes.Bge_Un)
            return bge;
        if (opcode == OpCodes.Ble_S || opcode == OpCodes.Ble || opcode == OpCodes.Ble_Un_S || opcode == OpCodes.Ble_Un)
            return ble;
        if (opcode == OpCodes.Bgt_S || opcode == OpCodes.Bgt || opcode == OpCodes.Bgt_Un_S || opcode == OpCodes.Bgt_Un)
            return bgt;
        if (opcode == OpCodes.Blt_S || opcode == OpCodes.Blt || opcode == OpCodes.Blt_Un_S || opcode == OpCodes.Blt_Un)
            return blt;

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>ldc</c>.
    /// </summary>
    /// <remarks>Use <see cref="IsBrAny"/> for the same check but all parameters default to <see langword="true"/>.</remarks>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="int">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldc.i4</c>.</param>
    /// <param name="long">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldc.i8</c>.</param>
    /// <param name="float">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldc.r4</c>.</param>
    /// <param name="double">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldc.r8</c>.</param>
    /// <param name="string">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldstr</c>.</param>
    /// <param name="null">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>ldnull</c>.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsLdc(this OpCode opcode, bool @int = true, bool @long = false, bool @float = false, bool @double = false, bool @string = false, bool @null = false)
    {
        if (opcode == OpCodes.Ldc_I4_0 || opcode == OpCodes.Ldc_I4_1 || opcode == OpCodes.Ldc_I4_S ||
            opcode == OpCodes.Ldc_I4 || opcode == OpCodes.Ldc_I4_2 || opcode == OpCodes.Ldc_I4_3 ||
            opcode == OpCodes.Ldc_I4_4 || opcode == OpCodes.Ldc_I4_5 || opcode == OpCodes.Ldc_I4_6 ||
            opcode == OpCodes.Ldc_I4_7 || opcode == OpCodes.Ldc_I4_8 || opcode == OpCodes.Ldc_I4_M1)
            return @int;
        if (opcode == OpCodes.Ldc_R4)
            return @float;
        if (opcode == OpCodes.Ldc_R8)
            return @double;
        if (opcode == OpCodes.Ldc_I8)
            return @long;
        if (opcode == OpCodes.Ldstr)
            return @string;
        if (opcode == OpCodes.Ldnull)
            return @null;

        return false;
    }

    /// <summary>
    /// Is this opcode any variants of <c>conv</c>.
    /// </summary>
    /// <remarks>Use <see cref="IsBrAny"/> for the same check but all parameters default to <see langword="true"/>.</remarks>
    /// <param name="opcode"><see cref="OpCode"/> to check.</param>
    /// <param name="nint">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i</c> or <c>conv.u</c>.</param>
    /// <param name="byte">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i1</c> or <c>conv.u1</c>.</param>
    /// <param name="short">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i2</c> or <c>conv.u2</c>.</param>
    /// <param name="int">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i4</c> or <c>conv.u4</c>.</param>
    /// <param name="long">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.i8</c> or <c>conv.u8</c>.</param>
    /// <param name="float">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.r4</c> or <c>conv.r.un</c>.</param>
    /// <param name="double">Return <see langword="true"/> if <paramref name="opcode"/> is any variant of <c>conv.r8</c>.</param>
    /// <param name="fromUnsigned">Allow converting from unsigned checks.</param>
    /// <param name="toUnsigned">Allow converting to unsigned checks.</param>
    /// <param name="signed">Allow converting to signed checks.</param>
    /// <param name="overflowCheck">Allow overflow checks.</param>
    /// <param name="noOverflowCheck">Allow no overflow checks.</param>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsConv(this OpCode opcode, bool nint = true, bool @byte = true, bool @short = true, bool @int = true, bool @long = true, bool @float = true, bool @double = true,
        bool fromUnsigned = true, bool toUnsigned = true, bool signed = true, bool overflowCheck = true, bool noOverflowCheck = true)
    {
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I || toUnsigned && opcode == OpCodes.Conv_U) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I || fromUnsigned && opcode == OpCodes.Conv_Ovf_I_Un))
            return nint;
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I1 || toUnsigned && opcode == OpCodes.Conv_U1) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I1 || fromUnsigned && opcode == OpCodes.Conv_Ovf_I1_Un))
            return @byte;
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I2 || toUnsigned && opcode == OpCodes.Conv_U2) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I2 || fromUnsigned && opcode == OpCodes.Conv_Ovf_I2_Un))
            return @short;
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I4 || toUnsigned && opcode == OpCodes.Conv_U4) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I4 || fromUnsigned && opcode == OpCodes.Conv_Ovf_I4_Un))
            return @int;
        if (noOverflowCheck && (signed && opcode == OpCodes.Conv_I8 || toUnsigned && opcode == OpCodes.Conv_U8) || overflowCheck && (signed && opcode == OpCodes.Conv_Ovf_I8 || fromUnsigned && opcode == OpCodes.Conv_Ovf_I8_Un))
            return @long;
        if (noOverflowCheck && (opcode == OpCodes.Conv_R4 || fromUnsigned && opcode == OpCodes.Conv_R_Un))
            return @float;
        if (noOverflowCheck && opcode == OpCodes.Conv_R8)
            return @double;

        return false;
    }

    /// <summary>
    /// Return the correct call <see cref="OpCode"/> to use depending on the method. Usually you will use <see cref="GetCallRuntime"/> instead as it doesn't account for possible future keyword changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static OpCode GetCall(this MethodBase method)
    {
        return method.ShouldCallvirt() ? OpCodes.Callvirt : OpCodes.Call;
    }

    /// <summary>
    /// Return the correct call <see cref="OpCode"/> to use depending on the method at runtime. Doesn't account for future changes.
    /// </summary>
    /// <remarks>Note that not using call instead of callvirt may remove the check for a null instance.</remarks>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static OpCode GetCallRuntime(this MethodBase method)
    {
        return method.ShouldCallvirtRuntime() ? OpCodes.Callvirt : OpCodes.Call;
    }

    /// <summary>
    /// Parse an op-code in <see langword="ilasm"/> style, ex. <c>ldarg.1</c>. Case and culture insensitive.
    /// </summary>
    /// <exception cref="ArgumentNullException">Given string was <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Given string was empty.</exception>
    /// <exception cref="FormatException">Failed to find a matching op-code.</exception>
#if NET6_0_OR_GREATER
    public static OpCode ParseOpCode(ReadOnlySpan<char> opCodeString)
#else
    public static OpCode ParseOpCode(string opCodeString)
#endif
    {
#if !NET6_0_OR_GREATER
        if (opCodeString == null)
            throw new ArgumentNullException(nameof(opCodeString));
#endif
        if (opCodeString.Length == 0)
            throw new ArgumentException("String was empty.", nameof(opCodeString));

        if (!TryParseOpCode(opCodeString, out OpCode opCode))
            throw new FormatException("Failed to parse OpCode.");

        return opCode;
    }

    /// <summary>
    /// Parse an op-code in <see langword="ilasm"/> style, ex. <c>ldarg.1</c>. Case and culture insensitive.
    /// </summary>
    /// <returns><see langword="true"/> if a matching op-code was found, otherwise <see langword="false"/>.</returns>
#if NET6_0_OR_GREATER
    public static bool TryParseOpCode(ReadOnlySpan<char> opCodeString, out OpCode opCode)
#else
    public static bool TryParseOpCode(string opCodeString, out OpCode opCode)
#endif
    {
        opCode = default;

#if NET6_0_OR_GREATER
        if (opCodeString.Length == 0)
            return false;

        scoped ReadOnlySpan<char> stringToCheck;
#else
        if (string.IsNullOrEmpty(opCodeString))
            return false;

        string stringToCheck = opCodeString;
#endif
        if (opCodeString.IndexOf('.') != -1)
        {
#if NET6_0_OR_GREATER
            Span<char> toReplace = stackalloc char[opCodeString.Length];
            stringToCheck = toReplace;
            opCodeString.CopyTo(toReplace);
            for (int i = 0; i < toReplace.Length; ++i)
            {
                ref char c = ref toReplace[i];
                if (c == '.') c = '_';
            }
#else
            stringToCheck = opCodeString.Replace('.', '_');
#endif
        }
#if NET6_0_OR_GREATER
        else
        {
            stringToCheck = opCodeString;
        }
#endif

        if (_opCodeEnumType == null || _opCodesByValue == null)
        {
            SetupOpCodeInfo();
            if (_opCodeEnumType == null)
            {
                if (Accessor.LogWarningMessages)
                {
                    Accessor.Logger.LogWarning(
                        "EmitUtility.TryParseOpCode",
                        "The type 'System.Reflection.Emit.OpCodeValues' could not be found in the current environment."
                    );
                }
                return false;
            }
        }

        ushort value;
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        if (Enum.TryParse(_opCodeEnumType, stringToCheck, true, out object? resultEnum))
        {
            value = unchecked( (ushort)Convert.ToInt16(resultEnum) );
        }
        else
        {
            return false;
        }
#else
        try
        {
            object resultEnum = Enum.Parse(_opCodeEnumType, stringToCheck, true);
            value = unchecked( (ushort)Convert.ToInt16(resultEnum) );
        }
        catch (ArgumentException)
        {
            return false;
        }
#endif

        if (value >= _opCodesByValue!.Length)
            return false;

        opCode = _opCodesByValue[value];
        return true;

    }
    private static void SetupOpCodeInfo()
    {
        if (_allOpCodes == null || _opCodesByValue == null)
        {
            FieldInfo?[] opCodeFields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
            int c = 0;
            for (int i = 0; i < opCodeFields.Length; ++i)
            {
                ref FieldInfo? field = ref opCodeFields[i];
                if (field != null && field.FieldType == typeof(OpCode))
                    ++c;
                else
                {
                    field = null;
                    for (int j = i + 1; j < opCodeFields.Length; ++j)
                        opCodeFields[j - 1] = opCodeFields[j];
                }
            }

            OpCode[] opCodes = new OpCode[c];
            int maxValue = 0;
            for (int i = 0; i < opCodes.Length; ++i)
            {
                ref OpCode opCode = ref opCodes[i];
                opCode = (OpCode)opCodeFields[i]!.GetValue(null)!;
                int val = unchecked( (ushort)opCode.Value );
                if (maxValue < val)
                    maxValue = val;
            }

            Array.Sort(opCodes, (a, b) => a.Value.CompareTo(b.Value));

            OpCode[] opCodesByValue = new OpCode[maxValue + 1];
            for (int i = 0; i < opCodes.Length; ++i)
            {
                ref OpCode opCode = ref opCodes[i];
                opCodesByValue[unchecked( (ushort)opCode.Value) ] = opCode;
            }

            _allOpCodes = opCodes;
            _allOpCodesReadonly = new ReadOnlyCollection<OpCode>(opCodes);
            _opCodesByValue = opCodesByValue;
        }

        _opCodeEnumType ??= typeof(OpCode).Assembly.GetType("System.Reflection.Emit.OpCodeValues", false, false);
    }
}