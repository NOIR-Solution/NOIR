using NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

namespace NOIR.Application.UnitTests.Features.Permissions;

/// <summary>
/// Unit tests for GetPermissionTemplatesQueryHandler.
/// Tests permission template retrieval scenarios with mocked dependencies.
/// </summary>
public class GetPermissionTemplatesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IPermissionTemplateQueryService> _queryServiceMock;
    private readonly GetPermissionTemplatesQueryHandler _handler;

    public GetPermissionTemplatesQueryHandlerTests()
    {
        _queryServiceMock = new Mock<IPermissionTemplateQueryService>();

        _handler = new GetPermissionTemplatesQueryHandler(_queryServiceMock.Object);
    }

    private static PermissionTemplateDto CreateTestTemplateDto(
        Guid? id = null,
        string name = "Test Template",
        string? description = "Test Description",
        Guid? tenantId = null,
        bool isSystem = false,
        string? iconName = "icon-test",
        string? color = "#000000",
        int sortOrder = 0,
        IReadOnlyList<string>? permissions = null)
    {
        return new PermissionTemplateDto(
            id ?? Guid.NewGuid(),
            name,
            description,
            tenantId,
            isSystem,
            iconName,
            color,
            sortOrder,
            permissions ?? new List<string> { "permission.read", "permission.write" });
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithoutTenantFilter_ShouldReturnAllTemplates()
    {
        // Arrange
        var templates = new List<PermissionTemplateDto>
        {
            CreateTestTemplateDto(name: "Admin Template", isSystem: true),
            CreateTestTemplateDto(name: "User Template", isSystem: true),
            CreateTestTemplateDto(name: "Custom Template", isSystem: false)
        };

        _queryServiceMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetPermissionTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().BeEquivalentTo(templates);
    }

    [Fact]
    public async Task Handle_WithTenantFilter_ShouldReturnTenantTemplates()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var templates = new List<PermissionTemplateDto>
        {
            CreateTestTemplateDto(name: "System Template", isSystem: true),
            CreateTestTemplateDto(name: "Tenant Template", tenantId: tenantId, isSystem: false)
        };

        _queryServiceMock
            .Setup(x => x.GetAllAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetPermissionTemplatesQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        _queryServiceMock.Verify(x => x.GetAllAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnTemplatesWithPermissions()
    {
        // Arrange
        var permissions = new List<string> { "users.read", "users.write", "roles.read" };
        var templates = new List<PermissionTemplateDto>
        {
            CreateTestTemplateDto(name: "Manager Template", permissions: permissions)
        };

        _queryServiceMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetPermissionTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Permissions.Should().HaveCount(3);
        result.Value.First().Permissions.Should().Contain("users.read");
    }

    #endregion

    #region Empty Results

    [Fact]
    public async Task Handle_WhenNoTemplatesExist_ShouldReturnEmptyList()
    {
        // Arrange
        _queryServiceMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PermissionTemplateDto>());

        var query = new GetPermissionTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNoTemplatesForTenant_ShouldReturnEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _queryServiceMock
            .Setup(x => x.GetAllAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PermissionTemplateDto>());

        var query = new GetPermissionTemplatesQuery(tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToService()
    {
        // Arrange
        var templates = new List<PermissionTemplateDto>
        {
            CreateTestTemplateDto()
        };

        _queryServiceMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetPermissionTemplatesQuery();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(query, token);

        // Assert
        _queryServiceMock.Verify(x => x.GetAllAsync(null, token), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnTemplatesWithAllMetadata()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var templates = new List<PermissionTemplateDto>
        {
            CreateTestTemplateDto(
                id: templateId,
                name: "Complete Template",
                description: "A complete template",
                isSystem: true,
                iconName: "shield-check",
                color: "#4CAF50",
                sortOrder: 5)
        };

        _queryServiceMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetPermissionTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var template = result.Value.First();
        template.Id.Should().Be(templateId);
        template.Name.Should().Be("Complete Template");
        template.Description.Should().Be("A complete template");
        template.IsSystem.Should().BeTrue();
        template.IconName.Should().Be("shield-check");
        template.Color.Should().Be("#4CAF50");
        template.SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithMultipleTemplates_ShouldPreserveOrder()
    {
        // Arrange
        var templates = new List<PermissionTemplateDto>
        {
            CreateTestTemplateDto(name: "First", sortOrder: 1),
            CreateTestTemplateDto(name: "Second", sortOrder: 2),
            CreateTestTemplateDto(name: "Third", sortOrder: 3)
        };

        _queryServiceMock
            .Setup(x => x.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        var query = new GetPermissionTemplatesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Select(t => t.Name).Should().ContainInOrder("First", "Second", "Third");
    }

    #endregion
}
