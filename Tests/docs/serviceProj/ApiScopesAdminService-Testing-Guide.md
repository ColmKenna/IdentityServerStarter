# ApiScopesAdminService — Test Coverage & Findings

## Target File

- **Service:** `src/IdentityServerServices/ApiScopesAdminService.cs`
- **Interface:** `src/IdentityServerServices/IApiScopesAdminService.cs`

## Dependencies (Constructor-Injected)

| Dependency | Type | Purpose |
|---|---|---|
| `ConfigurationDbContext` | Duende IdentityServer EF | CRUD operations on `ApiScopes` and their `UserClaims` child collections |
| `ApplicationDbContext` | EF Core | Querying all distinct user claim types for the "available claims" dropdown |

## Test Files

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/IdentityServerServices.UnitTests/ApiScopesAdminServiceTests.cs` | Unit | 19 |
| `Tests/Integration/.../Services/ApiScopesAdminServiceTests.cs` | Integration | 4 |

---

## Test Level Classification

1. **All service methods** — Unit test (primary coverage)
   - Both `ConfigurationDbContext` and `ApplicationDbContext` run against real SQLite in-memory databases in unit tests, giving real EF Core query translation.
   - No `UserManager` dependency — no mocking gap to bridge.
2. **DI wiring and full app configuration** — Integration test
   - The existing 4 integration tests verify that the service resolves correctly from the DI container and operates against the shared in-memory database.

---

## Unit Test Coverage (19 tests)

### GetApiScopesAsync
- `ShouldReturnScopesOrderedByName`: Verifies alphabetical ordering and DTO field mapping (Name, DisplayName, Enabled).

### GetForCreateAsync
- `ShouldReturnEmptyInputWithAvailableClaims`: Empty input with `Enabled = true` default; available claims populated from `ApplicationDbContext.UserClaims`.

### GetForEditAsync
- `ShouldReturnNull_WhenScopeDoesNotExist`: Non-existent ID returns null.
- `ShouldReturnAvailableClaims_ExcludingAppliedClaims`: Claims already on the scope are excluded from available claims. Verifies Input DTO mapping (Name, DisplayName, Enabled).

### CreateAsync
- `ShouldReturnSuccess_WhenNameIsUnique`: Persists entity with trimmed name. Verifies `ShowInDiscoveryDocument = true` default.
- `ShouldReturnDuplicateName_WhenNameAlreadyExists`: Duplicate detection works with trimmed input.
- `ShouldNullifyWhitespaceOnlyOptionalFields`: Whitespace-only `DisplayName` and `Description` become null via `NormalizeOptional`.

### UpdateAsync
- `ShouldReturnNotFound_WhenScopeDoesNotExist`: Non-existent ID returns `NotFound`.
- `ShouldReturnDuplicateName_WhenAnotherScopeHasSameName`: Duplicate name check excludes the scope's own ID (trimmed input).
- `ShouldUpdateFields_WhenValid`: All fields updated correctly (Name, DisplayName, Description, Enabled).
- `ShouldAllowSameName_WhenItBelongsToSameScope`: Keeping the same name on update does not trigger duplicate name error (verifies `scope.Id != id` exclusion).
- `ShouldNullifyWhitespaceOnlyOptionalFields`: Whitespace-only optional fields cleared to null on update.

### AddClaimAsync
- `ShouldReturnNotFound_WhenScopeDoesNotExist`: Non-existent scope ID.
- `ShouldReturnAlreadyApplied_WhenClaimExistsOnScope`: Duplicate claim detection with trimmed input.
- `ShouldAddClaim_WhenNotAlreadyApplied`: Claim added and persisted. Whitespace trimmed from claim type. Verified by reloading `UserClaims` collection.

### RemoveClaimAsync
- `ShouldReturnNotFound_WhenScopeDoesNotExist`: Non-existent scope ID.
- `ShouldReturnNotApplied_WhenClaimDoesNotExistOnScope`: Claim type not present on scope.
- `ShouldRemoveClaim_WhenApplied`: Claim removed, other claims preserved. Whitespace trimmed. Verified by reloading `UserClaims` collection.

---

## Integration Test Coverage (4 tests)

| Test | What It Verifies |
|---|---|
| `CreateAsync_ShouldPersistScope_WhenNameIsUnique` | DI-resolved service persists a new scope. Verified by direct `DbContext.FindAsync`. |
| `CreateAsync_ShouldReturnDuplicateName_WhenNameAlreadyExists` | Duplicate name detection with trailing space (trim logic). |
| `UpdateAsync_ShouldUpdateEntity_WhenIdExistsAndNameUnique` | Field updates persisted correctly through the DI-resolved service. |
| `AddClaimAsync_ShouldAddNewClaim_WhenNotAlreadyApplied` | Claim addition persisted. Verified by reloading the `UserClaims` collection. |

---

## Technical Notes

### Dual DbContext Setup

Unit tests create both contexts independently with unique SQLite connection strings:

```csharp
private static ConfigurationDbContext CreateConfigurationDbContext()
{
    var storeOptions = new ConfigurationStoreOptions();
    var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
        .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
        .Options;

    var context = new ConfigurationDbContext(options);
    context.StoreOptions = storeOptions;  // Required by Duende's ConfigurationDbContext
    context.Database.OpenConnection();
    context.Database.EnsureCreated();
    return context;
}
```

The `ConfigurationStoreOptions` assignment is required — without it, `ConfigurationDbContext` throws during schema creation.

### Why Unit Tests Exist Despite No Mocking Gap

Unlike `IdentityResourcesAdminService` (which uses the same dual-context pattern and was tested only at the integration level), `ApiScopesAdminService` has unit tests because:

1. **More methods to cover** — 7 public methods vs 5, with more branching (two overloads of `DuplicateNameExists`, `NormalizeOptional` logic).
2. **`NormalizeOptional` edge cases** — Whitespace-only strings becoming null is easy to miss and fast to verify at the unit level.
3. **Same-ID duplicate name exclusion** — The `UpdateAsync` path that allows keeping the same name requires verifying the `scope.Id != id` condition, which is a subtle correctness check.

### Reloading Navigation Collections After Mutation

After `AddClaimAsync` or `RemoveClaimAsync`, the `UserClaims` collection on the tracked entity may be stale. Tests reload it explicitly:

```csharp
await configDb.Entry(scope).Collection(s => s.UserClaims).LoadAsync();
scope.UserClaims.Should().ContainSingle(c => c.Type == "email");
```

### SQLite FK Constraints on UserClaims

When seeding `IdentityUserClaim<string>` rows in `ApplicationDbContext`, a `UserId` and corresponding `Users` row must exist — SQLite enforces foreign key constraints. Tests that need user claims must seed a user first:

```csharp
var testUser = new ApplicationUser { Id = "u1", UserName = "test_user1", Email = "test1@example.com" };
appDb.Users.Add(testUser);
appDb.UserClaims.Add(new IdentityUserClaim<string> { UserId = "u1", ClaimType = "role", ClaimValue = "admin" });
```

---

## Conventions

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Structure:** Static factory methods (`CreateConfigurationDbContext`, `CreateApplicationDbContext`, `CreateSut`); no constructor fields since no mocks are needed
- **Assertions:** FluentAssertions
- **Database:** Real SQLite in-memory for both contexts in unit tests
- **Integration lifecycle:** `IClassFixture<CustomWebApplicationFactory>` + `IAsyncLifetime` with per-test state cleanup (`RemoveRange` on `ApiScopes`)
