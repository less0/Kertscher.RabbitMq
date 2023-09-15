namespace Kertscher.RabbitMq.Rpc;

public interface IControllerResolver
{
    void RegisterController<T>(out string[] registeredMethods);
    Task<byte[]> CallMethodAsync(string methodName, byte[] data);
}