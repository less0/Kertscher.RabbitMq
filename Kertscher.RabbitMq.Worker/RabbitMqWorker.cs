using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Kertscher.RabbitMq.Worker;

public class RabbitMqWorker : IHostedService
{
    private readonly ILogger<RabbitMqWorker> _logger;
    private readonly string? _hostName;
    private IConnection? _connection;

    public RabbitMqWorker(ILogger<RabbitMqWorker> logger, IConfiguration configuration)
    {
        _logger = logger;

        _hostName = configuration.GetRequiredSection("RabbitMQ")
            .GetValue<string>("Host");
    }

    protected bool IsConnected => _connection?.IsOpen ?? false;
    
    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        await ConnectWithRetryAsync(cancellationToken);
        await OnConnected(false);
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        _connection?.Dispose();
        return Task.CompletedTask;
    }

    protected virtual Task OnConnected(bool reconnected)
    {
        return Task.CompletedTask;
    }

    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromMinutes(5);
        var startedAt = DateTime.Now;
        
        _logger.LogInformation("Connecting...");

        while (!cancellationToken.IsCancellationRequested
               && !IsConnected)
        {
            try
            {
                ConnectionFactory connectionFactory = new()
                {
                    HostName = _hostName
                };
                _connection = connectionFactory.CreateConnection();
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _logger.LogInformation("Connected.");
            }
            catch (BrokerUnreachableException)
            {
                if (DateTime.Now - startedAt > timeout)
                {
                    _logger.LogCritical("Connection timed out.");
                    throw new TimeoutException();
                }
                
                _logger.LogWarning("Connection failed. Retrying in 5 s.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        if (e.Initiator != ShutdownInitiator.Application)
        {
            Task.Run(async () =>
            {
                await ConnectWithRetryAsync(new());
                await OnConnected(true);
            });
        }
    }
}
