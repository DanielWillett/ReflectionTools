using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Formats op-codes and their operands into string values.
/// </summary>
public interface IOpCodeFormatter : ICloneable
{
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(OpCode, Span{char})"/>.
    /// </summary>
    /// <param name="opCode">The op-code to format.</param>
    /// <returns>The length in characters of <paramref name="opCode"/> as a string.</returns>
    int GetFormatLength(OpCode opCode);

    /// <summary>
    /// Format <paramref name="opCode"/> into a string representation. Use <see cref="GetFormatLength(OpCode)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="opCode">The op-code to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <returns>The length in characters of <paramref name="opCode"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(OpCode opCode, Span<char> output);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(Label, Span{char})"/>.
    /// </summary>
    /// <param name="label">The label to format.</param>
    /// <returns>The length in characters of <paramref name="label"/> as a string.</returns>
    int GetFormatLength(Label label);

    /// <summary>
    /// Format <paramref name="label"/> into a string representation. Use <see cref="GetFormatLength(Label)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="label">The label to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <returns>The length in characters of <paramref name="label"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(Label label, Span<char> output);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(OpCode, object?, Span{char}, OpCodeFormattingContext)"/>.
    /// </summary>
    /// <param name="opCode">The op-code to format.</param>
    /// <param name="operand">Optional operand to format after the op-code name.</param>
    /// <param name="usageContext">Whether to format the op-code/operand pair on one line (<see cref="OpCodeFormattingContext.InLine"/>) or on multiple (<see cref="OpCodeFormattingContext.List"/>).</param>
    /// <returns>The length in characters of <paramref name="opCode"/> and <paramref name="operand"/> as a string.</returns>
    int GetFormatLength(OpCode opCode, object? operand, OpCodeFormattingContext usageContext);

    /// <summary>
    /// Format <paramref name="opCode"/> and <paramref name="operand"/> into a string representation. Use <see cref="GetFormatLength(OpCode, object?, OpCodeFormattingContext)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="opCode">The op-code to format.</param>
    /// <param name="operand">Optional operand to format after the op-code name.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="usageContext">Whether to format the op-code/operand pair on one line (<see cref="OpCodeFormattingContext.InLine"/>) or on multiple (<see cref="OpCodeFormattingContext.List"/>).</param>
    /// <returns>The length in characters of <paramref name="opCode"/> and <paramref name="operand"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(OpCode opCode, object? operand, Span<char> output, OpCodeFormattingContext usageContext);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(Type, Span{char}, bool, ByRefTypeMode)"/>.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <returns>The length in characters of <paramref name="type"/> as a string.</returns>
    int GetFormatLength(Type type, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Format <paramref name="type"/> into a string representation. Use <see cref="GetFormatLength(Type, bool, ByRefTypeMode)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="type"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <param name="type">The type to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(Type type, Span<char> output, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format{T}(Span{char}, bool, ByRefTypeMode)"/>.
    /// </summary>
    /// <typeparam name="T">The type to format. This type will be made by-ref if <paramref name="refMode"/> is anything but <see cref="ByRefTypeMode.Ignore"/>.</typeparam>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <returns>The length in characters of <typeparamref name="T"/> as a string.</returns>
    public int GetFormatLength<T>(bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ignore)
    {
        Type type = typeof(T);
        if (refMode is > ByRefTypeMode.Ignore and <= ByRefTypeMode.ScopedRefReadOnly && !type.IsByRef)
            type = type.MakeByRefType();

        return GetFormatLength(type, includeDefinitionKeywords, refMode);
    }

    /// <summary>
    /// Format <typeparam name="T"/> into a string representation. Use <see cref="GetFormatLength{T}(bool, ByRefTypeMode)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <returns>The length in characters of <typeparamref name="T"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <typeparam name="T">The type to format. This type will be made by-ref if <paramref name="refMode"/> is anything but <see cref="ByRefTypeMode.Ignore"/>.</typeparam>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    public int Format<T>(Span<char> output, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ignore)
    {
        Type type = typeof(T);
        if (refMode is > ByRefTypeMode.Ignore and <= ByRefTypeMode.ScopedRefReadOnly && !type.IsByRef)
            type = type.MakeByRefType();

        return Format(type, output, includeDefinitionKeywords, refMode);
    }

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(TypeDefinition, Span{char}, ByRefTypeMode)"/>.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <returns>The length in characters of <paramref name="type"/> as a string.</returns>
    int GetFormatLength(TypeDefinition type, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Format <paramref name="type"/> into a string representation. Use <see cref="GetFormatLength(TypeDefinition, ByRefTypeMode)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="type"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <param name="type">The type to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(TypeDefinition type, Span<char> output, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(MethodBase, Span{char}, bool)"/>.
    /// </summary>
    /// <param name="method">The method to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="method"/> as a string.</returns>
    int GetFormatLength(MethodBase method, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="method"/> into a string representation. Use <see cref="GetFormatLength(MethodBase, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="method">The method to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="method"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(MethodBase method, Span<char> output, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(MethodDefinition, Span{char})"/>.
    /// </summary>
    /// <param name="method">The method to format.</param>
    /// <returns>The length in characters of <paramref name="method"/> as a string.</returns>
    int GetFormatLength(MethodDefinition method);

    /// <summary>
    /// Format <paramref name="method"/> into a string representation. Use <see cref="GetFormatLength(MethodDefinition)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="method">The method to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <returns>The length in characters of <paramref name="method"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(MethodDefinition method, Span<char> output);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(FieldInfo, Span{char}, bool)"/>.
    /// </summary>
    /// <param name="field">The field to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'const', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="field"/> as a string.</returns>
    int GetFormatLength(FieldInfo field, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="field"/> into a string representation. Use <see cref="GetFormatLength(FieldInfo, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="field">The field to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'const', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="field"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(FieldInfo field, Span<char> output, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(FieldDefinition, Span{char})"/>.
    /// </summary>
    /// <param name="field">The field to format.</param>
    /// <returns>The length in characters of <paramref name="field"/> as a string.</returns>
    int GetFormatLength(FieldDefinition field);

    /// <summary>
    /// Format <paramref name="field"/> into a string representation. Use <see cref="GetFormatLength(FieldDefinition)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="field">The field to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <returns>The length in characters of <paramref name="field"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(FieldDefinition field, Span<char> output);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(PropertyInfo, Span{char}, bool, bool)"/>.
    /// </summary>
    /// <param name="property">The property to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="property"/> as a string.</returns>
    int GetFormatLength(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="property"/> into a string representation. Use <see cref="GetFormatLength(PropertyInfo, bool, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="property">The property to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="property"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(PropertyInfo property, Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(PropertyDefinition, Span{char}, bool)"/>.
    /// </summary>
    /// <param name="property">The property to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <returns>The length in characters of <paramref name="property"/> as a string.</returns>
    int GetFormatLength(PropertyDefinition property, bool includeAccessors = true);

    /// <summary>
    /// Format <paramref name="property"/> into a string representation. Use <see cref="GetFormatLength(PropertyDefinition, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="property">The property to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <returns>The length in characters of <paramref name="property"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(PropertyDefinition property, Span<char> output, bool includeAccessors = true);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(EventInfo, Span{char}, bool, bool, bool)"/>.
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="event"/> as a string.</returns>
    int GetFormatLength(EventInfo @event, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="event"/> into a string representation. Use <see cref="GetFormatLength(EventInfo, bool, bool, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="event"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(EventInfo @event, Span<char> output, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(EventDefinition, Span{char}, bool, bool)"/>.
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <returns>The length in characters of <paramref name="event"/> as a string.</returns>
    int GetFormatLength(EventDefinition @event, bool includeAccessors = true, bool includeEventKeyword = true);

    /// <summary>
    /// Format <paramref name="event"/> into a string representation. Use <see cref="GetFormatLength(EventDefinition, bool, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <returns>The length in characters of <paramref name="event"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(EventDefinition @event, Span<char> output, bool includeAccessors = true, bool includeEventKeyword = true);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(ParameterInfo, Span{char}, bool)"/>.
    /// </summary>
    /// <param name="parameter">The parameter to format.</param>
    /// <param name="isExtensionThisParameter">Append a 'this' in front of the parameter like an extension method.</param>
    /// <returns>The length in characters of <paramref name="parameter"/> as a string.</returns>
    int GetFormatLength(ParameterInfo parameter, bool isExtensionThisParameter = false);

    /// <summary>
    /// Format <paramref name="parameter"/> into a string representation. Use <see cref="GetFormatLength(ParameterInfo, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="parameter">The parameter to format.</param>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="isExtensionThisParameter">Append a 'this' in front of the parameter like an extension method.</param>
    /// <returns>The length in characters of <paramref name="parameter"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(ParameterInfo parameter, Span<char> output, bool isExtensionThisParameter = false);

    /// <summary>
    /// Format <paramref name="opCode"/> into a string representation.
    /// </summary>
    /// <param name="opCode">The op-code to format.</param>
    public string Format(OpCode opCode)
    {
        int formatLength = GetFormatLength(opCode);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, OpCode>(this, opCode), (span, state) =>
        {
            state.Item1.Format(state.Item2, span);
        });
    }

    /// <summary>
    /// Format <paramref name="label"/> into a string representation.
    /// </summary>
    /// <param name="label">The label to format.</param>
    public string Format(Label label)
    {
        int formatLength = GetFormatLength(label);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, Label>(this, label), (span, state) =>
        {
            state.Item1.Format(state.Item2, span);
        });
    }

    /// <summary>
    /// Format <paramref name="opCode"/> and <paramref name="operand"/> into a string representation.
    /// </summary>
    /// <param name="opCode">The op-code to format.</param>
    /// <param name="operand">Optional operand to format after the op-code name.</param>
    /// <param name="usageContext">Whether to format the op-code/operand pair on one line (<see cref="OpCodeFormattingContext.InLine"/>) or on multiple (<see cref="OpCodeFormattingContext.List"/>).</param>
    public string Format(OpCode opCode, object? operand, OpCodeFormattingContext usageContext)
    {
        int formatLength = GetFormatLength(opCode, operand, usageContext);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, OpCode, object?, OpCodeFormattingContext>(this, opCode, operand, usageContext), (span, state) =>
        {
            state.Item1.Format(state.Item2, state.Item3, span, state.Item4);
        });
    }

    /// <summary>
    /// Format <paramref name="type"/> into a string representation.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter if <paramref name="type"/> is a by-ref type..</param>
    public string Format(Type type, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        int formatLength = GetFormatLength(type, includeDefinitionKeywords, refMode);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, Type, bool, ByRefTypeMode>(this, type, includeDefinitionKeywords, refMode), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3, state.Item4);
        });
    }

    /// <summary>
    /// Format <paramref name="type"/> into a string representation.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter if <paramref name="type"/> is a by-ref type.</param>
    public string Format(TypeDefinition type, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        int formatLength = GetFormatLength(type, refMode);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, TypeDefinition, ByRefTypeMode>(this, type, refMode), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3);
        });
    }

    /// <summary>
    /// Format <typeparam name="T"/> into a string representation.
    /// </summary>
    /// <typeparam name="T">The type to format. This type will be made by-ref if <paramref name="refMode"/> is anything but <see cref="ByRefTypeMode.Ignore"/>.</typeparam>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    public string Format<T>(bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ignore)
    {
        Type type = typeof(T);
        if (refMode is > ByRefTypeMode.Ignore and <= ByRefTypeMode.ScopedRefReadOnly && !type.IsByRef)
            type = type.MakeByRefType();

        return Format(type, includeDefinitionKeywords, refMode);
    }

    /// <summary>
    /// Format <paramref name="method"/> into a string representation.
    /// </summary>
    /// <param name="method">The method to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    public string Format(MethodBase method, bool includeDefinitionKeywords = false)
    {
        int formatLength = GetFormatLength(method, includeDefinitionKeywords);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, MethodBase, bool>(this, method, includeDefinitionKeywords), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3);
        });
    }

    /// <summary>
    /// Format <paramref name="method"/> into a string representation.
    /// </summary>
    /// <param name="method">The method to format.</param>
    public string Format(MethodDefinition method)
    {
        int formatLength = GetFormatLength(method);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, MethodDefinition>(this, method), (span, state) =>
        {
            state.Item1.Format(state.Item2, span);
        });
    }

    /// <summary>
    /// Format <paramref name="field"/> into a string representation.
    /// </summary>
    /// <param name="field">The field to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'const', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    public string Format(FieldInfo field, bool includeDefinitionKeywords = false)
    {
        int formatLength = GetFormatLength(field, includeDefinitionKeywords);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, FieldInfo, bool>(this, field, includeDefinitionKeywords), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3);
        });
    }

    /// <summary>
    /// Format <paramref name="field"/> into a string representation.
    /// </summary>
    /// <param name="field">The field to format.</param>
    public string Format(FieldDefinition field)
    {
        int formatLength = GetFormatLength(field);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, FieldDefinition>(this, field), (span, state) =>
        {
            state.Item1.Format(state.Item2, span);
        });
    }

    /// <summary>
    /// Format <paramref name="property"/> into a string representation.
    /// </summary>
    /// <param name="property">The property to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    public string Format(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false)
    {
        int formatLength = GetFormatLength(property, includeAccessors, includeDefinitionKeywords);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, PropertyInfo, bool, bool>(this, property, includeAccessors, includeDefinitionKeywords), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3, state.Item4);
        });
    }

    /// <summary>
    /// Format <paramref name="property"/> into a string representation.
    /// </summary>
    /// <param name="property">The property to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    public string Format(PropertyDefinition property, bool includeAccessors = true)
    {
        int formatLength = GetFormatLength(property, includeAccessors);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, PropertyDefinition, bool>(this, property, includeAccessors), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3);
        });
    }

    /// <summary>
    /// Format <paramref name="event"/> into a string representation.
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    public string Format(EventInfo @event, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false)
    {
        int formatLength = GetFormatLength(@event, includeAccessors, includeEventKeyword, includeDefinitionKeywords);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, EventInfo, bool, bool, bool>(this, @event, includeAccessors, includeEventKeyword, includeDefinitionKeywords), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3, state.Item4, state.Item5);
        });
    }

    /// <summary>
    /// Format <paramref name="event"/> into a string representation.
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    public string Format(EventDefinition @event, bool includeAccessors = true, bool includeEventKeyword = true)
    {
        int formatLength = GetFormatLength(@event, includeAccessors, includeEventKeyword);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, EventDefinition, bool, bool>(this, @event, includeAccessors, includeEventKeyword), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3, state.Item4);
        });
    }

    /// <summary>
    /// Format <paramref name="parameter"/> into a string representation.
    /// </summary>
    /// <param name="parameter">The parameter to format.</param>
    /// <param name="isExtensionThisParameter">Append a 'this' in front of the parameter like an extension method.</param>
    public string Format(ParameterInfo parameter, bool isExtensionThisParameter = false)
    {
        int formatLength = GetFormatLength(parameter, isExtensionThisParameter);
        return string.Create(formatLength, new ValueTuple<IOpCodeFormatter, ParameterInfo, bool>(this, parameter, isExtensionThisParameter), (span, state) =>
        {
            state.Item1.Format(state.Item2, span, state.Item3);
        });
    }
#else
    /// <summary>
    /// Format <paramref name="opCode"/> into a string representation.
    /// </summary>
    string Format(OpCode opCode);

    /// <summary>
    /// Format <paramref name="label"/> into a string representation.
    /// </summary>
    string Format(Label label);

    /// <summary>
    /// Format <paramref name="opCode"/> and <paramref name="operand"/> into a string representation.
    /// </summary>
    /// <param name="opCode">The op-code to format.</param>
    /// <param name="operand">Optional operand to format after the op-code name.</param>
    /// <param name="usageContext">Whether to format the op-code/operand pair on one line (<see cref="OpCodeFormattingContext.InLine"/>) or on multiple (<see cref="OpCodeFormattingContext.List"/>).</param>
    string Format(OpCode opCode, object? operand, OpCodeFormattingContext usageContext);

    /// <summary>
    /// Format <paramref name="type"/> into a string representation.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    string Format(Type type, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Format <paramref name="type"/> into a string representation.
    /// </summary>
    /// <param name="type">The type to format.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    string Format(TypeDefinition type, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Format <paramref name="method"/> into a string representation.
    /// </summary>
    /// <param name="method">The method to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    string Format(MethodBase method, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="method"/> into a string representation.
    /// </summary>
    /// <param name="method">The method to format.</param>
    string Format(MethodDefinition method);

    /// <summary>
    /// Format <paramref name="field"/> into a string representation.
    /// </summary>
    /// <param name="field">The field to format.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'const', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    string Format(FieldInfo field, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="field"/> into a string representation.
    /// </summary>
    /// <param name="field">The field to format.</param>
    string Format(FieldDefinition field);

    /// <summary>
    /// Format <paramref name="property"/> into a string representation.
    /// </summary>
    /// <param name="property">The property to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    string Format(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="property"/> into a string representation.
    /// </summary>
    /// <param name="property">The property to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    string Format(PropertyDefinition property, bool includeAccessors = true);

    /// <summary>
    /// Format <paramref name="event"/> into a string representation.
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    string Format(EventInfo @event, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="event"/> into a string representation.
    /// </summary>
    /// <param name="event">The event to format.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    string Format(EventDefinition @event, bool includeAccessors = true, bool includeEventKeyword = true);

    /// <summary>
    /// Format <paramref name="parameter"/> into a string representation.
    /// </summary>
    /// <param name="parameter">The parameter to format.</param>
    /// <param name="isExtensionThisParameter">Append a 'this' in front of the parameter like an extension method.</param>
    string Format(ParameterInfo parameter, bool isExtensionThisParameter = false);
#endif
}