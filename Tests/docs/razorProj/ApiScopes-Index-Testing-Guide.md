# ApiScopes Index Page — Testing Lessons & Approach

## Target File

- **Page handler:** `src/IdentityServerAspNetIdentity/Pages/Admin/ApiScopes/Index.cshtml.cs`
- **Razor view:** `src/IdentityServerAspNetIdentity/Pages/Admin/ApiScopes/Index.cshtml`

## Test Files Created

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/.../Pages/Admin/ApiScopes/IndexModelTests.cs` | Unit | 2 |
| `Tests/Integration/.../Pages/Admin/ApiScopes/IndexModelIntegrationTests.cs` | Integration | 4 |
| `Tests/E2E/.../Pages/Admin/ApiScopes/IndexPageE2eTests.cs` | E2E | 2 |

---

## Test Level Decision Framework

### When to use each level

| Level | Use when... | What it catches |
|---|---|---|
| **Unit** | Testing a single class in isolation; all dependencies can be mocked | Logic errors in the handler itself (e.g., wrong property assignment, missing `.ToList()`) |
| **Integration** | Testing middleware enforcement (auth, routing), or the real service-to-database query pipeline | Authorization attribute enforcement, LINQ-to-SQL query bugs (wrong filters, missing WHERE clauses), HTML rendering issues |
| **E2E** | Testing multi-page navigation flows, browser-rendered UI, JavaScript-dependent behaviour | Visibility issues (CSS hiding elements), web component DOM transformations, link navigation correctness |

### Key decision: when are integration happy-path tests redundant?

If separate **service-level tests** exist that verify the LINQ queries against a real database, then integration tests for the page's happy path (admin sees data) become redundant. The integration tests that **remain uniquely valuable** are the **authorization tests** (401/403), because the `[Authorize]` attribute is only enforced by the ASP.NET Core middleware pipeline — never exercised in a unit test.

---

## Lessons Learned

### 1. Unit test PageContext setup

Razor Page handlers access `HttpContext.RequestAborted` for cancellation tokens. In production, `HttpContext` is never null — ASP.NET Core always provides it. Set up the unit test SUT with a `DefaultHttpContext` to exercise the **production code path**, not the null-coalescing fallback:

```csharp
_sut = new IndexModel(_mockService.Object)
{
    PageContext = new PageContext
    {
        HttpContext = new DefaultHttpContext()
    }
};
```

### 2. AllowAutoRedirect = false for auth tests

ASP.NET Core redirects unauthorized/forbidden requests to the login page by default. Without disabling auto-redirect, the test sees a `200 OK` from the login page instead of the expected `401`/`403`:

```csharp
using var nonAdminClient = nonAdminFactory.CreateClient(
    new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
```

### 3. Separate factories for different auth scenarios

Each auth scenario needs its own `CustomWebApplicationFactory<TAuthHandler>`:

- `CustomWebApplicationFactory` (default) — uses `TestAuthHandler` (admin)
- `CustomWebApplicationFactory<NonAdminTestAuthHandler>` — authenticated, non-admin (expects 403)
- `CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>` — no identity (expects 401)

### 4. Web component selectors differ between integration and E2E

- **AngleSharp (integration tests):** Parses raw server-rendered HTML. Use the Razor markup element names (e.g., `ck-responsive-row`, `ck-responsive-tbody`).
- **Playwright (E2E tests):** Runs a real browser where web components execute JavaScript and transform the DOM. Use the **rendered** element names (e.g., `tr`, `tbody`).

This is a genuine difference that E2E tests uniquely reveal — the Razor markup and the browser DOM are not the same when web components are involved.

### 5. Mocked unit tests cannot catch query translation bugs

A mocked `IApiScopesAdminService` returns exactly what you tell it — it can never reveal that the real LINQ query forgot a `.Where()` clause, selected wrong columns, or returned too many rows. These bugs live in the query translation layer and require a real database to surface.

### 6. Every test should justify its existence

Ask: "What bug would this test catch that no other test catches?" If the answer is "nothing," the test is redundant and adds build time without value. The auth integration tests (401/403) are uniquely valuable because no other level exercises middleware enforcement. The happy-path integration tests become redundant once service-level database tests exist.

---

## Conventions Followed

These conventions were detected from existing tests and should be maintained for consistency:

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Structure:** Constructor injection for test setup (mock + SUT creation)
- **Assertions:** FluentAssertions (`Should().Be()`, `Should().HaveCount()`, `Should().BeEquivalentTo()`)
- **Mocking:** Moq (`Setup`, `ReturnsAsync`, `It.IsAny<T>()`)
- **Integration test disposal:** `IDisposable` with explicit `Dispose()` for factory and client
- **E2E test lifecycle:** `IClassFixture<PlaywrightFixture>` + `IAsyncLifetime` for per-test browser context
- **E2E selectors:** Prefer `GetByRole` and `:has-text()` over brittle CSS class selectors
- **Test data seeding:** Use `TestDataHelper` static methods (e.g., `SeedApiScopeAsync`) — never seed data by manipulating the database directly in tests

---

## Template: Adding Tests for a New Admin Page

When testing a new admin Razor Page that follows the same pattern (list page with `[Authorize(Roles = "ADMIN")]`):

### Unit tests (2 minimum)
1. `OnGetAsync_ShouldPopulate[Property]_WhenServiceReturnsData`
2. `OnGetAsync_ShouldReturnEmptyList_WhenServiceReturnsNoData`

### Integration tests (4 minimum)
1. `Get_ShouldShowEmptyState_WhenNo[Items]Exist` — verify `.alert-info` message
2. `Get_ShouldRender[Item]Rows_When[Items]Exist` — seed data, verify rows in HTML
3. `Get_ShouldReturnForbidden_WhenUserIsNotAdmin` — 403 with `NonAdminTestAuthHandler`
4. `Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated` — 401 with `UnauthenticatedTestAuthHandler`

### E2E tests (focus on navigation flows)
1. `IndexPage_ShouldNavigateToEditPage_WhenEditClicked` — seed data, click Edit, verify URL + page content
2. `IndexPage_ShouldNavigateToCreatePage_WhenCreateClicked` — click Create, verify URL + page content
