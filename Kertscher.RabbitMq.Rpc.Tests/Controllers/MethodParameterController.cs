namespace Kertscher.RabbitMq.Rpc.Tests.Controllers;

public class MethodParameterController
{
    public Task MethodWithParameter(Parameter parameter)
    {
        LastParameter = parameter;
        return Task.CompletedTask;
    }

    public Parameter? LastParameter { get; set; }
}