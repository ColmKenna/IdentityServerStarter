using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using System.Security.Claims;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class UserEditorTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<IPersistedGrantStore> _mockGrantStore;
    private readonly Mock<IServerSideSessionStore> _mockSessionStore;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserEditor _sut;

    public UserEditorTests()
    {
        _mockUserManager = MockUserManager();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(), null!, null!, null!, null!);
        _mockGrantStore = new Mock<IPersistedGrantStore>();
        _mockSessionStore = new Mock<IServerSideSessionStore>();
        _dbContext = CreateDbContext();
        _sut = new UserEditor(
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockGrantStore.Object,
            _dbContext,
            _mockSessionStore.Object);
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"UserEditorTests-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }

    private void SetupUserFound(string userId, ApplicationUser? user = null)
    {
        user ??= new ApplicationUser { Id = userId, UserName = "testuser", Email = "test@example.com" };
        _mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
    }

    private void SetupUpdateSuccess()
    {
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
    }

    #region UpdateUserFromEditPostAsync — Input Validation

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnUserMissing_WhenUserIdIsNull()
    {
        var request = new UserEditPostUpdateRequest { UserId = null! };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeFalse();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.Should().ContainSingle(e => e.Code == "UserIdMissing");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnUserMissing_WhenUserIdIsWhitespace(string userId)
    {
        var request = new UserEditPostUpdateRequest { UserId = userId };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeFalse();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.Should().ContainSingle(e => e.Code == "UserIdMissing");
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        _mockUserManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((ApplicationUser?)null);
        var request = new UserEditPostUpdateRequest { UserId = "nonexistent" };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeFalse();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.Should().ContainSingle(e => e.Code == "UserNotFound");
    }

    #endregion

    #region UpdateUserFromEditPostAsync — Profile Update

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldUpdateProfileFields_WhenProfileProvided()
    {
        var user = new ApplicationUser { Id = "123", UserName = "old", Email = "old@example.com" };
        SetupUserFound("123", user);
        SetupUpdateSuccess();

        var request = new UserEditPostUpdateRequest
        {
            UserId = "123",
            Profile = new UserProfileEditViewModel
            {
                Username = "newuser",
                Email = "new@example.com",
                EmailConfirmed = true,
                PhoneNumber = "555-1234",
                PhoneNumberConfirmed = true
            }
        };

        ApplicationUser? capturedUser = null;
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(u => capturedUser = u);

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
        capturedUser.Should().NotBeNull();
        capturedUser!.UserName.Should().Be("newuser");
        capturedUser.Email.Should().Be("new@example.com");
        capturedUser.EmailConfirmed.Should().BeTrue();
        capturedUser.PhoneNumber.Should().Be("555-1234");
        capturedUser.PhoneNumberConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldSetConcurrencyStamp_WhenProvided()
    {
        var user = new ApplicationUser { Id = "123", ConcurrencyStamp = "old-stamp" };
        SetupUserFound("123", user);

        ApplicationUser? capturedUser = null;
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(u => capturedUser = u);

        var request = new UserEditPostUpdateRequest
        {
            UserId = "123",
            Profile = new UserProfileEditViewModel { ConcurrencyStamp = "new-stamp" }
        };

        await _sut.UpdateUserFromEditPostAsync(request);

        capturedUser!.ConcurrencyStamp.Should().Be("new-stamp");
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenUpdateAsyncFails()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "UpdateFailed", Description = "Update failed" }));

        var request = new UserEditPostUpdateRequest
        {
            UserId = "123",
            Profile = new UserProfileEditViewModel { Username = "test" },
            LockoutEnabled = true,
            TwoFactorEnabled = false,
            NewPassword = "NewPass1!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.Should().ContainSingle(e => e.Code == "UpdateFailed");
        _mockUserManager.Verify(x => x.SetLockoutEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>()), Times.Never);
        _mockUserManager.Verify(x => x.SetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>()), Times.Never);
        _mockUserManager.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region UpdateUserFromEditPostAsync — Password

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldSkipPasswordChange_WhenNewPasswordIsNull()
    {
        SetupUserFound("123");
        var request = new UserEditPostUpdateRequest { UserId = "123" };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.Result.Succeeded.Should().BeTrue();
        _mockUserManager.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserManager.Verify(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserManager.Verify(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenNewPasswordIsEmpty(string password)
    {
        SetupUserFound("123");
        var request = new UserEditPostUpdateRequest { UserId = "123", NewPassword = password };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.Should().ContainSingle(e => e.Code == "PasswordMissing");
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldRemoveAndAddPassword_WhenUserHasExistingPassword()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), "NewPass1!")).ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest { UserId = "123", NewPassword = "NewPass1!" };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.Result.Succeeded.Should().BeTrue();
        _mockUserManager.Verify(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _mockUserManager.Verify(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), "NewPass1!"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldAddPasswordDirectly_WhenUserHasNoPassword()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(false);
        _mockUserManager.Setup(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), "NewPass1!")).ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest { UserId = "123", NewPassword = "NewPass1!" };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.Result.Succeeded.Should().BeTrue();
        _mockUserManager.Verify(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserManager.Verify(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), "NewPass1!"), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenRemovePasswordFails()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Remove failed" }));

        var request = new UserEditPostUpdateRequest { UserId = "123", NewPassword = "NewPass1!" };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.Result.Succeeded.Should().BeFalse();
        _mockUserManager.Verify(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    // TODO: This test documents a partial-update risk — if RemovePasswordAsync succeeds but
    // AddPasswordAsync fails, the user is left without any password. See UserEditor.cs lines 199-202.
    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenAddPasswordFails()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), "NewPass1!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak" }));

        var request = new UserEditPostUpdateRequest { UserId = "123", NewPassword = "NewPass1!" };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.Result.Succeeded.Should().BeFalse();
        _mockUserManager.Verify(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    #endregion

    #region UpdateUserFromEditPostAsync — Lockout and TwoFactor

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldSetLockout_WhenLockoutEnabledHasValue()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.SetLockoutEnabledAsync(It.IsAny<ApplicationUser>(), true))
            .ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest { UserId = "123", LockoutEnabled = true };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.Result.Succeeded.Should().BeTrue();
        _mockUserManager.Verify(x => x.SetLockoutEnabledAsync(It.IsAny<ApplicationUser>(), true), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldSetTwoFactor_WhenTwoFactorEnabledHasValue()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.SetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), true))
            .ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest { UserId = "123", TwoFactorEnabled = true };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.Result.Succeeded.Should().BeTrue();
        _mockUserManager.Verify(x => x.SetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), true), Times.Once);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenSetLockoutEnabledFails()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.SetLockoutEnabledAsync(It.IsAny<ApplicationUser>(), true))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "LockoutFailed", Description = "Lockout failed" }));

        var request = new UserEditPostUpdateRequest
        {
            UserId = "123",
            LockoutEnabled = true,
            TwoFactorEnabled = false,
            NewPassword = "NewPass1!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.Should().ContainSingle(e => e.Code == "LockoutFailed");
        _mockUserManager.Verify(x => x.SetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>()), Times.Never);
        _mockUserManager.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnFailure_WhenSetTwoFactorEnabledFails()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.SetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), true))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "TwoFactorFailed", Description = "2FA failed" }));

        var request = new UserEditPostUpdateRequest
        {
            UserId = "123",
            TwoFactorEnabled = true,
            NewPassword = "NewPass1!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeFalse();
        result.Result.Errors.Should().ContainSingle(e => e.Code == "TwoFactorFailed");
        _mockUserManager.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region UpdateUserFromEditPostAsync — Full Success

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldReturnSuccess_WhenAllUpdatesSucceed()
    {
        SetupUserFound("123");
        SetupUpdateSuccess();
        _mockUserManager.Setup(x => x.SetLockoutEnabledAsync(It.IsAny<ApplicationUser>(), true)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.SetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), false)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.RemovePasswordAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.AddPasswordAsync(It.IsAny<ApplicationUser>(), "NewPass1!")).ReturnsAsync(IdentityResult.Success);

        var request = new UserEditPostUpdateRequest
        {
            UserId = "123",
            Profile = new UserProfileEditViewModel { Username = "updated" },
            LockoutEnabled = true,
            TwoFactorEnabled = false,
            NewPassword = "NewPass1!"
        };

        var result = await _sut.UpdateUserFromEditPostAsync(request);

        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();
    }

    #endregion

    #region GetUserForEditAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserForEditAsync_ShouldReturnNull_WhenUserIdIsEmpty(string? userId)
    {
        var result = await _sut.GetUserForEditAsync(userId!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserForEditAsync_ShouldReturnNull_WhenUserNotFound()
    {
        _mockUserManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.GetUserForEditAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserForEditAsync_ShouldReturnMappedViewModel_WhenUserExists()
    {
        var user = new ApplicationUser
        {
            Id = "123",
            UserName = "alice",
            Email = "alice@example.com",
            EmailConfirmed = true,
            PhoneNumber = "555-0000",
            PhoneNumberConfirmed = false,
            ConcurrencyStamp = "stamp-1"
        };
        SetupUserFound("123", user);

        var result = await _sut.GetUserForEditAsync("123");

        result.Should().NotBeNull();
        result!.UserId.Should().Be("123");
        result.Username.Should().Be("alice");
        result.Email.Should().Be("alice@example.com");
        result.EmailConfirmed.Should().BeTrue();
        result.PhoneNumber.Should().Be("555-0000");
        result.PhoneNumberConfirmed.Should().BeFalse();
        result.ConcurrencyStamp.Should().Be("stamp-1");
    }

    #endregion

    #region GetUserEditPageDataAsync — Input Validation

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldReturnNull_WhenUserIdIsEmpty()
    {
        var request = new UserEditPageDataRequest { UserId = "" };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldReturnNull_WhenUserNotFound()
    {
        _mockUserManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((ApplicationUser?)null);
        var request = new UserEditPageDataRequest { UserId = "nonexistent" };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().BeNull();
    }

    #endregion

    #region GetUserEditPageDataAsync — Conditional Flags

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldFetchClaims_WhenIncludeClaimsIsTrue()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<Claim>());

        var request = new UserEditPageDataRequest { UserId = "123", IncludeClaims = true };

        await _sut.GetUserEditPageDataAsync(request);

        _mockUserManager.Verify(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldSkipClaimsFetch_WhenIncludeClaimsIsFalse()
    {
        SetupUserFound("123");
        var request = new UserEditPageDataRequest { UserId = "123", IncludeClaims = false };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().NotBeNull();
        result!.Claims.Should().BeEmpty();
        result.AvailableClaims.Should().BeEmpty();
        _mockUserManager.Verify(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldFetchRoles_WhenIncludeRolesIsTrue()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());
        _mockRoleManager.Setup(x => x.Roles)
            .Returns(new List<IdentityRole>().BuildMock());

        var request = new UserEditPageDataRequest { UserId = "123", IncludeRoles = true };

        await _sut.GetUserEditPageDataAsync(request);

        _mockUserManager.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldSkipRolesFetch_WhenIncludeRolesIsFalse()
    {
        SetupUserFound("123");
        var request = new UserEditPageDataRequest { UserId = "123", IncludeRoles = false };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().NotBeNull();
        result!.Roles.Should().BeEmpty();
        result.AvailableRoles.Should().BeEmpty();
        _mockUserManager.Verify(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldFetchUserTabData_WhenIncludeUserTabDataIsTrue()
    {
        SetupUserFound("123");
        _mockUserManager.Setup(x => x.GetLoginsAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<UserLoginInfo>());
        _mockUserManager.Setup(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(false);
        _mockUserManager.Setup(x => x.GetValidTwoFactorProvidersAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(new List<string>());

        var request = new UserEditPageDataRequest { UserId = "123", IncludeUserTabData = true };

        await _sut.GetUserEditPageDataAsync(request);

        _mockUserManager.Verify(x => x.GetLoginsAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _mockUserManager.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _mockUserManager.Verify(x => x.GetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>()), Times.Once);
        _mockUserManager.Verify(x => x.GetValidTwoFactorProvidersAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldSkipUserTabDataFetch_WhenIncludeUserTabDataIsFalse()
    {
        SetupUserFound("123");
        var request = new UserEditPageDataRequest { UserId = "123", IncludeUserTabData = false };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().NotBeNull();
        result!.HasPassword.Should().BeFalse();
        result.AccountStatus.Should().Be("Active");
        _mockUserManager.Verify(x => x.GetLoginsAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserManager.Verify(x => x.HasPasswordAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserManager.Verify(x => x.GetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserManager.Verify(x => x.GetValidTwoFactorProvidersAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldFetchGrants_WhenIncludeGrantsIsTrue()
    {
        SetupUserFound("123");
        _mockGrantStore.Setup(x => x.GetAllAsync(It.IsAny<PersistedGrantFilter>()))
            .ReturnsAsync(new List<PersistedGrant>());

        var request = new UserEditPageDataRequest { UserId = "123", IncludeGrants = true };

        await _sut.GetUserEditPageDataAsync(request);

        _mockGrantStore.Verify(x => x.GetAllAsync(It.Is<PersistedGrantFilter>(f => f.SubjectId == "123")), Times.Once);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldSkipGrantsFetch_WhenIncludeGrantsIsFalse()
    {
        SetupUserFound("123");
        var request = new UserEditPageDataRequest { UserId = "123", IncludeGrants = false };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().NotBeNull();
        result!.Grants.Should().BeEmpty();
        _mockGrantStore.Verify(x => x.GetAllAsync(It.IsAny<PersistedGrantFilter>()), Times.Never);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldFetchSessions_WhenIncludeSessionsIsTrue()
    {
        SetupUserFound("123");
        _mockSessionStore.Setup(x => x.GetSessionsAsync(It.IsAny<SessionFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServerSideSession>());

        var request = new UserEditPageDataRequest { UserId = "123", IncludeSessions = true };

        await _sut.GetUserEditPageDataAsync(request);

        _mockSessionStore.Verify(x => x.GetSessionsAsync(It.Is<SessionFilter>(f => f.SubjectId == "123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldSkipSessionsFetch_WhenIncludeSessionsIsFalse()
    {
        SetupUserFound("123");
        var request = new UserEditPageDataRequest { UserId = "123", IncludeSessions = false };

        var result = await _sut.GetUserEditPageDataAsync(request);

        result.Should().NotBeNull();
        result!.Sessions.Should().BeEmpty();
        _mockSessionStore.Verify(x => x.GetSessionsAsync(It.IsAny<SessionFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region FetchSessionsAsync — Null Session Store

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldReturnEmptySessions_WhenSessionStoreIsNull()
    {
        var sutWithoutSessions = new UserEditor(
            _mockUserManager.Object,
            _mockRoleManager.Object,
            _mockGrantStore.Object,
            _dbContext,
            sessionStore: null);

        SetupUserFound("123");
        var request = new UserEditPageDataRequest { UserId = "123", IncludeSessions = true };

        var result = await sutWithoutSessions.GetUserEditPageDataAsync(request);

        result.Should().NotBeNull();
        result!.Sessions.Should().BeEmpty();
    }

    #endregion
}
