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
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin;

public class UsersEditModelTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly Mock<IAuthorizationService> _mockAuthService;
    private readonly Mock<IPersistedGrantStore> _mockGrantStore;
    private readonly Mock<IUserEditor> _mockUserEditor;
    private readonly Mock<IServerSideSessionStore> _mockSessionStore;

    public UsersEditModelTests()
    {
        _mockUserManager = TestHelpers.CreateMockUserManager();
        _mockAuthService = new Mock<IAuthorizationService>();
        _mockGrantStore = new Mock<IPersistedGrantStore>();
        _mockUserEditor = new Mock<IUserEditor>();
        _mockSessionStore = new Mock<IServerSideSessionStore>();

        SetupAuthorizationAlwaysSucceeds();
    }

    private EditModel CreateSut(bool includeSessionStore = true)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "mock"));

        var sut = new EditModel(
            _mockUserManager.Object,
            _mockAuthService.Object,
            _mockGrantStore.Object,
            _mockUserEditor.Object,
            includeSessionStore ? _mockSessionStore.Object : null)
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };

        return sut;
    }

    private void SetupAuthorizationAlwaysSucceeds()
    {
        _mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
    }

    private void SetupAuthorizationFails(string policyName)
    {
        _mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), policyName))
            .ReturnsAsync(AuthorizationResult.Failed());
    }

    private void SetupUserFound(string userId, ApplicationUser? user = null)
    {
        user ??= new ApplicationUser { Id = userId, UserName = "testuser" };
        _mockUserManager.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
    }

    private void SetupPopulatePageData()
    {
        _mockUserEditor.Setup(x => x.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserEditPageDataDto
            {
                Profile = new UserProfileEditViewModel { UserId = "123", Username = "testuser" }
            });
    }

    private void SetupUpdateSuccess()
    {
        _mockUserEditor.Setup(x => x.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success });
    }

    private void SetupFormContent(EditModel sut, params (string key, string value)[] formValues)
    {
        var dict = formValues.ToDictionary(k => k.key, v => new StringValues(v.value));
        sut.HttpContext.Request.ContentType = "application/x-www-form-urlencoded";
        sut.HttpContext.Request.Form = new FormCollection(dict);
    }

    #region Profile Tab

    [Fact]
    public async Task OnGetAsync_ShouldReturnPage_WhenUserExists()
    {
        // Arrange
        var sut = CreateSut();
        sut.UserId = "123";
        SetupUserFound("123");
        SetupPopulatePageData();

        // Act
        var result = await sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        sut.Input.Should().NotBeNull();
        sut.Input.UserId.Should().Be("123");
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var sut = CreateSut();
        sut.UserId = "999";
        _mockUserManager.Setup(x => x.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenServiceReturnsNull()
    {
        // Arrange
        var sut = CreateSut();
        sut.UserId = "123";
        SetupUserFound("123");
        _mockUserEditor.Setup(x => x.GetUserEditPageDataAsync(It.IsAny<UserEditPageDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserEditPageDataDto?)null);

        // Act
        var result = await sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UsersWrite);

        var result = await sut.OnPostAsync("123");

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnBadRequest_WhenNoUserIdResolvable()
    {
        var sut = CreateSut();
        sut.UserId = "";
        sut.Input = new ProfileViewModel { UserId = "" };

        var result = await sut.OnPostAsync("");

        result.Should().BeOfType<BadRequestResult>();
    }

    [Theory]
    [InlineData("paramId", "", "", "paramId")]
    [InlineData("", "propId", "", "propId")]
    [InlineData("", "", "inputId", "inputId")]
    public async Task OnPostAsync_ShouldResolveUserId_FromFallbackChain(string param, string prop, string inputId, string expectedId)
    {
        var sut = CreateSut();
        sut.UserId = prop;
        if (sut.Input != null)
        {
            sut.Input.UserId = inputId;
        }
        else
        {
            sut.Input = new ProfileViewModel { UserId = inputId };
        }
        
        SetupUserFound(expectedId);
        SetupUpdateSuccess();

        var result = await sut.OnPostAsync(param);

        result.Should().BeOfType<RedirectToPageResult>();
        sut.UserId.Should().Be(expectedId);
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPage_WhenModelStateInvalid()
    {
        var sut = CreateSut();
        sut.UserId = "123";
        SetupUserFound("123");
        SetupPopulatePageData();
        sut.ModelState.AddModelError("Error", "Invalid");

        var result = await sut.OnPostAsync("123");

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldCallUserEditor_WithCorrectRequest_OnSuccess()
    {
        var sut = CreateSut();
        sut.UserId = "123";
        sut.Input = new ProfileViewModel { Username = "updated" };
        sut.NewPassword = "newpassword";
        SetupFormContent(sut, ("LockoutEnabledInput", "true"));
        sut.LockoutEnabledInput = true;
        
        SetupUserFound("123");
        
        UserEditPostUpdateRequest? capturedRequest = null;
        _mockUserEditor.Setup(x => x.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success })
            .Callback<UserEditPostUpdateRequest>(r => capturedRequest = r);

        var result = await sut.OnPostAsync("123");

        result.Should().BeOfType<RedirectToPageResult>();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.UserId.Should().Be("123");
        capturedRequest.Profile.Should().NotBeNull();
        capturedRequest.Profile!.Username.Should().Be("updated");
        capturedRequest.NewPassword.Should().Be("newpassword");
        capturedRequest.LockoutEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostAsync_ShouldAddConcurrencyMessage_WhenConcurrencyFailure()
    {
        var sut = CreateSut();
        sut.UserId = "123";
        SetupUserFound("123");
        SetupPopulatePageData();
        
        _mockUserEditor.Setup(x => x.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult 
            { 
                UserFound = true, 
                Result = IdentityResult.Failed(new IdentityError { Code = "ConcurrencyFailure", Description = "Concurrency conflict" }) 
            });

        var result = await sut.OnPostAsync("123");

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors).Should().Contain(e => e.ErrorMessage.Contains("modified by another administrator"));
    }

    #endregion

    #region Delete

    [Fact]
    public async Task OnPostDeleteAsync_ShouldReturnBadRequest_WhenSelfDelete()
    {
        var sut = CreateSut();
        var adminUser = new ApplicationUser { Id = "123", UserName = "admin" };
        SetupUserFound("123", adminUser);
        sut.UserId = "123";

        var result = await sut.OnPostDeleteAsync("123");

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnPostDeleteAsync_ShouldDeleteAndRedirect_OnSuccess()
    {
        var sut = CreateSut();
        var user = new ApplicationUser { Id = "123", UserName = "target" };
        SetupUserFound("123", user);
        _mockUserManager.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostDeleteAsync("123");

        var redirectResult = result.Should().BeOfType<RedirectResult>().Subject;
        redirectResult.Url.Should().Be("/Admin/Users");
        _mockUserManager.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    #endregion

    #region Claims Tab

    [Fact]
    public async Task OnPostClaimsAddAsync_ShouldReturnPageWithErrors_WhenClaimTypeEmpty()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.NewClaimType = "";
        SetupPopulatePageData();

        var result = await sut.OnPostClaimsAddAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostClaimsAddAsync_ShouldAddClaimAndRedirect_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.NewClaimType = "role";
        sut.NewClaimValue = "admin";

        _mockUserManager.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostClaimsAddAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.AddClaimAsync(It.IsAny<ApplicationUser>(), It.Is<Claim>(c => c.Type == "role" && c.Value == "admin")), Times.Once);
    }

    [Fact]
    public async Task OnPostClaimsRemoveAsync_ShouldParseClaims_AndRemove_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedClaims = new List<string> { "role:admin", "customclaim" };

        _mockUserManager.Setup(x => x.RemoveClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostClaimsRemoveAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.RemoveClaimsAsync(
            It.IsAny<ApplicationUser>(), 
            It.Is<IEnumerable<Claim>>(claims => 
                claims.Any(c => c.Type == "role" && c.Value == "admin") && 
                claims.Any(c => c.Type == "customclaim" && c.Value == ""))), 
            Times.Once);
    }

    [Fact]
    public async Task OnPostClaimsReplaceAsync_ShouldReplaceClaimAndRedirect_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.OldClaimType = "oldType";
        sut.OldClaimValue = "oldValue";
        sut.ReplacementClaimType = "newType";
        sut.ReplacementClaimValue = "newValue";

        _mockUserManager.Setup(x => x.ReplaceClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostClaimsReplaceAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.ReplaceClaimAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<Claim>(c => c.Type == "oldType" && c.Value == "oldValue"),
            It.Is<Claim>(c => c.Type == "newType" && c.Value == "newValue")), 
            Times.Once);
    }

    [Fact]
    public async Task OnPostClaimsAddAsync_ShouldReturnPageWithErrors_WhenAddClaimFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.NewClaimType = "role";
        sut.NewClaimValue = "admin";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Claim add failed" }));

        var result = await sut.OnPostClaimsAddAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Claim add failed");
        sut.TempData["Success"].Should().BeNull();
    }

    [Fact]
    public async Task OnPostClaimsRemoveAsync_ShouldReturnPageWithErrors_WhenRemoveClaimsFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedClaims = new List<string> { "role:admin" };
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.RemoveClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Claims remove failed" }));

        var result = await sut.OnPostClaimsRemoveAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Claims remove failed");
        sut.TempData["Success"].Should().BeNull();
    }

    [Fact]
    public async Task OnPostClaimsReplaceAsync_ShouldReturnPageWithErrors_WhenReplaceClaimFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.OldClaimType = "oldType";
        sut.OldClaimValue = "oldValue";
        sut.ReplacementClaimType = "newType";
        sut.ReplacementClaimValue = "newValue";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.ReplaceClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>(), It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Replace failed" }));

        var result = await sut.OnPostClaimsReplaceAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Replace failed");
        sut.TempData["Success"].Should().BeNull();
    }

    #endregion

    #region Roles Tab

    [Fact]
    public async Task OnPostRolesAddAsync_ShouldAddRolesAndRedirect_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedRolesToAdd = new List<string> { "Admin", "User" };

        _mockUserManager.Setup(x => x.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostRolesAddAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.AddToRolesAsync(It.IsAny<ApplicationUser>(), sut.SelectedRolesToAdd), Times.Once);
    }

    [Fact]
    public async Task OnPostRolesRemoveAsync_ShouldRemoveRolesAndRedirect_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedRolesToRemove = new List<string> { "Guest" };

        _mockUserManager.Setup(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostRolesRemoveAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), sut.SelectedRolesToRemove), Times.Once);
    }

    [Fact]
    public async Task OnPostRolesAddAsync_ShouldReturnPageWithErrors_WhenAddToRolesFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedRolesToAdd = new List<string> { "Admin" };
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.AddToRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role add failed" }));

        var result = await sut.OnPostRolesAddAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Role add failed");
        sut.TempData["Success"].Should().BeNull();
    }

    [Fact]
    public async Task OnPostRolesRemoveAsync_ShouldReturnPageWithErrors_WhenRemoveFromRolesFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedRolesToRemove = new List<string> { "Guest" };
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Role remove failed" }));

        var result = await sut.OnPostRolesRemoveAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Role remove failed");
        sut.TempData["Success"].Should().BeNull();
    }

    #endregion

    #region Security Tab

    [Fact]
    public async Task OnPostSecurityDisableAccountAsync_ShouldSetLockoutToMaxValue_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), DateTimeOffset.MaxValue))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostSecurityDisableAccountAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), DateTimeOffset.MaxValue), Times.Once);
    }

    [Fact]
    public async Task OnPostSecurityDisableAccountAsync_ShouldReturnPageWithErrors_WhenSetLockoutFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), DateTimeOffset.MaxValue))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Lockout not supported" }));

        var result = await sut.OnPostSecurityDisableAccountAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Lockout not supported");
        sut.TempData["Success"].Should().BeNull();
    }

    [Fact]
    public async Task OnPostSecurityEnableAccountAsync_ShouldReturnPageWithErrors_WhenSetLockoutFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Enable failed" }));

        var result = await sut.OnPostSecurityEnableAccountAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Enable failed");
        sut.TempData["Success"].Should().BeNull();
    }

    [Fact]
    public async Task OnPostSecurityClearLockoutAsync_ShouldClearLockoutAndResetCount_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostSecurityClearLockoutAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null), Times.Once);
        _mockUserManager.Verify(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task OnPostSecurityClearLockoutAsync_ShouldReturnPageWithErrors_WhenSetLockoutFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Lockout clear failed" }));

        var result = await sut.OnPostSecurityClearLockoutAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Lockout clear failed");
        _mockUserManager.Verify(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
        sut.TempData["Success"].Should().BeNull();
    }

    // TODO: This test documents a partial-update risk — lockout is cleared (first call succeeds)
    // but failed access count remains elevated (second call fails), leaving the user in an
    // inconsistent state. See the corresponding TODO in Edit.cshtml.cs OnPostSecurityClearLockoutAsync.
    [Fact]
    public async Task OnPostSecurityClearLockoutAsync_ShouldReturnPageWithErrors_WhenResetFailedCountFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null))
            .ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Reset count failed" }));

        var result = await sut.OnPostSecurityClearLockoutAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Reset count failed");
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null), Times.Once);
        sut.TempData["Success"].Should().BeNull();
    }

    [Fact]
    public async Task OnPostSecurityResetFailedCountAsync_ShouldReturnPageWithErrors_WhenResetFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Reset failed" }));

        var result = await sut.OnPostSecurityResetFailedCountAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Reset failed");
        sut.TempData["Success"].Should().BeNull();
    }

    [Fact]
    public async Task OnPostSecurityResetAuthenticatorAsync_ShouldReturnPageWithErrors_WhenResetFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.ResetAuthenticatorKeyAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Authenticator reset failed" }));

        var result = await sut.OnPostSecurityResetAuthenticatorAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Authenticator reset failed");
        sut.TempData["Success"].Should().BeNull();
    }

    [Fact]
    public async Task OnPostSecurityForceSignOutAsync_ShouldReturnPageWithErrors_WhenUpdateStampFails()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Stamp update failed" }));

        var result = await sut.OnPostSecurityForceSignOutAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().ContainSingle(e => e.ErrorMessage == "Stamp update failed");
        sut.TempData["Success"].Should().BeNull();
    }

    [Theory]
    [InlineData(true, "Lockout enabled")]
    [InlineData(false, "Lockout disabled")]
    public async Task OnPostSecurityToggleLockoutEnabledAsync_ShouldSetCorrectSuccessMessage(bool isEnabled, string expectedMessage)
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.LockoutEnabledInput = isEnabled;
        SetupUpdateSuccess();

        var result = await sut.OnPostSecurityToggleLockoutEnabledAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        sut.TempData["Success"].Should().Be(expectedMessage);
    }

    [Fact]
    public async Task OnPostSecurityForceSignOutAsync_ShouldUpdateSecurityStamp_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";

        _mockUserManager.Setup(x => x.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostSecurityForceSignOutAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    #endregion

    #region Grants/Sessions Tab

    [Fact]
    public async Task OnPostGrantsSessionsRevokeGrantAsync_ShouldRemoveGrant_WhenKeyProvided()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.GrantKey = "grant-123";

        var result = await sut.OnPostGrantsSessionsRevokeGrantAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockGrantStore.Verify(x => x.RemoveAsync("grant-123"), Times.Once);
    }

    [Fact]
    public async Task OnPostGrantsSessionsRevokeAllGrantsAsync_ShouldRemoveAllGrants_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";

        var result = await sut.OnPostGrantsSessionsRevokeAllGrantsAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockGrantStore.Verify(x => x.RemoveAllAsync(It.Is<PersistedGrantFilter>(f => f.SubjectId == "123")), Times.Once);
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndSessionAsync_ShouldDeleteSession_WhenStoreExistsAndKeyProvided()
    {
        var sut = CreateSut(includeSessionStore: true);
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SessionKey = "session-123";

        var result = await sut.OnPostGrantsSessionsEndSessionAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockSessionStore.Verify(x => x.DeleteSessionAsync("session-123", default), Times.Once);
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndSessionAsync_ShouldNotThrow_WhenSessionStoreIsNull()
    {
        var sut = CreateSut(includeSessionStore: false);
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SessionKey = "session-123";

        var result = await sut.OnPostGrantsSessionsEndSessionAsync();

        result.Should().BeOfType<RedirectToPageResult>(); // Graceful fallback
    }

    #endregion

    #region Authorization Denied — Forbid

    [Fact]
    public async Task OnPostDeleteAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UsersDelete);

        var result = await sut.OnPostDeleteAsync("123");

        result.Should().BeOfType<ForbidResult>();
        _mockUserManager.Verify(x => x.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task OnPostClaimsAddAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserClaimsWrite);

        var result = await sut.OnPostClaimsAddAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostClaimsRemoveAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserClaimsDelete);

        var result = await sut.OnPostClaimsRemoveAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostClaimsReplaceAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserClaimsWrite);

        var result = await sut.OnPostClaimsReplaceAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostRolesAddAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserRolesWrite);

        var result = await sut.OnPostRolesAddAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostRolesRemoveAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserRolesDelete);

        var result = await sut.OnPostRolesRemoveAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostGrantsSessionsRevokeGrantAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserGrantsDelete);

        var result = await sut.OnPostGrantsSessionsRevokeGrantAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostGrantsSessionsRevokeAllGrantsAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserGrantsDelete);

        var result = await sut.OnPostGrantsSessionsRevokeAllGrantsAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndSessionAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserSessionsDelete);

        var result = await sut.OnPostGrantsSessionsEndSessionAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndAllSessionsAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UserSessionsDelete);

        var result = await sut.OnPostGrantsSessionsEndAllSessionsAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    #endregion

    #region Security — Reset Password

    [Fact]
    public async Task OnPostSecurityResetPasswordAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UsersWrite);

        var result = await sut.OnPostSecurityResetPasswordAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityResetPasswordAsync_ShouldReturnPage_WhenPasswordEmpty()
    {
        var sut = CreateSut();
        sut.UserId = "123";
        sut.NewPassword = null;
        SetupUserFound("123");
        SetupPopulatePageData();

        var result = await sut.OnPostSecurityResetPasswordAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task OnPostSecurityResetPasswordAsync_ShouldCallUserEditor_WithPasswordIncluded_OnSuccess()
    {
        var sut = CreateSut();
        sut.UserId = "123";
        sut.NewPassword = "NewPass1!";
        SetupUserFound("123");

        UserEditPostUpdateRequest? capturedRequest = null;
        _mockUserEditor.Setup(x => x.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = true, Result = IdentityResult.Success })
            .Callback<UserEditPostUpdateRequest>(r => capturedRequest = r);

        var result = await sut.OnPostSecurityResetPasswordAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.NewPassword.Should().Be("NewPass1!");
        sut.TempData["Success"].Should().NotBeNull();
    }

    #endregion

    #region Security — Enable Account

    [Fact]
    public async Task OnPostSecurityEnableAccountAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UsersWrite);

        var result = await sut.OnPostSecurityEnableAccountAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityEnableAccountAsync_ShouldSetLockoutToNull_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";

        _mockUserManager.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostSecurityEnableAccountAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null), Times.Once);
        sut.TempData["Success"].Should().NotBeNull();
    }

    #endregion

    #region Security — Reset Failed Count

    [Fact]
    public async Task OnPostSecurityResetFailedCountAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UsersWrite);

        var result = await sut.OnPostSecurityResetFailedCountAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityResetFailedCountAsync_ShouldResetCount_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";

        _mockUserManager.Setup(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostSecurityResetFailedCountAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    #endregion

    #region Security — Toggle Two-Factor

    [Fact]
    public async Task OnPostSecurityToggleTwoFactorAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UsersWrite);

        var result = await sut.OnPostSecurityToggleTwoFactorAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Theory]
    [InlineData(true, "Two-factor authentication enabled")]
    [InlineData(false, "Two-factor authentication disabled")]
    public async Task OnPostSecurityToggleTwoFactorAsync_ShouldSetCorrectSuccessMessage(bool isEnabled, string expectedMessage)
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.TwoFactorEnabledInput = isEnabled;
        SetupUpdateSuccess();

        var result = await sut.OnPostSecurityToggleTwoFactorAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        sut.TempData["Success"].Should().Be(expectedMessage);
    }

    #endregion

    #region Security — Reset Authenticator

    [Fact]
    public async Task OnPostSecurityResetAuthenticatorAsync_ShouldReturnForbid_WhenNotAuthorized()
    {
        var sut = CreateSut();
        SetupAuthorizationFails(UserPolicyConstants.UsersWrite);

        var result = await sut.OnPostSecurityResetAuthenticatorAsync();

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task OnPostSecurityResetAuthenticatorAsync_ShouldResetKey_OnSuccess()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";

        _mockUserManager.Setup(x => x.ResetAuthenticatorKeyAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await sut.OnPostSecurityResetAuthenticatorAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.ResetAuthenticatorKeyAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    #endregion

    #region Grants/Sessions — End All Sessions

    [Fact]
    public async Task OnPostGrantsSessionsEndAllSessionsAsync_ShouldDeleteAllSessions_WhenStoreExists()
    {
        var sut = CreateSut(includeSessionStore: true);
        SetupUserFound("123");
        sut.UserId = "123";

        var result = await sut.OnPostGrantsSessionsEndAllSessionsAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _mockSessionStore.Verify(
            x => x.DeleteSessionsAsync(It.Is<SessionFilter>(f => f.SubjectId == "123"), default),
            Times.Once);
    }

    [Fact]
    public async Task OnPostGrantsSessionsEndAllSessionsAsync_ShouldNotThrow_WhenSessionStoreIsNull()
    {
        var sut = CreateSut(includeSessionStore: false);
        SetupUserFound("123");
        sut.UserId = "123";

        var result = await sut.OnPostGrantsSessionsEndAllSessionsAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    #endregion

    #region Failure Paths

    [Fact]
    public async Task OnPostDeleteAsync_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        var sut = CreateSut();
        sut.UserId = "999";
        _mockUserManager.Setup(x => x.FindByIdAsync("999")).ReturnsAsync((ApplicationUser?)null);

        var result = await sut.OnPostDeleteAsync("999");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostDeleteAsync_ShouldReturnPage_WhenDeleteFails()
    {
        var sut = CreateSut();
        var user = new ApplicationUser { Id = "123", UserName = "target" };
        SetupUserFound("123", user);
        sut.UserId = "123";
        SetupPopulatePageData();

        _mockUserManager.Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed" }));

        var result = await sut.OnPostDeleteAsync("123");

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors).Should().ContainSingle(e => e.ErrorMessage == "Delete failed");
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnNotFound_WhenUpdateReturnsUserNotFound()
    {
        var sut = CreateSut();
        sut.UserId = "123";
        SetupUserFound("123");

        _mockUserEditor.Setup(x => x.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult { UserFound = false, Result = IdentityResult.Success });

        var result = await sut.OnPostAsync("123");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPage_WhenUpdateFailsWithNonConcurrencyError()
    {
        var sut = CreateSut();
        sut.UserId = "123";
        SetupUserFound("123");
        SetupPopulatePageData();

        _mockUserEditor.Setup(x => x.UpdateUserFromEditPostAsync(It.IsAny<UserEditPostUpdateRequest>()))
            .ReturnsAsync(new UserProfileUpdateResult
            {
                UserFound = true,
                Result = IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Token is invalid" })
            });

        var result = await sut.OnPostAsync("123");

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().Contain(e => e.ErrorMessage == "Token is invalid");
    }

    #endregion

    #region Empty Selection Validation

    [Fact]
    public async Task OnPostClaimsRemoveAsync_ShouldReturnPageWithError_WhenSelectionEmpty()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedClaims = new List<string>();
        SetupPopulatePageData();

        var result = await sut.OnPostClaimsRemoveAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().Contain(e => e.ErrorMessage.Contains("Select at least one claim"));
    }

    [Fact]
    public async Task OnPostRolesAddAsync_ShouldReturnPageWithError_WhenSelectionEmpty()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedRolesToAdd = new List<string>();
        SetupPopulatePageData();

        var result = await sut.OnPostRolesAddAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().Contain(e => e.ErrorMessage.Contains("Select at least one role"));
    }

    [Fact]
    public async Task OnPostRolesRemoveAsync_ShouldReturnPageWithError_WhenSelectionEmpty()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.SelectedRolesToRemove = new List<string>();
        SetupPopulatePageData();

        var result = await sut.OnPostRolesRemoveAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().Contain(e => e.ErrorMessage.Contains("Select at least one role"));
    }

    [Fact]
    public async Task OnPostClaimsReplaceAsync_ShouldReturnPageWithError_WhenClaimTypesEmpty()
    {
        var sut = CreateSut();
        SetupUserFound("123");
        sut.UserId = "123";
        sut.OldClaimType = null;
        sut.ReplacementClaimType = null;
        SetupPopulatePageData();

        var result = await sut.OnPostClaimsReplaceAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Values.SelectMany(v => v.Errors)
            .Should().Contain(e => e.ErrorMessage == "Claim type is required");
    }

    #endregion
}
