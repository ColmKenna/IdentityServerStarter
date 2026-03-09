using IdentityServerAspNetIdentity.Pages.Admin.Clients;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Clients;

public class EditModelTests
{
    private readonly Mock<IClientAdminService> _clientAdminService = new();

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenClientDoesNotExist()
    {
        var sut = CreateSut();
        sut.Id = 42;

        _clientAdminService.Setup(x => x.GetClientForEditAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientEditViewModel?)null);

        var result = await sut.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateInputAndReturnPage_WhenClientExists()
    {
        var sut = CreateSut();
        sut.Id = 42;
        var client = CreateValidInput();
        client.ClientId = "interactive-client";
        client.ClientName = "Interactive Client";
        client.AllowedGrantTypes = ["authorization_code"];
        client.AllowedScopes = ["openid", "api.read"];

        _clientAdminService.Setup(x => x.GetClientForEditAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        var result = await sut.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        sut.Input.Should().BeEquivalentTo(client);
    }

    [Fact]
    public async Task OnPostAsync_ShouldRehydrateAvailableOptionsAndReturnPage_WhenModelStateIsInvalid()
    {
        var sut = CreateSut();
        sut.Id = 7;
        sut.Input = CreateValidInput();
        sut.Input.ClientName = "Edited Client";
        sut.ModelState.AddModelError("Input.ClientId", "Required");

        var existingClient = CreateValidInput();
        existingClient.AvailableScopes = ["openid", "api.read"];
        existingClient.AvailableGrantTypes = ["authorization_code", "client_credentials"];

        _clientAdminService.Setup(x => x.GetClientForEditAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingClient);

        var result = await sut.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        sut.Input.ClientName.Should().Be("Edited Client");
        sut.Input.AvailableScopes.Should().Equal("openid", "api.read");
        sut.Input.AvailableGrantTypes.Should().Equal("authorization_code", "client_credentials");
        _clientAdminService.Verify(
            x => x.UpdateClientAsync(It.IsAny<int>(), It.IsAny<ClientEditViewModel>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnNotFound_WhenUpdateReturnsFalse()
    {
        var sut = CreateSut();
        sut.Id = 7;
        sut.Input = CreateValidInput();

        _clientAdminService.Setup(x => x.UpdateClientAsync(7, sut.Input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await sut.OnPostAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldRedirectToEditPageAndSetSuccessMessage_WhenUpdateSucceeds()
    {
        var sut = CreateSut();
        sut.Id = 7;
        sut.Input = CreateValidInput();

        _clientAdminService.Setup(x => x.UpdateClientAsync(7, sut.Input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await sut.OnPostAsync();

        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/Clients/Edit");
        redirectResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(7);
        sut.TempData["Success"].Should().Be("Client updated successfully");
        _clientAdminService.Verify(x => x.UpdateClientAsync(7, sut.Input, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void OnPostAddRedirectUri_ShouldAppendEmptyItem_WhenInvoked()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();

        var result = sut.OnPostAddRedirectUri();

        result.Should().BeOfType<PageResult>();
        sut.Input.RedirectUris.Should().EndWith(string.Empty);
        sut.Input.RedirectUris.Should().HaveCount(2);
    }

    [Fact]
    public void OnPostRemoveRedirectUri_ShouldRemoveItem_WhenIndexValid()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();
        sut.Input.RedirectUris = ["https://one.test/callback", "https://two.test/callback"];

        var result = sut.OnPostRemoveRedirectUri(0);

        result.Should().BeOfType<PageResult>();
        sut.Input.RedirectUris.Should().Equal("https://two.test/callback");
    }

    [Fact]
    public void OnPostRemoveRedirectUri_ShouldLeaveCollectionUnchanged_WhenIndexInvalid()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();
        sut.Input.RedirectUris = ["https://one.test/callback"];

        var result = sut.OnPostRemoveRedirectUri(1);

        result.Should().BeOfType<PageResult>();
        sut.Input.RedirectUris.Should().Equal("https://one.test/callback");
    }

    [Fact]
    public void OnPostAddPostLogoutRedirectUri_ShouldAppendEmptyItem_WhenInvoked()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();

        var result = sut.OnPostAddPostLogoutRedirectUri();

        result.Should().BeOfType<PageResult>();
        sut.Input.PostLogoutRedirectUris.Should().EndWith(string.Empty);
        sut.Input.PostLogoutRedirectUris.Should().HaveCount(2);
    }

    [Fact]
    public void OnPostRemovePostLogoutRedirectUri_ShouldRemoveItem_WhenIndexValid()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();
        sut.Input.PostLogoutRedirectUris = ["https://one.test/logout", "https://two.test/logout"];

        var result = sut.OnPostRemovePostLogoutRedirectUri(0);

        result.Should().BeOfType<PageResult>();
        sut.Input.PostLogoutRedirectUris.Should().Equal("https://two.test/logout");
    }

    [Fact]
    public void OnPostRemovePostLogoutRedirectUri_ShouldLeaveCollectionUnchanged_WhenIndexInvalid()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();
        sut.Input.PostLogoutRedirectUris = ["https://one.test/logout"];

        var result = sut.OnPostRemovePostLogoutRedirectUri(-1);

        result.Should().BeOfType<PageResult>();
        sut.Input.PostLogoutRedirectUris.Should().Equal("https://one.test/logout");
    }

    [Fact]
    public void OnPostAddAllowedScope_ShouldAppendEmptyItem_WhenInvoked()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();

        var result = sut.OnPostAddAllowedScope();

        result.Should().BeOfType<PageResult>();
        sut.Input.AllowedScopes.Should().EndWith(string.Empty);
        sut.Input.AllowedScopes.Should().HaveCount(2);
    }

    [Fact]
    public void OnPostRemoveAllowedScope_ShouldRemoveItem_WhenIndexValid()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();
        sut.Input.AllowedScopes = ["openid", "api.read"];

        var result = sut.OnPostRemoveAllowedScope(1);

        result.Should().BeOfType<PageResult>();
        sut.Input.AllowedScopes.Should().Equal("openid");
    }

    [Fact]
    public void OnPostRemoveAllowedScope_ShouldLeaveCollectionUnchanged_WhenIndexInvalid()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();
        sut.Input.AllowedScopes = ["openid"];

        var result = sut.OnPostRemoveAllowedScope(2);

        result.Should().BeOfType<PageResult>();
        sut.Input.AllowedScopes.Should().Equal("openid");
    }

    [Fact]
    public void OnPostAddGrantType_ShouldAppendEmptyItem_WhenInvoked()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();

        var result = sut.OnPostAddGrantType();

        result.Should().BeOfType<PageResult>();
        sut.Input.AllowedGrantTypes.Should().EndWith(string.Empty);
        sut.Input.AllowedGrantTypes.Should().HaveCount(2);
    }

    [Fact]
    public void OnPostRemoveGrantType_ShouldRemoveItem_WhenIndexValid()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();
        sut.Input.AllowedGrantTypes = ["authorization_code", "client_credentials"];

        var result = sut.OnPostRemoveGrantType(0);

        result.Should().BeOfType<PageResult>();
        sut.Input.AllowedGrantTypes.Should().Equal("client_credentials");
    }

    [Fact]
    public void OnPostRemoveGrantType_ShouldLeaveCollectionUnchanged_WhenIndexInvalid()
    {
        var sut = CreateSut();
        sut.Input = CreateValidInput();
        sut.Input.AllowedGrantTypes = ["authorization_code"];

        var result = sut.OnPostRemoveGrantType(5);

        result.Should().BeOfType<PageResult>();
        sut.Input.AllowedGrantTypes.Should().Equal("authorization_code");
    }

    private EditModel CreateSut()
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState);
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);

        return new EditModel(_clientAdminService.Object)
        {
            PageContext = new PageContext(actionContext) { ViewData = viewData },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    private static ClientEditViewModel CreateValidInput()
    {
        return new ClientEditViewModel
        {
            ClientId = "interactive-client",
            ClientName = "Interactive Client",
            Description = "Interactive description",
            ClientUri = "https://app.test",
            LogoUri = "https://app.test/logo.png",
            Enabled = true,
            RequirePkce = true,
            RequireClientSecret = true,
            RequireConsent = false,
            AllowOfflineAccess = true,
            FrontChannelLogoutUri = "https://app.test/front-channel-logout",
            BackChannelLogoutUri = "https://app.test/back-channel-logout",
            AccessTokenLifetime = 3600,
            IdentityTokenLifetime = 300,
            SlidingRefreshTokenLifetime = 1296000,
            AlwaysIncludeUserClaimsInIdToken = false,
            AllowedGrantTypes = ["authorization_code"],
            RedirectUris = ["https://app.test/callback"],
            PostLogoutRedirectUris = ["https://app.test/logout"],
            AllowedScopes = ["openid"],
            AvailableGrantTypes = ["authorization_code", "client_credentials"],
            AvailableScopes = ["openid", "api.read"]
        };
    }
}
