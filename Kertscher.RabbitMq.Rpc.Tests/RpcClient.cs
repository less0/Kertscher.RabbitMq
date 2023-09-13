namespace Kertscher.RabbitMq.Rpc.Tests;

public class RpcClient : RpcClientBase
{
    private readonly int _callTimeoutInSeconds;

    public RpcClient(string hostName, string exchangeName, int timeoutInSeconds = 300, int callTimeoutInSeconds = 5) 
        : base(hostName, exchangeName, timeoutInSeconds)
    {
        _callTimeoutInSeconds = callTimeoutInSeconds;
    }

    public async Task<byte[]> GetSomething()
    {
        CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(_callTimeoutInSeconds));
        return await CallMethod("getSomething", Array.Empty<byte>(), cancellationTokenSource.Token);
    }
}