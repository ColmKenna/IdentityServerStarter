# ApiScopes E2E Obsolete Navigation API

Status: Open

Priority: Low

## Problem

An existing API scopes Playwright test still uses the obsolete `RunAndWaitForNavigationAsync` helper.

## Why This Matters

The test still passes, but the suite emits a compiler warning and keeps old Playwright usage alive in the codebase.

## Evidence

- `Tests/E2E/IdentityServerAspNetIdentity.E2ETests/Pages/Admin/ApiScopes/EditModelE2eTests.cs:58`

## Suggested Next Step

Replace the obsolete call with the same pattern used in the Clients E2E tests:

- `Task.WhenAll(...)`
- `WaitForURLAsync(...)`
- button click

## Acceptance

- The API scopes E2E test no longer emits the obsolete API warning.
- Navigation waiting is explicit and consistent across the browser test suite.
