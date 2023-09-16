using Kertscher.RabbitMq.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Kertscher.RabbitMq.Rpc;

public class RpcServer : RabbitMqWorker
{
    private readonly IControllerResolver _controllerResolver;
    private readonly List<string> _registeredMethods = new();
    private IModel? _channel;
    private readonly string? _exchangeName;
    private string _queueName;
    private EventingBasicConsumer _consumer;

    public RpcServer(ILogger<RpcServer> logger, 
        IConfiguration configuration, 
        IControllerResolver controllerResolver) 
        : base(logger, configuration)
    {
        _controllerResolver = controllerResolver;
        _exchangeName = configuration.GetValue<string>("RabbitMQ:Exchange");
    }

    public void RegisterController<TController>()
    {
        _controllerResolver.RegisterController<TController>(out var registeredMethods);
        _registeredMethods.AddRange(registeredMethods);

        if (IsConnected)
        {
            SubscribeMethodRoutes(registeredMethods);
        }
    }

    protected override Task OnConnected(IConnection connection, bool reconnected)
    {
        _channel = connection.CreateModel();
        _queueName = _channel.QueueDeclare().QueueName;
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);

        _consumer = new(_channel);
        _consumer.Received += ConsumerOnReceived;
        _channel.BasicConsume(_consumer, _queueName);
        
        SubscribeMethodRoutes(_registeredMethods);
        
        return Task.CompletedTask;
    }

    private void ConsumerOnReceived(object? sender, BasicDeliverEventArgs e)
    {
        Task.Run(async () => await OnReceivedAsync(e));
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs e)
    {
        var result = await _controllerResolver.CallMethodAsync(e.RoutingKey, e.Body.ToArray());
        ReturnResult(e.BasicProperties.CorrelationId, e.BasicProperties.ReplyTo, result);
    }

    private void ReturnResult(string correlationId, string replyTo, byte[] result)
    {
        var returnProperties = _channel.CreateBasicProperties();
        returnProperties.CorrelationId = correlationId;
        _channel.BasicPublish(string.Empty, replyTo, returnProperties, result);
    }

    private void SubscribeMethodRoutes(IEnumerable<string> registeredMethods)
    {
        foreach (var methodName in registeredMethods)
        {
            _channel.QueueBind(_queueName, _exchangeName, methodName);
        }
    }
}