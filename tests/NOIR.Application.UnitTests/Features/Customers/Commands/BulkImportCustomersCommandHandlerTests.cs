using NOIR.Application.Features.Customers.Commands.BulkImportCustomers;

namespace NOIR.Application.UnitTests.Features.Customers.Commands;

/// <summary>
/// Unit tests for BulkImportCustomersCommandHandler.
/// Tests batch import with validation, dedup, and error handling.
/// </summary>
public class BulkImportCustomersCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<BulkImportCustomersCommandHandler>> _loggerMock;
    private readonly BulkImportCustomersCommandHandler _handler;

    private const string TestTenantId = "tenant-123";

    public BulkImportCustomersCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<IRepository<Customer, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<BulkImportCustomersCommandHandler>>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new BulkImportCustomersCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private static ImportCustomerDto CreateImportDto(
        string email = "john@example.com",
        string firstName = "John",
        string lastName = "Doe",
        string? phone = null,
        string? tags = null)
    {
        return new ImportCustomerDto(email, firstName, lastName, phone, tags);
    }

    private void SetupNoExistingCustomers()
    {
        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersEmailCheckSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>());
    }

    private void SetupExistingCustomerEmails(params string[] emails)
    {
        var existing = emails.Select(e =>
            Customer.Create(null, e, "Existing", "Customer", null, TestTenantId)).ToList();

        _customerRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CustomersEmailCheckSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCustomers_ImportsSuccessfully()
    {
        // Arrange
        SetupNoExistingCustomers();

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            CreateImportDto("alice@test.com", "Alice", "Smith"),
            CreateImportDto("bob@test.com", "Bob", "Jones")
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(2);
        result.Value.Failed.ShouldBe(0);
        result.Value.Errors.ShouldBeEmpty();

        _customerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_CallsSaveChangesAsync()
    {
        // Arrange
        SetupNoExistingCustomers();

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            CreateImportDto("alice@test.com", "Alice", "Smith")
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Duplicate Email Scenarios

    [Fact]
    public async Task Handle_WithDuplicateEmail_ReportsError()
    {
        // Arrange
        SetupExistingCustomerEmails("existing@test.com");

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            CreateImportDto("existing@test.com", "Dupe", "User"),
            CreateImportDto("new@test.com", "New", "User")
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].Message.ShouldContain("already exists");
    }

    [Fact]
    public async Task Handle_WithDuplicateEmailInBatch_ReportsError()
    {
        // Arrange
        SetupNoExistingCustomers();

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            CreateImportDto("same@test.com", "First", "User"),
            CreateImportDto("same@test.com", "Second", "User")
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].Message.ShouldContain("Duplicate email");
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithMissingEmail_ReportsError()
    {
        // Arrange
        SetupNoExistingCustomers();

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            new("", "NoEmail", "User", null, null),
            CreateImportDto("valid@test.com", "Valid", "User")
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].Message.ShouldContain("Email is required");
    }

    [Fact]
    public async Task Handle_WithWhitespaceEmail_ReportsError()
    {
        // Arrange
        SetupNoExistingCustomers();

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            new("   ", "Whitespace", "User", null, null)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.Failed.ShouldBe(1);
        result.Value.Errors[0].Message.ShouldContain("Email is required");
    }

    #endregion

    #region Tags Scenarios

    [Fact]
    public async Task Handle_WithTags_AddsTags()
    {
        // Arrange
        SetupNoExistingCustomers();

        Customer? capturedCustomer = null;
        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Callback<Customer, CancellationToken>((c, _) => capturedCustomer = c)
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            CreateImportDto("tagged@test.com", "Tagged", "User", tags: "vip,premium")
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(1);
        capturedCustomer.ShouldNotBeNull();
        capturedCustomer!.Tags.ShouldContain("vip");
        capturedCustomer.Tags.ShouldContain("premium");
    }

    #endregion

    #region Partial Failure Scenarios

    [Fact]
    public async Task Handle_WithPartialFailure_ReturnsMixedResult()
    {
        // Arrange
        SetupExistingCustomerEmails("existing@test.com");

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            CreateImportDto("new1@test.com", "New1", "User"),     // success
            CreateImportDto("existing@test.com", "Dupe", "User"), // fail: existing
            new("", "Empty", "User", null, null),                 // fail: missing email
            CreateImportDto("new2@test.com", "New2", "User")      // success
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Success.ShouldBe(2);
        result.Value.Failed.ShouldBe(2);
        result.Value.Errors.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_ErrorRowNumbers_AreCorrect()
    {
        // Arrange
        SetupNoExistingCustomers();

        _customerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken _) => c);

        var command = new BulkImportCustomersCommand(new List<ImportCustomerDto>
        {
            CreateImportDto("ok@test.com", "OK", "User"),        // row 2 (index 0 + header + 1-indexed)
            new("", "NoEmail", "User", null, null),               // row 3
            CreateImportDto("ok2@test.com", "OK2", "User")        // row 4
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value.Errors.Count().ShouldBe(1);
        result.Value.Errors[0].Row.ShouldBe(3); // 1-indexed + header row
    }

    #endregion
}
