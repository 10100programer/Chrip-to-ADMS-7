using Spectre.Console;
using Spectre.Console.Cli;

namespace Chirp2Ftm400;

internal sealed class ConsoleTypeResolver : ITypeResolver, IDisposable
{
    private readonly IAnsiConsole _console;
    private readonly Dictionary<Type, Type> _registrations;
    private readonly Dictionary<Type, object> _instances;
    private readonly Dictionary<Type, Func<object>> _factories;

    public ConsoleTypeResolver(
        IAnsiConsole console,
        Dictionary<Type, Type> registrations,
        Dictionary<Type, object> instances,
        Dictionary<Type, Func<object>> factories)
    {
        _console = console;
        _registrations = registrations;
        _instances = instances;
        _factories = factories;
    }

    public object? Resolve(Type? type)
    {
        if (type is null) return null;

        if (_instances.TryGetValue(type, out var instance))
            return instance;

        if (_factories.TryGetValue(type, out var factory))
            return factory();

        Type impl = _registrations.TryGetValue(type, out var registered) ? registered : type;

        // Inject IAnsiConsole into commands that need it
        foreach (var ctor in impl.GetConstructors())
        {
            var parameters = ctor.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(IAnsiConsole))
                return ctor.Invoke([_console]);
        }

        // Return empty enumerable for collection requests Spectre makes internally
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = type.GetGenericArguments()[0];
            return Array.CreateInstance(elementType, 0);
        }

        try
        {
            return Activator.CreateInstance(impl);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() { }
}

internal sealed class ConsoleTypeRegistrar : ITypeRegistrar
{
    private readonly IAnsiConsole _console;
    private readonly Dictionary<Type, Type> _registrations = new();
    private readonly Dictionary<Type, object> _instances = new();
    private readonly Dictionary<Type, Func<object>> _factories = new();

    public ConsoleTypeRegistrar(IAnsiConsole console)
    {
        _console = console;
    }

    public ITypeResolver Build() =>
        new ConsoleTypeResolver(_console, _registrations, _instances, _factories);

    public void Register(Type service, Type implementation) =>
        _registrations[service] = implementation;

    public void RegisterInstance(Type service, object implementation) =>
        _instances[service] = implementation;

    public void RegisterLazy(Type service, Func<object> factory) =>
        _factories[service] = factory;
}
