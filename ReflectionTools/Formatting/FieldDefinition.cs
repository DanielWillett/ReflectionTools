using System;
using System.Reflection;

namespace DanielWillett.ReflectionTools.Formatting;

/// <summary>
/// Represents a shell of a field for formatting purposes.
/// </summary>
public class FieldDefinition : IMemberDefinition
{
    private bool _isConstant;
    private bool _isReadOnly;
    private bool _isStatic;

    /// <summary>
    /// Name of the field.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Type the field is declared in.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public Type? DeclaringType { get; set; }

    /// <summary>
    /// Type the field stores.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/>.</remarks>
    public Type? FieldType { get; set; }

    /// <summary>
    /// If the field is a <see langword="const"/> field.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will always be <see langword="false"/> if <see cref="IsConstant"/> is <see langword="false"/> or <see cref="IsStatic"/> is <see langword="false"/>.</remarks>
    public bool IsConstant
    {
        get => _isConstant;
        set
        {
            _isConstant = value;
            if (value)
            {
                _isReadOnly = true;
                _isStatic = true;
            }
        }
    }

    /// <summary>
    /// If the field is a <see langword="readonly"/> field.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will always be <see langword="true"/> if <see cref="IsConstant"/> is <see langword="true"/>.</remarks>
    public bool IsReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (!value)
                _isConstant = false;
            _isReadOnly = value;
        }
    }

    /// <summary>
    /// If the method requires an instance of <see cref="DeclaringType"/> to be accessed.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. Will always be <see langword="true"/> if <see cref="IsConstant"/> is <see langword="true"/>.</remarks>
    public bool IsStatic
    {
        get => _isStatic;
        set
        {
            if (!value)
                _isConstant = false;
            
            _isStatic = value;
        }
    }

    /// <summary>
    /// Create a field definition, starting with a field name.
    /// </summary>
    public FieldDefinition(string fieldName)
    {
        Name = fieldName;
    }

    /// <summary>
    /// Create a field definition from an existing field.
    /// </summary>
    public static FieldDefinition FromField(FieldInfo field)
    {
        return new FieldDefinition(field.Name)
        {
            IsStatic = field.IsStatic,
            DeclaringType = field.DeclaringType,
            IsReadOnly = field.IsInitOnly,
            IsConstant = field.IsLiteral,
            FieldType = field.FieldType
        };
    }

    /// <summary>
    /// Set the type stored in the field.
    /// </summary>
    public FieldDefinition WithFieldType<TFieldType>()
    {
        FieldType = typeof(TFieldType);
        return this;
    }

    /// <summary>
    /// Set the type stored in the field.
    /// </summary>
    public FieldDefinition WithFieldType(Type fieldType)
    {
        FieldType = fieldType;
        return this;
    }

    /// <summary>
    /// Set the type stored in the field.
    /// </summary>
    public FieldDefinition WithFieldType(string fieldType)
    {
        FieldType = Type.GetType(fieldType);
        return this;
    }

    /// <summary>
    /// Set <see cref="IsConstant"/>, <see cref="IsReadOnly"/>, and <see cref="IsStatic"/> to <see langword="true"/>.
    /// </summary>
    public FieldDefinition AsConstant()
    {
        IsConstant = true;
        return this;
    }

    /// <summary>
    /// Set <see cref="IsReadOnly"/> to <see langword="true"/>.
    /// </summary>
    public FieldDefinition AsReadOnly()
    {
        IsReadOnly = true;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the field.
    /// </summary>
    public FieldDefinition DeclaredIn<TDeclaringType>(bool isStatic)
    {
        DeclaringType = typeof(TDeclaringType);
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the field.
    /// </summary>
    public FieldDefinition DeclaredIn(Type declaringType, bool isStatic)
    {
        DeclaringType = declaringType;
        IsStatic = isStatic;
        return this;
    }

    /// <summary>
    /// Set the declaring type of the field.
    /// </summary>
    public FieldDefinition DeclaredIn(string declaringType, bool isStatic)
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
#endif
    /// <inheritdoc />
    public string Format(IOpCodeFormatter formatter) => formatter.Format(this);

    IMemberDefinition IMemberDefinition.NestedIn<TDeclaringType>(bool isStatic) => DeclaredIn<TDeclaringType>(isStatic);
    IMemberDefinition IMemberDefinition.NestedIn(Type declaringType, bool isStatic) => DeclaredIn(declaringType, isStatic);
    IMemberDefinition IMemberDefinition.NestedIn(string declaringType, bool isStatic) => DeclaredIn(declaringType, isStatic);

    /// <inheritdoc />
    public override string ToString() => Accessor.ExceptionFormatter.Format(this);
}