using System.Diagnostics;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace Kertscher.RabbitMq.Rpc.Tests;

[CollectionDefinition(nameof(RabbitMqServer))]
public class RabbitMqServer : IDisposable, ICollectionFixture<RabbitMqServer>
{
    public RabbitMqServer()
    {
        StopAndRemoveContainer();
        StartServer();
        WaitForConnection();
    }

    public void Dispose()
    {
        StopAndRemoveContainer();
    }

    private static void StartServer()
    {
        Process.Start(new ProcessStartInfo("docker")
        {
            Arguments = "run --name rabbit_testing -p 5672:5672 rabbitmq:latest"
        });
    }

    private static void StopAndRemoveContainer()
    {
        var process = Process.Start(new ProcessStartInfo("docker")
        {
            Arguments = "stop rabbit_testing"
        });
        process.WaitForExit();

        process = Process.Start(new ProcessStartInfo("docker")
        {
            Arguments = "rm rabbit_testing"
        });
        process.WaitForExit();
    }

    private void WaitForConnection()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();
        
        IConnection? connection = null;
        
        Console.WriteLine("Connecting...");
        
        while (connection == null)
        {
            try
            {
                ConnectionFactory connectionFactory = new()
                {
                    HostName = "localhost"
                };
                connection = connectionFactory.CreateConnection();
                Console.WriteLine("Connected.");
            }
            catch (BrokerUnreachableException)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException("Connection timed out.");
                }
                
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }
    }
}