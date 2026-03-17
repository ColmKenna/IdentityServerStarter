# IdentityResourcesAdminService — Testing Lessons & Approach

## Target File

- **Service:** `src/IdentityServerServices/IdentityResourcesAdminService.cs`

## Test Files Created

| File | Level | Tests |
|---|---|---|
| `Tests/Integration/.../Services/IdentityResourcesAdminServiceTests.cs` | Integration | 13 |
| `Tests/Unit/.../IdentityResourcesAdminServiceTests.cs` | Unit | 0 (Deliberately omitted) |

---

## Test Level Decision Framework

### Why No Unit Tests?

The core logic of `IdentityResourcesAdminService` is fundamentally tied to Entity Framework Core query translation and database constraints. The methods perform data retrieval (e.g., `GetIdentityResourcesAsync`), joining and filtering claims against `UserClaims` (`GetForEditAsync`), and manipulating claims collections attached to entities (`AddClaimAsync`, `RemoveClaimAsync`).

While we technically *could* have mocked `ConfigurationDbContext` and `ApplicationDbContext` using `MockQueryable.Moq`, doing so would have introduced significant maintenance overhead while providing **zero additional bug-catching value**. 

The Service Integration Tests already comprehensively exercise every control flow path (`NotFound`, `DuplicateName`, `AlreadyApplied`, `NotApplied`) against a real in-memory SQLite database. A mocked unit test for these same flows would simply be testing that our mocks return the data we told them to return, without proving that EF Core would successfully translate the underlying LINQ queries.

As codified in the `Testing-Reference-Guide.md`: **Avoid Redundant Unit Tests for DB-Heavy Services**.

---

## Integration Testing Approach (13 tests)

### Infrastructure Setup

The tests leverage `IClassFixture<CustomWebApplicationFactory>` alongside `IAsyncLifetime`:

1. **`InitializeAsync`**: 
   - Creates a fresh `IServiceScope` before every test.
   - Clears the `IdentityResources` `DbSet` and calls `SaveChangesAsync()` to guarantee a clean slate for each test method. 
   *(Note: This is critical because `IClassFixture` shares the in-memory database instance across all tests in the class).*
2. **`DisposeAsync`**: 
   - Disposes the `IServiceScope` to clean up tracked EF Core entities and prevent memory leaks.

### Key Behaviours Tested

**1. Data Retrieval (`GetIdentityResourcesAsync`)**
- Validates the mapped `IdentityResourceListItemDto` properties.
- Ensures results are ordered alphabetically by `Name`.

**2. Joining and Filtering (`GetForEditAsync`)**
- Verifies the service can correctly fetch a resource *including* its `UserClaims` child collection.
- Tests the calculation of `AvailableUserClaims` (ensuring that claims already applied to the resource are correctly excluded from the system-wide available claims list).

**3. Updating Entities (`UpdateAsync`)**
- Tests the `NotFound` failure path.
- Tests the duplicate name constraint (ensuring a rename doesn't collide with another existing `IdentityResource.Name`).
- Verifies `.Trim()` logic by appending trailing spaces to the duplicate name check.

**4. Modifying Child Collections (`AddClaimAsync` / `RemoveClaimAsync`)**
- Tests `NotFound` failure paths.
- **AddClaim:** Tests the `AlreadyApplied` flow (prevents duplicate claims on the same resource).
- **RemoveClaim:** Tests the `NotApplied` flow (trying to remove a claim that doesn't exist).
- *Lesson:* To assert the claims were actually modified in the database after the service call, the test re-fetches the entity and explicitly loads the navigation collection: `await _configDbContext.Entry(updated!).Collection(r => r.UserClaims).LoadAsync();`

---

## Conventions Followed

- **Clean State:** Explicitly clearing the tables in `InitializeAsync` rather than relying on unique database names per test class, ensuring test isolation.
- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Assertions:** Leveraging FluentAssertions (`BeOfType`, `Should().ContainSingle()`).
