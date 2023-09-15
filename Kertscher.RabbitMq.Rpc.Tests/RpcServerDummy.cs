using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Kertscher.RabbitMq.Rpc.Tests;

public class RpcServerDummy : IDisposable
{
    private IConnection? _connection;
    private IModel? _channel;
    private EventingBasicConsumer? _consumer;
    private readonly string? _queueName;

    public RpcServerDummy(string hostName, string exchange)
    {
        ConnectionFactory connectionFactory = new()
        {
            HostName = hostName
        };
        _connection = connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(exchange, ExchangeType.Direct);
        _queueName = _channel.QueueDeclare().QueueName;
        
        _channel.QueueBind(_queueName, exchange, "getSomething");

        _consumer = new(_channel);
        _consumer.Received += ConsumerOnReceived;
        _channel.BasicConsume(_consumer, _queueName, autoAck: false);
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
        _channel?.Dispose();

        if (_consumer != null)
        {
            _consumer.Received -= ConsumerOnReceived;
        }
    }

    private void ConsumerOnReceived(object? sender, BasicDeliverEventArgs e)
    {
        var basicProperties = _channel.CreateBasicProperties();
        basicProperties.CorrelationId = e.BasicProperties.CorrelationId;

        var message = Encoding.UTF8.GetBytes("Something");
        _channel.BasicPublish(string.Empty, e.BasicProperties.ReplyTo, basicProperties, message);
        
        _channel.BasicAck(e.DeliveryTag, false);
    }
}