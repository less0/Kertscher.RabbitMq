using System.Text.Json;
using Kertscher.RabbitMq.Rpc.Tests.Controllers;
using NSubstitute.Core;
using ReturnValue = Kertscher.RabbitMq.Rpc.Tests.Controllers.ReturnValue;

namespace Kertscher.RabbitMq.Rpc.Tests;

public class FullRpcClient : RpcClientBase
{
    public FullRpcClient(string hostName, string exchangeName, int timeoutInSeconds = 300) 
        : base(hostName, exchangeName, timeoutInSeconds)
    {
    }

    public async Task<ReturnValue?> MethodWithResult()
    {
        CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(1));
        var result = await CallMethod(nameof(MethodWithResult), Array.Empty<byte>(), cancellationTokenSource.Token);
        return JsonSerializer.Deserialize<ReturnValue>(result);
    }

    public async Task MethodWithParameter(Parameter parameter)
    {
        CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(1));
        await CallMethod(nameof(MethodWithParameter), JsonSerializer.SerializeToUtf8Bytes(parameter),
            cancellationTokenSource.Token);
    }
}