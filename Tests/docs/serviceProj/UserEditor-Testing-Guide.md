# UserEditor Service — Test Coverage & Findings

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

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/IdentityServerServices.UnitTests/UserEditorTests.cs` | Unit | 37 |
| `Tests/Unit/IdentityServerServices.UnitTests/UserListItemDtoTests.cs` | Unit | 4 |
| `Tests/Integration/.../Services/UserEditorTests.cs` | Integration | 13 |

---

## Test Level Classification Rules

These rules were applied when deciding where each behaviour should be tested:

1. **Pure public logic, no dependencies** (e.g., `UserListItemDto.LockoutStatus`) — unit test
2. **Orchestration/delegation with mockable dependencies** (e.g., `UpdateUserFromEditPostAsync` branching) — unit test for branching, verify calls are made
3. **EF Core queries or real infrastructure needed** (e.g., `GetUsersAsync`, claims/roles queries) — integration test
4. **Thin wrappers with no logic** (e.g., `UpdateUserProfileAsync`) — covered by the method they delegate to
5. **Private methods** (e.g., `GetAccountStatus`) — covered through public callers, never tested directly

---

## Unit Test Coverage

### UserListItemDtoTests (4 tests)

Tests the `LockoutStatus` computed property — pure logic, three branches plus a boundary case:

| Test | Branch |
|---|---|
| `ShouldReturnDisabled_WhenLockoutEndIsMaxValue` | `DateTimeOffset.MaxValue` |
| `ShouldReturnLockedOut_WhenLockoutEndIsFutureDate` | Future date |
| `ShouldReturnActive_WhenLockoutEndIsNull` | Null |
| `ShouldReturnActive_WhenLockoutEndIsInThePast` | Expired lockout |

### UserEditorTests — UpdateUserFromEditPostAsync (17 tests)

#### Input Validation
- Null, empty, whitespace UserId returns `UserFound = false` with `UserIdMissing` error
- Non-existent user returns `UserFound = false` with `UserNotFound` error

#### Profile Update
- Fields mapped correctly (captured via `Callback`)
- ConcurrencyStamp set when provided
- `UpdateAsync` failure returns error and **skips all downstream operations** (lockout, 2FA, password)

#### Lockout & TwoFactor
- `SetLockoutEnabledAsync` called when `LockoutEnabled.HasValue`
- `SetTwoFactorEnabledAsync` called when `TwoFactorEnabled.HasValue`
- `SetLockoutEnabledAsync` failure returns error and **skips 2FA and password**
- `SetTwoFactorEnabledAsync` failure returns error and **skips password**

#### Password
- Null password skips password flow entirely (`HasPasswordAsync`, `GeneratePasswordResetTokenAsync`, `ResetPasswordAsync`, and `AddPasswordAsync` never called)
- Empty/whitespace password returns `PasswordMissing` error (validated before any DB write)
- Existing password: `GeneratePasswordResetTokenAsync` then `ResetPasswordAsync` (atomic — never removes without valid replacement)
- No existing password: `AddPasswordAsync` only
- `ResetPasswordAsync` failure returns error; `AddPasswordAsync` is never called and existing password is preserved
- `AddPasswordAsync` failure (passwordless user) returns error

#### Full Success
- All operations succeed in sequence

### UserEditorTests — GetUserEditPageDataAsync (12 tests)

#### Input Validation
- Empty UserId returns null
- Non-existent user returns null

#### Conditional Flags (symmetric coverage)

Each flag has both a "called when true" and "skipped when false" test:

| Flag | True (verify called) | False (verify skipped) |
|---|---|---|
| `IncludeUserTabData` | Verifies `GetLoginsAsync`, `HasPasswordAsync`, `GetTwoFactorEnabledAsync`, `GetValidTwoFactorProvidersAsync` | Verifies none called, defaults returned |
| `IncludeClaims` | Verifies `GetClaimsAsync` | Verifies not called, empty lists |
| `IncludeRoles` | Verifies `GetRolesAsync` (requires `MockQueryable.BuildMock()` for `_roleManager.Roles`) | Verifies not called, empty lists |
| `IncludeGrants` | Verifies `GetAllAsync` with correct `SubjectId` filter | Verifies not called, empty list |
| `IncludeSessions` | Verifies `GetSessionsAsync` with correct `SubjectId` filter | Verifies not called, empty list |

#### Null Session Store
- `IServerSideSessionStore` is null — returns empty sessions

### UserEditorTests — GetUserForEditAsync (3 tests)
- Null/empty/whitespace UserId returns null
- Non-existent user returns null
- Existing user returns correctly mapped `UserProfileEditViewModel`

---

## Integration Test Coverage

| Test | What It Verifies |
|---|---|
| `ShouldUpdateProfileFields_WhenValidDataProvided` | Profile update persists to real DB |
| `ShouldRemoveAndAddPassword_WhenNewPasswordRequested` | Password change via `ResetPasswordAsync` — old password rejected, new password accepted |
| `ShouldRetrieveAllData_WhenFlagsAreTrue` | All include flags load data correctly |
| `ShouldExcludeCurrentUserClaims_FromAvailableList` | Claims query filters, deduplicates, and orders correctly |
| `ShouldReturnEmptyAvailableClaims_WhenNoOtherUsersHaveClaims` | Edge case — sole user |
| `ShouldReturnAvailableRoles_ExcludingCurrentRoles` | Roles subtraction logic |
| `ShouldReturnEmptyAvailableRoles_WhenUserHasAllRoles` | Edge case — all roles assigned |
| `ShouldReturnAllUsers_MappedToDto` | `GetUsersAsync` LINQ projection |
| `ShouldReturnDisabled_WhenLockoutEndIsMaxValue` | Account status date logic |
| `ShouldReturnLockedOut_WhenLockoutEndIsFutureDate` | Account status date logic |
| `ShouldMapLockoutEnd_ForDisabledUser` | `GetUsersAsync` lockout status |
| `ShouldMapLockoutEnd_ForLockedOutUser` | `GetUsersAsync` lockout status |
| `ShouldRollbackProfileChanges_WhenPasswordChangeFails` | Profile and security changes are rolled back when a downstream password change fails |

---

## Fixes Applied

### 1. Password Update — Atomic Reset Flow

**File:** `src/IdentityServerServices/UserEditor.cs`

**Previous behaviour:** `RemovePasswordAsync` followed by `AddPasswordAsync` as two separate operations. If `RemovePasswordAsync` succeeded but `AddPasswordAsync` failed (e.g., weak password), the user was left with no password.

**Fix:** Changed to `GeneratePasswordResetTokenAsync` + `ResetPasswordAsync` for users who already have a password. This is a single Identity operation — if the new password fails validation the existing password is never removed.

**Verified by:** Unit test `ShouldResetPassword_WhenUserHasExistingPassword` (verifies `GeneratePasswordResetTokenAsync` and `ResetPasswordAsync` are called; `RemovePasswordAsync` and `AddPasswordAsync` are never called). Unit test `ShouldReturnFailure_WhenResetPasswordFails` (verifies existing password is not removed on failure).

### 2. Atomic Update — Compensation Rollback on Failure

**File:** `src/IdentityServerServices/UserEditor.cs`

**Previous behaviour:** Profile, lockout, 2FA, and password were applied as separate sequential commits. A failure partway through left earlier changes committed while the method returned failure to the caller.

**Fix:** A `UserStateSnapshot` is captured before any writes. If a downstream step fails, earlier changes are rolled back by restoring the snapshot and calling `UpdateAsync` again. On real relational databases an EF Core transaction is used instead; the snapshot path covers the EF InMemory provider (used in tests) which does not support transactions.

**Verified by:** Integration test `ShouldRollbackProfileChanges_WhenPasswordChangeFails` — passes a valid profile update alongside a weak password, asserts the method returns failure, and confirms the original username, email, and password are all preserved.

---

## Technical Notes

### Mocking RoleManager.Roles

`FetchRolesDataAsync` queries `_roleManager.Roles` (an `IQueryable`) and calls `.ToListAsync()`. A plain Moq mock returns null for `Roles`, causing a `NullReferenceException`. Use `MockQueryable.BuildMock()`:

```csharp
_mockRoleManager.Setup(x => x.Roles)
    .Returns(new List<IdentityRole>().BuildMock());
```

The `using MockQueryable;` directive is required. This is already used in `ClaimsAdminServiceTests.cs` for `_userManagerMock.Setup(m => m.Users)`.

### Mocking UserManager Constructor

`UserManager<T>` requires 9 constructor parameters. Use the established pattern:

```csharp
private static Mock<UserManager<ApplicationUser>> MockUserManager()
{
    var store = new Mock<IUserStore<ApplicationUser>>();
    return new Mock<UserManager<ApplicationUser>>(
        store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
}
```

### In-Memory DbContext for Unit Tests

`ApplicationDbContext` is used directly (not via an interface) for the claims query. Unit tests use `UseInMemoryDatabase` with a unique name per test class:

```csharp
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseInMemoryDatabase($"UserEditorTests-{Guid.NewGuid()}")
    .Options;
return new ApplicationDbContext(options);
```

---

## Conventions

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Structure:** Constructor injection, `_sut` field for System Under Test
- **Assertions:** FluentAssertions
- **Mocking:** Moq + MockQueryable.Moq for IQueryable support
- **Integration lifecycle:** `IClassFixture<CustomWebApplicationFactory>` + `IAsyncLifetime` with per-test state cleanup
