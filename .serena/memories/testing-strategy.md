# NOIR Testing Strategy

## Test Count
5,370+ tests across all projects

## Test Location
`tests/`

## Test Stack
| Package | Purpose |
|---------|---------|
| xUnit | Test framework |
| Moq | Mocking |
| Shouldly | Assertion library |
| Bogus | Fake data generation |
| Microsoft.AspNetCore.Mvc.Testing | Integration tests |

## Test Categories

### Unit Tests
- Domain logic
- Application handlers
- Specifications
- Mappers

### Integration Tests
- API endpoints
- Database operations
- Authentication flows

## Running Tests
```bash
# All tests
dotnet test src/NOIR.sln

# Specific project
dotnet test tests/NOIR.Domain.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Test Patterns

### Handler Tests
```csharp
public class CreateUserHandlerTests
{
    private readonly Mock<IRepository<User, Guid>> _mockRepo = new();

    [Fact]
    public async Task Handle_ValidCommand_CreatesUser()
    {
        // Arrange
        var command = new CreateUserCommand("test@example.com", "password");
        
        // Act
        var result = await CreateUserHandler.Handle(command, _mockRepo.Object, CancellationToken.None);
        
        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
```

### Specification Tests
```csharp
[Fact]
public void ActiveCustomersSpec_FiltersInactiveCustomers()
{
    var spec = new ActiveCustomersSpec();
    var customers = new List<Customer> { active, inactive };
    
    var result = customers.AsQueryable().Where(spec.WhereExpressions.First()).ToList();
    
    result.ShouldHaveSingleItem().ShouldBe(active);
}
```

## Test Database
- Unit tests: Mocked (no DB)
- Integration tests: SQL Server LocalDB
