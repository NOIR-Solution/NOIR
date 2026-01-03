# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | :white_check_mark: |

## Reporting a Vulnerability

If you discover a security vulnerability, please report it responsibly:

1. **Do not** open a public issue
2. Email security concerns to the project maintainers
3. Include detailed information about the vulnerability
4. Allow reasonable time for a fix before public disclosure

## Security Best Practices

This project implements:

- JWT authentication with refresh token rotation
- Password hashing with ASP.NET Core Identity
- Rate limiting on authentication endpoints
- HTTPS enforcement in production
- SQL injection prevention via parameterized queries
- XSS prevention via output encoding
- CSRF protection
- Security headers (CSP, HSTS, X-Frame-Options)

## Development Security

When contributing:

- Never commit secrets or credentials
- Use environment variables for sensitive configuration
- Follow the soft delete pattern (no hard deletes unless GDPR-required)
- Review dependencies for known vulnerabilities
