#if NET461_OR_GREATER || !NETFRAMEWORK
using DanielWillett.ReflectionTools.Formatting;
using DanielWillett.ReflectionTools.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace DanielWillett.ReflectionTools.Tests;

[TestClass]
public class DependencyInjection
{
    [ClassInitialize]
    public static void Initialize(TestContext testContext)
    {
        TestSetup.Initialize(testContext);
    }

    [TestMethod]
    public void AddAccessor()
    {
        IServiceCollection collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddReflectionTools();

        IServiceProvider provider = collection.BuildServiceProvider();

        IAccessor accessor = provider.GetRequiredService<IAccessor>();

        Assert.AreEqual(accessor.Logger, provider.GetRequiredService<IReflectionToolsLogger>());
        Assert.AreEqual(accessor.Formatter, provider.GetRequiredService<IOpCodeFormatter>());

        if (provider is IDisposable disp)
            disp.Dispose();
    }

    [TestMethod]
    public void AddAccessorStaticDefault()
    {
        IServiceCollection collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddReflectionTools(isStaticDefault: true);

        IServiceProvider provider = collection.BuildServiceProvider();

        IAccessor accessor = provider.GetRequiredService<IAccessor>();

        Assert.AreEqual(accessor, Accessor.Active);
        Assert.AreEqual(accessor.Logger, provider.GetRequiredService<IReflectionToolsLogger>());
        Assert.AreEqual(accessor.Formatter, provider.GetRequiredService<IOpCodeFormatter>());

        if (provider is IDisposable disp)
            disp.Dispose();
    }
    
    [TestMethod]
    public void AddAccessorWithSameFormatters()
    {
        IServiceCollection collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddReflectionTools<DefaultOpCodeFormatter, DefaultOpCodeFormatter>();

        IServiceProvider provider = collection.BuildServiceProvider();

        IAccessor accessor = provider.GetRequiredService<IAccessor>();

        Assert.AreEqual(accessor.Logger, provider.GetRequiredService<IReflectionToolsLogger>());
        Assert.AreEqual(accessor.Formatter, provider.GetRequiredService<DefaultOpCodeFormatter>());
        Assert.AreEqual(accessor.Formatter, provider.GetRequiredService<IOpCodeFormatter>());
        Assert.AreEqual(accessor.ExceptionFormatter, provider.GetRequiredService<IOpCodeFormatter>());
        Assert.AreEqual(accessor.ExceptionFormatter, provider.GetRequiredService<DefaultOpCodeFormatter>());

        if (provider is IDisposable disp)
            disp.Dispose();
    }
    
    [TestMethod]
    public void AddAccessorWithFormatters()
    {
        IServiceCollection collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddReflectionTools<Formatter2, DefaultOpCodeFormatter>();

        IServiceProvider provider = collection.BuildServiceProvider();

        IAccessor accessor = provider.GetRequiredService<IAccessor>();

        Assert.AreEqual(accessor.Logger, provider.GetRequiredService<IReflectionToolsLogger>());
        Assert.AreEqual(accessor.Formatter, provider.GetRequiredService<Formatter2>());
        Assert.AreEqual(accessor.Formatter, provider.GetRequiredService<IOpCodeFormatter>());
        Assert.AreEqual(accessor.ExceptionFormatter, provider.GetRequiredService<DefaultOpCodeFormatter>());

        if (provider is IDisposable disp)
            disp.Dispose();
    }

    [TestMethod]
    public void AddAccessorConfigure()
    {
        IServiceCollection collection = new ServiceCollection();
        collection.AddLogging();
        DefaultOpCodeFormatter formatter2 = new DefaultOpCodeFormatter();
        collection.AddReflectionTools(configure =>
        {
            configure.Formatter = formatter2;
        });

        IServiceProvider provider = collection.BuildServiceProvider();

        IAccessor accessor = provider.GetRequiredService<IAccessor>();

        Assert.AreEqual(accessor.Logger, provider.GetRequiredService<IReflectionToolsLogger>());
        Assert.AreEqual(accessor.Formatter, formatter2);

        if (provider is IDisposable disp)
            disp.Dispose();
    }
}
public class Formatter2 : DefaultOpCodeFormatter;
#endif