# RolesAdminService — Test Coverage & Findings

## Target File

- **Service:** `src/IdentityServerServices/RolesAdminService.cs`
- **Interface:** `src/IdentityServerServices/IRolesAdminService.cs`

## Dependencies (Constructor-Injected)

| Dependency | Type | Purpose |
|---|---|---|
| `RoleManager<IdentityRole>` | ASP.NET Identity | Role listing, finding roles by ID |
| `UserManager<ApplicationUser>` | ASP.NET Identity | Finding users by ID, adding/removing users from roles, querying all users |

## Test Files

| File | Level | Tests |
|---|---|---|
| `Tests/Unit/IdentityServerServices.UnitTests/RolesAdminServiceTests.cs` | Unit | 11 |
| `Tests/Integration/.../Services/RolesAdminServiceTests.cs` | Integration | 4 |
| `Tests/E2E/.../Pages/Admin/Roles/RolesAdminE2eTests.cs` | E2E | 4 |

---

## Test Level Classification Rules

1. **Pure public logic, data mapping, missing entity checks** — Unit test
   - e.g., validating that role or user exists and returning appropriate status DTOs.
   - e.g., sorting and splitting users into `UsersInRole` and `AvailableUsers`.
2. **EF Core queries, persisting data** — Integration test
   - e.g., ensuring `RemoveUserFromRoleAsync` correctly writes changes to the SQL/EF Core backing store.
3. **UI rendering, HTML forms, and complete navigation paths** — E2E test
   - e.g., an admin clicking the "Remove" button on the Edit Page.

---

## Unit Test Coverage

### RolesAdminServiceTests (11 tests)

#### GetRolesAsync
- `ShouldReturnRolesMappedAndSorted`: Checks mapping to DTOs and sorting by role name.

#### GetRoleForEditAsync
- `ShouldReturnNull_WhenRoleDoesNotExist`: Input validation.
- `ShouldSeparateUsersIntoBuckets`: Evaluates splitting users between `UsersInRole` and `AvailableUsers`, ensuring users in the role are excluded from available users.

#### AddUserToRoleAsync
- `ShouldReturnRoleNotFound_WhenRoleDoesNotExist`: Validation.
- `ShouldReturnUserNotFound_WhenUserDoesNotExist`: Validation.
- `ShouldReturnFailed_WhenManagerFails`: Graceful handling of ASP.NET Identity errors.
- `ShouldReturnSuccess_WhenAddedSuccessfully`: Correct mapping of success DTO.

#### RemoveUserFromRoleAsync
- `ShouldReturnRoleNotFound_WhenRoleDoesNotExist`: Validation.
- `ShouldReturnUserNotFound_WhenUserDoesNotExist`: Validation.
- `ShouldReturnFailed_WhenManagerFails`: Graceful handling of ASP.NET Identity errors.
- `ShouldReturnSuccess_WhenRemovedSuccessfully`: Correct mapping of success DTO.

---

## Integration Test Coverage

### RolesAdminServiceTests (Integration)

| Test | What It Verifies |
|---|---|
| `AddUserToRoleAsync_ShouldReturnSuccess_WhenBothExist` | `AddToRoleAsync` changes are mapped correctly to EF Core. |
| `GetRoleForEditAsync_ShouldCorrectlySplitAvailableAndAssignedUsers_WhenAccessed` | `GetUsersInRoleAsync` properly filters in EF Core. |
| `GetRolesAsync_ShouldReturnRoles_SortedByName` | `Roles` DbSet works and orders correctly. |
| `RemoveUserFromRoleAsync_ShouldReturnSuccess_WhenBothExistAndUserIsInRole` | Verifies `RemoveFromRoleAsync` actually removes the database entry. |

---

## Technical Notes

### Mocking IQueryable with MockQueryable.Moq

To mock Entity Framework's `.ToListAsync()` method (which fails on standard generic lists), we use the `MockQueryable.Moq` library.

```csharp
var roles = new List<IdentityRole>
{
    new IdentityRole { Id = "1", Name = "Apple" }
};
_mockRoleManager.Setup(x => x.Roles).Returns(roles.BuildMock());
```

### Mocking RoleManager and UserManager Constructors

These classes require stores passed into their constructors, otherwise Moq cannot instantiate them. The standard helper pattern used:

```csharp
private static Mock<RoleManager<IdentityRole>> MockRoleManager()
{
    var store = new Mock<IRoleStore<IdentityRole>>();
    return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
}

private static Mock<UserManager<ApplicationUser>> MockUserManager()
{
    var store = new Mock<IUserStore<ApplicationUser>>();
    return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
}
```

---

## Conventions

- **Naming:** `MethodName_ShouldExpectedBehaviour_WhenCondition`
- **Structure:** Constructor injection, `_sut` field for System Under Test
- **Assertions:** FluentAssertions
- **Mocking:** Moq + MockQueryable.Moq for `IQueryable` support
- **Integration lifecycle:** `IClassFixture<CustomWebApplicationFactory>` + `IAsyncLifetime` with per-test state cleanup (`RemoveRange` for EF Core)