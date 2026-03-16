using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace IdentityServerAspNetIdentity.IntegrationTests.Services;

public class UserEditorTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ApplicationDbContext _appDbContext = default!;
    private UserManager<ApplicationUser> _userManager = default!;
    private IUserEditor _sut = default!;

    public UserEditorTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private RoleManager<IdentityRole> _roleManager = default!;

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _appDbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        _sut = _scope.ServiceProvider.GetRequiredService<IUserEditor>();

        _appDbContext.Users.RemoveRange(_appDbContext.Users);
        _appDbContext.UserClaims.RemoveRange(_appDbContext.UserClaims);
        _appDbContext.Roles.RemoveRange(_appDbContext.Roles);
        _appDbContext.UserRoles.RemoveRange(_appDbContext.UserRoles);
        await _appDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldUpdateProfileFields_WhenValidDataProvided()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "oldname", Email = "old@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            Profile = new UserProfileEditViewModel
            {
                UserId = user.Id,
                Username = "newname",
                Email = "new@test.com",
                EmailConfirmed = true,
                PhoneNumber = "12345",
                PhoneNumberConfirmed = false
            }
        };

        // Act
        var result = await _sut.UpdateUserFromEditPostAsync(request);

        // Assert
        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();

        var updatedUser = await _userManager.FindByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.UserName.Should().Be("newname");
        updatedUser.Email.Should().Be("new@test.com");
        updatedUser.EmailConfirmed.Should().BeTrue();
        updatedUser.PhoneNumber.Should().Be("12345");
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldRemoveAndAddPassword_WhenNewPasswordRequested()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "pwduser", Email = "p@test.com" };
        await _userManager.CreateAsync(user, "OldPass123$");

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            NewPassword = "NewPass123$"
        };

        // Act
        var result = await _sut.UpdateUserFromEditPostAsync(request);

        // Assert
        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();

        var pwdCheckOld = await _userManager.CheckPasswordAsync(user, "OldPass123$");
        var pwdCheckNew = await _userManager.CheckPasswordAsync(user, "NewPass123$");
        
        pwdCheckOld.Should().BeFalse();
        pwdCheckNew.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldRetrieveAllData_WhenFlagsAreTrue()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "datauser", Email = "d@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.AddClaimAsync(user, new Claim("TestClaim", "DataVal"));
        
        var request = new UserEditPageDataRequest
        {
            UserId = user.Id,
            IncludeUserTabData = true,
            IncludeClaims = true,
            IncludeRoles = true,
            IncludeGrants = true,
            IncludeSessions = true
        };

        // Act
        var result = await _sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Profile.Should().NotBeNull();
        result.Profile.Username.Should().Be("datauser");
        
        result.Claims.Should().ContainSingle(c => c.Type == "TestClaim");
        result.HasPassword.Should().BeTrue();
        result.AccountStatus.Should().Be("Active");
    }

    #region FetchClaimsDataAsync — Available Claims Query

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldExcludeCurrentUserClaims_FromAvailableList()
    {
        // Arrange — User A has "email" and "role", User B has "role", "department", "department" (dup)
        var userA = new ApplicationUser { UserName = "userA", Email = "a@test.com" };
        var userB = new ApplicationUser { UserName = "userB", Email = "b@test.com" };
        var userC = new ApplicationUser { UserName = "userC", Email = "c@test.com" };
        await _userManager.CreateAsync(userA, "Pass123$");
        await _userManager.CreateAsync(userB, "Pass123$");
        await _userManager.CreateAsync(userC, "Pass123$");

        await _userManager.AddClaimAsync(userA, new Claim("email", "a@test.com"));
        await _userManager.AddClaimAsync(userA, new Claim("role", "admin"));
        await _userManager.AddClaimAsync(userB, new Claim("role", "editor"));
        await _userManager.AddClaimAsync(userB, new Claim("department", "engineering"));
        await _userManager.AddClaimAsync(userB, new Claim("department", "sales"));
        await _userManager.AddClaimAsync(userC, new Claim("phone", "555-0000"));

        var request = new UserEditPageDataRequest
        {
            UserId = userA.Id,
            IncludeClaims = true
        };

        // Act
        var result = await _sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.AvailableClaims.Should().BeEquivalentTo(
            new[] { "department", "phone" },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldReturnEmptyAvailableClaims_WhenNoOtherUsersHaveClaims()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "lonely", Email = "lonely@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.AddClaimAsync(user, new Claim("myclaim", "myvalue"));

        var request = new UserEditPageDataRequest
        {
            UserId = user.Id,
            IncludeClaims = true
        };

        // Act
        var result = await _sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Claims.Should().ContainSingle();
        result.AvailableClaims.Should().BeEmpty();
    }

    #endregion

    #region FetchRolesDataAsync — Available Roles Query

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldReturnAvailableRoles_ExcludingCurrentRoles()
    {
        // Arrange
        await _roleManager.CreateAsync(new IdentityRole("Admin"));
        await _roleManager.CreateAsync(new IdentityRole("Editor"));
        await _roleManager.CreateAsync(new IdentityRole("Viewer"));

        var user = new ApplicationUser { UserName = "roleuser", Email = "r@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.AddToRoleAsync(user, "Admin");

        var request = new UserEditPageDataRequest
        {
            UserId = user.Id,
            IncludeRoles = true
        };

        // Act
        var result = await _sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Roles.Should().ContainSingle(r => r == "Admin");
        result.AvailableRoles.Should().BeEquivalentTo(new[] { "Editor", "Viewer" });
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldReturnEmptyAvailableRoles_WhenUserHasAllRoles()
    {
        // Arrange
        await _roleManager.CreateAsync(new IdentityRole("OnlyRole"));

        var user = new ApplicationUser { UserName = "allroles", Email = "all@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.AddToRoleAsync(user, "OnlyRole");

        var request = new UserEditPageDataRequest
        {
            UserId = user.Id,
            IncludeRoles = true
        };

        // Act
        var result = await _sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Roles.Should().ContainSingle(r => r == "OnlyRole");
        result.AvailableRoles.Should().BeEmpty();
    }

    #endregion

    #region GetUsersAsync — LINQ Projection

    [Fact]
    public async Task GetUsersAsync_ShouldReturnAllUsers_MappedToDto()
    {
        // Arrange
        var user1 = new ApplicationUser
        {
            UserName = "alice",
            Email = "alice@test.com",
            EmailConfirmed = true,
            TwoFactorEnabled = false
        };
        var user2 = new ApplicationUser
        {
            UserName = "bob",
            Email = "bob@test.com",
            EmailConfirmed = false,
            TwoFactorEnabled = true
        };
        await _userManager.CreateAsync(user1, "Pass123$");
        await _userManager.CreateAsync(user2, "Pass123$");

        // Act
        var result = await _sut.GetUsersAsync();

        // Assert
        result.Should().HaveCount(2);

        var alice = result.Should().ContainSingle(u => u.UserName == "alice").Subject;
        alice.Email.Should().Be("alice@test.com");
        alice.EmailConfirmed.Should().BeTrue();
        alice.TwoFactorEnabled.Should().BeFalse();
        alice.LockoutStatus.Should().Be("Active");

        var bob = result.Should().ContainSingle(u => u.UserName == "bob").Subject;
        bob.Email.Should().Be("bob@test.com");
        bob.EmailConfirmed.Should().BeFalse();
        bob.TwoFactorEnabled.Should().BeTrue();
    }

    #endregion

    #region GetAccountStatus — Date Logic

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldReturnDisabled_WhenLockoutEndIsMaxValue()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "disabled", Email = "disabled@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        var request = new UserEditPageDataRequest
        {
            UserId = user.Id,
            IncludeUserTabData = true
        };

        // Act
        var result = await _sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.AccountStatus.Should().Be("Disabled");
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldReturnLockedOut_WhenLockoutEndIsFutureDate()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "locked", Email = "locked@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));

        var request = new UserEditPageDataRequest
        {
            UserId = user.Id,
            IncludeUserTabData = true
        };

        // Act
        var result = await _sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.AccountStatus.Should().StartWith("Locked Out");
    }

    [Fact]
    public async Task GetUsersAsync_ShouldMapLockoutEnd_ForDisabledUser()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "disabledlist", Email = "dl@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        // Act
        var result = await _sut.GetUsersAsync();

        // Assert
        var dto = result.Should().ContainSingle(u => u.UserName == "disabledlist").Subject;
        dto.LockoutStatus.Should().Be("Disabled");
    }

    [Fact]
    public async Task GetUsersAsync_ShouldMapLockoutEnd_ForLockedOutUser()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "lockedlist", Email = "ll@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));

        // Act
        var result = await _sut.GetUsersAsync();

        // Assert
        var dto = result.Should().ContainSingle(u => u.UserName == "lockedlist").Subject;
        dto.LockoutStatus.Should().Be("Locked Out");
    }

    #endregion

    #region UpdateUserFromEditPostAsync — Partial Update Risks

    // Documents that profile changes persist even when a subsequent password change fails.
    // The method has no wrapping transaction, so a failure partway through leaves partial updates committed.
    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldLeaveProfileUpdated_WhenPasswordChangeFails()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "original", Email = "original@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            Profile = new UserProfileEditViewModel
            {
                UserId = user.Id,
                Username = "updated",
                Email = "updated@test.com",
                EmailConfirmed = true
            },
            NewPassword = "weak" // Too weak — will fail password validation
        };

        // Act
        var result = await _sut.UpdateUserFromEditPostAsync(request);

        // Assert — the method reports failure
        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();

        // But the profile change has already been persisted
        var reloadedUser = await _userManager.FindByIdAsync(user.Id);
        reloadedUser!.UserName.Should().Be("updated", "profile update was committed before password change was attempted");
        reloadedUser.Email.Should().Be("updated@test.com");

        // BUG: The old password was removed before the new one was validated.
        // RemovePasswordAsync succeeded, then AddPasswordAsync failed, leaving the user passwordless.
        var hasPassword = await _userManager.HasPasswordAsync(reloadedUser);
        hasPassword.Should().BeFalse("RemovePasswordAsync succeeded but AddPasswordAsync failed — user is left with no password");
    }

    #endregion
}
