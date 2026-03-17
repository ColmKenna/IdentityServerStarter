# Testing Reference Guide — IdentityServerStarter

> A comprehensive reference for writing unit, integration, and E2E tests in this project.
> Covers conventions, infrastructure, patterns, and lessons learned across sessions.

---

## Table of Contents

1. [Project Test Stack](#project-test-stack)
2. [Test Project Layout](#test-project-layout)
3. [Shared Infrastructure](#shared-infrastructure)
4. [Naming Conventions](#naming-conventions)
5. [Unit Testing Patterns](#unit-testing-patterns)
6. [Integration Testing Patterns](#integration-testing-patterns)
7. [E2E Testing Patterns](#e2e-testing-patterns)
8. [Test Level Decision Framework](#test-level-decision-framework)
9. [Authentication Testing](#authentication-testing)
10. [Common Pitfalls & Lessons Learned](#common-pitfalls--lessons-learned)
11. [Templates](#templates)

---

## Project Test Stack

| Component | Library | Version |
|---|---|---|
| Framework | .NET 10.0 | `net10.0` |
| Test framework | xUnit | 2.9.3 |
| Assertions | FluentAssertions | 8.8.0 |
| Mocking | Moq | 4.20.72 |
| Mock queryable | MockQueryable.Moq | 10.0.2 |
| HTML parsing | AngleSharp | 1.4.0 |
| Browser automation | Microsoft.Playwright | 1.49.0 |
| Test server | Microsoft.AspNetCore.Mvc.Testing | 10.0.3 |
| In-memory DB | Microsoft.EntityFrameworkCore.InMemory | 10.0.3 |
| Code coverage | coverlet.collector | 6.0.4 |

---

## Test Project Layout

```
Tests/
├── Common/
│   └── IdentityServerAspNetIdentity.TestSupport/    # Shared infrastructure
│       └── Infrastructure/
│           ├── CustomWebApplicationFactory.cs        # Base WebApplicationFactory
│           ├── CustomWebApplicationFactory`1.cs      # Generic version (swappable auth)
│           ├── TestAuthHandler.cs                    # Admin auth handler
│           ├── NonAdminTestAuthHandler.cs            # Non-admin auth handler
│           ├── UnauthenticatedTestAuthHandler.cs     # No-auth handler
│           ├── TestDataHelper.cs                     # User/role/claim/scope seeding
│           ├── ClientTestDataHelper.cs               # OAuth client/resource seeding
│           └── InMemoryServerSideSessionStore.cs     # Session store for tests
├── Unit/
│   ├── IdentityServerAspNetIdentity.UnitTests/      # Razor Page handler unit tests
│   │   ├── GlobalUsings.cs
│   │   ├── TestHelpers.cs                           # CreateMockUserManager()
│   │   └── Pages/Admin/...                          # Mirror source folder structure
│   └── IdentityServerServices.UnitTests/            # Service layer unit tests
├── Integration/
│   └── IdentityServerAspNetIdentity.IntegrationTests/
│       ├── GlobalUsings.cs
│       ├── Infrastructure/
│       │   ├── AngleSharpHelpers.cs                 # DOM parsing + form submission
│       │   ├── AngleSharpExtensions.cs              # GetAndParsePage, SubmitForm
│       │   └── AntiforgeryTokenHelper.cs            # Token generation for forms
│       ├── Pages/Admin/...                          # Page-level integration tests
│       └── Services/...                             # Service-level integration tests
├── E2E/
│   └── IdentityServerAspNetIdentity.E2ETests/
│       ├── GlobalUsings.cs
│       ├── Infrastructure/
│       │   ├── PlaywrightWebApplicationFactory.cs   # Kestrel + SQLite for real browser
│       │   └── PlaywrightFixture.cs                 # Browser lifecycle management
│       └── Pages/Admin/...                          # Browser-driven tests
└── docs/                                            # This folder — testing documentation
```

---

## Shared Infrastructure

### CustomWebApplicationFactory

The base class for all integration/E2E tests. Located in `TestSupport/Infrastructure/`.

**What it does:**
- Replaces all 3 DbContexts (Application, Configuration, PersistedGrant) with in-memory databases
- Each factory instance gets unique database names via GUID — no cross-test contamination
- Registers `TestAuthHandler` as the default authentication scheme (admin user with all claims)
- Disables antiforgery token validation
- Sets environment to "Testing"
- Registers `InMemoryServerSideSessionStore`

**Usage:**
```csharp
// Default: admin user, in-memory databases
public class MyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public MyTests(CustomWebApplicationFactory factory) => _factory = factory;
}
```

### CustomWebApplicationFactory\<TAuthHandler\>

Generic version that swaps the auth handler. Use this for authorization testing.

```csharp
// Non-admin user (expects 403)
using var factory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();

// Unauthenticated (expects 401)
using var factory = new CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>();
```

### TestDataHelper

Static methods for seeding test data via the DI container. Always use this — never manipulate the database directly in tests.

| Method | Returns | Purpose |
|---|---|---|
| `CreateTestUserAsync(factory, username, email, password?)` | `string` (userId) | Create user with optional password |
| `SeedUserAsync(factory, usernamePrefix)` | `string` (userId) | Create user with auto-generated Guid suffix |
| `AddUserClaimAsync(factory, userId, claimType, claimValue)` | `Task` | Add a claim to a user |
| `SeedRoleAsync(factory, roleName)` | `string` (roleId) | Create a role |
| `AddUserToRoleAsync(factory, userId, roleName)` | `Task` | Assign user to role |
| `SeedApiScopeAsync(factory, name, displayName?, ...)` | `int` (scopeId) | Create API scope |
| `SeedIdentityResourceAsync(factory, name, displayName?, ...)` | `int` (resourceId) | Create Identity resource |
| `DeleteTestUserAsync(factory, userId)` | `Task` | Remove test user |
| `GetUserByIdAsync(factory, userId)` | `ApplicationUser?` | Retrieve user for assertions |

### ClientTestDataHelper

| Method | Purpose |
|---|---|
| `SeedClientAsync(factory, clientId, ...)` | Create OAuth client with full configuration |
| `SeedIdentityResourcesAsync(factory, ...names)` | Seed OIDC identity resources |
| `SeedApiScopesAsync(factory, ...names)` | Seed API scopes via config store |

### AngleSharp Helpers

For integration tests that need to parse rendered HTML and submit forms:

```csharp
// Parse a page
var document = await client.GetAndParsePage("/Admin/Users");

// Query DOM elements
var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
var alert = document.QuerySelector(".alert-info");

// Submit a form
var form = document.QuerySelector<IHtmlFormElement>("form");
var response = await client.SubmitForm(form, new Dictionary<string, string>
{
    ["Input.Username"] = "newuser"
});
```

### TestHelpers (Unit Tests)

```csharp
// Create a properly configured mock UserManager
var mockUserManager = TestHelpers.CreateMockUserManager();
```

This sets up validators and the backing store — use it instead of manually creating `Mock<UserManager<ApplicationUser>>`.

---

## Naming Conventions

### Test Files

| Level | Pattern | Example |
|---|---|---|
| Unit | `{ClassName}Tests.cs` | `UsersEditModelTests.cs` |
| Integration (pages) | `{ClassName}IntegrationTests.cs` | `EditModelIntegrationTests.cs` |
| Integration (services) | `{ServiceName}Tests.cs` | `ClaimsAdminServiceTests.cs` |
| E2E | `{PageName}E2eTests.cs" | `IndexPageE2eTests.cs` |

### Test Methods

**Pattern:** `{MethodName}_Should{ExpectedBehaviour}_When{Condition}`

```csharp
// Unit
OnGetAsync_ShouldReturnClaims_WhenTheyExist
OnPostAsync_ShouldReturnForbid_WhenNotAuthorized
OnPostDeleteAsync_ShouldReturnBadRequest_WhenSelfDelete

// Integration
Get_ShouldShowEmptyState_WhenNoApiScopesExist
Get_ShouldReturnForbidden_WhenUserIsNotAdmin
Post_ShouldReRenderValidationAndAvailableOptions_WhenModelIsInvalid

// E2E
IndexPage_ShouldNavigateToEditPage_WhenEditClicked
```

### Class Structure

All test classes use **constructor-based initialization** (not `[SetUp]` or `IAsyncLifetime` for unit tests):

```csharp
public class MyServiceTests
{
    private readonly Mock<IDependency> _mockDep;
    private readonly MyService _sut;  // System Under Test — always named _sut

    public MyServiceTests()
    {
        _mockDep = new Mock<IDependency>();
        _sut = new MyService(_mockDep.Object);
    }
}
```

### Region Organisation

Complex test files group tests with `#region`:

```csharp
#region Profile Tab
#region Delete
#region Claims Tab
#region Roles Tab
#region Security Tab
#region Authorization Denied — Forbid
```

---

## Unit Testing Patterns

### PageModel Unit Tests

Every PageModel unit test needs a `PageContext` with `DefaultHttpContext`:

```csharp
var sut = new IndexModel(_mockService.Object)
{
    PageContext = new PageContext
    {
        HttpContext = new DefaultHttpContext()
    }
};
```

For handlers that use TempData:
```csharp
var httpContext = new DefaultHttpContext();
sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
```

For handlers that check `User` claims:
```csharp
httpContext.User = new ClaimsPrincipal(
    new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "mock"));
```

For handlers that read form data:
```csharp
private void SetupFormContent(EditModel sut, params (string key, string value)[] formValues)
{
    var dict = formValues.ToDictionary(k => k.key, v => new StringValues(v.value));
    sut.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
    sut.HttpContext.Request.Form = new FormCollection(dict);
}
```

### Mocking Authorization

When a PageModel injects `IAuthorizationService`:

```csharp
// Always succeeds
_mockAuthService.Setup(x => x.AuthorizeAsync(
    It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
    .ReturnsAsync(AuthorizationResult.Success());

// Fails for a specific policy
_mockAuthService.Setup(x => x.AuthorizeAsync(
    It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), "SpecificPolicy"))
    .ReturnsAsync(AuthorizationResult.Failed());
```

### Capturing Mock Arguments

To assert what was passed to a dependency:

```csharp
UserEditPostUpdateRequest? capturedRequest = null;
_mockUserEditor
    .Setup(x => x.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
    .ReturnsAsync(new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success })
    .Callback<UserEditPostUpdateRequest>(r => capturedRequest = r);

// Act...

capturedRequest.Should().NotBeNull();
capturedRequest!.UserId.Should().Be("user123");
```

### Asserting Action Results

```csharp
// Page result
result.Should().BeOfType<PageResult>();

// Redirect
var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
redirect.PageName.Should().Be("/Admin/Users/Edit");

// Redirect with route values
redirect.RouteValues!["userId"].Should().Be("user123");

// Forbid
result.Should().BeOfType<ForbidResult>();

// NotFound
result.Should().BeOfType<NotFoundResult>();

// BadRequest
result.Should().BeOfType<BadRequestResult>();
```

### Theory Tests (Parameterised)

```csharp
[Theory]
[InlineData("paramId", "", "", "paramId")]
[InlineData("", "propId", "", "propId")]
[InlineData("", "", "inputId", "inputId")]
public async Task OnPostAsync_ShouldResolveUserId_FromFallbackChain(
    string param, string prop, string inputId, string expectedId)
{
    // ...
}
```

---

## Integration Testing Patterns

### Page Integration Tests (IDisposable)

```csharp
public class IndexModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IndexModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### Service Integration Tests (IAsyncLifetime)

```csharp
public class ApiScopesAdminServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ConfigurationDbContext _configDbContext = default!;
    private IApiScopesAdminService _sut = default!;

    public ApiScopesAdminServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _configDbContext = _scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        _sut = _scope.ServiceProvider.GetRequiredService<IApiScopesAdminService>();

        // Clean state — remove all seeded data
        _configDbContext.ApiScopes.RemoveRange(_configDbContext.ApiScopes);
        return _configDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }
}
```

### DOM Assertions with AngleSharp

```csharp
var response = await _client.GetAsync("/Admin/ApiScopes");
var document = await AngleSharpHelpers.GetDocumentAsync(response);

// Status code
response.StatusCode.Should().Be(HttpStatusCode.OK);

// Empty state
document.QuerySelector(".alert-info")!.TextContent.Should().Contain("No API scopes found.");

// Rows
var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
rows.Should().HaveCount(2);
rows.Should().Contain(r => r.TextContent.Contains("api1"));

// Specific element
document.QuerySelector("ck-responsive-table").Should().BeNull();
```

### Form Submission

```csharp
var page = await _client.GetAndParsePage("/Admin/Clients/1/Edit");
var form = page.QuerySelector<IHtmlFormElement>("form[action*='Edit']");
var response = await _client.SubmitForm(form, new Dictionary<string, string>
{
    ["Input.ClientName"] = "Updated Client",
    ["Input.Description"] = "New description"
});
```

---

## E2E Testing Patterns

### Test Class Structure

```csharp
[Trait("Category", "E2E")]
public class IndexPageE2eTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public IndexPageE2eTests(PlaywrightFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync() => await _context.CloseAsync();
}
```

### Key Differences from Integration Tests

| Aspect | Integration | E2E |
|---|---|---|
| Database | In-memory | SQLite (file-based) |
| Server | TestHost (in-process) | Kestrel (real HTTP) |
| Client | HttpClient | Chromium browser |
| HTML parsing | AngleSharp (raw server HTML) | Playwright (rendered DOM) |
| Web component selectors | `ck-responsive-row` (Razor markup) | `tr` (browser-rendered) |
| Auth | TestScheme (no real login) | TestScheme (no real login) |

### Playwright Assertions

```csharp
// Navigate
await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes");

// Locate elements
var row = _page.Locator("tr:has-text('E2E Test Scope')");
await Expect(row).ToBeVisibleAsync();

// Click and navigate
var editLink = row.GetByRole(AriaRole.Link, new() { Name = "Edit" });
await editLink.ClickAsync();

// URL assertion
await Expect(_page).ToHaveURLAsync(new Regex($"/Admin/ApiScopes/{scopeId}/Edit"));

// Content assertion
await Expect(_page.Locator("h2").First).ToContainTextAsync("Edit API Scope");
```

### Selector Strategy

Prefer in this order:
1. `GetByRole(AriaRole.Link, new() { Name = "Edit" })` — accessibility roles (also pierces Shadow DOM)
2. `:has-text('...')` — visible text content
3. `data-testid` attributes (when available)
4. `span[data-valmsg-for='Input.Name']` — attribute selectors for validation spans (avoids strict mode violations)
5. CSS selectors (last resort)

**Shadow DOM:** For web components like `ck-tabs`, always use `GetByRole` — CSS selectors cannot pierce Shadow DOM boundaries.

---

## Test Level Decision Framework

### When to use each level

| Level | Use when... | What it uniquely catches |
|---|---|---|
| **Unit** | Testing a single class in isolation; all dependencies can be mocked | Logic errors in the handler itself (wrong property assignment, missing null check, incorrect mapping) |
| **Integration** | Testing middleware enforcement (auth, routing), service-to-database pipeline, HTML rendering | Authorization attribute enforcement, LINQ-to-SQL translation bugs, HTML rendering issues, form binding |
| **E2E** | Testing multi-page flows, browser-rendered UI, JavaScript-dependent behaviour | CSS visibility issues, web component DOM transformations, navigation flows, JavaScript interactions |

### Decision Flowchart

```
Is the behaviour purely logic (no I/O, no database, no HTTP)?
  → YES: Unit test
  → NO: Does it need a real database or the HTTP pipeline?
    → YES (pipeline/auth): Integration test
    → YES (browser/JS): E2E test
    → YES (database query): Service-level integration test
```

### When integration happy-path tests are redundant

If separate **service-level integration tests** exist that verify LINQ queries against a real database, then page-level integration tests for the happy path (admin sees data) add little value. The integration tests that **remain uniquely valuable** are:
- **Authorization tests** (401/403) — `[Authorize]` is only enforced by the middleware pipeline
- **Form binding tests** — model binding, validation messages, antiforgery behaviour
- **Redirect/routing tests** — PRG patterns, tab state preservation

---

## Authentication Testing

### Three Auth Scenarios

| Scenario | Handler | Expected Status | Factory |
|---|---|---|---|
| Admin (default) | `TestAuthHandler` | 200 OK | `CustomWebApplicationFactory` |
| Authenticated non-admin | `NonAdminTestAuthHandler` | 403 Forbidden | `CustomWebApplicationFactory<NonAdminTestAuthHandler>` |
| Unauthenticated | `UnauthenticatedTestAuthHandler` | 401 Unauthorized | `CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>` |

### TestAuthHandler Claims (Admin)

The default `TestAuthHandler` provides these claims:

```csharp
new Claim(ClaimTypes.Name, "testadmin"),
new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
new Claim(ClaimTypes.Email, "admin@test.com"),
new Claim(ClaimTypes.Role, "ADMIN"),
new Claim("admin", "admin:users"),
new Claim("admin", "admin:user_claims"),
new Claim("admin", "admin:user_roles"),
new Claim("admin", "admin:user_grants"),
new Claim("admin", "admin:user_sessions"),
```

### Critical: Disable Auto-Redirect

ASP.NET Core redirects 401/403 to the login page by default. Without disabling auto-redirect, your auth tests see `200 OK` instead of the expected status:

```csharp
using var client = factory.CreateClient(
    new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
```

### Unit Test Authorization Mocking

In unit tests, mock `IAuthorizationService` directly rather than relying on the pipeline:

```csharp
// Setup helpers
private void SetupAuthorizationAlwaysSucceeds()
{
    _mockAuthService.Setup(x => x.AuthorizeAsync(
        It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
        .ReturnsAsync(AuthorizationResult.Success());
}

private void SetupAuthorizationFails(string policyName)
{
    _mockAuthService.Setup(x => x.AuthorizeAsync(
        It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), policyName))
        .ReturnsAsync(AuthorizationResult.Failed());
}
```

---

## Common Pitfalls & Lessons Learned

### 1. Mocked tests cannot catch query translation bugs

A mocked `IService` returns exactly what you configure — it can never reveal LINQ-to-SQL failures (wrong filters, missing WHERE clauses, wrong column selection). These bugs require a real database to surface.

### 2. Web component selectors differ between integration and E2E

- **AngleSharp (integration):** Sees raw server-rendered HTML → use Razor markup names (`ck-responsive-row`)
- **Playwright (E2E):** Sees browser-rendered DOM after JavaScript executes → use rendered names (`tr`)

### 3. Always provide PageContext for PageModel unit tests

Razor Pages expect `HttpContext` to never be null. Without `PageContext`, cancellation token access and other framework features fail:

```csharp
sut.PageContext = new PageContext { HttpContext = new DefaultHttpContext() };
```

### 4. Every test should justify its existence

Ask: *"What bug would this test catch that no other test catches?"* If the answer is "nothing," the test is redundant and adds build time without value.

### 5. Clean state per test

Integration tests sharing a `CustomWebApplicationFactory` via `IClassFixture` share the same in-memory database. Clean up at the start of each test (in `InitializeAsync`), not at the end:

```csharp
public Task InitializeAsync()
{
    _configDbContext.ApiScopes.RemoveRange(_configDbContext.ApiScopes);
    return _configDbContext.SaveChangesAsync();
}
```

### 6. Use TestDataHelper — never seed directly

All test data seeding should go through `TestDataHelper` (or `ClientTestDataHelper`). This ensures:
- Consistent data creation patterns
- Proper DI scope management
- Unique identifiers (Guid suffixes) to prevent collisions

### 7. IDisposable vs IAsyncLifetime

- **Page integration tests** that create their own factory: use `IDisposable`
- **Service integration tests** sharing a factory via `IClassFixture`: use `IAsyncLifetime`
- **E2E tests:** always use `IClassFixture<PlaywrightFixture>` + `IAsyncLifetime`

### 8. Form data setup for POST handler unit tests

When testing POST handlers that read `Request.Form`, you must set both `ContentType` and `Form`:

```csharp
sut.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
sut.HttpContext.Request.Form = new FormCollection(
    formValues.ToDictionary(k => k.key, v => new StringValues(v.value)));
```

### 9. TempData needs explicit setup in unit tests

Handlers using `TempData["Success"] = "..."` will throw unless you provide a `TempDataDictionary`:

```csharp
sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
```

### 10. Mock UserManager requires proper construction

Don't create `Mock<UserManager<...>>()` directly — use the shared helper that sets up validators:

```csharp
var mockUserManager = TestHelpers.CreateMockUserManager();
```

### 11. Shadow DOM requires `GetByRole` in E2E tests

Custom web components (e.g., `ck-tabs`) render their interactive elements inside Shadow DOM. Standard CSS selectors cannot pierce Shadow DOM boundaries. Playwright's `GetByRole` automatically pierces through:

```csharp
// WRONG — times out because the button is inside Shadow DOM
await _page.Locator("ck-tab-header:has-text('User Claims')").ClickAsync();

// CORRECT — GetByRole pierces Shadow DOM
await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();
```

The `ck-tabs` component renders `<button role="tab" class="tab-heading">` elements inside its Shadow DOM. Use `AriaRole.Tab` to select them.

### 12. Tab content is hidden until the tab is clicked

Elements inside non-active tabs (e.g., `ck-tab`) are not visible in the browser. Playwright's `ToBeVisibleAsync()` will fail even if the element exists in the DOM. Always click the tab first before asserting on its content:

```csharp
// Click tab to reveal content
await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();

// Now the content is visible
var table = _page.Locator("#applied-user-claims-table");
await Expect(table).ToBeVisibleAsync();
```

### 13. Available claim types come from ASP.NET Identity UserClaims

The `AvailableUserClaims` dropdown on edit pages is populated from the `UserClaims` table (distinct claim types from all users). In E2E tests, you must seed a user with claims before the dropdown will have options:

```csharp
var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, "e2e-claimuser");
await TestDataHelper.AddUserClaimAsync(_fixture.Factory, userId, "profile", "test-value");
```

### 14. Strict mode violations with broad CSS selectors

Playwright's strict mode throws when a locator matches multiple elements. Validation error spans (`.text-danger`, `.field-validation-error`) often exist for every form field. Target the specific field's validation span:

```csharp
// WRONG — matches all validation spans (3+ elements)
await Expect(_page.Locator(".text-danger, .field-validation-error")).ToBeVisibleAsync();

// CORRECT — target the specific field
await Expect(_page.Locator("span[data-valmsg-for='Input.Name'].field-validation-error")).ToBeVisibleAsync();
```

### 15. JavaScript confirmation dialogs require a two-step click pattern in E2E tests

Some buttons use `confirmAction()` from `confirmModal.js`, which calls `event.preventDefault()` and shows a `<dialog>` modal before submitting the form. Clicking the button alone does nothing — you must also click the confirmation button inside the modal.

**Affected buttons (users-edit.js):** `#remove-selected-btn`, `#remove-roles-btn`, `#disable-account-btn`, `#reset-password-btn`, `#force-signout-btn`, `#reset-authenticator-btn`

```csharp
// Helper method for confirmation dialog buttons
private async Task ClickAndConfirmAsync(string buttonSelector)
{
    await _page.Locator(buttonSelector).ClickAsync();
    await Expect(_page.Locator("#confirm-modal")).ToBeVisibleAsync();
    await _page.Locator("#confirm-modal-confirm").ClickAsync();
}

// Usage
await ClickAndConfirmAsync("#disable-account-btn");
await Expect(_page.Locator(".alert-success")).ToBeVisibleAsync();
```

**How to spot these:** Look for `setupConfirm()` calls or `confirmAction()` usage in the page's JavaScript file. The pattern is: button click → `preventDefault()` → show `<dialog>` → user clicks Confirm → original action proceeds.

### 16. Multi-tab forms with hidden `required` fields block submission

When a single `<form>` wraps multiple tabs (e.g., `ck-tabs`), a `required` field on an inactive/hidden tab blocks form submission for buttons that don't have `formnovalidate`. The browser validates ALL fields in the form, even those not visible.

**Example:** The User Edit page has one `<form id="edit-user-page-form">` wrapping Profile, Security, Claims, and Roles tabs. The Security tab has `<input type="password" required />`. The Profile Save button lacks `formnovalidate`, so clicking it triggers validation failure on the hidden password field.

**Workaround in E2E tests:** Bypass client-side validation via JavaScript:

```csharp
await _page.EvaluateAsync(@"() => {
    const form = document.getElementById('edit-user-page-form');
    if (form) {
        form.noValidate = true;
        form.requestSubmit();
    }
}");
```

**Note:** `form.noValidate = true` disables HTML5 validation. `requestSubmit()` (not `submit()`) is needed because `submit()` bypasses the form's submit event listeners entirely.

### 17. Strict mode violations with duplicate text in links and spans

When a page renders the same text in both an `<a>` link and a `<span>` (e.g., for responsive table layouts), `GetByText()` matches multiple elements and Playwright's strict mode throws. Use `GetByRole` with a name filter to target the specific element:

```csharp
// WRONG — matches both <a> and <span> containing the username
var userLink = _page.GetByText(actualUsername);

// CORRECT — targets only the <a> link
var userLink = _page.GetByRole(AriaRole.Link, new() { Name = actualUsername });
```

### 18. Assert on outcomes, not implementation details

Don't use `mock.Verify(x => x.Method(), Times.Never)` when the return-value assertion already proves the method wasn't called. If a handler returns `BadRequestResult` before calling the service, asserting on `BadRequestResult` is sufficient — an explicit `Verify` adds brittleness without catching additional bugs.

```csharp
// WRONG — redundant Verify
result.Should().BeOfType<BadRequestResult>();
_mockService.Verify(x => x.GetForEditAsync(It.IsAny<string>(), ...), Times.Never); // unnecessary

// CORRECT — the assertion on the return type is enough
result.Should().BeOfType<BadRequestResult>();
```

`Verify` IS the right tool when testing **side-effect-only methods** (void returns, fire-and-forget) where there's no observable return value to assert on.

### 19. One behaviour per test — don't combine validation checks

When a handler validates multiple fields independently, test each in its own test method. Combined tests mask regressions:

```csharp
// WRONG — if the NewClaimValue check is deleted, this still passes
// because SelectedUserId alone makes ModelState.IsValid == false
[Fact]
public async Task OnPostAsync_ShouldReturnErrors_WhenBothFieldsEmpty()
{
    _sut.SelectedUserId = "";
    _sut.NewClaimValue = "";
    // ... only proves *some* error exists, not which one
}

// CORRECT — separate tests pinpoint exactly what broke
OnPostAsync_ShouldAddModelError_WhenSelectedUserIdIsEmpty
OnPostAsync_ShouldAddModelError_WhenNewClaimValueIsEmpty
```

### 20. ModelState stores multiple errors per key

`ModelState.AddModelError(key, message)` does NOT overwrite — it appends. Each key maps to a `ModelStateEntry` with a list of errors. Assert on the errors collection:

```csharp
var errors = _sut.ModelState[string.Empty]!.Errors;
errors.Should().HaveCount(2);
errors.Select(e => e.ErrorMessage).Should().Contain("error 1");
errors.Select(e => e.ErrorMessage).Should().Contain("error 2");
```

### 21. Use real TempDataDictionary, not Mock\<ITempDataDictionary\>

Mocking `ITempDataDictionary` forces you to use `VerifySet` for assertions, which tests implementation rather than outcomes. Use a real `TempDataDictionary` so you can assert on actual values:

```csharp
// WRONG — mock forces implementation-detail assertions
var tempDataMock = new Mock<ITempDataDictionary>();
_sut.TempData = tempDataMock.Object;
// ... later
tempDataMock.VerifySet(t => t["Success"] = It.IsAny<string>(), Times.Once);

// CORRECT — real TempData allows outcome assertions
var httpContext = new DefaultHttpContext();
_sut.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
// ... later
_sut.TempData["Success"].Should().Be("Claim 'Role' assigned to user 'alice'.");
```

### 22. Conditional Razor rendering affects integration test setup

Razor views often conditionally render forms based on model data (e.g., `@if (Model.AvailableUsers.Any())`). In integration tests, if the form doesn't appear in the HTML, AngleSharp can't find it to submit. Ensure test data creates the conditions for the UI element to render:

```csharp
// WRONG — only one user seeded, already assigned → no available users → form not rendered
var userId = await TestDataHelper.SeedUserAsync(_factory, "user1");
await TestDataHelper.AddUserClaimAsync(_factory, userId, "Role", "Admin");
// QuerySelector("#add-user-claim-form") returns null!

// CORRECT — seed an additional unassigned user so AvailableUsers is non-empty
await TestDataHelper.SeedUserAsync(_factory, "unassignedUser");
// Now the form renders and can be submitted
```

### 23. AngleSharp IHtmlDocument.TextContent may be null — use specific selectors

When asserting on parsed HTML, avoid `document.TextContent` which can be null. Instead, query specific elements and assert on their text:

```csharp
// FRAGILE — TextContent can be null on IHtmlDocument
resultDocument.TextContent.Should().Contain("Please select a user");

// ROBUST — target the specific validation element
var span = resultDocument.QuerySelector("[data-valmsg-for='SelectedUserId']");
span.Should().NotBeNull();
span!.TextContent.Should().Contain("Please select a user");
```

### 24. EF Core InMemory Stale State in Integration Tests

In integration tests, reusing the same `UserManager` or `DbContext` after an HTTP request can lead to false failures. The entities may be tracked in the pre-request scope and won't reflect changes made in the request's scope (which runs in a separate DI scope).

**Fix:** Always resolve a fresh DI scope and a new `UserManager` for post-request verification:
```csharp
// Act
await client.SubmitForm(form);

// Assert - verify in DB with a FRESH scope
using var verifyScope = factory.Services.CreateScope();
var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var updatedUser = await verifyUserManager.FindByIdAsync(userId);
(await verifyUserManager.IsInRoleAsync(updatedUser!, role)).Should().BeTrue();
```

### 25. Avoid Redundant Unit Tests for DB-Heavy Services

When a service is already comprehensively tested at the integration level against a real/in-memory database (covering control flows like `NotFound`, `DuplicateName`, and success paths), writing duplicate unit tests using `MockQueryable.Moq` for those exact same flows adds maintenance overhead without catching any unique bugs.

**Rule of thumb:** If the service's logic heavily relies on EF Core queries, let the Service Integration Tests handle the coverage. Only write Service Unit Tests if there is significant non-database logic (e.g., complex mapping or validation) that is brittle to test at the integration level.

### 26. EF Core Identity Map Bypassing in Integration Tests

When asserting in an integration test, if you use the same `DbContext` for **Act** and **Assert**, EF Core might return the object from its memory (the Identity Map) without actually fetching the latest values from the database.

**Fix:** Use `_configDbContext.ChangeTracker.Clear()` after the Act phase or chain `.AsNoTracking()` to your query to force a fresh read from the disk.

```csharp
// Act
await _sut.UpdateAsync(id, model);

// Assert
_configDbContext.ChangeTracker.Clear(); // Force clear for fresh DB read
var updated = await _configDbContext.Entities.AsNoTracking().SingleAsync(e => e.Id == id);
```

### 27. Exhaustive Mapping Tests for DTO-Heavy Services

For services that map a large number of scalar properties (e.g., `ClientAdminService.UpdateClientAsync` mapping 15+ properties), one comprehensive integration test is often more pragmatic than many smaller tests. This ensures every line of the mapping code is executed and persisted correctly without cluttering the test suite with redundant boilerplate.

### 28. Mock Callback to Simulate DB Side Effects

When a mocked method (e.g., `UserManager.RemoveClaimAsync`) doesn't actually modify the database but subsequent service logic queries the database (e.g., `HasRemainingAssignments`), the test will get stale data. Use a `Callback` on the mock to simulate the DB mutation:

```csharp
_userManagerMock.Setup(m => m.RemoveClaimAsync(user, It.IsAny<Claim>()))
    .Callback(() =>
    {
        var claim = context.UserClaims.First(c => c.UserId == "u1" && c.ClaimType == "role");
        context.UserClaims.Remove(claim);
        context.SaveChanges();
    })
    .ReturnsAsync(IdentityResult.Success);
```

This is a code smell that signals the behaviour is better covered by an **integration test** with a real `UserManager`. Use the Callback for unit test completeness, but always pair it with an integration test for confidence.

### 29. SQLite FK Constraints Require Parent Rows

When seeding `IdentityUserClaim<string>` rows in `ApplicationDbContext` with SQLite, you must create a corresponding `Users` row first — SQLite enforces foreign key constraints even in test databases:

```csharp
// WRONG — throws FK constraint violation
appDb.UserClaims.Add(new IdentityUserClaim<string> { ClaimType = "role", ClaimValue = "admin" });

// CORRECT — seed the user first
var testUser = new ApplicationUser { Id = "u1", UserName = "test", Email = "t@t.com" };
appDb.Users.Add(testUser);
appDb.UserClaims.Add(new IdentityUserClaim<string> { UserId = "u1", ClaimType = "role", ClaimValue = "admin" });
```

### 30. Asymmetric Failure-Path Coverage Is a Common Blind Spot

When two handler methods share the same branching structure (e.g., `OnPostAddUserAsync` and `OnPostRemoveUserAsync` both use the same `switch` on status), it's easy to assume "I tested this pattern already" and skip failure paths for the second method. **Each method needs its own tests** — the mock setup differs, and a copy-paste error in either method would only be caught by its own tests.

```csharp
// Both use the same switch structure, but BOTH need NotFound + Failed tests:
// OnPostAddUserAsync
switch (result.Status)
{
    case AddUserToRoleStatus.RoleNotFound:
    case AddUserToRoleStatus.UserNotFound:
        return NotFound();
    case AddUserToRoleStatus.Failed:
        // ...
}

// OnPostRemoveUserAsync  — structurally identical, easily skipped
switch (result.Status)
{
    case RemoveUserFromRoleStatus.RoleNotFound:
    case RemoveUserFromRoleStatus.UserNotFound:
        return NotFound();
    case RemoveUserFromRoleStatus.Failed:
        // ...
}
```

**Rule:** When reviewing test coverage, compare parallel handler methods side-by-side. If `AddX` has 4 tests and `RemoveX` has 2, the gap is almost certainly in the failure paths.

### 31. Authorization Forbid Tests Should Be Exhaustive, Not Sampled

Every handler that checks `IsAuthorizedAsync` needs its own forbid test, even if multiple handlers check the same policy. The cost is ~5 lines per test; the risk of a missing authorization check reaching production is much higher.

```csharp
// All four check UsersWrite — but each still needs its own test.
// If someone refactors to per-handler policies, you need the safety net.
OnPostSecurityDisableAccountAsync_ShouldReturnForbid_WhenNotAuthorized
OnPostSecurityClearLockoutAsync_ShouldReturnForbid_WhenNotAuthorized
OnPostSecurityToggleLockoutEnabledAsync_ShouldReturnForbid_WhenNotAuthorized
OnPostSecurityForceSignOutAsync_ShouldReturnForbid_WhenNotAuthorized
```

### 32. Success-Path Service Tests Catch Different Bugs Than Failure-Path Tests

Failure-path tests (NotFound, DuplicateName, AlreadyApplied) verify guard clauses and early returns. Success-path tests verify:
- **Data actually persists** — `SaveChangesAsync` was called and the entity was mutated
- **Input normalization works end-to-end** — trim, case folding, null coalescing
- **Mapping is complete** — all fields are mapped, not just the ones the failure tests exercise

```csharp
// This test catches bugs that no failure-path test can:
[Fact]
public async Task AddClaimAsync_ShouldAddClaimAndPersist_WhenClaimIsNew()
{
    // Act: pass " role " with spaces to test Trim()
    var result = await sut.AddClaimAsync(resource.Id, " role ");

    // Assert: persisted AND trimmed
    result.Status.Should().Be(AddIdentityResourceClaimStatus.Success);
    var reloaded = await configDb.IdentityResources
        .Include(r => r.UserClaims)
        .FirstAsync(r => r.Id == resource.Id);
    reloaded.UserClaims.Should().Contain(c => c.Type == "role"); // trimmed
}
```

### 33. `DateTime.UtcNow` — Testability Smell

Direct use of `DateTime.UtcNow` in production code makes it impossible to assert exact timestamps in tests. .NET 8+ provides `TimeProvider` as an injectable abstraction. This project uses `DateTime.UtcNow` in `IdentityResourcesAdminService` for the `Updated` field — not urgent (the timestamp isn't business-critical), but worth knowing for future time-sensitive logic.

```csharp
// Current — not testable
resource.Updated = DateTime.UtcNow;

// Better — injectable
resource.Updated = _timeProvider.GetUtcNow().UtcDateTime;
```

### 34. Partial Success in Multi-Operation Handlers

When a handler performs two sequential operations (e.g., `SetLockoutEndDateAsync` then `ResetAccessFailedCountAsync`), test the scenario where the first succeeds and the second fails. The user may be left in an inconsistent state.

**Real example:** `OnPostSecurityClearLockoutAsync` in `Users/Edit.cshtml.cs` (lines 405–432) performs two operations:

```csharp
// Operation 1 — may succeed
var lockoutResult = await UserManager.SetLockoutEndDateAsync(TargetUser!, null);
if (!lockoutResult.Succeeded) { /* return early */ }

// Operation 2 — may fail after Operation 1 already committed
var resetResult = await UserManager.ResetAccessFailedCountAsync(TargetUser!);
if (!resetResult.Succeeded) { /* return error — but lockout is already cleared! */ }
```

This was identified during a coverage gap analysis and subsequently **fixed** using a best-effort rollback strategy. The handler now captures the original lockout end date before clearing, and restores it if the second operation fails:

```csharp
var originalLockoutEnd = await UserManager.GetLockoutEndDateAsync(TargetUser!);

var lockoutResult = await UserManager.SetLockoutEndDateAsync(TargetUser!, null);
if (!lockoutResult.Succeeded) { /* return error — nothing to roll back */ }

var resetResult = await UserManager.ResetAccessFailedCountAsync(TargetUser!);
if (!resetResult.Succeeded)
{
    // Best-effort rollback: restore the original lockout to avoid inconsistent state
    await UserManager.SetLockoutEndDateAsync(TargetUser!, originalLockoutEnd);
    AddIdentityErrors(resetResult);
    return await ReturnPageForTabAsync("security");
}
```

**The test** (`OnPostSecurityClearLockoutAsync_ShouldRollbackLockoutAndReturnErrors_WhenResetFailedCountFails`) verifies the rollback by asserting that `SetLockoutEndDateAsync` is called a second time with the original `DateTimeOffset.MaxValue` value.

**Three test strategies for multi-operation handlers:**

| Strategy | When to use |
|---|---|
| Test both operations succeeding (happy path) | Always |
| Test first operation failing (early return) | Always — verifies second operation is skipped |
| Test first succeeds, second fails (partial success) | Always when operations are not atomic — verify rollback behaviour |

**Resolution strategies (in order of preference):**
1. **Best-effort rollback** — capture original state, restore on failure (used here)
2. **Database transaction** — wrap both operations in a transaction (requires DbContext access)
3. **Accept and document** — characterization test that locks in the inconsistency (last resort)

### 35. Case-Insensitive Collection Syncing

When syncing collections (like Scopes or Redirect URIs) from a ViewModel's list of strings, always use `StringComparer.OrdinalIgnoreCase` and normalize casing (e.g., `.ToLowerInvariant()`) during the transformation phase to prevent duplicates and ensure data consistency.

> **Lessons 30–34 were added after a coverage gap analysis in March 2026** — see the [Testing Tutorial, Part 6](Testing-Tutorial.md#part-6-coverage-gap-analysis) for the full methodology.

```csharp
var desired = newValues
    .Where(v => !string.IsNullOrWhiteSpace(v))
    .Select(v => v.Trim().ToLowerInvariant())
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
```

---

## Templates

### Template: Unit Test for a Razor PageModel

```csharp
public class MyPageModelTests
{
    private readonly Mock<IMyService> _mockService;
    private readonly MyPageModel _sut;

    public MyPageModelTests()
    {
        _mockService = new Mock<IMyService>();
        _sut = new MyPageModel(_mockService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateItems_WhenServiceReturnsData()
    {
        // Arrange
        var expected = new List<ItemDto> { new() { Name = "Test" } };
        _mockService.Setup(x => x.GetItemsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Items.Should().BeEquivalentTo(expected);
    }
}
```

### Template: Integration Test for an Admin Page

```csharp
public class MyPageIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MyPageIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldReturnOk_WhenAdminUser()
    {
        var response = await _client.GetAsync("/Admin/MyPage");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        using var nonAdminFactory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();
        using var nonAdminClient = nonAdminFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await nonAdminClient.GetAsync("/Admin/MyPage");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        using var unauthFactory = new CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>();
        using var unauthClient = unauthFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await unauthClient.GetAsync("/Admin/MyPage");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

### Template: Service Integration Test

```csharp
public class MyServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private IMyService _sut = default!;

    public MyServiceTests(CustomWebApplicationFactory factory) => _factory = factory;

    public Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _sut = _scope.ServiceProvider.GetRequiredService<IMyService>();
        // Clean state if needed
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetItemsAsync_ShouldReturnSeededItems()
    {
        // Arrange — seed via TestDataHelper
        await TestDataHelper.SeedSomethingAsync(_factory, "test-item");

        // Act
        var result = await _sut.GetItemsAsync();

        // Assert
        result.Should().ContainSingle(x => x.Name == "test-item");
    }
}
```

### Template: E2E Test

```csharp
[Trait("Category", "E2E")]
public class MyPageE2eTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public MyPageE2eTests(PlaywrightFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync() => await _context.CloseAsync();

    [Fact]
    public async Task MyPage_ShouldNavigateToEdit_WhenEditClicked()
    {
        // Arrange
        var id = await TestDataHelper.SeedSomethingAsync(
            _fixture.Factory, $"e2e-item-{Guid.NewGuid():N}");

        // Act
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/MyPage");
        var row = _page.Locator("tr:has-text('e2e-item')");
        await Expect(row).ToBeVisibleAsync();

        var editLink = row.GetByRole(AriaRole.Link, new() { Name = "Edit" });
        await editLink.ClickAsync();

        // Assert
        await Expect(_page).ToHaveURLAsync(new Regex($"/Admin/MyPage/{id}/Edit"));
    }
}
```

### Minimum Test Coverage for a New Admin Page

| Level | Minimum Tests |
|---|---|
| **Unit** | 2: happy path (data returned) + empty state (no data) |
| **Integration** | 4: happy path DOM, empty state DOM, 403 (non-admin), 401 (unauthenticated) |
| **E2E** | 2: navigate to edit, navigate to create (if applicable) |

For pages with POST handlers, add per-handler:
- **Unit:** success path, validation failure, authorization failure
- **Integration:** form submission with DOM verification, validation message rendering

For **edit/create pages** (dual-mode with `Id == 0` → create, `Id > 0` → edit):

| Level | Minimum Tests |
|---|---|
| **Unit** | Per-handler: create happy path, edit happy path, validation failure, not-found, duplicate name. Plus claim add/remove handlers if applicable |
| **Integration** | Create form (empty fields), edit form (pre-filled), create POST, edit POST, duplicate name, 403, 401 |
| **E2E** | Create flow (fill + submit + redirect), edit flow (update + success message), claim add/remove if applicable, validation error display |
