# Clients Edit Stable E2E Selectors

Status: Open

Priority: Medium

## Problem

The Clients edit page currently relies on tab labels, fieldset IDs, button text, and rendered text content for browser automation. It does not expose stable `data-testid` hooks for the higher-friction areas of the UI.

## Why This Matters

The new Playwright coverage works, but it is still more brittle than it should be. Harmless text or markup changes can break tests that are meant to validate behavior, not wording.

## Evidence

- `src/IdentityServerAspNetIdentity/Pages/Admin/Clients/Edit.cshtml:125`
- `src/IdentityServerAspNetIdentity/Pages/Admin/Clients/Edit.cshtml:149`
- `Tests/E2E/IdentityServerAspNetIdentity.E2ETests/Pages/Admin/Clients/EditModelE2eTests.cs`

## Suggested Next Step

Add stable `data-testid` attributes for:

- the success alert
- the grant-types and scopes sections
- the URI editor hosts
- the primary submit button

## Acceptance

- Playwright tests can target the critical UI with `data-testid` instead of text-dependent selectors.
- Cosmetic markup changes do not force E2E locator rewrites.
