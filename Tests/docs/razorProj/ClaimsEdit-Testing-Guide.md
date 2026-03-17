# Claims Edit Page — Testing Lessons & Approach

## Target Files

- **PageModel:** `src/IdentityServerAspNetIdentity/Pages/Admin/Claims/Edit.cshtml.cs`
- **Razor View:** `src/IdentityServerAspNetIdentity/Pages/Admin/Claims/Edit.cshtml`

## Dependencies (Constructor-Injected)

| Dependency | Type | Purpose |
|---|---|---|
| `IClaimsAdminService` | Service interface | All claim CRUD operations (add/remove user assignments, fetch edit page data) |

Single dependency — makes this an ideal unit test target.

## Test Files

| File | Level | Tests | Status |
|---|---|---|---|
| `Tests/Unit/.../Pages/Admin/Claims/EditModelTests.cs` | Unit | 26 | Complete |
| `Tests/Integration/.../Pages/Admin/Claims/EditModelIntegrationTests.cs` | Integration | 9 | Complete |
| `Tests/E2E/.../Pages/Admin/Claims/EditModelE2eTests.cs` | E2E | 5 | Complete (3 existing + 2 added) |
| `Tests/E2E/.../Pages/Admin/Claims/ClaimsAdminE2eTests.cs` | E2E | 4 | Pre-existing — covers remove flows with confirmation modal |

---

## Test Level Decision Framework

### What belongs where for Claims Edit

| Behaviour | Test Level | Reasoning |
|---|---|---|
| `OnGetAsync` — returns BadRequest when ClaimType is null/empty | **Unit** | Pure input validation, no I/O |
| `OnGetAsync` — returns NotFound when service returns null | **Unit** | Mock `GetForEditAsync` to return null |
| `OnGetAsync` — populates UsersInClaim, AvailableUsers, NewClaimValue | **Unit** | Mock returns data, assert properties match |
| `OnPostAddUserAsync` — model error when SelectedUserId empty | **Unit** | Validation logic, no service call needed |
| `OnPostAddUserAsync` — model error when NewClaimValue empty | **Unit** | Validation logic, separate test from above |
| `OnPostAddUserAsync` — NotFound when service reports UserNotFound | **Unit** | Mock returns specific status enum |
| `OnPostAddUserAsync` — model error when AlreadyAssigned | **Unit** | Mock returns status + assert error message includes username |
| `OnPostAddUserAsync` — all errors added when IdentityFailure | **Unit** | Mock returns multiple errors, verify all appear in ModelState |
| `OnPostAddUserAsync` — sets TempData and redirects on success | **Unit** | Assert on TempData value + RedirectToPageResult properties |
| `OnPostRemoveUserAsync` — redirects to Index when no remaining assignments | **Unit** | Mock `HasRemainingAssignments = false`, check redirect target + TempData key |
| `OnPostRemoveUserAsync` — redirects to Edit when assignments remain | **Unit** | Mock `HasRemainingAssignments = true`, check redirect + route values |
| `[Authorize(Roles = "ADMIN")]` enforcement | **Integration** | Attribute is metadata — only middleware pipeline enforces it |
| Full data pipeline: seed → EF Core → service → page → HTML | **Integration** | Mocked service hides query bugs |
| Form submission → service → database → redirect | **Integration** | Tests real HTTP pipeline including model binding |
| Validation error visibility in rendered HTML | **Integration** | AngleSharp verifies the `data-valmsg-for` span contains the error text |
| Conditional form rendering (add form only shows when available users exist) | **Integration** | Razor `@if (Model.AvailableUsers.Any())` — only real rendering catches this |
| Available users dropdown excludes already-assigned users | **Integration** | Full pipeline needed — mock can't verify service correctly filters users |
| Confirmation modal before remove | **E2E** | JavaScript-driven modal — HttpClient/AngleSharp can't execute JS |
| Validation error visible to the user | **E2E** | CSS could hide the error span, JS could interfere — only a real browser catches this |

### Key decision: why all three test levels?

`EditModel` is a **thin orchestration layer** — it delegates to `IClaimsAdminService` and makes routing/error decisions. This makes it ideal for unit testing (single dependency, pure decision logic). However:

1. **Integration tests** are needed because `[Authorize(Roles = "ADMIN")]` is enforced by middleware (not the class itself), and because rendered HTML can differ from what the page model returns
2. **E2E tests** are needed because the remove flow uses a JavaScript confirmation modal, and validation error visibility depends on CSS/JS rendering

---

## Lessons Learned

### 1. Assert on outcomes, not implementation details

When testing `OnGetAsync` with `ClaimType = null`, asserting `result.Should().BeOfType<BadRequestResult>()` is sufficient proof that the service was never called. Adding `_mockService.Verify(x => x.GetForEditAsync(...), Times.Never)` is redundant — if someone rearranges the code so the service is called before the null check, the test would already fail (the mock would throw or return null unexpectedly).

**Rule:** Use `Verify` only for side-effect-only methods where there's no observable return value.

### 2. One behaviour per test — especially for independent validations

`OnPostAddUserAsync` validates `SelectedUserId` and `NewClaimValue` independently. Testing both in one test masks regressions — if someone removes the `NewClaimValue` check, a combined test still passes because `SelectedUserId` alone makes `ModelState.IsValid == false`.

### 3. Use real TempDataDictionary, not Mock\<ITempDataDictionary\>

The original tests used `Mock<ITempDataDictionary>` and verified with `VerifySet(t => t["Success"] = It.IsAny<string>())`. This tests *implementation* (did you call the setter?) rather than *outcomes* (what value was set?).

Using a real `TempDataDictionary` enables:
```csharp
_sut.TempData["Success"].Should().Be("Claim 'Role' assigned to user 'alice'.");
```

### 4. ModelState is NOT a simple dictionary

`ModelState.AddModelError(string.Empty, error)` does not overwrite — it appends. Each key maps to a `ModelStateEntry` with a **list** of errors. The `AddErrors` helper calls `AddModelError` in a loop, and all errors accumulate under the same key:

```csharp
var errors = _sut.ModelState[string.Empty]!.Errors;
errors.Should().HaveCount(2);
```

### 5. Conditional Razor rendering affects integration test data setup

The add-user form only renders when `Model.AvailableUsers.Any()` is true. If you only seed users who already have the claim, the dropdown is empty and Razor doesn't render the form. Integration tests that submit the add form must seed at least one **unassigned** user.

### 6. AngleSharp IHtmlDocument.TextContent can be null

Don't assert on `resultDocument.TextContent` directly — it can be null on an `IHtmlDocument`. Instead, query the specific element:

```csharp
// Fragile
resultDocument.TextContent.Should().Contain("Please select a user");

// Robust
var span = resultDocument.QuerySelector("[data-valmsg-for='SelectedUserId']");
span!.TextContent.Should().Contain("Please select a user");
```

### 7. [Authorize] is metadata, not enforcement

`[Authorize(Roles = "ADMIN")]` on the class is just an attribute — it does nothing when you construct `EditModel` directly in a unit test. The ASP.NET Core middleware pipeline reads the attribute and enforces it. This is why auth tests **must** be integration tests that send real HTTP requests.

### 8. RedirectToPageResult has PageName and RouteValues

When asserting on redirects, check both:
```csharp
var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
redirect.PageName.Should().Be("/Admin/Claims/Edit");
redirect.RouteValues!["claimType"].Should().Be("Role");
```

The `PageName` contains the page path, and `RouteValues` is a dictionary of the route parameters passed with the redirect.

---

## Test Coverage Summary

### Unit Tests (26 tests)

| Handler | Test | What it verifies |
|---|---|---|
| `OnGetAsync` | BadRequest (Theory: null, "", " ") | Null/whitespace ClaimType returns 400 |
| `OnGetAsync` | NotFound | Service returns null → 404 |
| `OnGetAsync` | PopulatePageData | Service returns data → UsersInClaim, AvailableUsers, NewClaimValue populated |
| `OnPostAddUserAsync` | BadRequest (Theory) | Null/whitespace ClaimType returns 400 |
| `OnPostAddUserAsync` | NotFound (service null) | Service returns null → 404 |
| `OnPostAddUserAsync` | ModelError (empty SelectedUserId) | "Please select a user" error added |
| `OnPostAddUserAsync` | ModelError (empty NewClaimValue) | "Claim value is required" error added |
| `OnPostAddUserAsync` | NotFound (UserNotFound status) | Service returns UserNotFound → 404 |
| `OnPostAddUserAsync` | ModelError (AlreadyAssigned) | Error message includes "already has this claim type" |
| `OnPostAddUserAsync` | All errors (IdentityFailure) | Both error messages appear in ModelState |
| `OnPostAddUserAsync` | TempData + redirect (Success) | TempData["Success"] set, redirects to Edit with claimType |
| `OnPostRemoveUserAsync` | BadRequest (Theory) | Null/whitespace ClaimType returns 400 |
| `OnPostRemoveUserAsync` | NotFound (service null) | Service returns null → 404 |
| `OnPostRemoveUserAsync` | ModelError (empty RemoveUserId) | "Claim assignment details are required" |
| `OnPostRemoveUserAsync` | ModelError (null RemoveClaimValue) | Same error message |
| `OnPostRemoveUserAsync` | NotFound (UserNotFound status) | Service returns UserNotFound → 404 |
| `OnPostRemoveUserAsync` | ModelError (AssignmentNotFound) | "The selected claim assignment does not exist" |
| `OnPostRemoveUserAsync` | All errors (IdentityFailure) | Error messages appear in ModelState |
| `OnPostRemoveUserAsync` | Redirect to Index (no remaining) | TempData["Warning"] set, redirects to Index |
| `OnPostRemoveUserAsync` | Redirect to Edit (remaining) | TempData["Success"] set, redirects to Edit with claimType |

### Integration Tests (9 tests)

| Test | What it verifies |
|---|---|
| Forbidden (non-admin) | `[Authorize(Roles = "ADMIN")]` blocks non-admin |
| Unauthorized | Unauthenticated users get 401 |
| NotFound (no claim type) | Non-existent claim type → 404 |
| Render user table | Full pipeline: seeded data appears in `#users-in-claim-table` |
| Available users dropdown | Unassigned users in `#SelectedUserId`, assigned users excluded |
| POST add user (success) | Form submission → DB write → redirect → `.alert-success` |
| POST add user (validation) | Empty SelectedUserId → `[data-valmsg-for]` span shows error |
| POST remove user (remaining) | Remove → redirect to Edit with `.alert-success` |
| POST remove last user | Remove → redirect to Index with `.alert-warning` |

### E2E Tests (5 tests in EditModelE2eTests + 4 in ClaimsAdminE2eTests)

| Test | What it verifies |
|---|---|
| Render claim details | Page heading shows claim type, user table shows seeded data |
| Add user form submission | Select user + fill value + click → success alert + user in table |
| Validation: no user selected | Click Add without selecting → validation span visible |
| Validation: empty claim value | Select user but no value → validation span visible |
| Navigate Index → Edit | Click "Manage Users" → navigates to Edit page (ClaimsAdminE2eTests) |
| Add user to claim | Full add flow with success alert (ClaimsAdminE2eTests) |
| Remove user (with remaining) | Click remove → confirm modal → success alert (ClaimsAdminE2eTests) |
| Remove last user | Click remove → confirm modal → redirect to Index with warning (ClaimsAdminE2eTests) |

---

## Conventions Followed

These match the project-wide conventions (see `Tests/docs/Testing-Reference-Guide.md`):

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Structure:** Constructor injection, `_sut` field for System Under Test
- **Assertions:** FluentAssertions — `BeOfType`, `BeEquivalentTo`, `ContainSingle().Which`
- **Mocking:** Moq — single `Mock<IClaimsAdminService>`
- **TempData:** Real `TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())`
- **Section headers:** `// --------- OnGetAsync ---------` to group tests by handler
- **Integration lifecycle:** `IDisposable` with per-class factory
- **E2E lifecycle:** `IClassFixture<PlaywrightFixture>` + `IAsyncLifetime`
- **Test data:** `TestDataHelper.SeedUserAsync()` + `AddUserClaimAsync()` with unique names
