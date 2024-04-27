using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DanielWillett.ReflectionTools.Emit;
#if NETFRAMEWORK || (NETSTANDARD && !NETSTANDARD2_1_OR_GREATER)
using System.Text;
#endif

namespace DanielWillett.ReflectionTools.Formatting;
#pragma warning disable CA2014
/// <summary>
/// Default plain-text low-alloc formatter for reflection members and op-codes.
/// </summary>
public class DefaultOpCodeFormatter : IOpCodeFormatter
{
    /// <summary>Keyword: <see langword="private"/>.</summary>
    protected const string LitPrivate = "private";
    /// <summary>Keyword: <see langword="protected"/>.</summary>
    protected const string LitProtected = "protected";
    /// <summary>Keyword: <see langword="public"/>.</summary>
    protected const string LitPublic = "public";
    /// <summary>Keyword: <see langword="internal"/>.</summary>
    protected const string LitInternal = "internal";
    /// <summary>Keyword: <see langword="private protected"/>.</summary>
    protected const string LitPrivateProtected = "private protected";
    /// <summary>Keyword: <see langword="protected internal"/>.</summary>
    protected const string LitProtectedInternal = "protected internal";
    /// <summary>Keyword: <see langword="static"/>.</summary>
    protected const string LitStatic = "static";
    /// <summary>Keyword: <see langword="event"/>.</summary>
    protected const string LitEvent = "event";
    /// <summary>Keyword: <see langword="readonly"/>.</summary>
    protected const string LitReadonly = "readonly";
    /// <summary>Keyword: <see langword="const"/>.</summary>
    protected const string LitConst = "const";
    /// <summary>Keyword: <see langword="abstract"/>.</summary>
    protected const string LitAbstract = "abstract";
    /// <summary>Keyword: <see langword="ref"/>.</summary>
    protected const string LitRef = "ref";
    /// <summary>Keyword: <see langword="scoped"/>.</summary>
    protected const string LitScoped = "scoped";
    /// <summary>Keyword: <see langword="out"/>.</summary>
    protected const string LitOut = "out";
    /// <summary>Keyword: <see langword="enum"/>.</summary>
    protected const string LitEnum = "enum";
    /// <summary>Keyword: <see langword="class"/>.</summary>
    protected const string LitClass = "class";
    /// <summary>Keyword: <see langword="struct"/>.</summary>
    protected const string LitStruct = "struct";
    /// <summary>Keyword: <see langword="interface"/>.</summary>
    protected const string LitInterface = "interface";
    /// <summary>Keyword: <see langword="delegate"/>.</summary>
    protected const string LitDelegate = "delegate";
    /// <summary>Keyword: <see langword="this"/>.</summary>
    protected const string LitThis = "this";
    /// <summary>Keyword: <see langword="params"/>.</summary>
    protected const string LitParams = "params";
    /// <summary>Keyword: <see langword="in"/>.</summary>
    protected const string LitIn = "in";
    /// <summary>Keyword: <see langword="get"/>.</summary>
    protected const string LitGet = "get";
    /// <summary>Keyword: <see langword="set"/>.</summary>
    protected const string LitSet = "set";
    /// <summary>Keyword: <see langword="add"/>.</summary>
    protected const string LitAdd = "add";
    /// <summary>Keyword: <see langword="remove"/>.</summary>
    protected const string LitRemove = "remove";
    /// <summary>Keyword: <see langword="raise"/>.</summary>
    protected const string LitRaise = "raise";

    private IAccessor _accessor;

    internal IAccessor Accessor
    {
        get => _accessor;
        set => _accessor = value;
    }

    /// <summary>
    /// Create a formatter.
    /// </summary>
#if NET461_OR_GREATER || !NETFRAMEWORK
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
#endif
    public DefaultOpCodeFormatter()
    {
        _accessor = ReflectionTools.Accessor.Active;
    }
    internal DefaultOpCodeFormatter(IAccessor? accessor)
    {
        _accessor = accessor ?? ReflectionTools.Accessor.Active;
    }

    /// <summary>
    /// Should formatted members and types use their full (namespace-declared) names? Defaults to <see langword="false"/>.
    /// </summary>
    public bool UseFullTypeNames { get; set; }

    /// <summary>
    /// Should formatted keywords for types instead of CLR type names, ex. <see langword="int"/> instead of <see langword="Int32"/>. Defaults to <see langword="true"/>.
    /// </summary>
    public bool UseTypeKeywords { get; set; } = true;

    /// <inheritdoc />
    public virtual object Clone() => new DefaultOpCodeFormatter(_accessor)
    {
        UseFullTypeNames = UseFullTypeNames,
        UseTypeKeywords = UseTypeKeywords
    };

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
        if (!((uint)label.GetLabelId()).TryFormat(output, out int chrsWritten, "D0", CultureInfo.InvariantCulture))
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

                return fmtLen + 5 + InternalUtil.CountDigits((uint)lbl.GetLabelId());

            case OperandType.InlineField:
                if (operand is not FieldInfo field)
                    break;

                return fmtLen + 1 + GetFormatLength(field);

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

                return fmtLen + 1 + GetFormatLength(method);

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

                if (jumps.Length == 0)
                    fmtLen += 4;
                else if (usageContext == OpCodeFormattingContext.InLine)
                {
                    fmtLen += 3 + (Math.Max(jumps.Length, 1) - 1) * 2 + jumps.Length * 4;
                    for (int i = 0; i < jumps.Length; ++i)
                        fmtLen += InternalUtil.CountDigits((uint)jumps[i].GetLabelId());
                }
                else
                {
                    fmtLen += 2 + Environment.NewLine.Length * (2 + jumps.Length) + jumps.Length * 10;
                    for (int i = 0; i < jumps.Length; ++i)
                        fmtLen += InternalUtil.CountDigits((uint)jumps[i].GetLabelId()) + InternalUtil.CountDigits(i);
                }
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

                return fmtLen + 1 + GetFormatLength(type);

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
        switch (opCode.OperandType)
        {
            case OperandType.ShortInlineBrTarget:
            case OperandType.InlineBrTarget:
                if (operand is not Label lbl)
                    break;

                output[index] = ' ';
                output[index + 1] = 'l';
                output[index + 2] = 'b';
                output[index + 3] = 'l';
                output[index + 4] = '.';
                index += 5;
                uint lblId = (uint)lbl.GetLabelId();
                if (!lblId.TryFormat(output[index..], out int charsWritten, "D0", CultureInfo.InvariantCulture))
                    throw new IndexOutOfRangeException();

                return index + charsWritten;

            case OperandType.InlineField:
                if (operand is not FieldInfo field)
                    break;

                output[index] = ' ';
                ++index;
                index += Format(field, output[index..]);
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
                if (!l64.TryFormat(output[index..], out charsWritten, "D0", CultureInfo.InvariantCulture))
                    throw new IndexOutOfRangeException();

                return index + charsWritten;

            case OperandType.InlineMethod:
                if (operand is not MethodBase method)
                    break;

                output[index] = ' ';
                ++index;
                index += Format(method, output[index..]);
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
                if (!l64.TryFormat(output[index..], out charsWritten, "D0", CultureInfo.InvariantCulture))
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

                if (jumps.Length == 0)
                {
                    output[index] = ' ';
                    output[index + 1] = usageContext == OpCodeFormattingContext.InLine ? '(' : '{';
                    output[index + 2] = ' ';
                    output[index + 3] = usageContext == OpCodeFormattingContext.InLine ? ')' : '}';
                    index += 4;
                }
                else if (usageContext == OpCodeFormattingContext.InLine)
                {
                    output[index] = ' ';
                    output[index + 1] = '(';
                    index += 2;
                    for (int i = 0; i < jumps.Length; ++i)
                    {
                        if (i != 0)
                        {
                            output[index] = ',';
                            output[index + 1] = ' ';
                            index += 2;
                        }

                        output[index] = 'l';
                        output[index + 1] = 'b';
                        output[index + 2] = 'l';
                        output[index + 3] = '.';
                        index += 4;

                        if (!((uint)jumps[i].GetLabelId()).TryFormat(output[index..], out charsWritten, "D0", CultureInfo.InvariantCulture))
                            throw new IndexOutOfRangeException();

                        index += charsWritten;
                    }

                    output[index] = ')';
                    ++index;
                }
                else
                {
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

                        if (!((uint)i).TryFormat(output[index..], out charsWritten, "D0", CultureInfo.InvariantCulture))
                            throw new IndexOutOfRangeException();

                        index += charsWritten;
                        output[index] = ' ';
                        output[index + 1] = '=';
                        output[index + 2] = '>';
                        output[index + 3] = ' ';
                        output[index + 4] = 'l';
                        output[index + 5] = 'b';
                        output[index + 6] = 'l';
                        output[index + 7] = '.';
                        index += 8;

                        lblId = (uint)jumps[i].GetLabelId();
                        if (!lblId.TryFormat(output[index..], out charsWritten, "D0", CultureInfo.InvariantCulture))
                            throw new IndexOutOfRangeException();

                        index += charsWritten;
                    }
                    nl.CopyTo(output[index..]);
                    index += nl.Length;
                    output[index] = '}';
                    ++index;
                }

                return index;

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
                    if (!((uint)lb.LocalIndex).TryFormat(output[index..], out charsWritten, "D0", CultureInfo.InvariantCulture))
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
                if (!l64.TryFormat(output[index..], out charsWritten, "D0", CultureInfo.InvariantCulture))
                    throw new IndexOutOfRangeException();

                return index + charsWritten;
        }

        return index;
    }

    /// <inheritdoc/>
    public virtual unsafe int GetFormatLength(Type type, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        int s = GetTypeKeywordLength(type);
        if (s != -1)
            return s;

        TypeMetaInfo main = default;
        Type originalType = type;
        main.Init(ref type, out string? elementKeyword, this);
        if (main.ElementTypesLength != 0)
        {
            int* elementTypes = stackalloc int[main.ElementTypesLength];
            main.ElementTypes = elementTypes;
            main.SetupDimensionsAndOrdering(originalType);
            main.LoadRefType(refMode, this);
        }

        TypeMetaInfo delegateRtnType = default;

        MemberVisibility vis = includeDefinitionKeywords ? _accessor.GetVisibility(type) : default;
        bool isValueType = type.IsValueType;
        bool isAbstract = !isValueType && includeDefinitionKeywords && type is { IsAbstract: true, IsSealed: false, IsInterface: false };
        bool isDelegate = !isValueType && includeDefinitionKeywords && type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
        bool isReadOnly = isValueType && includeDefinitionKeywords && _accessor.IsReadOnly(type);
        bool isStatic = !isValueType && includeDefinitionKeywords && _accessor.GetIsStatic(type);
        bool isByRefType = includeDefinitionKeywords && type.IsByRefLike;

        int length = main.Length;
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
                    MethodInfo invokeMethod = _accessor.GetInvokeMethod(type);
                    Type delegateReturnType = invokeMethod.ReturnType;
                    Type originalDelegateReturnType = delegateReturnType;
                    delegateRtnType.Init(ref delegateReturnType, out string? delegateKeyword, this);
                    int* delegateReturnTypeElementStack = stackalloc int[delegateRtnType.ElementTypesLength];
                    delegateRtnType.ElementTypes = delegateReturnTypeElementStack;
                    delegateRtnType.SetupDimensionsAndOrdering(originalDelegateReturnType);
                    delegateRtnType.LoadReturnType(invokeMethod, this);

                    ++length;
                    length += delegateRtnType.Length;
                    if (delegateKeyword != null)
                        length += delegateKeyword.Length;
                    else
                    {
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

        return length;
    }

    /// <inheritdoc/>
    public virtual unsafe int Format(Type type, Span<char> output, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        string? s = GetTypeKeyword(type);
        if (s != null)
        {
            s.AsSpan().CopyTo(output);
            return s.Length;
        }

        TypeMetaInfo main = default;
        Type originalType = type;
        main.Init(ref type, out string? elementKeyword, this);
        int* elementTypes = stackalloc int[main.ElementTypesLength];
        main.ElementTypes = elementTypes;
        main.SetupDimensionsAndOrdering(originalType);
        main.LoadRefType(refMode, this);

        TypeMetaInfo delegateRtnType = default;

        MemberVisibility vis = includeDefinitionKeywords ? _accessor.GetVisibility(type) : default;
        bool isValueType = type.IsValueType;
        bool isAbstract = !isValueType && includeDefinitionKeywords && type is { IsAbstract: true, IsSealed: false, IsInterface: false };
        bool isDelegate = !isValueType && includeDefinitionKeywords && type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
        bool isReadOnly = isValueType && includeDefinitionKeywords && _accessor.IsReadOnly(type);
        bool isStatic = !isValueType && includeDefinitionKeywords && _accessor.GetIsStatic(type);
        bool isByRefType = includeDefinitionKeywords && type.IsByRefLike;
        int index = 0;
        WritePreDimensionsAndOrdering(in main, output, ref index);
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
                MethodInfo invokeMethod = _accessor.GetInvokeMethod(type);
                Type delegateReturnType = invokeMethod.ReturnType;
                originalType = delegateReturnType;
                delegateRtnType.Init(ref delegateReturnType, out string? delegateKeyword, this);
                int* intlDelegateElementTypes = stackalloc int[delegateRtnType.ElementTypesLength];
                delegateRtnType.ElementTypes = intlDelegateElementTypes;
                delegateRtnType.SetupDimensionsAndOrdering(originalType);
                delegateRtnType.LoadReturnType(invokeMethod, this);
                WriteKeyword(LitDelegate, ref index, output, spaceSuffix: true);
                FormatType(delegateReturnType, in delegateRtnType, delegateKeyword, output, ref index);
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
            elementKeyword.AsSpan().CopyTo(output[index..]);
            index += elementKeyword.Length;
        }
        else
        {
            FormatType(type, output, ref index);
        }
        WritePostDimensionsAndOrdering(in main, output, ref index);

        return index;
    }

    /// <inheritdoc/>
    public virtual unsafe int GetFormatLength(TypeDefinition type, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        int s = GetKeywordLength(type);
        if (s != -1)
            return s;

        TypeMetaInfo main = default;
        int* elementTypes = stackalloc int[type.ElementTypesIntl?.Count ?? 0];
        main.ElementTypes = elementTypes;
        main.Init(type, out string? elementKeyword, this);
        main.LoadRefType(refMode, this);

        // todo delegate support

        int length = main.Length;
        if (elementKeyword != null)
        {
            length += elementKeyword.Length;
        }
        return length;
    }

    /// <inheritdoc/>
    public virtual unsafe int Format(TypeDefinition type, Span<char> output, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        string? s = GetKeyword(type);
        if (s != null)
        {
            s.AsSpan().CopyTo(output);
            return s.Length;
        }

        TypeMetaInfo main = default;
        int* elementTypes = stackalloc int[type.ElementTypesIntl?.Count ?? 0];
        main.ElementTypes = elementTypes;
        main.Init(type, out string? elementKeyword, this);
        main.FlipArrayGroups();
        main.LoadRefType(refMode, this);

        // todo delegate support

        int index = 0;
        WritePreDimensionsAndOrdering(in main, output, ref index);

        if (elementKeyword != null)
        {
            elementKeyword.AsSpan().CopyTo(output[index..]);
            index += elementKeyword.Length;
        }

        WritePostDimensionsAndOrdering(in main, output, ref index);
        return index;
    }

    /// <inheritdoc/>
    public virtual unsafe int GetFormatLength(MethodBase method, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = _accessor.GetVisibility(method);
        ParameterInfo[] parameters = method.GetParameters();
        Type[] genericTypeParameters = method.IsGenericMethod ? method.GetGenericArguments() : Type.EmptyTypes;
        string methodName = method.Name;
        int len = 0;
        bool isReadOnly = _accessor.IsReadOnly(method);
        bool isCtor = method is ConstructorInfo;
        if (includeDefinitionKeywords && (!isCtor || !method.IsStatic))
            len += GetVisibilityLength(vis) + 1;

        Type? declType = method.DeclaringType;
        if (!isReadOnly && declType is { IsValueType: true } && _accessor.IsReadOnly(declType))
            isReadOnly = true;

        if (method.IsStatic)
            len += 7;

        if (includeDefinitionKeywords && isReadOnly)
            len += 9;

        Type? returnType = method is MethodInfo returnableMethod ? returnableMethod.ReturnType : declType;
        Type? originalReturnType = returnType;

        TypeMetaInfo methodTypeMeta = default;
        TypeMetaInfo declTypeMeta = default;

        if (returnType != null)
        {
            methodTypeMeta.Init(ref returnType, out string? methodTypeKeyword, this);
            int* methodTypeElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
            methodTypeMeta.ElementTypes = methodTypeElementStack;
            methodTypeMeta.SetupDimensionsAndOrdering(originalReturnType!);
            methodTypeMeta.LoadReturnType(method, this);
            len += GetNonDeclaritiveTypeNameLengthNoSetup(returnType, ref methodTypeMeta, methodTypeKeyword) + (!isCtor ? 1 : 0) * (methodName.Length + 1);
        }

        if (declType != null && !isCtor)
        {
            declTypeMeta.Init(ref declType, out string? declTypeKeyword, this);
            len += GetNonDeclaritiveTypeNameLengthNoSetup(declType, ref declTypeMeta, declTypeKeyword) + 1;
        }

        if (genericTypeParameters.Length > 0)
        {
            len += 2 + (genericTypeParameters.Length - 1) * 2;
            if (method.IsGenericMethodDefinition)
            {
                for (int i = 0; i < genericTypeParameters.Length; ++i)
                {
                    len += genericTypeParameters[i].Name.Length;
                }
            }
            else
            {
                for (int i = 0; i < genericTypeParameters.Length; ++i)
                {
                    TypeMetaInfo genParamMetaInfo = default;
                    Type genParameterType = genericTypeParameters[i];
                    Type originalGenParamType = genParameterType;

                    genParamMetaInfo.Init(ref genParameterType, out string? elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* genParamElementStack = stackalloc int[genParamMetaInfo.ElementTypesLength];
                    genParamMetaInfo.ElementTypes = genParamElementStack;
                    len += GetNonDeclaritiveTypeNameLength(genParameterType, originalGenParamType, ref genParamMetaInfo, elementKeyword);
                }
            }
        }

        len += 2 + (Math.Max(parameters.Length, 1) - 1) * 2;
        for (int i = 0; i < parameters.Length; ++i)
        {
            ParameterInfo parameter = parameters[i];
            TypeMetaInfo paramMetaInfo = default;
            Type parameterType = parameter.ParameterType;
            Type originalParamType = parameterType;
            string? name = parameter.Name;

            if (i == 0 && method.IsStatic && declType != null && _accessor.GetIsStatic(declType) && _accessor.IsDefinedSafe<ExtensionAttribute>(method))
                len += 5;

            if (!string.IsNullOrEmpty(name))
                len += name.Length + 1;

            paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
            // ReSharper disable once StackAllocInsideLoop
            int* paramElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
            paramMetaInfo.ElementTypes = paramElementStack;
            paramMetaInfo.SetupDimensionsAndOrdering(originalParamType);
            paramMetaInfo.LoadParameter(parameter, this);
            len += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
        }

        return len;
    }

    /// <inheritdoc/>
    public virtual unsafe int Format(MethodBase method, Span<char> output, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = _accessor.GetVisibility(method);
        ParameterInfo[] parameters = method.GetParameters();
        Type[] genericTypeParameters = method.IsGenericMethod ? method.GetGenericArguments() : Type.EmptyTypes;
        string methodName = method.Name;
        bool isCtor = method is ConstructorInfo;
        int index = 0;
        if (includeDefinitionKeywords && (!isCtor || !method.IsStatic))
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }
        
        bool isReadOnly = _accessor.IsReadOnly(method);
        Type? declType = method.DeclaringType;
        Type? originalDeclType = declType;

        if (!isReadOnly && declType is { IsValueType: true } && _accessor.IsReadOnly(declType))
            isReadOnly = true;

        if (method.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        if (includeDefinitionKeywords && isReadOnly)
        {
            WriteKeyword(LitReadonly, ref index, output, spaceSuffix: true);
        }
        Type? returnType = method is MethodInfo returnableMethod ? returnableMethod.ReturnType : declType;
        Type? originalReturnType = returnType;

        TypeMetaInfo methodTypeMeta = default;
        TypeMetaInfo declTypeMeta = default;

        if (returnType != null)
        {
            methodTypeMeta.Init(ref returnType, out string? returnTypeKeyword, this);
            int* methodTypeElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
            methodTypeMeta.ElementTypes = methodTypeElementStack;
            methodTypeMeta.SetupDimensionsAndOrdering(originalReturnType!);
            methodTypeMeta.LoadReturnType(method, this);
            FormatType(returnType, in methodTypeMeta, returnTypeKeyword, output, ref index);
            if (!isCtor)
            {
                output[index] = ' ';
                ++index;
            }
        }
        if (declType != null && !isCtor)
        {
            declTypeMeta.Init(ref declType, out string? declTypeKeyword, this);
            int* declTypeElementStack = stackalloc int[declTypeMeta.ElementTypesLength];
            declTypeMeta.ElementTypes = declTypeElementStack;
            declTypeMeta.SetupDimensionsAndOrdering(originalDeclType!);
            FormatType(declType, in declTypeMeta, declTypeKeyword, output, ref index);
            output[index] = '.';
            ++index;
        }

        if (!isCtor)
        {
            methodName.AsSpan().CopyTo(output[index..]);
            index += methodName.Length;
        }

        if (genericTypeParameters.Length > 0)
        {
            output[index] = '<';
            ++index;
            if (method.IsGenericMethodDefinition)
            {
                for (int i = 0; i < genericTypeParameters.Length; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    ReadOnlySpan<char> name = genericTypeParameters[i].Name;
                    name.CopyTo(output[index..]);
                    index += name.Length;
                }
            }
            else
            {
                for (int i = 0; i < genericTypeParameters.Length; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    TypeMetaInfo genParamMetaInfo = default;
                    Type genParameterType = genericTypeParameters[i];
                    Type originalGenParamType = genParameterType;

                    genParamMetaInfo.Init(ref genParameterType, out string? elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* genParamElementStack = stackalloc int[genParamMetaInfo.ElementTypesLength];
                    genParamMetaInfo.ElementTypes = genParamElementStack;
                    genParamMetaInfo.SetupDimensionsAndOrdering(originalGenParamType);
                    FormatType(genParameterType, in genParamMetaInfo, elementKeyword, output, ref index);
                }
            }
            output[index] = '>';
            ++index;
        }

        output[index] = '(';
        ++index;
        for (int i = 0; i < parameters.Length; ++i)
        {
            if (i != 0)
            {
                output[index] = ',';
                output[index + 1] = ' ';
                index += 2;
            }

            ParameterInfo parameter = parameters[i];
            TypeMetaInfo paramMeta = default;
            Type parameterType = parameter.ParameterType;
            Type originalParameterType = parameterType;
            paramMeta.Init(ref parameterType, out string? elementKeyword, this);
            // ReSharper disable once StackAllocInsideLoop
            int* parameterElementStack = stackalloc int[paramMeta.ElementTypesLength];
            paramMeta.ElementTypes = parameterElementStack;
            paramMeta.SetupDimensionsAndOrdering(originalParameterType);
            paramMeta.LoadParameter(parameter, this);
            WriteParameter(parameters[i], parameterType, elementKeyword, output, ref index, in paramMeta,
                isExtensionThisParameter: i == 0 && method.IsStatic && declType != null && _accessor.GetIsStatic(declType) && _accessor.IsDefinedSafe<ExtensionAttribute>(method));
        }
        output[index] = ')';
        return index + 1;
    }
    
    /// <inheritdoc/>
    public virtual unsafe int GetFormatLength(MethodDefinition method)
    {
        string? methodName = method.Name;
        List<MethodParameterDefinition>? parameters = method.ParametersIntl;
        bool isDef = method.GenDefsIntl != null && (method.GenValsIntl == null || method.GenDefsIntl.Count > method.GenValsIntl.Count) && method.GenDefsIntl.Count > 0;
        bool isGenVal = !isDef && method.GenValsIntl is { Count: > 0 };
        int len = 0;
        bool isCtor = method.IsConstructor;

        Type? declType = method.DeclaringType;

        if (method.IsStatic)
            len += 7;

        Type? returnType = method.ReturnType;
        Type? originalReturnType = returnType;

        TypeMetaInfo methodTypeMeta = default;
        TypeMetaInfo declTypeMeta = default;

        if (returnType != null)
        {
            methodTypeMeta.Init(ref returnType, out string? methodTypeKeyword, this);
            int* methodTypeElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
            methodTypeMeta.ElementTypes = methodTypeElementStack;
            methodTypeMeta.SetupDimensionsAndOrdering(originalReturnType!);
            methodTypeMeta.LoadReturnType(method, this);
            len += GetNonDeclaritiveTypeNameLengthNoSetup(returnType, ref methodTypeMeta, methodTypeKeyword);
            if (!isCtor && (methodName != null || declType != null))
            {
                ++len;
            }
        }
        else if (isDef && method.ReturnTypeGenericIndex >= 0 && method.ReturnTypeGenericIndex < method.GenDefsIntl!.Count)
        {
            methodTypeMeta.InitReturnTypeMeta(method, out string? methodTypeKeyword, this);
            if (methodTypeKeyword != null)
            {
                // ReSharper disable once StackAllocInsideLoop
                int* parameterElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
                methodTypeMeta.ElementTypes = parameterElementStack;
                methodTypeMeta.LoadReturnType(method, this);
                len += methodTypeMeta.Length;
                if (!isCtor && (methodName != null || declType != null))
                {
                    ++len;
                }
            }
        }

        if (declType != null && !isCtor)
        {
            declTypeMeta.Init(ref declType, out string? declTypeKeyword, this);
            len += GetNonDeclaritiveTypeNameLengthNoSetup(declType, ref declTypeMeta, declTypeKeyword);
            if (methodName != null)
            {
                ++len;
            }
        }

        if (!isCtor && methodName != null)
            len += methodName.Length;

        if (isDef || isGenVal)
        {
            if (isDef)
            {
                len += 2 + (method.GenDefsIntl!.Count - 1) * 2;
                for (int i = 0; i < method.GenDefsIntl!.Count; ++i)
                {
                    string? name = method.GenDefsIntl![i];
                    if (name != null)
                        len += name.Length;
                }
            }
            else
            {
                len += 2 + (method.GenValsIntl!.Count - 1) * 2;
                for (int i = 0; i < method.GenValsIntl.Count; ++i)
                {
                    TypeMetaInfo genParamMetaInfo = default;
                    Type? genParameterType = method.GenValsIntl[i];
                    if (genParameterType == null)
                        continue;

                    Type originalGenParamType = genParameterType;

                    genParamMetaInfo.Init(ref genParameterType, out string? elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* genParamElementStack = stackalloc int[genParamMetaInfo.ElementTypesLength];
                    genParamMetaInfo.ElementTypes = genParamElementStack;
                    len += GetNonDeclaritiveTypeNameLength(genParameterType, originalGenParamType, ref genParamMetaInfo, elementKeyword);
                }
            }
        }

        if (parameters != null)
        {
            len += 2 + (Math.Max(parameters.Count, 1) - 1) * 2;
            for (int i = 0; i < parameters.Count; ++i)
            {
                MethodParameterDefinition parameter = parameters[i];
                TypeMetaInfo paramMetaInfo = default;
                Type? parameterType = parameter.Type;
                string? name = parameter.Name;

                if (i == 0 && method.IsStatic && declType != null && _accessor.GetIsStatic(declType) && method is { IsStatic: true, IsExtensionMethod: true })
                    len += 5;

                if (parameterType != null)
                {
                    if (!string.IsNullOrEmpty(name))
                        len += name.Length + 1;
                    Type originalParamType = parameterType;
                    paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* paramElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                    paramMetaInfo.ElementTypes = paramElementStack;
                    paramMetaInfo.SetupDimensionsAndOrdering(originalParamType);
                    paramMetaInfo.LoadParameter(ref parameter, this);
                    len += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
                }
                else if (parameter.GenericTypeIndex >= 0 && method.GenDefsIntl != null &&
                         (method.GenValsIntl == null || method.GenValsIntl.Count < method.GenDefsIntl.Count))
                {
                    if (!string.IsNullOrEmpty(name))
                        len += name.Length + 1;
                    paramMetaInfo.Init(ref parameter, method, out _, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                    paramMetaInfo.ElementTypes = parameterElementStack;
                    paramMetaInfo.LoadParameter(ref parameter, this);
                    len += paramMetaInfo.Length;
                }
                else if (!string.IsNullOrEmpty(name))
                    len += name.Length;
            }
        }

        return len;
    }

    /// <inheritdoc/>
    public virtual unsafe int Format(MethodDefinition method, Span<char> output)
    {
        string? methodName = method.Name;
        List<MethodParameterDefinition>? parameters = method.ParametersIntl;
        bool isDef = method.GenDefsIntl != null && (method.GenValsIntl == null || method.GenDefsIntl.Count > method.GenValsIntl.Count) && method.GenDefsIntl.Count > 0;
        bool isGenVal = !isDef && method.GenValsIntl is { Count: > 0 };
        bool isCtor = method.IsConstructor;

        int index = 0;
        
        Type? declType = method.DeclaringType;
        Type? originalDeclType = declType;

        if (method.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        Type? returnType = method.ReturnType;
        Type? originalReturnType = returnType;

        TypeMetaInfo methodTypeMeta = default;
        TypeMetaInfo declTypeMeta = default;

        if (returnType != null)
        {
            methodTypeMeta.Init(ref returnType, out string? returnTypeKeyword, this);
            int* methodTypeElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
            methodTypeMeta.ElementTypes = methodTypeElementStack;
            methodTypeMeta.SetupDimensionsAndOrdering(originalReturnType!);
            methodTypeMeta.LoadReturnType(method, this);
            FormatType(returnType, in methodTypeMeta, returnTypeKeyword, output, ref index);
            if (!isCtor && (methodName != null || declType != null))
            {
                output[index] = ' ';
                ++index;
            }
        }
        else if (isDef && method.ReturnTypeGenericIndex >= 0 && method.ReturnTypeGenericIndex < method.GenDefsIntl!.Count)
        {
            methodTypeMeta.InitReturnTypeMeta(method, out string? returnTypeKeyword, this);
            if (returnTypeKeyword != null)
            {
                // ReSharper disable once StackAllocInsideLoop
                int* parameterElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
                methodTypeMeta.ElementTypes = parameterElementStack;
                methodTypeMeta.FlipArrayGroups();
                methodTypeMeta.LoadReturnType(method, this);
                FormatType(null!, in methodTypeMeta, returnTypeKeyword, output, ref index);
                if (!isCtor && (methodName != null || declType != null))
                {
                    output[index] = ' ';
                    ++index;
                }
            }
        }
        if (declType != null && !isCtor)
        {
            declTypeMeta.Init(ref declType, out string? declTypeKeyword, this);
            int* declTypeElementStack = stackalloc int[declTypeMeta.ElementTypesLength];
            declTypeMeta.ElementTypes = declTypeElementStack;
            declTypeMeta.SetupDimensionsAndOrdering(originalDeclType!);
            FormatType(declType, in declTypeMeta, declTypeKeyword, output, ref index);
            if (methodName != null)
            {
                output[index] = '.';
                ++index;
            }
        }

        if (!isCtor && methodName != null)
        {
            methodName.AsSpan().CopyTo(output[index..]);
            index += methodName.Length;
        }

        if (isDef || isGenVal)
        {
            output[index] = '<';
            ++index;
            if (isDef)
            {
                for (int i = 0; i < method.GenDefsIntl!.Count; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    string name = method.GenDefsIntl![i];
                    if (name == null)
                        continue;

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
                    name.AsSpan().CopyTo(output[index..]);
#else
                    for (int j = 0; j < name.Length; ++j)
                    {
                        output[index + j] = name[j];
                    }
#endif
                    index += name.Length;
                }
            }
            else
            {
                for (int i = 0; i < method.GenValsIntl!.Count; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    TypeMetaInfo genParamMetaInfo = default;
                    Type? genParameterType = method.GenValsIntl![i];
                    if (genParameterType == null)
                        continue;

                    Type originalGenParameterType = genParameterType;

                    genParamMetaInfo.Init(ref genParameterType, out string? elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* genParamElementStack = stackalloc int[genParamMetaInfo.ElementTypesLength];
                    genParamMetaInfo.ElementTypes = genParamElementStack;
                    genParamMetaInfo.SetupDimensionsAndOrdering(originalGenParameterType);
                    FormatType(genParameterType, in genParamMetaInfo, elementKeyword, output, ref index);
                }
            }
            output[index] = '>';
            ++index;
        }

        output[index] = '(';
        ++index;
        if (parameters != null)
        {
            for (int i = 0; i < parameters.Count; ++i)
            {
                if (i != 0)
                {
                    output[index] = ',';
                    output[index + 1] = ' ';
                    index += 2;
                }

                MethodParameterDefinition parameter = parameters[i];
                TypeMetaInfo paramMeta = default;
                Type? parameterType = parameter.Type;
                string? elementKeyword = null;
                if (parameter.GenericTypeIndex >= 0 && method.GenValsIntl != null &&
                    (method.GenDefsIntl == null || method.GenDefsIntl.Count < method.GenValsIntl.Count) && parameterType == null)
                {
                    parameterType = parameter.GenericTypeIndex < method.GenValsIntl!.Count ? method.GenValsIntl[parameter.GenericTypeIndex] : null;
                    parameter.Type = parameterType;
                    parameters[i] = parameter;
                }
                if (parameterType != null)
                {
                    Type originalParameterType = parameterType;
                    paramMeta.Init(ref parameterType, out elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* parameterElementStack = stackalloc int[paramMeta.ElementTypesLength];
                    paramMeta.ElementTypes = parameterElementStack;
                    paramMeta.SetupDimensionsAndOrdering(originalParameterType);
                    paramMeta.LoadParameter(ref parameter, this);
                }
                else
                {
                    paramMeta.Init(ref parameter, method, out elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* parameterElementStack = stackalloc int[paramMeta.ElementTypesLength];
                    paramMeta.ElementTypes = parameterElementStack;
                    paramMeta.FlipArrayGroups();
                    paramMeta.LoadParameter(ref parameter, this);
                }

                WriteParameter(ref parameter, method, parameterType, elementKeyword, output, ref index, in paramMeta,
                    isExtensionThisParameter: i == 0 && method.IsStatic && declType != null && _accessor.GetIsStatic(declType) && method is { IsStatic: true, IsExtensionMethod: true });
            }
        }

        output[index] = ')';
        return index + 1;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(FieldInfo field, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = _accessor.GetVisibility(field);
        int len = 0;
        if (includeDefinitionKeywords)
        {
            len += GetVisibilityLength(vis) + 1;
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
        MemberVisibility vis = _accessor.GetVisibility(field);
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
    public virtual int GetFormatLength(FieldDefinition field)
    {
        bool nameIsNull = string.IsNullOrEmpty(field.Name);
        int len = 0;

        if (field.IsConstant)
        {
            len += 5;
            if (field.FieldType != null || field.DeclaringType != null || !nameIsNull)
                ++len;
        }
        else if (field.IsStatic)
        {
            len += 6;
            if (field.FieldType != null || field.DeclaringType != null || !nameIsNull)
                ++len;
        }

        if (field.FieldType != null)
        {
            len += GetFormatLength(field.FieldType, false);
            if (!nameIsNull || field.DeclaringType != null)
                ++len;
        }

        if (field.DeclaringType != null)
        {
            len += GetFormatLength(field.DeclaringType);
            if (!nameIsNull)
                ++len;
        }

        if (!nameIsNull)
            len += field.Name!.Length;

        return len;
    }

    /// <inheritdoc/>
    public virtual int Format(FieldDefinition field, Span<char> output)
    {
        bool nameIsNull = string.IsNullOrEmpty(field.Name);
        int index = 0;

        if (field.IsConstant)
        {
            WriteKeyword(LitConst, ref index, output, spaceSuffix: field.FieldType != null || field.DeclaringType != null || !nameIsNull);
        }
        else if (field.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: field.FieldType != null || field.DeclaringType != null || !nameIsNull);
        }

        if (field.FieldType != null)
        {
            index += Format(field.FieldType, output[index..]);
            if (!nameIsNull || field.DeclaringType != null)
            {
                output[index] = ' ';
                ++index;
            }
        }

        if (field.DeclaringType != null)
        {
            index += Format(field.DeclaringType, output[index..]);
            if (!nameIsNull)
            {
                output[index] = '.';
                ++index;
            }
        }

        if (!nameIsNull)
        {
            ReadOnlySpan<char> name = field.Name;
            name.CopyTo(output[index..]);
            index += name.Length;
        }

        return index;
    }

    /// <inheritdoc/>
    public virtual unsafe int GetFormatLength(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false)
    {
        int len = GetFormatLength(property.PropertyType, false) + 1;

        ParameterInfo[] indexParameters = property.GetIndexParameters();

        if (indexParameters.Length == 0)
            len += property.Name.Length;
        else
            len += 4; // this

        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetSetMethod(true);

        MemberVisibility vis = _accessor.GetHighestVisibility(getMethod, setMethod, null);

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
            {
                ParameterInfo parameter = indexParameters[i];
                TypeMetaInfo paramMetaInfo = default;
                Type parameterType = parameter.ParameterType;
                Type originalParamType = parameterType;
                string? name = parameter.Name;

                if (!string.IsNullOrEmpty(name))
                    len += name.Length + 1;

                paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
                // ReSharper disable once StackAllocInsideLoop
                int* paramElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                paramMetaInfo.ElementTypes = paramElementStack;
                paramMetaInfo.SetupDimensionsAndOrdering(originalParamType);
                paramMetaInfo.LoadParameter(parameter, this);
                len += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
            }
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
    public virtual unsafe int Format(PropertyInfo property, Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false)
    {
        ParameterInfo[] indexParameters = property.GetIndexParameters();

        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetSetMethod(true);

        int index = 0;
        MemberVisibility vis = _accessor.GetHighestVisibility(getMethod, setMethod);
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

                ParameterInfo parameter = indexParameters[i];
                TypeMetaInfo paramMeta = default;
                Type parameterType = parameter.ParameterType;
                Type originalParameterType = parameterType;
                paramMeta.Init(ref parameterType, out string? elementKeyword, this);
                // ReSharper disable once StackAllocInsideLoop
                int* parameterElementStack = stackalloc int[paramMeta.ElementTypesLength];
                paramMeta.ElementTypes = parameterElementStack;
                paramMeta.SetupDimensionsAndOrdering(originalParameterType);
                paramMeta.LoadParameter(parameter, this);
                WriteParameter(indexParameters[i], parameterType, elementKeyword, output, ref index, in paramMeta);
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
    public virtual unsafe int GetFormatLength(PropertyDefinition property, bool includeAccessors = true)
    {
        bool nameIsNull = string.IsNullOrEmpty(property.Name);
        int len = 0;

        if (property.PropertyType != null)
        {
            len += GetFormatLength(property.PropertyType, false);
            if (!nameIsNull || property.DeclaringType != null)
                ++len;
        }

        if (property.DeclaringType != null)
        {
            len += GetFormatLength(property.DeclaringType, false);
            if (!nameIsNull)
                ++len;
        }

        List<MethodParameterDefinition>? indexParameters = property.IndexParametersIntl;
        bool isIndexer = indexParameters is { Count: > 0 };

        if (!nameIsNull && !isIndexer)
            len += property.Name!.Length;
        else if (isIndexer)
            len += 4; // this

        if (property.IsStatic)
        {
            len += 6;
            if (isIndexer || includeAccessors || property.DeclaringType != null || property.PropertyType != null)
                ++len;
        }

        if (isIndexer)
        {
            len += 2 + (indexParameters!.Count - 1) * 2;
            for (int i = 0; i < indexParameters.Count; ++i)
            {
                MethodParameterDefinition parameter = indexParameters[i];
                TypeMetaInfo paramMetaInfo = default;
                Type? parameterType = parameter.Type;
                string? name = parameter.Name;

                if (!string.IsNullOrEmpty(name))
                {
                    if (parameterType != null)
                        ++len;
                    len += name!.Length;
                }

                if (parameterType == null)
                    continue;

                Type originalParameterType = parameterType;

                paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
                // ReSharper disable once StackAllocInsideLoop
                int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                paramMetaInfo.ElementTypes = parameterElementStack;
                paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
                paramMetaInfo.LoadParameter(ref parameter, this);
                len += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
            }
        }

        if (includeAccessors)
        {
            if (property.DeclaringType != null || property.PropertyType != null)
                ++len; // " "

            len += 3; // "{ }"

            if (property.HasGetter)
                len += 5;
            if (property.HasSetter)
                len += 5;
        }

        return len;
    }

    /// <inheritdoc/>
    public virtual unsafe int Format(PropertyDefinition property, Span<char> output, bool includeAccessors = true)
    {
        bool nameIsNull = string.IsNullOrEmpty(property.Name);
        List<MethodParameterDefinition>? indexParameters = property.IndexParametersIntl;
        bool isIndexer = indexParameters is { Count: > 0 };

        int index = 0;

        if (property.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: isIndexer || includeAccessors || property.DeclaringType != null || property.PropertyType != null);
        }

        if (property.PropertyType != null)
        {
            index += Format(property.PropertyType, output[index..]);
            if (property.DeclaringType != null || !nameIsNull)
            {
                output[index] = ' ';
                ++index;
            }
        }

        if (property.DeclaringType != null)
        {
            index += Format(property.DeclaringType, output[index..]);
            if (!nameIsNull)
            {
                output[index] = '.';
                ++index;
            }
        }

        if (!isIndexer)
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
            for (int i = 0; i < indexParameters!.Count; ++i)
            {
                if (i != 0)
                {
                    output[index] = ',';
                    output[index + 1] = ' ';
                    index += 2;
                }

                MethodParameterDefinition parameter = indexParameters[i];
                Type? parameterType = parameter.Type;
                string? name = parameter.Name;

                if (parameterType == null)
                {
                    if (!string.IsNullOrEmpty(name))
                    {
                        name.AsSpan().CopyTo(output[index..]);
                        index += name.Length;
                    }
                    continue;
                }
                TypeMetaInfo paramMetaInfo = default;

                Type originalParameterType = parameterType;

                paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
                // ReSharper disable once StackAllocInsideLoop
                int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                paramMetaInfo.ElementTypes = parameterElementStack;
                paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
                paramMetaInfo.LoadParameter(ref parameter, this);
                WriteParameter(ref parameter, property, parameterType, elementKeyword, output, ref index, in paramMetaInfo);
            }
            output[index] = ']';
            ++index;
        }

        if (includeAccessors)
        {
            if (property.DeclaringType != null || property.PropertyType != null)
            {
                output[index] = ' ';
                ++index;
            }

            output[index] = '{';
            output[index + 1] = ' ';
            index += 2;

            if (property.HasGetter)
            {
                WriteKeyword(LitGet, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

            if (property.HasSetter)
            {
                WriteKeyword(LitSet, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

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

        MemberVisibility vis = _accessor.GetHighestVisibility(addMethod, removeMethod, raiseMethod);

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

        int len = GetFormatLength(delegateType, false) + 1;

        if (includeEventKeyword)
            len += 6;

        if (includeDefinitionKeywords)
            len += GetVisibilityLength(vis) + 1;

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

        MemberVisibility vis = _accessor.GetHighestVisibility(addMethod, removeMethod, raiseMethod);

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
        if (includeDefinitionKeywords)
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }

        if (addMethod != null && addMethod.IsStatic || removeMethod != null && removeMethod.IsStatic || raiseMethod != null && raiseMethod.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        if (includeEventKeyword)
        {
            WriteKeyword(LitEvent, ref index, output, spaceSuffix: true);
        }

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
    public virtual int GetFormatLength(EventDefinition @event, bool includeAccessors = true, bool includeEventKeyword = true)
    {
        Type? delegateType = @event.HandlerType;

        bool nameIsNull = string.IsNullOrEmpty(@event.Name);
        int len = 0;

        if (delegateType != null)
        {
            len += GetFormatLength(delegateType, false);
            if (!nameIsNull || @event.DeclaringType != null)
                ++len;
        }

        if (@event.DeclaringType != null)
        {
            len += GetFormatLength(@event.DeclaringType, false);
            if (!nameIsNull)
                ++len;
        }

        if (includeEventKeyword)
        {
            len += 5;
            if (includeAccessors || @event.DeclaringType != null || delegateType != null)
                ++len;
        }

        if (!nameIsNull)
            len += @event.Name!.Length;

        if (@event.IsStatic)
        {
            len += 6;
            if (includeAccessors || includeEventKeyword || @event.DeclaringType != null || delegateType != null)
                ++len;
        }

        if (includeAccessors)
        {
            if (@event.DeclaringType != null || @event.HandlerType != null)
                ++len; // " "

            len += 3; // "{ }"

            if (@event.HasAdder)
                len += 5;
            if (@event.HasRemover)
                len += 8;
            if (@event.HasRaiser)
                len += 7;
        }

        return len;
    }

    /// <inheritdoc/>
    public virtual int Format(EventDefinition @event, Span<char> output, bool includeAccessors = true, bool includeEventKeyword = true)
    {
        Type? delegateType = @event.HandlerType;

        bool nameIsNull = string.IsNullOrEmpty(@event.Name);

        int index = 0;

        if (@event.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: includeAccessors || includeEventKeyword || @event.DeclaringType != null || delegateType != null);
        }

        if (includeEventKeyword)
        {
            WriteKeyword(LitEvent, ref index, output, spaceSuffix: includeAccessors || @event.DeclaringType != null || delegateType != null);
        }

        if (delegateType != null)
        {
            index += Format(delegateType, output[index..]);
            if (!nameIsNull || @event.DeclaringType != null)
            {
                output[index] = ' ';
                ++index;
            }
        }

        if (@event.DeclaringType != null)
        {
            index += Format(@event.DeclaringType, output[index..]);
            if (!nameIsNull)
            {
                output[index] = '.';
                ++index;
            }
        }

        if (@event.Name != null)
        {
            ReadOnlySpan<char> name = @event.Name;
            name.CopyTo(output[index..]);
            index += name.Length;
        }

        if (includeAccessors)
        {
            if (@event.DeclaringType != null || @event.HandlerType != null)
            {
                output[index] = ' ';
                ++index;
            }

            output[index] = '{';
            output[index + 1] = ' ';
            index += 2;

            if (@event.HasAdder)
            {
                WriteKeyword(LitAdd, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

            if (@event.HasRemover)
            {
                WriteKeyword(LitRemove, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

            if (@event.HasRaiser)
            {
                WriteKeyword(LitRaise, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

            output[index] = '}';
            ++index;
        }

        return index;
    }

    /// <inheritdoc/>
    public virtual unsafe int GetFormatLength(ParameterInfo parameter, bool isExtensionThisParameter = false)
    {
        TypeMetaInfo paramMetaInfo = default;
        Type parameterType = parameter.ParameterType;
        Type originalParameterType = parameterType;
        string? name = parameter.Name;
        int len = 0;
        if (isExtensionThisParameter)
            len += 5;

        if (!string.IsNullOrEmpty(name))
            len += name.Length + 1;

        paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
        // ReSharper disable once StackAllocInsideLoop
        int* paramElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
        paramMetaInfo.ElementTypes = paramElementStack;
        paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
        paramMetaInfo.LoadParameter(parameter, this);

        len += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
        return len;
    }

    /// <inheritdoc/>
    public virtual unsafe int Format(ParameterInfo parameter, Span<char> output, bool isExtensionThisParameter = false)
    {
        TypeMetaInfo paramMetaInfo = default;
        Type parameterType = parameter.ParameterType;
        Type originalParameterType = parameterType;
        paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
        int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
        paramMetaInfo.ElementTypes = parameterElementStack;
        paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
        paramMetaInfo.LoadParameter(parameter, this);

        int index = 0;

        WriteParameter(parameter, parameterType, elementKeyword, output, ref index, in paramMetaInfo, isExtensionThisParameter);
        return index;
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
    public virtual string Format(Label label) => ((uint)label.GetLabelId()).ToString("D0", CultureInfo.InvariantCulture);
    
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

                return name + " lbl." + ((uint)lbl.GetLabelId()).ToString("D0", CultureInfo.InvariantCulture);

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

                return name + " " + l64.ToString("D0", CultureInfo.InvariantCulture);

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

                return name + " " + l64.ToString("D0", CultureInfo.InvariantCulture);

            case OperandType.InlineString:
                if (operand is not string str)
                    break;

                return name + " \"" + str + "\"";

            case OperandType.InlineSwitch:
                if (operand is not Label[] jumps)
                    break;

                if (jumps.Length == 0)
                {
                    return name + (usageContext == OpCodeFormattingContext.InLine ? " ( )" : " { }");
                }

                StringBuilder switchBuilder = new StringBuilder(name);

                if (usageContext == OpCodeFormattingContext.InLine)
                {
                    switchBuilder
                        .Append(' ')
                        .Append('(');

                    for (int i = 0; i < jumps.Length; ++i)
                    {
                        if (i != 0)
                            switchBuilder.Append(',').Append(' ');

                        switchBuilder
                            .Append("lbl.")
                            .Append(((uint)jumps[i].GetLabelId()).ToString("D0", CultureInfo.InvariantCulture));
                    }

                    switchBuilder
                        .Append(')');
                }
                else
                {
                    switchBuilder
                        .Append(Environment.NewLine)
                        .Append('{');

                    for (int i = 0; i < jumps.Length; ++i)
                    {
                        switchBuilder.Append(Environment.NewLine)
                            .Append(' ', 2)
                            .Append(((uint)i).ToString("D0", CultureInfo.InvariantCulture))
                            .Append(" => lbl.")
                            .Append(((uint)jumps[i].GetLabelId()).ToString("D0", CultureInfo.InvariantCulture));
                    }

                    switchBuilder
                        .Append(Environment.NewLine)
                        .Append('}');
                }


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
                    name += " " + ((uint)lb.LocalIndex).ToString("D0", CultureInfo.InvariantCulture);
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

                return name + " " + l64.ToString("D0", CultureInfo.InvariantCulture);
        }

        return name;
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

        MemberVisibility vis = _accessor.GetVisibility(method);
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
                int s = GetTypeKeywordLength(type);
                if (s != -1)
                    length += s;
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
    protected virtual int GetNonDeclaritiveTypeNameLength(Type type, Type originalType, ref TypeMetaInfo metaInfo, string? elementKeyword)
    {
        metaInfo.SetupDimensionsAndOrdering(originalType);
        int length = metaInfo.Length;

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
    /// Calculate the length of a type name without adding definition keywords.
    /// </summary>
    protected virtual int GetNonDeclaritiveTypeNameLengthNoSetup(Type type, ref TypeMetaInfo metaInfo, string? elementKeyword)
    {
        int length = metaInfo.Length;

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
    /// Gets the language keyword for the type instead of the CLR type name, or <see langword="null"/> if the type doesn't have a keyword. Also override <see cref="GetTypeKeywordLength"/>.
    /// </summary>
    protected virtual string? GetTypeKeyword(Type type)
    {
        if (!UseTypeKeywords)
            return null;

        return InternalUtil.GetKeyword(type);
    }

    /// <summary>
    /// Gets the language keyword for the type instead of the CLR type name, or <see langword="null"/> if the type doesn't have a keyword.
    /// </summary>
    /// <returns>The length of the keyword, or -1 if there is no associated keyword.</returns>
    protected virtual int GetTypeKeywordLength(Type type)
    {
        if (!UseTypeKeywords)
            return -1;

        return InternalUtil.GetKeywordLength(type);
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(ParameterInfo parameter, bool isExtensionThisParameter = false)
    {
        TypeMetaInfo paramMetaInfo = default;
        Type parameterType = parameter.ParameterType;
        Type originalParameterType = parameterType;
        string? name = parameter.Name;
        int len = 0;
        if (isExtensionThisParameter)
            len += 5;

        if (!string.IsNullOrEmpty(name))
            len += name.Length + 1;

        paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
        int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
        paramMetaInfo.ElementTypes = parameterElementStack;
        paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
        paramMetaInfo.LoadParameter(parameter, this);
        len += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);


#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[len];
#else
        char* output = stackalloc char[len];
#endif
        int index = 0;

        WriteParameter(parameter, parameterType, elementKeyword, output, ref index, in paramMetaInfo, isExtensionThisParameter);
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false)
    {
        ParameterInfo[] indexParameters = property.GetIndexParameters();
        TypeMetaInfo* indexInfo = stackalloc TypeMetaInfo[indexParameters.Length];
            
        MethodInfo? getMethod = property.GetGetMethod(true);
        MethodInfo? setMethod = property.GetSetMethod(true);

        MemberVisibility vis = _accessor.GetHighestVisibility(getMethod, setMethod);

        Type? declaringType = property.DeclaringType;
        Type? originalDeclaringType = declaringType;
        string? declTypeElementKeyword = null;
        bool isStatic = getMethod is { IsStatic: true } || setMethod is { IsStatic: true };
        Type returnType = property.PropertyType;
        Type originalReturnType = returnType;
        bool isIndexer = indexParameters.Length > 0;
        TypeMetaInfo declTypeMetaInfo = default;
        TypeMetaInfo returnTypeMetaInfo = default;

        returnTypeMetaInfo.Init(ref returnType, out string? returnElementKeyword, this);
        int* returnTypeElements = stackalloc int[returnTypeMetaInfo.ElementTypesLength];
        returnTypeMetaInfo.ElementTypes = returnTypeElements;
        int length = GetNonDeclaritiveTypeNameLength(returnType, originalReturnType, ref returnTypeMetaInfo, returnElementKeyword) + 1;

        if (declaringType != null)
        {
            declTypeMetaInfo.Init(ref declaringType, out declTypeElementKeyword, this);
            int* declTypeElements = stackalloc int[declTypeMetaInfo.ElementTypesLength];
            declTypeMetaInfo.ElementTypes = declTypeElements;
            length += GetNonDeclaritiveTypeNameLength(declaringType, originalDeclaringType!, ref declTypeMetaInfo, declTypeElementKeyword) + 1;
        }

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
                ParameterInfo parameter = indexParameters[i];
                ref TypeMetaInfo paramMetaInfo = ref indexInfo[i];
                Type parameterType = parameter.ParameterType;
                Type originalParameterType = parameterType;
                string? name = parameter.Name;

                if (!string.IsNullOrEmpty(name))
                    length += name.Length + 1;

                paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
                // ReSharper disable once StackAllocInsideLoop
                int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                paramMetaInfo.ElementTypes = parameterElementStack;
                paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
                paramMetaInfo.LoadParameter(parameter, this);
                length += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
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

                WriteParameter(indexParameters[i], output, ref index, in indexInfo[i], false);
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
    public virtual unsafe string Format(PropertyDefinition property, bool includeAccessors = true)
    {
        List<MethodParameterDefinition>? indexParameters = property.IndexParametersIntl;
        TypeMetaInfo* indexInfo = stackalloc TypeMetaInfo[indexParameters?.Count ?? 0];

        Type? declaringType = property.DeclaringType;
        Type? originalDeclaringType = declaringType;
        string? declTypeElementKeyword = null;
        string? returnElementKeyword = null;
        bool isStatic = property.IsStatic;
        Type? returnType = property.PropertyType;
        Type? originalReturnType = returnType;
        bool isIndexer = indexParameters is { Count: > 0 };
        TypeMetaInfo declTypeMetaInfo = default;
        TypeMetaInfo returnTypeMetaInfo = default;
        bool isNameNull = isIndexer || string.IsNullOrEmpty(property.Name);
        
        int length = 0;
        if (returnType != null)
        {
            returnTypeMetaInfo.Init(ref returnType, out returnElementKeyword, this);
            int* returnTypeElements = stackalloc int[returnTypeMetaInfo.ElementTypesLength];
            returnTypeMetaInfo.ElementTypes = returnTypeElements;
            length = GetNonDeclaritiveTypeNameLength(returnType, originalReturnType!, ref returnTypeMetaInfo, returnElementKeyword) + 1;
        }

        if (declaringType != null)
        {
            declTypeMetaInfo.Init(ref declaringType, out declTypeElementKeyword, this);
            int* declTypeElements = stackalloc int[declTypeMetaInfo.ElementTypesLength];
            declTypeMetaInfo.ElementTypes = declTypeElements;
            length += GetNonDeclaritiveTypeNameLength(declaringType, originalDeclaringType!, ref declTypeMetaInfo, declTypeElementKeyword) + 1;
        }

        if (!isIndexer)
            length += !isNameNull ? property.Name!.Length : 0;
        else
            length += 4; // this

        if (isStatic)
        {
            length += 6;
            if (includeAccessors || declaringType != null || returnType != null)
                ++length;
        }

        if (isIndexer)
        {
            length += 2 + (indexParameters!.Count - 1) * 2;
            for (int i = 0; i < indexParameters.Count; ++i)
            {
                MethodParameterDefinition parameter = indexParameters[i];
                ref TypeMetaInfo paramMetaInfo = ref indexInfo[i];
                Type? parameterType = parameter.Type;
                string? name = parameter.Name;

                if (!string.IsNullOrEmpty(name))
                {
                    if (parameterType != null)
                        ++length;
                    length += name!.Length;
                }

                if (parameterType == null)
                    continue;

                Type originalParameterType = parameterType;

                paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
                // ReSharper disable once StackAllocInsideLoop
                int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                paramMetaInfo.ElementTypes = parameterElementStack;
                paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
                paramMetaInfo.LoadParameter(ref parameter, this);
                length += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
            }
        }

        if (includeAccessors)
        {
            if (isIndexer || declaringType != null || returnType != null)
                ++length; // " "

            length += 3; // "{ }"

            if (property.HasGetter)
                length += 5;
            if (property.HasSetter)
                length += 5;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[length];
#else
        char* output = stackalloc char[length];
#endif
        int index = 0;
        if (isStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: includeAccessors || declaringType != null || returnType != null);
        }

        if (returnType != null)
        {
            FormatType(returnType, in returnTypeMetaInfo, returnElementKeyword, output, ref index);
            
            if (declaringType != null || isIndexer || !isNameNull)
            {
                output[index] = ' ';
                ++index;
            }
        }

        if (declaringType != null)
        {
            FormatType(declaringType, in declTypeMetaInfo, declTypeElementKeyword, output, ref index);
            if (isIndexer || !isNameNull)
            {
                output[index] = '.';
                ++index;
            }
        }

        if (!isIndexer && !isNameNull)
        {
            string name = property.Name!;
            for (int i = 0; i < name.Length; ++i)
                output[index + i] = name[i];
            index += name.Length;
        }
        else if (isIndexer)
        {
            WriteKeyword(LitThis, ref index, output);
            output[index] = '[';
            ++index;
            for (int i = 0; i < indexParameters!.Count; ++i)
            {
                if (i != 0)
                {
                    output[index] = ',';
                    output[index + 1] = ' ';
                    index += 2;
                }

                MethodParameterDefinition parameter = indexParameters[i];

                ref readonly TypeMetaInfo paramMetaInfo = ref indexInfo[i];
                Type? parameterType = parameter.Type;
                string? elementKeyword = null;
                if (parameterType != null)
                {
                    if (paramMetaInfo.ElementTypesLength > 0)
                    {
                        for (Type? elemType = parameterType.GetElementType(); elemType != null; elemType = elemType.GetElementType())
                            parameterType = elemType;
                        elementKeyword = GetTypeKeyword(parameterType);
                    }
                }

                WriteParameter(ref parameter, property, parameterType, elementKeyword, output, ref index, in paramMetaInfo);
            }
            output[index] = ']';
            ++index;
        }

        if (includeAccessors)
        {
            if (isIndexer || declaringType != null || returnType != null)
            {
                output[index] = ' ';
                ++index;
            }
            output[index] = '{';
            output[index + 1] = ' ';
            index += 2;

            if (property.HasGetter)
            {
                WriteKeyword(LitGet, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

            if (property.HasSetter)
            {
                WriteKeyword(LitSet, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

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

        MemberVisibility vis = _accessor.GetHighestVisibility(addMethod, removeMethod, raiseMethod);

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
        Type? originalDeclaringType = declaringType;
        string? declTypeElementKeyword = null;
        bool isStatic = addMethod is { IsStatic: true } || removeMethod is { IsStatic: true } || raiseMethod is { IsStatic: true };
        TypeMetaInfo declTypeMetaInfo = default;
        TypeMetaInfo handlerTypeMetaInfo = default;

        handlerTypeMetaInfo.Init(ref delegateType, out _, this);
        handlerTypeMetaInfo.ElementTypesLength = 0;
        int length = GetNonDeclaritiveTypeNameLength(delegateType, delegateType, ref handlerTypeMetaInfo, null) + 1;

        if (declaringType != null)
        {
            declTypeMetaInfo.Init(ref declaringType, out declTypeElementKeyword, this);
            int* declaringTypeElementStack = stackalloc int[declTypeMetaInfo.ElementTypesLength];
            declTypeMetaInfo.ElementTypes = declaringTypeElementStack;
            length += GetNonDeclaritiveTypeNameLength(declaringType, originalDeclaringType!, ref declTypeMetaInfo, declTypeElementKeyword) + 1;
        }

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
    public virtual unsafe string Format(EventDefinition @event, bool includeAccessors = true, bool includeEventKeyword = true)
    {
        // get delegate type
        Type? delegateType = @event.HandlerType;

        Type? declaringType = @event.DeclaringType;
        Type? originalDeclaringType = declaringType;
        string? declTypeElementKeyword = null;
        bool isStatic = @event.IsStatic;
        TypeMetaInfo declTypeMetaInfo = default;
        TypeMetaInfo handlerTypeMetaInfo = default;
        bool nameIsNull = string.IsNullOrEmpty(@event.Name);

        int length = 0;
        if (delegateType != null)
        {
            handlerTypeMetaInfo.Init(ref delegateType, out _, this);
            handlerTypeMetaInfo.ElementTypesLength = 0;
            length = GetNonDeclaritiveTypeNameLength(delegateType, delegateType, ref handlerTypeMetaInfo, null) + 1;
        }

        if (declaringType != null)
        {
            declTypeMetaInfo.Init(ref declaringType, out declTypeElementKeyword, this);
            int* declaringTypeElementStack = stackalloc int[declTypeMetaInfo.ElementTypesLength];
            declTypeMetaInfo.ElementTypes = declaringTypeElementStack;
            length += GetNonDeclaritiveTypeNameLength(declaringType, originalDeclaringType!, ref declTypeMetaInfo, declTypeElementKeyword) + 1;
        }

        if (isStatic)
        {
            length += 6;
            if (includeAccessors || includeEventKeyword || declaringType != null || delegateType != null)
                ++length;
        }

        if (includeEventKeyword)
        {
            length += 6;
            if (includeAccessors || declaringType != null || delegateType != null)
                ++length;
        }

        if (!nameIsNull)
            length += @event.Name!.Length;

        if (includeAccessors)
        {
            if (declaringType != null || delegateType != null)
                ++length; // " "

            length += 3; // "{ }"

            if (@event.HasAdder)
                length += 5;
            if (@event.HasRemover)
                length += 8;
            if (@event.HasRaiser)
                length += 7;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[length];
#else
        char* output = stackalloc char[length];
#endif
        int index = 0;
        if (isStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: includeAccessors || includeEventKeyword || declaringType != null || delegateType != null);
        }

        if (includeEventKeyword)
        {
            WriteKeyword(LitEvent, ref index, output, spaceSuffix: includeAccessors || declaringType != null || delegateType != null);
        }

        if (delegateType != null)
        {
            FormatType(delegateType, in handlerTypeMetaInfo, null, output, ref index);

            if (declaringType != null || !nameIsNull)
            {
                output[index] = ' ';
                ++index;
            }
        }

        if (declaringType != null)
        {
            FormatType(declaringType, in declTypeMetaInfo, declTypeElementKeyword, output, ref index);
            if (!nameIsNull)
            {
                output[index] = '.';
                ++index;
            }
        }

        if (!nameIsNull)
        {
            string name = @event.Name!;
            for (int i = 0; i < name.Length; ++i)
                output[index + i] = name[i];
            index += name.Length;
        }

        if (includeAccessors)
        {
            output[index] = ' ';
            output[index + 1] = '{';
            output[index + 2] = ' ';
            index += 3;

            if (@event.HasAdder)
            {
                WriteKeyword(LitAdd, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

            if (@event.HasRemover)
            {
                WriteKeyword(LitRemove, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

            if (@event.HasRaiser)
            {
                WriteKeyword(LitRaise, ref index, output);
                output[index] = ';';
                output[index + 1] = ' ';
                index += 2;
            }

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
    public virtual unsafe string Format(Type type, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        string? s = GetTypeKeyword(type);
        if (s != null)
            return s;

        TypeMetaInfo main = default;
        Type originalType = type;
        main.Init(ref type, out string? elementKeyword, this);
        int* elementTypes = stackalloc int[main.ElementTypesLength];
        main.ElementTypes = elementTypes;
        main.SetupDimensionsAndOrdering(originalType);
        main.LoadRefType(refMode, this);

        TypeMetaInfo delegateRtnType = default;

        MemberVisibility vis = includeDefinitionKeywords ? _accessor.GetVisibility(type) : default;
        bool isValueType = type.IsValueType;
        bool isAbstract = !isValueType && includeDefinitionKeywords && type is { IsAbstract: true, IsSealed: false, IsInterface: false };
        bool isDelegate = !isValueType && includeDefinitionKeywords && type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
        bool isReadOnly = isValueType && includeDefinitionKeywords && _accessor.IsReadOnly(type);
        bool isStatic = !isValueType && includeDefinitionKeywords && _accessor.GetIsStatic(type);
        bool isByRefType = false;
        Type? delegateReturnType = null;
        isByRefType = includeDefinitionKeywords && _accessor.IsByRefLikeType(type);

        int length = main.Length;
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
                    MethodInfo invokeMethod = _accessor.GetInvokeMethod(type);
                    delegateReturnType = invokeMethod.ReturnType;
                    originalType = delegateReturnType;
                    delegateRtnType.Init(ref delegateReturnType, out _, this);
                    int* intlDelegateElementTypes = stackalloc int[delegateRtnType.ElementTypesLength];
                    delegateRtnType.ElementTypes = intlDelegateElementTypes;
                    delegateRtnType.SetupDimensionsAndOrdering(originalType);
                    delegateRtnType.LoadReturnType(invokeMethod, this);

                    ++length;
                    length += delegateRtnType.Length;
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
        WritePreDimensionsAndOrdering(in main, output, ref index);
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
                FormatType(delegateReturnType!, in delegateRtnType, null, output, ref index);
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
        WritePostDimensionsAndOrdering(in main, output, ref index);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }

    private string? GetKeyword(TypeDefinition type)
    {
        string? s = type.KeywordIntl;
        if (s == null || type.ElementTypesIntl is { Count: > 0 })
            return null;

        if (GetType() == typeof(DefaultOpCodeFormatter))
            return s;

        Type? t = InternalUtil.GetTypeFromKeyword(s);
        return t == null ? null : GetTypeKeyword(t);
    }
    private int GetKeywordLength(TypeDefinition type)
    {
        string? s = type.KeywordIntl;
        if (s == null || type.ElementTypesIntl is { Count: > 0 })
            return -1;

        if (GetType() == typeof(DefaultOpCodeFormatter))
            return s.Length;

        Type? t = InternalUtil.GetTypeFromKeyword(s);
        return t == null ? -1 : GetTypeKeywordLength(t);
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(TypeDefinition type, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        string? s = GetKeyword(type);
        if (s != null)
            return s;

        TypeMetaInfo main = default;
        int* elementTypes = stackalloc int[type.ElementTypesIntl?.Count ?? 0];
        main.ElementTypes = elementTypes;
        main.Init(type, out string? elementKeyword, this);
        main.FlipArrayGroups();
        main.LoadRefType(refMode, this);

        // todo delegate support

        int length = main.Length;
        if (elementKeyword != null)
        {
            length += elementKeyword.Length;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[length];
#else
        char* output = stackalloc char[length];
#endif
        int index = 0;
        WritePreDimensionsAndOrdering(in main, output, ref index);

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

        WritePostDimensionsAndOrdering(in main, output, ref index);

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }


    /// <summary>
    /// Get the length of the by-ref type to the output.
    /// </summary>
    protected int GetRefTypeLength(ByRefTypeMode mode)
    {
        return mode switch
        {
            ByRefTypeMode.Ref => 4,
            ByRefTypeMode.In => 3,
            ByRefTypeMode.RefReadOnly => 13,
            ByRefTypeMode.Out => 4,
            ByRefTypeMode.ScopedRef => 11,
            ByRefTypeMode.ScopedIn => 10,
            ByRefTypeMode.ScopedRefReadOnly => 20,
            _ => 0
        };
    }

    /// <summary>
    /// Write the passed by-ref type to the output, incrimenting index.
    /// </summary>
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    protected void WriteRefType(ByRefTypeMode mode, ref int index, Span<char> output)
#else
    protected unsafe void WriteRefType(ByRefTypeMode mode, ref int index, char* output)
#endif
    {
        switch (mode)
        {
            case ByRefTypeMode.Ref:
                WriteKeyword(LitRef, ref index, output, spaceSuffix: true);
                break;
            case ByRefTypeMode.In:
                WriteKeyword(LitIn, ref index, output, spaceSuffix: true);
                break;
            case ByRefTypeMode.RefReadOnly:
                WriteKeyword(LitRef, ref index, output, spaceSuffix: true);
                WriteKeyword(LitReadonly, ref index, output, spaceSuffix: true);
                break;
            case ByRefTypeMode.Out:
                WriteKeyword(LitOut, ref index, output, spaceSuffix: true);
                break;
            case ByRefTypeMode.ScopedRef:
                WriteKeyword(LitScoped, ref index, output, spaceSuffix: true);
                WriteKeyword(LitRef, ref index, output, spaceSuffix: true);
                break;
            case ByRefTypeMode.ScopedIn:
                WriteKeyword(LitScoped, ref index, output, spaceSuffix: true);
                WriteKeyword(LitIn, ref index, output, spaceSuffix: true);
                break;
            case ByRefTypeMode.ScopedRefReadOnly:
                WriteKeyword(LitScoped, ref index, output, spaceSuffix: true);
                WriteKeyword(LitRef, ref index, output, spaceSuffix: true);
                WriteKeyword(LitReadonly, ref index, output, spaceSuffix: true);
                break;
        }
    }
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    private void FormatType(Type type, in TypeMetaInfo metaInfo, string? elementKeyword, Span<char> output, ref int index)
#else
    private unsafe void FormatType(Type type, in TypeMetaInfo metaInfo, string? elementKeyword, char* output, ref int index)
#endif
    {
        WritePreDimensionsAndOrdering(in metaInfo, output, ref index);
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

        WritePostDimensionsAndOrdering(in metaInfo, output, ref index);
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
    /// Write parameter to buffer.
    /// </summary>
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    protected virtual void WriteParameter(ParameterInfo parameter, Span<char> output, ref int index, in TypeMetaInfo paramMetaInfo, bool isExtensionThisParameter = false)
#else
    protected virtual unsafe void WriteParameter(ParameterInfo parameter, char* output, ref int index, in TypeMetaInfo paramMetaInfo, bool isExtensionThisParameter = false)
#endif
    {
        Type type = parameter.ParameterType;
        string? parameterElementKeyword = null;
        if (paramMetaInfo.ElementTypesLength > 0)
        {
            for (Type? elemType = type.GetElementType(); elemType != null; elemType = elemType.GetElementType())
                type = elemType;
            parameterElementKeyword = GetTypeKeyword(type);
        }

        WriteParameter(parameter, type, parameterElementKeyword, output, ref index, in paramMetaInfo, isExtensionThisParameter);
    }

    /// <summary>
    /// Write parameter to buffer.
    /// </summary>
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    protected virtual void WriteParameter(ParameterInfo parameter, Type parameterType, string? parameterElementKeyword, Span<char> output, ref int index, in TypeMetaInfo paramMetaInfo, bool isExtensionThisParameter = false)
#else
    protected virtual unsafe void WriteParameter(ParameterInfo parameter, Type parameterType, string? parameterElementKeyword, char* output, ref int index, in TypeMetaInfo paramMetaInfo, bool isExtensionThisParameter = false)
#endif
    {
        string? name = parameter.Name;
        if (isExtensionThisParameter)
        {
            WriteKeyword(LitThis, ref index, output, spaceSuffix: true);
        }
        if (paramMetaInfo.IsParams)
        {
            WriteKeyword(LitParams, ref index, output, spaceSuffix: true);
        }

        FormatType(parameterType, in paramMetaInfo, parameterElementKeyword, output, ref index);

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
    /// Write parameter to buffer.
    /// </summary>
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    protected virtual void WriteParameter(ref MethodParameterDefinition parameter, IMemberDefinition member, Type? parameterType, string? parameterElementKeyword, Span<char> output, ref int index, in TypeMetaInfo paramMetaInfo, bool isExtensionThisParameter = false)
#else
    protected virtual unsafe void WriteParameter(ref MethodParameterDefinition parameter, IMemberDefinition member, Type? parameterType, string? parameterElementKeyword, char* output, ref int index, in TypeMetaInfo paramMetaInfo, bool isExtensionThisParameter = false)
#endif
    {
        string? name = parameter.Name;
        if (isExtensionThisParameter)
        {
            WriteKeyword(LitThis, ref index, output, spaceSuffix: true);
        }
        if (paramMetaInfo.IsParams)
        {
            WriteKeyword(LitParams, ref index, output, spaceSuffix: true);
        }

        bool wroteType = true;
        if (parameterType != null)
            FormatType(parameterType, in paramMetaInfo, parameterElementKeyword, output, ref index);
        else if (member is MethodDefinition { GenericDefinitions: not null } method && parameter.GenericTypeIndex >= 0 && parameter.GenericTypeIndex < method.GenericDefinitions.Count)
        {
            WritePreDimensionsAndOrdering(in paramMetaInfo, output, ref index);
            parameterElementKeyword = method.GenericDefinitions[parameter.GenericTypeIndex];
            if (parameterElementKeyword != null)
            {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
                parameterElementKeyword.AsSpan().CopyTo(output[index..]);
#else
                for (int i = 0; i < parameterElementKeyword.Length; ++i)
                    output[index + i] = parameterElementKeyword[i];
#endif
                index += parameterElementKeyword.Length;
            }

            WritePostDimensionsAndOrdering(in paramMetaInfo, output, ref index);
        }
        else wroteType = false;

        if (string.IsNullOrEmpty(name))
            return;

        if (wroteType)
        {
            output[index] = ' ';
            ++index;
        }
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        name.AsSpan().CopyTo(output[index..]);
#else
        for (int i = 0; i < name!.Length; ++i)
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

        MemberVisibility vis = _accessor.GetVisibility(method);
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
    /// Writes all necessary prefix element types to the output buffer.
    /// </summary>
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    protected virtual unsafe void WritePreDimensionsAndOrdering(in TypeMetaInfo meta, Span<char> output, ref int index)
#else
    protected virtual unsafe void WritePreDimensionsAndOrdering(in TypeMetaInfo meta, char* output, ref int index)
#endif
    {
        for (int i = 0; i < meta.ElementTypesLength; ++i)
        {
            int dim = meta.ElementTypes[meta.ElementTypesLength - i - 1];
            if (dim >= -1)
                continue;

            ByRefTypeMode mode = (ByRefTypeMode)(-(dim + 1));
            WriteRefType(mode, ref index, output);
        }
    }

    /// <summary>
    /// Writes all necessary suffix element types to the output buffer.
    /// </summary>
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    protected virtual unsafe void WritePostDimensionsAndOrdering(in TypeMetaInfo meta, Span<char> output, ref int index)
#else
    protected virtual unsafe void WritePostDimensionsAndOrdering(in TypeMetaInfo meta, char* output, ref int index)
#endif
    {
        for (int i = 0; i < meta.ElementTypesLength; ++i)
        {
            int dim = meta.ElementTypes[meta.ElementTypesLength - i - 1];
            if (dim > 0)
            {
                output[index] = '[';
                for (int d = 1; d < dim; ++d)
                {
                    output[index + d] = ',';
                }
                output[index + dim] = ']';
                index += dim + 1;
            }
            else if (dim == -1)
            {
                output[index] = '*';
                ++index;
            }
        }
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(MethodBase method, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = _accessor.GetVisibility(method);
        ParameterInfo[] parameters = method.GetParameters();
        Type[] genericTypeParameters = method.IsGenericMethod ? method.GetGenericArguments() : Type.EmptyTypes;
        TypeMetaInfo* paramInfo = stackalloc TypeMetaInfo[parameters.Length];
        TypeMetaInfo* genParamInfo = stackalloc TypeMetaInfo[method.IsGenericMethodDefinition ? 0 : genericTypeParameters.Length];
        string methodName = method.Name;
        int len = 0;
        bool isCtor = method is ConstructorInfo;
        bool isReadOnly = _accessor.IsReadOnly(method);

        if (includeDefinitionKeywords && (!isCtor || !method.IsStatic))
            len += GetVisibilityLength(vis) + 1;
        
        Type? declType = method.DeclaringType;
        Type? originalDeclType = declType;

        if (!isReadOnly && declType is { IsValueType: true } && _accessor.IsReadOnly(declType))
            isReadOnly = true;

        bool isExtensionMethod = !isCtor && method.IsStatic && declType != null && _accessor.GetIsStatic(declType) && _accessor.IsDefinedSafe<ExtensionAttribute>(method);

        if (method.IsStatic)
            len += 7;

        if (includeDefinitionKeywords && isReadOnly)
            len += 9;

        Type? returnType = method is MethodInfo returnableMethod ? returnableMethod.ReturnType : declType;
        Type? originalReturnType = returnType;

        TypeMetaInfo methodTypeMeta = default;
        TypeMetaInfo declTypeMeta = default;
        string? declTypeKeyword = null;
        string? methodTypeKeyword = null;

        if (returnType != null)
        {
            methodTypeMeta.Init(ref returnType, out methodTypeKeyword, this);
            int* returnTypeElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
            methodTypeMeta.ElementTypes = returnTypeElementStack;
            methodTypeMeta.SetupDimensionsAndOrdering(originalReturnType!);
            methodTypeMeta.LoadReturnType(method, this);
            len += GetNonDeclaritiveTypeNameLengthNoSetup(returnType, ref methodTypeMeta, methodTypeKeyword) + (!isCtor ? 1 : 0) * (methodName.Length + 1);
        }

        if (declType != null && !isCtor)
        {
            declTypeMeta.Init(ref declType, out declTypeKeyword, this);
            int* declTypeElementStack = stackalloc int[declTypeMeta.ElementTypesLength];
            declTypeMeta.ElementTypes = declTypeElementStack;
            len += GetNonDeclaritiveTypeNameLength(declType, originalDeclType!, ref declTypeMeta, declTypeKeyword) + 1;
        }

        if (genericTypeParameters.Length > 0)
        {
            len += 2 + (genericTypeParameters.Length - 1) * 2;
            if (method.IsGenericMethodDefinition)
            {
                for (int i = 0; i < genericTypeParameters.Length; ++i)
                {
                    len += genericTypeParameters[i].Name.Length;
                }
            }
            else
            {
                for (int i = 0; i < genericTypeParameters.Length; ++i)
                {
                    ref TypeMetaInfo genParamMetaInfo = ref genParamInfo[i];
                    Type genParameterType = genericTypeParameters[i];
                    Type originalGenParamType = genParameterType;

                    genParamMetaInfo.Init(ref genParameterType, out string? elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* genParamElementStack = stackalloc int[genParamMetaInfo.ElementTypesLength];
                    genParamMetaInfo.ElementTypes = genParamElementStack;
                    len += GetNonDeclaritiveTypeNameLength(genParameterType, originalGenParamType, ref genParamMetaInfo, elementKeyword);
                }
            }
        }

        len += 2 + (Math.Max(parameters.Length, 1) - 1) * 2;
        for (int i = 0; i < parameters.Length; ++i)
        {
            ParameterInfo parameter = parameters[i];
            ref TypeMetaInfo paramMetaInfo = ref paramInfo[i];
            Type parameterType = parameter.ParameterType;
            Type originalParameterType = parameterType;
            string? name = parameter.Name;

            if (isExtensionMethod && i == 0)
                len += 5;

            if (!string.IsNullOrEmpty(name))
                len += name.Length + 1;

            paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
            // ReSharper disable once StackAllocInsideLoop
            int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
            paramMetaInfo.ElementTypes = parameterElementStack;
            paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
            paramMetaInfo.LoadParameter(parameter, this);
            len += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[len];
#else
        char* output = stackalloc char[len];
#endif

        int index = 0;
        if (includeDefinitionKeywords && (!isCtor || !method.IsStatic))
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }

        if (method.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        if (includeDefinitionKeywords && isReadOnly)
        {
            WriteKeyword(LitReadonly, ref index, output, spaceSuffix: true);
        }

        if (returnType != null)
        {
            FormatType(returnType, in methodTypeMeta, methodTypeKeyword, output, ref index);
            if (!isCtor)
            {
                output[index] = ' ';
                ++index;
            }
        }
        if (declType != null && !isCtor)
        {
            FormatType(declType, in declTypeMeta, declTypeKeyword, output, ref index);
            output[index] = '.';
            ++index;
        }

        if (!isCtor)
        {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
            methodName.AsSpan().CopyTo(output[index..]);
#else
            for (int i = 0; i < methodName.Length; ++i)
            {
                output[index + i] = methodName[i];
            }
#endif

            index += methodName.Length;
        }


        if (genericTypeParameters.Length > 0)
        {
            output[index] = '<';
            ++index;
            if (method.IsGenericMethodDefinition)
            {
                for (int i = 0; i < genericTypeParameters.Length; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    string name = genericTypeParameters[i].Name;
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
                    name.AsSpan().CopyTo(output[index..]);
#else
                    for (int j = 0; j < name.Length; ++j)
                    {
                        output[index + j] = name[j];
                    }
#endif
                    index += name.Length;
                }
            }
            else
            {
                for (int i = 0; i < genericTypeParameters.Length; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    ref readonly TypeMetaInfo genParamMetaInfo = ref genParamInfo[i];
                    Type genParameterType = genericTypeParameters[i];
                    string? elementKeyword = null;
                    if (genParamMetaInfo.ElementTypesLength > 0)
                    {
                        for (Type? elemType = genParameterType.GetElementType(); elemType != null; elemType = elemType.GetElementType())
                            genParameterType = elemType;
                        elementKeyword = GetTypeKeyword(genParameterType);
                    }

                    FormatType(genParameterType, in genParamMetaInfo, elementKeyword, output, ref index);
                }
            }
            output[index] = '>';
            ++index;
        }
        output[index] = '(';
        ++index;
        for (int i = 0; i < parameters.Length; ++i)
        {
            if (i != 0)
            {
                output[index] = ',';
                output[index + 1] = ' ';
                index += 2;
            }

            WriteParameter(parameters[i], output, ref index, in paramInfo[i], isExtensionThisParameter: isExtensionMethod && i == 0);
        }
        output[index] = ')';
        ++index;

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(MethodDefinition method)
    {
        List<MethodParameterDefinition>? parameters = method.ParametersIntl;
        bool isDef = method.GenDefsIntl != null && (method.GenValsIntl == null || method.GenDefsIntl.Count > method.GenValsIntl.Count) && method.GenDefsIntl.Count > 0;
        bool isGenVal = !isDef && method.GenValsIntl is { Count: > 0 };
        TypeMetaInfo* paramInfo = stackalloc TypeMetaInfo[parameters?.Count ?? 0];
        TypeMetaInfo* genParamInfo = stackalloc TypeMetaInfo[!isGenVal ? 0 : method.GenValsIntl!.Count];
        string? methodName = method.Name;
        int len = 0;
        bool isCtor = method.IsConstructor;

        Type? declType = method.DeclaringType;
        Type? originalDeclType = declType;

        bool isExtensionMethod = !isCtor && method.IsStatic && declType != null && _accessor.GetIsStatic(declType) && method is { IsStatic: true, IsExtensionMethod: true };

        if (method.IsStatic)
            len += 7;

        Type? returnType = method.ReturnType;
        Type? originalReturnType = returnType;

        TypeMetaInfo methodTypeMeta = default;
        TypeMetaInfo declTypeMeta = default;
        string? declTypeKeyword = null;
        string? methodTypeKeyword = null;

        if (returnType != null)
        {
            methodTypeMeta.Init(ref returnType, out methodTypeKeyword, this);
            int* returnTypeElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
            methodTypeMeta.ElementTypes = returnTypeElementStack;
            methodTypeMeta.SetupDimensionsAndOrdering(originalReturnType!);
            methodTypeMeta.LoadReturnType(method, this);
            len += GetNonDeclaritiveTypeNameLengthNoSetup(returnType, ref methodTypeMeta, methodTypeKeyword);
            if (!isCtor && (methodName != null || declType != null))
            {
                ++len;
            }
        }
        else if (isDef && method.ReturnTypeGenericIndex >= 0 && method.ReturnTypeGenericIndex < method.GenDefsIntl!.Count)
        {
            methodTypeMeta.InitReturnTypeMeta(method, out methodTypeKeyword, this);
            // ReSharper disable once StackAllocInsideLoop
            int* parameterElementStack = stackalloc int[methodTypeMeta.ElementTypesLength];
            methodTypeMeta.ElementTypes = parameterElementStack;
            methodTypeMeta.FlipArrayGroups();
            methodTypeMeta.LoadReturnType(method, this);
            len += methodTypeMeta.Length;
        }

        if (declType != null && !isCtor)
        {
            declTypeMeta.Init(ref declType, out declTypeKeyword, this);
            int* declTypeElementStack = stackalloc int[declTypeMeta.ElementTypesLength];
            declTypeMeta.ElementTypes = declTypeElementStack;
            len += GetNonDeclaritiveTypeNameLength(declType, originalDeclType!, ref declTypeMeta, declTypeKeyword) + 1;
            if (methodName != null)
            {
                ++len;
            }
        }

        if (!isCtor && methodName != null)
            len += methodName.Length;

        if (isDef || isGenVal)
        {
            if (isDef)
            {
                len += 2 + (method.GenDefsIntl!.Count - 1) * 2;
                for (int i = 0; i < method.GenDefsIntl!.Count; ++i)
                {
                    string? name = method.GenDefsIntl![i];
                    if (name != null)
                        len += name.Length;
                }
            }
            else
            {
                len += 2 + (method.GenValsIntl!.Count - 1) * 2;
                for (int i = 0; i < method.GenValsIntl.Count; ++i)
                {
                    ref TypeMetaInfo genParamMetaInfo = ref genParamInfo[i];
                    Type? genParameterType = method.GenValsIntl[i];
                    if (genParameterType == null)
                        continue;

                    Type originalGenParamType = genParameterType;

                    genParamMetaInfo.Init(ref genParameterType, out string? elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* genParamElementStack = stackalloc int[genParamMetaInfo.ElementTypesLength];
                    genParamMetaInfo.ElementTypes = genParamElementStack;
                    len += GetNonDeclaritiveTypeNameLength(genParameterType, originalGenParamType, ref genParamMetaInfo, elementKeyword);
                }
            }
        }

        if (parameters != null)
        {
            len += 2 + (Math.Max(parameters.Count, 1) - 1) * 2;
            for (int i = 0; i < parameters.Count; ++i)
            {
                MethodParameterDefinition parameter = parameters[i];
                ref TypeMetaInfo paramMetaInfo = ref paramInfo[i];
                Type? parameterType = parameter.Type;

                if (parameterType == null)
                {
                    paramMetaInfo.Init(ref parameter, method, out _, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                    paramMetaInfo.ElementTypes = parameterElementStack;
                    paramMetaInfo.FlipArrayGroups();
                    paramMetaInfo.LoadParameter(ref parameter, this);
                    len += paramMetaInfo.Length;
                }
                else
                {
                    Type originalParameterType = parameterType;
                    paramMetaInfo.Init(ref parameterType, out string? elementKeyword, this);
                    // ReSharper disable once StackAllocInsideLoop
                    int* parameterElementStack = stackalloc int[paramMetaInfo.ElementTypesLength];
                    paramMetaInfo.ElementTypes = parameterElementStack;
                    paramMetaInfo.SetupDimensionsAndOrdering(originalParameterType);
                    paramMetaInfo.LoadParameter(ref parameter, this);
                    len += GetNonDeclaritiveTypeNameLengthNoSetup(parameterType, ref paramMetaInfo, elementKeyword);
                }

                string? name = parameter.Name;

                if (isExtensionMethod && i == 0)
                    len += 5;

                if (!string.IsNullOrEmpty(name))
                    len += name!.Length + 1;
            }
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[len];
#else
        char* output = stackalloc char[len];
#endif

        int index = 0;

        if (method.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: true);
        }

        if (returnType != null || methodTypeKeyword != null)
        {
            FormatType(returnType!, in methodTypeMeta, methodTypeKeyword, output, ref index);
            if (!isCtor && (declType != null || methodName != null))
            {
                output[index] = ' ';
                ++index;
            }
        }

        if (declType != null && !isCtor)
        {
            FormatType(declType, in declTypeMeta, declTypeKeyword, output, ref index);
            if (methodName != null)
            {
                output[index] = '.';
                ++index;
            }
        }

        if (!isCtor && methodName != null)
        {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
            methodName.AsSpan().CopyTo(output[index..]);
#else
            for (int i = 0; i < methodName.Length; ++i)
            {
                output[index + i] = methodName[i];
            }
#endif

            index += methodName.Length;
        }


        if (isDef || isGenVal)
        {
            output[index] = '<';
            ++index;
            if (isDef)
            {
                for (int i = 0; i < method.GenDefsIntl!.Count; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    string name = method.GenDefsIntl![i];
                    if (name == null)
                        continue;

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
                    name.AsSpan().CopyTo(output[index..]);
#else
                    for (int j = 0; j < name.Length; ++j)
                    {
                        output[index + j] = name[j];
                    }
#endif
                    index += name.Length;
                }
            }
            else
            {
                for (int i = 0; i < method.GenValsIntl!.Count; ++i)
                {
                    if (i != 0)
                    {
                        output[index] = ',';
                        output[index + 1] = ' ';
                        index += 2;
                    }

                    ref readonly TypeMetaInfo genParamMetaInfo = ref genParamInfo[i];
                    Type? genParameterType = method.GenValsIntl![i];
                    if (genParameterType == null)
                        continue;

                    string? elementKeyword = null;
                    if (genParamMetaInfo.ElementTypesLength > 0)
                    {
                        for (Type? elemType = genParameterType.GetElementType(); elemType != null; elemType = elemType.GetElementType())
                            genParameterType = elemType;
                        elementKeyword = GetTypeKeyword(genParameterType);
                    }

                    FormatType(genParameterType, in genParamMetaInfo, elementKeyword, output, ref index);
                }
            }
            output[index] = '>';
            ++index;
        }

        if (parameters != null)
        {
            output[index] = '(';
            ++index;
            for (int i = 0; i < parameters.Count; ++i)
            {
                if (i != 0)
                {
                    output[index] = ',';
                    output[index + 1] = ' ';
                    index += 2;
                }

                MethodParameterDefinition parameter = parameters[i];

                ref readonly TypeMetaInfo paramMetaInfo = ref paramInfo[i];
                Type? parameterType = parameter.Type;
                string? elementKeyword = null;
                if (parameterType != null)
                {
                    if (paramMetaInfo.ElementTypesLength > 0)
                    {
                        for (Type? elemType = parameterType.GetElementType(); elemType != null; elemType = elemType.GetElementType())
                            parameterType = elemType;
                        elementKeyword = GetTypeKeyword(parameterType);
                    }
                }

                WriteParameter(ref parameter, method, parameterType, elementKeyword, output, ref index, in paramMetaInfo, isExtensionThisParameter: i == 0 && isExtensionMethod);
            }
            output[index] = ')';
            ++index;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(FieldInfo field, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = _accessor.GetVisibility(field);
        string fieldName = field.Name;
        int len = 0;
        if (includeDefinitionKeywords)
        {
            len += GetVisibilityLength(vis) + 1;
        }

        if (field.IsLiteral)
            len += 6;
        else if (field.IsStatic)
            len += 7;

        if (includeDefinitionKeywords && field is { IsLiteral: false, IsInitOnly: true })
            len += 9;

        Type fieldType = field.FieldType;
        Type originalFieldType = fieldType;
        Type? declType = field.DeclaringType;
        Type? originalDeclType = declType;

        TypeMetaInfo fieldTypeMeta = default;
        TypeMetaInfo declTypeMeta = default;
        string? declTypeKeyword = null;
        fieldTypeMeta.Init(ref fieldType, out string? fieldTypeKeyword, this);
        int* fieldTypeElementStack = stackalloc int[fieldTypeMeta.ElementTypesLength];
        fieldTypeMeta.ElementTypes = fieldTypeElementStack;
        len += GetNonDeclaritiveTypeNameLength(fieldType, originalFieldType, ref fieldTypeMeta, fieldTypeKeyword) + 1 + fieldName.Length;

        if (declType != null)
        {
            declTypeMeta.Init(ref declType, out declTypeKeyword, this);
            int* declTypeElementStack = stackalloc int[declTypeMeta.ElementTypesLength];
            declTypeMeta.ElementTypes = declTypeElementStack;
            len += GetNonDeclaritiveTypeNameLength(declType, originalDeclType!, ref declTypeMeta, declTypeKeyword) + 1;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[len];
#else
        char* output = stackalloc char[len];
#endif

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

        FormatType(fieldType, in fieldTypeMeta, fieldTypeKeyword, output, ref index);
        output[index] = ' ';
        ++index;
        if (declType != null)
        {
            FormatType(declType, in declTypeMeta, declTypeKeyword, output, ref index);
            output[index] = '.';
            ++index;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        fieldName.AsSpan().CopyTo(output[index..]);
#else
        for (int i = 0; i < fieldName.Length; ++i)
        {
            output[index + i] = fieldName[i];
        }
#endif
        index += fieldName.Length;

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }

    /// <inheritdoc/>
    public virtual unsafe string Format(FieldDefinition field)
    {
        string? fieldName = field.Name;
        bool nameIsNull = string.IsNullOrEmpty(fieldName);
        int len = 0;

        if (field.IsConstant)
        {
            len += 5;
            if (field.FieldType != null || field.DeclaringType != null || !nameIsNull)
                ++len;
        }
        else if (field.IsStatic)
        {
            len += 6;
            if (field.FieldType != null || field.DeclaringType != null || !nameIsNull)
                ++len;
        }
        
        Type? fieldType = field.FieldType;
        Type? originalFieldType = fieldType;
        Type? declType = field.DeclaringType;
        Type? originalDeclType = declType;

        TypeMetaInfo fieldTypeMeta = default;
        TypeMetaInfo declTypeMeta = default;
        string? declTypeKeyword = null;
        string? fieldTypeKeyword = null;

        if (fieldType != null)
        {
            fieldTypeMeta.Init(ref fieldType, out fieldTypeKeyword, this);
            int* fieldTypeElementStack = stackalloc int[fieldTypeMeta.ElementTypesLength];
            fieldTypeMeta.ElementTypes = fieldTypeElementStack;
            len += GetNonDeclaritiveTypeNameLength(fieldType, originalFieldType!, ref fieldTypeMeta, fieldTypeKeyword);
            if (!nameIsNull || declType != null)
                ++len;
        }

        if (declType != null)
        {
            declTypeMeta.Init(ref declType, out declTypeKeyword, this);
            int* declTypeElementStack = stackalloc int[declTypeMeta.ElementTypesLength];
            declTypeMeta.ElementTypes = declTypeElementStack;
            len += GetNonDeclaritiveTypeNameLength(declType, originalDeclType!, ref declTypeMeta, declTypeKeyword);
            if (!nameIsNull)
                ++len;
        }

        if (!nameIsNull)
        {
            len += fieldName!.Length;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        Span<char> output = stackalloc char[len];
#else
        char* output = stackalloc char[len];
#endif

        int index = 0;

        if (field.IsConstant)
        {
            WriteKeyword(LitConst, ref index, output, spaceSuffix: field.FieldType != null || field.DeclaringType != null || !nameIsNull);
        }
        else if (field.IsStatic)
        {
            WriteKeyword(LitStatic, ref index, output, spaceSuffix: field.FieldType != null || field.DeclaringType != null || !nameIsNull);
        }

        if (fieldType != null)
        {
            FormatType(fieldType, in fieldTypeMeta, fieldTypeKeyword, output, ref index);
            if (!nameIsNull || declType != null)
            {
                output[index] = ' ';
                ++index;
            }
        }

        if (declType != null)
        {
            FormatType(declType, in declTypeMeta, declTypeKeyword, output, ref index);
            if (!nameIsNull)
            {
                output[index] = '.';
                ++index;
            }
        }

        if (!nameIsNull)
        {
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
            fieldName!.AsSpan().CopyTo(output[index..]);
#else
            for (int i = 0; i < fieldName!.Length; ++i)
            {
                output[index + i] = fieldName[i];
            }
#endif
            index += fieldName.Length;
        }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        return new string(output[..index]);
#else
        return new string(output, 0, index);
#endif
    }

    /// <summary>
    /// Represents info about a type for writing.
    /// </summary>
    protected ref struct TypeMetaInfo
    {
        /// <summary>
        /// Required size of <see cref="ElementTypes"/>.
        /// </summary>
        public int ElementTypesLength;

        /// <summary>
        /// Pointer to the first element in a list of array dimensions (positive nums), pointers (-1), or refs (-ByRefTypeMode - 1).
        /// <para>
        /// Element meanings:
        /// <code>
        /// <br/>* dim = -1 = pointer
        /// <br/>* dim &gt; 0  = array of rank {n}
        /// <br/>* dim &lt; -1 = -(int){ByRefTypeMode} - 1
        /// </code>
        /// </para>
        /// </summary>
        public unsafe int* ElementTypes;

        /// <summary>
        /// Total string length of all the different ref types.
        /// </summary>
        public int Length;

        /// <summary>
        /// If this type is a params array.
        /// </summary>
        public bool IsParams;


        /// <summary>
        /// Initialize the values with the given parameter.
        /// </summary>
        public unsafe void LoadParameter(ParameterInfo parameter, DefaultOpCodeFormatter formatter)
        {
            Type parameterType = parameter.ParameterType;
            IsParams = parameterType.IsArray && formatter._accessor.IsDefinedSafe<ParamArrayAttribute>(parameter);
            if (IsParams)
                Length += 7;

            if (!parameterType.IsByRef)
                return;

            bool isScoped = !parameter.IsOut && formatter._accessor.IsCompilerAttributeDefinedSafe(parameter, "ScopedRefAttribute");

            if (parameter.IsIn)
            {
                ByRefTypeMode mode = isScoped ? ByRefTypeMode.ScopedIn : ByRefTypeMode.In;
                Length += formatter.GetRefTypeLength(mode) - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)mode - 1;
            }
            else if (parameter.IsOut)
            {
                Length += 4 - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)ByRefTypeMode.Out - 1;
            }
            else if (isScoped)
            {
                Length += 11 - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)ByRefTypeMode.ScopedRef - 1;
            }
        }
        /// <summary>
        /// Initialize the values with the given parameter.
        /// </summary>
        public unsafe void LoadParameter(ref MethodParameterDefinition parameter, DefaultOpCodeFormatter formatter)
        {
            Type? parameterType = parameter.Type;
            if (parameterType == null)
            {
                for (int i = 0; i < ElementTypesLength; ++i)
                    ElementTypes[i] = parameter.GenericTypeElementTypes![i];

                IsParams = parameter is { IsParams: true, GenericTypeElementTypesLength: > 0 } && parameter.GenericTypeElementTypes![0] > 0;
                if (IsParams)
                    Length += 7;

                if (parameter is not { GenericTypeElementTypesLength: > 0 } || parameter.GenericTypeElementTypes![0] >= -1)
                    return;
            }
            else
            {
                IsParams = parameter.IsParams && parameterType.IsArray;
                if (IsParams)
                    Length += 7;

                if (!parameterType.IsByRef)
                    return;
            }

            bool isScoped = parameter.ByRefMode is ByRefTypeMode.ScopedIn or ByRefTypeMode.ScopedRefReadOnly or ByRefTypeMode.ScopedRef;

            if (parameter.ByRefMode is ByRefTypeMode.In or ByRefTypeMode.ScopedIn or ByRefTypeMode.RefReadOnly or ByRefTypeMode.ScopedRefReadOnly)
            {
                ByRefTypeMode mode = isScoped ? ByRefTypeMode.ScopedIn : ByRefTypeMode.In;
                Length += formatter.GetRefTypeLength(mode) - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)mode - 1;
            }
            else if (parameter.ByRefMode == ByRefTypeMode.Out)
            {
                Length += 4 - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)ByRefTypeMode.Out - 1;
            }
            else if (isScoped)
            {
                Length += 11 - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)ByRefTypeMode.ScopedRef - 1;
            }
        }

        /// <summary>
        /// Initialize the values with the given by-ref type.
        /// </summary>
        public unsafe void LoadRefType(ByRefTypeMode mode, DefaultOpCodeFormatter formatter)
        {
            if (ElementTypesLength == 0 || ElementTypes[0] >= -1)
                return;
            if (mode == ByRefTypeMode.Ignore)
            {
                Length -= formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = 0;
            }
            else
            {
                Length += formatter.GetRefTypeLength(mode) - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)mode - 1;
            }
        }

        /// <summary>
        /// Initialize the values with the given method return value.
        /// </summary>
        public unsafe void LoadReturnType(MethodBase method, DefaultOpCodeFormatter formatter)
        {
            if (method is MethodInfo returnableMethod
                && ElementTypesLength > 0
                && returnableMethod.ReturnType.IsByRef
                && returnableMethod.ReturnParameter != null
                && formatter._accessor.IsReadOnly(returnableMethod.ReturnParameter))
            {
                Length += 13 - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)ByRefTypeMode.RefReadOnly - 1;
            }
        }

        /// <summary>
        /// Initialize the values with the given method return value.
        /// </summary>
        public unsafe void LoadReturnType(MethodDefinition method, DefaultOpCodeFormatter formatter)
        {
            if (method.ReturnType != null)
            {
                if (ElementTypesLength > 0 && method.ReturnType.IsByRef && method.ReturnRefTypeMode is ByRefTypeMode.RefReadOnly or ByRefTypeMode.In)
                {
                    Length += 13 - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                    ElementTypes[0] = -(int)ByRefTypeMode.RefReadOnly - 1;
                }
            }
            else
            {
                for (int i = 0; i < ElementTypesLength; ++i)
                    ElementTypes[i] = method.ReturnTypeGenericTypeElementTypes![i];

                if (ElementTypesLength > 0
                    && method is { ReturnTypeGenericTypeElementTypesLength: > 0 } && method.ReturnTypeGenericTypeElementTypes![0] < -1
                    && method.ReturnRefTypeMode is ByRefTypeMode.RefReadOnly or ByRefTypeMode.In)
                {
                    Length += 13 - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                    ElementTypes[0] = -(int)ByRefTypeMode.RefReadOnly - 1;
                }
            }
        }

        /// <summary>
        /// Initialize the array ranks.
        /// </summary>
        public unsafe void SetupDimensionsAndOrdering(Type originalType)
        {
            Type? elementType = originalType.GetElementType();
            if (elementType == null)
                return;

            int index = -1;
            for (Type? nextElementType = originalType; nextElementType != null; nextElementType = nextElementType.GetElementType())
            {
                if (nextElementType is { IsByRef: false, IsPointer: false, IsArray: false })
                    break;

                if (nextElementType.IsPointer)
                {
                    ElementTypes[++index] = -1;
                }
                else if (nextElementType.IsByRef)
                {
                    ElementTypes[++index] = -(int)ByRefTypeMode.Ref - 1;
                }
                else
                {
                    ElementTypes[++index] = nextElementType.GetArrayRank();
                }
            }

            FlipArrayGroups();
        }

        /// <summary>
        /// Flips groups of array elements to correspond with their flipped definition notation in C#.
        /// </summary>
        public unsafe void FlipArrayGroups()
        {
            int startIndex = -1;
            for (int i = 0; i < ElementTypesLength; ++i)
            {
                int dim = ElementTypes[i];
                if (dim <= 0)
                {
                    if (startIndex != -1)
                    {
                        ProcessArrayGroup(ElementTypes, startIndex, i - startIndex);
                        startIndex = -1;
                    }
                    continue;
                }

                if (startIndex == -1)
                    startIndex = i;
            }

            if (startIndex != -1)
                ProcessArrayGroup(ElementTypes, startIndex, ElementTypesLength - startIndex);

            return;

            static void ProcessArrayGroup(int* ptr, int startIndex, int length)
            {
                for (int i = 0; i < length / 2; ++i)
                {
                    int* index1 = ptr + (startIndex + i);
                    int* index2 = ptr + (startIndex + (length - i - 1));

                    int tmp = *index1;
                    *index1 = *index2;
                    *index2 = tmp;
                }
            }
        }

        /// <summary>
        /// Initialize the values with the given type.
        /// </summary>
        public void Init(ref MethodParameterDefinition parameter, MethodDefinition method, out string? elementKeyword, DefaultOpCodeFormatter formatter)
        {
            ElementTypesLength = parameter.GenericTypeElementTypesLength;
            Length = 0;
            IsParams = false;
            elementKeyword =
                method.GenDefsIntl == null || parameter.GenericTypeIndex < 0 ||
                parameter.GenericTypeIndex >= method.GenDefsIntl.Count
                    ? null
                    : method.GenDefsIntl[parameter.GenericTypeIndex];
            Length = elementKeyword?.Length ?? 0;
            if (parameter.GenericTypeElementTypes == null)
                return;
            for (int i = 0; i < ElementTypesLength; i++)
            {
                switch (parameter.GenericTypeElementTypes[i])
                {
                    case -1:
                        ++Length;
                        break;
                    case < -1:
                        Length += formatter.GetRefTypeLength((ByRefTypeMode)(-parameter.GenericTypeElementTypes[i] - 1));
                        break;
                    default:
                        Length += parameter.GenericTypeElementTypes[i] + 1;
                        break;
                }
            }
        }

        /// <summary>
        /// Initialize the values with the given type.
        /// </summary>
        public void InitReturnTypeMeta(MethodDefinition method, out string? elementKeyword, DefaultOpCodeFormatter formatter)
        {
            ElementTypesLength = method.ReturnTypeGenericTypeElementTypesLength;
            Length = 0;
            IsParams = false;
            elementKeyword =
                method.GenDefsIntl == null || method.ReturnTypeGenericIndex < 0 ||
                method.ReturnTypeGenericIndex >= method.GenDefsIntl.Count
                    ? null
                    : method.GenDefsIntl[method.ReturnTypeGenericIndex];
            Length = elementKeyword?.Length ?? 0;
            if (method.ReturnTypeGenericTypeElementTypes == null)
                return;
            for (int i = 0; i < ElementTypesLength; i++)
            {
                switch (method.ReturnTypeGenericTypeElementTypes[i])
                {
                    case -1:
                        ++Length;
                        break;
                    case < -1:
                        Length += formatter.GetRefTypeLength((ByRefTypeMode)(-method.ReturnTypeGenericTypeElementTypes[i] - 1));
                        break;
                    default:
                        Length += method.ReturnTypeGenericTypeElementTypes[i] + 1;
                        break;
                }
            }
        }

        /// <summary>
        /// Initialize the values with the given type.
        /// </summary>
        public void Init(ref Type type, out string? elementKeyword, DefaultOpCodeFormatter formatter)
        {
            ElementTypesLength = 0;
            Length = 0;
            IsParams = false;

            elementKeyword = formatter.GetTypeKeyword(type);
            if (elementKeyword != null)
                return;

            Type? elementType = type.GetElementType();
            if (elementType == null)
            {
                elementKeyword = null;
                return;
            }

            for (Type? nextElementType = type; nextElementType != null; nextElementType = nextElementType.GetElementType())
            {
                elementType = nextElementType;
                if (nextElementType is { IsByRef: false, IsPointer: false, IsArray: false })
                {
                    if (nextElementType == type)
                    {
                        elementKeyword = null;
                        return;
                    }

                    break;
                }

                ++ElementTypesLength;
                if (nextElementType.IsPointer)
                {
                    ++Length;
                }
                else if (nextElementType.IsByRef)
                {
                    Length += 4;
                }
                else
                {
                    Length += nextElementType.GetArrayRank() + 1;
                }
            }

            type = elementType;
            elementKeyword = formatter.GetTypeKeyword(elementType);
        }

        /// <summary>
        /// Initialize the values with the given type definition.
        /// </summary>
        public unsafe void Init(TypeDefinition type, out string? elementKeyword, DefaultOpCodeFormatter formatter)
        {
            Length = 0;
            IsParams = false;
            elementKeyword = type.Name;

            if (type.ElementTypesIntl is not { Count: > 0 })
            {
                return;
            }


            for (int i = 0; i < type.ElementTypesIntl.Count; ++i)
            {
                int dim = type.ElementTypesIntl[i];
                ElementTypes[i] = dim;
                if (dim == -1)
                {
                    ++Length;
                }
                else if (dim < -1)
                {
                    Length += formatter.GetRefTypeLength((ByRefTypeMode)(-dim - 1));
                }
                else
                {
                    Length += dim + 1;
                }
            }
        }
    }
}
#pragma warning restore CA2014