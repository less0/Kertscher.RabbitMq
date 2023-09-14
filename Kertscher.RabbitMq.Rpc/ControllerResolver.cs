using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace Kertscher.RabbitMq.Rpc;

internal class ControllerResolver : IControllerResolver
{
    private readonly Dictionary<string, MethodInfo> _methods = new();
    private readonly IServiceProvider _serviceProvider;

    public ControllerResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void RegisterController<T>(out string[] registeredMethods)
    {
        var type = typeof(T);

        var methods = from method in type.GetMethods()
                      where method.ReturnType.IsAssignableTo(typeof(Task))
                      where method.DeclaringType != null
                      select new { method.Name, MethodInfo = method };

        List<string> registeredMethodNames = new();
        foreach (var method in methods)
        {
            _methods.Add(method.Name, method.MethodInfo);
            registeredMethodNames.Add(method.Name);
        }

        registeredMethods = registeredMethodNames.ToArray();
    }

    public async Task<byte[]> CallMethodAsync(string methodName, byte[] data)
    {
        var methodInfo = GetMethodToCall(methodName);
        var instance = GetControllerInstance(methodInfo);

        var task = CallMethod(methodInfo, instance, data);
        await task;

        return await GetReturnValueAsync(task);
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

    private static Task CallMethod(MethodInfo methodInfo, object? instance, byte[] data)
    {
        var parameters = methodInfo.GetParameters();

        if (parameters.Length == 0)
        {
            return CallMethodWithoutParameter(methodInfo, instance);
        }

        return CallMethodWithParameter(methodInfo, instance, data, parameters);
    }

    private static Task CallMethodWithParameter(MethodInfo methodInfo, object? instance, byte[] data,
        ParameterInfo[] parameters)
    {
        var parameterType = parameters[0].ParameterType;
        var parameterValue = JsonSerializer.Deserialize(data, parameterType);

        if (methodInfo.Invoke(instance, new[] { parameterValue }) is not Task task)
        {
            throw new InvalidOperationException("All controller methods have to return Task values.");
        }

        return task;
    }

    private static Task CallMethodWithoutParameter(MethodInfo methodInfo, object? instance)
    {
        var methodDelegate = methodInfo.CreateDelegate<Func<Task>>(instance);
        return methodDelegate();
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
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, result);
        return stream.ToArray();
    }
}