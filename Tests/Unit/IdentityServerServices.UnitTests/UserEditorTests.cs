using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using FluentAssertions;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Moq;
using System.Security.Claims;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class UserEditorTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<IPersistedGrantStore> _grantStoreMock;
    private readonly Mock<IServerSideSessionStore> _sessionStoreMock;
    private readonly UserEditor _sut;

    public UserEditorTests()
    {
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!);

        _grantStoreMock = new Mock<IPersistedGrantStore>();
        _sessionStoreMock = new Mock<IServerSideSessionStore>();

        _sut = new UserEditor(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _grantStoreMock.Object,
            _sessionStoreMock.Object);
    }

    private static ApplicationUser CreateTestUser(string id = "user-1", string? userName = "testuser", string? email = "test@example.com")
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = "123-456-7890",
            PhoneNumberConfirmed = false,
            ConcurrencyStamp = "stamp-1",
            LockoutEnabled = true,
            AccessFailedCount = 0
        };
    }

    private void SetupUserExists(ApplicationUser user)
    {
        _userManagerMock.Setup(x => x.FindByIdAsync(user.Id)).ReturnsAsync(user);
    }

    private void SetupUserTabDataMocks(
        ApplicationUser user,
        bool hasPassword = true,
        bool twoFactorEnabled = false,
        List<UserLoginInfo>? externalLogins = null,
        List<string>? twoFactorProviders = null)
    {
        _userManagerMock.Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(externalLogins ?? new List<UserLoginInfo>());
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(hasPassword);
        _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(twoFactorEnabled);
        _userManagerMock.Setup(x => x.GetValidTwoFactorProvidersAsync(user))
            .ReturnsAsync(twoFactorProviders ?? new List<string>());
    }

    private void SetupSuccessfulUpdatePipeline(
        ApplicationUser user,
        bool lockoutEnabled = true,
        bool twoFactorEnabled = true,
        string newPassword = "NewPass123!",
        bool hasExistingPassword = false)
    {
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.SetLockoutEnabledAsync(user, lockoutEnabled)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.SetTwoFactorEnabledAsync(user, twoFactorEnabled)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(hasExistingPassword);
        if (hasExistingPassword)
        {
            _userManagerMock.Setup(x => x.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
        }
        _userManagerMock.Setup(x => x.AddPasswordAsync(user, newPassword)).ReturnsAsync(IdentityResult.Success);
    }

    private static UserEditPostUpdateRequest CreateProfileUpdateRequest(
        ApplicationUser user,
        string username = "newname",
        string email = "new@example.com",
        string? concurrencyStamp = null)
    {
        return new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            Profile = new UserProfileEditViewModel
            {
                UserId = user.Id,
                Username = username,
                Email = email,
                ConcurrencyStamp = concurrencyStamp
            }
        };
    }

    #region GetUserForEditAsync

    [Fact]
    public async Task GetUserForEditAsync_WithNullUserId_ReturnsNull()
    {
        var result = await _sut.GetUserForEditAsync(null!);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserForEditAsync_WithEmptyOrWhitespaceUserId_ReturnsNull(string userId)
    {
        var result = await _sut.GetUserForEditAsync(userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserForEditAsync_WhenUserNotFound_ReturnsNull()
    {
        _userManagerMock.Setup(x => x.FindByIdAsync("nonexistent"))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetUserForEditAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserForEditAsync_WhenUserExists_ReturnsMappedViewModel()
    {
        var user = CreateTestUser();
        SetupUserExists(user);

        var result = await _sut.GetUserForEditAsync(user.Id);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        result.Username.Should().Be(user.UserName);
        result.Email.Should().Be(user.Email);
        result.EmailConfirmed.Should().Be(user.EmailConfirmed);
        result.PhoneNumber.Should().Be(user.PhoneNumber);
        result.PhoneNumberConfirmed.Should().Be(user.PhoneNumberConfirmed);
        result.ConcurrencyStamp.Should().Be(user.ConcurrencyStamp);
    }

    [Fact]
    public async Task GetUserForEditAsync_WhenUserHasNullUserNameAndEmail_MapsToEmptyString()
    {
        var user = CreateTestUser(userName: null, email: null);
        SetupUserExists(user);

        var result = await _sut.GetUserForEditAsync(user.Id);

        result.Should().NotBeNull();
        result!.Username.Should().Be(string.Empty);
        result.Email.Should().Be(string.Empty);
    }

    #endregion

    #region GetUserEditPageDataAsync

    [Fact]
    public async Task GetUserEditPageDataAsync_WithNullUserId_ReturnsNull()
    {
        var request = new UserEditPageDataRequest { UserId = null! };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithEmptyUserId_ReturnsNull()
    {
        var request = new UserEditPageDataRequest { UserId = "" };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WhenUserNotFound_ReturnsNull()
    {
        _userManagerMock.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);
        var request = new UserEditPageDataRequest { UserId = "missing" };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithNoIncludes_ReturnsProfileOnly()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        var request = new UserEditPageDataRequest { UserId = user.Id };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().NotBeNull();
        result!.Profile.UserId.Should().Be(user.Id);
        result.Claims.Should().BeEmpty();
        result.Roles.Should().BeEmpty();
        result.Grants.Should().BeEmpty();
        result.Sessions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithIncludeClaims_ReturnsClaims()
    {
        var user = CreateTestUser();
        var claims = new List<Claim> { new("role", "admin"), new("email", "test@example.com") };
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(claims);

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeClaims = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.Claims.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithIncludeRoles_ReturnsRolesAndAvailableRoles()
    {
        var user = CreateTestUser();
        var userRoles = new List<string> { "Admin" };
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(userRoles);

        var allRoles = new List<IdentityRole>
        {
            new("Admin"),
            new("User"),
            new("Manager")
        }.AsQueryable();
        _roleManagerMock.Setup(x => x.Roles).Returns(new TestAsyncEnumerable<IdentityRole>(allRoles));

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeRoles = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.Roles.Should().Contain("Admin");
        result.AvailableRoles.Should().Contain("User");
        result.AvailableRoles.Should().Contain("Manager");
        result.AvailableRoles.Should().NotContain("Admin");
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithIncludeRoles_FiltersOutRolesWithNullName()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

        var allRoles = new List<IdentityRole>
        {
            new("Admin"),
            new() { Name = null, NormalizedName = null }
        }.AsQueryable();
        _roleManagerMock.Setup(x => x.Roles).Returns(new TestAsyncEnumerable<IdentityRole>(allRoles));

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeRoles = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.AvailableRoles.Should().HaveCount(1);
        result.AvailableRoles.Should().Contain("Admin");
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithIncludeGrants_ReturnsGrants()
    {
        var user = CreateTestUser();
        var grants = new List<PersistedGrant>
        {
            new() { Key = "grant-1", SubjectId = user.Id, ClientId = "client-1", Type = "authorization_code" }
        };
        SetupUserExists(user);
        _grantStoreMock.Setup(x => x.GetAllAsync(It.Is<PersistedGrantFilter>(f => f.SubjectId == user.Id)))
            .ReturnsAsync(grants);

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeGrants = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.Grants.Should().HaveCount(1);
        result.Grants[0].Key.Should().Be("grant-1");
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithIncludeSessions_ReturnsSessions()
    {
        var user = CreateTestUser();
        var sessions = new List<ServerSideSession>
        {
            new() { Key = "session-1", SubjectId = user.Id, Scheme = "scheme" }
        };
        SetupUserExists(user);
        _sessionStoreMock.Setup(x => x.GetSessionsAsync(
                It.Is<SessionFilter>(f => f.SubjectId == user.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeSessions = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.Sessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithAllIncludes_ReturnsAllData()
    {
        var user = CreateTestUser();
        user.LockoutEnabled = true;
        user.AccessFailedCount = 2;
        SetupUserExists(user);
        SetupUserTabDataMocks(user, hasPassword: true, twoFactorEnabled: true,
            twoFactorProviders: new List<string> { "Authenticator" });

        var claims = new List<Claim> { new("role", "admin") };
        _userManagerMock.Setup(x => x.GetClaimsAsync(user)).ReturnsAsync(claims);

        var userRoles = new List<string> { "Admin" };
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(userRoles);
        var allRoles = new List<IdentityRole> { new("Admin"), new("User") }.AsQueryable();
        _roleManagerMock.Setup(x => x.Roles).Returns(new TestAsyncEnumerable<IdentityRole>(allRoles));

        var grants = new List<PersistedGrant>
        {
            new() { Key = "grant-1", SubjectId = user.Id, ClientId = "client-1", Type = "authorization_code" }
        };
        _grantStoreMock.Setup(x => x.GetAllAsync(It.Is<PersistedGrantFilter>(f => f.SubjectId == user.Id)))
            .ReturnsAsync(grants);

        var sessions = new List<ServerSideSession>
        {
            new() { Key = "session-1", SubjectId = user.Id, Scheme = "scheme" }
        };
        _sessionStoreMock.Setup(x => x.GetSessionsAsync(
                It.Is<SessionFilter>(f => f.SubjectId == user.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var request = new UserEditPageDataRequest
        {
            UserId = user.Id,
            IncludeUserTabData = true,
            IncludeClaims = true,
            IncludeRoles = true,
            IncludeGrants = true,
            IncludeSessions = true
        };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().NotBeNull();
        result!.Profile.UserId.Should().Be(user.Id);
        result.Claims.Should().HaveCount(1);
        result.Roles.Should().Contain("Admin");
        result.AvailableRoles.Should().Contain("User");
        result.Grants.Should().HaveCount(1);
        result.Sessions.Should().HaveCount(1);
        result.HasPassword.Should().BeTrue();
        result.TwoFactorEnabled.Should().BeTrue();
        result.LockoutEnabled.Should().BeTrue();
        result.AccessFailedCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithIncludeUserTabData_ReturnsSecurityInfo()
    {
        var user = CreateTestUser();
        user.LockoutEnabled = true;
        user.AccessFailedCount = 3;
        SetupUserExists(user);
        SetupUserTabDataMocks(user, hasPassword: true, twoFactorEnabled: true,
            twoFactorProviders: new List<string> { "Authenticator" });

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeUserTabData = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.HasPassword.Should().BeTrue();
        result.LockoutEnabled.Should().BeTrue();
        result.AccessFailedCount.Should().Be(3);
        result.TwoFactorEnabled.Should().BeTrue();
        result.TwoFactorProviders.Should().Contain("Authenticator");
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WithIncludeUserTabData_ReturnsExternalLogins()
    {
        var user = CreateTestUser();
        var logins = new List<UserLoginInfo>
        {
            new("Google", "google-key-1", "Google")
        };
        SetupUserExists(user);
        SetupUserTabDataMocks(user, externalLogins: logins);

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeUserTabData = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.ExternalLogins.Should().HaveCount(1);
        result.ExternalLogins[0].LoginProvider.Should().Be("Google");
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WhenUserIsDisabled_ReturnsDisabledStatus()
    {
        var user = CreateTestUser();
        user.LockoutEnd = DateTimeOffset.MaxValue;
        SetupUserExists(user);
        SetupUserTabDataMocks(user);

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeUserTabData = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.AccountStatus.Should().Be("Disabled");
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WhenUserIsLockedOut_ReturnsLockedOutStatus()
    {
        var user = CreateTestUser();
        user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(1);
        SetupUserExists(user);
        SetupUserTabDataMocks(user);

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeUserTabData = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.AccountStatus.Should().StartWith("Locked Out");
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_WhenLockoutExpired_ReturnsActiveStatus()
    {
        var user = CreateTestUser();
        user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(-1);
        SetupUserExists(user);
        SetupUserTabDataMocks(user);

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeUserTabData = true };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result!.AccountStatus.Should().Be("Active");
    }

    #endregion

    #region UpdateUserFromEditPostAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateUserFromEditPostAsync_WithMissingUserId_ReturnsUserNotFound(string? userId)
    {
        var request = new UserEditPostUpdateRequest { UserId = userId! };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeFalse();
        result.Result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WhenUserNotFound_ReturnsUserNotFound()
    {
        _userManagerMock.Setup(x => x.FindByIdAsync("missing")).ReturnsAsync((ApplicationUser?)null);
        var request = new UserEditPostUpdateRequest { UserId = "missing" };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeFalse();
        result.Result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithProfileUpdate_CallsUpdateAsync()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            Profile = new UserProfileEditViewModel
            {
                UserId = user.Id,
                Username = "newname",
                Email = "new@example.com",
                EmailConfirmed = false,
                PhoneNumber = "999-999-9999",
                PhoneNumberConfirmed = true,
                ConcurrencyStamp = "new-stamp"
            }
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithProfileConcurrencyStampEmpty_PreservesOriginalStamp()
    {
        var user = CreateTestUser();
        var originalStamp = user.ConcurrencyStamp;
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var request = CreateProfileUpdateRequest(user, concurrencyStamp: "");

        await _sut.UpdateUserFromEditPostAsync(request);

        user.ConcurrencyStamp.Should().Be(originalStamp);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithProfileConcurrencyStampNull_PreservesOriginalStamp()
    {
        var user = CreateTestUser();
        var originalStamp = user.ConcurrencyStamp;
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var request = CreateProfileUpdateRequest(user, concurrencyStamp: null);

        await _sut.UpdateUserFromEditPostAsync(request);

        user.ConcurrencyStamp.Should().Be(originalStamp);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WhenProfileFails_StopsEarly()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure", Description = "Conflict" }));

        var request = CreateProfileUpdateRequest(user);
        request.LockoutEnabled = true;
        request.TwoFactorEnabled = true;
        request.NewPassword = "NewPass123!";

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        _userManagerMock.Verify(x => x.SetLockoutEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>()), Times.Never);
        _userManagerMock.Verify(x => x.SetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>()), Times.Never);
        _userManagerMock.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _userManagerMock.Verify(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithNullProfile_SkipsProfileAndProceedsToLockout()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            LockoutEnabled = true
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _userManagerMock.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WhenLockoutFails_StopsBeforeTwoFactorOrPassword()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.SetLockoutEnabledAsync(user, true))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Failed" }));

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            LockoutEnabled = true,
            TwoFactorEnabled = true,
            NewPassword = "NewPass123!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        _userManagerMock.Verify(x => x.SetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>()), Times.Never);
        _userManagerMock.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WhenTwoFactorFails_StopsBeforePassword()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.SetTwoFactorEnabledAsync(user, false))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Failed" }));

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            TwoFactorEnabled = false,
            NewPassword = "NewPass123!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        _userManagerMock.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithAllStepsSucceeding_ExecutesFullPipeline()
    {
        var user = CreateTestUser();
        SetupSuccessfulUpdatePipeline(user);

        var request = CreateProfileUpdateRequest(user);
        request.LockoutEnabled = true;
        request.TwoFactorEnabled = true;
        request.NewPassword = "NewPass123!";

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _userManagerMock.Verify(x => x.SetLockoutEnabledAsync(user, true), Times.Once);
        _userManagerMock.Verify(x => x.SetTwoFactorEnabledAsync(user, true), Times.Once);
        _userManagerMock.Verify(x => x.AddPasswordAsync(user, "NewPass123!"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithNewPassword_WhenHasExistingPassword_RemovesThenAdds()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddPasswordAsync(user, "NewPass123!")).ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            NewPassword = "NewPass123!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.RemovePasswordAsync(user), Times.Once);
        _userManagerMock.Verify(x => x.AddPasswordAsync(user, "NewPass123!"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithNewPassword_WhenNoExistingPassword_AddsDirectly()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.AddPasswordAsync(user, "NewPass123!")).ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            NewPassword = "NewPass123!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _userManagerMock.Verify(x => x.AddPasswordAsync(user, "NewPass123!"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithNullPassword_SkipsPasswordStep()
    {
        var user = CreateTestUser();
        SetupUserExists(user);

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            NewPassword = null
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _userManagerMock.Verify(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateUserFromEditPostAsync_WithEmptyNewPassword_ReturnsFailure(string password)
    {
        var user = CreateTestUser();
        SetupUserExists(user);

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            NewPassword = password
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.Should().Contain(e => e.Code == "PasswordMissing");
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WhenRemovePasswordFails_ReturnsFailureWithoutAddingPassword()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.RemovePasswordAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "Error", Description = "Failed" }));

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            NewPassword = "NewPass123!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        _userManagerMock.Verify(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WhenAddPasswordFails_ReturnsFailure()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.HasPasswordAsync(user)).ReturnsAsync(false);
        _userManagerMock.Setup(x => x.AddPasswordAsync(user, "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "PasswordTooWeak", Description = "Too weak" }));

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            NewPassword = "weak"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_WithNoChanges_ReturnsSuccess()
    {
        var user = CreateTestUser();
        SetupUserExists(user);

        var request = new UserEditPostUpdateRequest { UserId = user.Id };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region UpdateUserProfileAsync

    [Fact]
    public async Task UpdateUserProfileAsync_WithValidProfile_DelegatesToUpdatePipeline()
    {
        var user = CreateTestUser();
        SetupUserExists(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var viewModel = new UserProfileEditViewModel
        {
            UserId = user.Id,
            Username = "updated",
            Email = "updated@example.com"
        };

        var result = await _sut.UpdateUserProfileAsync(viewModel);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    #endregion

    #region Constructor - optional sessionStore

    [Fact]
    public async Task GetUserEditPageDataAsync_WithoutSessionStore_SkipsSessions()
    {
        var editorWithoutSessions = new UserEditor(
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _grantStoreMock.Object,
            sessionStore: null);

        var user = CreateTestUser();
        SetupUserExists(user);

        var request = new UserEditPageDataRequest { UserId = user.Id, IncludeSessions = true };

        var result = await editorWithoutSessions.GetUserEditPageDataAsync(request);

        result!.Sessions.Should().BeEmpty();
        _sessionStoreMock.Verify(
            x => x.GetSessionsAsync(It.IsAny<SessionFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
