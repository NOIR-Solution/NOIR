# Contributing to NOIR

Thank you for your interest in contributing to NOIR! This document provides guidelines and instructions for contributing.

## Code of Conduct

Please be respectful and constructive in all interactions. We welcome contributors of all experience levels.

## How to Contribute

### Reporting Issues

- Check existing issues before creating a new one
- Use a clear, descriptive title
- Include steps to reproduce for bugs
- Provide environment details (.NET version, OS, database)

### Submitting Changes

1. **Fork the repository** and create your branch from `main`
2. **Follow the coding standards** outlined in [CLAUDE.md](CLAUDE.md)
3. **Write tests** for new functionality
4. **Run all tests** before submitting: `dotnet test src/NOIR.sln`
5. **Build successfully**: `dotnet build src/NOIR.sln`
6. **Submit a pull request** with a clear description

## Development Setup

See [SETUP.md](SETUP.md) for comprehensive development environment setup instructions for Windows, macOS, and Linux.

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
- **Soft delete only** - never hard delete unless explicitly required
- Follow existing patterns in similar files

## Pull Request Process

1. Update documentation if needed
2. Add tests for new functionality
3. Ensure all tests pass
4. Update the README.md if applicable
5. PR will be reviewed by maintainers

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

- Pattern documentation is in `.claude/patterns/`
- Decision records are in `.claude/decisions/`
- AI assistant instructions are in `CLAUDE.md`

## Questions?

If you have questions, please open a discussion or issue on GitHub.

## License

By contributing, you agree that your contributions will be licensed under the Apache License 2.0.
