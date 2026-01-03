# Contributing to NOIR

Thank you for your interest in contributing to NOIR! This document provides guidelines and instructions for contributing.

## Code of Conduct

Please read and follow our [Code of Conduct](CODE_OF_CONDUCT.md). We expect all contributors to be respectful and constructive.

## Getting Started

1. Read [SETUP.md](SETUP.md) for development environment setup
2. Review [CLAUDE.md](CLAUDE.md) for coding standards
3. Check the [docs/](docs/) folder for architecture documentation

## How to Contribute

### Reporting Issues

Before creating an issue:
- Check existing issues to avoid duplicates
- Use the appropriate issue template in `.github/ISSUE_TEMPLATE/`

When reporting bugs:
- Use a clear, descriptive title
- Include steps to reproduce
- Provide environment details (.NET version, OS, database)
- Attach relevant logs or error messages

### Submitting Changes

1. **Fork the repository** and create your branch from `main`
2. **Follow the coding standards** outlined in [CLAUDE.md](CLAUDE.md)
3. **Write tests** for new functionality
4. **Run all tests** before submitting: `dotnet test src/NOIR.sln`
5. **Build successfully**: `dotnet build src/NOIR.sln`
6. **Submit a pull request** using the PR template

## Coding Standards

### Architecture Rules

- Follow Clean Architecture - code belongs in the correct layer
- Use **Specifications** for all database queries (never raw `DbSet` queries)
- Add `TagWith("MethodName")` to all specifications for SQL debugging
- Use **marker interfaces** (`IScopedService`, etc.) for DI registration

### Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Handler | `[Command]Handler` | `CreateOrderHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |

### Code Style

- **No using statements in files** - add to `GlobalUsings.cs` instead
- **Soft delete only** - never hard delete unless explicitly required for GDPR
- Follow existing patterns in similar files

## Pull Request Process

1. Fill out the PR template completely
2. Ensure all tests pass
3. Update documentation if needed
4. Request review from maintainers
5. Address review feedback promptly

## Testing

```bash
# Run all tests (1,739+)
dotnet test src/NOIR.sln

# Run with coverage
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/NOIR.Application.UnitTests
```

### Testing Guidelines

- Unit tests for business logic (handlers, validators, specifications)
- Integration tests for API endpoints
- Use SQL Server for integration tests (matches production)
- Follow existing test patterns

## Documentation

All documentation is in the `docs/` folder:

| Location | Content |
|----------|---------|
| `docs/backend/` | Backend patterns and guides |
| `docs/frontend/` | Frontend architecture |
| `docs/decisions/` | Architecture Decision Records |

## Questions?

If you have questions, please open a discussion or issue on GitHub.

## License

By contributing, you agree that your contributions will be licensed under the Apache License 2.0.
