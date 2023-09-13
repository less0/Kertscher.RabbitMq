using System.Text;
using FluentAssertions;
using Xunit;

namespace Kertscher.RabbitMq.Rpc.Tests;

[Collection(nameof(RabbitMqServer))]
public class RpcClientBaseTests
{
    [Fact]
    public async Task CallMethod_CancellationTokenTimesOut_ThrowsTaskCanceledException()
    {
        using RpcClient componentUnderTest = new("localhost", "test");
        var exception = await Record.ExceptionAsync(async () => await componentUnderTest.GetSomething());
        exception.Should().BeOfType<TaskCanceledException>();
    }

    [Fact]
    public async Task CallMethod_ReceivesResult_ResultIsReturned()
    {
        using RpcClient componentUnderTest = new("localhost", "test");
        using RpcServer server = new("localhost", "test");

        var response = await componentUnderTest.GetSomething();
        var responseText = Encoding.UTF8.GetString(response);

        responseText.Should().Be("Something");
    }
}