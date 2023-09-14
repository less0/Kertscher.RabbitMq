namespace Kertscher.RabbitMq.Rpc;

internal interface IControllerResolver
{
    void RegisterController<T>(out string[] registeredMethods);
    Task<byte[]> CallMethodAsync(string methodName, byte[] data);
}