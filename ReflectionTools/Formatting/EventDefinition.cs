using System;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Represents a shell of a event for formatting purposes.
/// </summary>
public class EventDefinition : IMemberDefinition
{
    private Type? _handlerType;

    /// <summary>
    /// Name of the event.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Type the event is declared in.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public Type? DeclaringType { get; set; }

    /// <summary>
    /// Type of the event handler.
    /// </summary>
    /// <exception cref="ArgumentException">HandlerType must be a delegate type.</exception>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public Type? HandlerType
    {
        get => _handlerType;
        set
        {
            if (value != null && !value.IsSubclassOf(typeof(Delegate)))
                throw new ArgumentException("HandlerType must be a delegate type.");

            _handlerType = value;
        }
    }

    /// <summary>
    /// If the method requires an instance of <see cref="DeclaringType"/> to be accessed.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    public bool IsStatic { get; set; }

    /// <summary>
    /// If this event has an <see langword="add"/> accessor.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool HasAdder { get; set; } = true;

    /// <summary>
    /// If this event has a <see langword="remove"/> accessor.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool HasRemover { get; set; } = true;

    /// <summary>
    /// If this event has a <see langword="raise"/> accessor.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. While not supported by C#, the CLR supports a 'raise' or 'fire' method for events, which 'invokes' the event.</remarks>
    public bool HasRaiser { get; set; }

    /// <summary>
    /// Create a event definition, starting with a event name.
    /// </summary>
    public EventDefinition(string eventName)
    {
        Name = eventName;
    }

    /// <summary>
    /// Create an event definition from an existing event.
    /// </summary>
    public static EventDefinition FromEvent(EventInfo @event)
    {
        MethodInfo? addMethod = @event.GetAddMethod(true);
        MethodInfo? removeMethod = @event.GetRemoveMethod(true);
        MethodInfo? raiseMethod = @event.GetRaiseMethod(true);

        return new EventDefinition(@event.Name)
        {
            IsStatic = addMethod != null && addMethod.IsStatic || removeMethod != null && removeMethod.IsStatic || raiseMethod != null && raiseMethod.IsStatic,
            DeclaringType = @event.DeclaringType,
            HasAdder = addMethod != null,
            HasRemover = removeMethod != null,
            HasRaiser = raiseMethod != null,
            _handlerType = @event.EventHandlerType
        };
    }

    /// <summary>
    /// Specify that this event has no <see langword="add"/> accessor.
    /// </summary>
    public EventDefinition WithNoAdder()
    {
        HasAdder = false;
        return this;
    }

    /// <summary>
    /// Specify that this event has no <see langword="remove"/> accessor.
    /// </summary>
    public EventDefinition WithNoRemover()
    {
        HasRemover = false;
        return this;
    }

    /// <summary>
    /// Specify that this event has a <see langword="raise"/> accessor.
    /// </summary>
    /// <remarks>While not supported by C#, the CLR supports a 'raise' or 'fire' method for events, which 'invokes' the event.</remarks>
    public EventDefinition WithRaiser()
    {
        HasRaiser = true;
        return this;
    }

    /// <summary>
    /// Set the type stored in the event.
    /// </summary>
    public EventDefinition WithHandlerType<THandlerType>() where THandlerType : Delegate
    {
        HandlerType = typeof(THandlerType);
        return this;
    }

    /// <summary>
    /// Set the type stored in the event.
    /// </summary>
    /// <exception cref="ArgumentException">HandlerType must be a delegate type.</exception>
    public EventDefinition WithHandlerType(Type eventType)
    {
        HandlerType = eventType;
        return this;
    }

    /// <summary>
    /// Set the type stored in the event.
    /// </summary>
    /// <exception cref="ArgumentException">HandlerType must be a delegate type.</exception>
    public EventDefinition WithHandlerType(string eventType)
    {
        HandlerType = Type.GetType(eventType);
        return this;
    }

    /// <summary>
    /// Set the declaring type of the event.
    /// </summary>
    public EventDefinition DeclaredIn<TDeclaringType>(bool isStatic)
    {
        DeclaringType = typeof(TDeclaringType);
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the event.
    /// </summary>
    public EventDefinition DeclaredIn(Type declaringType, bool isStatic)
    {
        DeclaringType = declaringType;
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the event.
    /// </summary>
    public EventDefinition DeclaredIn(string declaringType, bool isStatic)
    {
        DeclaringType = Type.GetType(declaringType);
        IsStatic = isStatic;
        return this;
    }

#if !NETFRAMEWORK && (!NETSTANDARD || NETSTANDARD2_1_OR_GREATER)
    /// <inheritdoc />
    public int GetFormatLength(IOpCodeFormatter formatter) => formatter.GetFormatLength(this);

    /// <inheritdoc />
    public int Format(IOpCodeFormatter formatter, Span<char> output) => formatter.Format(this, output);

    /// <summary>
    /// Calculate the length of the string returned by <see cref="Format(IOpCodeFormatter,Span{char},bool)"/>.
    /// </summary>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <returns>The length in characters of this as a string.</returns>
    public int GetFormatLength(IOpCodeFormatter formatter, bool includeAccessors) => formatter.GetFormatLength(this, includeAccessors);

    /// <summary>
    /// Format this into a string representation. Use <see cref="GetFormatLength(IOpCodeFormatter,bool)"/> to get the desired length of <paramref name="output"/>.
    /// </summary>
    /// <param name="output">Buffer to put the formatted characters in.</param>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    /// <returns>The length in characters of this as a string that were written to <paramref name="output"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="output"/> is not large enough.</exception>
    public int Format(IOpCodeFormatter formatter, Span<char> output, bool includeAccessors) => formatter.Format(this, output, includeAccessors);
#endif

    /// <inheritdoc />
    public string Format(IOpCodeFormatter formatter) => formatter.Format(this);

    /// <summary>
    /// Format this into a string representation.
    /// </summary>
    /// <param name="formatter">Instance of <see cref="IOpCodeFormatter"/> to use for the formatting.</param>
    /// <param name="includeAccessors">Should the accessors be put at the end.</param>
    public string Format(IOpCodeFormatter formatter, bool includeAccessors) => formatter.Format(this, includeAccessors);

    IMemberDefinition IMemberDefinition.NestedIn<TDeclaringType>(bool isStatic) => DeclaredIn<TDeclaringType>(isStatic);
    IMemberDefinition IMemberDefinition.NestedIn(Type declaringType, bool isStatic) => DeclaredIn(declaringType, isStatic);
    IMemberDefinition IMemberDefinition.NestedIn(string declaringType, bool isStatic) => DeclaredIn(declaringType, isStatic);

    /// <inheritdoc />
    public override string ToString() => Accessor.ExceptionFormatter.Format(this);
}