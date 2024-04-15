using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
#if NETFRAMEWORK || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)
using System.Text;
#endif

namespace DanielWillett.ReflectionTools.Formatting;

/// <inheritdoc cref="IOpCodeFormatter"/>
public class DefaultOpCodeFormatter : IOpCodeFormatter
{
    /// <summary><see langword="private"/></summary>
    protected const string LitPrivate = "private";
    /// <summary><see langword="protected"/></summary>
    protected const string LitProtected = "protected";
    /// <summary><see langword="public"/></summary>
    protected const string LitPublic = "public";
    /// <summary><see langword="internal"/></summary>
    protected const string LitInternal = "internal";
    /// <summary><see langword="private protected"/></summary>
    protected const string LitPrivateProtected = "private protected";
    /// <summary><see langword="protected internal"/></summary>
    protected const string LitProtectedInternal = "protected internal";
    /// <summary><see langword="static"/></summary>
    protected const string LitStatic = "static";
    /// <summary><see langword="event"/></summary>
    protected const string LitEvent = "event";
    /// <summary><see langword="readonly"/></summary>
    protected const string LitReadonly = "readonly";
    /// <summary><see langword="const"/></summary>
    protected const string LitConst = "const";
    /// <summary><see langword="abstract"/></summary>
    protected const string LitAbstract = "abstract";
    /// <summary><see langword="ref"/></summary>
    protected const string LitRef = "ref";
    /// <summary><see langword="out"/></summary>
    protected const string LitOut = "out";
    /// <summary><see langword="enum"/></summary>
    protected const string LitEnum = "enum";
    /// <summary><see langword="class"/></summary>
    protected const string LitClass = "class";
    /// <summary><see langword="struct"/></summary>
    protected const string LitStruct = "struct";
    /// <summary><see langword="interface"/></summary>
    protected const string LitInterface = "interface";
    /// <summary><see langword="delegate"/></summary>
    protected const string LitDelegate = "delegate";
    /// <summary><see langword="this"/></summary>
    protected const string LitThis = "this";
    /// <summary><see langword="params"/></summary>
    protected const string LitParams = "params";
    /// <summary><see langword="get"/></summary>
    protected const string LitGet = "get";
    /// <summary><see langword="set"/></summary>
    protected const string LitSet = "set";
    /// <summary><see langword="add"/></summary>
    protected const string LitAdd = "add";
    /// <summary><see langword="remove"/></summary>
    protected const string LitRemove = "remove";
    /// <summary><see langword="raise"/></summary>
    protected const string LitRaise = "raise";

    /// <summary>
    /// Should formatted members and types use their full (namespace-declared) names? Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseFullTypeNames { get; set; }

    /// <summary>
    /// Should formatted keywords for types instead of CLR type names, ex. <see langword="int"/> instead of <see langword="Int32"/>. Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseTypeKeywords { get; set; } = true;


#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    /// <inheritdoc/>
    public virtual int GetFormatLength(OpCode opCode) => (opCode.Name ?? throw new ArgumentNullException(nameof(opCode))).Length;

    /// <inheritdoc/>
    public virtual int Format(OpCode opCode, Span<char> output)
    {
        ReadOnlySpan<char> code = opCode.Name ?? throw new ArgumentNullException(nameof(opCode));
        code.CopyTo(output);
        return code.Length;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(Label label) => InternalUtil.CountDigits((uint)label.GetLabelId());

    /// <inheritdoc/>
    public virtual int Format(Label label, Span<char> output)
    {
        if (!((uint)label.GetLabelId()).TryFormat(output, out int chrsWritten, "N0", CultureInfo.InvariantCulture))
            throw new IndexOutOfRangeException();

        return chrsWritten;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(OpCode opCode, object? operand, OpCodeFormattingContext usageContext)
    {
        int fmtLen = GetFormatLength(opCode);
        switch (opCode.OperandType)
        {
            case OperandType.ShortInlineBrTarget:
            case OperandType.InlineBrTarget:
                if (operand is not Label lbl)
                    break;

                return fmtLen + 1 + InternalUtil.CountDigits((uint)lbl.GetLabelId());

            case OperandType.InlineField:
                if (operand is not FieldInfo field)
                    break;

                return fmtLen + 1 + GetFormatLength(field, includeDefinitionKeywords: false);

            case OperandType.ShortInlineI:
            case OperandType.InlineI8:
            case OperandType.InlineI:
                try
                {
                    long num = Convert.ToInt64(operand);
                    return fmtLen + 1 + InternalUtil.CountDigits(num);
                }
                catch
                {
                    break;
                }

            case OperandType.InlineMethod:
                if (operand is not MethodBase method)
                    break;

                return fmtLen + 1 + GetFormatLength(method, includeDefinitionKeywords: false);

            case OperandType.ShortInlineR:
            case OperandType.InlineR:
                try
                {
                    double num = Convert.ToDouble(operand);
                    // 0.000000
                    return fmtLen + 1 + InternalUtil.CountDigits((long)Math.Round(num)) + 7;
                }
                catch
                {
                    break;
                }

            case OperandType.InlineSig:
                try
                {
                    long num = Convert.ToInt64(operand);
                    return fmtLen + 1 + InternalUtil.CountDigits(num);
                }
                catch
                {
                    break;
                }

            case OperandType.InlineString:
                if (operand is not string str)
                    break;

                return fmtLen + 3 + str.Length;

            case OperandType.InlineSwitch:
                if (operand is not Label[] jumps)
                    break;

                fmtLen = fmtLen + 3 + Environment.NewLine.Length * (2 + jumps.Length) + jumps.Length * 14;
                for (int i = 0; i < jumps.Length; ++i)
                    fmtLen += InternalUtil.CountDigits((uint)jumps[i].GetLabelId()) + InternalUtil.CountDigits(i);

                return fmtLen;

            case OperandType.InlineTok:
                switch (operand)
                {
                    case Type typeToken:
                        return fmtLen + 1 + GetFormatLength(typeToken);
                    case MethodBase methodToken:
                        return fmtLen + 1 + GetFormatLength(methodToken);
                    case FieldInfo fieldToken:
                        return fmtLen + 1 + GetFormatLength(fieldToken);
                }

                break;

            case OperandType.InlineType:
                if (operand is not Type type)
                    break;

                return GetFormatLength(type);

            case OperandType.ShortInlineVar:
            case OperandType.InlineVar:
                if (operand is LocalBuilder lb)
                {
                    if (lb.LocalType == null)
                        return fmtLen + 1 + InternalUtil.CountDigits((uint)lb.LocalIndex);

                    return fmtLen + 4 + InternalUtil.CountDigits((uint)lb.LocalIndex) + GetFormatLength(lb.LocalType);
                }

                try
                {
                    ulong num = Convert.ToUInt64(operand);
                    return fmtLen + 1 + InternalUtil.CountDigits(num);
                }
                catch
                {
                    break;
                }
        }

        return fmtLen;
    }

    /// <inheritdoc/>
    public virtual int Format(OpCode opCode, object? operand, Span<char> output, OpCodeFormattingContext usageContext)
    {
        int index = Format(opCode, output);
        output[index] = ' ';
        ++index;
        switch (opCode.OperandType)
        {
            case OperandType.ShortInlineBrTarget:
            case OperandType.InlineBrTarget:
                if (operand is not Label lbl)
                    break;

                output[index] = ' ';
                ++index;
                uint lblId = (uint)lbl.GetLabelId();
                if (!lblId.TryFormat(output[index..], out int charsWritten, "N0", CultureInfo.InvariantCulture))
                    throw new IndexOutOfRangeException();

                return index + charsWritten;

            case OperandType.InlineField:
                if (operand is not FieldInfo field)
                    break;

                output[index] = ' ';
                ++index;
                index += Format(field, output[index..], includeDefinitionKeywords: false);
                return index;

            case OperandType.ShortInlineI:
            case OperandType.InlineI8:
            case OperandType.InlineI:
                long l64;
                try
                {
                    l64 = Convert.ToInt64(operand);
                }
                catch
                {
                    break;
                }

                output[index] = ' ';
                ++index;
                if (!l64.TryFormat(output[index..], out charsWritten, "N0", CultureInfo.InvariantCulture))
                    throw new IndexOutOfRangeException();

                return index + charsWritten;

            case OperandType.InlineMethod:
                if (operand is not MethodBase method)
                    break;

                output[index] = ' ';
                ++index;
                index += Format(method, output[index..], includeDefinitionKeywords: false);
                return index;

            case OperandType.ShortInlineR:
            case OperandType.InlineR:
                double r8;
                try
                {
                    r8 = Convert.ToDouble(operand);
                }
                catch
                {
                    break;
                }

                output[index] = ' ';
                ++index;
                if (!r8.TryFormat(output[index..], out charsWritten, "F6", CultureInfo.InvariantCulture))
                    throw new IndexOutOfRangeException();

                return index + charsWritten;

            case OperandType.InlineSig:
                try
                {
                    l64 = Convert.ToInt64(operand);
                }
                catch
                {
                    break;
                }

                output[index] = ' ';
                ++index;
                if (!l64.TryFormat(output[index..], out charsWritten, "N0", CultureInfo.InvariantCulture))
                    throw new IndexOutOfRangeException();

                return index + charsWritten;

            case OperandType.InlineString:
                if (operand is not string str)
                    break;

                output[index] = ' ';
                ++index;
                output[index] = '"';
                ++index;
                str.AsSpan().CopyTo(output[index..]);
                index += str.Length;
                output[index] = '"';
                return index + 1;

            case OperandType.InlineSwitch:
                if (operand is not Label[] jumps)
                    break;

                output[index] = ' ';
                ++index;
                ReadOnlySpan<char> nl = Environment.NewLine;
                nl.CopyTo(output[index..]);
                index += nl.Length;
                output[index] = '{';
                ++index;
                for (int i = 0; i < jumps.Length; ++i)
                {
                    nl.CopyTo(output[index..]);
                    index += nl.Length;
                    output[index] = ' ';
                    output[index + 1] = ' ';
                    index += 2;

                    if (!((uint)i).TryFormat(output[index..], out charsWritten, "N0", CultureInfo.InvariantCulture))
                        throw new IndexOutOfRangeException();

                    index += charsWritten;
                    output[index]     = ' ';
                    output[index + 1] = '=';
                    output[index + 2] = '>';
                    output[index + 3] = ' ';
                    output[index + 4] = 'L';
                    output[index + 5] = 'a';
                    output[index + 6] = 'b';
                    output[index + 7] = 'e';
                    output[index + 8] = 'l';
                    output[index + 9] = ' ';
                    output[index + 10] = '#';
                    index += 11;

                    lblId = (uint)jumps[i].GetLabelId();
                    if (!lblId.TryFormat(output[index..], out charsWritten, "N0", CultureInfo.InvariantCulture))
                        throw new IndexOutOfRangeException();

                    index += charsWritten;
                }
                nl.CopyTo(output[index..]);
                index += nl.Length;
                output[index] = '}';
                return index + 1;

            case OperandType.InlineTok:
                switch (operand)
                {
                    case Type typeToken:
                        output[index] = ' ';
                        ++index;
                        index += Format(typeToken, output[index..]);
                        return index;

                    case MethodBase methodToken:
                        output[index] = ' ';
                        ++index;
                        index += Format(methodToken, output[index..]);
                        return index;

                    case FieldInfo fieldToken:
                        output[index] = ' ';
                        ++index;
                        index += Format(fieldToken, output[index..]);
                        return index;
                }

                break;

            case OperandType.InlineType:
                if (operand is not Type type)
                    break;

                output[index] = ' ';
                ++index;
                index += Format(type, output[index..]);
                return index;

            case OperandType.ShortInlineVar:
            case OperandType.InlineVar:
                if (operand is LocalBuilder lb)
                {
                    output[index] = ' ';
                    ++index;
                    if (!((uint)lb.LocalIndex).TryFormat(output[index..], out charsWritten, "N0", CultureInfo.InvariantCulture))
                        throw new IndexOutOfRangeException();

                    index += charsWritten;

                    if (lb.LocalType == null)
                        return index;

                    output[index]     = ' ';
                    output[index + 1] = ':';
                    output[index + 2] = ' ';
                    index += 3;
                    index += Format(lb.LocalType, output[index..]);
                    return index;
                }

                try
                {
                    l64 = Convert.ToInt64(operand);
                }
                catch
                {
                    break;
                }

                output[index] = ' ';
                ++index;
                if (!l64.TryFormat(output[index..], out charsWritten, "N0", CultureInfo.InvariantCulture))
                    throw new IndexOutOfRangeException();

                return index + charsWritten;
        }

        return index;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(Type type, bool includeDefinitionKeywords = false, bool isOutType = false)
    {
        string? s = GetTypeKeyword(type);
        if (s != null)
            return s.Length;

        if (type.IsPointer)
        {
            return 1 + GetFormatLength(type.GetElementType()!, includeDefinitionKeywords);
        }
        if (type.IsArray)
        {
            return 2 + GetFormatLength(type.GetElementType()!, includeDefinitionKeywords);
        }
        if (type.IsByRef && type.GetElementType() is { } elemType)
        {
            return 4 + GetFormatLength(elemType, includeDefinitionKeywords);
        }

        int length = 0;

        if (includeDefinitionKeywords)
        {
            length += GetVisibilityLength(type.GetVisibility()) + 1;
            if (type.IsValueType)
            {
                if (type.IsByRefLike)
                {
                    length += 4;
                }
                if (type.IsReadOnly())
                {
                    length += 9;
                }

                length += type.IsEnum ? 5 : 7;
            }
            else
            {
                bool isDelegate = type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
                if (type.IsInterface)
                    length += 10;
                else if (!isDelegate)
                {
                    if (type is { IsAbstract: true, IsSealed: false })
                        length += 9;

                    length += 6;
                }
                if (type.GetIsStatic())
                {
                    length += 7;
                }
                else if (isDelegate)
                {
                    length += 9;

                    try
                    {
                        Type returnType = Accessor.GetReturnType(type);
                        length += GetFormatLength(returnType) + 1;
                    }
                    catch (NotSupportedException)
                    {
                        // ignored
                    }
                }
            }
        }

        for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
        {
            if (!ReferenceEquals(nestingType, type))
                ++length;

            bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;

            length += GetNestedInvariantTypeNameLength(nestingType, willBreakNext);

            if (willBreakNext)
                break;
        }

        return length;
    }

    /// <inheritdoc/>
    public virtual int Format(Type type, Span<char> output, bool includeDefinitionKeywords = false, bool isOutType = false)
    {
        string? s = GetTypeKeyword(type);
        if (s != null)
        {
            s.AsSpan().CopyTo(output);
            return s.Length;
        }

        if (type.IsPointer)
        {
            int c = Format(type.GetElementType()!, output, includeDefinitionKeywords);
            output[c] = '*';
            return c + 1;
        }

        if (type.IsArray)
        {
            int c = Format(type.GetElementType()!, output, includeDefinitionKeywords);
            output[c] = '[';
            output[c + 1] = ']';
            return c + 2;
        }

        if (type.IsByRef && type.GetElementType() is { } elemType)
        {
            if (isOutType)
            {
                output[0] = 'o';
                output[1] = 'u';
                output[2] = 't';
            }
            else
            {
                output[0] = 'r';
                output[1] = 'e';
                output[2] = 'f';
            }

            output[3] = ' ';
            int c = Format(elemType, output[4..], includeDefinitionKeywords);
            return c + 4;
        }

        int index = 0;
        if (includeDefinitionKeywords)
        {
            WriteVisibility(type.GetVisibility(), ref index, output);
            output[index] = ' ';
            ++index;
            bool isDelegate = type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
            if (type.IsValueType)
            {
                if (type.IsReadOnly())
                {
                    WriteKeyword(LitReadonly, ref index, output, spaceSuffix: true);
                }

                if (type.IsByRefLike)
                {
                    WriteKeyword(LitRef, ref index, output, spaceSuffix: true);
                }
            }
            else if (type.GetIsStatic())
            {
                WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
            }
            else if (isDelegate)
            {
                WriteKeyword(LitDelegate, ref index, output, spaceSuffix: true);

                try
                {
                    Type returnType = Accessor.GetReturnType(type);
                    index += Format(returnType, output[index..]);
                    output[index] = ' ';
                    ++index;
                }
                catch (NotSupportedException)
                {
                    // ignored
                }
            }
            if (type.IsValueType)
            {
                if (type.IsEnum)
                {
                    WriteKeyword(LitEnum, ref index, output, spaceSuffix: true);
                }
                else
                {
                    WriteKeyword(LitStruct, ref index, output, spaceSuffix: true);
                }
            }
            else if (type.IsInterface)
            {
                WriteKeyword(LitInterface, ref index, output, spaceSuffix: true);
            }
            else
            {
                if (type is { IsAbstract: true, IsSealed: false })
                {
                    WriteKeyword(LitAbstract, ref index, output, spaceSuffix: true);
                }

                if (!isDelegate)
                {
                    WriteKeyword(LitClass, ref index, output, spaceSuffix: true);
                }
            }
        }


        int ct = 0;
        for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
        {
            bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;
            ++ct;
            if (willBreakNext)
                break;
        }

        if (ct == 1)
        {
            ReadOnlySpan<char> name = UseFullTypeNames ? type.FullName ?? type.Name : type.Name;

            if (type.IsGenericType)
            {
                int graveIndex = name.IndexOf('`');
                if (graveIndex != -1)
                    name = name[..graveIndex];
                name.CopyTo(output[index..]);
                index += name.Length;

                output[index] = '<';
                ++index;

                Type[] types = type.GetGenericArguments();
                for (int i = 0; i < types.Length; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    index += Format(types[i], output[index..], false);
                }

                output[index] = '>';
                ++index;
            }
            else
            {
                name.CopyTo(output[index..]);
                index += name.Length;
            }
        }
        else
        {
            Span<int> nestedSizes = stackalloc int[ct];
            int nestedInd = -1;
            for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
            {
                bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;

                nestedSizes[++nestedInd] = GetNestedInvariantTypeNameLength(nestingType, willBreakNext);
                if (willBreakNext)
                    break;
            }

            nestedInd = 0;
            for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
            {
                int pos = index;
                for (int i = nestedSizes.Length - 1; i > nestedInd; --i)
                    pos += nestedSizes[i];
                ++nestedInd;

                bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;
                if (!willBreakNext)
                {
                    pos += nestedSizes.Length - nestedInd;
                    output[pos - 1] = '.';
                }

                ReadOnlySpan<char> name = willBreakNext && UseFullTypeNames ? nestingType.FullName ?? nestingType.Name : nestingType.Name;

                if (nestingType.IsGenericType)
                {
                    int graveIndex = name.IndexOf('`');
                    if (graveIndex != -1)
                        name = name[..graveIndex];
                    name.CopyTo(output[pos..]);
                    pos += name.Length;

                    output[pos] = '<';
                    ++pos;

                    Type[] types = nestingType.GetGenericArguments();
                    for (int i = 0; i < types.Length; ++i)
                    {
                        if (i != 0)
                        {
                            output[pos] = ',';
                            output[pos + 1] = ' ';
                            pos += 2;
                        }

                        pos += Format(types[i], output[pos..], false);
                    }

                    output[pos] = '>';
                    ++pos;
                }
                else
                {
                    name.CopyTo(output[pos..]);
                }

                if (willBreakNext)
                    break;
            }

            for (int i = nestedSizes.Length - 1; i >= 0; --i)
                index += nestedSizes[i];

            index += ct - 1;
        }

        return index;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(MethodBase method, bool includeDefinitionKeywords = false)
    {
        return 0;
    }

    /// <inheritdoc/>
    public virtual int Format(MethodBase method, Span<char> output, bool includeDefinitionKeywords = false)
    {
        return 0;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(FieldInfo field, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = field.GetVisibility();
        int len = 0;
        if (includeDefinitionKeywords)
        {
            len += GetVisibilityLength(vis);
        }

        if (field.IsLiteral)
            len += 6;
        else if (field.IsStatic)
            len += 7;

        if (includeDefinitionKeywords && field is { IsLiteral: false, IsInitOnly: true })
            len += 9;

        len += GetFormatLength(field.FieldType, false) + 1 + field.Name.Length;

        if (field.DeclaringType != null)
            len += GetFormatLength(field.DeclaringType) + 1;

        return len;
    }

    /// <inheritdoc/>
    public virtual int Format(FieldInfo field, Span<char> output, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = field.GetVisibility();
        int index = 0;
        if (includeDefinitionKeywords)
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }

        if (field.IsLiteral)
        {
            WriteKeyword(LitConst, ref index, output, spaceSuffix: true);
        }
        else if (field.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        if (includeDefinitionKeywords && field is { IsLiteral: false, IsInitOnly: true })
        {
            WriteKeyword(LitReadonly, ref index, output, spaceSuffix: true);
        }

        index += Format(field.FieldType, output[index..]);
        output[index] = ' ';
        ++index;
        if (field.DeclaringType != null)
        {
            index += Format(field.DeclaringType, output[index..]);
            output[index] = '.';
            ++index;
        }

        ReadOnlySpan<char> name = field.Name;
        name.CopyTo(output[index..]);
        return index + name.Length;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false)
    {
        int len = GetFormatLength(property.PropertyType, false) + 1;

        ParameterInfo[] indexParameters = property.GetIndexParameters();

        if (indexParameters.Length == 0)
            len += property.Name.Length;
        else
            len += 4; // this

        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetSetMethod(true);

        MemberVisibility vis = Accessor.GetHighestVisibility(getMethod, setMethod, null);

        if (includeDefinitionKeywords)
        {
            len += GetVisibilityLength(vis) + 1;
        }

        if (property.DeclaringType != null)
            len += GetFormatLength(property.DeclaringType) + 1;

        if (getMethod != null && getMethod.IsStatic || setMethod != null && setMethod.IsStatic)
            len += 7;

        if (indexParameters.Length > 0)
        {
            len += 2 + (indexParameters.Length - 1) * 2;
            for (int i = 0; i < indexParameters.Length; ++i)
                len += GetFormatLength(indexParameters[i]);
        }

        if (includeAccessors)
        {
            len += 4; // { }

            MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

            CountAccessorIfExists(3, defVis, getMethod, ref len);

            CountAccessorIfExists(3, defVis, setMethod, ref len);
        }

        return len;
    }

    /// <inheritdoc/>
    public virtual int Format(PropertyInfo property, Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false)
    {
        ParameterInfo[] indexParameters = property.GetIndexParameters();

        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetSetMethod(true);

        int index = 0;
        MemberVisibility vis = Accessor.GetHighestVisibility(getMethod, setMethod);
        if (includeDefinitionKeywords)
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }

        if (getMethod != null && getMethod.IsStatic || setMethod != null && setMethod.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        index += Format(property.PropertyType, output[index..]);
        output[index] = ' ';
        ++index;

        if (property.DeclaringType != null)
        {
            index += Format(property.DeclaringType, output[index..]);
            output[index] = '.';
            ++index;
        }

        if (indexParameters.Length == 0)
        {
            ReadOnlySpan<char> name = property.Name;
            name.CopyTo(output[index..]);
            index += name.Length;
        }
        else
        {
            WriteKeyword(LitThis, ref index, output);
            output[index] = '[';
            ++index;
            for (int i = 0; i < indexParameters.Length; ++i)
            {
                if (i != 0)
                {
                    output[index] = ',';
                    output[index + 1] = ' ';
                    index += 2;
                }

                index += Format(indexParameters[i], output[index..]);
            }
            output[index] = ']';
            ++index;
        }

        if (includeAccessors)
        {
            output[index] = ' ';
            output[index + 1] = '{';
            output[index + 2] = ' ';
            index += 3;

            MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

            WriteAccessorIfExists(LitGet, defVis, getMethod, ref index, output);

            WriteAccessorIfExists(LitSet, defVis, setMethod, ref index, output);

            output[index] = '}';
            ++index;
        }

        return index;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(EventInfo @event, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false)
    {
        MethodInfo? addMethod = @event.GetAddMethod(true);
        MethodInfo? removeMethod = @event.GetRemoveMethod(true);
        MethodInfo? raiseMethod = @event.GetRaiseMethod(true);

        MemberVisibility vis = Accessor.GetHighestVisibility(addMethod, removeMethod, raiseMethod);

        // get delegate type
        Type delegateType = typeof(Delegate);
        MethodInfo? methodToUse = addMethod ?? removeMethod;
        if (methodToUse != null)
        {
            ParameterInfo[] addParameters = methodToUse.GetParameters();
            for (int i = 0; i < addParameters.Length; i++)
            {
                Type c = addParameters[i].ParameterType;
                if (!c.IsSubclassOf(delegateType))
                    continue;

                delegateType = c;
                break;
            }
        }

        int len = 6 + GetFormatLength(delegateType, false) + 1;

        len += @event.Name.Length;

        if (@event.DeclaringType != null)
            len += GetFormatLength(@event.DeclaringType) + 1;

        if (addMethod != null && addMethod.IsStatic || removeMethod != null && removeMethod.IsStatic || raiseMethod != null && raiseMethod.IsStatic)
            len += 7;

        if (includeAccessors)
        {
            len += 4; // { }

            MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

            CountAccessorIfExists(3, defVis, addMethod, ref len);

            CountAccessorIfExists(6, defVis, removeMethod, ref len);

            CountAccessorIfExists(5, defVis, raiseMethod, ref len);
        }

        return len;
    }

    /// <inheritdoc/>
    public virtual int Format(EventInfo @event, Span<char> output, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false)
    {
        MethodInfo? addMethod = @event.GetAddMethod(true);
        MethodInfo? removeMethod = @event.GetRemoveMethod(true);
        MethodInfo? raiseMethod = @event.GetRaiseMethod(true);

        MemberVisibility vis = Accessor.GetHighestVisibility(addMethod, removeMethod, raiseMethod);

        // get delegate type
        Type delegateType = typeof(Delegate);
        MethodInfo? methodToUse = addMethod ?? removeMethod;
        if (methodToUse != null)
        {
            ParameterInfo[] addParameters = methodToUse.GetParameters();
            for (int i = 0; i < addParameters.Length; i++)
            {
                Type c = addParameters[i].ParameterType;
                if (!c.IsSubclassOf(delegateType))
                    continue;

                delegateType = c;
                break;
            }
        }

        int index = 0;
        if (addMethod != null && addMethod.IsStatic || removeMethod != null && removeMethod.IsStatic || raiseMethod != null && raiseMethod.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        WriteKeyword(LitEvent, ref index, output, spaceSuffix: true);

        index += Format(delegateType, output[index..]);
        output[index] = ' ';
        ++index;

        if (@event.DeclaringType != null)
        {
            index += Format(@event.DeclaringType, output[index..]);
            output[index] = '.';
            ++index;
        }

        ReadOnlySpan<char> name = @event.Name;
        name.CopyTo(output[index..]);
        index += name.Length;

        if (includeAccessors)
        {
            output[index] = ' ';
            output[index + 1] = '{';
            output[index + 2] = ' ';
            index += 3;

            MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

            WriteAccessorIfExists(LitAdd, defVis, addMethod, ref index, output);

            WriteAccessorIfExists(LitRemove, defVis, removeMethod, ref index, output);

            WriteAccessorIfExists(LitRaise, defVis, raiseMethod, ref index, output);

            output[index] = '}';
            ++index;
        }

        return index;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(ParameterInfo parameter, bool isExtensionThisParameter = false)
    {
        TypeMetaInfo meta = default;
        int length = GetParmeterLength(parameter, ref meta, out _, isExtensionThisParameter);
        return length;
    }

    /// <inheritdoc/>
    public virtual int Format(ParameterInfo parameter, Span<char> output, bool isExtensionThisParameter = false)
    {
        TypeMetaInfo meta = default;
        Type type = parameter.ParameterType;
        meta.IsParams = type.IsArray && parameter.IsDefinedSafe<ParamArrayAttribute>();
        meta.Init(ref type, out string? elementKeyword, this);
        int index = 0;
        WriteParameter(parameter, output, ref index, in meta, elementKeyword, isExtensionThisParameter);
        return index;
    }

    /// <inheritdoc/>
    public virtual string Format(ParameterInfo parameter, bool isExtensionThisParameter = false)
    {
        TypeMetaInfo meta = default;
        int length = GetParmeterLength(parameter, ref meta, out string? elementKeyword, isExtensionThisParameter);

        Span<char> output = stackalloc char[length];
        int index = 0;

        WriteParameter(parameter, output, ref index, in meta, elementKeyword, isExtensionThisParameter);
        return new string(output[..index]);
    }

    /// <summary>
    /// Write the visibilty keyword for the given visibility.
    /// </summary>
    protected virtual void WriteVisibility(MemberVisibility visibility, ref int index, Span<char> output)
    {
        ReadOnlySpan<char> span = visibility switch
        {
            MemberVisibility.Private => LitPrivate,
            MemberVisibility.PrivateProtected => LitPrivateProtected,
            MemberVisibility.Protected => LitProtected,
            MemberVisibility.ProtectedInternal => LitProtectedInternal,
            MemberVisibility.Internal => LitInternal,
            MemberVisibility.Public => LitPublic,
            _ => default
        };

        if (span.Length == 0)
            return;

        span.CopyTo(output[index..]);
        index += span.Length;
    }

    /// <summary>
    /// Write the passed literal to the output, incrimenting index.
    /// </summary>
    protected void WriteKeyword(ReadOnlySpan<char> keyword, ref int index, Span<char> output, bool spacePrefix = false, bool spaceSuffix = false)
    {
        if (keyword.Length == 0)
        {
            if (!spacePrefix || !spaceSuffix)
                return;

            output[index] = ' ';
            ++index;
            return;
        }

        if (spacePrefix)
        {
            output[index] = ' ';
            ++index;
        }
        keyword.CopyTo(output[index..]);
        index += keyword.Length;
        if (!spaceSuffix)
            return;
        output[index] = ' ';
        ++index;
    }
#else

    /// <summary>
    /// Write the visibilty keyword for the given visibility.
    /// </summary>
    protected virtual unsafe void WriteVisibility(MemberVisibility visibility, ref int index, char* output)
    {
        string? span = visibility switch
        {
            MemberVisibility.Private => LitPrivate,
            MemberVisibility.PrivateProtected => LitPrivateProtected,
            MemberVisibility.Protected => LitProtected,
            MemberVisibility.ProtectedInternal => LitProtectedInternal,
            MemberVisibility.Internal => LitInternal,
            MemberVisibility.Public => LitPublic,
            _ => default
        };

        if (span == null)
            return;

        for (int i = 0; i < span.Length; ++i)
            output[index + i] = span[i];

        index += span.Length;
    }

    /// <inheritdoc/>
    public virtual string Format(OpCode opCode) => opCode.Name;
    
    /// <inheritdoc/>
    public virtual string Format(Label label) => ((uint)label.GetLabelId()).ToString("N0", CultureInfo.InvariantCulture);
    
    /// <inheritdoc/>
    public virtual string Format(OpCode opCode, object? operand, OpCodeFormattingContext usageContext)
    {
        string name = opCode.Name;
        switch (opCode.OperandType)
        {
            case OperandType.ShortInlineBrTarget:
            case OperandType.InlineBrTarget:
                if (operand is not Label lbl)
                    break;

                return name + " " + ((uint)lbl.GetLabelId()).ToString("N0", CultureInfo.InvariantCulture);

            case OperandType.InlineField:
                if (operand is not FieldInfo field)
                    break;

                return name + " " + Format(field);

            case OperandType.ShortInlineI:
            case OperandType.InlineI8:
            case OperandType.InlineI:
                long l64;
                try
                {
                    l64 = Convert.ToInt64(operand);
                }
                catch
                {
                    break;
                }

                return name + " " + l64.ToString("N0", CultureInfo.InvariantCulture);

            case OperandType.InlineMethod:
                if (operand is not MethodBase method)
                    break;

                return name + " " + Format(method);

            case OperandType.ShortInlineR:
            case OperandType.InlineR:
                double r8;
                try
                {
                    r8 = Convert.ToDouble(operand);
                }
                catch
                {
                    break;
                }

                return name + " " + r8.ToString("F6", CultureInfo.InvariantCulture);

            case OperandType.InlineSig:
                try
                {
                    l64 = Convert.ToInt64(operand);
                }
                catch
                {
                    break;
                }

                return name + " " + l64.ToString("N0", CultureInfo.InvariantCulture);

            case OperandType.InlineString:
                if (operand is not string str)
                    break;

                return name + " \"" + str + "\"";

            case OperandType.InlineSwitch:
                if (operand is not Label[] jumps)
                    break;

                StringBuilder switchBuilder = new StringBuilder(name)
                    .Append(' ')
                    .Append(Environment.NewLine)
                    .Append('{');

                for (int i = 0; i < jumps.Length; ++i)
                {
                    switchBuilder.Append(Environment.NewLine)
                        .Append(' ', 2)
                        .Append(((uint)i).ToString("N0", CultureInfo.InvariantCulture))
                        .Append(" => Label #")
                        .Append(((uint)jumps[i].GetLabelId()).ToString("N0", CultureInfo.InvariantCulture));
                }
                switchBuilder
                    .Append(Environment.NewLine)
                    .Append('}');

                return switchBuilder.ToString();

            case OperandType.InlineTok:
                switch (operand)
                {
                    case Type typeToken:
                        return name + " " + Format(typeToken);

                    case MethodBase methodToken:
                        return name + " " + Format(methodToken);

                    case FieldInfo fieldToken:
                        return name + " " + Format(fieldToken);
                }

                break;

            case OperandType.InlineType:
                if (operand is not Type type)
                    break;

                return name + " " + Format(type);

            case OperandType.ShortInlineVar:
            case OperandType.InlineVar:
                if (operand is LocalBuilder lb)
                {
                    name += " " + ((uint)lb.LocalIndex).ToString("N0", CultureInfo.InvariantCulture);
                    if (lb.LocalType == null)
                        return name;

                    return name + " : " + Format(lb.LocalType);
                }

                try
                {
                    l64 = Convert.ToInt64(operand);
                }
                catch
                {
                    break;
                }

                return name + " " + l64.ToString("N0", CultureInfo.InvariantCulture);
        }

        return name;
    }

    /// <inheritdoc/>
    public virtual string Format(MethodBase method, bool includeDefinitionKeywords = false) => throw new NotImplementedException();

    /// <inheritdoc/>
    public virtual string Format(FieldInfo field, bool includeDefinitionKeywords = false) => throw new NotImplementedException();

    /// <inheritdoc/>
    public virtual unsafe string Format(ParameterInfo parameter, bool isExtensionThisParameter = false)
    {
        TypeMetaInfo meta = default;
        int length = GetParmeterLength(parameter, ref meta, out string? elementKeyword, isExtensionThisParameter);

        char* output = stackalloc char[length];
        int index = 0;

        WriteParameter(parameter, output, ref index, in meta, elementKeyword, isExtensionThisParameter);
        return new string(output, 0, index);
    }

    /// <summary>
    /// Write the passed literal to the output, incrimenting index.
    /// </summary>
    protected unsafe void WriteKeyword(string keyword, ref int index, char* output, bool spacePrefix = false, bool spaceSuffix = false)
    {
        if (keyword.Length == 0)
        {
            if (!spacePrefix || !spaceSuffix)
                return;

            output[index] = ' ';
            ++index;
            return;
        }

        if (spacePrefix)
        {
            output[index] = ' ';
            ++index;
        }
        for (int i = 0; i < keyword.Length; ++i)
            output[i + index] = keyword[i];
        index += keyword.Length;
        if (!spaceSuffix)
            return;
        output[index] = ' ';
        ++index;
    }
#endif

    /// <summary>
    /// Counts the length of a property or event method with the given <paramref name="keywordLength"/>.
    /// </summary>
    protected virtual void CountAccessorIfExists(int keywordLength, MemberVisibility defVis, MethodInfo? method, ref int length)
    {
        if (method == null)
            return;

        MemberVisibility vis = method.GetVisibility();
        if (vis != defVis)
        {
            length += GetVisibilityLength(vis) + 1;
        }

        length += keywordLength + 2;
    }

    /// <summary>
    /// Calculate the length of a type name without adding declaring (nesting parent) types.
    /// </summary>
    protected virtual int GetNestedInvariantTypeNameLength(Type nestingType, bool allowFullTypeName)
    {
        int length = 0;
        string name = allowFullTypeName && UseFullTypeNames ? nestingType.FullName ?? nestingType.Name : nestingType.Name;

        if (nestingType.IsGenericType)
        {
            int graveIndex = name.IndexOf('`');
            if (graveIndex != -1)
                length += graveIndex;
            else length += name.Length;
            Type[] types = nestingType.GetGenericArguments();
            length += 2 + (types.Length - 1) * 2;

            for (int i = 0; i < types.Length; ++i)
            {
                Type type = types[i];
                if (type.IsArray)
                {
                    type = type.GetElementType()!;
                    length += 2;
                }
                string? s = GetTypeKeyword(type);
                if (s != null)
                    length += s.Length;
                else
                    length += GetNestedInvariantTypeNameLength(type, true);
            }
        }
        else length += name.Length;

        return length;
    }

    /// <summary>
    /// Calculate the length of a type name without adding definition keywords.
    /// </summary>
    protected virtual int GetNonDeclaritiveTypeNameLength(ref Type type, ref TypeMetaInfo metaInfo, out string? elementKeyword)
    {
        elementKeyword = GetTypeKeyword(type);
        if (elementKeyword != null)
            return elementKeyword.Length;
        
        metaInfo.Init(ref type, out elementKeyword, this);

        int length = metaInfo.LengthToAdd;

        if (elementKeyword != null)
        {
            length += elementKeyword.Length;
        }
        else
        {
            for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
            {
                if (!ReferenceEquals(nestingType, type))
                    ++length;

                bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;

                length += GetNestedInvariantTypeNameLength(nestingType, willBreakNext);

                if (willBreakNext)
                    break;
            }
        }

        return length;
    }

    /// <summary>
    /// Get the length of the visibilty keyword for the given visibility.
    /// </summary>
    protected virtual int GetVisibilityLength(MemberVisibility visibility)
    {
        return visibility switch
        {
            MemberVisibility.Private => 7,
            MemberVisibility.PrivateProtected => 17,
            MemberVisibility.Protected => 9,
            MemberVisibility.ProtectedInternal => 18,
            MemberVisibility.Internal => 8,
            MemberVisibility.Public => 6,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the language keyword for the type instead of the CLR type name, or <see langword="null"/> if the type doesn't have a keyword.
    /// </summary>
    protected virtual string? GetTypeKeyword(Type type)
    {
        if (!UseTypeKeywords)
            return null;
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
                return "string";
        }

        return null;
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false)
    {
        ParameterInfo[] indexParameters = property.GetIndexParameters();
        TypeMetaInfo* indexInfo = stackalloc TypeMetaInfo[indexParameters.Length];
        string?[]? parameterTypeNames = null;
            
        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetSetMethod(true);

        MemberVisibility vis = Accessor.GetHighestVisibility(getMethod, setMethod);

        Type? declaringType = property.DeclaringType;
        string? declTypeElementKeyword = null;
        bool isStatic = getMethod is { IsStatic: true } || setMethod is { IsStatic: true };
        Type returnType = property.PropertyType;
        bool isIndexer = indexParameters.Length > 0;
        TypeMetaInfo declTypeMetaInfo = default;
        TypeMetaInfo returnTypeMetaInfo = default;

        int length = GetNonDeclaritiveTypeNameLength(ref returnType, ref returnTypeMetaInfo, out string? returnElementKeyword) + 1;

        if (declaringType != null)
            length += GetNonDeclaritiveTypeNameLength(ref declaringType, ref declTypeMetaInfo, out declTypeElementKeyword) + 1;

        if (includeDefinitionKeywords)
            length += GetVisibilityLength(vis) + 1;

        if (!isIndexer)
            length += property.Name.Length;
        else
            length += 4; // this

        if (isStatic)
            length += 7;

        if (isIndexer)
        {
            length += 2 + (indexParameters.Length - 1) * 2;
            for (int i = 0; i < indexParameters.Length; ++i)
            {
                length += GetParmeterLength(indexParameters[i], ref indexInfo[i], out string? elementKeyword);
                if (elementKeyword != null)
                    (parameterTypeNames ??= new string[indexParameters.Length])[i] = elementKeyword;
            }
        }

        MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

        if (includeAccessors)
        {
            length += 4; // { }

            CountAccessorIfExists(3, defVis, getMethod, ref length);

            CountAccessorIfExists(3, defVis, setMethod, ref length);
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[length];
#else
        char* output = stackalloc char[length];
#endif
        int index = 0;
        if (includeDefinitionKeywords)
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }
        if (isStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        FormatType(returnType, in returnTypeMetaInfo, returnElementKeyword, output, ref index);
        
        output[index] = ' ';
        ++index;

        if (declaringType != null)
        {
            FormatType(declaringType, in declTypeMetaInfo, declTypeElementKeyword, output, ref index);
            output[index] = '.';
            ++index;
        }

        if (indexParameters.Length == 0)
        {
            string name = property.Name;
            for (int i = 0; i < name.Length; ++i)
                output[index + i] = name[i];
            index += name.Length;
        }
        else
        {
            WriteKeyword(LitThis, ref index, output);
            output[index] = '[';
            ++index;
            for (int i = 0; i < indexParameters.Length; ++i)
            {
                if (i != 0)
                {
                    output[index] = ',';
                    output[index + 1] = ' ';
                    index += 2;
                }

                WriteParameter(indexParameters[i], output, ref index, in indexInfo[i], parameterTypeNames?[i], false);
            }
            output[index] = ']';
            ++index;
        }

        if (includeAccessors)
        {
            output[index] = ' ';
            output[index + 1] = '{';
            output[index + 2] = ' ';
            index += 3;

            WriteAccessorIfExists(LitGet, defVis, getMethod, ref index, output);

            WriteAccessorIfExists(LitSet, defVis, setMethod, ref index, output);

            output[index] = '}';
            ++index;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(EventInfo @event, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false)
    {
        MethodInfo? addMethod = @event.GetAddMethod(true);
        MethodInfo? removeMethod = @event.GetRemoveMethod(true);
        MethodInfo? raiseMethod = @event.GetRaiseMethod(true);

        MemberVisibility vis = Accessor.GetHighestVisibility(addMethod, removeMethod, raiseMethod);

        // get delegate type
        Type delegateType = typeof(Delegate);
        MethodInfo? methodToUse = addMethod ?? removeMethod;
        if (methodToUse != null)
        {
            ParameterInfo[] addParameters = methodToUse.GetParameters();
            for (int i = 0; i < addParameters.Length; i++)
            {
                Type c = addParameters[i].ParameterType;
                if (!c.IsSubclassOf(delegateType))
                    continue;

                delegateType = c;
                break;
            }
        }


        Type? declaringType = @event.DeclaringType;
        string? declTypeElementKeyword = null;
        bool isStatic = addMethod is { IsStatic: true } || removeMethod is { IsStatic: true } || raiseMethod is { IsStatic: true };
        TypeMetaInfo declTypeMetaInfo = default;
        TypeMetaInfo handlerTypeMetaInfo = default;

        int length = GetNonDeclaritiveTypeNameLength(ref delegateType, ref handlerTypeMetaInfo, out _) + 1;

        if (declaringType != null)
            length += GetNonDeclaritiveTypeNameLength(ref declaringType, ref declTypeMetaInfo, out declTypeElementKeyword) + 1;

        if (includeDefinitionKeywords)
            length += GetVisibilityLength(vis) + 1;

        if (isStatic)
            length += 7;

        if (includeEventKeyword)
            length += 6;

        length += @event.Name.Length;

        MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

        if (includeAccessors)
        {
            length += 4; // { }

            CountAccessorIfExists(3, defVis, addMethod, ref length);

            CountAccessorIfExists(6, defVis, removeMethod, ref length);

            CountAccessorIfExists(5, defVis, raiseMethod, ref length);
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[length];
#else
        char* output = stackalloc char[length];
#endif
        int index = 0;
        if (includeDefinitionKeywords)
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }
        if (isStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        if (includeEventKeyword)
        {
            WriteKeyword(LitEvent, ref index, output, spaceSuffix: true);
        }

        FormatType(delegateType, in handlerTypeMetaInfo, null, output, ref index);
        
        output[index] = ' ';
        ++index;

        if (declaringType != null)
        {
            FormatType(declaringType, in declTypeMetaInfo, declTypeElementKeyword, output, ref index);
            output[index] = '.';
            ++index;
        }

        string name = @event.Name;
        for (int i = 0; i < name.Length; ++i)
            output[index + i] = name[i];
        index += name.Length;

        if (includeAccessors)
        {
            output[index] = ' ';
            output[index + 1] = '{';
            output[index + 2] = ' ';
            index += 3;

            WriteAccessorIfExists(LitAdd, defVis, addMethod, ref index, output);

            WriteAccessorIfExists(LitRemove, defVis, removeMethod, ref index, output);

            WriteAccessorIfExists(LitRaise, defVis, raiseMethod, ref index, output);

            output[index] = ' ';
            ++index;
        }
        
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(Type type, bool includeDefinitionKeywords = false, bool isOutType = false)
    {
        string? s = GetTypeKeyword(type);
        if (s != null)
            return s;

        TypeMetaInfo main = default;
        main.Init(ref type, out string? elementKeyword, this);

        TypeMetaInfo delegateRtnType = default;

        MemberVisibility vis = includeDefinitionKeywords ? type.GetVisibility() : default;
        bool isValueType = type.IsValueType;
        bool isAbstract = !isValueType && includeDefinitionKeywords && type is { IsAbstract: true, IsSealed: false };
        bool isDelegate = !isValueType && includeDefinitionKeywords && type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
        bool isReadOnly = isValueType && includeDefinitionKeywords && type.IsReadOnly();
        bool isStatic = !isValueType && includeDefinitionKeywords && type.GetIsStatic();
        bool isByRefType = false;
        Type? delegateReturnType = null;
#if NETCOREAPP2_1_OR_GREATER || NET || NETSTANDARD2_1_OR_GREATER
        isByRefType = includeDefinitionKeywords && type.IsByRefLike;
#endif

        int length = main.LengthToAdd;
        if (includeDefinitionKeywords && elementKeyword is null)
        {
            length += GetVisibilityLength(vis) + 1;
            if (isValueType)
            {
                if (isReadOnly)
                    length += 9;
                if (isByRefType)
                    length += 4;

                length += type.IsEnum ? 5 : 7;
            }
            else if (isDelegate)
            {
                length += 9;

                try
                {
                    delegateReturnType = Accessor.GetReturnType(type);
                    delegateRtnType.Init(ref delegateReturnType, out _, this);

                    ++length;
                    length += delegateRtnType.LengthToAdd;
                    for (Type? nestingType = delegateReturnType; nestingType != null; nestingType = nestingType.DeclaringType)
                    {
                        if (!ReferenceEquals(nestingType, delegateReturnType))
                            ++length;

                        bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;

                        length += GetNestedInvariantTypeNameLength(nestingType, willBreakNext);

                        if (willBreakNext)
                            break;
                    }
                }
                catch (NotSupportedException)
                {
                    // ignored
                }
            }
            else
            {
                length += type.IsInterface ? 10 : 6;
                if (isStatic)
                    length += 7;
                else if (isAbstract)
                    length += 9;
            }

        }

        if (elementKeyword != null)
        {
            length += elementKeyword.Length;
        }
        else
        {
            for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
            {
                if (!ReferenceEquals(nestingType, type))
                    ++length;

                bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;

                length += GetNestedInvariantTypeNameLength(nestingType, willBreakNext);

                if (willBreakNext)
                    break;
            }
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[length];
#else
        char* output = stackalloc char[length];
#endif
        int index = 0;
        if (main.IsByRef)
        {
            if (isOutType)
            {
                output[0] = 'o';
                output[1] = 'u';
                output[2] = 't';
            }
            else
            {
                output[0] = 'r';
                output[1] = 'e';
                output[2] = 'f';
            }

            output[3] = ' ';
            index += 4;
        }
        if (includeDefinitionKeywords && elementKeyword is null)
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
            if (isValueType)
            {
                if (isReadOnly)
                {
                    WriteKeyword(LitReadonly, ref index, output, spaceSuffix: true);
                }

                if (isByRefType)
                {
                    WriteKeyword(LitRef, ref index, output, spaceSuffix: true);
                }
            }
            else if (isStatic)
            {
                WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
            }
            else if (isDelegate)
            {
                WriteKeyword(LitDelegate, ref index, output, spaceSuffix: true);

                if (delegateRtnType.IsByRef)
                {
                    WriteKeyword(LitRef, ref index, output, spaceSuffix: true);
                }
                FormatType(delegateReturnType!, output, ref index);
                if (delegateRtnType.PointerDepth > 0)
                {
                    for (int i = 0; i < delegateRtnType.PointerDepth; ++i)
                    {
                        output[index] = '*';
                        ++index;
                    }
                }
                if (delegateRtnType.IsArray)
                {
                    output[index] = '[';
                    output[index + 1] = ']';
                    index += 2;
                }
                output[index] = ' ';
                ++index;
            }
            if (isValueType)
            {
                if (type.IsEnum)
                {
                    WriteKeyword(LitEnum, ref index, output, spaceSuffix: true);
                }
                else
                {
                    WriteKeyword(LitStruct, ref index, output, spaceSuffix: true);
                }
            }
            else if (type.IsInterface)
            {
                WriteKeyword(LitInterface, ref index, output, spaceSuffix: true);
            }
            else
            {
                if (isAbstract)
                {
                    WriteKeyword(LitAbstract, ref index, output, spaceSuffix: true);
                }

                if (!isDelegate)
                {
                    WriteKeyword(LitClass, ref index, output, spaceSuffix: true);
                }
            }
        }

        if (elementKeyword != null)
        {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
            elementKeyword.AsSpan().CopyTo(output[index..]);
#else
            for (int i = 0; i < elementKeyword.Length; ++i)
                output[index + i] = elementKeyword[i];
#endif
            index += elementKeyword.Length;
        }
        else
        {
            FormatType(type, output, ref index);
        }

        if (main.PointerDepth > 0)
        {
            for (int i = 0; i < main.PointerDepth; ++i)
            {
                output[index] = '*';
                ++index;
            }
        }
        if (main.IsArray)
        {
            output[index] = '[';
            output[index + 1] = ']';
            index += 2;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    private unsafe void FormatType(Type type, in TypeMetaInfo metaInfo, string? elementKeyword, Span<char> output, ref int index)
#else
    private unsafe void FormatType(Type type, in TypeMetaInfo metaInfo, string? elementKeyword, char* output, ref int index)
#endif
    {
        if (elementKeyword != null)
        {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
            elementKeyword.AsSpan().CopyTo(output[index..]);
#else
            for (int i = 0; i < elementKeyword.Length; ++i)
                output[index + i] = elementKeyword[i];
#endif
            index += elementKeyword.Length;
        }
        else
        {
            FormatType(type, output, ref index);
        }

        if (metaInfo.PointerDepth > 0)
        {
            for (int i = 0; i < metaInfo.PointerDepth; ++i)
            {
                output[index] = '*';
                ++index;
            }
        }
        if (metaInfo.IsArray)
        {
            output[index] = '[';
            output[index + 1] = ']';
            index += 2;
        }
    }
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    private unsafe void FormatType(Type type, Span<char> output, ref int index)
#else
    private unsafe void FormatType(Type type, char* output, ref int index)
#endif
    {
        string? keyword = GetTypeKeyword(type);
        if (keyword != null)
        {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
            keyword.AsSpan().CopyTo(output[index..]);
#else
            for (int i = 0; i < keyword.Length; ++i)
                output[index + i] = keyword[i];
#endif
            index += keyword.Length;
            return;
        }

        int ct = 0;
        for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
        {
            bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;
            ++ct;
            if (willBreakNext)
                break;
        }

        if (ct == 1)
        {
            string name = UseFullTypeNames ? type.FullName ?? type.Name : type.Name;

            if (type.IsGenericType)
            {
                int graveIndex = name.IndexOf('`');
                if (graveIndex == -1) graveIndex = name.Length;
                for (int i = 0; i < graveIndex; ++i)
                    output[i + index] = name[i];
                index += graveIndex;

                output[index] = '<';
                ++index;

                Type[] types = type.GetGenericArguments();
                for (int i = 0; i < types.Length; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    FormatType(types[i], output, ref index);
                }

                output[index] = '>';
                ++index;
            }
            else
            {
                for (int i = 0; i < name.Length; ++i)
                    output[i + index] = name[i];
                index += name.Length;
            }
        }
        else
        {
            int* nestedSizes = stackalloc int[ct];
            int nestedInd = -1;
            for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
            {
                bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;

                nestedSizes[++nestedInd] = GetNestedInvariantTypeNameLength(nestingType, willBreakNext);
                if (willBreakNext)
                    break;
            }

            nestedInd = 0;
            for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
            {
                int pos = index;
                for (int i = ct - 1; i > nestedInd; --i)
                    pos += nestedSizes[i];
                ++nestedInd;

                bool willBreakNext = nestingType.IsGenericParameter || nestingType.DeclaringType == null;
                if (!willBreakNext)
                {
                    pos += ct - nestedInd;
                    output[pos - 1] = '.';
                }

                string name = willBreakNext && UseFullTypeNames ? nestingType.FullName ?? nestingType.Name : nestingType.Name;

                if (nestingType.IsGenericType)
                {
                    int graveIndex = name.IndexOf('`');
                    if (graveIndex == -1) graveIndex = name.Length;
                    for (int i = 0; i < graveIndex; ++i)
                        output[i + pos] = name[i];
                    pos += graveIndex;

                    output[pos] = '<';
                    ++pos;

                    Type[] types = nestingType.GetGenericArguments();
                    for (int i = 0; i < types.Length; ++i)
                    {
                        if (i != 0)
                        {
                            output[pos] = ',';
                            output[pos + 1] = ' ';
                            pos += 2;
                        }

                        FormatType(types[i], output, ref pos);
                    }

                    output[pos] = '>';
                    ++pos;
                }
                else
                {
                    for (int i = 0; i < name.Length; ++i)
                        output[i + pos] = name[i];
                }

                if (willBreakNext)
                    break;
            }

            for (int i = ct - 1; i >= 0; --i)
                index += nestedSizes[i];

            index += ct - 1;
        }
    }
    /// <summary>
    /// Calculate the length of a parameter.
    /// </summary>
    protected virtual int GetParmeterLength(ParameterInfo parameter, ref TypeMetaInfo paramMetaInfo, out string? parameterElementKeyword, bool isExtensionThisParameter = false)
    {
        Type type = parameter.ParameterType;
        paramMetaInfo.IsParams = type.IsArray && parameter.IsDefinedSafe<ParamArrayAttribute>();
        string? name = parameter.Name;
        int length = 0;

        if (isExtensionThisParameter)
            length += 5;

        if (!string.IsNullOrEmpty(name))
            length += name.Length + 1;

        length += GetNonDeclaritiveTypeNameLength(ref type, ref paramMetaInfo, out parameterElementKeyword);
        return length;
    }

    /// <summary>
    /// Write parameter to buffer.
    /// </summary>
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    protected virtual void WriteParameter(ParameterInfo parameter, Span<char> output, ref int index, in TypeMetaInfo paramMetaInfo, string? parameterElementKeyword, bool isExtensionThisParameter = false)
#else
    protected virtual unsafe void WriteParameter(ParameterInfo parameter, char* output, ref int index, in TypeMetaInfo paramMetaInfo, string? parameterElementKeyword, bool isExtensionThisParameter = false)
#endif
    {
        Type type = parameter.ParameterType;
        string? name = parameter.Name;
        if (isExtensionThisParameter)
        {
            WriteKeyword(LitThis, ref index, output, spaceSuffix: true);
        }
        if (paramMetaInfo.IsParams)
        {
            WriteKeyword(LitParams, ref index, output, spaceSuffix: true);
        }

        FormatType(type, in paramMetaInfo, parameterElementKeyword, output, ref index);

        if (string.IsNullOrEmpty(name))
            return;

        output[index] = ' ';
        ++index;
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        name.AsSpan().CopyTo(output[index..]);
#else
        for (int i = 0; i < name.Length; ++i)
        {
            output[index + i] = name[i];
        }
#endif
        index += name.Length;
    }

    /// <summary>
    /// Writes a property or event method with the given <paramref name="keyword"/>.
    /// </summary>
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    protected virtual void WriteAccessorIfExists(ReadOnlySpan<char> keyword, MemberVisibility defVis, MethodInfo? method, ref int index, Span<char> output)
#else
    protected virtual unsafe void WriteAccessorIfExists(string keyword, MemberVisibility defVis, MethodInfo? method, ref int index, char* output)
#endif
    {
        if (method == null)
            return;

        MemberVisibility vis = method.GetVisibility();
        if (vis != defVis)
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }

        for (int i = 0; i < keyword.Length; ++i)
            output[i + index] = keyword[i];
        index += keyword.Length;
        output[index] = ';';
        output[index + 1] = ' ';
        index += 2;
    }

    /// <summary>
    /// Represents info about a type for writing.
    /// </summary>
    protected struct TypeMetaInfo
    {
        /// <summary>
        /// Number of pointers on the type.
        /// </summary>
        /// <remarks>May refer to the number of pointers on the array or ref type's element type.</remarks>
        public int PointerDepth;

        /// <summary>
        /// If this type is an array.
        /// </summary>
        public bool IsArray;

        /// <summary>
        /// If this type is passed by-ref.
        /// </summary>
        public bool IsByRef;

        /// <summary>
        /// If this type is a params array.
        /// </summary>
        public bool IsParams;

        /// <summary>
        /// If this type has an element type.
        /// </summary>
        public bool HasElementType => PointerDepth > 0 || IsArray || IsByRef;

        /// <summary>
        /// Length of extra characters.
        /// </summary>
        public int LengthToAdd => (IsArray ? 1 : 0) * 2 + (IsByRef ? 1 : 0) * 4 + (IsParams ? 1 : 0) * 7 + PointerDepth;

        /// <summary>
        /// Initialize the values with the given type.
        /// </summary>
        public void Init(ref Type type, out string? elementKeyword, DefaultOpCodeFormatter formatter)
        {
            Type? elementType = type.GetElementType();
            PointerDepth = type.IsPointer ? 1 : 0;
            if (PointerDepth == 1)
            {
                while (elementType!.IsPointer)
                {
                    elementType = elementType.GetElementType()!;
                    ++PointerDepth;
                }
            }

            IsArray = PointerDepth == 0 && type.IsArray;
            IsByRef = type.IsByRef && elementType is not null;
            if ((IsByRef || IsArray) && elementType!.IsPointer)
            {
                while (elementType.IsPointer)
                {
                    elementType = elementType.GetElementType()!;
                    ++PointerDepth;
                }
            }

            elementKeyword = null;
            if (PointerDepth <= 0 && !IsArray && !IsByRef)
                return;

            type = elementType!;
            elementKeyword = formatter.GetTypeKeyword(type);
        }
    }
}
