# Integration Test Guide

## Purpose

Use this project for full request-pipeline tests that can run against `WebApplicationFactory` without a browser.

Use the E2E project instead when the scenario depends on:

- browser navigation or redirects as seen by a real browser
- client-side behavior
- Playwright locators or role-based browser assertions

## Shared infrastructure

Shared host and seed helpers now live in:

- `Tests/Common/IdentityServerAspNetIdentity.TestSupport/Infrastructure/CustomWebApplicationFactory.cs`
- `Tests/Common/IdentityServerAspNetIdentity.TestSupport/Infrastructure/TestDataHelper.cs`

Integration-only helpers stay in this project:

- `Infrastructure/AngleSharpExtensions.cs`
- `Infrastructure/HttpClientExtensions.cs`
- `Infrastructure/AntiforgeryTokenHelper.cs`

## Basic pattern

```csharp
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

public class ExampleIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public ExampleIntegrationTests()
    {
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_Page_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/some-page");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## Commands

```bash
dotnet test Tests/Integration/IdentityServerAspNetIdentity.IntegrationTests/IdentityServerAspNetIdentity.IntegrationTests.csproj
dotnet test Tests/E2E/IdentityServerAspNetIdentity.E2ETests/IdentityServerAspNetIdentity.E2ETests.csproj
```

## Rules of thumb

- Prefer `TestDataHelper` for common seeding.
- Prefer AngleSharp assertions when server-rendered HTML is sufficient.
- Move tests to the E2E project only when a real browser materially changes what is being verified.
