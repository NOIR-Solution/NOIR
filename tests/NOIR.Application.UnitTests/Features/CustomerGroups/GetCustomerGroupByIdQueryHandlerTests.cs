using Moq;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.CustomerGroups;
using NOIR.Application.Features.CustomerGroups.DTOs;
using NOIR.Application.Features.CustomerGroups.Queries.GetCustomerGroupById;
using NOIR.Application.Features.CustomerGroups.Specifications;
using NOIR.Domain.Common;
using NOIR.Domain.Entities.Customer;

namespace NOIR.Application.UnitTests.Features.CustomerGroups;

/// <summary>
/// Unit tests for GetCustomerGroupByIdQueryHandler.
/// </summary>
public class GetCustomerGroupByIdQueryHandlerTests
{
    private readonly Mock<IRepository<CustomerGroup, Guid>> _repositoryMock;
    private readonly GetCustomerGroupByIdQueryHandler _handler;

    public GetCustomerGroupByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<CustomerGroup, Guid>>();
        _handler = new GetCustomerGroupByIdQueryHandler(_repositoryMock.Object);
    }

    private static CustomerGroup CreateTestGroup(
        string name = "VIP Customers",
        string? description = "Top-tier customers")
    {
        return CustomerGroup.Create(name, description, "tenant-123");
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_ExistingGroup_ReturnsDtoWithAllProperties()
    {
        // Arrange
        var group = CreateTestGroup();
        var query = new GetCustomerGroupByIdQuery(group.Id);

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(group.Id);
        result.Value.Name.ShouldBe("VIP Customers");
        result.Value.Description.ShouldBe("Top-tier customers");
        result.Value.Slug.ShouldBe("vip-customers");
        result.Value.IsActive.ShouldBe(true);
        result.Value.MemberCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_MinimalGroup_ReturnsDtoWithNullDescription()
    {
        // Arrange
        var group = CreateTestGroup(description: null);
        var query = new GetCustomerGroupByIdQuery(group.Id);

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Description.ShouldBeNull();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_GroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var query = new GetCustomerGroupByIdQuery(Guid.NewGuid());
        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Code.ShouldBe(ErrorCodes.CustomerGroup.NotFound);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_CancellationToken_IsPassedToRepository()
    {
        // Arrange
        var query = new GetCustomerGroupByIdQuery(Guid.NewGuid());
        var cts = new CancellationTokenSource();

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        await _handler.Handle(query, cts.Token);

        // Assert
        _repositoryMock.Verify(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdSpec>(), cts.Token), Times.Once);
    }

    #endregion
}
