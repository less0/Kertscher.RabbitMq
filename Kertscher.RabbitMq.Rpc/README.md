# Kertscher.RabbitMq.Rpc

This is my personal approach to RPC with RabbitMQ. Just in case it's of use to anyone, here are some notes on the usage. 

The library is highly opinionated and designed to be used with `Microsoft.Extensions.Hosting` in a hosted application.

## Setting up the host builder

To use the `RpcServer` class with a host builder, one has to call the `AddRpc` extension for `IServiceCollection`

```csharp
using Kertscher.RabbitMqRpc();

var hostBuilder = Host.CreateApplicationBuilder();
hostBuilder.Services.AddRpc();
```

## Configuration

The `RpcServer` class requires the configuration values `Exchange` and `Host` in the configuration section `RabbitMQ`. 
Easiest is to create an `appsettings.json` (or extend an existing one) with the respective section

```json
{
  "RabbitMQ":
  {
    "Exchange": "RPC",
    "Host": "localhost"
  }
}
```

The value for `Exchange` can be chosen freely, but has to be the same for the respective clients. `Host` has to point to the respective host, RabbitMQ is running on. At the moment authentication is not implemented, therefor it only works with servers that allow anonymous connections.

## Adding controllers

In order to react to RPCs, controllers can be registered with the service collection *and* the server.

```csharp
using Kertscher.RabbitMqRpc();

var hostBuilder = Host.CreateApplicationBuilder();
hostBuilder.Services.AddRpc();
hostBuilder.Services.AddSingleton<MyController>();

var host = bostBuilder.Build();
var server = host.Services.GetRequiredService<RpcServer>();
server.RegisterController<MyController>();
```

### Controller methods

In order to work with the RPC server, controller methods have to suffice these conditions

- They have to return a `Task` or `Task<T>`
- The result value has to be able to be serialized to JSON
- They have to have either no or a single parameter
  - In the latter case the parameter has to be serializable from JSON