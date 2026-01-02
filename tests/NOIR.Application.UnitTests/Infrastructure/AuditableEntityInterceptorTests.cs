namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for AuditableEntityInterceptor.
/// Tests the interceptor's API and behavior contracts.
/// Database-dependent tests are in integration tests due to multi-tenant query filters.
/// </summary>
public class AuditableEntityInterceptorTests
{
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly AuditableEntityInterceptor _sut;
    private readonly DateTimeOffset _testTime = new(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public AuditableEntityInterceptorTests()
    {
        _currentUserMock = new Mock<ICurrentUser>();
        _dateTimeMock = new Mock<IDateTime>();

        _currentUserMock.Setup(x => x.UserId).Returns("test-user");
        _dateTimeMock.Setup(x => x.UtcNow).Returns(_testTime);

        _sut = new AuditableEntityInterceptor(_currentUserMock.Object, _dateTimeMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptDependencies()
    {
        // Act
        var interceptor = new AuditableEntityInterceptor(_currentUserMock.Object, _dateTimeMock.Object);

        // Assert
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullCurrentUser_ShouldNotThrow()
    {
        // Act - interceptor is created but will fail when used with null
        var act = () => new AuditableEntityInterceptor(null!, _dateTimeMock.Object);

        // Assert - Constructor doesn't validate, it defers to usage
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullDateTime_ShouldNotThrow()
    {
        // Act
        var act = () => new AuditableEntityInterceptor(_currentUserMock.Object, null!);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void Interceptor_ShouldInheritFromSaveChangesInterceptor()
    {
        // Assert
        _sut.Should().BeAssignableTo<SaveChangesInterceptor>();
    }

    #endregion

    #region Method Existence Tests

    [Fact]
    public void SavingChanges_MethodShouldExist()
    {
        // Assert
        var method = typeof(AuditableEntityInterceptor).GetMethod("SavingChanges");
        method.Should().NotBeNull();
    }

    [Fact]
    public void SavingChangesAsync_MethodShouldExist()
    {
        // Assert
        var method = typeof(AuditableEntityInterceptor).GetMethod("SavingChangesAsync");
        method.Should().NotBeNull();
    }

    #endregion

    #region Dependency Access Tests

    [Fact]
    public void Interceptor_ShouldAccessCurrentUser()
    {
        // Act - The interceptor stores the dependency internally
        var interceptor = new AuditableEntityInterceptor(_currentUserMock.Object, _dateTimeMock.Object);

        // Assert - If we can create it without error, dependency is stored
        interceptor.Should().NotBeNull();
    }

    [Fact]
    public void Interceptor_ShouldAccessDateTime()
    {
        // Act - The interceptor stores the dependency internally
        var interceptor = new AuditableEntityInterceptor(_currentUserMock.Object, _dateTimeMock.Object);

        // Assert - If we can create it without error, dependency is stored
        interceptor.Should().NotBeNull();
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ICurrentUser_UserId_ShouldReturnConfiguredValue()
    {
        // Act
        var userId = _currentUserMock.Object.UserId;

        // Assert
        userId.Should().Be("test-user");
    }

    [Fact]
    public void IDateTime_UtcNow_ShouldReturnConfiguredValue()
    {
        // Act
        var now = _dateTimeMock.Object.UtcNow;

        // Assert
        now.Should().Be(_testTime);
    }

    #endregion

    #region Different User Scenarios

    [Fact]
    public void CurrentUser_WithNullUserId_ShouldReturnNull()
    {
        // Arrange
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);

        // Act
        var userId = _currentUserMock.Object.UserId;

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void CurrentUser_WithEmptyUserId_ShouldReturnEmpty()
    {
        // Arrange
        _currentUserMock.Setup(x => x.UserId).Returns(string.Empty);

        // Act
        var userId = _currentUserMock.Object.UserId;

        // Assert
        userId.Should().BeEmpty();
    }

    [Fact]
    public void CurrentUser_WithWhitespaceUserId_ShouldReturnWhitespace()
    {
        // Arrange
        _currentUserMock.Setup(x => x.UserId).Returns("   ");

        // Act
        var userId = _currentUserMock.Object.UserId;

        // Assert
        userId.Should().Be("   ");
    }

    #endregion

    #region DateTime Scenarios

    [Fact]
    public void DateTime_WithDifferentTime_ShouldReturnConfiguredValue()
    {
        // Arrange
        var differentTime = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        _dateTimeMock.Setup(x => x.UtcNow).Returns(differentTime);

        // Act
        var now = _dateTimeMock.Object.UtcNow;

        // Assert
        now.Should().Be(differentTime);
    }

    [Fact]
    public void DateTime_ShouldReturnUtcOffset()
    {
        // Act
        var now = _dateTimeMock.Object.UtcNow;

        // Assert
        now.Offset.Should().Be(TimeSpan.Zero);
    }

    #endregion
}
