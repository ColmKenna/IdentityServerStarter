# IdentityServerAspNetIdentity.IntegrationTests

This project contains server-side integration tests built on `WebApplicationFactory`, in-memory databases, and AngleSharp DOM assertions.

## Project boundaries

- `Tests/Integration/IdentityServerAspNetIdentity.IntegrationTests`
  - HTTP and Razor Page integration tests
  - AngleSharp helpers and integration-only HTTP helpers
- `Tests/Common/IdentityServerAspNetIdentity.TestSupport`
  - shared `CustomWebApplicationFactory`, auth handler, and seed helpers
- `Tests/E2E/IdentityServerAspNetIdentity.E2ETests`
  - Playwright browser tests

## Run tests

```bash
dotnet test Tests/Integration/IdentityServerAspNetIdentity.IntegrationTests/IdentityServerAspNetIdentity.IntegrationTests.csproj
```

To run browser tests, use the separate E2E project:

```bash
dotnet test Tests/E2E/IdentityServerAspNetIdentity.E2ETests/IdentityServerAspNetIdentity.E2ETests.csproj
```

## Key test support

- `IdentityServerAspNetIdentity.TestSupport.Infrastructure.CustomWebApplicationFactory`
- `IdentityServerAspNetIdentity.TestSupport.Infrastructure.TestDataHelper`
- `Infrastructure/AngleSharpExtensions.cs`
- `Infrastructure/HttpClientExtensions.cs`
- `Infrastructure/AntiforgeryTokenHelper.cs`

## Adding tests here

- Keep browser automation out of this project.
- Use `CustomWebApplicationFactory` from the shared support project.
- Use `TestDataHelper` for common seeding instead of duplicating user/role/api-scope setup.
- Add DOM assertions with AngleSharp when the behavior can be verified without a real browser.
