# Roles Admin Pages — Testing Lessons & Approach

## Target Files

- **Index PageModel:** `src/IdentityServerAspNetIdentity/Pages/Admin/Roles/Index.cshtml.cs`
- **Edit PageModel:** `src/IdentityServerAspNetIdentity/Pages/Admin/Roles/Edit.cshtml.cs`
- **Razor Views:** `Index.cshtml`, `Edit.cshtml`

## Dependencies (Constructor-Injected)

| Dependency | Type | Purpose |
|---|---|---|
| `IRolesAdminService` | Service interface | Fetching roles, role details, and adding/removing user memberships |

## Test Files

| File | Level | Tests | Status |
|---|---|---|---|
| `Tests/Unit/.../Pages/Admin/Roles/IndexModelTests.cs` | Unit | 2 | Complete |
| `Tests/Unit/.../Pages/Admin/Roles/EditModelTests.cs` | Unit | 7 | Complete |
| `Tests/Integration/.../Pages/Admin/Roles/IndexModelIntegrationTests.cs` | Integration | 4 | Complete |
| `Tests/Integration/.../Pages/Admin/Roles/EditModelIntegrationTests.cs` | Integration | 5 | Complete |
| `Tests/E2E/.../Pages/Admin/Roles/RolesAdminE2eTests.cs` | E2E | 4 | Complete |

---

## Test Level Decision Framework

### What belongs where for Roles Admin

| Behaviour | Test Level | Reasoning |
|---|---|---|
| `OnGetAsync` — returns NotFound when role ID doesn't exist | **Unit** | Mock `GetRoleForEditAsync` to return null |
| `OnPostAddUserAsync` — model error when no user selected | **Unit** | Pure validation logic before service call |
| `OnPostAddUserAsync` — handle service failure status | **Unit** | Mock returns `Failed`, verify errors added to ModelState |
| `[Authorize(Roles = "ADMIN")]` enforcement | **Integration** | Requires the real middleware pipeline |
| 401 Unauthorized / 403 Forbidden scenarios | **Integration** | Verified via `CustomWebApplicationFactory<TAuth>` |
| Full data flow: Seed → DB → Service → HTML | **Integration** | Verifies the real `RolesAdminService` LINQ queries |
| Form submission actually updates the database | **Integration** | Uses real `UserManager` to verify state change after POST |
| "Edit" link navigation from Index | **E2E** | Verifies URL routing and user-facing link text |
| Success alert visibility after role update | **E2E** | Proof that the UI correctly displays feedback to the user |
| Validating custom TagHelper rendering (e.g. `ck-responsive-row`) | **E2E** | AngleSharp sees markup; only browser sees the final `tr` |

---

## Lessons Learned

### 1. EF Core InMemory Stale State
In integration tests, reusing the same `UserManager` or `DbContext` after an HTTP request can lead to false failures. The entities may be tracked in the pre-request scope and won't reflect changes made in the request's scope.
**Fix:** Always use a fresh DI scope (`_factory.Services.CreateScope()`) to resolve a new `UserManager` for post-request verification.

### 2. Form Visibility Depends on Seeding
The "Add User" form in `Edit.cshtml` only renders if `Model.AvailableUsers.Any()` is true.
**Impact:** Integration and E2E tests that attempt to submit the "Add" form will fail with "form not found" if you don't seed at least one user who is **not** already in the role.

### 3. Playwright vs. Razor TagHelpers
Custom TagHelpers like `<ck-responsive-row>` are visible to AngleSharp (integration tests) because it parses the raw server output. However, Playwright (E2E) sees the browser-rendered DOM where the TagHelper has been transformed into standard HTML (like `<tr>`).
**Rule:** Use standard HTML tags (`tr`, `button`) or ARIA roles in Playwright locators, even if the source code uses TagHelpers.

### 4. Handling Success Alerts in E2E
Redirecting after a POST results in a `200 OK` from the final destination page when using `SubmitForm`. 
**Verification:** Instead of checking for a `302`, verify the success alert text:
```csharp
await Expect(_page.Locator(".alert-success")).ToContainTextAsync("added to role");
```

---

## Test Coverage Summary

### Unit Tests

| Handler | Test | What it verifies |
|---|---|---|
| `Index.OnGetAsync` | PopulateRoles | Service returns data → Roles list populated |
| `Edit.OnGetAsync` | NotFound | Service returns null → 404 |
| `Edit.OnPostAddUserAsync` | ModelError | Empty SelectedUserId → validation error |
| `Edit.OnPostAddUserAsync` | Success | Valid input → redirect + TempData success message |
| `Edit.OnPostAddUserAsync` | FailedStatus | Service returns Failed → errors added to ModelState |

### Integration Tests

| Test | What it verifies |
|---|---|
| Index Forbidden | Non-admin gets 403 on `/Admin/Roles` |
| Index Unauthorized | Unauthenticated user gets 401 |
| Index ShowRoles | Seeded roles appear in the rendered HTML table |
| Edit AddUser (Success) | Form POST → redirect → user actually added in DB (fresh scope) |
| Edit AddUser (Error) | Empty dropdown → page re-renders with validation message |

### E2E Tests (Playwright)

| Test | What it verifies |
|---|---|
| Navigate Index → Edit | Click "Edit" link → URL changes to role edit page |
| Add User | Full UI journey: Select user → Click Add → Success alert + row appears |
| Remove User | Full UI journey: Click Remove → Success alert + row disappears |
| Validation Error | Click Add without selection → Error message visible in browser |
