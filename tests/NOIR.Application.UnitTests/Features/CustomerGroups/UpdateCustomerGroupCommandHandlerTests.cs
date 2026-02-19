using FluentAssertions;
using Moq;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.CustomerGroups;
using NOIR.Application.Features.CustomerGroups.Commands.UpdateCustomerGroup;
using NOIR.Application.Features.CustomerGroups.DTOs;
using NOIR.Application.Features.CustomerGroups.Specifications;
using NOIR.Domain.Common;
using NOIR.Domain.Entities.Customer;

namespace NOIR.Application.UnitTests.Features.CustomerGroups;

/// <summary>
/// Unit tests for UpdateCustomerGroupCommandHandler.
/// </summary>
public class UpdateCustomerGroupCommandHandlerTests
{
    private readonly Mock<IRepository<CustomerGroup, Guid>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateCustomerGroupCommandHandler _handler;

    public UpdateCustomerGroupCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<CustomerGroup, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateCustomerGroupCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CustomerGroup CreateTestGroup(
        string name = "VIP Customers",
        string? description = "Top-tier customers")
    {
        return CustomerGroup.Create(name, description, "tenant-123");
    }

    private static UpdateCustomerGroupCommand CreateValidCommand(
        Guid? id = null,
        string name = "Updated Group",
        string? description = "Updated description",
        bool isActive = true) =>
        new(id ?? Guid.NewGuid(), name, description, isActive) { UserId = "user-1" };

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidCommand_UpdatesGroupAndReturnsDto()
    {
        // Arrange
        var group = CreateTestGroup();
        var command = CreateValidCommand(id: group.Id);

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated Group");
        result.Value.Description.Should().Be("Updated description");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SameName_SkipsDuplicateCheck()
    {
        // Arrange
        var group = CreateTestGroup(name: "VIP Customers");
        var command = CreateValidCommand(id: group.Id, name: "VIP Customers");

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeactivateGroup_SetsIsActiveFalse()
    {
        // Arrange
        var group = CreateTestGroup();
        var command = CreateValidCommand(id: group.Id, isActive: false);

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_GroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var command = CreateValidCommand();
        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerGroup?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.CustomerGroup.NotFound);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_DuplicateName_ReturnsConflict()
    {
        // Arrange
        var group = CreateTestGroup(name: "Original Name");
        var command = CreateValidCommand(id: group.Id, name: "Existing Name");

        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(group);

        var existingGroup = CreateTestGroup(name: "Existing Name");
        _repositoryMock.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<CustomerGroupNameExistsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGroup);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.CustomerGroup.DuplicateName);
    }

    #endregion
}
