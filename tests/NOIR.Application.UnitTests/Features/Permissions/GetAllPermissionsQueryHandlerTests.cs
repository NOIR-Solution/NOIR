using NOIR.Application.Features.Permissions.Queries.GetAllPermissions;

namespace NOIR.Application.UnitTests.Features.Permissions;

/// <summary>
/// Unit tests for GetAllPermissionsQueryHandler.
/// Tests permission retrieval scenarios.
/// </summary>
public class GetAllPermissionsQueryHandlerTests
{
    #region Test Setup

    private readonly GetAllPermissionsQueryHandler _handler;

    public GetAllPermissionsQueryHandlerTests()
    {
        _handler = new GetAllPermissionsQueryHandler();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ShouldReturnAllPermissions()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsWithCorrectStructure()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(permission =>
        {
            permission.Id.Should().NotBeNullOrEmpty();
            permission.Name.Should().NotBeNullOrEmpty();
            permission.Resource.Should().NotBeNullOrEmpty();
            permission.Action.Should().NotBeNullOrEmpty();
            permission.DisplayName.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsWithMetadata()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify that permissions have categories assigned
        var categorizedPermissions = result.Value.Where(p => p.Category is not null).ToList();
        categorizedPermissions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsWithIncrementingSortOrder()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var sortOrders = result.Value.Select(p => p.SortOrder).ToList();
        sortOrders.Should().BeInAscendingOrder();
        sortOrders.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsMarkedAsSystem()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // All static permissions should be marked as system
        result.Value.Should().AllSatisfy(p => p.IsSystem.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsWithTenantAllowedFlag()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify IsTenantAllowed is populated (some should be allowed, some not)
        var tenantAllowedPermissions = result.Value.Where(p => p.IsTenantAllowed).ToList();
        var notTenantAllowedPermissions = result.Value.Where(p => !p.IsTenantAllowed).ToList();

        // Both lists should have items (some permissions are tenant-allowed, some are not)
        tenantAllowedPermissions.Should().NotBeEmpty();
        notTenantAllowedPermissions.Should().NotBeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        var result = await _handler.Handle(query, token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_CalledMultipleTimes_ShouldReturnConsistentResults()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result1 = await _handler.Handle(query, CancellationToken.None);
        var result2 = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().HaveCount(result2.Value.Count);
        result1.Value.Select(p => p.Name).Should().BeEquivalentTo(result2.Value.Select(p => p.Name));
    }

    [Fact]
    public async Task Handle_ShouldReturnKnownPermissions()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var permissionNames = result.Value.Select(p => p.Name).ToList();

        // Verify some known permissions exist
        permissionNames.Should().Contain(p => p.Contains("users"));
        permissionNames.Should().Contain(p => p.Contains("roles"));
    }

    #endregion
}
