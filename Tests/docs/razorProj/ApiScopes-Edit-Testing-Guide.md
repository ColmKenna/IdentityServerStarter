# ApiScopes Edit Page — Testing Lessons & Approach

## Target File

- **Page handler:** `src/IdentityServerAspNetIdentity/Pages/Admin/ApiScopes/Edit.cshtml.cs`
- **Razor view:** `src/IdentityServerAspNetIdentity/Pages/Admin/ApiScopes/Edit.cshtml`

## Test Files Created

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/.../Pages/Admin/ApiScopes/EditModelTests.cs` | Unit | 25 |
| `Tests/Integration/.../Pages/Admin/ApiScopes/EditModelIntegrationTests.cs` | Integration | 9 |
| `Tests/E2E/.../Pages/Admin/ApiScopes/EditPageE2eTests.cs` | E2E | 9 |

---

## Page Architecture

The Edit page is a **dual-mode page** — create and edit are handled by the same PageModel:

- **Create mode:** `Id == 0` (route: `/Admin/ApiScopes/0/Edit`)
- **Edit mode:** `Id > 0` (route: `/Admin/ApiScopes/{id}/Edit`)

### Key Properties

| Property | Type | Notes |
|---|---|---|
| `Id` | `int` | `[BindProperty(SupportsGet = true)]` — drives create vs edit mode |
| `IsCreateMode` | `bool` | Computed: `Id == 0` |
| `Input` | `ApiScopeInputModel` | `[BindProperty]` — Name (required), DisplayName, Description, Enabled |
| `AppliedUserClaims` | `IList<string>` | Read-only, populated by `ApplyPageData` |
| `AvailableUserClaims` | `IList<string>` | Read-only, populated by `ApplyPageData` |
| `SelectedClaimType` | `string?` | `[BindProperty]` — for add claim handler |
| `RemoveClaimType` | `string?` | `[BindProperty]` — for remove claim handler |

### Handlers & Code Paths

| Handler | Code Paths |
|---|---|
| `OnGetAsync` | Create mode (return Page), Edit mode (return Page), Edit not found (return NotFound) |
| `OnPostAsync` | Create: invalid ModelState, duplicate name, success. Edit: not found, invalid ModelState, duplicate name, update not found, success |
| `OnPostAddClaimAsync` | Not found, empty claim type, already applied, success |
| `OnPostRemoveClaimAsync` | Not found, empty claim type, not applied, success |

### `ApplyPageData(pageData, mapInput)` — Critical Detail

The private `ApplyPageData` method has a `mapInput` boolean:
- **`mapInput: true`** — overwrites `Input` from the service response (used on GET, and on claim add/remove)
- **`mapInput: false`** — preserves the user's form input (used on POST validation failure re-render)

This is essential for the UX: when a POST fails validation, the user's entered data is preserved in the form rather than being replaced with database values.

---

## Unit Testing Approach (25 tests)

### Setup Pattern

```csharp
private readonly Mock<IApiScopesAdminService> _mockApiScopesAdminService;
private readonly Mock<ITempDataDictionary> _mockTempData;
private readonly EditModel _sut;

public EditModelTests()
{
    _mockApiScopesAdminService = new Mock<IApiScopesAdminService>();
    _mockTempData = new Mock<ITempDataDictionary>();
    _sut = new EditModel(_mockApiScopesAdminService.Object)
    {
        PageContext = new PageContext { HttpContext = new DefaultHttpContext() },
        TempData = _mockTempData.Object
    };
}
```

### Key Testing Decisions

**1. Use specific IDs in mock setups, not `It.IsAny<int>()`**

```csharp
// GOOD — verifies the handler passes the correct Id to the service
_mockApiScopesAdminService
    .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
    .ReturnsAsync((ApiScopeEditPageDataDto?)null);

// BAD — would pass even if the handler accidentally sends Id=0
_mockApiScopesAdminService
    .Setup(x => x.GetForEditAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync((ApiScopeEditPageDataDto?)null);
```

**2. Use `ModelState.AddModelError` for invalid state**

Don't try to set properties to null to trigger validation — validation attributes are only enforced by the model binder in the HTTP pipeline. In unit tests, manipulate `ModelState` directly:

```csharp
_sut.ModelState.AddModelError("Input.Name", "Required");
```

**3. Verify `Times.Never` on invalid paths**

When ModelState is invalid, the service's create/update methods should never be called:

```csharp
_mockApiScopesAdminService.Verify(
    x => x.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()),
    Times.Never);
```

**4. Edit mode null check takes priority over ModelState**

In `OnPostAsync` edit mode, the handler fetches existing data first, then checks for null, then checks ModelState. This ordering matters because both the null and invalid paths need the fetched data. Test that a null return produces `NotFoundResult` even when ModelState is invalid.

### Test Categories

| Category | Tests | What They Verify |
|---|---|---|
| OnGetAsync | 4 | Create mode populates empty Input; edit mode populates from service; not-found returns 404 |
| OnPostAsync (create) | 4 | Invalid ModelState re-renders; duplicate name adds error; success redirects with new ID + TempData |
| OnPostAsync (edit) | 5 | Not found; invalid ModelState re-renders; duplicate name; update not-found; success redirects + TempData |
| OnPostAddClaimAsync | 4 | Not found; empty claim type; already applied; success redirects + TempData |
| OnPostRemoveClaimAsync | 4 | Not found; empty claim type; not applied; success redirects + TempData |
| Theory tests | 4 | `[InlineData(null, "", "   ")]` for `string.IsNullOrWhiteSpace` edge cases on claim types |

---

## Integration Testing Approach (9 tests)

### Setup Pattern

```csharp
public class EditModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditModelIntegrationTests()
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

### Key Testing Decisions

**1. Assert empty form fields on create page**

The create form should render with empty inputs — this verifies that `ApplyPageData(createData, mapInput: true)` doesn't accidentally populate fields with stale data:

```csharp
var nameInput = document.QuerySelector<IHtmlInputElement>("#Input_Name");
nameInput!.Value.Should().BeEmpty();
```

**2. Form submission via AngleSharp**

Use `GetAndParsePage` + `SubmitForm` to exercise the full POST pipeline including model binding and validation:

```csharp
var page = await _client.GetAndParsePage("/Admin/ApiScopes/0/Edit");
var form = page.QuerySelector<IHtmlFormElement>("#edit-api-scope-form");
var response = await _client.SubmitForm(form, new Dictionary<string, string>
{
    ["Input.Name"] = uniqueName,
    ["Input.DisplayName"] = "Test Scope"
});
```

### Test Categories

| Test | What It Uniquely Catches |
|---|---|
| Create form renders with empty fields | `ApplyPageData` mapping bug on create |
| Edit form renders with seeded data | Service-to-page data mapping, EF query correctness |
| Edit form shows applied claims | User claim seeding + join query correctness |
| Not-found returns 404 | Route constraint + service null handling |
| Create POST redirects + success | Full create pipeline: binding → service → redirect |
| Edit POST updates data | Full update pipeline: binding → service → redirect |
| Duplicate name shows validation error | Server-side duplicate check + error message rendering |
| 403 for non-admin | `[Authorize(Roles = "ADMIN")]` enforcement |
| 401 for unauthenticated | Authentication middleware enforcement |

---

## E2E Testing Approach (9 tests)

### Key Lessons Learned

**1. Shadow DOM requires `GetByRole` — not CSS selectors**

The `ck-tabs` web component renders tab header buttons (`<button role="tab">`) inside its Shadow DOM. Standard CSS selectors like `ck-tab-header:has-text('...')` cannot pierce Shadow DOM and will timeout. Playwright's `GetByRole` automatically pierces:

```csharp
// WRONG — times out (Shadow DOM)
await _page.Locator("ck-tab-header:has-text('User Claims')").ClickAsync();

// CORRECT — pierces Shadow DOM
await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();
```

**2. Tab content is hidden until clicked**

Elements inside non-active `ck-tab` panels are not visible in the rendered DOM. Always click the tab before asserting on its content:

```csharp
await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();
// NOW the content is visible
await Expect(_page.Locator("#applied-user-claims-table")).ToBeVisibleAsync();
```

**3. Available claims require user claim seeding**

The "Available User Claims" dropdown is populated from `ApplicationDbContext.UserClaims` (distinct claim types from all users). Without seeding, the dropdown is empty and the `#available-user-claims-select` element doesn't render:

```csharp
var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, "e2e-claimuser");
await TestDataHelper.AddUserClaimAsync(_fixture.Factory, userId, "profile", "test-value");
```

**4. Strict mode violations with broad validation selectors**

Playwright's strict mode throws when a locator resolves to multiple elements. `.text-danger` and `.field-validation-error` spans exist for every form field (even when valid — they have `field-validation-valid` class). Target the specific field:

```csharp
// WRONG — matches 3+ spans, throws strict mode violation
await Expect(_page.Locator(".text-danger, .field-validation-error")).ToBeVisibleAsync();

// CORRECT — targets the Name field's validation span
await Expect(_page.Locator("span[data-valmsg-for='Input.Name'].field-validation-error")).ToBeVisibleAsync();
```

### Test Categories

| Category | Tests | What They Verify |
|---|---|---|
| Create mode | 4 | Empty form, save-before-claims message, create flow (fill → submit → redirect), validation error |
| Edit mode | 2 | Pre-filled form fields, update flow (edit → submit → success message) |
| User claims | 3 | Applied claims display, add claim (with user claim seeding), remove claim |

---

## Conventions Followed

These conventions were detected from existing tests and maintained for consistency:

- **Naming:** `{PageContext}_Should{ExpectedBehaviour}_When{Condition}` (e.g., `CreatePage_ShouldDisplayCreateHeading_AndEmptyForm`)
- **E2E lifecycle:** `IClassFixture<PlaywrightFixture>` + `IAsyncLifetime` for per-test browser context
- **E2E selectors:** `GetByRole` for Shadow DOM elements, `#id` for standard HTML elements, `span[data-valmsg-for='...']` for validation
- **Test data:** `TestDataHelper.SeedApiScopeAsync` with `Guid.NewGuid()` names to prevent collisions across shared fixture
- **Trait:** `[Trait("Category", "E2E")]` on all E2E test classes
- **Section comments:** `// -- Create Mode --`, `// -- Edit Mode --`, `// -- User Claims --` to organize tests

---

## Template: Adding Tests for a New Edit/Create Page

When testing a new admin Razor Page that follows the dual-mode pattern (`Id == 0` → create, `Id > 0` → edit):

### Unit tests (minimum per handler)

**OnGetAsync (3):**
1. `OnGetAsync_ShouldSetIsCreateMode_WhenIdIsZero`
2. `OnGetAsync_ShouldPopulateInput_WhenEditDataExists`
3. `OnGetAsync_ShouldReturnNotFound_WhenEditDataIsNull`

**OnPostAsync create (3):**
1. `OnPostAsync_ShouldReturnPage_WhenModelStateIsInvalid` — verify `CreateAsync` never called
2. `OnPostAsync_ShouldReturnPageWithError_WhenDuplicateName`
3. `OnPostAsync_ShouldRedirectWithNewId_WhenCreateSucceeds`

**OnPostAsync edit (4):**
1. `OnPostAsync_ShouldReturnNotFound_WhenExistingDataIsNull`
2. `OnPostAsync_ShouldReturnPage_WhenModelStateIsInvalid`
3. `OnPostAsync_ShouldReturnPageWithError_WhenDuplicateName`
4. `OnPostAsync_ShouldRedirectWithSuccessMessage_WhenUpdateSucceeds`

### Integration tests (7 minimum)
1. `Get_ShouldRenderCreateForm_WhenIdIsZero` — verify empty form fields
2. `Get_ShouldRenderEditForm_WhenScopeExists` — verify pre-filled fields
3. `Get_ShouldReturnNotFound_WhenScopeDoesNotExist`
4. `Post_ShouldCreateAndRedirect_WhenValid`
5. `Post_ShouldUpdateAndRedirect_WhenValid`
6. `Get_ShouldReturnForbidden_WhenUserIsNotAdmin`
7. `Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated`

### E2E tests (5 minimum)
1. `CreatePage_ShouldDisplayEmptyForm` — verify heading + empty inputs
2. `CreatePage_ShouldCreateAndRedirect` — fill form, submit, verify redirect + success
3. `EditPage_ShouldDisplayExistingData` — seed + navigate, verify pre-filled fields
4. `EditPage_ShouldUpdateAndShowSuccess` — edit field, submit, verify persistence
5. `CreatePage_ShouldShowValidationError_WhenRequired` — submit empty, verify error
