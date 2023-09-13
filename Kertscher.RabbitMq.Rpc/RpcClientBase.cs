using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Kertscher.RabbitMq.Rpc;

public class RpcClientBase : IDisposable
{
    private readonly string _hostName;
    private readonly string _exchangeName;
    private readonly int _timeoutInSeconds;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _registeredCalls = new();
    private IConnection? _connection;
    private IModel? _channel;
    private string? _responseQueueName;
    private EventingBasicConsumer? _consumer;

    public RpcClientBase(string hostName, string exchangeName, int timeoutInSeconds = 300)
    {
        _hostName = hostName;
        _exchangeName = exchangeName;
        _timeoutInSeconds = timeoutInSeconds;
    }

    [MemberNotNullWhen(true, nameof(_connection))]
    private bool IsConnected => _connection?.IsOpen ?? false;

    protected async Task<byte[]> CallMethod(string methodName, byte[] data, CancellationToken cancellationToken = default)
    {
        await EnsureIsConnectedAsync(cancellationToken);
        
        var basicProperties = _channel.CreateBasicProperties();
        basicProperties.CorrelationId = Guid.NewGuid().ToString();
        basicProperties.ReplyTo = _responseQueueName;

        TaskCompletionSource<byte[]> completionSource = new();
        cancellationToken.Register(() => CancelCall(basicProperties.CorrelationId));
        _registeredCalls.TryAdd(basicProperties.CorrelationId, completionSource);
        
        _channel.BasicPublish(_exchangeName, methodName, basicProperties, data);
        
        return await completionSource.Task;
    }

    private void CancelCall(string correlationId)
    {
        if (_registeredCalls.TryRemove(correlationId, out var taskCompletionSource))
        {
            taskCompletionSource.SetCanceled();
        }
    }

    [MemberNotNull(nameof(_channel), nameof(_connection))]
    private async Task EnsureIsConnectedAsync(CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (!cancellationToken.IsCancellationRequested
               && !IsConnected)
        {
            try
            {
                CleanupConnection();
                
                ConnectionFactory connectionFactory = new()
                {
                    HostName = _hostName
                };
                _connection = connectionFactory.CreateConnection();
                _channel = _connection.CreateModel();
                
                _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
                _responseQueueName = _channel.QueueDeclare().QueueName;

                _consumer = new(_channel);
                _consumer.Received += ConsumerOnReceived;
                _channel.BasicConsume(_consumer, _responseQueueName, true);
            }
            catch (BrokerUnreachableException)
            {
                if (stopwatch.Elapsed.TotalSeconds > _timeoutInSeconds)
                {
                    throw new TimeoutException("Connection timed out.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        if (_connection == null
            || _channel == null)
        {
            throw new TaskCanceledException();
        }
    }

    public void Dispose()
    {
        CleanupConnection();
    }

    private void CleanupConnection()
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
        if (!_registeredCalls.TryRemove(e.BasicProperties.CorrelationId, out var taskCompletionSource))
        {
            return;
        }

        taskCompletionSource.TrySetResult(e.Body.ToArray());
    }
}
