# Testing Tutorial — How the IdentityServerStarter Tests Were Written

> A practical, example-driven tutorial that walks through how tests were developed for this project.
> Companion to the [Testing Reference Guide](Testing-Reference-Guide.md).

---

## Table of Contents

1. [Introduction](#introduction)
2. [Project Overview](#project-overview)
3. [Test Architecture at a Glance](#test-architecture-at-a-glance)
4. [Part 1: Unit Testing a Service](#part-1-unit-testing-a-service)
   - [Setting Up Mocks](#setting-up-mocks)
   - [Writing Your First Test](#writing-your-first-test)
   - [Testing Edge Cases with Theory](#testing-edge-cases-with-theory)
   - [Mocking IQueryable with MockQueryable](#mocking-iqueryable-with-mockqueryable)
   - [Using In-Memory SQLite for EF Core](#using-in-memory-sqlite-for-ef-core)
5. [Part 2: Unit Testing a Razor PageModel](#part-2-unit-testing-a-razor-pagemodel)
   - [PageModel Setup Pattern](#pagemodel-setup-pattern)
   - [Testing OnGet Handlers](#testing-onget-handlers)
   - [Testing OnPost Handlers](#testing-onpost-handlers)
   - [Testing ModelState Validation](#testing-modelstate-validation)
   - [Testing TempData Messages](#testing-tempdata-messages)
6. [Part 3: Integration Testing Services](#part-3-integration-testing-services)
   - [When Unit Tests Aren't Enough](#when-unit-tests-arent-enough)
   - [CustomWebApplicationFactory](#customwebapplicationfactory)
   - [IAsyncLifetime for Test Isolation](#iasynclifetime-for-test-isolation)
   - [Writing an Integration Test](#writing-an-integration-test)
7. [Part 4: Integration Testing Pages](#part-4-integration-testing-pages)
   - [HTTP Pipeline Testing](#http-pipeline-testing)
   - [Authentication & Authorization Tests](#authentication--authorization-tests)
   - [HTML Assertion with AngleSharp](#html-assertion-with-anglesharp)
8. [Part 5: E2E Testing with Playwright](#part-5-e2e-testing-with-playwright)
   - [When to Write E2E Tests](#when-to-write-e2e-tests)
   - [Shadow DOM and Web Components](#shadow-dom-and-web-components)
   - [Playwright Selector Strategies](#playwright-selector-strategies)
9. [Decision Framework: Which Test Level?](#decision-framework-which-test-level)
10. [Part 6: Coverage Gap Analysis](#part-6-coverage-gap-analysis)
    - [The Methodology](#the-methodology)
    - [What We Found](#what-we-found)
    - [Lessons That Emerged](#lessons-that-emerged)
11. [Patterns and Conventions Reference](#patterns-and-conventions-reference)
12. [Common Pitfalls and Lessons Learned](#common-pitfalls-and-lessons-learned)

---

## Introduction

This tutorial explains how the tests in this project were written, the decisions behind each testing approach, and the patterns that emerged across sessions. It is structured as a walkthrough — start from Part 1 and work through progressively, or jump to a specific section if you need guidance on a particular test type.

Every example in this tutorial is drawn from real tests in this codebase, not hypothetical code.

---

## Project Overview

### Technology Stack

| Component | Library | Purpose |
|---|---|---|
| .NET 10.0 | `net10.0` | Runtime |
| xUnit 2.9.3 | Test framework | Test runner and attributes |
| FluentAssertions 8.8.0 | Assertion library | Readable assertions |
| Moq 4.20.72 | Mocking framework | Dependency isolation |
| MockQueryable.Moq 10.0.2 | IQueryable mocking | EF Core LINQ in unit tests |
| AngleSharp 1.4.0 | HTML parser | Integration test HTML assertions |
| Playwright 1.49.0 | Browser automation | E2E testing |
| WebApplicationFactory | Test server | Integration/E2E infrastructure |
| EF Core InMemory / SQLite | Test databases | Data layer testing |

### What's Being Tested

The project is an IdentityServer admin UI with:

- **Services** — `ClaimsAdminService`, `RolesAdminService`, `ClientAdminService`, `IdentityResourcesAdminService`, `ApiScopesAdminService`, `UserEditor`
- **Razor Pages** — Admin pages for managing users, roles, claims, clients, API scopes, and identity resources
- **Authorization** — `[Authorize]` policies restricting admin access

---

## Test Architecture at a Glance

```
Tests/
├── Common/
│   └── IdentityServerAspNetIdentity.TestSupport/    # Shared infrastructure
│       └── Infrastructure/
│           ├── CustomWebApplicationFactory.cs        # WebApplicationFactory base
│           ├── TestAuthHandler.cs                    # Admin auth for tests
│           ├── NonAdminTestAuthHandler.cs            # Non-admin auth
│           ├── UnauthenticatedTestAuthHandler.cs     # No auth
│           ├── TestDataHelper.cs                     # Seed helpers
│           └── InMemoryServerSideSessionStore.cs     # Session test double
│
├── Unit/
│   ├── IdentityServerAspNetIdentity.UnitTests/      # Page model unit tests
│   │   ├── Pages/Admin/*/                            # One test file per page
│   │   └── TestHelpers.cs                            # Mock factory methods
│   └── IdentityServerServices.UnitTests/             # Service unit tests
│       ├── ClaimsAdminServiceTests.cs
│       ├── RolesAdminServiceTests.cs
│       ├── ClientAdminServiceTests.cs
│       ├── ApiScopesAdminServiceTests.cs
│       ├── UserEditorTests.cs
│       └── UserListItemDtoTests.cs
│
├── Integration/
│   └── IdentityServerAspNetIdentity.IntegrationTests/
│       ├── Services/                                 # Service integration tests
│       ├── Pages/Admin/*/                            # Page integration tests
│       └── Infrastructure/                           # Integration helpers
│           ├── TestDatabaseHelper.cs
│           ├── AngleSharpExtensions.cs
│           ├── HttpClientExtensions.cs
│           └── AntiforgeryTokenHelper.cs
│
└── E2E/
    └── IdentityServerAspNetIdentity.E2ETests/        # Playwright tests
```

### The Three Test Levels

| Level | What It Tests | Speed | Infrastructure |
|---|---|---|---|
| **Unit** | Single class in isolation, mocked dependencies | Fast (~ms) | Moq, in-memory SQLite |
| **Integration** | Real components through HTTP pipeline | Medium (~100ms) | WebApplicationFactory, in-memory DB |
| **E2E** | Full user journey in a real browser | Slow (~seconds) | Playwright + WebApplicationFactory |

---

## Part 1: Unit Testing a Service

### Setting Up Mocks

Every unit test class follows the same structure: declare mocks as fields, initialize them in the constructor, and expose the System Under Test (SUT) as `_sut`.

**Example: `RolesAdminServiceTests`**

```csharp
public class RolesAdminServiceTests
{
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly RolesAdminService _sut;

    public RolesAdminServiceTests()
    {
        // RoleManager requires an IRoleStore in its constructor
        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null!, null!, null!, null!);

        // UserManager requires an IUserStore + 9 null parameters
        _mockUserManager = TestHelpers.CreateMockUserManager();

        _sut = new RolesAdminService(
            _mockRoleManager.Object,
            _mockUserManager.Object);
    }
}
```

**Key takeaway:** `UserManager` and `RoleManager` have complex constructors. The project uses a `TestHelpers.CreateMockUserManager()` factory method to avoid repeating those 9+ null parameters everywhere.

### Writing Your First Test

Tests follow the **Arrange-Act-Assert** pattern and use the naming convention `MethodName_ShouldExpectedBehaviour_WhenCondition`.

**Example: Testing that `GetRolesAsync` returns sorted roles**

```csharp
[Fact]
public async Task GetRolesAsync_ShouldReturnRolesSortedByName()
{
    // Arrange
    var roles = new List<IdentityRole>
    {
        new("Editor") { Id = "1" },
        new("Admin")  { Id = "2" },
        new("Viewer") { Id = "3" },
    };
    _mockRoleManager.Setup(x => x.Roles)
        .Returns(roles.BuildMock());  // MockQueryable converts List to IQueryable

    // Act
    var result = await _sut.GetRolesAsync();

    // Assert
    result.Should().HaveCount(3);
    result[0].Name.Should().Be("Admin");
    result[1].Name.Should().Be("Editor");
    result[2].Name.Should().Be("Viewer");
}
```

**Why `BuildMock()`?** Standard `List<T>.AsQueryable()` doesn't support async LINQ operators like `ToListAsync()`. The `MockQueryable.Moq` library provides `.BuildMock()` which wraps a list in a mock `IQueryable<T>` that supports async enumeration.

### Testing Edge Cases with Theory

Use `[Theory]` with `[InlineData]` to test multiple inputs without duplicating test methods. This pattern appears throughout the codebase for input validation.

**Example: Testing whitespace/null claim types in `ClaimsAdminServiceTests`**

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public async Task AddUserToClaimAsync_ShouldReturnError_WhenClaimTypeIsInvalid(
    string? claimType)
{
    // Arrange — no mock setup needed, validation happens before any service call

    // Act
    var result = await _sut.AddUserToClaimAsync(claimType!, "user1", "value");

    // Assert
    result.Status.Should().Be(AddClaimAssignmentStatus.ClaimTypeNotFound);
}
```

**Pattern:** Test validation/guard logic first — these tests are fast, require no mock setup, and catch a common category of bugs.

### Mocking IQueryable with MockQueryable

When a service queries `RoleManager.Roles` or `UserManager.Users` (which return `IQueryable<T>`), use `BuildMock()`:

```csharp
// Arrange
var users = new List<ApplicationUser>
{
    new() { Id = "1", UserName = "alice@example.com", Email = "alice@example.com" },
    new() { Id = "2", UserName = "bob@example.com", Email = "bob@example.com" },
};
_mockUserManager.Setup(m => m.Users).Returns(users.BuildMock());
```

This is required because the service code uses async EF Core extensions (`ToListAsync`, `FirstOrDefaultAsync`) that fail on plain `IEnumerable<T>.AsQueryable()`.

### Using In-Memory SQLite for EF Core

Some services use `ApplicationDbContext` directly. Rather than mocking `DbContext` (which is fragile and doesn't test query translation), these tests use real SQLite in-memory databases.

**Example: `ClaimsAdminServiceTests` setup**

```csharp
public ClaimsAdminServiceTests()
{
    // Each test gets its own database (GUID in connection string)
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
        .Options;

    _context = new ApplicationDbContext(options);
    _context.Database.OpenConnection();
    _context.Database.EnsureCreated();

    _mockUserManager = TestHelpers.CreateMockUserManager();
    _sut = new ClaimsAdminService(_mockUserManager.Object, _context);
}
```

**Why SQLite instead of `UseInMemoryDatabase`?**
- SQLite respects foreign key constraints
- SQLite translates LINQ to real SQL, catching query translation bugs
- `UseInMemoryDatabase` silently succeeds on queries that would fail against a real database

**Per-test isolation:** The `Guid.NewGuid()` in the connection string ensures each test gets a fresh, empty database.

### The Callback Hack for Mocked Managers

When mocking `UserManager` operations like `RemoveClaimAsync`, the mock doesn't actually modify the database. If your Assert phase queries the database, you need to simulate the side effect:

```csharp
_mockUserManager
    .Setup(m => m.RemoveClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
    .Callback<ApplicationUser, Claim>((user, claim) =>
    {
        // Manually remove from the real SQLite DB so Assert can verify
        var toRemove = _context.UserClaims
            .First(c => c.UserId == user.Id && c.ClaimType == claim.Type);
        _context.UserClaims.Remove(toRemove);
        _context.SaveChanges();
    })
    .ReturnsAsync(IdentityResult.Success);
```

**This is a key lesson:** When your service uses both mocked managers *and* a real `DbContext`, you need to keep them in sync manually via `Callback`.

---

## Part 2: Unit Testing a Razor PageModel

### PageModel Setup Pattern

Page model tests mock the injected service and create a minimal `PageContext`:

```csharp
public class EditModelTests
{
    private readonly Mock<IClaimsAdminService> _mockClaimsAdminService;
    private readonly EditModel _sut;

    public EditModelTests()
    {
        _mockClaimsAdminService = new Mock<IClaimsAdminService>();
        _sut = new EditModel(_mockClaimsAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>())
        };
    }
}
```

**Important:** Always use a real `TempDataDictionary`, not a mocked one. This lets you assert actual TempData values rather than just verifying setter calls.

### Testing OnGet Handlers

```csharp
[Fact]
public async Task OnGetAsync_ShouldPopulatePageProperties_WhenClaimTypeExists()
{
    // Arrange
    _mockClaimsAdminService.Setup(s => s.GetForEditAsync("email"))
        .ReturnsAsync(new ClaimEditDto
        {
            ClaimType = "email",
            UsersInClaim = new List<UserClaimAssignmentDto>(),
            AvailableUsers = new List<AvailableUserDto>()
        });

    // Act
    var result = await _sut.OnGetAsync("email");

    // Assert
    result.Should().BeOfType<PageResult>();
    _sut.ClaimType.Should().Be("email");
    _sut.UsersInClaim.Should().BeEmpty();
}
```

**Pattern:** Mock the service to return specific data, call the handler, then assert both:
1. The **return type** (`PageResult`, `NotFoundResult`, `RedirectToPageResult`)
2. The **page properties** that the Razor view binds to

### Testing OnPost Handlers

Post handlers involve validating input, calling the service, and setting TempData messages:

```csharp
[Fact]
public async Task OnPostAddUserAsync_ShouldRedirectWithSuccess_WhenServiceReturnsSuccess()
{
    // Arrange
    _sut.ClaimType = "role";
    _sut.SelectedUserId = "user-1";
    _sut.NewClaimValue = "admin";

    _mockClaimsAdminService
        .Setup(s => s.AddUserToClaimAsync("role", "user-1", "admin"))
        .ReturnsAsync(new AddClaimAssignmentResult
        {
            Status = AddClaimAssignmentStatus.Success,
            UserName = "alice@example.com"
        });

    // Act
    var result = await _sut.OnPostAddUserAsync();

    // Assert
    var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
    redirect.PageName.Should().Be("/Admin/Claims/Edit");
    redirect.RouteValues!["claimType"].Should().Be("role");

    _sut.TempData["SuccessMessage"].Should().Be(
        "User 'alice@example.com' added to claim 'role'.");
}
```

### Testing ModelState Validation

Validation attributes (`[Required]`, etc.) are only enforced by the HTTP pipeline, not by PageModel directly. To test validation logic in unit tests, add errors to `ModelState` manually:

```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public async Task OnPostAddUserAsync_ShouldReturnBadRequest_WhenSelectedUserIdIsInvalid(
    string? userId)
{
    // Arrange
    _sut.ClaimType = "role";
    _sut.SelectedUserId = userId!;

    // Act
    var result = await _sut.OnPostAddUserAsync();

    // Assert
    result.Should().BeOfType<BadRequestResult>();
    _mockClaimsAdminService.Verify(
        s => s.AddUserToClaimAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
        Times.Never);  // Service should never be called for invalid input
}
```

**Key insight:** Use `Verify(Times.Never)` to confirm the service was **not** called when validation should have short-circuited.

### Testing TempData Messages

```csharp
// Assert success message
_sut.TempData["SuccessMessage"].Should().Be("Role 'Editor' created successfully.");

// Assert warning message
_sut.TempData["WarningMessage"].Should().Contain("already exists");
```

**Pattern:** The project consistently uses `SuccessMessage` and `WarningMessage` keys in TempData for user feedback.

---

## Part 3: Integration Testing Services

### When Unit Tests Aren't Enough

The project initially skipped unit tests for `IdentityResourcesAdminService` (using only integration tests instead). The reasoning:

> The service's logic is fundamentally tied to EF Core query translation and database constraints. Mocking would add maintenance overhead without catching additional bugs since the integration tests already exercise all control flow paths against real SQLite.

**Update (March 2026):** A [coverage gap analysis](#part-6-coverage-gap-analysis) later revealed that the service unit tests (using real SQLite in-memory databases, not mocks) were missing success-path tests entirely — only failure paths were covered. Unit-level tests using SQLite were added for `GetIdentityResourcesAsync`, `UpdateAsync` (success), `AddClaimAsync` (success), and `RemoveClaimAsync` (success) to verify data persistence and input normalization. The key distinction: these tests use **real SQLite**, not mocked `DbContext`, so they catch the same query translation bugs as integration tests while being faster and more focused.

**Rule of thumb from this project:**
- **Use unit tests** for pure logic, conditional branching, input validation, DTO mapping
- **Use integration tests** when the logic is inseparable from EF Core queries, `UserManager`/`RoleManager` operations, or database constraints

### CustomWebApplicationFactory

All integration tests share a `CustomWebApplicationFactory` that:

1. Replaces real databases with in-memory databases
2. Configures test authentication (admin by default)
3. Removes antiforgery token validation
4. Seeds initial test data

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            // Replace DbContexts with in-memory databases
            // Configure test authentication scheme
            // Remove antiforgery filters
        });
    }
}
```

### IAsyncLifetime for Test Isolation

Integration test classes implement `IAsyncLifetime` to set up and tear down per-test state:

```csharp
public class IdentityResourcesAdminServiceTests
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = null!;
    private IdentityResourcesAdminService _sut = null!;
    private ConfigurationDbContext _configContext = null!;

    public IdentityResourcesAdminServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        // Create a fresh DI scope for each test
        _scope = _factory.Services.CreateScope();
        _configContext = _scope.ServiceProvider
            .GetRequiredService<ConfigurationDbContext>();
        _sut = _scope.ServiceProvider
            .GetRequiredService<IdentityResourcesAdminService>();

        // Clean state: remove all identity resources before each test
        _configContext.IdentityResources.RemoveRange(
            _configContext.IdentityResources);
        await _configContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }
}
```

**Critical pattern:** Clean the database at the start of each test in `InitializeAsync`, not at the end. This guarantees isolation even if a previous test failed and didn't clean up.

### Writing an Integration Test

```csharp
[Fact]
public async Task UpdateAsync_ShouldReturnDuplicateName_WhenAnotherResourceHasSameName()
{
    // Arrange — seed two resources
    _configContext.IdentityResources.AddRange(
        new IdentityResource { Name = "profile", DisplayName = "Profile" },
        new IdentityResource { Name = "email", DisplayName = "Email" });
    await _configContext.SaveChangesAsync();

    var existing = await _configContext.IdentityResources
        .FirstAsync(r => r.Name == "email");

    // Act — try to rename "email" to "profile" (collision)
    var result = await _sut.UpdateAsync(existing.Id, new IdentityResourceEditDto
    {
        Name = "profile",
        DisplayName = "Email Address"
    });

    // Assert
    result.Should().Be(UpdateIdentityResourceStatus.DuplicateName);
}
```

**Pattern:** Integration tests seed their own data, call the real service, and assert against the actual database state.

### Reloading Navigation Collections

After mutating child collections (e.g., adding claims to a resource), you must explicitly reload the navigation collection to verify persistence:

```csharp
// After adding a claim
await _sut.AddClaimAsync(resourceId, "email");

// Reload to verify it was persisted (not just in EF's change tracker)
var updated = await _configContext.IdentityResources.FindAsync(resourceId);
await _configContext.Entry(updated!).Collection(r => r.UserClaims).LoadAsync();

updated.UserClaims.Should().ContainSingle()
    .Which.Type.Should().Be("email");
```

### Avoiding EF Core Identity Map

Integration tests can be tricked by EF Core's change tracker returning cached entities instead of querying the database. Two strategies:

```csharp
// Strategy 1: Clear the change tracker
_configContext.ChangeTracker.Clear();
var fresh = await _configContext.Clients.FindAsync(clientId);

// Strategy 2: Use AsNoTracking
var fresh = await _configContext.Clients
    .AsNoTracking()
    .FirstAsync(c => c.Id == clientId);
```

---

## Part 4: Integration Testing Pages

### HTTP Pipeline Testing

Page integration tests send real HTTP requests to the test server and assert on responses:

```csharp
public class IndexModelIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IndexModelIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Index_ShouldRenderRolesTable_WhenDataExists()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await HtmlHelpers.GetDocumentAsync(response);
        var rows = html.QuerySelectorAll("table tbody tr");
        rows.Should().NotBeEmpty();
    }
}
```

### Authentication & Authorization Tests

The project uses three custom `TestAuthHandler` implementations to test different auth scenarios:

| Handler | Simulates | Claims |
|---|---|---|
| `TestAuthHandler` | Admin user | `role: ADMIN` |
| `NonAdminTestAuthHandler` | Authenticated non-admin | No admin claims |
| `UnauthenticatedTestAuthHandler` | No authentication | Fails auth |

```csharp
[Fact]
public async Task Index_ShouldReturn403_WhenUserIsNotAdmin()
{
    // Use a factory configured with NonAdminTestAuthHandler
    var client = _factory
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace auth handler with non-admin version
            });
        })
        .CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false  // Capture 403 before redirect
        });

    var response = await client.GetAsync("/Admin/Roles");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task Index_ShouldReturn401_WhenUserIsNotAuthenticated()
{
    var client = _unauthenticatedFactory.CreateClient(
        new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false  // Capture 401 before redirect
        });

    var response = await client.GetAsync("/Admin/Roles");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

**Critical:** Set `AllowAutoRedirect = false`. Without this, the test follows the redirect to the login page and gets a 200 OK, hiding the auth failure.

### HTML Assertion with AngleSharp

AngleSharp parses the response HTML so you can query the DOM:

```csharp
[Fact]
public async Task Edit_ShouldRenderAvailableUsersDropdown_WhenUsersExist()
{
    var response = await _client.GetAsync("/Admin/Roles/Edit/1");
    var html = await HtmlHelpers.GetDocumentAsync(response);

    // Query specific elements
    var select = html.QuerySelector("select[name='SelectedUserId']");
    select.Should().NotBeNull();

    var options = html.QuerySelectorAll("select[name='SelectedUserId'] option");
    options.Should().HaveCountGreaterThan(0);
}
```

**Pattern:** Use CSS selectors to find form elements, then assert their content, attributes, or visibility.

---

## Part 5: E2E Testing with Playwright

### When to Write E2E Tests

E2E tests were written for scenarios that **cannot** be verified at lower test levels:

| Scenario | Why E2E? |
|---|---|
| Shadow DOM / web component rendering | Integration tests see raw HTML, not rendered components |
| Tab navigation visibility | Content hidden until JavaScript runs |
| Confirmation modals | Browser-level interaction |
| Multi-page navigation flows | Full user journey |
| Success/warning alerts after redirects | TempData rendering in the browser |

### Shadow DOM and Web Components

The project uses custom web components (`<ck-responsive-row>`, etc.) that render in Shadow DOM. This fundamentally changes selector strategies:

```csharp
// CSS selectors FAIL on Shadow DOM
// ❌ page.QuerySelectorAsync("ck-responsive-row td")  // returns null

// Use GetByRole for Shadow DOM elements
// ✅
await page.GetByRole(AriaRole.Row).Filter(new() { HasText = "admin" }).ClickAsync();
```

### Playwright Selector Strategies

The project adopted these selector priorities:

1. **`GetByRole`** — preferred, works across Shadow DOM, accessible
2. **`GetByText`** — for visible text content
3. **`data-testid` attributes** — for elements without semantic roles
4. **CSS selectors** — last resort, avoid for Shadow DOM elements

**Example: E2E test for adding a user to a role**

```csharp
[Fact]
[Trait("Category", "E2E")]
public async Task AddUser_ShouldShowSuccessAlert_WhenUserAddedToRole()
{
    // Navigate
    await _page.GotoAsync("/Admin/Roles/Edit/1");

    // Click the "Users" tab
    await _page.GetByRole(AriaRole.Tab, new() { Name = "Users" }).ClickAsync();

    // Select a user from the dropdown
    await _page.SelectOptionAsync("select[name='SelectedUserId']",
        new SelectOptionValue { Label = "alice@example.com" });

    // Submit
    await _page.GetByRole(AriaRole.Button, new() { Name = "Add User" }).ClickAsync();

    // Assert success alert appeared
    var alert = _page.Locator(".alert-success");
    await Expect(alert).ToBeVisibleAsync();
    await Expect(alert).ToContainTextAsync("alice@example.com");
}
```

### E2E Test Infrastructure

E2E tests use `IClassFixture<PlaywrightFixture>` and `IAsyncLifetime`:

```csharp
[Trait("Category", "E2E")]
public class RolesAdminE2eTests
    : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _browser = await _fixture.Playwright.Chromium.LaunchAsync();
        var context = await _browser.NewContextAsync();
        _page = await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
    }
}
```

---

## Part 6: Coverage Gap Analysis

After building up unit, integration, and E2E tests across all Admin pages, a systematic coverage gap analysis was performed to find significant methods still lacking tests. This section documents the methodology, findings, and lessons — serving as a template for future coverage reviews.

### The Methodology

Coverage gap analysis follows a three-step process:

**Step 1: Inventory all testable methods.** For each source file in the target area, list every public handler method and its branching paths. For PageModels, this means every `OnGetAsync`, `OnPostAsync`, and named handler (`OnPostAddClaimAsync`, etc.). For services, this means every public method on the interface.

**Step 2: Inventory all existing tests.** For each test file, list the test method names and which source method + branch they exercise. Group by source method to see coverage at a glance.

**Step 3: Diff the two inventories.** The missing entries are your gaps. Prioritise by:

| Priority | Criteria |
|---|---|
| **High** | Entire file/method with zero tests |
| **High** | Service success paths (data persistence) with only failure paths tested |
| **Medium** | Authorization (forbid) tests missing for security-sensitive handlers |
| **Medium** | Failure paths missing when the parallel method's failures are fully tested |
| **Low** | Edge cases in already-tested methods |

### What We Found

The analysis of `Pages/Admin/` revealed these gaps:

#### 1. Clients Index Page — No tests at all (High)

Every other Index page (`ApiScopes`, `Claims`, `IdentityResources`, `Roles`, `Users`) had unit tests. The Clients Index was the only one missing. This was simply an oversight — the file structure made it easy to miss because `Clients/EditModelTests.cs` existed, creating the impression that Clients was covered.

**Generated:** `Clients/IndexModelTests.cs` with 2 tests matching the exact pattern from `ApiScopes/IndexModelTests.cs`:

```csharp
public class IndexModelTests
{
    private readonly Mock<IClientAdminService> _mockClientAdminService;
    private readonly IndexModel _sut;

    public IndexModelTests()
    {
        _mockClientAdminService = new Mock<IClientAdminService>();
        _sut = new IndexModel(_mockClientAdminService.Object)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateClients_WhenServiceReturnsData()
    {
        // Arrange
        var expectedClients = new List<ClientListItemDto>
        {
            new() { Id = 1, ClientId = "spa-client", ClientName = "SPA", Enabled = true },
            new() { Id = 2, ClientId = "api-client", ClientName = "API", Enabled = false }
        };
        _mockClientAdminService
            .Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClients);

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Clients.Should().HaveCount(2);
        _sut.Clients.Should().BeEquivalentTo(expectedClients);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnEmptyList_WhenServiceReturnsNoClients()
    {
        _mockClientAdminService
            .Setup(x => x.GetClientsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClientListItemDto>());

        await _sut.OnGetAsync();

        _sut.Clients.Should().BeEmpty();
    }
}
```

**Lesson:** During coverage reviews, check test *files* before checking test *methods*. A missing file is the largest possible gap.

#### 2. IdentityResourcesAdminService — Success paths missing (High)

The existing tests covered failure paths thoroughly: `NotFound`, `DuplicateName`, `AlreadyApplied`, `NotApplied`. But the success paths — where data actually gets created, updated, or deleted in the database — had no tests. This meant a regression breaking `SaveChangesAsync` or the mapping logic would go undetected.

**Generated 5 tests:**

| Test | What it uniquely catches |
|---|---|
| `GetIdentityResourcesAsync_ShouldReturnMappedDtos_SortedByName` | Null coalescing (`DisplayName ?? ""`) and sort order |
| `GetIdentityResourcesAsync_ShouldReturnEmptyList_WhenNoResourcesExist` | Empty DB doesn't throw |
| `UpdateAsync_ShouldUpdateAllFields_WhenResourceExists` | All fields mapped, `Trim()` applied, data persisted |
| `AddClaimAsync_ShouldAddClaimAndPersist_WhenClaimIsNew` | Claim actually added to navigation collection and saved |
| `RemoveClaimAsync_ShouldRemoveClaimAndPersist_WhenClaimExists` | Claim actually removed and saved |

**Key pattern — verifying persistence after mutations:**

```csharp
// Act
var result = await sut.AddClaimAsync(resource.Id, " role ");

// Assert: reload and verify the claim was actually persisted
var reloaded = await configDb.IdentityResources
    .Include(r => r.UserClaims)
    .FirstAsync(r => r.Id == resource.Id);
reloaded.UserClaims.Should().HaveCount(2);
reloaded.UserClaims.Select(c => c.Type).Should().Contain("role");
```

**Lesson:** Success-path tests and failure-path tests catch completely different categories of bugs. A suite with only failure tests proves the guard clauses work — but doesn't prove the happy path actually saves data.

#### 3. Roles Edit — Asymmetric remove-user coverage (Medium)

`OnPostAddUserAsync` had tests for `RoleNotFound`, `Failed`, and `Success`. `OnPostRemoveUserAsync` — which uses the exact same `switch` structure — only had `NoUserSelected` and `Success`. The `NotFound` and `Failed` paths were missing.

**Generated 2 tests** mirroring the existing `AddUser` tests:

```csharp
[Fact]
public async Task OnPostRemoveUserAsync_ShouldReturnNotFound_WhenServiceReportsNotFound()
{
    _sut.RoleId = "role-1";
    _sut.SelectedUserId = "user-1";
    _mockRolesAdminService
        .Setup(x => x.RemoveUserFromRoleAsync(_sut.RoleId, _sut.SelectedUserId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new RemoveUserFromRoleResult { Status = RemoveUserFromRoleStatus.RoleNotFound });

    var result = await _sut.OnPostRemoveUserAsync();

    result.Should().BeOfType<NotFoundResult>();
}
```

**Lesson:** When two methods share the same branching structure, they're the most likely candidates for asymmetric coverage. Always compare parallel methods side-by-side.

#### 4. Users Edit — Missing forbid and failure tests (Medium)

Out of 15 security handler methods, 11 had forbid tests. The 4 missing were:
- `OnPostSecurityDisableAccountAsync`
- `OnPostSecurityClearLockoutAsync`
- `OnPostSecurityToggleLockoutEnabledAsync`
- `OnPostSecurityForceSignOutAsync`

Similarly, `ToggleLockoutEnabled`, `ToggleTwoFactor`, and `ResetPassword` handlers were missing failure-path tests (what happens when `UpdateUserFromEditPostAsync` returns a failed `IdentityResult`).

**Generated 7 tests** — 4 forbid + 3 failure paths.

**Lesson:** Authorization tests should be exhaustive, not sampled. The cost is ~5 lines each; the risk of a missing auth check is production-critical.

### Lessons That Emerged

Five insights from this analysis that aren't obvious from individual test-writing sessions:

1. **Missing test files are the biggest gap.** Before checking individual methods, verify every source file has a corresponding test file. The Clients Index had no test file at all — easy to miss when the adjacent Edit file existed.

2. **Success paths and failure paths catch different bugs.** A suite with only failure-path tests proves guard clauses work but doesn't prove the happy path actually saves data. Always test both.

3. **Parallel methods need parallel test coverage.** When `AddX` and `RemoveX` share structure, the Remove tests are often incomplete because the developer thought "I already tested this pattern."

4. **Authorization forbid tests should be exhaustive.** Even if handlers share the same policy, each needs its own test. Policy assignments can change independently.

5. **`DateTime.UtcNow` is a testability smell.** Direct time access makes assertions impossible. .NET 8+ provides `TimeProvider` as an injectable abstraction. Flag it for future refactoring.

6. **Multi-operation handlers need partial-success tests.** `OnPostSecurityClearLockoutAsync` performs `SetLockoutEndDateAsync` then `ResetAccessFailedCountAsync` sequentially. A coverage analysis found that the original code had no rollback — if the second operation failed, the lockout was already cleared, leaving the user in an inconsistent state (with a TODO comment acknowledging the issue). This was **fixed** by capturing the original lockout end date before clearing and restoring it on failure. The test (`OnPostSecurityClearLockoutAsync_ShouldRollbackLockoutAndReturnErrors_WhenResetFailedCountFails`) now verifies the rollback by asserting that `SetLockoutEndDateAsync` is called a second time with the original value. See [Reference Guide lesson #34](Testing-Reference-Guide.md#34-partial-success-in-multi-operation-handlers) for the full pattern.

---

## Decision Framework: Which Test Level?

This framework was used consistently across the project to decide where each test belongs:

| Question | If Yes → | If No → |
|---|---|---|
| Does the logic depend on EF Core query translation? | Integration | Continue |
| Does it require real `UserManager`/`RoleManager` operations? | Integration | Continue |
| Is it pure conditional/branching logic with injectable deps? | Unit | Continue |
| Does it require the HTTP middleware pipeline (auth, routing)? | Integration (page-level) | Continue |
| Does it involve browser rendering, JS, Shadow DOM? | E2E | Continue |
| Does it require multi-page navigation or real user flow? | E2E | Unit or Integration |

### Real Examples from This Project

| Behaviour | Level | Reasoning |
|---|---|---|
| `GetRolesAsync` returns sorted list | Unit | Pure mapping + LINQ, deps mockable |
| `UpdateAsync` detects duplicate names | Integration | Requires EF Core unique constraint |
| `[Authorize]` blocks non-admin users | Integration (page) | Middleware pipeline enforcement |
| Tab visibility in Shadow DOM | E2E | JavaScript rendering required |
| `AddClaimAsync` returns `AlreadyApplied` | Integration | Database constraint check |
| `ShouldDefaultNewClaimValue` boolean logic | Unit | Pure conditional logic |
| Collection sanitization (trim, dedup) | Unit | Pure string manipulation |
| Confirmation modal before delete | E2E | Browser-level UI interaction |

---

## Patterns and Conventions Reference

### Naming Convention

```
MethodName_ShouldExpectedBehaviour_WhenCondition
```

Examples:
- `GetRolesAsync_ShouldReturnRolesSortedByName`
- `AddUserToClaimAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist`
- `OnPostAddUserAsync_ShouldReturnBadRequest_WhenClaimTypeIsWhitespace`
- `UpdateClientAsync_ShouldSanitizeInput_AndHandleDuplicatesCaseInsensitively`

### Test Class Structure

```csharp
public class [ComponentName]Tests
{
    // 1. Mock declarations (readonly)
    private readonly Mock<IDependency> _mockDep;

    // 2. SUT declaration (readonly)
    private readonly ComponentUnderTest _sut;

    // 3. Constructor — initialize mocks and SUT
    public [ComponentName]Tests()
    {
        _mockDep = new Mock<IDependency>();
        _sut = new ComponentUnderTest(_mockDep.Object);
    }

    // 4. Tests grouped by method, organized with comment headers
    // -- GetAsync --

    [Fact]
    public async Task GetAsync_ShouldReturnItems()
    {
        // Arrange
        // Act
        // Assert
    }

    // -- CreateAsync --

    [Fact]
    public async Task CreateAsync_ShouldSucceed()
    {
        // ...
    }
}
```

### FluentAssertions Patterns Used

```csharp
// Basic
result.Should().NotBeNull();
result.Should().Be(ExpectedStatus.Success);
result.Should().BeOfType<PageResult>();

// Collections
result.Should().HaveCount(3);
result.Should().BeEmpty();
result.Should().ContainSingle().Which.Name.Should().Be("admin");
result.Should().AllSatisfy(x => x.IsActive.Should().BeTrue());

// Nested property chains
result.UsersInClaim.Single().IsLastUserAssignment.Should().BeTrue();

// Redirect assertions
var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
redirect.PageName.Should().Be("/Admin/Claims/Edit");
redirect.RouteValues!["claimType"].Should().Be("role");

// ModelState assertions
_sut.ModelState[nameof(_sut.SelectedUserId)]!.Errors
    .Should().ContainSingle()
    .Which.ErrorMessage.Should().Be("Please select a user");

// Exception assertions
await act.Should().ThrowAsync<ArgumentNullException>();
```

### Moq Patterns Used

```csharp
// Setup with return
_mock.Setup(s => s.GetByIdAsync(42)).ReturnsAsync(entity);

// Setup with any argument
_mock.Setup(s => s.FindAsync(It.IsAny<string>())).ReturnsAsync(user);

// Verify method was called
_mock.Verify(s => s.SaveAsync(), Times.Once);

// Verify method was NOT called
_mock.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);

// Callback for side effects
_mock.Setup(s => s.RemoveAsync(It.IsAny<Item>()))
    .Callback<Item>(item => _context.Items.Remove(item))
    .ReturnsAsync(Result.Success);

// Specific argument matching (prefer over It.IsAny for important parameters)
_mock.Setup(s => s.GetAsync(It.Is<int>(id => id == 42)))
    .ReturnsAsync(expectedResult);
```

---

## Common Pitfalls and Lessons Learned

These lessons emerged from real debugging sessions during test development:

### 1. `It.IsAny<T>()` Hides Bugs

Using `It.IsAny<int>()` for ID parameters means the test passes even if the SUT sends the wrong ID to the service. **Use specific values:**

```csharp
// ❌ Hides a bug where the page sends the wrong ID
_mock.Setup(s => s.GetForEditAsync(It.IsAny<int>())).ReturnsAsync(dto);

// ✅ Verifies the correct ID is passed through
_mock.Setup(s => s.GetForEditAsync(42)).ReturnsAsync(dto);
_sut.Id = 42;
```

### 2. `[Authorize]` Is Metadata, Not Logic

`[Authorize]` attributes are only enforced by the ASP.NET Core middleware pipeline. In unit tests, there is no middleware — the handler runs regardless. **Auth enforcement must be tested at the integration level.**

### 3. ModelState Is Not Validated Automatically in Unit Tests

Validation attributes (`[Required]`, `[StringLength]`) are processed by the model binding pipeline during HTTP requests. In unit tests, `ModelState` starts valid. You must either:
- Add errors manually: `_sut.ModelState.AddModelError("Name", "Required")`
- Test validation at the integration level with real HTTP requests

### 4. EF Core Change Tracker Can Lie

After saving an entity, querying it from the same `DbContext` may return the cached version, not what's actually in the database. Always clear the tracker or use `AsNoTracking()` when asserting persistence.

### 5. SQLite FK Constraints Affect Test Seeding

When seeding `IdentityUserClaim<string>` records, you must first seed the user they reference, or SQLite's foreign key constraints will throw. `UseInMemoryDatabase` would silently allow this, masking a real bug.

### 6. Collection Reloading After Mutations

After adding/removing items from a navigation collection, you must explicitly reload it:

```csharp
await _context.Entry(entity).Collection(e => e.UserClaims).LoadAsync();
```

Without this, the collection may be empty (if it was never loaded) or stale (if the mutation happened through a different code path).

### 7. `AllowAutoRedirect = false` for Auth Tests

Without this setting, auth test failures (401/403) redirect to the login page, which returns 200 OK — making the test pass when it should fail.

### 8. Shadow DOM Breaks Standard CSS Selectors

Playwright's `QuerySelector` cannot pierce Shadow DOM boundaries. Use `GetByRole`, `GetByText`, or `GetByTestId` instead of CSS selectors for web components.

### 9. One Behaviour Per Test

Combining multiple independent validations in one test masks failures. If the first assertion fails, you never know if the second one works:

```csharp
// ❌ Two independent behaviours in one test
[Fact]
public async Task OnPost_ShouldValidate()
{
    // ... test null input ...
    result.Should().BeOfType<BadRequestResult>();

    // ... test whitespace input ...  // Never runs if null test fails
    result2.Should().BeOfType<BadRequestResult>();
}

// ✅ Separate tests, use [Theory] if inputs vary
[Theory]
[InlineData(null)]
[InlineData("   ")]
public async Task OnPost_ShouldReturnBadRequest_WhenInputIsInvalid(string? input)
{
    // ...
}
```

### 10. Password Update Atomicity — A Real Bug Found by Tests

Tests for `UserEditor` revealed that the original password update flow (`RemovePasswordAsync` + `AddPasswordAsync`) could leave users in a passwordless state if `AddPasswordAsync` failed. This was refactored to use `GeneratePasswordResetTokenAsync` + `ResetPasswordAsync` — a single atomic operation that never removes the existing password.

**The test that caught it:**

```csharp
[Fact]
public async Task UpdateUser_ShouldNotRemoveExistingPassword_WhenNewPasswordFails()
{
    // Arrange — user has existing password
    _mockUserManager.Setup(m => m.HasPasswordAsync(It.IsAny<ApplicationUser>()))
        .ReturnsAsync(true);
    _mockUserManager.Setup(m => m.ResetPasswordAsync(...))
        .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Too weak" }));

    // Act
    var result = await _sut.UpdateUserFromEditPostAsync(dto);

    // Assert — user still has their old password (no RemovePasswordAsync call)
    _mockUserManager.Verify(m => m.RemovePasswordAsync(It.IsAny<ApplicationUser>()),
        Times.Never);
}
```

### 11. Case-Insensitive Collection Syncing — Another Bug Found

Tests for `ClientAdminService` discovered that OAuth scope collections used case-sensitive comparison, allowing `"openid"` and `"OPENID"` to coexist as separate entries. The fix: change from `StringComparer.Ordinal` to `StringComparer.OrdinalIgnoreCase` with `.ToLowerInvariant()`.

### 12. Asymmetric Coverage — The Silent Gap

When two methods share the same branching structure (`OnPostAddUserAsync` and `OnPostRemoveUserAsync` both use the same `switch`), it's natural to test one thoroughly and assume the other is implicitly covered. It isn't — each method needs its own failure-path tests. This was caught during the [coverage gap analysis](#part-6-coverage-gap-analysis) when `OnPostRemoveUserAsync` in `Roles/Edit` was found to have only 2 tests vs. `OnPostAddUserAsync`'s 4.

### 13. Success-Path Service Tests Are Not Optional

The `IdentityResourcesAdminService` had thorough failure-path tests (`NotFound`, `DuplicateName`, `AlreadyApplied`) but zero success-path tests. This meant a regression that broke `SaveChangesAsync` or the field mapping logic would have gone completely undetected. The coverage analysis added tests that verify data actually persists and input normalization (e.g., `Trim()`) works end-to-end.

### 14. Authorization Forbid Tests Must Be Exhaustive

The Users Edit page had forbid tests for 11 out of 15 security handlers — a 73% coverage rate that looks adequate at a glance. But each handler is an independent authorization boundary. If someone refactors to per-handler policies, the 4 untested handlers would have no safety net. The coverage analysis added the missing 4 forbid tests at ~5 lines each.

---

## Summary

The testing approach in this project follows a clear philosophy:

1. **Test at the right level** — unit tests for logic, integration for infrastructure, E2E for UI
2. **Real databases over mocks** — SQLite in-memory catches real query translation issues
3. **One behaviour per test** — each test proves exactly one thing
4. **Tests find real bugs** — several production bugs were discovered and fixed through test-first development
5. **Shared infrastructure** — `CustomWebApplicationFactory`, `TestHelpers`, and `TestDataHelper` reduce boilerplate
6. **Specific mock setup** — use exact values, not `It.IsAny<T>()`, for important parameters
7. **Convention-driven** — consistent naming, structure, and assertion patterns across all tests
8. **Periodic coverage analysis** — systematic gap reviews catch asymmetric coverage, missing files, and untested success paths that incremental test-writing misses

For the full reference of shared infrastructure, templates, and configuration, see the [Testing Reference Guide](Testing-Reference-Guide.md).
