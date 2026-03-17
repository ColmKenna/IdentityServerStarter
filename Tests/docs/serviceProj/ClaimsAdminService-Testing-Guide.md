# ClaimsAdminService — Test Coverage & Findings

## Target File

- **Service:** `src/IdentityServerServices/ClaimsAdminService.cs`
- **Interface:** `src/IdentityServerServices/IClaimsAdminService.cs`

## Dependencies (Constructor-Injected)

| Dependency | Type | Purpose |
|---|---|---|
| `ApplicationDbContext` | EF Core | Querying `UserClaims` table directly for claim types, assignments, and existence checks |
| `UserManager<ApplicationUser>` | ASP.NET Identity | Finding users by ID, adding/removing claims, querying all users |

## Test Files

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/IdentityServerServices.UnitTests/ClaimsAdminServiceTests.cs` | Unit | 15 |
| `Tests/Integration/.../Services/ClaimsAdminServiceTests.cs` | Integration | 3 |

---

## Test Level Classification

1. **Pure logic, data mapping, status result paths** — Unit test
   - e.g., returning `UserNotFound`, `AlreadyAssigned`, `IdentityFailure` statuses.
   - e.g., `IsLastUserAssignment` calculation, `ShouldDefaultNewClaimValue` boolean detection.
2. **Real UserManager interacting with the database** — Integration test
   - e.g., verifying `UserManager.RemoveClaimAsync` actually deletes the DB row and `HasRemainingAssignments` reads correctly afterward.

---

## Unit Test Coverage (15 tests)

### GetClaimsAsync
- `ShouldReturnDistinctSortedClaims`: Verifies distinct claim types, alphabetical ordering, and filtering of null claim types.

### GetForEditAsync
- `ShouldReturnNull_WhenNoUsersHaveClaim`: Returns null when no assignments exist for the claim type.
- `ShouldSetIsLastUserAssignmentCorrectly`: Multiple users with varying assignment counts — verifies `IsLastUserAssignment` is false when more than one unique user exists.
- `ShouldSetIsLastUserAssignmentTrue_WhenOnlyOneUserWithOneAssignment`: Single user, single assignment — the only case where `IsLastUserAssignment` is true.
- `ShouldDefaultNewClaimValueToTrueString_WhenAllValuesAreBooleans`: When all existing claim values parse as booleans, `NewClaimValue` defaults to `"true"`.
- `ShouldPassThroughNewClaimValue_WhenExplicitlyProvided`: When `currentNewClaimValue` is non-whitespace, it passes through unchanged.
- `ShouldNotDefaultNewClaimValue_WhenValuesAreNotAllBooleans`: When claim values are non-boolean strings (e.g., `"admin"`), `NewClaimValue` stays null.

### AddUserToClaimAsync
- `ShouldReturnUserNotFound`: User ID not found via `FindByIdAsync`.
- `ShouldReturnAlreadyAssigned`: Claim assignment already exists in the database.
- `ShouldReturnIdentityFailure_IfUserManagerFails`: `AddClaimAsync` returns `IdentityResult.Failed` — error descriptions are captured.
- `ShouldReturnSuccess_WhenValid`: Successful add — verifies claim type/value passed to `UserManager` and `UserName` mapped in result.

### RemoveUserFromClaimAsync
- `ShouldReturnUserNotFound`: User ID not found.
- `ShouldReturnAssignmentNotFound`: No matching claim assignment in the database.
- `ShouldReturnIdentityFailure_IfUserManagerFails`: `RemoveClaimAsync` returns failed result — error descriptions captured.
- `ShouldReturnHasRemainingAssignmentsFalse_WhenLastAssignmentRemoved`: Last assignment for the claim type removed — uses mock `Callback` to simulate DB deletion so `HasRemainingAssignments` reads correctly.
- `ShouldDetermineHasRemainingAssignments`: Another user still holds the claim type — `HasRemainingAssignments` is true.

---

## Integration Test Coverage (3 tests)

| Test | What It Verifies |
|---|---|
| `AddUserToClaimAsync_ShouldAddClaim_WhenUserExists` | Real `UserManager.AddClaimAsync` persists the claim to the database. Verified via `GetClaimsAsync`. |
| `GetForEditAsync_ShouldFlagLastAssignment_WhenOnlyOneUserHasClaim` | `IsLastUserAssignment` works correctly with real `UserManager.Users` and real `UserClaims` table. |
| `RemoveUserFromClaimAsync_ShouldRemoveClaimAndReportRemainingAssignments` | Real `UserManager.RemoveClaimAsync` deletes the DB row. `HasRemainingAssignments` correctly reads true when another user still holds the claim. Verified via `GetClaimsAsync` that the removed user no longer has the claim. |

---

## Technical Notes

### Why Both Unit and Integration Tests?

Unlike `IdentityResourcesAdminService` (which is purely DB-driven and only needs integration tests), `ClaimsAdminService` has two characteristics that justify unit tests:

1. **Non-trivial in-memory logic** — `SetIsLastUserAssignment` and `ShouldDefaultNewClaimValue` contain branching logic that is faster and easier to cover exhaustively at the unit level.
2. **Mocked UserManager requires a Callback hack** — The `HasRemainingAssignments` check after `RemoveClaimAsync` queries the database. Since the mocked `UserManager` doesn't actually modify the DB, the unit test uses a `Callback` to manually delete the row. The integration test verifies this flow naturally with the real `UserManager`.

### SQLite In-Memory for Unit Tests

Unit tests use per-test SQLite databases (unique GUID in connection string) for the `ApplicationDbContext`, giving real EF Core query translation without cross-test contamination. `UserManager` is mocked because it requires the full ASP.NET Identity pipeline to function.

---

## Conventions

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Structure:** Constructor injection with `_userManagerMock` field; `CreateSut` factory method
- **Assertions:** FluentAssertions
- **Mocking:** Moq for `UserManager`; MockQueryable.Moq for `Users` IQueryable; real SQLite for `ApplicationDbContext`
- **Integration lifecycle:** `IClassFixture<CustomWebApplicationFactory>` + `IAsyncLifetime` with per-test state cleanup
