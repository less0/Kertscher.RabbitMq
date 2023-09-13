using FluentAssertions;
using Xunit;

namespace Kertscher.RabbitMq.Rpc.Tests;

[Collection(nameof(RabbitMqServer))]
public class RpcClientBaseTests
{
    [Fact]
    public async Task CallMethod_CancellationTokenTimesOut_ThrowsTaskCanceledException()
    {
        var componentUnderTest = new RpcClient("localhost", "test");
        var exception = await Record.ExceptionAsync(async () => await componentUnderTest.GetSomething());
        exception.Should().BeOfType<TaskCanceledException>();
    }
}