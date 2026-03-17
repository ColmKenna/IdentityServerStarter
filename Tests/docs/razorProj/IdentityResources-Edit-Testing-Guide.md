# IdentityResources Edit Page — Testing Lessons & Approach

## Target File

- **Page handler:** `src/IdentityServerAspNetIdentity/Pages/Admin/IdentityResources/Edit.cshtml.cs`
- **Razor view:** `src/IdentityServerAspNetIdentity/Pages/Admin/IdentityResources/Edit.cshtml`

## Test Files Created

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/.../Pages/Admin/IdentityResources/EditModelTests.cs` | Unit | 14 |
| `Tests/Integration/.../Services/IdentityResourcesAdminServiceTests.cs` | Integration | 13 |
| `Tests/E2E/.../Pages/Admin/IdentityResources/EditPageE2eTests.cs` | E2E | 5 |

---

## Page Architecture

The Edit page handles updates and claims management for a specific IdentityResource:

- **Route:** `/Admin/IdentityResources/{id}/Edit`

### Key Properties

| Property | Type | Notes |
|---|---|---|
| `Id` | `int` | `[BindProperty(SupportsGet = true)]` |
| `Input` | `IdentityResourceInputModel` | `[BindProperty]` — Name, DisplayName, Description, Enabled |
| `AppliedUserClaims` | `IList<string>` | Populated from service |
| `AvailableUserClaims` | `IList<string>` | Populated from service (distinct claims in system) |
| `SelectedClaimType` | `string?` | `[BindProperty]` — for adding claims |
| `RemoveClaimType` | `string?` | `[BindProperty]` — for removing claims |

---

## Unit Testing Approach (14 tests)

### Key Testing Decisions

**1. Mock vs Logic**
We mocked the `IIdentityResourcesAdminService` to test the PageModel's **coordination logic** (handling redirects, TempData, and mapping to the service).

**2. Claims Management**
Tested scenarios for:
- Successful addition/removal
- Resource not found
- Claim already applied (for adding)
- Claim not applied (for removal)

**3. Avoiding Redundant Service Unit Tests**
Since `IdentityResourcesAdminService` logic relies on EF Core queries, we opted to skip testing it with `MockQueryable.Moq`. The DB logic and exact control flows (Duplicate Name, Not Found, etc.) are already comprehensively tested in Service Integration Tests. Duplicating these with mocked DbSets would introduce maintenance overhead without catching any additional bugs.

---

## Integration Testing Approach (13 tests)

We chose to implement **Service-level integration tests** rather than Page-level. This verified:
- EF Core query correctness for IdentityResources + UserClaims join.
- Database constraints (e.g., duplicate names).
- Claim lifecycle (adding/removing from the database).

---

## E2E Testing Approach (5 tests)

### Key Lessons Learned

**1. Shadow DOM Awareness**
The `ck-tabs` component uses Shadow DOM. We used `GetByRole(AriaRole.Tab, ...)` to select the "User Claims" tab, as standard CSS selectors would fail.

**2. Tab Visibility**
Content in the "User Claims" tab is hidden until clicked. The E2E tests always click the tab before asserting on the claims table or add-claim form.

**3. Data Seeding**
We used `TestDataHelper.SeedIdentityResourceAsync` and `TestDataHelper.AddUserClaimAsync` to ensure the environment was set up for specific test cases (e.g., verifying that a system claim appears in the "Available" dropdown).

**4. Client-Side Form Validation in E2E**
To prove that clearing a required field correctly shows validation errors without submitting, an E2E test was added. It uses specific attribute selectors (`span[data-valmsg-for='Input.Name'].field-validation-error`) to avoid Playwright strict mode violations.

---

## Conventions Followed

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Infrastructure:** `TestDataHelper` for all seeding.
- **Tools:** Playwright (E2E), FluentAssertions.
