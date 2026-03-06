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

        // Default: any GetClientForEditAsync returns standard view model
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(It.IsAny<int>()))
            .ReturnsAsync(CreateTestViewModel());

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
            AllowedGrantTypes = ["authorization_code"],
            RedirectUris = ["https://example.com/callback"],
            PostLogoutRedirectUris = ["https://example.com/logout"],
            AllowedScopes = ["openid", "profile"],
            AvailableScopes = ["openid", "profile", "email"],
            AvailableGrantTypes = ["authorization_code", "client_credentials"],
        };
    }

    private void ArrangePage(int id, ClientEditViewModel? input = null, string? modelError = null)
    {
        _pageModel.Id = id;
        _pageModel.Input = input ?? CreateTestViewModel();
        if (modelError != null) _pageModel.ModelState.AddModelError("Input.ClientName", modelError);
    }

    private void AssertRedirectToEdit(IActionResult result, int expectedId, string expectedMessage)
    {
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Clients/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(expectedId);
        _pageModel.TempData["Success"].Should().Be(expectedMessage);
    }

    [Fact]
    public async Task OnGetAsync_ClientFound_ReturnsPageWithPopulatedInput()
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
    public async Task OnGetAsync_ClientMissing_ReturnsNotFound()
    {
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(999))
            .ReturnsAsync((ClientEditViewModel?)null);
        _pageModel.Id = 999;

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_UpdateSucceeds_RedirectsWithSuccessMessage()
    {
        ArrangePage(3);
        _mockClientAdminService
            .Setup(s => s.UpdateClientAsync(3, _pageModel.Input))
            .ReturnsAsync(true);

        var result = await _pageModel.OnPostAsync();

        AssertRedirectToEdit(result, 3, "Client updated successfully");
    }

    [Fact]
    public async Task OnPostAsync_UpdateFails_ReturnsNotFound()
    {
        ArrangePage(1);
        _mockClientAdminService
            .Setup(s => s.UpdateClientAsync(1, _pageModel.Input))
            .ReturnsAsync(false);

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        ArrangePage(1, modelError: "Required");

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_RepopulatesOptions()
    {
        ArrangePage(1, modelError: "Required");
        _pageModel.Input.AvailableScopes = []; // cleared

        var existingClient = CreateTestViewModel();
        existingClient.AvailableScopes = ["openid", "profile", "email"];
        existingClient.AvailableGrantTypes = ["authorization_code", "client_credentials"];
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(1))
            .ReturnsAsync(existingClient);

        await _pageModel.OnPostAsync();

        _pageModel.Input.AvailableScopes.Should().BeEquivalentTo(["openid", "profile", "email"]);
        _pageModel.Input.AvailableGrantTypes.Should().BeEquivalentTo(["authorization_code", "client_credentials"]);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_DoesNotCallUpdate()
    {
        ArrangePage(1, modelError: "Required");

        await _pageModel.OnPostAsync();

        _mockClientAdminService.Verify(s => s.UpdateClientAsync(It.IsAny<int>(), It.IsAny<ClientEditViewModel>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ClientReloadReturnsNull_RetainsExistingInputOptions()
    {
        ArrangePage(999, modelError: "Required");
        _mockClientAdminService
            .Setup(s => s.GetClientForEditAsync(999))
            .ReturnsAsync((ClientEditViewModel?)null);

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.Input.AvailableScopes.Should().BeEquivalentTo(["openid", "profile", "email"]);
        _pageModel.Input.AvailableGrantTypes.Should().BeEquivalentTo(["authorization_code", "client_credentials"]);
    }

    [Fact]
    public void OnPostAddRedirectUri_AddsEmptyRedirectUri()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.RedirectUris.Count;

        _pageModel.OnPostAddRedirectUri();

        _pageModel.Input.RedirectUris.Should().HaveCount(initialCount + 1);
        _pageModel.Input.RedirectUris.Last().Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveRedirectUri_IndexValid_RemovesRedirectUri()
    {
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.RedirectUris = ["https://a.com", "https://b.com", "https://c.com"];

        _pageModel.OnPostRemoveRedirectUri(1);

        _pageModel.Input.RedirectUris.Should().HaveCount(2);
        _pageModel.Input.RedirectUris.Should().NotContain("https://b.com");
    }

    [Fact]
    public void OnPostRemoveRedirectUri_IndexNegative_DoesNotRemove()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.RedirectUris.Count;

        _pageModel.OnPostRemoveRedirectUri(-1);

        _pageModel.Input.RedirectUris.Should().HaveCount(initialCount);
    }

    [Fact]
    public void OnPostRemoveRedirectUri_IndexOutOfRange_DoesNotRemove()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.RedirectUris.Count;

        _pageModel.OnPostRemoveRedirectUri(100);

        _pageModel.Input.RedirectUris.Should().HaveCount(initialCount);
    }

    [Fact]
    public void OnPostAddPostLogoutRedirectUri_AddsEmptyPostLogoutRedirectUri()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.PostLogoutRedirectUris.Count;

        _pageModel.OnPostAddPostLogoutRedirectUri();

        _pageModel.Input.PostLogoutRedirectUris.Should().HaveCount(initialCount + 1);
        _pageModel.Input.PostLogoutRedirectUris.Last().Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemovePostLogoutRedirectUri_IndexValid_RemovesPostLogoutRedirectUri()
    {
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.PostLogoutRedirectUris = ["https://a.com/logout", "https://b.com/logout"];

        _pageModel.OnPostRemovePostLogoutRedirectUri(0);

        _pageModel.Input.PostLogoutRedirectUris.Should().HaveCount(1);
        _pageModel.Input.PostLogoutRedirectUris[0].Should().Be("https://b.com/logout");
    }

    [Fact]
    public void OnPostRemovePostLogoutRedirectUri_IndexNegative_DoesNotRemove()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.PostLogoutRedirectUris.Count;

        _pageModel.OnPostRemovePostLogoutRedirectUri(-1);

        _pageModel.Input.PostLogoutRedirectUris.Should().HaveCount(initialCount);
    }

    [Fact]
    public void OnPostRemovePostLogoutRedirectUri_IndexOutOfRange_DoesNotRemove()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.PostLogoutRedirectUris.Count;

        _pageModel.OnPostRemovePostLogoutRedirectUri(100);

        _pageModel.Input.PostLogoutRedirectUris.Should().HaveCount(initialCount);
    }

    [Fact]
    public void OnPostAddAllowedScope_AddsEmptyAllowedScope()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedScopes.Count;

        _pageModel.OnPostAddAllowedScope();

        _pageModel.Input.AllowedScopes.Should().HaveCount(initialCount + 1);
        _pageModel.Input.AllowedScopes.Last().Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveAllowedScope_IndexValid_RemovesAllowedScope()
    {
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.AllowedScopes = ["openid", "profile", "email"];

        _pageModel.OnPostRemoveAllowedScope(2);

        _pageModel.Input.AllowedScopes.Should().HaveCount(2);
        _pageModel.Input.AllowedScopes.Should().NotContain("email");
    }

    [Fact]
    public void OnPostRemoveAllowedScope_IndexNegative_DoesNotRemove()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedScopes.Count;

        _pageModel.OnPostRemoveAllowedScope(-1);

        _pageModel.Input.AllowedScopes.Should().HaveCount(initialCount);
    }

    [Fact]
    public void OnPostRemoveAllowedScope_IndexOutOfRange_DoesNotRemove()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedScopes.Count;

        _pageModel.OnPostRemoveAllowedScope(100);

        _pageModel.Input.AllowedScopes.Should().HaveCount(initialCount);
    }

    [Fact]
    public void OnPostAddGrantType_AddsEmptyGrantType()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedGrantTypes.Count;

        _pageModel.OnPostAddGrantType();

        _pageModel.Input.AllowedGrantTypes.Should().HaveCount(initialCount + 1);
        _pageModel.Input.AllowedGrantTypes.Last().Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveGrantType_IndexValid_RemovesGrantType()
    {
        _pageModel.Input = CreateTestViewModel();
        _pageModel.Input.AllowedGrantTypes = ["authorization_code", "client_credentials"];

        _pageModel.OnPostRemoveGrantType(0);

        _pageModel.Input.AllowedGrantTypes.Should().HaveCount(1);
        _pageModel.Input.AllowedGrantTypes[0].Should().Be("client_credentials");
    }

    [Fact]
    public void OnPostRemoveGrantType_IndexNegative_DoesNotRemove()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedGrantTypes.Count;

        _pageModel.OnPostRemoveGrantType(-1);

        _pageModel.Input.AllowedGrantTypes.Should().HaveCount(initialCount);
    }

    [Fact]
    public void OnPostRemoveGrantType_IndexOutOfRange_DoesNotRemove()
    {
        _pageModel.Input = CreateTestViewModel();
        var initialCount = _pageModel.Input.AllowedGrantTypes.Count;

        _pageModel.OnPostRemoveGrantType(100);

        _pageModel.Input.AllowedGrantTypes.Should().HaveCount(initialCount);
    }
}
