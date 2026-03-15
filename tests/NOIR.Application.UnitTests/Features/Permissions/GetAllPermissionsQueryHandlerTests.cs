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
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsWithCorrectStructure()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        foreach (var permission in result.Value)
        {
            permission.Id.ShouldNotBeNullOrEmpty();
            permission.Name.ShouldNotBeNullOrEmpty();
            permission.Resource.ShouldNotBeNullOrEmpty();
            permission.Action.ShouldNotBeNullOrEmpty();
            permission.DisplayName.ShouldNotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsWithMetadata()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        // Verify that permissions have categories assigned
        var categorizedPermissions = result.Value.Where(p => p.Category is not null).ToList();
        categorizedPermissions.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsWithIncrementingSortOrder()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        var sortOrders = result.Value.Select(p => p.SortOrder).ToList();
        sortOrders.ShouldBeInOrder(SortDirection.Ascending);
        sortOrders.ShouldBeUnique();
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsMarkedAsSystem()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        // All static permissions should be marked as system
        result.Value.ShouldAllBe(p => p.IsSystem == true);
    }

    [Fact]
    public async Task Handle_ShouldReturnPermissionsWithTenantAllowedFlag()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        // Verify IsTenantAllowed is populated (some should be allowed, some not)
        var tenantAllowedPermissions = result.Value.Where(p => p.IsTenantAllowed).ToList();
        var notTenantAllowedPermissions = result.Value.Where(p => !p.IsTenantAllowed).ToList();

        // Both lists should have items (some permissions are tenant-allowed, some are not)
        tenantAllowedPermissions.ShouldNotBeEmpty();
        notTenantAllowedPermissions.ShouldNotBeEmpty();
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
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeEmpty();
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
        result1.IsSuccess.ShouldBe(true);
        result2.IsSuccess.ShouldBe(true);
        result1.Value.Count().ShouldBe(result2.Value.Count);
        result1.Value.Select(p => p.Name).ShouldBe(result2.Value.Select(p => p.Name));
    }

    [Fact]
    public async Task Handle_ShouldReturnKnownPermissions()
    {
        // Arrange
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        var permissionNames = result.Value.Select(p => p.Name).ToList();

        // Verify some known permissions exist
        permissionNames.ShouldContain(p => p.Contains("users"));
        permissionNames.ShouldContain(p => p.Contains("roles"));
    }

    #endregion
}
