﻿#if NET40_OR_GREATER || !NETFRAMEWORK
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
}