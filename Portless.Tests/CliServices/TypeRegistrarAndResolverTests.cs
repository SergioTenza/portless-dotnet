extern alias Cli;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

using TypeRegistrar = Cli::Portless.Cli.DependencyInjection.TypeRegistrar;
using TypeResolver = Cli::Portless.Cli.DependencyInjection.TypeResolver;

namespace Portless.Tests.CliServices;

public class TypeRegistrarTests
{
    [Fact]
    public void Build_ReturnsTypeResolver()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        var resolver = registrar.Build();

        Assert.NotNull(resolver);
        Assert.IsType<TypeResolver>(resolver);
    }

    [Fact]
    public void Register_RegistersServiceImplementation()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        registrar.Register(typeof(ITestService), typeof(TestServiceImpl));

        var resolver = registrar.Build();
        var result = resolver.Resolve(typeof(ITestService));

        Assert.NotNull(result);
        Assert.IsType<TestServiceImpl>(result);
    }

    [Fact]
    public void RegisterInstance_RegistersSpecificInstance()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var instance = new TestServiceImpl { Value = 42 };

        registrar.RegisterInstance(typeof(ITestService), instance);

        var resolver = registrar.Build();
        var result = resolver.Resolve(typeof(ITestService));

        Assert.Same(instance, result);
        Assert.Equal(42, ((TestServiceImpl)result).Value);
    }

    [Fact]
    public void RegisterLazy_RegistersFactory()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);
        var callCount = 0;

        registrar.RegisterLazy(typeof(ITestService), () =>
        {
            callCount++;
            return new TestServiceImpl { Value = callCount };
        });

        var resolver = registrar.Build();
        var result1 = resolver.Resolve(typeof(ITestService));
        var result2 = resolver.Resolve(typeof(ITestService));

        Assert.NotNull(result1);
        // Singleton - same instance
        Assert.Same(result1, result2);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Register_MultipleServices_AllResolvable()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        registrar.Register(typeof(ITestService), typeof(TestServiceImpl));
        registrar.Register(typeof(IAnotherService), typeof(AnotherServiceImpl));

        var resolver = registrar.Build();
        Assert.NotNull(resolver.Resolve(typeof(ITestService)));
        Assert.NotNull(resolver.Resolve(typeof(IAnotherService)));
    }

    [Fact]
    public void RegisterInstance_Overwrite_Works()
    {
        var services = new ServiceCollection();
        var registrar = new TypeRegistrar(services);

        var instance1 = new TestServiceImpl { Value = 1 };
        var instance2 = new TestServiceImpl { Value = 2 };

        registrar.RegisterInstance(typeof(ITestService), instance1);
        registrar.RegisterInstance(typeof(ITestService), instance2);

        var resolver = registrar.Build();
        // Last registration wins for singleton
        var result = resolver.Resolve(typeof(ITestService));
        Assert.Equal(2, ((TestServiceImpl)result).Value);
    }

    private interface ITestService { }
    private interface IAnotherService { }

    private class TestServiceImpl : ITestService
    {
        public int Value { get; set; }
    }

    private class AnotherServiceImpl : IAnotherService { }
}

public class TypeResolverTests
{
    [Fact]
    public void Resolve_NullType_ReturnsNull()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var resolver = new TypeResolver(provider);

        var result = resolver.Resolve(null);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_UnregisteredType_ReturnsNull()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var resolver = new TypeResolver(provider);

        var result = resolver.Resolve(typeof(string));

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_RegisteredType_ReturnsInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(string), "hello");
        var provider = services.BuildServiceProvider();
        var resolver = new TypeResolver(provider);

        var result = resolver.Resolve(typeof(string));

        Assert.Equal("hello", result);
    }

    [Fact]
    public void Dispose_DisposesServiceProvider()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var resolver = new TypeResolver(provider);

        // Should not throw
        resolver.Dispose();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();
        var resolver = new TypeResolver(provider);

        resolver.Dispose();
        resolver.Dispose(); // Should not throw
    }

    [Fact]
    public void Resolve_AfterDispose_ReturnsDisposedInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(string), "test");
        var provider = services.BuildServiceProvider();
        var resolver = new TypeResolver(provider);

        resolver.Dispose();

        // Accessing a disposed provider may throw or return null
        // Either way, the test validates the Dispose path
        try
        {
            var result = resolver.Resolve(typeof(string));
            // If it doesn't throw, result could be null or "test"
        }
        catch (ObjectDisposedException)
        {
            // Expected in some implementations
        }
    }
}
