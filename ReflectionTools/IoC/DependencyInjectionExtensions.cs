#if NET461_OR_GREATER || !NETFRAMEWORK
using DanielWillett.ReflectionTools.Formatting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DanielWillett.ReflectionTools.IoC;

/// <summary>
/// Extensions for adding ReflectionTools to a service provider.
/// </summary>
[Ignore]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Add <see cref="IAccessor"/> as a service with logging.
    /// </summary>
    public static IServiceCollection AddReflectionTools(this IServiceCollection collection, Action<IAccessor>? configureAccessor = null, bool isStaticDefault = false)
    {
        collection.AddSingleton<IReflectionToolsLogger, ReflectionToolsLoggerProxy>();

        collection.AddSingleton<IOpCodeFormatter, DefaultOpCodeFormatter>();

        return collection.AddSingleton<IAccessor, DefaultAccessor>(serviceProvider
            => new DefaultAccessor(
                serviceProvider.GetRequiredService<IReflectionToolsLogger>(),
                serviceProvider.GetService<IOpCodeFormatter>(),
                null,
                configureAccessor,
                isStaticDefault)
        );
    }

    /// <summary>
    /// Add <see cref="IAccessor"/> as a service with logging and the specified formatters.
    /// </summary>
    public static IServiceCollection AddReflectionTools<TFormatter, TExceptionFormatter>(this IServiceCollection collection, Action<IAccessor>? configureAccessor = null, bool isStaticDefault = false)
        where TFormatter : class, IOpCodeFormatter where TExceptionFormatter : class, IOpCodeFormatter
    {
        collection.AddSingleton<IReflectionToolsLogger, ReflectionToolsLoggerProxy>();

        collection.AddSingleton<TFormatter>();
        collection.AddSingleton<IOpCodeFormatter, TFormatter>(serviceProvider => serviceProvider.GetRequiredService<TFormatter>());

        if (typeof(TFormatter) != typeof(TExceptionFormatter))
        {
            collection.AddSingleton<TExceptionFormatter>();
        }

        return collection.AddSingleton<IAccessor, DefaultAccessor>(serviceProvider
            => new DefaultAccessor(
                serviceProvider.GetRequiredService<IReflectionToolsLogger>(),
                serviceProvider.GetService<IOpCodeFormatter>(),
                serviceProvider.GetService<TExceptionFormatter>(),
                configureAccessor,
                isStaticDefault)
        );
    }
}
#endif