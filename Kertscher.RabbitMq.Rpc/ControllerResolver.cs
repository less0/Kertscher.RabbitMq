using System.Diagnostics.CodeAnalysis;
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

        var task = CallMethod(methodInfo, instance); 
        await task;

        return await GetReturnValueAsync(task);
    }

    private static Task CallMethod(MethodInfo methodInfo, object? instance)
    {
        var methodDelegate = methodInfo.CreateDelegate<Func<Task>>(instance);
        var task = methodDelegate();
        return task;
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

    private object? GetControllerInstance(MethodInfo methodInfo)
    {
        var declaringType = methodInfo.DeclaringType!;
        var instance = _serviceProvider.GetService(declaringType);
        return instance;
    }

    private static async Task<byte[]> GetReturnValueAsync(Task task)
    {
        if (TryGetResult(task, out var result))
        {
            return await SerializeToByteArrayAsync(result);
        }

        return Array.Empty<byte>();
    }

    private static bool TryGetResult(Task task, [NotNullWhen(true)] out object? result)
    {
        var taskType = task.GetType();
        var resultProperty = taskType.GetProperty("Result");
        result = resultProperty?.GetValue(task);

        return result != null;
    }

    private static async Task<byte[]> SerializeToByteArrayAsync(object result)
    {
        using MemoryStream stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, result);
        return stream.ToArray();
    }
}