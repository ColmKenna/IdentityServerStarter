# Clients Edit URI Hydration Observability

Status: Open

Priority: Medium

## Problem

`clients-edit.js` hydrates `ck-edit-array` by reading `data-initial-json` and assigning `el.data`. If parsing or initialization fails, it only logs to the browser console.

## Why This Matters

The page can still return `200 OK`, and server-side integration tests can still pass, while the browser UI silently fails to render existing redirect URIs or post-logout redirect URIs.

## Evidence

- `src/IdentityServerAspNetIdentity/wwwroot/js/clients-edit.js`
- `src/IdentityServerAspNetIdentity/Pages/Admin/Clients/Edit.cshtml:153`
- `src/IdentityServerAspNetIdentity/Pages/Admin/Clients/Edit.cshtml:174`

## Suggested Next Step

Add one of these source-side signals after successful hydration:

- a visible fallback/error state inside each URI editor when hydration fails
- a deterministic custom event such as `clients-edit:hydrated`

## Acceptance

- Hydration success is observable without reading console output.
- Hydration failure is visible in the UI or emitted as a deterministic browser event.
- A browser test can detect success or failure directly.
