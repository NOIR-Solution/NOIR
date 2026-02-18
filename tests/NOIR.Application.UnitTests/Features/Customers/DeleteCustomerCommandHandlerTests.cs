namespace NOIR.Application.UnitTests.Features.Customers;

/// <summary>
/// Unit tests for DeleteCustomerCommandHandler.
/// Tests customer soft-deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteCustomerCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteCustomerCommandHandler _handler;

    public DeleteCustomerCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteCustomerCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Customer CreateTestCustomer(
        string email = "john@example.com",
        string firstName = "John",
        string lastName = "Doe")
    {
        return Customer.Create(null, email, firstName, lastName, null, "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenCustomerExists_ShouldSucceed()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteCustomerCommand(customerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("john@example.com");
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");

        _customerRepositoryMock.Verify(
            x => x.Remove(existingCustomer),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtoBeforeRemoval()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer(
            email: "test@example.com",
            firstName: "Test",
            lastName: "Customer");

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        var command = new DeleteCustomerCommand(customerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be("test@example.com");
        result.Value.FirstName.Should().Be("Test");
        result.Value.LastName.Should().Be("Customer");
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCustomerNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var customerId = Guid.NewGuid();

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        var command = new DeleteCustomerCommand(customerId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CUSTOMER-002");

        _customerRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Customer>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var existingCustomer = CreateTestCustomer();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _customerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CustomerByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(existingCustomer);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new DeleteCustomerCommand(customerId);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _customerRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CustomerByIdForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
