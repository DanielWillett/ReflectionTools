using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Formatting;

/// <inheritdoc cref="IOpCodeFormatter"/>
public class DefaultOpCodeFormatter : IOpCodeFormatter
{
    private const string LitPrivate = "private";
    private const string LitProtected = "protected";
    private const string LitPublic = "public";
    private const string LitInternal = "internal";
    private const string LitPrivateProtected = "private protected";
    private const string LitProtectedInternal = "protected internal";

    /// <summary>
    /// Should formatted members and types use their full (namespace-declared) names? Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseFullTypeNames { get; set; }

    /// <summary>
    /// Should formatted keywords for types instead of CLR type names, ex. <see langword="int"/> instead of <see langword="Int32"/>.
    /// </summary>
    public bool UseTypeKeywords { get; set; }


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
            throw new ArgumentOutOfRangeException(nameof(output));

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

                int fieldSize = field.Name.Length;
                if (field.DeclaringType != null)
                    fieldSize += field.DeclaringType.Name.Length + 1;
                return fmtLen + 1 + fieldSize;

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
                    fmtLen += InternalUtil.CountDigits((uint)jumps[i].GetLabelId());

                return fmtLen;

            case OperandType.InlineTok:
                switch (operand)
                {
                    case Type typeToken:
                        return GetFormatLength(typeToken);
                    case MethodBase methodToken:
                        return GetFormatLength(methodToken);
                    case FieldInfo fieldToken:
                        return GetFormatLength(fieldToken);
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
    public virtual int Format(OpCode opCode, object? operand, Span<char> output, OpCodeFormattingContext usageContext) => throw new NotImplementedException();

    /// <inheritdoc/>
    public virtual int GetFormatLength(Type type, bool includeDefinitionKeywords = false, bool isOutType = false)
    {
        string? s = GetTypeKeyword(type);
        if (s != null)
            return s.Length;

        if (type.IsPointer)
        {
            return 1 + GetFormatLength(type.GetElementType()!);
        }
        if (type.IsArray)
        {
            return 2 + GetFormatLength(type.GetElementType()!);
        }
        if (type.IsByRef && type.GetElementType() is { } elemType)
        {
            return 4 + GetFormatLength(elemType);
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

            bool willBreakNext = nestingType.IsGenericTypeParameter || nestingType.IsGenericMethodParameter || nestingType.DeclaringType == null;

            ReadOnlySpan<char> name = willBreakNext && UseFullTypeNames ? nestingType.FullName ?? nestingType.Name : nestingType.Name;

            if (nestingType.IsGenericType)
            {
                int graveIndex = name.IndexOf('`');
                if (graveIndex != -1)
                    length += graveIndex;
                else length += name.Length;
                Type[] types = nestingType.GetGenericArguments();
                length += 2 + (types.Length - 1) * 2;

                for (int i = 0; i < types.Length; ++i)
                    length += GetFormatLength(types[i]);
            }
            else length += name.Length;

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
            int c = Format(type.GetElementType()!, output);
            output[c] = '*';
            return c + 1;
        }

        if (type.IsArray)
        {
            int c = Format(type.GetElementType()!, output);
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
            int c = Format(elemType, output[4..]);
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
                    output[index]     = 'r';
                    output[index + 1] = 'e';
                    output[index + 2] = 'a';
                    output[index + 3] = 'd';
                    output[index + 4] = 'o';
                    output[index + 5] = 'n';
                    output[index + 6] = 'l';
                    output[index + 7] = 'y';
                    output[index + 8] = ' ';
                    index += 9;
                }

                if (type.IsByRefLike)
                {
                    output[index]     = 'r';
                    output[index + 1] = 'e';
                    output[index + 2] = 'f';
                    output[index + 3] = ' ';
                    index += 4;
                }
            }
            else if (type.GetIsStatic())
            {
                output[index]     = 's';
                output[index + 1] = 't';
                output[index + 2] = 'a';
                output[index + 3] = 't';
                output[index + 4] = 'i';
                output[index + 5] = 'c';
                output[index + 6] = ' ';
                index += 7;
            }
            else if (isDelegate)
            {
                output[index]     = 'd';
                output[index + 1] = 'e';
                output[index + 2] = 'l';
                output[index + 3] = 'e';
                output[index + 4] = 'g';
                output[index + 5] = 'a';
                output[index + 6] = 't';
                output[index + 7] = 'e';
                output[index + 8] = ' ';
                index += 9;

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
                    output[index]     = 'e';
                    output[index + 1] = 'n';
                    output[index + 2] = 'u';
                    output[index + 3] = 'm';
                    output[index + 4] = ' ';
                    index += 5;
                }
                else
                {
                    output[index]     = 's';
                    output[index + 1] = 't';
                    output[index + 2] = 'r';
                    output[index + 3] = 'u';
                    output[index + 4] = 'c';
                    output[index + 5] = 't';
                    output[index + 6] = ' ';
                    index += 7;
                }
            }
            else if (type.IsInterface)
            {
                output[index]     = 'i';
                output[index + 1] = 'n';
                output[index + 2] = 't';
                output[index + 3] = 'e';
                output[index + 4] = 'r';
                output[index + 5] = 'f';
                output[index + 6] = 'a';
                output[index + 7] = 'c';
                output[index + 8] = 'e';
                output[index + 9] = ' ';
                index += 10;
            }
            else
            {
                if (type is { IsAbstract: true, IsSealed: false })
                {
                    output[index]     = 'a';
                    output[index + 1] = 'b';
                    output[index + 2] = 's';
                    output[index + 3] = 't';
                    output[index + 4] = 'r';
                    output[index + 5] = 'a';
                    output[index + 6] = 'c';
                    output[index + 7] = 't';
                    output[index + 8] = ' ';
                    index += 9;
                }

                if (!isDelegate)
                {
                    output[index] = 'c';
                    output[index + 1] = 'l';
                    output[index + 2] = 'a';
                    output[index + 3] = 's';
                    output[index + 4] = 's';
                    output[index + 5] = ' ';
                    index += 6;
                }
            }
        }
        
        // todo flip this somehow
        for (Type? nestingType = type; nestingType != null; nestingType = nestingType.DeclaringType)
        {
            if (!ReferenceEquals(nestingType, type))
            {
                output[index] = '.';
                ++index;
            }

            bool willBreakNext = nestingType.IsGenericTypeParameter || nestingType.IsGenericMethodParameter || nestingType.DeclaringType == null;

            ReadOnlySpan<char> name = willBreakNext && UseFullTypeNames ? nestingType.FullName ?? nestingType.Name : nestingType.Name;

            if (nestingType.IsGenericType)
            {
                int graveIndex = name.IndexOf('`');
                if (graveIndex != -1)
                    name = name[..graveIndex];
                name.CopyTo(output[index..]);
                index += name.Length;

                output[index] = '<';
                ++index;

                Type[] types = nestingType.GetGenericArguments();
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

            if (willBreakNext)
                break;
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
            output[index]     = 'c';
            output[index + 1] = 'o';
            output[index + 2] = 'n';
            output[index + 3] = 's';
            output[index + 4] = 't';
            output[index + 5] = ' ';
            index += 6;
        }
        else if (field.IsStatic)
        {
            output[index]     = 's';
            output[index + 1] = 't';
            output[index + 2] = 'a';
            output[index + 3] = 't';
            output[index + 4] = 'i';
            output[index + 5] = 'c';
            output[index + 6] = ' ';
            index += 7;
        }

        if (includeDefinitionKeywords && field is { IsLiteral: false, IsInitOnly: true })
        {
            output[index]     = 'r';
            output[index + 1] = 'e';
            output[index + 2] = 'a';
            output[index + 3] = 'd';
            output[index + 4] = 'o';
            output[index + 5] = 'n';
            output[index + 6] = 'l';
            output[index + 7] = 'y';
            output[index + 8] = ' ';
            index += 9;
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
    public virtual int GetFormatLength(PropertyInfo property, bool includeAccessors = false, bool includeDefinitionKeywords = false)
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

        len += 4; // { }

        MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

        CountAccessorIfExists(3, defVis, getMethod, ref len);

        CountAccessorIfExists(3, defVis, setMethod, ref len);

        return len;
    }

    /// <inheritdoc/>
    public virtual int Format(PropertyInfo property, Span<char> output, bool includeAccessors = false, bool includeDefinitionKeywords = false)
    {
        ParameterInfo[] indexParameters = property.GetIndexParameters();

        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetSetMethod(true);

        MemberVisibility vis = Accessor.GetHighestVisibility(getMethod, setMethod);

        int index = 0;
        if (getMethod != null && getMethod.IsStatic || setMethod != null && setMethod.IsStatic)
        {
            output[0] = 's';
            output[1] = 't';
            output[2] = 'a';
            output[3] = 't';
            output[4] = 'i';
            output[5] = 'c';
            output[6] = ' ';
            index += 7;
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
            output[index] = 't';
            output[index + 1] = 'h';
            output[index + 2] = 'i';
            output[index + 3] = 's';
            index += 4;
        }

        output[index] = ' ';
        output[index + 1] = '{';
        index += 2;

        MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

        WriteAccessorIfExists("get", defVis, getMethod, ref index, output);

        WriteAccessorIfExists("set", defVis, getMethod, ref index, output);

        output[index] = ' ';
        output[index + 1] = '}';

        return index + 2;
    }

    /// <inheritdoc/>
    public virtual string Format(PropertyInfo property, bool includeAccessors = false, bool includeDefinitionKeywords = false)
    {
        ParameterInfo[] indexParameters = property.GetIndexParameters();

        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetSetMethod(true);

        MemberVisibility vis = Accessor.GetHighestVisibility(getMethod, setMethod);

        Type propertyType = property.PropertyType;
        Type? declaringType = property.DeclaringType;

        bool isStatic = getMethod != null && getMethod.IsStatic || setMethod != null && setMethod.IsStatic;
        int len = GetFormatLength(propertyType, false) + 1;
        if (indexParameters.Length == 0)
            len += property.Name.Length;
        else
            len += 4; // this

        if (declaringType != null)
            len += GetFormatLength(declaringType) + 1;

        if (isStatic)
            len += 7;

        if (indexParameters.Length > 0)
        {
            len += 2 + (indexParameters.Length - 1) * 2;
            for (int i = 0; i < indexParameters.Length; ++i)
                len += GetFormatLength(indexParameters[i]);
        }

        len += 4; // { }

        MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

        CountAccessorIfExists(3, defVis, getMethod, ref len);

        CountAccessorIfExists(3, defVis, setMethod, ref len);

        Span<char> output = stackalloc char[len];

        int index = 0;
        if (isStatic)
        {
            output[0] = 's';
            output[1] = 't';
            output[2] = 'a';
            output[3] = 't';
            output[4] = 'i';
            output[5] = 'c';
            output[6] = ' ';
            index += 7;
        }

        index += Format(propertyType, output[index..]);
        output[index] = ' ';
        ++index;

        if (declaringType != null)
        {
            index += Format(declaringType, output[index..]);
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
            output[index] = 't';
            output[index + 1] = 'h';
            output[index + 2] = 'i';
            output[index + 3] = 's';
            index += 4;
        }

        output[index] = ' ';
        output[index + 1] = '{';
        index += 2;

        WriteAccessorIfExists("get", defVis, getMethod, ref index, output);

        WriteAccessorIfExists("set", defVis, getMethod, ref index, output);

        output[index] = ' ';
        output[index + 1] = '}';

        index += 2;
        return new string(output[..index]);
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(EventInfo @event, bool includeAccessors = false, bool includeEventKeyword = true, bool includeDefinitionKeywords = false)
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

        len += 4; // { }

        MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

        CountAccessorIfExists(3, defVis, addMethod, ref len);

        CountAccessorIfExists(6, defVis, removeMethod, ref len);

        CountAccessorIfExists(5, defVis, raiseMethod, ref len);

        return len;
    }

    /// <inheritdoc/>
    public virtual int Format(EventInfo @event, Span<char> output, bool includeAccessors = false, bool includeEventKeyword = true, bool includeDefinitionKeywords = false)
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
            output[0] = 's';
            output[1] = 't';
            output[2] = 'a';
            output[3] = 't';
            output[4] = 'i';
            output[5] = 'c';
            output[6] = ' ';
            index += 7;
        }

        output[index] = 'e';
        output[index + 1] = 'v';
        output[index + 2] = 'e';
        output[index + 3] = 'n';
        output[index + 4] = 't';
        output[index + 5] = ' ';
        index += 6;

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

        output[index] = ' ';
        output[index + 1] = '{';
        index += 2;

        MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

        WriteAccessorIfExists("add", defVis, addMethod, ref index, output);

        WriteAccessorIfExists("remove", defVis, removeMethod, ref index, output);

        WriteAccessorIfExists("raise", defVis, raiseMethod, ref index, output);

        output[index] = ' ';
        output[index + 1] = '}';

        return index + 2;
    }

    /// <inheritdoc/>
    public virtual string Format(EventInfo @event, bool includeAccessors = false, bool includeEventKeyword = true, bool includeDefinitionKeywords = false)
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

        len += 4; // { }

        MemberVisibility defVis = includeDefinitionKeywords ? vis : MemberVisibility.Public;

        CountAccessorIfExists(3, defVis, addMethod, ref len);

        CountAccessorIfExists(6, defVis, removeMethod, ref len);

        CountAccessorIfExists(5, defVis, raiseMethod, ref len);

        Span<char> output = stackalloc char[len];

        int index = 0;
        if (addMethod != null && addMethod.IsStatic || removeMethod != null && removeMethod.IsStatic || raiseMethod != null && raiseMethod.IsStatic)
        {
            output[0] = 's';
            output[1] = 't';
            output[2] = 'a';
            output[3] = 't';
            output[4] = 'i';
            output[5] = 'c';
            output[6] = ' ';
            index += 7;
        }

        output[index] = 'e';
        output[index + 1] = 'v';
        output[index + 2] = 'e';
        output[index + 3] = 'n';
        output[index + 4] = 't';
        output[index + 5] = ' ';
        index += 6;

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

        output[index] = ' ';
        output[index + 1] = '{';
        index += 2;

        WriteAccessorIfExists("add", defVis, addMethod, ref index, output);

        WriteAccessorIfExists("remove", defVis, removeMethod, ref index, output);

        WriteAccessorIfExists("raise", defVis, raiseMethod, ref index, output);

        output[index] = ' ';
        output[index + 1] = '}';

        index += 2;
        return new string(output[..index]);
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(ParameterInfo parameter, bool isExtensionThisParameter = false)
    {
        Type type = parameter.ParameterType;
        bool isParamsArray = type.IsArray && parameter.IsDefinedSafe<ParamArrayAttribute>();
        string? name = parameter.Name;
        int length = 0;

        if (isExtensionThisParameter)
            length += 5;

        if (!string.IsNullOrEmpty(name))
            length += name.Length + 1;

        if (isParamsArray)
            length += 7;

        length += GetFormatLength(type, isOutType: parameter.IsOut);
        return length;
    }

    /// <inheritdoc/>
    public virtual int Format(ParameterInfo parameter, Span<char> output, bool isExtensionThisParameter = false)
    {
        Type type = parameter.ParameterType;
        bool isParamsArray = type.IsArray && parameter.IsDefinedSafe<ParamArrayAttribute>();
        string? name = parameter.Name;
        int index = 0;

        if (isExtensionThisParameter)
        {
            output[0] = 't';
            output[1] = 'h';
            output[2] = 'i';
            output[3] = 's';
            output[4] = ' ';
            index += 5;
        }

        if (isParamsArray)
        {
            output[index] = 'p';
            output[index + 1] = 'a';
            output[index + 2] = 'r';
            output[index + 3] = 'a';
            output[index + 4] = 'm';
            output[index + 5] = 's';
            output[index + 6] = ' ';
            index += 7;
        }

        index += Format(type, output[index..], isOutType: parameter.IsOut);

        if (!string.IsNullOrEmpty(name))
        {
            output[index] = ' ';
            ReadOnlySpan<char> nameSpan = name;
            nameSpan.CopyTo(output[(index + 1)..]);
            index += nameSpan.Length + 1;
        }

        return index;
    }

    /// <inheritdoc/>
    public virtual string Format(ParameterInfo parameter, bool isExtensionThisParameter)
    {
        Type type = parameter.ParameterType;
        bool isParamsArray = type.IsArray && parameter.IsDefinedSafe<ParamArrayAttribute>();
        string? name = parameter.Name;
        int index = 0, length = 0;
        bool isOut = parameter.IsOut;

        if (isExtensionThisParameter)
            length += 5;

        if (!string.IsNullOrEmpty(name))
            length += name.Length + 1;

        if (isParamsArray)
            length += 7;

        length += GetFormatLength(type, isOut);
        Span<char> output = stackalloc char[length];

        if (isExtensionThisParameter)
        {
            output[0] = 't';
            output[1] = 'h';
            output[2] = 'i';
            output[3] = 's';
            output[4] = ' ';
            index += 5;
        }

        if (isParamsArray)
        {
            output[index] = 'p';
            output[index + 1] = 'a';
            output[index + 2] = 'r';
            output[index + 3] = 'a';
            output[index + 4] = 'm';
            output[index + 5] = 's';
            output[index + 6] = ' ';
            index += 7;
        }

        index += Format(type, output[index..], isOutType: isOut);

        if (string.IsNullOrEmpty(name))
            return new string(output[..index]);

        output[index] = ' ';
        ReadOnlySpan<char> nameSpan = name;
        nameSpan.CopyTo(output[(index + 1)..]);
        index += nameSpan.Length;

        return new string(output[..index]);
    }

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
    /// Writes a property or event method with the given <paramref name="keyword"/>.
    /// </summary>
    protected virtual void WriteAccessorIfExists(ReadOnlySpan<char> keyword, MemberVisibility defVis, MethodInfo? method, ref int index, Span<char> output)
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

        keyword.CopyTo(output[index..]);
        index += keyword.Length;
        output[index] = ';';
        output[index + 1] = ' ';
        index += 2;
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
#else

    /// <inheritdoc/>
    public virtual string Format(OpCode opCode) => opCode.Name;
    
    /// <inheritdoc/>
    public virtual string Format(Label label) => ((uint)label.GetLabelId()).ToString("N0", CultureInfo.InvariantCulture);
    
    /// <inheritdoc/>
    public virtual string Format(OpCode opCode, object? operand, OpCodeFormattingContext usageContext) => throw new NotImplementedException();

    public string Format(Type type, bool includeDefinitionKeywords = false, bool isOutType = false) => throw new NotImplementedException();

    public string Format(MethodBase method, bool includeDefinitionKeywords = false) => throw new NotImplementedException();

    public string Format(FieldInfo field, bool includeDefinitionKeywords = false) => throw new NotImplementedException();

    public string Format(PropertyInfo property, bool includeAccessors = false, bool includeDefinitionKeywords = false) => throw new NotImplementedException();

    public string Format(EventInfo @event, bool includeAccessors = false, bool includeEventKeyword = true,
        bool includeDefinitionKeywords = false) =>
        throw new NotImplementedException();

    public string Format(ParameterInfo parameter, bool isExtensionThisParameter = false) => throw new NotImplementedException();
#endif

    /// <summary>
    /// Gets the language keyword for the type instead of the CLR type name, or <see langword="null"/> if the type doesn't have a keyword.
    /// </summary>
    protected virtual string? GetTypeKeyword(Type type)
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
                return "string";
        }

        return null;
    }
}
