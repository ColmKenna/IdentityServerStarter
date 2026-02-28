using IdentityServerAspNetIdentity.Pages.Admin.Clients;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ClientsEditModelTests
{
    private readonly Mock<IClientAdminService> _mockClientAdminService;
    private readonly EditModel _pageModel;

    public ClientsEditModelTests()
    {
        _mockClientAdminService = new Mock<IClientAdminService>();
        _pageModel = new EditModel(_mockClientAdminService.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    private static ClientEditViewModel CreateTestViewModel(
        string clientId = "test-client",
        string clientName = "Test Client",
        string? description = "A test client",
        bool enabled = true)
    {
        return new ClientEditViewModel
        {
            ClientId = clientId,
            ClientName = clientName,
            Description = description,
            Enabled = enabled,
            RequirePkce = true,
            RequireClientSecret = true,
            AccessTokenLifetime = 3600,
            IdentityTokenLifetime = 300,
            SlidingRefreshTokenLifetime = 1296000,
            AllowedGrantTypes = new List<string> { "authorization_code" },
            RedirectUris = new List<string> { "https://example.com/callback" },
            PostLogoutRedirectUris = new List<string> { "https://example.com/logout" },
            AllowedScopes = new List<string> { "openid", "profile" },
            AvailableScopes = new List<string> { "openid", "profile", "email" },
            AvailableGrantTypes = new List<string> { "authorization_code", "client_credentials" },
        };
    }

    #region OnGetAsync Tests

    [Fact]
    public async Task Should_ReturnPageWithPopulatedInput_When_ClientFound()
    {
        var viewModel = CreateTestViewModel(clientId: "my-app", clientName: "My App");
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(5))
            .ReturnsAsync(viewModel);
        _pageModel.Id = 5;

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.Input.Should().BeSameAs(viewModel);
        _pageModel.Input.ClientId.Should().Be("my-app");
        _pageModel.Input.ClientName.Should().Be("My App");
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_ClientMissing_OnGet()
    {
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(999))
            .ReturnsAsync((ClientEditViewModel?)null);
        _pageModel.Id = 999;

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region OnPostAsync Tests

    [Fact]
    public async Task Should_RedirectWithSuccessMessage_When_UpdateSucceeds()
    {
        _pageModel.Id = 3;
        _pageModel.Input = CreateTestViewModel();
        _mockClientAdminService
            .Setup(s => s.UpdateClientAsync(3, _pageModel.Input))
            .ReturnsAsync(true);

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/Clients/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(3);
        _pageModel.TempData["Success"].Should().Be("Client updated successfully");
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_UpdateFails()
    {
        _pageModel.Id = 1;
        _pageModel.Input = CreateTestViewModel();
        _mockClientAdminService
            .Setup(s => s.UpdateClientAsync(1, _pageModel.Input))
            .ReturnsAsync(false);

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Should_ReturnPage_When_ModelStateInvalid()
    {
        _pageModel.Id = 1;
        _pageModel.Input = CreateTestViewModel();
        _pageModel.ModelState.AddModelError("Input.ClientName", "Required");

        var existingClient = CreateTestViewModel();
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(1))
            .ReturnsAsync(existingClient);

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task Should_RepopulateOptions_When_ModelStateInvalid()
    {
        _pageModel.Id = 1;
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.AvailableScopes = new List<string>(); // cleared
        _pageModel.ModelState.AddModelError("Input.ClientName", "Required");

        var existingClient = CreateTestViewModel();
        existingClient.AvailableScopes = new List<string> { "openid", "profile", "email" };
        existingClient.AvailableGrantTypes = new List<string> { "authorization_code", "client_credentials" };
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(1))
            .ReturnsAsync(existingClient);

        await _pageModel.OnPostAsync();

        _pageModel.Input.AvailableScopes.Should().BeEquivalentTo(new[] { "openid", "profile", "email" });
        _pageModel.Input.AvailableGrantTypes.Should().BeEquivalentTo(new[] { "authorization_code", "client_credentials" });
    }

    [Fact]
    public async Task Should_NotCallUpdate_When_ModelStateInvalid()
    {
        _pageModel.Id = 1;
        _pageModel.Input = CreateTestViewModel();
        _pageModel.ModelState.AddModelError("Input.ClientName", "Required");
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(1))
            .ReturnsAsync(CreateTestViewModel());

        await _pageModel.OnPostAsync();

        _mockClientAdminService.Verify(s => s.UpdateClientAsync(It.IsAny<int>(), It.IsAny<ClientEditViewModel>()), Times.Never);
    }

    [Fact]
    public async Task Should_ReturnPage_When_ModelStateInvalid_AndClientMissing()
    {
        _pageModel.Id = 999;
        _pageModel.Input = CreateTestViewModel();
        _pageModel.ModelState.AddModelError("Input.ClientName", "Required");
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(999))
            .ReturnsAsync((ClientEditViewModel?)null);

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
    }

    #endregion

    #region OnPostAddRedirectUri Tests

    [Fact]
    public void Should_AddEmptyRedirectUri_When_AddRedirectUriPosted()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.RedirectUris.Count;

        _pageModel.OnPostAddRedirectUri();

        _pageModel.Input.RedirectUris.Should().HaveCount(initialCount + 1);
        _pageModel.Input.RedirectUris.Last().Should().BeEmpty();
    }

    #endregion

    #region OnPostRemoveRedirectUri Tests

    [Fact]
    public void Should_RemoveRedirectUri_When_IndexValid()
    {
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.RedirectUris = new List<string> { "https://a.com", "https://b.com", "https://c.com" };

        _pageModel.OnPostRemoveRedirectUri(1);

        _pageModel.Input.RedirectUris.Should().HaveCount(2);
        _pageModel.Input.RedirectUris.Should().NotContain("https://b.com");
    }

    [Fact]
    public void Should_NotRemoveRedirectUri_When_IndexNegative()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.RedirectUris.Count;

        _pageModel.OnPostRemoveRedirectUri(-1);

        _pageModel.Input.RedirectUris.Should().HaveCount(initialCount);
    }

    [Fact]
    public void Should_NotRemoveRedirectUri_When_IndexOutOfRange()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.RedirectUris.Count;

        _pageModel.OnPostRemoveRedirectUri(100);

        _pageModel.Input.RedirectUris.Should().HaveCount(initialCount);
    }

    #endregion

    #region OnPostAddPostLogoutRedirectUri Tests

    [Fact]
    public void Should_AddEmptyPostLogoutRedirectUri_When_AddPostLogoutRedirectUriPosted()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.PostLogoutRedirectUris.Count;

        _pageModel.OnPostAddPostLogoutRedirectUri();

        _pageModel.Input.PostLogoutRedirectUris.Should().HaveCount(initialCount + 1);
        _pageModel.Input.PostLogoutRedirectUris.Last().Should().BeEmpty();
    }

    #endregion

    #region OnPostRemovePostLogoutRedirectUri Tests

    [Fact]
    public void Should_RemovePostLogoutRedirectUri_When_IndexValid()
    {
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.PostLogoutRedirectUris = new List<string> { "https://a.com/logout", "https://b.com/logout" };

        _pageModel.OnPostRemovePostLogoutRedirectUri(0);

        _pageModel.Input.PostLogoutRedirectUris.Should().HaveCount(1);
        _pageModel.Input.PostLogoutRedirectUris[0].Should().Be("https://b.com/logout");
    }

    [Fact]
    public void Should_NotRemovePostLogoutRedirectUri_When_IndexNegative()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.PostLogoutRedirectUris.Count;

        _pageModel.OnPostRemovePostLogoutRedirectUri(-1);

        _pageModel.Input.PostLogoutRedirectUris.Should().HaveCount(initialCount);
    }

    [Fact]
    public void Should_NotRemovePostLogoutRedirectUri_When_IndexOutOfRange()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.PostLogoutRedirectUris.Count;

        _pageModel.OnPostRemovePostLogoutRedirectUri(100);

        _pageModel.Input.PostLogoutRedirectUris.Should().HaveCount(initialCount);
    }

    #endregion

    #region OnPostAddAllowedScope Tests

    [Fact]
    public void Should_AddEmptyAllowedScope_When_AddAllowedScopePosted()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedScopes.Count;

        _pageModel.OnPostAddAllowedScope();

        _pageModel.Input.AllowedScopes.Should().HaveCount(initialCount + 1);
        _pageModel.Input.AllowedScopes.Last().Should().BeEmpty();
    }

    #endregion

    #region OnPostRemoveAllowedScope Tests

    [Fact]
    public void Should_RemoveAllowedScope_When_IndexValid()
    {
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.AllowedScopes = new List<string> { "openid", "profile", "email" };

        _pageModel.OnPostRemoveAllowedScope(2);

        _pageModel.Input.AllowedScopes.Should().HaveCount(2);
        _pageModel.Input.AllowedScopes.Should().NotContain("email");
    }

    [Fact]
    public void Should_NotRemoveAllowedScope_When_IndexNegative()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedScopes.Count;

        _pageModel.OnPostRemoveAllowedScope(-1);

        _pageModel.Input.AllowedScopes.Should().HaveCount(initialCount);
    }

    [Fact]
    public void Should_NotRemoveAllowedScope_When_IndexOutOfRange()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedScopes.Count;

        _pageModel.OnPostRemoveAllowedScope(100);

        _pageModel.Input.AllowedScopes.Should().HaveCount(initialCount);
    }

    #endregion

    #region OnPostAddGrantType Tests

    [Fact]
    public void Should_AddEmptyGrantType_When_AddGrantTypePosted()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedGrantTypes.Count;

        _pageModel.OnPostAddGrantType();

        _pageModel.Input.AllowedGrantTypes.Should().HaveCount(initialCount + 1);
        _pageModel.Input.AllowedGrantTypes.Last().Should().BeEmpty();
    }

    #endregion

    #region OnPostRemoveGrantType Tests

    [Fact]
    public void Should_RemoveGrantType_When_IndexValid()
    {
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.AllowedGrantTypes = new List<string> { "authorization_code", "client_credentials" };

        _pageModel.OnPostRemoveGrantType(0);

        _pageModel.Input.AllowedGrantTypes.Should().HaveCount(1);
        _pageModel.Input.AllowedGrantTypes[0].Should().Be("client_credentials");
    }

    [Fact]
    public void Should_NotRemoveGrantType_When_IndexNegative()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedGrantTypes.Count;

        _pageModel.OnPostRemoveGrantType(-1);

        _pageModel.Input.AllowedGrantTypes.Should().HaveCount(initialCount);
    }

    [Fact]
    public void Should_NotRemoveGrantType_When_IndexOutOfRange()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedGrantTypes.Count;

        _pageModel.OnPostRemoveGrantType(100);

        _pageModel.Input.AllowedGrantTypes.Should().HaveCount(initialCount);
    }

    #endregion
}
