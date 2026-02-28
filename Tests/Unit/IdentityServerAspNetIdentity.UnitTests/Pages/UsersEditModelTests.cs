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
        _mockUserManager = CreateMockUserManager();
        _mockAuthorizationService = new Mock<IAuthorizationService>();
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

    #region Test Helpers

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        mockUserManager.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());
        return mockUserManager;
    }

    private static HttpContext CreateMockHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }));
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
            Claims = new List<Claim>(),
            Roles = new List<string>(),
            AvailableRoles = new List<string> { "Admin", "User" },
            ExternalLogins = new List<UserLoginInfo>(),
            Grants = new List<PersistedGrant>(),
            Sessions = new List<ServerSideSession>(),
            HasPassword = true,
            LockoutEnabled = true,
            AccessFailedCount = 0,
            TwoFactorEnabled = false,
            TwoFactorProviders = new List<string>(),
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
            UserPolicyConstants.UserGrantsDelete, UserPolicyConstants.UserSessionsDelete
        })
        {
            SetupAuthorizationMock(policy, true);
        }
    }

    #endregion

    #region OnGetAsync Tests

    [Fact]
    public async Task Should_ReturnPageWithUserData_When_UserFoundAndAuthorized()
    {
        // Arrange
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAllAuthorizationsAllowed();

        _mockUserManager
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;

        // Act
        var result = await _pageModel.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _pageModel.UserData.Should().NotBeNull();
        _pageModel.UserData.Profile.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UserNotFound()
    {
        // Arrange
        _mockUserManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        _pageModel.UserId = "nonexistent-user";

        // Act
        var result = await _pageModel.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UserEditPageDataNotFound()
    {
        // Arrange
        var user = CreateTestUser();
        SetupAllAuthorizationsAllowed();

        _mockUserManager
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync((UserEditPageDataDto?)null);

        _pageModel.UserId = user.Id;

        // Act
        var result = await _pageModel.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region OnPostAsync Tests

    [Fact]
    public async Task Should_ReturnForbid_When_UserNotAuthorizedToWrite()
    {
        // Arrange
        var user = CreateTestUser();
        var request = CreateUserEditRequest();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);

        _mockUserManager
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _pageModel.UserId = user.Id;
        _pageModel.Input = request;

        // Act
        var result = await _pageModel.OnPostAsync(user.Id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_NoUserIdProvided()
    {
        // Arrange
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        _pageModel.UserId = "";
        _pageModel.Input = CreateUserEditRequest();

        // Act
        var result = await _pageModel.OnPostAsync("");

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UserNotFoundDuringPost()
    {
        // Arrange
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);
        _mockUserManager
            .Setup(m => m.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);

        _pageModel.UserId = "nonexistent";
        _pageModel.Input = CreateUserEditRequest();

        // Act
        var result = await _pageModel.OnPostAsync("nonexistent");

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region OnPostDeleteAsync Tests

    [Fact]
    public async Task Should_ReturnForbid_When_NotAuthorizedToDelete()
    {
        // Arrange
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersDelete, false);

        _pageModel.UserId = user.Id;

        // Act
        var result = await _pageModel.OnPostDeleteAsync(user.Id);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_AttemptingSelfDelete()
    {
        // Arrange
        var user = CreateTestUser(username: "admin");
        SetupAuthorizationMock(UserPolicyConstants.UsersDelete, true);

        _mockUserManager
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _pageModel.UserId = user.Id;

        // Act
        var result = await _pageModel.OnPostDeleteAsync(user.Id);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
    }

    #endregion

    #region Claims Handler Tests

    [Fact]
    public async Task Should_ReturnForbid_When_NotAuthorizedToClaims()
    {
        // Arrange
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, false);

        _pageModel.UserId = user.Id;
        _pageModel.NewClaimType = "custom-claim";

        // Act
        var result = await _pageModel.OnPostClaimsAddAsync();

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Should_ReturnPageWithError_When_ClaimTypeEmpty()
    {
        // Arrange
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UserClaimsWrite, true);

        _mockUserManager
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;
        _pageModel.NewClaimType = "";

        // Act
        var result = await _pageModel.OnPostClaimsAddAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("NewClaimType");
    }

    #endregion

    #region Security Handler Tests

    [Fact]
    public async Task Should_ReturnForbid_When_NotAuthorizedForSecurity()
    {
        // Arrange
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, false);

        _pageModel.UserId = user.Id;
        _pageModel.NewPassword = "NewPassword123!";

        // Act
        var result = await _pageModel.OnPostSecurityResetPasswordAsync();

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Should_ReturnPageWithError_When_PasswordEmpty()
    {
        // Arrange
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UsersWrite, true);

        _mockUserManager
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;
        _pageModel.NewPassword = "";

        // Act
        var result = await _pageModel.OnPostSecurityResetPasswordAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().ContainKey("NewPassword");
    }

    #endregion

    #region Roles Handler Tests

    [Fact]
    public async Task Should_ReturnForbid_When_NotAuthorizedForRoles()
    {
        // Arrange
        var user = CreateTestUser();
        SetupAuthorizationMock(UserPolicyConstants.UserRolesWrite, false);

        _pageModel.UserId = user.Id;
        _pageModel.SelectedRolesToAdd = new List<string> { "Admin" };

        // Act
        var result = await _pageModel.OnPostRolesAddAsync();

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Should_ReturnPageWithError_When_NoRolesSelectedForAdd()
    {
        // Arrange
        var user = CreateTestUser();
        var userData = CreateTestUserEditPageData();
        SetupAuthorizationMock(UserPolicyConstants.UserRolesWrite, true);

        _mockUserManager
            .Setup(m => m.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _mockUserEditor
            .Setup(m => m.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>()))
            .ReturnsAsync(userData);

        _pageModel.UserId = user.Id;
        _pageModel.SelectedRolesToAdd = new List<string>();

        // Act
        var result = await _pageModel.OnPostRolesAddAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.Should().HaveCount(1);
    }

    #endregion

    #region Private Helpers

    private ProfileViewModel CreateUserEditRequest(string username = "testuser", string email = "test@example.com")
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

    #endregion
}
