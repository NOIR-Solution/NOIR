# Architecture Decision Records (ADRs)

This folder contains Architecture Decision Records documenting significant technical decisions for the NOIR project.

## What is an ADR?

An Architecture Decision Record (ADR) captures an important architectural decision made along with its context and consequences. ADRs are:

- **Immutable** - Once accepted, ADRs are not modified (superseded instead)
- **Numbered** - Sequential numbering for easy reference
- **Dated** - Record when the decision was made
- **Contextual** - Explain why the decision was needed

## ADR Index

| Number | Title | Status | Date |
|--------|-------|--------|------|
| [001](001-tech-stack.md) | Technology Stack Selection | Accepted | 2025-12-29 |
| [002](002-frontend-ui-stack.md) | Frontend UI Stack | Accepted | 2026-01-03 |

## ADR Template

When creating new ADRs, use this template:

```markdown
# [Number] - [Title]

**Date:** YYYY-MM-DD
**Status:** Proposed | Accepted | Deprecated | Superseded by [ADR-XXX]
**Deciders:** [List of people involved]

## Context

[Describe the issue or opportunity that led to this decision]

## Decision

[Describe the decision that was made]

## Consequences

### Positive
- [Benefit 1]
- [Benefit 2]

### Negative
- [Tradeoff 1]
- [Tradeoff 2]

## Alternatives Considered

### [Alternative 1]
- Pros: ...
- Cons: ...

## References

- [Link to relevant documentation]
```

## Naming Convention

ADR files use the format: `NNN-kebab-case-title.md`

Examples:
- `001-tech-stack.md`
- `002-frontend-ui-stack.md`
- `003-authentication-strategy.md`

## When to Write an ADR

Write an ADR when:

1. **Choosing technologies** - Framework, library, or platform selection
2. **Architectural patterns** - Design patterns affecting multiple components
3. **Infrastructure decisions** - Hosting, CI/CD, monitoring strategies
4. **Breaking changes** - Changes affecting backward compatibility
5. **Significant tradeoffs** - Decisions with notable pros/cons
