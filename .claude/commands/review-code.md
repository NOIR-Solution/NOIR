# Code Review Command

Review code changes for NOIR project standards compliance.

## Checklist

When reviewing "$ARGUMENTS", check:

### Architecture
- [ ] Code in correct layer (Domain/Application/Infrastructure/Web)
- [ ] No direct DbContext usage in Application layer
- [ ] Repository pattern used for data access
- [ ] Specifications used for complex queries

### Performance
- [ ] Specifications have `TagWith()` for SQL debugging
- [ ] `AsSplitQuery()` used for multiple collection includes
- [ ] No N+1 query issues (check includes)
- [ ] `CancellationToken` passed to async methods

### Security
- [ ] No sensitive data in logs
- [ ] Input validation with FluentValidation
- [ ] Authorization checks where needed

### Code Quality
- [ ] Follows naming conventions (`{Entity}{Filter}Spec`, `{Action}{Entity}Command`)
- [ ] No magic strings (use constants)
- [ ] Proper null handling
- [ ] XML documentation on public APIs

### Testing
- [ ] Unit tests for business logic
- [ ] Integration tests for handlers

## Common Issues

1. **Missing TagWith**: All specifications must have `.TagWith("MethodName")`
2. **Raw DbSet queries**: Use specifications, not `_dbContext.Set<T>().Where(...)`
3. **Forgotten CancellationToken**: All async methods should accept and pass CT
4. **Cartesian explosion**: Use `.AsSplitQuery()` when including multiple collections
