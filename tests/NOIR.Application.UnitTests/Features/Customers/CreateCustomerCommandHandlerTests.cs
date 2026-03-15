namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for CreateCustomerCommandHandler.
/// Tests customer creation scenarios with mocked dependencies.
/// </summary>
public class CreateCustomerCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        _handler = new CreateCustomerCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static CreateCustomerCommand CreateValidCommand(
        string email = "john@example.com",
        string firstName = "John",
        string lastName = "Doe",
        string? phone = null,
        string? userId = null,
        string? tags = null,
        string? notes = null)
    {
        return new CreateCustomerCommand(
            email,
            firstName,
            lastName,
            phone,
            userId,
            tags,
            notes);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var command = CreateValidCommand();

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Email.ShouldBe("john@example.com");
        result.Value.FirstName.ShouldBe("John");
        result.Value.LastName.ShouldBe("Doe");

        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPhoneAndUserId_ShouldSetProperties()
    {
        // Arrange
        var command = CreateValidCommand(
            phone: "0901234567",
            userId: "user-abc-123");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Phone.ShouldBe("0901234567");
        result.Value.UserId.ShouldBe("user-abc-123");
    }

    [Fact]
    public async Task Handle_WithTags_ShouldAddTags()
    {
        // Arrange
        var command = CreateValidCommand(tags: "vip,premium,loyal");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Tags.ShouldContain("vip");
        result.Value.Tags.ShouldContain("premium");
        result.Value.Tags.ShouldContain("loyal");
    }

    [Fact]
    public async Task Handle_WithNotes_ShouldAddNotes()
    {
        // Arrange
        var command = CreateValidCommand(notes: "Important customer - handle with care");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Notes.ShouldBe("Important customer - handle with care");
    }

    [Fact]
    public async Task Handle_ShouldSetDefaultSegmentAndTier()
    {
        // Arrange
        var command = CreateValidCommand();

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Segment.ShouldBe(CustomerSegment.New);
        result.Value.Tier.ShouldBe(CustomerTier.Standard);
        result.Value.IsActive.ShouldBe(true);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var command = CreateValidCommand(email: "existing@example.com");

        var existingCustomer = Customer.Create(
            null, "existing@example.com", "Existing", "Customer", null, "tenant-123");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-CUSTOMER-001");

        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        const string tenantId = "tenant-abc";
        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = CreateValidCommand();

        Customer? capturedCustomer = null;

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((customer, _) => capturedCustomer = customer)
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        capturedCustomer.ShouldNotBeNull();
        capturedCustomer!.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var command = CreateValidCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                token))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), token))
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CustomerByEmailSpec>(), token),
            Times.Once);

        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyTags_ShouldNotSetTags()
    {
        // Arrange
        var command = CreateValidCommand(tags: "");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Tags.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyNotes_ShouldNotSetNotes()
    {
        // Arrange
        var command = CreateValidCommand(notes: "");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByEmailSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer customer, CancellationToken _) => customer);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Notes.ShouldBeNull();
    }

    #endregion
}
