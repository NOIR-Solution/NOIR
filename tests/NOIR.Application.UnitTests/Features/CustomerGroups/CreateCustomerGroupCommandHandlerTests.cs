using FluentAssertions;
using Moq;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.CustomerGroups;
using NOIR.Application.Features.CustomerGroups.Commands.CreateCustomerGroup;
using NOIR.Application.Features.CustomerGroups.DTOs;
using NOIR.Application.Features.CustomerGroups.Specifications;
using NOIR.Domain.Common;
using NOIR.Domain.Entities.Customer;

namespace NOIR.Application.UnitTests.Features.CustomerGroups;

/// <summary>
/// Unit tests for CreateCustomerGroupCommandHandler.
/// </summary>
public class CreateCustomerGroupCommandHandlerTests
{
    private readonly Mock<IRepository<CustomerGroup, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateCustomerGroupCommandHandler _handler;

    public CreateCustomerGroupCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<CustomerGroup, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        _handler = new CreateCustomerGroupCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CreateCustomerGroupCommand CreateValidCommand(
        string name = "VIP Customers",
        string? description = "Top-tier customers with high spending") =>
        new(name, description) { UserId = "user-1" };

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidCommand_CreatesGroupAndReturnsDto()
    {
        // Arrange
        var command = CreateValidCommand();
        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("VIP Customers");
        result.Value.Description.Should().Be("Top-tier customers with high spending");
        result.Value.Slug.Should().Be("vip-customers");
        result.Value.IsActive.Should().BeTrue();
        result.Value.MemberCount.Should().Be(0);

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<CustomerGroup>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_WithoutDescription_CreatesGroup()
    {
        // Arrange
        var command = CreateValidCommand(description: null);
        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        // Arrange
        var command = CreateValidCommand();
        var existing = CustomerGroup.Create("VIP Customers", null, "tenant-123");
        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.CustomerGroup.DuplicateName);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_CancellationToken_IsPassedToRepository()
    {
        // Arrange
        var command = CreateValidCommand();
        var cts = new CancellationTokenSource();
        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _repositoryMock.Verify(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), cts.Token), Times.Once);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<CustomerGroup>(), cts.Token), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(cts.Token), Times.Once);
    }

    #endregion
}
