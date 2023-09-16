using FluentAssertions;
using Kertscher.RabbitMq.Rpc.Tests.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kertscher.RabbitMq.Rpc.Tests;

[Collection(nameof(RabbitMqServer))]
public class RpcIntegrationTests
{
    private readonly IHost _host;
    private const string ExchangeName = "RPC";

    public RpcIntegrationTests()
    {
        var applicationBuilder = Host.CreateApplicationBuilder();

        applicationBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RabbitMQ:Host"] = "localhost",
            ["RabbitMQ:Exchange"] = ExchangeName
        });
        
        applicationBuilder.Services.AddRpc();
        applicationBuilder.Services.AddSingleton<ControllerWithMultipleMethods>();
        applicationBuilder.Services.AddSingleton<MethodParameterController>();
        applicationBuilder.Services.AddSingleton<ReturnValueController>();
        
        _host = applicationBuilder.Build();
        _host.Start();
    }

    [Fact]
    public async Task MethodWithReturnValue_CorrectValueIsReturned()
    {
        RpcServer server = _host.Services.GetRequiredService<RpcServer>();
        server.RegisterController<ReturnValueController>();
        FullRpcClient client = new("localhost", ExchangeName);
        
        var result = await client.MethodWithResult();
        
        result.Should().NotBeNull();
        result!.AProperty.Should().Be("Hello, world!");
    }

    [Fact]
    public async Task MethodWithParameter_ParameterIsPassedCorrectly()
    {
        RpcServer server = _host.Services.GetRequiredService<RpcServer>();
        server.RegisterController<MethodParameterController>();
        FullRpcClient client = new("localhost", ExchangeName);

        await client.MethodWithParameter(new Parameter()
        {
            AString = "FooBar",
            AnInteger = 4321
        });

        var controller = _host.Services.GetRequiredService<MethodParameterController>();
        controller.LastParameter.Should().NotBeNull();
        controller.LastParameter!.AString.Should().Be("FooBar");
        controller.LastParameter.AnInteger.Should().Be(4321);
    }
}