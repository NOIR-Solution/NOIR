using FluentAssertions;
using Moq;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.CustomerGroups;
using NOIR.Application.Features.CustomerGroups.DTOs;
using NOIR.Application.Features.CustomerGroups.Queries.GetCustomerGroups;
using NOIR.Application.Features.CustomerGroups.Specifications;
using NOIR.Domain.Common;
using NOIR.Domain.Entities.Customer;

namespace NOIR.Application.UnitTests.Features.CustomerGroups;

/// <summary>
/// Unit tests for GetCustomerGroupsQueryHandler.
/// </summary>
public class GetCustomerGroupsQueryHandlerTests
{
    private readonly Mock<IRepository<CustomerGroup, Guid>> _repositoryMock;
    private readonly GetCustomerGroupsQueryHandler _handler;

    public GetCustomerGroupsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<CustomerGroup, Guid>>();
        _handler = new GetCustomerGroupsQueryHandler(_repositoryMock.Object);
    }

    private static CustomerGroup CreateTestGroup(string name, string? description = null)
    {
        return CustomerGroup.Create(name, description, "tenant-123");
    }

    private static List<CustomerGroup> CreateTestGroups(int count)
    {
        var groups = new List<CustomerGroup>();
        for (var i = 1; i <= count; i++)
        {
            groups.Add(CreateTestGroup($"Group {i}", $"Description {i}"));
        }
        return groups;
    }

    #region Success Scenarios

    [Fact]
    public async Task Handle_DefaultPaging_ReturnsPagedResult()
    {
        // Arrange
        var query = new GetCustomerGroupsQuery();
        var groups = CreateTestGroups(3);

        _repositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomerGroupsPagedSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(groups);

        _repositoryMock.Setup(x => x.CountAsync(
            It.IsAny<CustomerGroupsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
        result.Value.PageIndex.Should().Be(0);
        result.Value.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyResults_ReturnsEmptyPagedResult()
    {
        // Arrange
        var query = new GetCustomerGroupsQuery();

        _repositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomerGroupsPagedSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerGroup>());

        _repositoryMock.Setup(x => x.CountAsync(
            It.IsAny<CustomerGroupsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_CustomPaging_ReturnsCorrectPageInfo()
    {
        // Arrange
        var query = new GetCustomerGroupsQuery(PageNumber: 2, PageSize: 5);
        var groups = CreateTestGroups(5);

        _repositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomerGroupsPagedSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(groups);

        _repositoryMock.Setup(x => x.CountAsync(
            It.IsAny<CustomerGroupsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageIndex.Should().Be(1); // 0-based
        result.Value.PageNumber.Should().Be(2); // 1-based
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_MapsToListDto_Correctly()
    {
        // Arrange
        var query = new GetCustomerGroupsQuery();
        var groups = new List<CustomerGroup> { CreateTestGroup("VIP Customers", "Top-tier") };

        _repositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomerGroupsPagedSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(groups);

        _repositoryMock.Setup(x => x.CountAsync(
            It.IsAny<CustomerGroupsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.First();
        item.Name.Should().Be("VIP Customers");
        item.Slug.Should().Be("vip-customers");
        item.IsActive.Should().BeTrue();
        item.MemberCount.Should().Be(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_CancellationToken_IsPassedToRepository()
    {
        // Arrange
        var query = new GetCustomerGroupsQuery();
        var cts = new CancellationTokenSource();

        _repositoryMock.Setup(x => x.ListAsync(
            It.IsAny<CustomerGroupsPagedSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CustomerGroup>());

        _repositoryMock.Setup(x => x.CountAsync(
            It.IsAny<CustomerGroupsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, cts.Token);

        // Assert
        _repositoryMock.Verify(x => x.ListAsync(
            It.IsAny<CustomerGroupsPagedSpec>(), cts.Token), Times.Once);
        _repositoryMock.Verify(x => x.CountAsync(
            It.IsAny<CustomerGroupsCountSpec>(), cts.Token), Times.Once);
    }

    #endregion
}
