# Clients Edit RequestAborted Propagation

Status: Open

Priority: Medium

## Problem

`Clients/Edit` does not propagate `HttpContext.RequestAborted` into `IClientAdminService`, even though the service interface accepts a `CancellationToken`.

## Why This Matters

`Clients/Index` already forwards request cancellation, but `Clients/Edit` does not. That makes edit operations inconsistent and weakens cancellation behavior for GET and POST handlers.

## Evidence

- `src/IdentityServerAspNetIdentity/Pages/Admin/Clients/Index.cshtml.cs:22`
- `src/IdentityServerAspNetIdentity/Pages/Admin/Clients/Edit.cshtml.cs:27`
- `src/IdentityServerAspNetIdentity/Pages/Admin/Clients/Edit.cshtml.cs:42`
- `src/IdentityServerAspNetIdentity/Pages/Admin/Clients/Edit.cshtml.cs:51`
- `src/IdentityServerServices/IClientAdminService.cs:8`
- `src/IdentityServerServices/IClientAdminService.cs:9`

## Suggested Next Step

Capture `var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;` in the edit handlers and pass it into:

- `GetClientForEditAsync`
- `UpdateClientAsync`

## Acceptance

- `Clients/Edit` matches the cancellation pattern already used in other admin pages.
- Unit tests can verify token propagation for edit GET and POST handlers.
