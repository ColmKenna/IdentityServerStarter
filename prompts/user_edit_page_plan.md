## User Edit Page — Implementation Plan

This document captures the implementation plan for the User Edit (detail) pages for the IdentityServer admin site. It is based on the project patterns and the user admin specification.

## Context & Observations

These notes capture useful, concrete findings from the repository that influenced the plan and will help implementation:

- Admin pages currently present in the project: `Pages/Admin/Accounts` (user list) and `Pages/Admin/Clients` (list + edit). The `Clients/Edit` page is the canonical tabbed edit example.
- Authorization currently uses role-based checks on admin pages (`[Authorize(Roles = "ADMIN")]`) rather than claim-based policies; no custom authorization policies are registered in `HostingExtensions.cs` yet.
- The `Clients/Edit` page uses client-side web components for tabs and arrays (`<ck-tabs>`, `<ck-tab>`, `<ck-edit-array>`) and named POST handlers for dynamic operations — follow that UI pattern (or create per-spec separate pages for each tab).
- No custom base `PageModel` classes exist for admin pages—pages inherit `PageModel` directly; the plan introduces a `UserDetailPageModel` to centralize user-loading and auth helpers.
- The project uses `TempData["Success"]` and inline alert divs for success toasts; reuse this pattern for save/delete confirmations.
- No confirmation dialog/modal implementation is currently used in the repo; the theming CSS includes modal styles, so adding a small reusable modal partial + JS is recommended for destructive actions.
- Tag helpers and components available:
	- Project tag helper: `MultiselectCheckboxTagHelper` (used for multi-select role/scope UIs).
	- NPM webcomponents: `@colmkenna/ck-tabs`, `ck-responsive-table`, `ck-edit-array` are already used by `Clients/Edit`.
- Identity user model: `ApplicationUser : IdentityUser` with an extra `FavoriteColor` property (see `ApplicationUser` in the data project). There is also a composite `ApplicationUserWithClaims` DTO used elsewhere.
- Seed data currently creates admin users but does not yet assign the `admin:*` claim set required by the new spec; update `SeedData.cs` if you want seeded claim-based admin users.
- Layout and navigation:
	- Sidebar includes a `Users` link pointing to `/Admin/Accounts/Index` — the plan recommends moving/creating `Pages/Admin/Users` with routes matching the spec (`/Admin/Users`, `/Admin/Users/{id}`, etc.).
	- `_ViewImports.cshtml` already registers `CK.Taghelpers` in addition to project tag helpers.
- Validation uses jQuery Unobtrusive validation via `_ValidationScriptsPartial.cshtml`.

Include these details when implementing so the work aligns with existing patterns and reuses project helpers/components.

---

## Phase 0: Infrastructure & Cross-Cutting

- [x] **0.1 Add authorization policies** — Register claim-based policies (`UsersRead`, `UsersWrite`, `UsersDelete`, etc.) in `HostingExtensions.cs` so pages can use `[Authorize(Policy = "...")]`.
- [x] **0.2 Create `UserPolicyConstants.cs`** — Define string constants for the user-related policy names.
- [x] **0.3 Build confirmation dialog system** — Implement a reusable confirmation modal partial + minimal JS to invoke it for destructive actions.
- [x] **0.4 Seed admin claims for test users** — Update `SeedData.cs` to grant `admin:users`, `admin:user_claims`, `admin:user_roles`, `admin:user_grants`, `admin:user_sessions` to admin accounts.

---

## Phase 1: Shared Tab Infrastructure

- [x] **1.1 `_UserDetailLayout.cshtml` partial** — renders the tab bar for the User Detail pages and shows/hides tabs using `IAuthorizationService`.
- [x] **1.2 `UserDetailPageModel` base class** — common logic for loading user by id, injecting `UserManager<ApplicationUser>` and `IAuthorizationService`, and self-detection helper.
- [x] **1.3 Breadcrumb partial** — shared breadcrumb (Home → Users → {Username} → {Tab}).

---

## Phase 2: Profile Tab (Default Landing Tab)

- Route: `/Admin/Users/{userId}`

- [x] Create `ProfileViewModel` (UserId, Username, Email, EmailConfirmed, PhoneNumber, PhoneNumberConfirmed) with validation.
- [x] `Edit.cshtml.cs` extends `UserDetailPageModel`: `OnGetAsync` loads user; `OnPostAsync` checks `UsersWrite` policy, updates username/email/phone via `UserManager`, maps `IdentityResult` errors, implements concurrency handling.
- [x] `Edit.cshtml` uses `<ck-tabs>` partial, shows Save only when `write` claim present, Delete button visible with `delete` claim, uses `TempData["Success"]` for toast.
- [x] Implement `OnPostDeleteAsync` protected from self-delete (400) and requiring `UsersDelete` policy.

---

## Phase 3: Security Tab

- Route: `/Admin/Users/{userId}/Security`
- [ ] `SecurityViewModel` with HasPassword, AccountStatus, LockoutEnd, LockoutEnabled, AccessFailedCount, TwoFactorEnabled, TwoFactorProviders.
- [ ] `Security.cshtml.cs` handles named POST handlers for: reset password, disable/enable account, clear lockout, toggle lockout enabled, reset failed count, toggle 2FA, reset authenticator key, force sign-out. All modifying actions require `UsersWrite` policy and self-protection warnings where appropriate.
- [ ] `Security.cshtml` UI with sections for Password, Lockout, Two-Factor and Force Sign-Out; destructive actions show confirmation dialog.

---

## Phase 4: Claims Tab

- Route: `/Admin/Users/{userId}/Claims`
- [ ] `Claims.cshtml.cs` with `UserClaimsRead`/`UserClaimsWrite`/`UserClaimsDelete` checks. `OnGetAsync` loads claims; `OnPostAddAsync`, `OnPostRemoveAsync`, `OnPostReplaceAsync` implement add/remove/replace flows. Self-protection warnings when modifying own `admin:*` claims.
- [ ] `Claims.cshtml` uses `<ck-responsive-table>`, provides Add form (supports multiple entries) and bulk remove with confirmation.

---

## Phase 5: Roles Tab

- Route: `/Admin/Users/{userId}/Roles`
- [ ] `Roles.cshtml.cs` uses `RoleManager` and `UserManager` to list assigned roles and available roles. `OnPostAddAsync` and `OnPostRemoveAsync` require `UserRolesWrite`/`UserRolesDelete`. Self-protection when removing self from admin role.
- [ ] `Roles.cshtml` shows assigned roles table and a multi-select to add roles (reuse `multiselect-checkbox`).

---

## Phase 6: External Logins Tab

- Route: `/Admin/Users/{userId}/ExternalLogins`
- [ ] Read-only `ExternalLogins.cshtml.cs` uses `GetLoginsAsync` and `ExternalLogins.cshtml` shows provider display name and key.

---

## Phase 7: Grants & Sessions Tab

- Route: `/Admin/Users/{userId}/GrantsSessions`
- [ ] `GrantsSessions.cshtml.cs` injects `IPersistedGrantStore` and `IServerSideSessionStore`, loads grants and sessions filtered by `SubjectId`. POST handlers for revoking grant(s) and ending session(s). Policies: `UserGrantsRead`/`UserGrantsDelete`, `UserSessionsRead`/`UserSessionsDelete` as appropriate. Self-protection warnings for self-affecting operations.
- [ ] `GrantsSessions.cshtml` shows Grants and Sessions sections, per-row revoke/end and Revoke All / End All buttons.

---

## Phase 8: Navigation & List Page Updates

- [ ] Update sidebar link to point to `/Admin/Users` and create `Pages/Admin/Users/Index` (or migrate `Accounts/Index`) to match spec table columns (Username, Email, Email Confirmed, Lockout Status, 2FA Enabled). Add Create User page (`/Admin/Users/Create`) with Username, Email, optional Password.

---

## Phase 9: Testing

- [ ] Add unit tests for PageModels using mocked `UserManager`/`RoleManager`/`IPersistedGrantStore`/`IAuthorizationService` following existing tests patterns. Include tests for self-protection, authorization, and `IdentityResult` error mapping.

---

## Suggested Implementation Order

1. Phase 0 (Infrastructure)
2. Phase 1 (Shared tabs)
3. Phase 8 (List upgrade)
4. Phase 2 (Profile)
5. Phase 3 (Security)
6. Phase 4 (Claims)
7. Phase 5 (Roles)
8. Phase 6 (External Logins)
9. Phase 7 (Grants & Sessions)
10. Phase 9 (Tests)

---

## Notes & Decisions

- Tab routing: spec requires separate Razor Pages per tab; that differs from the single-page Client edit pattern. This plan follows the spec and creates separate pages that share a `_UserDetailLayout` partial.
- Authorization: existing admin pages use `[Authorize(Roles = "ADMIN")]`. Introducing claim-based policies will mix models unless all admin pages are migrated. Consider migrating or scoping changes to Users only.
- Confirmation dialogs: quick implementation could use `window.confirm()`; ideal is a styled modal using existing theming variables.

---

If you want, I can now create the `prompts/user_edit_page_plan.md` file in the repository (this file), update `SeedData.cs` and `HostingExtensions.cs` with policy skeletons, or scaffold the first pages (`Users/Index` and `Users/Edit`)—tell me which step to start next.
