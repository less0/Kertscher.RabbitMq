using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kertscher.RabbitMq.Rpc;

internal class ControllerResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, MethodInfo> _methods = new();

    public ControllerResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    internal void RegisterController<T>()
    {
        var type = typeof(T);
        var methods = from method in type.GetMethods()
            where method.ReturnType.IsAssignableTo(typeof(Task)) 
            where method.DeclaringType != null
            select new{method.Name, MethodInfo = method};
        foreach (var method in methods)
        {
            _methods.Add(method.Name, method.MethodInfo);
        }
    }

    internal async Task<byte[]> CallMethodAsync(string methodName, byte[] data)
    {
        var methodInfo = GetMethodToCall(methodName);
        var instance = GetControllerInstance(methodInfo);

        var methodDelegate = methodInfo.CreateDelegate<Func<Task>>(instance);
        var task = methodDelegate();
        
        await task;
        return await GetReturnValue(task);
    }

    private static async Task<byte[]> GetReturnValue(Task task)
    {
        byte[] value = Array.Empty<byte>();

        var taskType = task.GetType();
       
        var resultProperty = taskType.GetProperty("Result");
        
        if (resultProperty != null)
        {
            var result = resultProperty.GetValue(task);

            using MemoryStream stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, result);
            value = stream.ToArray();
        }

        return value;
    }

    private object? GetControllerInstance(MethodInfo methodInfo)
    {
        var declaringType = methodInfo.DeclaringType!;
        var instance = _serviceProvider.GetService(declaringType);
        return instance;
    }

    private MethodInfo GetMethodToCall(string methodName)
    {
        if (!_methods.ContainsKey(methodName))
        {
            throw new UnknownControllerMethodException();
        }

        var methodInfo = _methods[methodName];
        return methodInfo;
    }
}