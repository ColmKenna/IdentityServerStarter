using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.Pages.Admin.Users;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class UsersEditModelTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IAuthorizationService> _mockAuthorizationService;
    private readonly Mock<IPersistedGrantStore> _mockGrantStore;
    private readonly Mock<IUserEditor> _mockUserEditor;
    private readonly Mock<IServerSideSessionStore> _mockSessionStore;
    private readonly EditModel _pageModel;

    public UsersEditModelTests()
    {
        _mockUserManager = TestHelpers.CreateMockUserManager();
        _mockAuthorizationService = new Mock<IAuthorizationService>();
        _mockAuthorizationService
            .Setup(s => s.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), null, It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Failed());
        _mockGrantStore = new Mock<IPersistedGrantStore>();
        _mockUserEditor = new Mock<IUserEditor>();
        _mockSessionStore = new Mock<IServerSideSessionStore>();

        _pageModel = new EditModel(
            _mockUserManager.Object,
            _mockAuthorizationService.Object,
            _mockGrantStore.Object,
            _mockUserEditor.Object,
            _mockSessionStore.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext()
            {
                HttpContext = CreateMockHttpContext()
            }
        };
    }

    private static HttpContext CreateMockHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")]));
        httpContext.User = user;
        return httpContext;
    }

    private static ApplicationUser CreateTestUser(string userId = "test-user-1", string username = "testuser", string email = "test@example.com")
    {
        return new ApplicationUser
        {
            Id = userId,
            UserName = username,
            Email = email,
            EmailConfirmed = false,
            PhoneNumber = null,
            PhoneNumberConfirmed = false,
            ConcurrencyStamp = "concurrency-1"
        };
    }

    private static UserEditPageDataDto CreateTestUserEditPageData(string userId = "test-user-1", string username = "testuser")
    {
        return new UserEditPageDataDto
        {
            Profile = new UserProfileEditViewModel
            {
                UserId = userId,
                Username = username,
                Email = "test@example.com",
                EmailConfirmed = false,
                PhoneNumber = null,
                PhoneNumberConfirmed = false,
                ConcurrencyStamp = "concurrency-1"
            },
            Claims = [],
            AvailableClaims = ["department", "location"],
            Roles = [],
            AvailableRoles = ["Admin", "User"],
            ExternalLogins = [],
            Grants = [],
            Sessions = [],
            HasPassword = true,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            TwoFactorEnabled = false,
            TwoFactorProviders = [],
            AccountStatus = "Active"
        };
    }

    private void SetupAuthorizationMock(string policy, bool allowed = true)
    {
        _mockAuthorizationService
            .Setup(s => s.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), null, policy))
            .ReturnsAsync(allowed ? AuthorizationResult.Success() : AuthorizationResult.Failed());
    }

    private void SetupAllAuthorizationsAllowed()
    {
        foreach (var policy in new[] {
            UserPolicyConstants.UsersRead, UserPolicyConstants.UsersWrite, UserPolicyConstants.UsersDelete,
            UserPolicyConstants.UserClaimsRead, UserPolicyConstants.UserClaimsWrite, UserPolicyConstants.UserClaimsDelete,
            UserPolicyConstants.UserRolesRead, UserPolicyConstants.UserRolesWrite, UserPolicyConstants.UserRolesDelete,
            UserPolicyConstants.UserGrantsRead, UserPolicyConstants.UserGrantsDelete,
            UserPolicyConstants.UserSessionsRead, UserPolicyConstants.UserSessionsDelete
        })
        {
            SetupAuthorizationMock(policy, true);
        }
    }

    private void SetupUserFoundWithPageData(ApplicationUser? user = null, UserEditPageDataDto? userData = null)
    {
        user ??= CreateTestUser();
        userData ??= CreateTestUserEditPageData();

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);
    }

    private static ProfileViewModel CreateUserEditRequest(string username = "testuser", string email = "test@example.com")
    {
        return new ProfileViewModel
        {
            UserId = "test-user-1",
            Username = username,
            Email = email,
            EmailConfirmed = false,
            PhoneNumber = null,
            PhoneNumberConfirmed = false,
            ConcurrencyStamp = "concurrency-1"
        };
    }

    private void AssertRedirectWithSuccess(IActionResult result, string expectedMessage)
    {
        result.Should().BeOfType<RedirectToPageResult>();
        _pageModel.TempData["Success"].Should().Be(expectedMessage);
    }

    [Fact]
    public async Task OnGetAsync_UserFoundAndAuthorized_ReturnsPageWithUserData()
    {
        var user = CreateTestUser();
        SetupAllAuthorizationsAllowed();
        SetupUserFoundWithPageData(user);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.UserData.Should().NotBeNull();
        _pageModel.UserData.Profile.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task OnGetAsync_PreservesAvailableClaimsFromUserEditorData()
    {
        var user = CreateTestUser();
        SetupAllAuthorizationsAllowed();
        SetupUserFoundWithPageData(user);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.UserData.AvailableClaims.Should().Contain("department");
        _pageModel.UserData.AvailableClaims.Should().Contain("location");
    }

    [Fact]
    public async Task OnGetAsync_UserNotFound_ReturnsNotFound()
    {
        _mockUserManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        _pageModel.UserId = "nonexistent-user";

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_UserEditPageDataNotFound_ReturnsNotFound()
    {
        var user = CreateTestUser();
        SetupAllAuthorizationsAllowed();

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync((UserEditPageDataDto?)null);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_UserNotAuthorizedToWrite_ReturnsForbid()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        _pageModel.UserId = user.Id;
        _pageModel.Input = CreateUserEditRequest();

        var result = await _pageModel.OnPostAsync(user.Id);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostAsync_NoUserIdProvided_ReturnsBadRequest()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        _pageModel.UserId = "";
        _pageModel.Input = CreateUserEditRequest();
        _pageModel.Input.UserId = "";

        var result = await _pageModel.OnPostAsync("");

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnPostAsync_UserNotFound_ReturnsNotFound()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        _mockUserManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        _pageModel.UserId = "nonexistent";
        _pageModel.Input = CreateUserEditRequest();

        var result = await _pageModel.OnPostAsync("nonexistent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_UpdateSucceeds_RedirectsWithSuccessMessage()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        UserEditPostUpdateRequest? capturedRequest = null;
        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .Callback<UserEditPostUpdateRequest>(r => capturedRequest = r)
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success });

        _pageModel.UserId = user.Id;
        _pageModel.Input = CreateUserEditRequest(username: "newusername");

        var result = await _pageModel.OnPostAsync(user.Id);

        AssertRedirectWithSuccess(result, "User updated successfully");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be(user.Id);
        capturedRequest.Profile.Should().NotBeNull();
        capturedRequest.Profile!.Username.Should().Be("newusername");
        capturedRequest.NewPassword.Should().BeNull();
        capturedRequest.LockoutEnabled.Should().BeNull();
        capturedRequest.TwoFactorEnabled.Should().BeNull();
    }

    [Fact]
    public async Task OnPostAsync_UpdateFails_ReturnsPageWithErrors()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        SetupAllAuthorizationsAllowed();

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult
            {
                UserFound = true,
                Result = IdentityResult.Failed(new IdentityError { Description = "Update failed" })
            });
        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;
        _pageModel.Input = CreateUserEditRequest();

        var result = await _pageModel.OnPostAsync(user.Id);

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
        _pageModel.UserData.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostAsync_UpdateUserNotFound_ReturnsNotFound()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = false });

        _pageModel.UserId = user.Id;
        _pageModel.Input = CreateUserEditRequest();

        var result = await _pageModel.OnPostAsync(user.Id);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ConcurrencyFailure_ReturnsPageWithReloadGuidance()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        SetupAllAuthorizationsAllowed();

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult
            {
                UserFound = true,
                Result = IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure", Description = "Concurrency failure" })
            });
        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;
        _pageModel.Input = CreateUserEditRequest();

        var result = await _pageModel.OnPostAsync(user.Id);

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
        var allErrors = _pageModel.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
        allErrors.Should().Contain("The user was modified by another administrator. Please reload and try again.");
    }

    [Fact]
    public async Task OnPostDeleteAsync_NotAuthorizedToDelete_ReturnsForbid()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersDelete, false);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostDeleteAsync(user.Id);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostDeleteAsync_AttemptingSelfDelete_ReturnsBadRequest()
    {
        var user = CreateTestUser(username: "admin");
        SetupAuthorizationMock(UserPolicyConstants.UsersDelete, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostDeleteAsync(user.Id);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnPostDeleteAsync_Success_RedirectsToUsersList()
    {
        var user = CreateTestUser(username: "otheruser");
        SetupAuthorizationMock(UserPolicyConstants.UsersDelete, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostDeleteAsync(user.Id);

        result.Should().BeOfType<RedirectResult>();
        _pageModel.TempData["Success"].Should().Be("User deleted successfully");
    }

    [Fact]
    public async Task OnPostDeleteAsync_DeleteFails_ReturnsPageWithErrors()
    {
        var user = CreateTestUser(username: "otheruser");
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UsersDelete, true);
        SetupAllAuthorizationsAllowed();

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));
        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostDeleteAsync(user.Id);

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostDeleteAsync_UserNotFound_ReturnsNotFound()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersDelete, true);

        _mockUserManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        _pageModel.UserId = "nonexistent";

        var result = await _pageModel.OnPostDeleteAsync("nonexistent");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostClaimsAddAsync_NotAuthorizedToClaims_ReturnsForbid()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, false);

        _pageModel.UserId = user.Id;
        _pageModel.NewClaimType = "custom-claim";

        var result = await _pageModel.OnPostClaimsAddAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostClaimsAddAsync_ClaimTypeEmpty_ReturnsPageWithError()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, true);
        SetupAllAuthorizationsAllowed();
        SetupUserFoundWithPageData(user, userData);

        _pageModel.UserId = user.Id;
        _pageModel.NewClaimType = "";

        var result = await _pageModel.OnPostClaimsAddAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("NewClaimType");
    }

    [Fact]
    public async Task OnPostClaimsAddAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.AddClaimAsync(user, It.IsAny<Claim>())).ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;
        _pageModel.NewClaimType = "custom-claim";
        _pageModel.NewClaimValue = "custom-value";

        var result = await _pageModel.OnPostClaimsAddAsync();

        AssertRedirectWithSuccess(result, "Claim added successfully");
    }

    [Fact]
    public async Task OnPostClaimsAddAsync_IdentityFails_ReturnsPageWithErrors()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, true);
        SetupAllAuthorizationsAllowed();

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.AddClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Claim add failed" }));
        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;
        _pageModel.NewClaimType = "custom-claim";
        _pageModel.NewClaimValue = "custom-value";

        var result = await _pageModel.OnPostClaimsAddAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostClaimsRemoveAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsDelete, false);

        _pageModel.UserId = "test-user-1";
        _pageModel.SelectedClaims = ["claim:value"];

        var result = await _pageModel.OnPostClaimsRemoveAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostClaimsRemoveAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsDelete, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.RemoveClaimsAsync(user, It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);

        _pageModel.SelectedClaims = ["custom-claim:custom-value"];
        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostClaimsRemoveAsync();

        AssertRedirectWithSuccess(result, "1 claim(s) removed");
    }

    [Fact]
    public async Task OnPostClaimsRemoveAsync_NoClaims_ReturnsPageWithError()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsDelete, true);
        SetupAllAuthorizationsAllowed();
        SetupUserFoundWithPageData(user, userData);

        _pageModel.SelectedClaims = [];
        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostClaimsRemoveAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostClaimsReplaceAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, false);

        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostClaimsReplaceAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostClaimsReplaceAsync_MissingClaimTypes_ReturnsPageWithError()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, true);
        SetupAllAuthorizationsAllowed();
        SetupUserFoundWithPageData(user, userData);

        _pageModel.UserId = user.Id;
        _pageModel.OldClaimType = "";
        _pageModel.ReplacementClaimType = "";

        var result = await _pageModel.OnPostClaimsReplaceAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostClaimsReplaceAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ReplaceClaimAsync(user, It.IsAny<Claim>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;
        _pageModel.OldClaimType = "old-type";
        _pageModel.OldClaimValue = "old-value";
        _pageModel.ReplacementClaimType = "new-type";
        _pageModel.ReplacementClaimValue = "new-value";

        var result = await _pageModel.OnPostClaimsReplaceAsync();

        AssertRedirectWithSuccess(result, "Claim replaced successfully");
    }

    [Fact]
    public async Task OnPostSecurityResetPasswordAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);

        _pageModel.UserId = "test-user-1";
        _pageModel.NewPassword = "NewPassword123!";

        var result = await _pageModel.OnPostSecurityResetPasswordAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityResetPasswordAsync_PasswordEmpty_ReturnsPageWithError()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        SetupAllAuthorizationsAllowed();
        SetupUserFoundWithPageData(user, userData);

        _pageModel.UserId = user.Id;
        _pageModel.NewPassword = "";

        var result = await _pageModel.OnPostSecurityResetPasswordAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("NewPassword");
    }

    [Fact]
    public async Task OnPostSecurityResetPasswordAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        UserEditPostUpdateRequest? capturedRequest = null;
        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .Callback<UserEditPostUpdateRequest>(r => capturedRequest = r)
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success });

        _pageModel.UserId = user.Id;
        _pageModel.Input = CreateUserEditRequest();
        _pageModel.NewPassword = "NewPassword123!";

        var result = await _pageModel.OnPostSecurityResetPasswordAsync();

        AssertRedirectWithSuccess(result, "Password reset successfully");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.NewPassword.Should().Be("NewPassword123!");
        capturedRequest.Profile.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostSecurityDisableAccountAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostSecurityDisableAccountAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityDisableAccountAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue))
            .ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostSecurityDisableAccountAsync();

        AssertRedirectWithSuccess(result, "Account disabled");
    }

    [Fact]
    public async Task OnPostSecurityEnableAccountAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostSecurityEnableAccountAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityEnableAccountAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(user, null))
            .ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostSecurityEnableAccountAsync();

        AssertRedirectWithSuccess(result, "Account enabled");
    }

    [Fact]
    public async Task OnPostSecurityClearLockoutAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostSecurityClearLockoutAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityClearLockoutAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostSecurityClearLockoutAsync();

        AssertRedirectWithSuccess(result, "Lockout cleared");
        _mockUserManager.Verify(m => m.SetLockoutEndDateAsync(user, null), Times.Once);
        _mockUserManager.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task OnPostSecurityClearLockoutAsync_UserNotFound_ReturnsNotFound()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        _pageModel.UserId = "nonexistent";

        var result = await _pageModel.OnPostSecurityClearLockoutAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostSecurityClearLockoutAsync_SetLockoutFails_ReturnsPageWithErrors()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        SetupAllAuthorizationsAllowed();

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(user, null))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Lockout clear failed" }));
        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostSecurityClearLockoutAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostSecurityClearLockoutAsync_ResetAccessFailedCountFails_ReturnsPageWithErrors()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        SetupAllAuthorizationsAllowed();

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.ResetAccessFailedCountAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Reset failed" }));
        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostSecurityClearLockoutAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostSecurityToggleLockoutEnabledAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostSecurityToggleLockoutEnabledAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityToggleLockoutEnabledAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        UserEditPostUpdateRequest? capturedRequest = null;
        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .Callback<UserEditPostUpdateRequest>(r => capturedRequest = r)
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success });

        _pageModel.UserId = user.Id;
        _pageModel.Input = CreateUserEditRequest();
        _pageModel.LockoutEnabledInput = true;

        var result = await _pageModel.OnPostSecurityToggleLockoutEnabledAsync();

        AssertRedirectWithSuccess(result, "Lockout enabled");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.LockoutEnabled.Should().Be(true);
        capturedRequest.Profile.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostSecurityResetFailedCountAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostSecurityResetFailedCountAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityResetFailedCountAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostSecurityResetFailedCountAsync();

        AssertRedirectWithSuccess(result, "Failed access count reset");
    }

    [Fact]
    public async Task OnPostSecurityToggleTwoFactorAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostSecurityToggleTwoFactorAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityToggleTwoFactorAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        UserEditPostUpdateRequest? capturedRequest = null;
        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserEditor
            .Setup(m => m.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .Callback<UserEditPostUpdateRequest>(r => capturedRequest = r)
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success });

        _pageModel.UserId = user.Id;
        _pageModel.Input = CreateUserEditRequest();
        _pageModel.TwoFactorEnabledInput = true;

        var result = await _pageModel.OnPostSecurityToggleTwoFactorAsync();

        AssertRedirectWithSuccess(result, "Two-factor authentication enabled");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.TwoFactorEnabled.Should().Be(true);
        capturedRequest.Profile.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostSecurityResetAuthenticatorAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostSecurityResetAuthenticatorAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityResetAuthenticatorAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.ResetAuthenticatorKeyAsync(user)).ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostSecurityResetAuthenticatorAsync();

        AssertRedirectWithSuccess(result, "Authenticator key reset");
    }

    [Fact]
    public async Task OnPostSecurityForceSignOutAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostSecurityForceSignOutAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityForceSignOutAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostSecurityForceSignOutAsync();

        AssertRedirectWithSuccess(result, "User has been signed out of all sessions");
    }

    [Fact]
    public async Task OnPostRolesAddAsync_NotAuthorizedForRoles_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UserRolesWrite, false);

        _pageModel.UserId = "test-user-1";
        _pageModel.SelectedRolesToAdd = ["Admin"];

        var result = await _pageModel.OnPostRolesAddAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostRolesAddAsync_NoRolesSelected_ReturnsPageWithError()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UserRolesWrite, true);
        SetupAllAuthorizationsAllowed();
        SetupUserFoundWithPageData(user, userData);

        _pageModel.UserId = user.Id;
        _pageModel.SelectedRolesToAdd = [];

        var result = await _pageModel.OnPostRolesAddAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostRolesAddAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserRolesWrite, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.AddToRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

        _pageModel.UserId = user.Id;
        _pageModel.SelectedRolesToAdd = ["Admin", "User"];

        var result = await _pageModel.OnPostRolesAddAsync();

        AssertRedirectWithSuccess(result, "Added to 2 role(s)");
    }

    [Fact]
    public async Task OnPostRolesRemoveAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UserRolesDelete, false);

        _pageModel.UserId = "test-user-1";
        _pageModel.SelectedRolesToRemove = ["Admin"];

        var result = await _pageModel.OnPostRolesRemoveAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostRolesRemoveAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserRolesDelete, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);

        _pageModel.SelectedRolesToRemove = ["Admin"];
        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostRolesRemoveAsync();

        AssertRedirectWithSuccess(result, "Removed from 1 role(s)");
    }

    [Fact]
    public async Task OnPostRolesRemoveAsync_NoRolesSelected_ReturnsPageWithError()
    {
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UserRolesDelete, true);
        SetupAllAuthorizationsAllowed();
        SetupUserFoundWithPageData(user, userData);

        _pageModel.SelectedRolesToRemove = [];
        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostRolesRemoveAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostGrantsSessionsRevokeGrantAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UserGrantsDelete, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostGrantsSessionsRevokeGrantAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostGrantsSessionsRevokeGrantAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserGrantsDelete, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        _pageModel.UserId = user.Id;
        _pageModel.GrantKey = "grant-key-1";

        var result = await _pageModel.OnPostGrantsSessionsRevokeGrantAsync();

        AssertRedirectWithSuccess(result, "Grant revoked");
        _mockGrantStore.Verify(s => s.RemoveAsync("grant-key-1"), Times.Once);
    }

    [Fact]
    public async Task OnPostGrantsSessionsRevokeAllGrantsAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UserGrantsDelete, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostGrantsSessionsRevokeAllGrantsAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostGrantsSessionsRevokeAllGrantsAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserGrantsDelete, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostGrantsSessionsRevokeAllGrantsAsync();

        AssertRedirectWithSuccess(result, "All grants revoked");
        _mockGrantStore.Verify(
            s => s.RemoveAllAsync(It.Is<PersistedGrantFilter>(f => f.SubjectId == user.Id)),
            Times.Once);
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndSessionAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UserSessionsDelete, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostGrantsSessionsEndSessionAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndSessionAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserSessionsDelete, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        _pageModel.UserId = user.Id;
        _pageModel.SessionKey = "session-key-1";

        var result = await _pageModel.OnPostGrantsSessionsEndSessionAsync();

        AssertRedirectWithSuccess(result, "Session ended");
        _mockSessionStore.Verify(s => s.DeleteSessionAsync("session-key-1", default), Times.Once);
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndAllSessionsAsync_NotAuthorized_ReturnsForbid()
    {
        SetupAuthorizationMock(UserPolicyConstants.UserSessionsDelete, false);
        _pageModel.UserId = "test-user-1";

        var result = await _pageModel.OnPostGrantsSessionsEndAllSessionsAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndAllSessionsAsync_Success_RedirectsWithTempData()
    {
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserSessionsDelete, true);

        _mockUserManager.Setup(m => m.FindByIdAsync(user.Id)).ReturnsAsync(user);

        _pageModel.UserId = user.Id;

        var result = await _pageModel.OnPostGrantsSessionsEndAllSessionsAsync();

        AssertRedirectWithSuccess(result, "All sessions ended");
        _mockSessionStore.Verify(
            s => s.DeleteSessionsAsync(It.Is<SessionFilter>(f => f.SubjectId == user.Id), default),
            Times.Once);
    }

}
