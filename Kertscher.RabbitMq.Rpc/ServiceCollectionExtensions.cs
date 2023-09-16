using Microsoft.Extensions.DependencyInjection;

namespace Kertscher.RabbitMq.Rpc;

public static class ServiceCollectionExtensions
{
    public static void AddRpc(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IControllerResolver, ControllerResolver>();

        serviceCollection.AddSingleton<RpcServer>();
        serviceCollection.AddHostedService<RpcServer>(provider => provider.GetRequiredService<RpcServer>());
    }
}