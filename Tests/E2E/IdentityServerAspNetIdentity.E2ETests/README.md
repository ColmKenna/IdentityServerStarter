# IdentityServerAspNetIdentity.E2ETests

This project contains Playwright-based browser tests for `IdentityServerAspNetIdentity`.

## Prerequisites

- .NET 10 SDK
- Playwright browser binaries

## Build and install browsers

```bash
dotnet build Tests/E2E/IdentityServerAspNetIdentity.E2ETests/IdentityServerAspNetIdentity.E2ETests.csproj
powershell -ExecutionPolicy Bypass -File Tests/E2E/IdentityServerAspNetIdentity.E2ETests/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Run tests

```bash
dotnet test Tests/E2E/IdentityServerAspNetIdentity.E2ETests/IdentityServerAspNetIdentity.E2ETests.csproj
```

## Shared support

The browser tests reuse the shared test host and seed helpers from:

- `Tests/Common/IdentityServerAspNetIdentity.TestSupport`

## Current coverage

- API scope create/edit browser flow
- Claims admin index/edit/add/remove browser flow
