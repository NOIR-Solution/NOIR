using NOIR.Infrastructure.Audit;

namespace NOIR.Application.UnitTests.Audit;

/// <summary>
/// Unit tests for BeforeStateRegistrationHostedService.
/// Tests that resolver registrations are properly applied at startup.
/// </summary>
public class BeforeStateRegistrationHostedServiceTests
{
    private readonly Mock<ILogger<BeforeStateRegistrationHostedService>> _loggerMock;
    private readonly Mock<ILogger<WolverineBeforeStateProvider>> _providerLoggerMock;

    public BeforeStateRegistrationHostedServiceTests()
    {
        _loggerMock = new Mock<ILogger<BeforeStateRegistrationHostedService>>();
        _providerLoggerMock = new Mock<ILogger<WolverineBeforeStateProvider>>();
    }

    [Fact]
    public async Task StartAsync_WithRegistrations_ShouldApplyAll()
    {
        // Arrange
        // Create a properly mocked service provider that returns empty registrations for lazy init
        var innerServiceProviderMock = new Mock<IServiceProvider>();
        innerServiceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IBeforeStateResolverRegistration>)))
            .Returns(Array.Empty<IBeforeStateResolverRegistration>());

        var provider = new WolverineBeforeStateProvider(
            innerServiceProviderMock.Object,
            _providerLoggerMock.Object);

        var registrations = new List<IBeforeStateResolverRegistration>
        {
            new TestResolverRegistration<TestDto1>(),
            new TestResolverRegistration<TestDto2>()
        };

        var services = new ServiceCollection();
        services.AddSingleton<IBeforeStateProvider>(provider);
        foreach (var reg in registrations)
        {
            services.AddSingleton(reg);
        }

        var serviceProvider = services.BuildServiceProvider();
        var sut = new BeforeStateRegistrationHostedService(serviceProvider, _loggerMock.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert - Both DTOs should now have resolvers
        var result1 = await provider.GetBeforeStateAsync(typeof(TestDto1), "1", CancellationToken.None);
        var result2 = await provider.GetBeforeStateAsync(typeof(TestDto2), "2", CancellationToken.None);

        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_WithNoProvider_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        // No IBeforeStateProvider registered
        var serviceProvider = services.BuildServiceProvider();
        var sut = new BeforeStateRegistrationHostedService(serviceProvider, _loggerMock.Object);

        // Act
        var act = async () => await sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_WithNoRegistrations_ShouldNotThrow()
    {
        // Arrange
        var innerServiceProviderMock = new Mock<IServiceProvider>();
        innerServiceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IBeforeStateResolverRegistration>)))
            .Returns(Array.Empty<IBeforeStateResolverRegistration>());

        var provider = new WolverineBeforeStateProvider(
            innerServiceProviderMock.Object,
            _providerLoggerMock.Object);

        var services = new ServiceCollection();
        services.AddSingleton<IBeforeStateProvider>(provider);
        // No registrations

        var serviceProvider = services.BuildServiceProvider();
        var sut = new BeforeStateRegistrationHostedService(serviceProvider, _loggerMock.Object);

        // Act
        var act = async () => await sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_WhenRegistrationThrows_ShouldContinueWithOthers()
    {
        // Arrange
        var innerServiceProviderMock = new Mock<IServiceProvider>();
        innerServiceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IBeforeStateResolverRegistration>)))
            .Returns(Array.Empty<IBeforeStateResolverRegistration>());

        var provider = new WolverineBeforeStateProvider(
            innerServiceProviderMock.Object,
            _providerLoggerMock.Object);

        var services = new ServiceCollection();
        services.AddSingleton<IBeforeStateProvider>(provider);
        services.AddSingleton<IBeforeStateResolverRegistration>(new ThrowingRegistration());
        services.AddSingleton<IBeforeStateResolverRegistration>(new TestResolverRegistration<TestDto1>());

        var serviceProvider = services.BuildServiceProvider();
        var sut = new BeforeStateRegistrationHostedService(serviceProvider, _loggerMock.Object);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert - The good registration should still work
        var result = await provider.GetBeforeStateAsync(typeof(TestDto1), "1", CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteImmediately()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var sut = new BeforeStateRegistrationHostedService(serviceProvider, _loggerMock.Object);

        // Act
        var act = async () => await sut.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #region Test Helpers

    private sealed record TestDto1(string Id);
    private sealed record TestDto2(string Id);

    private sealed class TestResolverRegistration<TDto> : IBeforeStateResolverRegistration
        where TDto : class
    {
        public void Register(WolverineBeforeStateProvider provider)
        {
            provider.Register<TDto>(async (sp, id, ct) =>
            {
                // Return a mock DTO using reflection to create instance
                var constructor = typeof(TDto).GetConstructors().First();
                var parameters = constructor.GetParameters()
                    .Select(p => p.ParameterType == typeof(string) ? id.ToString() : null)
                    .ToArray();
                return (TDto)constructor.Invoke(parameters);
            });
        }
    }

    private sealed class ThrowingRegistration : IBeforeStateResolverRegistration
    {
        public void Register(WolverineBeforeStateProvider provider)
        {
            throw new InvalidOperationException("Simulated registration failure");
        }
    }

    #endregion
}
