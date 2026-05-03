# Contributing

Thank you for your interest in contributing to Task Tracker!

## Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes
4. Add tests for any new business logic
5. Run the test suite: `dotnet test`
6. Commit your changes: `git commit -m "feat: add your feature"`
7. Push to your branch: `git push origin feature/your-feature-name`
8. Open a Pull Request

## Architecture

Task Tracker uses a 3-layer architecture:
- **Controllers** — thin HTTP layer; delegate to services
- **Services** (`ITaskService`) — business logic, ownership checks, timestamps
- **Repositories** — raw ADO.NET SQL; `CancellationToken` support

New features should follow this pattern: controller calls service, service calls repository.

## Code Style

- All SQL queries must be parameterized — no string concatenation
- Password handling: use `PasswordHelper` (PBKDF2-SHA256, timing-safe verify)
- New service methods should have corresponding unit tests (xUnit + Moq)

## Pull Request Guidelines

- Keep PRs focused on a single concern
- Include a clear description of what changed and why
- Ensure all existing tests pass (`dotnet test`)
- Add tests for new features or bug fixes

## Reporting Issues

Please use [GitHub Issues](../../issues) to report bugs or request features.
