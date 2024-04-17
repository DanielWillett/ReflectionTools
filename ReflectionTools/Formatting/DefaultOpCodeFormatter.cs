using System;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
                if (!l64.TryFormat(output[index..], out charsWritten, "D0", CultureInfo.InvariantCulture))
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
        string? s = GetTypeKeyword(type);
        if (s != null)
            return s.Length;

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

        MemberVisibility vis = includeDefinitionKeywords ? type.GetVisibility() : default;
        bool isValueType = type.IsValueType;
        bool isAbstract = !isValueType && includeDefinitionKeywords && type is { IsAbstract: true, IsSealed: false, IsInterface: false };
        bool isDelegate = !isValueType && includeDefinitionKeywords && type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
        bool isReadOnly = isValueType && includeDefinitionKeywords && type.IsReadOnly();
        bool isStatic = !isValueType && includeDefinitionKeywords && type.GetIsStatic();
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
                    MethodInfo invokeMethod = Accessor.GetInvokeMethod(type);
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

        MemberVisibility vis = includeDefinitionKeywords ? type.GetVisibility() : default;
        bool isValueType = type.IsValueType;
        bool isAbstract = !isValueType && includeDefinitionKeywords && type is { IsAbstract: true, IsSealed: false, IsInterface: false };
        bool isDelegate = !isValueType && includeDefinitionKeywords && type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
        bool isReadOnly = isValueType && includeDefinitionKeywords && type.IsReadOnly();
        bool isStatic = !isValueType && includeDefinitionKeywords && type.GetIsStatic();
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
                MethodInfo invokeMethod = Accessor.GetInvokeMethod(type);
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
    public virtual unsafe int GetFormatLength(MethodBase method, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = method.GetVisibility();
        ParameterInfo[] parameters = method.GetParameters();
        string methodName = method.Name;
        int len = 0;
        bool isReadOnly = method.IsReadOnly();
        if (includeDefinitionKeywords)
            len += GetVisibilityLength(vis) + 1;

        Type? declType = method.DeclaringType;
        if (!isReadOnly && declType is { IsValueType: true } && declType.IsReadOnly())
            isReadOnly = true;
        bool isCtor = method.IsConstructor;

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

        len += 2 + (Math.Max(parameters.Length, 1) - 1) * 2;
        for (int i = 0; i < parameters.Length; ++i)
        {
            ParameterInfo parameter = parameters[i];
            TypeMetaInfo paramMetaInfo = default;
            Type parameterType = parameter.ParameterType;
            Type originalParamType = parameterType;
            string? name = parameter.Name;

            if (i == 0 && method.IsStatic && declType != null && declType.GetIsStatic() && method.IsDefinedSafe<ExtensionAttribute>())
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
        MemberVisibility vis = method.GetVisibility();
        ParameterInfo[] parameters = method.GetParameters();
        string methodName = method.Name;
        bool isCtor = method.IsConstructor;
        int index = 0;
        if (includeDefinitionKeywords)
        {
            WriteVisibility(vis, ref index, output);
            output[index] = ' ';
            ++index;
        }
        
        bool isReadOnly = method.IsReadOnly();
        Type? declType = method.DeclaringType;
        Type? originalDeclType = declType;

        if (!isReadOnly && declType is { IsValueType: true } && declType.IsReadOnly())
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

        methodName.AsSpan().CopyTo(output[index..]);
        index += methodName.Length;
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
                isExtensionThisParameter: i == 0 && method.IsStatic && declType != null && declType.GetIsStatic() && method.IsDefinedSafe<ExtensionAttribute>());
        }
        output[index] = ')';
        return index + 1;
    }

    /// <inheritdoc/>
    public virtual int GetFormatLength(FieldInfo field, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = field.GetVisibility();
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

        MemberVisibility vis = Accessor.GetHighestVisibility(getMethod, setMethod);

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

        MemberVisibility vis = includeDefinitionKeywords ? type.GetVisibility() : default;
        bool isValueType = type.IsValueType;
        bool isAbstract = !isValueType && includeDefinitionKeywords && type is { IsAbstract: true, IsSealed: false, IsInterface: false };
        bool isDelegate = !isValueType && includeDefinitionKeywords && type != typeof(MulticastDelegate) && type.IsSubclassOf(typeof(Delegate));
        bool isReadOnly = isValueType && includeDefinitionKeywords && type.IsReadOnly();
        bool isStatic = !isValueType && includeDefinitionKeywords && type.GetIsStatic();
        bool isByRefType = false;
        Type? delegateReturnType = null;
        isByRefType = includeDefinitionKeywords && type.IsByRefLikeType();

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
                    MethodInfo invokeMethod = Accessor.GetInvokeMethod(type);
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


    /// <summary>
    /// Get the length of the by-ref type to the output.
    /// </summary>
    protected int GetRefTypeLength(ByRefTypeMode mode)
    {
        return mode switch
        {
            ByRefTypeMode.Ref => 4,
            ByRefTypeMode.In => 3,
            ByRefTypeMode.RefReadonly => 13,
            ByRefTypeMode.Out => 4,
            ByRefTypeMode.ScopedRef => 11,
            ByRefTypeMode.ScopedIn => 10,
            ByRefTypeMode.ScopedRefReadonly => 20,
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
            case ByRefTypeMode.RefReadonly:
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
            case ByRefTypeMode.ScopedRefReadonly:
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
        MemberVisibility vis = method.GetVisibility();
        ParameterInfo[] parameters = method.GetParameters();
        TypeMetaInfo* paramInfo = stackalloc TypeMetaInfo[parameters.Length];
        string methodName = method.Name;
        int len = 0;
        bool isReadOnly = method.IsReadOnly();

        if (includeDefinitionKeywords)
            len += GetVisibilityLength(vis) + 1;
        
        Type? declType = method.DeclaringType;
        Type? originalDeclType = declType;

        if (!isReadOnly && declType is { IsValueType: true } && declType.IsReadOnly())
            isReadOnly = true;

        bool isCtor = method.IsConstructor;
        bool isExtensionMethod = !isCtor && method.IsStatic && declType != null && declType.GetIsStatic() && method.IsDefinedSafe<ExtensionAttribute>();

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
        if (includeDefinitionKeywords)
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

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
        methodName.AsSpan().CopyTo(output[index..]);
#else
        for (int i = 0; i < methodName.Length; ++i)
        {
            output[index + i] = methodName[i];
        }
#endif
        index += methodName.Length;
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
    public virtual unsafe string Format(FieldInfo field, bool includeDefinitionKeywords = false)
    {
        MemberVisibility vis = field.GetVisibility();
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
            IsParams = parameterType.IsArray && parameter.IsDefinedSafe<ParamArrayAttribute>();
            if (IsParams)
                Length += 7;

            if (!parameterType.IsByRef)
                return;

            bool isScoped = !parameter.IsOut && parameter.IsCompilerAttributeDefinedSafe("ScopedRefAttribute");

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
            if (method is MethodInfo returnableMEthod
                && ElementTypesLength > 0
                && returnableMEthod.ReturnType.IsByRef
                && returnableMEthod.ReturnParameter != null
                && returnableMEthod.ReturnParameter.IsReadOnly())
            {
                Length += 13 - formatter.GetRefTypeLength((ByRefTypeMode)(-ElementTypes[0] - 1));
                ElementTypes[0] = -(int)ByRefTypeMode.RefReadonly - 1;
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

            int startIndex = -1;

            // flip array groups
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
    }
}
#pragma warning restore CA2014