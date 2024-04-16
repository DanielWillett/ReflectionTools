using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Formats <see cref="OpCode"/>s and their operands into string values.
/// </summary>
public interface IOpCodeFormatter
{
#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(OpCode, Span{char})"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="opCode"/> as a string.</returns>
    int GetFormatLength(OpCode opCode);

    /// <summary>
    /// Format <paramref name="opCode"/> into a string representation. Use <see cref="GetFormatLength(OpCode)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="opCode"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(OpCode opCode, Span<char> output);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(Label, Span{char})"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="label"/> as a string.</returns>
    int GetFormatLength(Label label);

    /// <summary>
    /// Format <paramref name="label"/> into a string representation. Use <see cref="GetFormatLength(Label)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="label"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(Label label, Span<char> output);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(OpCode, object?, Span{char}, OpCodeFormattingContext)"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="opCode"/> and <paramref name="operand"/> as a string.</returns>
    int GetFormatLength(OpCode opCode, object? operand, OpCodeFormattingContext usageContext);

    /// <summary>
    /// Format <paramref name="opCode"/> and <paramref name="operand"/> into a string representation. Use <see cref="GetFormatLength(OpCode, object?, OpCodeFormattingContext)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="opCode"/> and <paramref name="operand"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(OpCode opCode, object? operand, Span<char> output, OpCodeFormattingContext usageContext);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(Type, Span{char}, bool, ByRefTypeMode)"/>.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <returns>The length in characters of <paramref name="type"/> as a string.</returns>
    int GetFormatLength(Type type, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Format <paramref name="type"/> into a string representation. Use <see cref="GetFormatLength(Type, bool, ByRefTypeMode)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <returns>The length in characters of <paramref name="type"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(Type type, Span<char> output, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(MethodBase, Span{char}, bool)"/>.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="method"/> as a string.</returns>
    int GetFormatLength(MethodBase method, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="method"/> into a string representation. Use <see cref="GetFormatLength(MethodBase, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="method"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(MethodBase method, Span<char> output, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(FieldInfo, Span{char})"/>.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'const', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="field"/> as a string.</returns>
    int GetFormatLength(FieldInfo field, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="field"/> into a string representation. Use <see cref="GetFormatLength(FieldInfo, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'const', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="field"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(FieldInfo field, Span<char> output, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(PropertyInfo, Span{char}, bool, bool)"/>.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="property"/> as a string.</returns>
    int GetFormatLength(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="property"/> into a string representation. Use <see cref="GetFormatLength(PropertyInfo, bool, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="property"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(PropertyInfo property, Span<char> output, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(EventInfo, Span{char}, bool, bool, bool)"/>.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="event"/> as a string.</returns>
    int GetFormatLength(EventInfo @event, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="@event"/> into a string representation. Use <see cref="GetFormatLength(EventInfo, bool, bool, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    /// <returns>The length in characters of <paramref name="event"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(EventInfo @event, Span<char> output, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(ParameterInfo, Span{char}, bool)"/>.
    /// </summary>
    /// <param name="isExtensionThisParameter">Append a 'this' in front of the parameter like an extension method.</param>
    /// <returns>The length in characters of <paramref name="parameter"/> as a string.</returns>
    int GetFormatLength(ParameterInfo parameter, bool isExtensionThisParameter = false);

    /// <summary>
    /// Format <paramref name="parameter"/> into a string representation. Use <see cref="GetFormatLength(ParameterInfo, bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="isExtensionThisParameter">Append a 'this' in front of the parameter like an extension method.</param>
    /// <returns>The length in characters of <paramref name="parameter"/> as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    int Format(ParameterInfo parameter, Span<char> output, bool isExtensionThisParameter = false);

    /// <summary>
    /// Format <paramref name="opCode"/> into a string representation.
    /// </summary>
    public string Format(OpCode opCode)
    {
        int formatLength = GetFormatLength(opCode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(opCode, span)];
        return new string(span);
    }

    /// <summary>
    /// Format <paramref name="label"/> into a string representation.
    /// </summary>
    public string Format(Label label)
    {
        int formatLength = GetFormatLength(label);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(label, span)];
        return new string(span);
    }

    /// <summary>
    /// Format <paramref name="opCode"/> and <paramref name="operand"/> into a string representation.
    /// </summary>
    public string Format(OpCode opCode, object? operand, OpCodeFormattingContext usageContext)
    {
        int formatLength = GetFormatLength(opCode, operand, usageContext);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(opCode, operand, span, usageContext)];
        return new string(span);
    }

    /// <summary>
    /// Format <paramref name="type"/> into a string representation.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    public string Format(Type type, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref)
    {
        int formatLength = GetFormatLength(type, includeDefinitionKeywords, refMode);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(type, span, includeDefinitionKeywords, refMode)];
        return new string(span);
    }

    /// <summary>
    /// Format <paramref name="method"/> into a string representation.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    public string Format(MethodBase method, bool includeDefinitionKeywords = false)
    {
        int formatLength = GetFormatLength(method, includeDefinitionKeywords);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(method, span, includeDefinitionKeywords)];
        return new string(span);
    }

    /// <summary>
    /// Format <paramref name="field"/> into a string representation.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'const', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    public string Format(FieldInfo field, bool includeDefinitionKeywords = false)
    {
        int formatLength = GetFormatLength(field, includeDefinitionKeywords);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(field, span, includeDefinitionKeywords)];
        return new string(span);
    }

    /// <summary>
    /// Format <paramref name="property"/> into a string representation.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    public string Format(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false)
    {
        int formatLength = GetFormatLength(property, includeAccessors, includeDefinitionKeywords);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(property, span, includeAccessors, includeDefinitionKeywords)];
        return new string(span);
    }

    /// <summary>
    /// Format <paramref name="event"/> into a string representation.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    public string Format(EventInfo @event, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false)
    {
        int formatLength = GetFormatLength(@event, includeAccessors, includeEventKeyword, includeDefinitionKeywords);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(@event, span, includeAccessors, includeEventKeyword, includeDefinitionKeywords)];
        return new string(span);
    }

    /// <summary>
    /// Format <paramref name="parameter"/> into a string representation.
    /// </summary>
    /// <param name="isExtensionThisParameter">Append a 'this' in front of the parameter like an extension method.</param>
    public string Format(ParameterInfo parameter, bool isExtensionThisParameter = false)
    {
        int formatLength = GetFormatLength(parameter, isExtensionThisParameter);
        Span<char> span = stackalloc char[formatLength];
        span = span[..Format(parameter, span, isExtensionThisParameter)];
        return new string(span);
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
    string Format(OpCode opCode, object? operand, OpCodeFormattingContext usageContext);

    /// <summary>
    /// Format <paramref name="type"/> into a string representation.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'struct', 'class', 'static', 'ref', 'readonly' be included.</param>
    /// <param name="refMode">Describes the way a by-ref type is passed as a parameter.</param>
    string Format(Type type, bool includeDefinitionKeywords = false, ByRefTypeMode refMode = ByRefTypeMode.Ref);

    /// <summary>
    /// Format <paramref name="method"/> into a string representation.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    string Format(MethodBase method, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="field"/> into a string representation.
    /// </summary>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'const', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    string Format(FieldInfo field, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="property"/> into a string representation.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    string Format(PropertyInfo property, bool includeAccessors = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="event"/> into a string representation.
    /// </summary>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <param name="includeEventKeyword">Should the 'event' keyword be put at the beginning.</param>
    /// <param name="includeDefinitionKeywords">Should definition keywords such as 'readonly', 'public', 'virtual', 'abtract', 'private', etc be included.</param>
    string Format(EventInfo @event, bool includeAccessors = true, bool includeEventKeyword = true, bool includeDefinitionKeywords = false);

    /// <summary>
    /// Format <paramref name="parameter"/> into a string representation.
    /// </summary>
    /// <param name="isExtensionThisParameter">Append a 'this' in front of the parameter like an extension method.</param>
    string Format(ParameterInfo parameter, bool isExtensionThisParameter = false);
#endif
}