using System;
using System.Linq;
using System.Reflection;
#if NET40_OR_GREATER || !NETFRAMEWORK
using System.Diagnostics.Contracts;
#endif

namespace DanielWillett.ReflectionTools;

/// <summary>
/// Class for creating and extending <see cref="IVariable"/>, which bridges the gap between fields and properties.
/// </summary>
public static class Variables
{
    /// <summary>
    /// Creates an abstracted <see cref="IVariable"/> for <paramref name="field"/>.
    /// </summary>
    /// <param name="field">The underlying field.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <returns>An abstracted variable with <paramref name="field"/> as it's underlying field.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IVariable AsVariable(this FieldInfo field, IAccessor? accessor = null)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        accessor ??= Accessor.Active;
        return new FieldVariable(field, accessor);
    }

    /// <summary>
    /// Creates an abstracted <see cref="IVariable"/> for <paramref name="property"/>.
    /// </summary>
    /// <param name="property">The underlying property.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <returns>An abstracted variable with <paramref name="property"/> as it's underlying property.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IVariable AsVariable(this PropertyInfo property, IAccessor? accessor = null)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        accessor ??= Accessor.Active;
        return new PropertyVariable(property, accessor);
    }

    /// <summary>
    /// Creates an abstracted <see cref="IVariable"/> for <paramref name="member"/>, a field or property.
    /// </summary>
    /// <param name="member">The underlying field or property. Must be of type <see cref="FieldInfo"/> or <see cref="PropertyInfo"/>.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <returns>An abstracted variable with <paramref name="member"/> as it's underlying field or property.</returns>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"><paramref name="member"/> is not a field or property.</exception>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IVariable AsVariable(MemberInfo member, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        return member switch
        {
            PropertyInfo property => new PropertyVariable(property, accessor),
            FieldInfo field => new FieldVariable(field, accessor),
            null => throw new ArgumentNullException(nameof(member)),
            _ => throw new ArgumentException($"Invalid member type: {member.MemberType}.", nameof(member))
        };
    }

    /// <summary>
    /// Creates a static, strongly-typed, abstracted <see cref="IVariable"/> for <paramref name="field"/>.
    /// </summary>
    /// <param name="field">The underlying field.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TMemberType">The field type of <paramref name="field"/>.</typeparam>
    /// <returns>An abstracted variable with <paramref name="field"/> as it's underlying field.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IStaticVariable<TMemberType> AsStaticVariable<TMemberType>(this FieldInfo field, IAccessor? accessor = null)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        accessor ??= Accessor.Active;
        return new StaticFieldVariable<TMemberType>(field, accessor);
    }

    /// <summary>
    /// Creates a static, strongly-typed, abstracted <see cref="IVariable"/> for <paramref name="property"/>.
    /// </summary>
    /// <param name="property">The underlying property.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TMemberType">The property type of <paramref name="property"/>.</typeparam>
    /// <returns>An abstracted variable with <paramref name="property"/> as it's underlying property.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IStaticVariable<TMemberType> AsStaticVariable<TMemberType>(this PropertyInfo property, IAccessor? accessor = null)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        accessor ??= Accessor.Active;
        return new StaticPropertyVariable<TMemberType>(property, accessor);
    }

    /// <summary>
    /// Creates an instance, strongly-typed, abstracted <see cref="IVariable"/> for <paramref name="field"/>.
    /// </summary>
    /// <param name="field">The underlying field.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TMemberType">The field type of <paramref name="field"/>.</typeparam>
    /// <typeparam name="TDeclaringType">The type that <paramref name="field"/> is declared in.</typeparam>
    /// <returns>An abstracted variable with <paramref name="field"/> as it's underlying field.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IInstanceVariable<TDeclaringType, TMemberType> AsInstanceVariable<TDeclaringType, TMemberType>(this FieldInfo field, IAccessor? accessor = null)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        accessor ??= Accessor.Active;
        return new InstanceFieldVariable<TDeclaringType, TMemberType>(field, accessor);
    }

    /// <summary>
    /// Creates an instance, strongly-typed, abstracted <see cref="IVariable"/> for <paramref name="property"/>.
    /// </summary>
    /// <param name="property">The underlying property.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TMemberType">The property type of <paramref name="property"/>.</typeparam>
    /// <typeparam name="TDeclaringType">The type that <paramref name="property"/> is declared in.</typeparam>
    /// <returns>An abstracted variable with <paramref name="property"/> as it's underlying property.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IInstanceVariable<TDeclaringType, TMemberType> AsInstanceVariable<TDeclaringType, TMemberType>(this PropertyInfo property, IAccessor? accessor = null)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        accessor ??= Accessor.Active;
        return new InstancePropertyVariable<TDeclaringType, TMemberType>(property, accessor);
    }

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="variable">The found variable, or <see langword="null"/>.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool TryFind<TDeclaringType>(string name, out IVariable? variable, bool ignoreCase = false, IAccessor? accessor = null)
    {
        variable = Find(typeof(TDeclaringType), name, ignoreCase, accessor)!;
        return variable != null;
    }

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="declaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</param>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="variable">The found variable, or <see langword="null"/>.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool TryFind(Type declaringType, string name, out IVariable? variable, bool ignoreCase = false, IAccessor? accessor = null)
    {
        variable = Find(declaringType, name, ignoreCase, accessor)!;
        return variable != null;
    }

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="variable">The found variable, or <see langword="null"/>.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool TryFindStatic<TDeclaringType, TMemberType>(string name, out IStaticVariable<TMemberType>? variable, bool ignoreCase = false, IAccessor? accessor = null)
    {
        variable = FindStatic<TMemberType>(typeof(TDeclaringType), name, ignoreCase, accessor)!;
        return variable != null;
    }

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="variable">The found variable, or <see langword="null"/>.</param>
    /// <param name="declaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <returns><see langword="true"/> if the variable was found, otherwise <see langword="false"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool TryFindStatic<TMemberType>(Type declaringType, string name, out IStaticVariable<TMemberType>? variable, bool ignoreCase = false, IAccessor? accessor = null)
    {
        variable = FindStatic<TMemberType>(declaringType, name, ignoreCase, accessor)!;
        return variable != null;
    }

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IVariable? Find<TDeclaringType>(string name, bool ignoreCase = false, IAccessor? accessor = null) => Find(typeof(TDeclaringType), name, ignoreCase, accessor);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IStaticVariable<TMemberType>? FindStatic<TDeclaringType, TMemberType>(string name, bool ignoreCase = false, IAccessor? accessor = null) => FindStatic<TMemberType>(typeof(TDeclaringType), name, ignoreCase, accessor);

    /// <summary>
    /// Find a variable by declaring type and name.
    /// </summary>
    /// <param name="declaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</param>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IVariable? Find(Type declaringType, string name, bool ignoreCase = false, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        BindingFlags defaultFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | (BindingFlags)((ignoreCase ? 1 : 0) * (int)BindingFlags.IgnoreCase); 

        FieldInfo? field = declaringType.GetField(name, defaultFlags);
        if (field != null)
            return new FieldVariable(field, accessor);

        PropertyInfo? property;
        try
        {
            property = declaringType.GetProperty(name, defaultFlags);
        }
        catch (AmbiguousMatchException)
        {
            property = declaringType.GetProperties(defaultFlags).FirstOrDefault(x => x.Name.Equals(name, StringComparison.Ordinal));
        }
        if (property != null)
            return new PropertyVariable(property, accessor);

        declaringType = declaringType.BaseType!;
        if (declaringType == null)
            return null;

        field = declaringType.GetField(name, defaultFlags | BindingFlags.FlattenHierarchy);
        if (field != null)
            return new FieldVariable(field, accessor);

        try
        {
            property = declaringType.GetProperty(name, defaultFlags | BindingFlags.FlattenHierarchy);
        }
        catch (AmbiguousMatchException)
        {
            property = declaringType.GetProperties(defaultFlags | BindingFlags.FlattenHierarchy).FirstOrDefault(x => x.Name.Equals(name, StringComparison.Ordinal));
        }

        return property != null ? new PropertyVariable(property, accessor) : null;
    }

    /// <summary>
    /// Find a static variable by declaring type and name.
    /// </summary>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <param name="declaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</param>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IStaticVariable<TMemberType>? FindStatic<TMemberType>(Type declaringType, string name, bool ignoreCase = false, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        BindingFlags defaultFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | (BindingFlags)((ignoreCase ? 1 : 0) * (int)BindingFlags.IgnoreCase); 

        FieldInfo? field = declaringType.GetField(name, defaultFlags);
        if (field != null)
        {
            if (field.FieldType == typeof(TMemberType))
                return new StaticFieldVariable<TMemberType>(field, accessor);

            return null;
        }

        PropertyInfo? property;
        try
        {
            property = declaringType.GetProperty(name, defaultFlags);
        }
        catch (AmbiguousMatchException)
        {
            property = declaringType.GetProperties(defaultFlags).FirstOrDefault(x => x.Name.Equals(name, StringComparison.Ordinal));
        }
        if (property != null)
        {
            if (property.PropertyType == typeof(TMemberType))
                return new StaticPropertyVariable<TMemberType>(property, accessor);

            return null;
        }

        declaringType = declaringType.BaseType!;
        if (declaringType == null)
            return null;

        field = declaringType.GetField(name, defaultFlags | BindingFlags.FlattenHierarchy);
        if (field != null)
        {
            if (field.FieldType == typeof(TMemberType))
                return new StaticFieldVariable<TMemberType>(field, accessor);

            return null;
        }

        try
        {
            property = declaringType.GetProperty(name, defaultFlags | BindingFlags.FlattenHierarchy);
        }
        catch (AmbiguousMatchException)
        {
            property = declaringType.GetProperties(defaultFlags | BindingFlags.FlattenHierarchy).FirstOrDefault(x => x.Name.Equals(name, StringComparison.Ordinal));
        }

        if (property == null)
            return null;

        if (property.PropertyType == typeof(TMemberType))
            return new StaticPropertyVariable<TMemberType>(property, accessor);

        return null;
    }

    /// <summary>
    /// Find an instance variable by declaring type and name.
    /// </summary>
    /// <typeparam name="TMemberType">The type of the field or property.</typeparam>
    /// <typeparam name="TDeclaringType">Type which declares the member, or a type up the hierarchy of a type that declares the member.</typeparam>
    /// <param name="name">Exact name of the field or property.</param>
    /// <param name="ignoreCase">Whether or not to perform a case-insensitive search.</param>
    /// <param name="accessor"><see cref="IAccessor"/> instance to use for accessors. Defaults to <see cref="Accessor.Active"/>.</param>
    /// <returns>The found variable, or <see langword="null"/>.</returns>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static IInstanceVariable<TDeclaringType, TMemberType>? FindInstance<TDeclaringType, TMemberType>(string name, bool ignoreCase = false, IAccessor? accessor = null)
    {
        accessor ??= Accessor.Active;
        BindingFlags defaultFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | (BindingFlags)((ignoreCase ? 1 : 0) * (int)BindingFlags.IgnoreCase);
        Type declaringType = typeof(TDeclaringType);
        FieldInfo? field = declaringType.GetField(name, defaultFlags);
        if (field != null)
        {
            if (field.FieldType == typeof(TMemberType) && field.DeclaringType != null && field.DeclaringType.IsAssignableFrom(declaringType))
                return new InstanceFieldVariable<TDeclaringType, TMemberType>(field, accessor);

            return null;
        }

        PropertyInfo? property;
        try
        {
            property = declaringType.GetProperty(name, defaultFlags);
        }
        catch (AmbiguousMatchException)
        {
            property = declaringType.GetProperties(defaultFlags).FirstOrDefault(x => x.Name.Equals(name, StringComparison.Ordinal));
        }
        if (property != null)
        {
            if (property.PropertyType == typeof(TMemberType) && property.DeclaringType != null && property.DeclaringType.IsAssignableFrom(declaringType))
                return new InstancePropertyVariable<TDeclaringType, TMemberType>(property, accessor);

            return null;
        }

        declaringType = declaringType.BaseType!;
        if (declaringType == null)
            return null;

        field = declaringType.GetField(name, defaultFlags | BindingFlags.FlattenHierarchy);
        if (field != null)
        {
            if (field.FieldType == typeof(TMemberType) && field.DeclaringType != null && field.DeclaringType.IsAssignableFrom(declaringType))
                return new InstanceFieldVariable<TDeclaringType, TMemberType>(field, accessor);

            return null;
        }

        try
        {
            property = declaringType.GetProperty(name, defaultFlags | BindingFlags.FlattenHierarchy);
        }
        catch (AmbiguousMatchException)
        {
            property = declaringType.GetProperties(defaultFlags | BindingFlags.FlattenHierarchy).FirstOrDefault(x => x.Name.Equals(name, StringComparison.Ordinal));
        }

        if (property == null)
            return null;

        if (property.PropertyType == typeof(TMemberType) && property.DeclaringType != null && property.DeclaringType.IsAssignableFrom(declaringType))
            return new InstancePropertyVariable<TDeclaringType, TMemberType>(property, accessor);

        return null;
    }

    /// <summary>
    /// Checks if it is safe to set this variable to a value of <paramref name="type"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsAssignableFrom(this IVariable variable, Type type)
    {
        if (variable == null) throw new ArgumentNullException(nameof(variable));
        if (type == null) throw new ArgumentNullException(nameof(type));

        return variable.MemberType.IsAssignableFrom(type);
    }

    /// <summary>
    /// Checks if it is safe to set this variable to a value of <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsAssignableFrom<T>(this IVariable variable) => variable.IsAssignableFrom(typeof(T));

    /// <summary>
    /// Checks if it is safe to get a variable and cast the result to <paramref name="type"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsAssignableTo(this IVariable variable, Type type)
    {
        if (variable == null) throw new ArgumentNullException(nameof(variable));
        if (type == null) throw new ArgumentNullException(nameof(type));

        return type.IsAssignableFrom(variable.MemberType);
    }

    /// <summary>
    /// Checks if it is safe to get a variable and cast the result to <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool IsAssignableTo<T>(this IVariable variable) => variable.IsAssignableTo(typeof(T));

    /// <summary>
    /// Checks if it's possible for <paramref name="variable"/> to have a value of type <paramref name="type"/>. 
    /// </summary>
    /// <returns><see langword="true"/> if the type of <paramref name="variable"/> is assignable from <paramref name="type"/> or if <paramref name="type"/> is assignable from the type of <paramref name="variable"/>.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool CouldBeAssignedTo(this IVariable variable, Type type)
    {
        if (variable == null) throw new ArgumentNullException(nameof(variable));
        if (type == null) throw new ArgumentNullException(nameof(type));

        return variable.MemberType.IsAssignableFrom(type) || type.IsAssignableFrom(variable.MemberType);
    }

    /// <summary>
    /// Checks if it's possible for a variable of type <paramref name="variable"/> to have a value of type <typeparamref name="T"/>. 
    /// </summary>
    /// <returns><see langword="true"/> if the type of <paramref name="variable"/> is assignable from <typeparamref name="T"/> or if <typeparamref name="T"/> is assignable from the type of <paramref name="variable"/>.</returns>
    /// <exception cref="ArgumentNullException"/>
#if NET40_OR_GREATER || !NETFRAMEWORK
    [Pure]
#endif
    public static bool CouldBeAssignedTo<T>(this IVariable variable) => variable.CouldBeAssignedTo(typeof(T));
}
