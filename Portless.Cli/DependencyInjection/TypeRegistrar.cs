using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Portless.Cli.DependencyInjection;

public class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build()
    {
        return new TypeResolver(_services.BuildServiceProvider());
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067:DynamicallyAccessedMembers",
        Justification = "Spectre.Console.Cli ITypeRegistrar.Register interface does not carry DAM attributes — types are registered for DI and have public constructors by convention")]
    [UnconditionalSuppressMessage("Trimming", "IL2092:DynamicallyAccessedMembers",
        Justification = "Spectre.Console.Cli ITypeRegistrar.Register interface does not carry DAM attributes — cannot match interface signature")]
    public void Register(Type service, Type implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _services.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _services.AddSingleton(service, _ => factory());
    }
}
