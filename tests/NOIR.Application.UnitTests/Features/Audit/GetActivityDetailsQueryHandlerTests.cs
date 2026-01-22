using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries.GetActivityDetails;

namespace NOIR.Application.UnitTests.Features.Audit;

/// <summary>
/// Unit tests for GetActivityDetailsQueryHandler.
/// Tests activity detail retrieval scenarios.
/// </summary>
public class GetActivityDetailsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IAuditLogQueryService> _auditLogQueryServiceMock;
    private readonly GetActivityDetailsQueryHandler _handler;

    public GetActivityDetailsQueryHandlerTests()
    {
        _auditLogQueryServiceMock = new Mock<IAuditLogQueryService>();
        _handler = new GetActivityDetailsQueryHandler(_auditLogQueryServiceMock.Object);
    }

    private static ActivityDetailsDto CreateTestActivityDetails(Guid id)
    {
        var entry = new ActivityTimelineEntryDto(
            Id: id,
            Timestamp: DateTimeOffset.UtcNow,
            UserEmail: "admin@noir.local",
            UserId: Guid.NewGuid().ToString(),
            DisplayContext: "Users",
            OperationType: "Update",
            ActionDescription: "Updated user John Doe",
            TargetDisplayName: "John Doe",
            TargetDtoType: "UserDto",
            TargetDtoId: Guid.NewGuid().ToString(),
            IsSuccess: true,
            DurationMs: 150,
            EntityChangeCount: 2,
            CorrelationId: Guid.NewGuid().ToString(),
            HandlerName: "UpdateUserCommandHandler");

        return new ActivityDetailsDto(
            Entry: entry,
            InputParameters: """{"userId": "123", "name": "John Doe"}""",
            OutputResult: """{"success": true}""",
            DtoDiff: """[{"op": "replace", "path": "/name", "value": "John Doe"}]""",
            ErrorMessage: null,
            HttpRequest: new HttpRequestDetailsDto(
                Id: Guid.NewGuid(),
                Method: "PUT",
                Path: "/api/users/123",
                StatusCode: 200,
                QueryString: null,
                ClientIpAddress: "127.0.0.1",
                UserAgent: "Mozilla/5.0",
                RequestTime: DateTimeOffset.UtcNow.AddMinutes(-1),
                DurationMs: 200),
            EntityChanges: new List<EntityChangeDto>
            {
                new EntityChangeDto(
                    Id: Guid.NewGuid(),
                    EntityType: "User",
                    EntityId: "123",
                    Operation: "Update",
                    Version: 2,
                    Timestamp: DateTimeOffset.UtcNow,
                    Changes: new List<FieldChangeDto>
                    {
                        new FieldChangeDto("Name", "Jane", "John Doe", ChangeOperation.Modified)
                    })
            });
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnActivityDetails()
    {
        // Arrange
        var activityId = Guid.NewGuid();
        var expectedDetails = CreateTestActivityDetails(activityId);

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDetails));

        var query = new GetActivityDetailsQuery(activityId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Entry.Id.Should().Be(activityId);
        result.Value.Entry.DisplayContext.Should().Be("Users");
        result.Value.Entry.OperationType.Should().Be("Update");
        result.Value.InputParameters.Should().NotBeNullOrEmpty();
        result.Value.OutputResult.Should().NotBeNullOrEmpty();
        result.Value.HttpRequest.Should().NotBeNull();
        result.Value.EntityChanges.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnDetailsWithAllFields()
    {
        // Arrange
        var activityId = Guid.NewGuid();
        var expectedDetails = CreateTestActivityDetails(activityId);

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDetails));

        var query = new GetActivityDetailsQuery(activityId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var details = result.Value;
        details.Entry.UserEmail.Should().Be("admin@noir.local");
        details.Entry.ActionDescription.Should().Be("Updated user John Doe");
        details.Entry.TargetDisplayName.Should().Be("John Doe");
        details.Entry.IsSuccess.Should().BeTrue();
        details.Entry.DurationMs.Should().Be(150);
        details.Entry.EntityChangeCount.Should().Be(2);
        details.HttpRequest!.Method.Should().Be("PUT");
        details.HttpRequest.Path.Should().Be("/api/users/123");
        details.HttpRequest.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Handle_WithFailedActivity_ShouldReturnDetailsWithErrorMessage()
    {
        // Arrange
        var activityId = Guid.NewGuid();
        var entry = new ActivityTimelineEntryDto(
            Id: activityId,
            Timestamp: DateTimeOffset.UtcNow,
            UserEmail: "admin@noir.local",
            UserId: Guid.NewGuid().ToString(),
            DisplayContext: "Users",
            OperationType: "Delete",
            ActionDescription: "Failed to delete user",
            TargetDisplayName: "Test User",
            TargetDtoType: "UserDto",
            TargetDtoId: null,
            IsSuccess: false,
            DurationMs: 50,
            EntityChangeCount: 0,
            CorrelationId: Guid.NewGuid().ToString(),
            HandlerName: "DeleteUserCommandHandler");

        var failedDetails = new ActivityDetailsDto(
            Entry: entry,
            InputParameters: """{"userId": "456"}""",
            OutputResult: null,
            DtoDiff: null,
            ErrorMessage: "User not found",
            HttpRequest: null,
            EntityChanges: new List<EntityChangeDto>());

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(failedDetails));

        var query = new GetActivityDetailsQuery(activityId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Entry.IsSuccess.Should().BeFalse();
        result.Value.ErrorMessage.Should().Be("User not found");
        result.Value.EntityChanges.Should().BeEmpty();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WithNonExistentId_ShouldReturnNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ActivityDetailsDto>(
                Error.NotFound($"Activity entry with ID {nonExistentId} was not found.", ErrorCodes.Business.NotFound)));

        var query = new GetActivityDetailsQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain(nonExistentId.ToString());
        result.Error.Code.Should().Be(ErrorCodes.Business.NotFound);
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ShouldReturnNotFoundError()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(emptyGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ActivityDetailsDto>(
                Error.NotFound($"Activity entry with ID {emptyGuid} was not found.", ErrorCodes.Business.NotFound)));

        var query = new GetActivityDetailsQuery(emptyGuid);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ErrorCodes.Business.NotFound);
    }

    #endregion

    #region CancellationToken Scenarios

    [Fact]
    public async Task Handle_ShouldPassCancellationTokenToService()
    {
        // Arrange
        var activityId = Guid.NewGuid();
        var expectedDetails = CreateTestActivityDetails(activityId);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(activityId, token))
            .ReturnsAsync(Result.Success(expectedDetails));

        var query = new GetActivityDetailsQuery(activityId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.GetActivityDetailsAsync(activityId, token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var activityId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(activityId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var query = new GetActivityDetailsQuery(activityId);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(query, cts.Token));
    }

    #endregion

    #region Service Call Verification

    [Fact]
    public async Task Handle_ShouldCallServiceWithCorrectId()
    {
        // Arrange
        var activityId = Guid.NewGuid();
        var expectedDetails = CreateTestActivityDetails(activityId);

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDetails));

        var query = new GetActivityDetailsQuery(activityId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _auditLogQueryServiceMock.Verify(
            x => x.GetActivityDetailsAsync(activityId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Forbidden Scenarios

    [Fact]
    public async Task Handle_WhenUnauthorizedAccess_ShouldReturnForbiddenError()
    {
        // Arrange
        var activityId = Guid.NewGuid();

        _auditLogQueryServiceMock
            .Setup(x => x.GetActivityDetailsAsync(activityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ActivityDetailsDto>(
                Error.Forbidden("You do not have permission to view this activity.", ErrorCodes.Auth.Forbidden)));

        var query = new GetActivityDetailsQuery(activityId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        result.Error.Code.Should().Be(ErrorCodes.Auth.Forbidden);
    }

    #endregion
}
