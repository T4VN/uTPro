# Contributing to uTPro

Thank you for your interest in contributing to **uTPro - Umbraco Turbo Pro**!

## How to Contribute

1. **Fork** the repository
2. **Create a branch** for your feature or fix: `git checkout -b feature/my-feature`
3. **Make your changes** following the project conventions below
4. **Build and test**: `dotnet build uTPro/uTPro.sln`
5. **Commit** with a clear message: `git commit -m "Add: description of change"`
6. **Push** and create a **Pull Request**

## Project Conventions

- Follow the modular architecture (Common, Extension, Foundation, Feature, Project)
- Use `IComposer` pattern for DI registration
- New features go in `Feature/uTPro.Feature.{Name}/`
- Backoffice extensions go in `App_Plugins/{name}/` with `umbraco-package.json`
- Use Management API pattern (`ManagementApiControllerBase`) for backoffice APIs
- Use `ControllerBase` for public-facing APIs
- All POST endpoints for Management API (to avoid 404 redirect conflicts)

## Reporting Issues

- Use [GitHub Issues](https://github.com/T4VN/uTPro/issues)
- Include Umbraco version, .NET version, and steps to reproduce

## Questions & Support

- Email: [thientu@t4vn.com](mailto:thientu@t4vn.com)
- Website: [t4vn.com](https://t4vn.com)

**SPECIAL:** We also offer a **LOW COST premium** version for those who want exclusive customization tailored to their personal style. Your support means a lot to us!
