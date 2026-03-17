# IdentityResources Index Page — Testing Lessons & Approach

## Target File

- **Page handler:** `src/IdentityServerAspNetIdentity/Pages/Admin/IdentityResources/Index.cshtml.cs`
- **Razor view:** `src/IdentityServerAspNetIdentity/Pages/Admin/IdentityResources/Index.cshtml`

## Test Files Created

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/.../Pages/Admin/IdentityResources/IndexModelTests.cs` | Unit | 2 |
| `Tests/Integration/.../Pages/Admin/IdentityResources/IndexModelIntegrationTests.cs` | Integration | 4 |
| `Tests/E2E/.../Pages/Admin/IdentityResources/IndexPageE2eTests.cs` | E2E | 1 |

---

## Test Level Decision Framework

### When to use each level

| Level | Use when... | What it catches |
|---|---|---|
| **Unit** | Testing a single class in isolation; all dependencies can be mocked | Logic errors in the handler itself (e.g., wrong property assignment, missing `.ToList()`) |
| **Integration** | Testing middleware enforcement (auth, routing), HTML rendering | Authorization attribute enforcement (401/403), DOM rendering of lists and empty states |
| **E2E** | Testing multi-page navigation flows, browser-rendered UI, JavaScript-dependent behaviour | Visibility issues (CSS hiding elements), web component DOM transformations, link navigation correctness |

### Key decision: Page-Level vs Service-Level Integration Tests

We focused heavily on **Service-level integration tests** (`IdentityResourcesAdminServiceTests.cs`) to verify LINQ queries against a real database. However, this does **not** make page-level integration tests completely redundant.

Page-level integration tests are uniquely valuable for:
1. **Authorization tests (401/403):** Ensuring `[Authorize(Roles = "ADMIN")]` is actively enforced by the HTTP pipeline.
2. **DOM Assertions:** Ensuring Razor conditionally renders elements (like the "No identity resources found" alert).

---

## Lessons Learned

### 1. Unified Service Layer Testing

Instead of testing the `IndexModel` via integration tests to verify database connectivity, we tested the `IIdentityResourcesAdminService` directly in a service-level integration test. This pinpointed query bugs at the source rather than through the UI.

### 2. The Necessity of Page Integration Tests for Auth

Even with complete Service Integration Tests, Page Integration Tests are required to test `[Authorize]` attributes. These tests use specific AuthHandlers (`UnauthenticatedTestAuthHandler` and `NonAdminTestAuthHandler`) and rely on disabling `AllowAutoRedirect` to catch raw 401/403 status codes instead of redirects to a login page.

### 3. E2E Navigation Verification

The E2E test for the index page specifically verifies that the "Edit" button correctly resolves the route and navigates the browser to the Edit page. This is a crucial check for navigation wiring that unit tests cannot perform.

---

## Conventions Followed

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Structure:** Constructor injection for test setup (mock + SUT creation)
- **Assertions:** FluentAssertions
- **Mocking:** Moq
- **E2E lifecycle:** `IClassFixture<PlaywrightFixture>` + `IAsyncLifetime`
- **E2E selectors:** Prefer `GetByRole` and `:has-text()` over brittle CSS class selectors

---

## Template: Adding Tests for a New List Page

When testing a new admin list page:

### Unit tests (2 minimum)
1. `OnGetAsync_ShouldPopulate[Property]_WhenServiceReturnsData`
2. `OnGetAsync_ShouldReturnEmptyList_WhenServiceReturnsNoData`

### E2E tests (focus on navigation flows)
1. `IndexPage_ShouldNavigateToEditPage_WhenEditClicked` — seed data, click Edit, verify URL + page content
