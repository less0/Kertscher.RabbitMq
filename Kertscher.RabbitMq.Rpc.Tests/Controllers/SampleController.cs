namespace Kertscher.RabbitMq.Rpc.Tests.Controllers;

public class SampleController
{
    public Task<SampleReturnValue> AnyMethod()
    {
        Calls++;
        return Task.FromResult(new SampleReturnValue());
    }

    public int Calls { get; private set; }
}