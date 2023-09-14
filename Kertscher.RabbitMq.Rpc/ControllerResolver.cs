using System.Reflection;

namespace Kertscher.RabbitMq.Rpc;

internal class ControllerResolver
{
    private IServiceProvider _serviceProvider;
    private Dictionary<string, MethodInfo> _methods = new();

    public ControllerResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    internal void RegisterController<T>()
    {
        var type = typeof(T);
        var methods = from method in type.GetMethods()
            where method.ReturnType == typeof(Task<byte[]>) 
            where method.DeclaringType != null
            select new{method.Name, MethodInfo = method};
        foreach (var method in methods)
        {
            _methods.Add(method.Name, method.MethodInfo);
        }
    }

    internal async Task<byte[]> CallMethodAsync(string methodName, byte[] data)
    {
        if (!_methods.ContainsKey(methodName))
        {
            throw new UnknownControllerMethodException();
        }

        var methodInfo = _methods[methodName];
        var declaringType = methodInfo.DeclaringType!;
        var instance = _serviceProvider.GetService(declaringType);

        var methodDelegate = methodInfo.CreateDelegate<Func<Task<byte[]>>>(instance);
        return await methodDelegate();
    }
}