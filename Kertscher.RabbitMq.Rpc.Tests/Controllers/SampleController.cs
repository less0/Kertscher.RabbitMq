namespace Kertscher.RabbitMq.Rpc.Tests.Controllers;

public class SampleController
{
    public Task<byte[]> AnyMethod()
    {
        Calls++;
        return Task.FromResult(Array.Empty<byte>());
    }

    public int Calls { get; private set; }
}