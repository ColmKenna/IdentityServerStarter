# UserEditor Service — Testing Lessons & Approach

## Target File

- **Service:** `src/IdentityServerServices/UserEditor.cs`
- **Interface:** `src/IdentityServerServices/IUserEditor.cs`

## Dependencies (Constructor-Injected)

| Dependency | Type | Purpose |
|---|---|---|
| `UserManager<ApplicationUser>` | ASP.NET Identity | User CRUD, password, lockout, 2FA, claims, roles |
| `RoleManager<IdentityRole>` | ASP.NET Identity | Role listing |
| `IPersistedGrantStore` | Duende IdentityServer | OAuth/OIDC token grants |
| `ApplicationDbContext` | EF Core | Direct query for available claims |
| `IServerSideSessionStore?` | Duende IdentityServer | Optional — server-side sessions |

## Test Files

| File | Level | Tests | Status |
|---|---|---|---|
| `Tests/Integration/.../Services/UserEditorTests.cs` | Integration | 11 | Exists — covers profile update, password change, data retrieval, claims/roles queries, account status logic, user listing |
| `Tests/Unit/.../Pages/Admin/UsersEditModelTests.cs` | Unit (page) | ~45 | Exists — tests the EditModel page that *consumes* UserEditor |
| `Tests/E2E/.../Pages/Admin/Users/UsersAdminE2eTests.cs` | E2E | 9 | Exists — covers Index navigation, Profile view/update, Claims add/remove, Roles add/remove, Security disable/enable |
| *(No file)* | Unit (service) | 0 | **Gap** — no dedicated unit tests for UserEditor itself |

---

## Test Level Decision Framework

### What belongs where for UserEditor

| Behaviour | Test Level | Reasoning |
|---|---|---|
| `UpdateUserFromEditPostAsync` — null/empty UserId returns failure | **Unit** | Pure input validation, no I/O needed |
| `UpdateUserFromEditPostAsync` — user not found returns failure | **Unit** | Mock `UserManager.FindByIdAsync` to return null, assert result |
| `UpdateUserFromEditPostAsync` — profile fields mapped correctly | **Unit** | Verify the service sets `user.UserName`, `user.Email`, etc. before calling `UpdateAsync` |
| `UpdateUserFromEditPostAsync` — password remove + add sequence | **Unit** | Verify the correct sequence: `HasPasswordAsync` → `RemovePasswordAsync` → `AddPasswordAsync` |
| `UpdateUserFromEditPostAsync` — `IdentityResult.Failed` propagation | **Unit** | Mock `UpdateAsync` to return failure, verify it short-circuits |
| `GetUserEditPageDataAsync` — conditional data loading via flags | **Unit** | Verify that setting `IncludeClaims = false` skips the claims fetch |
| `GetAccountStatus` — date logic for Active/Disabled/Locked Out | **Unit** | Pure logic, no dependencies — but it's `private static`, so test via public methods |
| `FetchClaimsDataAsync` — available claims query | **Integration** | Uses `_dbContext.UserClaims` with LINQ-to-SQL — mock can't verify query correctness |
| `GetUsersAsync` — LINQ projection to DTO | **Integration** | Uses `_userManager.Users` (EF Core queryable) — mock can't verify query translation |
| `FetchSessionsAsync` — null session store handling | **Unit** | Verify graceful `[]` return when `_sessionStore` is null |

### Key decision: why both unit AND integration tests?

`UserEditor` has two kinds of logic interleaved:

1. **Orchestration logic** — input validation, conditional branching, mapping fields, calling UserManager methods in the right order, short-circuiting on failure. This is best tested at the **unit** level with mocked dependencies because:
   - Tests are fast and deterministic
   - You can force specific failure paths (e.g., `UpdateAsync` fails but `RemovePasswordAsync` succeeded — what happens?)
   - You can verify call sequences and captured arguments

2. **Query logic** — `GetUsersAsync` projects via EF Core, `FetchClaimsDataAsync` queries `_dbContext.UserClaims` with filtering. This is best tested at the **integration** level because:
   - Mocked queryables can't catch LINQ-to-SQL translation failures
   - The real query might return different results than the in-memory mock

The existing integration tests cover category 2 but barely touch category 1. The existing **page-level** unit tests (`UsersEditModelTests.cs`) test the EditModel page handler, which mocks `IUserEditor` entirely — so they never exercise UserEditor's internal branching.

---

## Existing Integration Test Coverage

The 3 integration tests in `Services/UserEditorTests.cs` cover:

1. **`UpdateUserFromEditPostAsync_ShouldUpdateProfileFields_WhenValidDataProvided`** — happy path profile update (username, email, confirmation flags, phone number)
2. **`UpdateUserFromEditPostAsync_ShouldRemoveAndAddPassword_WhenNewPasswordRequested`** — password change via the remove-then-add sequence
3. **`GetUserEditPageDataAsync_ShouldRetrieveAllData_WhenFlagsAreTrue`** — data retrieval with all include flags enabled

### What's NOT covered (gaps)

**Unit test gaps (no service-level unit tests exist):**
- Input validation (null/empty UserId, null profile)
- User not found path
- `IdentityResult.Failed` propagation from any UserManager call
- Password update when user has no existing password (skips `RemovePasswordAsync`)
- Empty password string vs null password handling
- Lockout toggle (`request.LockoutEnabled.HasValue` branch)
- 2FA toggle (`request.TwoFactorEnabled.HasValue` branch)
- Concurrency stamp assignment
- `GetAccountStatus` logic (Disabled when `LockoutEnd == MaxValue`, Locked Out when future date, Active otherwise)
- `FetchSessionsAsync` when `_sessionStore` is null → returns `[]`
- `GetUserForEditAsync` with null/empty userId → returns null
- `GetUserEditPageDataAsync` with missing user → returns null
- Conditional flag behaviour (e.g., `IncludeClaims = false` should NOT call `GetClaimsAsync`)

**Integration test gaps:**
- Claims fetching — the "available claims" query (`_dbContext.UserClaims` with filtering, deduplication, ordering)
- Roles fetching — the "available roles" computed by subtracting current roles from all roles
- Grants and sessions fetching
- `GetUsersAsync` — the LINQ projection to `UserListItemDto`
- Error paths through the real pipeline
- `UserListItemDto.LockoutStatus` computed property

---

## Lessons Learned

### 1. Mocking UserManager is non-trivial

`UserManager<T>` has a complex constructor with 9 parameters. Use the shared helper rather than constructing it manually:

```csharp
var mockUserManager = TestHelpers.CreateMockUserManager();
```

This sets up the backing `IUserStore<T>` and adds default validators. Without it, validator-dependent methods behave unexpectedly.

### 2. UserEditor has two failure-result patterns — test both

The service distinguishes between "user not found" and "operation failed":

```csharp
// User missing — UserFound = false
return new UserProfileUpdateResult
{
    UserFound = false,
    Result = IdentityResult.Failed(new IdentityError { Code = "UserNotFound", ... })
};

// Operation failed — UserFound = true, Result has errors
return new UserProfileUpdateResult
{
    UserFound = true,
    Result = IdentityResult.Failed(...)
};
```

The page handler (`EditModel`) treats these differently — `UserFound = false` → `NotFound()`, `UserFound = true` with errors → re-render page with validation messages. Unit tests for UserEditor should verify both result shapes.

### 3. UpdateUserFromEditPostAsync has a cascading failure pattern

The method applies multiple updates in sequence. Each step uses `FailIfFailed()` to short-circuit:

```csharp
if (FailIfFailed(await _userManager.UpdateAsync(user)) is { } updateFailure) return updateFailure;
// ...
if (FailIfFailed(await _userManager.RemovePasswordAsync(user)) is { } removeFailure) return removeFailure;
// ...
if (FailIfFailed(await _userManager.AddPasswordAsync(user, request.NewPassword)) is { } addFailure) return addFailure;
```

A unit test should verify: if `RemovePasswordAsync` succeeds but `AddPasswordAsync` fails, the failure result is returned (and the user is left without a password — a real concern worth documenting).

### 4. Conditional data loading is a testable contract

`GetUserEditPageDataAsync` respects boolean flags to skip expensive fetches:

```csharp
var (claims, availableClaims) =
    request.IncludeClaims
        ? await FetchClaimsDataAsync(user, ct)
        : (new List<Claim>(), new List<string>());
```

Unit tests should verify that when `IncludeClaims = false`, the UserManager's `GetClaimsAsync` is **never called** (use `mock.Verify(..., Times.Never)`). This is a performance contract the page handler depends on.

### 5. GetAccountStatus has three states driven by date logic

```csharp
if (lockoutEnd == DateTimeOffset.MaxValue) → "Disabled"
if (lockoutEnd > DateTimeOffset.UtcNow)   → "Locked Out (until {date})"
else                                       → "Active"
```

This is `private static`, so it can only be tested through `GetUserEditPageDataAsync` or `FetchUserTabDataAsync`. Set `user.LockoutEnd` to different values and verify the `AccountStatus` field in the returned DTO.

**Note:** `UserListItemDto.LockoutStatus` has the same three-state logic as a computed property — but uses different return values ("Locked Out" vs "Locked Out (until ...)"). These are independently testable since `LockoutStatus` is a public getter on the DTO.

### 6. The optional dependency pattern needs explicit testing

`IServerSideSessionStore?` is nullable. When null:
- `FetchSessionsAsync` returns `[]`
- The EditModel page handler's session operations are no-ops

Unit tests should cover both paths: with and without the session store injected.

### 7. Available claims query has subtle filtering logic

`FetchClaimsDataAsync` computes "available claims" by:
1. Getting the current user's claim types
2. Querying `_dbContext.UserClaims` for claims belonging to OTHER users
3. Excluding the current user's claim types
4. Deduplicating with `.Distinct()`
5. Ordering alphabetically

This query logic is best tested at the **integration** level with a real (in-memory) database. A mock of `ApplicationDbContext` wouldn't verify the LINQ translation.

---

## E2E Test Coverage

The E2E tests in `Tests/E2E/.../Pages/Admin/Users/UsersAdminE2eTests.cs` cover 9 user journeys:

| Test | Tab | What it verifies |
|---|---|---|
| `IndexPage_ShouldDisplayUsersAndNavigateToEdit` | Index | User appears in list, clicking navigates to Edit page |
| `ProfileTab_ShouldDisplayPrePopulatedFields` | Profile | Username and email fields pre-populated with correct values |
| `ProfileTab_ShouldUpdateFieldsAndShowSuccess` | Profile | Updating username/email saves and shows success message |
| `ClaimsTab_ShouldAddClaimViaModalAndDisplayInList` | Claims | Add claim via modal dialog, claim appears in table |
| `ClaimsTab_ShouldRemoveSelectedClaimAndUpdateList` | Claims | Select and remove claim via confirmation dialog |
| `RolesTab_ShouldAddRoleAndDisplayInCurrentRoles` | Roles | Check available role and add, role appears in assigned list |
| `RolesTab_ShouldRemoveRoleAndUpdateList` | Roles | Select and remove role via confirmation dialog |
| `SecurityTab_ShouldDisableAccountAndShowDisabledStatus` | Security | Disable account via confirmation dialog, status shows "Disabled" |
| `SecurityTab_ShouldEnableDisabledAccountAndShowActiveStatus` | Security | Enable previously disabled account, status shows "Active" |

### E2E-Specific Lessons for User Edit Page

1. **Confirmation dialogs:** The Remove Claims (`#remove-selected-btn`), Remove Roles (`#remove-roles-btn`), and Disable Account (`#disable-account-btn`) buttons all use `confirmAction()` from `confirmModal.js`. Tests must click the button, wait for `#confirm-modal` to be visible, then click `#confirm-modal-confirm`. See the `ClickAndConfirmAsync` helper.

2. **Profile Save bypasses validation via JavaScript:** The Profile Save button lacks `formnovalidate`, and the Security tab has a `required` password field in the same `<form>`. E2E tests use `form.noValidate = true` + `form.requestSubmit()` to submit.

3. **Enable Account has NO confirmation dialog:** Unlike Disable, the Enable button (`#enable-account-btn`) submits directly — no `ClickAndConfirmAsync` needed.

4. **Strict mode on Index page:** The username appears in both an `<a>` and a `<span>`. Use `GetByRole(AriaRole.Link, new() { Name = actualUsername })` to target the link specifically.

5. **Empty-state assertions:** After removing the last claim or role, assert on `#no-claims-message` or `#no-roles-message` visibility rather than checking the table/list doesn't contain the item (the table element may be removed entirely).

---

## Conventions Followed

These match the project-wide conventions (see `Tests/docs/Testing-Reference-Guide.md`):

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Structure:** Constructor injection, `_sut` field for System Under Test
- **Assertions:** FluentAssertions
- **Mocking:** Moq — use `TestHelpers.CreateMockUserManager()` for UserManager
- **Integration lifecycle:** `IClassFixture<CustomWebApplicationFactory>` + `IAsyncLifetime` with per-test state cleanup
- **Test data:** Use `TestDataHelper.CreateTestUserAsync()` and `TestDataHelper.AddUserClaimAsync()` for seeding

---

## Template: Unit Tests for UserEditor

When writing unit tests for `UserEditor`, mock all five dependencies:

```csharp
public class UserEditorTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<IPersistedGrantStore> _mockGrantStore;
    private readonly Mock<IServerSideSessionStore> _mockSessionStore;
    private readonly Mock<ApplicationDbContext> _mockDbContext;  // or use InMemory
    private readonly UserEditor _sut;

    public UserEditorTests()
    {
        _mockUserManager = TestHelpers.CreateMockUserManager();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(), null!, null!, null!, null!);
        _mockGrantStore = new Mock<IPersistedGrantStore>();
        _mockSessionStore = new Mock<IServerSideSessionStore>();
        // Note: ApplicationDbContext needs special handling — see lesson 7
        _sut = new UserEditor(
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockGrantStore.Object,
            /* dbContext */ ...,
            _mockSessionStore.Object);
    }
}
```

### Recommended unit tests (minimum)

#### UpdateUserFromEditPostAsync
1. `UpdateUserFromEditPostAsync_ShouldReturnUserMissing_WhenUserIdIsNull`
2. `UpdateUserFromEditPostAsync_ShouldReturnUserMissing_WhenUserIdIsWhitespace`
3. `UpdateUserFromEditPostAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist`
4. `UpdateUserFromEditPostAsync_ShouldUpdateProfileFields_WhenProfileProvided`
5. `UpdateUserFromEditPostAsync_ShouldSetConcurrencyStamp_WhenProvided`
6. `UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenUpdateAsyncFails`
7. `UpdateUserFromEditPostAsync_ShouldSkipPasswordChange_WhenNewPasswordIsNull`
8. `UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenNewPasswordIsEmpty`
9. `UpdateUserFromEditPostAsync_ShouldRemoveAndAddPassword_WhenUserHasExistingPassword`
10. `UpdateUserFromEditPostAsync_ShouldAddPasswordDirectly_WhenUserHasNoPassword`
11. `UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenRemovePasswordFails`
12. `UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenAddPasswordFails`
13. `UpdateUserFromEditPostAsync_ShouldSetLockout_WhenLockoutEnabledHasValue`
14. `UpdateUserFromEditPostAsync_ShouldSetTwoFactor_WhenTwoFactorEnabledHasValue`
15. `UpdateUserFromEditPostAsync_ShouldReturnSuccess_WhenAllUpdatesSucceed`

#### GetUserEditPageDataAsync
16. `GetUserEditPageDataAsync_ShouldReturnNull_WhenUserIdIsEmpty`
17. `GetUserEditPageDataAsync_ShouldReturnNull_WhenUserNotFound`
18. `GetUserEditPageDataAsync_ShouldSkipClaimsFetch_WhenIncludeClaimsIsFalse`
19. `GetUserEditPageDataAsync_ShouldSkipRolesFetch_WhenIncludeRolesIsFalse`
20. `GetUserEditPageDataAsync_ShouldReturnEmptySessions_WhenSessionStoreIsNull`

#### GetUserForEditAsync
21. `GetUserForEditAsync_ShouldReturnNull_WhenUserIdIsEmpty`
22. `GetUserForEditAsync_ShouldReturnNull_WhenUserNotFound`
23. `GetUserForEditAsync_ShouldReturnMappedViewModel_WhenUserExists`

### Recommended additional integration tests

#### Query logic (requires real database)
1. `GetUsersAsync_ShouldReturnAllUsers_MappedToDto`
2. `FetchClaimsDataAsync_ShouldExcludeCurrentUserClaims_FromAvailableList`
3. `FetchClaimsDataAsync_ShouldDeduplicateAndOrderAvailableClaims`
4. `FetchRolesDataAsync_ShouldReturnAvailableRoles_ExcludingCurrentRoles`
5. `GetUserEditPageDataAsync_ShouldReturnCorrectAccountStatus_WhenDisabled`
6. `GetUserEditPageDataAsync_ShouldReturnCorrectAccountStatus_WhenLockedOut`
