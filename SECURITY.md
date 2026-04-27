# Security Policy

## Supported Versions

| Version | Supported |
|---------|-----------|
| Umbraco 16.x + .NET 9 | Yes |
| Umbraco 15.x + .NET 9 | Best effort |
| Older versions | No |

## Built-in Security Features

uTPro includes the following security hardening out of the box:

- **HTTP Security Headers**: X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy
- **HSTS**: Strict-Transport-Security in production (1 year, includeSubDomains)
- **Secure Sessions**: HttpOnly, Secure, SameSite=Strict cookies
- **Request Limits**: 128MB max upload, protection against oversized payloads
- **Domain Policy**: Configurable domain allowlist with wildcard matching
- **Data Protection**: Encryption keys with 90-day automatic rotation
- **XSS Prevention**: HTML-encoded meta tag values in SEO output
- **Error Pages**: noindex/nofollow to prevent search engine indexing of error pages

## Reporting a Vulnerability

If you discover a security vulnerability, please report it responsibly:

- Email: [thientu@t4vn.com](mailto:thientu@t4vn.com)
- GitHub Issues: [uTPro Issues](https://github.com/T4VN/uTPro/issues) (use "security" label)

We aim to respond within 48 hours of receiving your report.

Please do **not** disclose security vulnerabilities publicly until we have had a chance to address them.

### Thank you for helping us improve the security and reliability of uTPro.
