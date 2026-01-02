using NOIR.Infrastructure.Audit;

namespace NOIR.Application.UnitTests.Audit;

/// <summary>
/// Unit tests for WolverineBeforeStateProvider.
/// Tests resolver registration, before-state fetching, and error handling.
/// </summary>
public class WolverineBeforeStateProviderTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<WolverineBeforeStateProvider>> _loggerMock;
    private readonly WolverineBeforeStateProvider _sut;

    public WolverineBeforeStateProviderTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<WolverineBeforeStateProvider>>();

        // Mock GetServices<IBeforeStateResolverRegistration> to return empty collection
        // This is required because EnsureInitialized() is called on first use
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IEnumerable<IBeforeStateResolverRegistration>)))
            .Returns(Array.Empty<IBeforeStateResolverRegistration>());

        _sut = new WolverineBeforeStateProvider(_serviceProviderMock.Object, _loggerMock.Object);
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidResolver_ShouldEnableFetching()
    {
        // Arrange
        var expectedDto = new TestDto("123", "Test Name");
        _sut.Register<TestDto>(async (sp, id, ct) => expectedDto);

        // Act
        var result = await _sut.GetBeforeStateAsync(typeof(TestDto), "123", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TestDto>();
        ((TestDto)result!).Id.Should().Be("123");
        ((TestDto)result).Name.Should().Be("Test Name");
    }

    [Fact]
    public async Task Register_WithNullResolver_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => _sut.Register<TestDto>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Register_CalledMultipleTimes_ShouldOverwritePreviousResolver()
    {
        // Arrange
        var firstDto = new TestDto("1", "First");
        var secondDto = new TestDto("2", "Second");

        _sut.Register<TestDto>(async (sp, id, ct) => firstDto);
        _sut.Register<TestDto>(async (sp, id, ct) => secondDto);

        // Act
        var result = await _sut.GetBeforeStateAsync(typeof(TestDto), "any", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        ((TestDto)result!).Name.Should().Be("Second");
    }

    #endregion

    #region GetBeforeStateAsync Tests

    [Fact]
    public async Task GetBeforeStateAsync_WithNoResolver_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetBeforeStateAsync(typeof(TestDto), "123", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBeforeStateAsync_WithNullDtoType_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetBeforeStateAsync(null!, "123", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBeforeStateAsync_WithNullTargetId_ShouldReturnNull()
    {
        // Arrange
        _sut.Register<TestDto>(async (sp, id, ct) => new TestDto(id.ToString()!, "Test"));

        // Act
        var result = await _sut.GetBeforeStateAsync(typeof(TestDto), null!, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBeforeStateAsync_WhenResolverThrows_ShouldReturnNull()
    {
        // Arrange
        _sut.Register<TestDto>(async (sp, id, ct) => throw new InvalidOperationException("Simulated error"));

        // Act
        var result = await _sut.GetBeforeStateAsync(typeof(TestDto), "123", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBeforeStateAsync_WhenResolverReturnsNull_ShouldReturnNull()
    {
        // Arrange
        _sut.Register<TestDto>(async (sp, id, ct) => null);

        // Act
        var result = await _sut.GetBeforeStateAsync(typeof(TestDto), "123", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBeforeStateAsync_WhenCancelled_ShouldReturnNull()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _sut.Register<TestDto>(async (sp, id, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return new TestDto("123", "Test");
        });

        // Act
        var result = await _sut.GetBeforeStateAsync(typeof(TestDto), "123", cts.Token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBeforeStateAsync_WithGuidId_ShouldPassCorrectId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        object? capturedId = null;

        _sut.Register<TestDto>(async (sp, id, ct) =>
        {
            capturedId = id;
            return new TestDto(id.ToString()!, "Test");
        });

        // Act
        await _sut.GetBeforeStateAsync(typeof(TestDto), expectedId, CancellationToken.None);

        // Assert
        capturedId.Should().Be(expectedId);
    }

    [Fact]
    public async Task GetBeforeStateAsync_WithStringId_ShouldPassCorrectId()
    {
        // Arrange
        const string expectedId = "user-123";
        object? capturedId = null;

        _sut.Register<TestDto>(async (sp, id, ct) =>
        {
            capturedId = id;
            return new TestDto(id.ToString()!, "Test");
        });

        // Act
        await _sut.GetBeforeStateAsync(typeof(TestDto), expectedId, CancellationToken.None);

        // Assert
        capturedId.Should().Be(expectedId);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task GetBeforeStateAsync_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var callCount = 0;
        _sut.Register<TestDto>(async (sp, id, ct) =>
        {
            Interlocked.Increment(ref callCount);
            await Task.Delay(10, ct); // Simulate async work
            return new TestDto(id.ToString()!, "Test");
        });

        // Act
        var tasks = Enumerable.Range(0, 100)
            .Select(i => _sut.GetBeforeStateAsync(typeof(TestDto), i.ToString(), CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        callCount.Should().Be(100);
    }

    [Fact]
    public async Task Register_ConcurrentRegistrations_ShouldBeThreadSafe()
    {
        // Arrange & Act
        var tasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(() =>
            {
                _sut.Register<TestDto>(async (sp, id, ct) => new TestDto(i.ToString(), $"Test-{i}"));
            }))
            .ToList();

        await Task.WhenAll(tasks);

        // Assert - should not throw, last registration wins
        var result = await _sut.GetBeforeStateAsync(typeof(TestDto), "any", CancellationToken.None);
        result.Should().NotBeNull();
    }

    #endregion

    #region Multiple DTO Types Tests

    [Fact]
    public async Task GetBeforeStateAsync_MultipleDtoTypes_ShouldResolveCorrectly()
    {
        // Arrange
        _sut.Register<TestDto>(async (sp, id, ct) => new TestDto(id.ToString()!, "TestDto"));
        _sut.Register<AnotherDto>(async (sp, id, ct) => new AnotherDto((int)(long)id, "AnotherDto"));

        // Act
        var testResult = await _sut.GetBeforeStateAsync(typeof(TestDto), "123", CancellationToken.None);
        var anotherResult = await _sut.GetBeforeStateAsync(typeof(AnotherDto), 456L, CancellationToken.None);

        // Assert
        testResult.Should().BeOfType<TestDto>();
        ((TestDto)testResult!).Name.Should().Be("TestDto");

        anotherResult.Should().BeOfType<AnotherDto>();
        ((AnotherDto)anotherResult!).Description.Should().Be("AnotherDto");
    }

    #endregion

    #region Test DTOs

    private sealed record TestDto(string Id, string Name);
    private sealed record AnotherDto(int Id, string Description);

    #endregion
}
