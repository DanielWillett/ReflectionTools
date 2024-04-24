
using System;
using System.Collections.Generic;
using DanielWillett.ReflectionTools.Formatting;
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif
namespace DanielWillett.ReflectionTools;
internal static class InternalUtil
{
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    internal static int CountDigits(int num, bool commas = false)
    {
        int c = num switch
        {
            < -999999999 => 10,
            < -99999999 => 9,
            < -9999999 => 8,
            < -999999 => 7,
            < -99999 => 6,
            < -9999 => 5,
            < -999 => 4,
            < -99 => 3,
            < -9 => 2,
            < 0 => 1,
            <= 9 => 1,
            <= 99 => 2,
            <= 999 => 3,
            <= 9999 => 4,
            <= 99999 => 5,
            <= 999999 => 6,
            <= 9999999 => 7,
            <= 99999999 => 8,
            <= 999999999 => 9,
            _ => 10
        };
        if (commas)
            c += (c - 1) / 3;
        if (num < 0)
            ++c;
        return c;
    }
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    internal static int CountDigits(uint num, bool commas = false)
    {
        int c = num switch
        {
            <= 9 => 1,
            <= 99 => 2,
            <= 999 => 3,
            <= 9999 => 4,
            <= 99999 => 5,
            <= 999999 => 6,
            <= 9999999 => 7,
            <= 99999999 => 8,
            <= 999999999 => 9,
            _ => 10
        };
        if (commas)
            c += (c - 1) / 3;
        return c;
    }
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    internal static int CountDigits(long num, bool commas = false)
    {
        int c = num switch
        {
            < -999999999999999999 => 19,
            < -99999999999999999 => 18,
            < -9999999999999999 => 17,
            < -999999999999999 => 16,
            < -99999999999999 => 15,
            < -9999999999999 => 14,
            < -999999999999 => 13,
            < -99999999999 => 12,
            < -9999999999 => 11,
            < -999999999 => 10,
            < -99999999 => 9,
            < -9999999 => 8,
            < -999999 => 7,
            < -99999 => 6,
            < -9999 => 5,
            < -999 => 4,
            < -99 => 3,
            < -9 => 2,
            < 0 => 1,
            <= 9 => 1,
            <= 99 => 2,
            <= 999 => 3,
            <= 9999 => 4,
            <= 99999 => 5,
            <= 999999 => 6,
            <= 9999999 => 7,
            <= 99999999 => 8,
            <= 999999999 => 9,
            <= 9999999999 => 10,
            <= 99999999999 => 11,
            <= 999999999999 => 12,
            <= 9999999999999 => 13,
            <= 99999999999999 => 14,
            <= 999999999999999 => 15,
            <= 9999999999999999 => 16,
            <= 99999999999999999 => 17,
            <= 999999999999999999 => 18,
            _ => 19
        };
        if (commas)
            c += (c - 1) / 3;
        if (num < 0)
            ++c;
        return c;
    }
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    internal static int CountDigits(ulong num, bool commas = false)
    {
        int c = num switch
        {
            <= 9 => 1,
            <= 99 => 2,
            <= 999 => 3,
            <= 9999 => 4,
            <= 99999 => 5,
            <= 999999 => 6,
            <= 9999999 => 7,
            <= 99999999 => 8,
            <= 999999999 => 9,
            <= 9999999999 => 10,
            <= 99999999999 => 11,
            <= 999999999999 => 12,
            <= 9999999999999 => 13,
            <= 99999999999999 => 14,
            <= 999999999999999 => 15,
            <= 9999999999999999 => 16,
            <= 99999999999999999 => 17,
            <= 999999999999999999 => 18,
            <= 9999999999999999999 => 19,
            _ => 20
        };
        if (commas)
            c += (c - 1) / 3;
        return c;
    }
    
    public static void GetElementTypes(ref List<int>? elemTypes, ref Type type)
    {
        if (!type.HasElementType)
            return;

        elemTypes = new List<int>(1);
        for (Type? elementType = type; elementType != null; elementType = elementType.GetElementType())
        {
            type = elementType;
            if (type.IsPointer)
                elemTypes.Add(-1);
            else if (type.IsArray)
                elemTypes.Add(type.GetArrayRank());
            else if (type.IsByRef)
                elemTypes.Add(-(int)ByRefTypeMode.Ref - 1);
        }
    }
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static string? GetKeyword(Type type)
    {
        if (type.IsPrimitive)
        {
            if (type == typeof(byte))
                return "byte";
            if (type == typeof(sbyte))
                return "sbyte";
            if (type == typeof(ushort))
                return "ushort";
            if (type == typeof(short))
                return "short";
            if (type == typeof(uint))
                return "uint";
            if (type == typeof(int))
                return "int";
            if (type == typeof(ulong))
                return "ulong";
            if (type == typeof(long))
                return "long";
            if (type == typeof(float))
                return "float";
            if (type == typeof(double))
                return "double";
            if (type == typeof(nint))
                return "nint";
            if (type == typeof(nuint))
                return "nuint";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(char))
                return "char";
        }
        else
        {
            if (type == typeof(void))
                return "void";
            if (type == typeof(object))
                return "object";
            if (type == typeof(string))
                return "string";
            if (type == typeof(decimal))
                return "decimal";
        }

        return null;
    }
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static int GetKeywordLength(Type type)
    {
        if (type.IsPrimitive)
        {
            if (type == typeof(byte))
                return 4;
            if (type == typeof(sbyte))
                return 5;
            if (type == typeof(ushort))
                return 6;
            if (type == typeof(short))
                return 5;
            if (type == typeof(uint))
                return 4;
            if (type == typeof(int))
                return 3;
            if (type == typeof(ulong))
                return 5;
            if (type == typeof(long))
                return 4;
            if (type == typeof(float))
                return 5;
            if (type == typeof(double))
                return 6;
            if (type == typeof(nint))
                return 4;
            if (type == typeof(nuint))
                return 5;
            if (type == typeof(bool))
                return 4;
            if (type == typeof(char))
                return 4;
        }
        else
        {
            if (type == typeof(void))
                return 4;
            if (type == typeof(object))
                return 6;
            if (type == typeof(string))
                return 6;
            if (type == typeof(decimal))
                return 7;
        }

        return -1;
    }
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static Type? GetTypeFromKeyword(string keyword)
    {
        if (keyword.Equals("string", StringComparison.Ordinal))
            return typeof(string);
        if (keyword.Equals("void", StringComparison.Ordinal))
            return typeof(void);
        if (keyword.Equals("byte", StringComparison.Ordinal))
            return typeof(byte);
        if (keyword.Equals("sbyte", StringComparison.Ordinal))
            return typeof(sbyte);
        if (keyword.Equals("ushort", StringComparison.Ordinal))
            return typeof(ushort);
        if (keyword.Equals("short", StringComparison.Ordinal))
            return typeof(short);
        if (keyword.Equals("uint", StringComparison.Ordinal))
            return typeof(uint);
        if (keyword.Equals("int", StringComparison.Ordinal))
            return typeof(int);
        if (keyword.Equals("ulong", StringComparison.Ordinal))
            return typeof(ulong);
        if (keyword.Equals("object", StringComparison.Ordinal))
            return typeof(object);
        if (keyword.Equals("decimal", StringComparison.Ordinal))
            return typeof(decimal);
        if (keyword.Equals("long", StringComparison.Ordinal))
            return typeof(long);
        if (keyword.Equals("float", StringComparison.Ordinal))
            return typeof(float);
        if (keyword.Equals("double", StringComparison.Ordinal))
            return typeof(double);
        if (keyword.Equals("nint", StringComparison.Ordinal))
            return typeof(nint);
        if (keyword.Equals("nuint", StringComparison.Ordinal))
            return typeof(nuint);
        if (keyword.Equals("bool", StringComparison.Ordinal))
            return typeof(bool);
        if (keyword.Equals("char", StringComparison.Ordinal))
            return typeof(char);

        return null;
    }
}
