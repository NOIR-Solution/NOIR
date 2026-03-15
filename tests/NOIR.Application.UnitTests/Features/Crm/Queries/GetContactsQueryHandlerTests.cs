using NOIR.Application.Features.Crm.Queries.GetContacts;
using NOIR.Domain.Entities.Crm;

namespace NOIR.Application.UnitTests.Features.Crm.Queries;

public class GetContactsQueryHandlerTests
{
    private readonly Mock<IRepository<CrmContact, Guid>> _contactRepoMock;
    private readonly GetContactsQueryHandler _handler;

    private const string TestTenantId = "tenant-123";

    public GetContactsQueryHandlerTests()
    {
        _contactRepoMock = new Mock<IRepository<CrmContact, Guid>>();
        _handler = new GetContactsQueryHandler(_contactRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldReturnPagedResult()
    {
        // Arrange
        var contacts = new List<CrmContact>
        {
            CrmContact.Create("John", "Doe", "john@example.com", ContactSource.Web, TestTenantId),
            CrmContact.Create("Jane", "Smith", "jane@example.com", ContactSource.Referral, TestTenantId)
        };

        _contactRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ContactsFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);
        _contactRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ContactsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var query = new GetContactsQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        _contactRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ContactsFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmContact>());
        _contactRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ContactsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetContactsQuery(Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldPassToSpec()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _contactRepoMock
            .Setup(x => x.ListAsync(It.IsAny<ContactsFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmContact>());
        _contactRepoMock
            .Setup(x => x.CountAsync(It.IsAny<ContactsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetContactsQuery(
            Search: "John",
            CompanyId: companyId,
            Source: ContactSource.Web,
            Page: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _contactRepoMock.Verify(
            x => x.ListAsync(It.IsAny<ContactsFilterSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
