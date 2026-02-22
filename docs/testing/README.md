# Testing Documentation

**Backend Testing Infrastructure for NOIR Project**

---

## Testing at a Glance

### Backend Testing (10,889+ tests)

```bash
# Run all backend tests
dotnet test src/NOIR.sln

# Run specific test project
dotnet test tests/NOIR.Application.UnitTests
dotnet test tests/NOIR.Domain.UnitTests
dotnet test tests/NOIR.IntegrationTests
dotnet test tests/NOIR.ArchitectureTests
```

**Coverage:**
- 2,586 domain unit tests
- 7,483 application unit tests (handlers, validators, services)
- 788 integration tests (API endpoints with database)
- 32 architecture tests (dependency rules, naming conventions)

**Execution Time:** ~2 minutes for full suite

**Test Projects:**
- **NOIR.Domain.UnitTests** - Domain entity validation, business rules
- **NOIR.Application.UnitTests** - CQRS handlers, validators, specifications
- **NOIR.IntegrationTests** - End-to-end API testing with real database
- **NOIR.ArchitectureTests** - Architecture compliance (NetArchTest)

---

## Test Organization

### Domain Tests (`tests/NOIR.Domain.UnitTests/`)

Pure domain logic testing with no external dependencies:
- Entity validation
- Value object behavior
- Business rule enforcement
- Domain event handling

**Example:**
```csharp
public class ProductTests
{
    [Fact]
    public void Create_ValidData_ReturnsProduct()
    {
        // Arrange & Act
        var product = Product.Create("Test Product", "SKU001");

        // Assert
        product.Should().NotBeNull();
        product.Name.Should().Be("Test Product");
    }
}
```

### Application Tests (`tests/NOIR.Application.UnitTests/`)

CQRS command/query handler testing with mocked dependencies:
- Command handlers
- Query handlers
- FluentValidation validators
- Specifications
- Service implementations

**Example:**
```csharp
public class CreateProductCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var repository = new Mock<IRepository<Product, Guid>>();
        var handler = new CreateProductCommandHandler(repository.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
```

### Integration Tests (`tests/NOIR.IntegrationTests/`)

Full API endpoint testing with real database:
- HTTP request/response validation
- Database state verification
- Authentication/authorization
- Multi-tenancy isolation

**Example:**
```csharp
public class ProductsEndpointsTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateProduct_ValidData_ReturnsCreated()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

### Architecture Tests (`tests/NOIR.ArchitectureTests/`)

Automated architecture rule enforcement:
- Layer dependency rules (Clean Architecture)
- Naming convention compliance
- Handler registration validation
- Repository pattern enforcement

**Example:**
```csharp
[Fact]
public void Domain_ShouldNotDependOn_Application()
{
    Types().That().ResideInNamespace("NOIR.Domain")
        .ShouldNot().HaveDependencyOn("NOIR.Application")
        .Check(Architecture);
}
```

---

## Running Tests

### All Tests
```bash
dotnet test src/NOIR.sln
```

### With Coverage
```bash
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"
```

### Specific Category
```bash
# Domain tests only
dotnet test tests/NOIR.Domain.UnitTests

# Integration tests only (requires database)
dotnet test tests/NOIR.IntegrationTests
```

### By Test Name
```bash
dotnet test --filter "FullyQualifiedName~CreateProduct"
```

---

## Test Conventions

### Naming
- Test class: `{ClassUnderTest}Tests`
- Test method: `{Method}_{Scenario}_{ExpectedBehavior}`
- Example: `CreateProduct_ValidData_ReturnsSuccess`

### Structure (AAA Pattern)
```csharp
[Fact]
public async Task Method_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies

    // Act - Execute the method under test

    // Assert - Verify the expected outcome
}
```

### Assertions
- Use FluentAssertions for readable assertions
- Example: `result.IsSuccess.Should().BeTrue()`

---

## Continuous Integration

Tests run automatically on every push to `main` branch via GitHub Actions.

**Backend Tests Workflow:**
- Builds solution
- Runs all 10,889+ tests
- Fails build if any test fails
- Execution time: ~3 minutes

---

## Writing New Tests

### For New Features
1. **Domain Tests** - Test entity creation and business rules
2. **Application Tests** - Test command/query handlers
3. **Integration Tests** - Test API endpoints end-to-end
4. **Architecture Tests** - Add rules if introducing new patterns

### Best Practices
- ✅ Write tests for all new code
- ✅ Test both success and failure paths
- ✅ Use meaningful test names
- ✅ Keep tests fast and isolated
- ✅ Mock external dependencies in unit tests
- ✅ Use real database in integration tests
- ✅ Follow AAA pattern (Arrange, Act, Assert)
- ❌ Don't test framework code (EF Core, FluentValidation)
- ❌ Don't share state between tests

---

## Test Coverage Goals

| Layer | Current Coverage | Goal |
|-------|-----------------|------|
| Domain | ~95% | 95%+ |
| Application | ~90% | 90%+ |
| Integration | ~70% | 75%+ |
| Overall | ~85% | 85%+ |

---

## Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/devlooped/moq)
- [NetArchTest Documentation](https://github.com/BenMorris/NetArchTest)

---

**Last Updated:** 2026-02-23
**Test Count:** 10,889+ tests
**Focus:** Backend unit, integration, and architecture testing
