namespace Kertscher.RabbitMq.Rpc.Tests.Controllers;

public class ControllerWithMultipleMethods
{
    public Task FirstMethod()
    {
        return Task.CompletedTask;
    }

    public Task SecondMethod(Parameter parameter)
    {
        return Task.CompletedTask;
    }
}