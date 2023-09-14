namespace Kertscher.RabbitMq.Rpc.Tests.Controllers;

public class SampleController
{
    public Task AnyMethod()
    {
        Calls++;
        return Task.CompletedTask;
    }

    public int Calls { get; private set; }
}