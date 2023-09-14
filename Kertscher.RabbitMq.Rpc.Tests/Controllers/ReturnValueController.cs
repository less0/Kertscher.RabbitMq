namespace Kertscher.RabbitMq.Rpc.Tests.Controllers;

public class ReturnValueController
{
    public Task<ReturnValue> MethodWithResult()
    {
        return Task.FromResult(new ReturnValue()
        {
            AProperty = "Hello, world!"
        });
    }
}