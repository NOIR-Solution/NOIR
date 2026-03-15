using NOIR.Application.Features.Crm.Queries.GetContactById;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetContactByIdQueryHandlerTests
{
    private readonly Mock<IRepository<CrmContact, Guid>> _contactRepoMock;
    private readonly GetContactByIdQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetContactByIdQueryHandlerTests()
    {
        _contactRepoMock = new Mock<IRepository<CrmContact, Guid>>();
        _handler = new GetContactByIdQueryHandler(_contactRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingContact_ShouldReturnDto()
    {
        // Arrange
        var contact = CrmContact.Create(
            "John", "Doe", "john@example.com",
            ContactSource.Web, TestTenantId,
            phone: "555-0100", jobTitle: "CTO");

        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdReadOnlySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contact);

        var query = new GetContactByIdQuery(contact.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(contact.Id);
        result.Value.FirstName.ShouldBe("John");
        result.Value.LastName.ShouldBe("Doe");
        result.Value.Email.ShouldBe("john@example.com");
    }

    [Fact]
    public async Task Handle_NonExistentContact_ShouldReturnNotFound()
    {
        // Arrange
        _contactRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ContactByIdReadOnlySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CrmContact?)null);

        var query = new GetContactByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
    }
}
