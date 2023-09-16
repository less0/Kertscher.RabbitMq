using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Kertscher.RabbitMq.Rpc.Tests.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Kertscher.RabbitMq.Rpc.Tests;

/// <summary>
/// Tests for <see cref="RpcServer"/>.
/// </summary>
[Collection(nameof(RabbitMqServer))]
public class RpcServerTests
{
    private readonly IControllerResolver _controllerResolver;
    private readonly string _exchangeName;
    private readonly RpcServer _componentUnderTest;
    private IConnection? _connection;
    private IModel? _channel;
    private string? _replyQueueName;

    public RpcServerTests()
    {
        _controllerResolver = Substitute.For<IControllerResolver>();
        ILogger<RpcServer> logger = Substitute.For<ILogger<RpcServer>>();

        ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
        _exchangeName = "RPC";
        configurationBuilder.Sources.Add(new MemoryConfigurationSource
        {
            InitialData = new Dictionary<string, string?>
            {
                ["RabbitMQ:Host"] = "localhost",
                ["RabbitMQ:Exchange"] = _exchangeName
            }
        });
        var configuration = configurationBuilder.Build();

        _componentUnderTest = new(logger, configuration, _controllerResolver);
    }

    [Fact]
    public async Task MethodCallIsDelegatedToControllerResolver()
    {
        await _componentUnderTest.StartAsync(default);
        
        RegisterController<SampleController>("AnyMethod");
        ConnectRabbitMq();
        CallMethod("AnyMethod");

        await Task.Delay(100);
        
        await _controllerResolver.Received()
            .CallMethodAsync(Arg.Is("AnyMethod"), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task ServerReturnsReturnValue()
    {
        var returnedData = new byte[1024];
        Random.Shared.NextBytes(returnedData);
        
        await _componentUnderTest.StartAsync(default);
        _controllerResolver.CallMethodAsync("AnyMethod", Arg.Any<byte[]>())
            .Returns(returnedData);
        
        RegisterController<SampleController>("AnyMethod");
        ConnectRabbitMq();
        var correlationId = CallMethod("AnyMethod");

        CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(1));
        var result = await WaitForResult(correlationId, cancellationTokenSource.Token);

        result.Should().BeEquivalentTo(returnedData, o => o.WithStrictOrdering());
    }

    private async Task<byte[]> WaitForResult(string correlationId, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<byte[]> taskCompletionSource = new();
        EventingBasicConsumer eventingBasicConsumer = new EventingBasicConsumer(_channel);

        eventingBasicConsumer.Received += (_, args) =>
        {
            if (args.BasicProperties.CorrelationId == correlationId)
            {
                taskCompletionSource.SetResult(args.Body.ToArray());
            }
        };
        
        _channel.BasicConsume(eventingBasicConsumer, _replyQueueName, true);
        cancellationToken.Register(taskCompletionSource.SetCanceled);
        
        return await taskCompletionSource.Task;
    }

    private void RegisterController<TController>(params string[] methodNames)
    {
        _controllerResolver.When(c => c.RegisterController<TController>(out Arg.Any<string[]>()))
            .Do(x => { x[0] = methodNames; });
        
        _componentUnderTest.RegisterController<TController>();
    }

    private string CallMethod(string methodName)
    {
        var properties = _channel.CreateBasicProperties();
        properties.ReplyTo = _replyQueueName;
        var correlationId = Guid.NewGuid().ToString();
        properties.CorrelationId = correlationId;

        _channel.BasicPublish(_exchangeName, methodName, properties, Array.Empty<byte>());

        return correlationId;
    }

    [MemberNotNull(nameof(_connection), nameof(_channel), nameof(_replyQueueName))]
    private void ConnectRabbitMq()
    {
        ConnectionFactory connectionFactory = new()
        {
            HostName = "localhost"
        };
        
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
        _replyQueueName = _channel.QueueDeclare().QueueName;
    }
}