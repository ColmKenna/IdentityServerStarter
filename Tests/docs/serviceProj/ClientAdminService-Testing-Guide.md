# ClientAdminService — Test Coverage & Findings

## Target File

- **Service:** `src/IdentityServerServices/ClientAdminService.cs`

## Test Files

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/IdentityServerServices.UnitTests/ClientAdminServiceTests.cs` | Unit | 6 |
| `Tests/Integration/.../Services/ClientAdminServiceTests.cs` | Integration | 3 |

---

## Test Level Classification Rules

1. **Exhaustive Mapping & Persistence** — Integration test
   - Verifying that all 18+ scalar properties (e.g., `RequirePkce`, `AccessTokenLifetime`) are correctly saved to the database.
2. **Data Sanitization & Logic** — Unit test
   - Testing `SyncCollection` logic for trimming, filtering whitespace, and handling duplicates case-insensitively.
3. **Identity Map Bypassing** — Integration technique
   - Using `AsNoTracking()` or `ChangeTracker.Clear()` to ensure assertions hit the database rather than EF Core's cache.

---

## Unit Test Coverage

### ClientAdminServiceTests (Unit)

#### GetClientsAsync
- `GetClientsAsync_ShouldReturnMappedDtos_SortedByClientName`: Verifies basic mapping and sorting.

#### GetClientForEditAsync
- `GetClientForEditAsync_ShouldExtractFlattenedCollections`: Verifies collection flattening (Scopes, Grant Types) into ViewModels.
- `GetClientForEditAsync_ShouldReturnNull_WhenClientNotFound`: Basic error handling.

#### UpdateClientAsync
- `UpdateClientAsync_ShouldSanitizeInput_AndHandleDuplicatesCaseInsensitively`: **(New)** Verifies that inputs like `["openid", "  OPENID  "]` are collapsed into a single, lowercase `"openid"`.
- `UpdateClientAsync_ShouldSyncCollectionsProperly_AndAddHashedSecret`: Verifies the standard syncing logic and hashing of new secrets.

---

## Integration Test Coverage

### ClientAdminServiceTests (Integration)

| Test | What It Verifies |
|---|---|
| `UpdateClientAsync_ShouldUpdateAllScalarPropertiesAndSyncCollections` | **(New)** A single, exhaustive test ensuring all 15+ scalar properties and 4 collections are persisted correctly to the DB. |
| `UpdateClientAsync_ShouldAddNewSecret_WhenNewSecretProvided` | Verifies secret hashing and custom descriptions. |
| `UpdateClientAsync_ShouldSyncCollections_WhenListsAreModified` | Verifies complex collection additions/removals in a real DB context. |

---

## Fixes Applied

### 1. Case-Insensitive Collection Syncing

**File:** `src/IdentityServerServices/ClientAdminService.cs`

**Previous behaviour:** `SyncCollection` used `StringComparer.Ordinal`, causing `"openid"` and `"OPENID"` to be treated as unique entries, potentially leading to database errors or duplicate UI elements.

**Fix:** Switched to `StringComparer.OrdinalIgnoreCase` and added `.ToLowerInvariant()` during the projection phase.

**Verified by:** Unit test `UpdateClientAsync_ShouldSanitizeInput_AndHandleDuplicatesCaseInsensitively`.

---

## Technical Notes

### EF Core Identity Map Pitfall

When asserting in an integration test, if you use the same `DbContext` for **Act** and **Assert**, EF Core might return the object from its memory (Identity Map) without querying the database.

**Solution:** Use `_configDbContext.ChangeTracker.Clear()` or chain `.AsNoTracking()` to your query to force a fresh read from the disk.

```csharp
_configDbContext.ChangeTracker.Clear();
var updated = await _configDbContext.Clients.AsNoTracking().SingleAsync(c => c.Id == id);
```

### Secret Description Fallback

The logic `viewModel.NewSecretDescription ?? "Added via admin"` only triggers for `null`. If a user provides an empty string or whitespace, that whitespace is currently preserved. This was noted but left as-is to maintain current behavior.
