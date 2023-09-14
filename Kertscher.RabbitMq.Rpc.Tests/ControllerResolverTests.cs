using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentAssertions;
using Kertscher.RabbitMq.Rpc.Tests.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kertscher.RabbitMq.Rpc.Tests;

/// <summary>
/// Tests for <see cref="ControllerResolver"/>.
/// </summary>
public class ControllerResolverTests
{
    private readonly ServiceCollection _serviceCollection = new();
    private ServiceProvider? _serviceProvider;

    [Fact]
    public async Task CallMethod_ControllerIsNotRegistered_ThrowsUnknownControllerMethodException()
    {
        ControllerResolver componentUnderTest = CreateControllerResolver();
        var exception = await Record.ExceptionAsync(async () =>
            await componentUnderTest.CallMethodAsync("AnyMethod", Array.Empty<byte>()));
        exception.Should().BeOfType<UnknownControllerMethodException>();
    }

    [Fact]
    public async Task CallMethod_ControllerIsRegistered_DoesNotThrow()
    {
        _serviceCollection.AddSingleton<SampleController>();
        
        ControllerResolver componentUnderTest = CreateControllerResolver();
        componentUnderTest.RegisterController<SampleController>();
        var exception =
            await Record.ExceptionAsync(() => componentUnderTest.CallMethodAsync("AnyMethod", Array.Empty<byte>()));
        exception.Should().BeNull();
    }

    [Fact]
    public async Task CallMethod_ControllerMethodIsCalled()
    {
        _serviceCollection.AddSingleton<SampleController>();

        ControllerResolver componentUnderTest = CreateControllerResolver();
        componentUnderTest.RegisterController<SampleController>();
        
        await componentUnderTest.CallMethodAsync("AnyMethod", Array.Empty<byte>());
        
        var controllerInstance = _serviceProvider.GetRequiredService<SampleController>();
        controllerInstance.Calls.Should().Be(1);
    }

    [Fact]
    public async Task CallMethod_ReturnValueIsSerialized()
    {
        _serviceCollection.AddSingleton<ReturnValueController>();

        var componentUnderTest = CreateControllerResolver();
        componentUnderTest.RegisterController<ReturnValueController>();

        var result = await componentUnderTest.CallMethodAsync("MethodWithResult", Array.Empty<byte>());

        var resultAsText = Encoding.UTF8.GetString(result);
        resultAsText.Should().Be("{\"AProperty\":\"Hello, world!\"}");
    }
    
    [MemberNotNull(nameof(_serviceProvider))]
    private ControllerResolver CreateControllerResolver()
    {
        _serviceProvider = _serviceCollection.BuildServiceProvider();
        return new ControllerResolver(_serviceProvider);
    }
}