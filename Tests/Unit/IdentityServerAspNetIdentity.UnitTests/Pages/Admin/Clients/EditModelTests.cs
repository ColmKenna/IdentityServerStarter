using IdentityServerAspNetIdentity.Pages.Admin.Clients;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Clients;

public class EditModelTests
{
    private readonly Mock<IClientAdminService> _mockClientAdminService;
    private readonly Mock<ITempDataDictionary> _mockTempData;
    private readonly EditModel _sut;

    public EditModelTests()
    {
        _mockClientAdminService = new Mock<IClientAdminService>();
        _mockTempData = new Mock<ITempDataDictionary>();

        _sut = new EditModel(_mockClientAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            TempData = _mockTempData.Object
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnPage_WhenClientExists()
    {
        // Arrange
        _sut.Id = 42;

        var client = new ClientEditViewModel
        {
            ClientId = "test-client",
            ClientName = "Test Client"
        };

        _mockClientAdminService
            .Setup(x => x.GetClientForEditAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.Should().BeSameAs(client);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenClientDoesNotExist()
    {
        // Arrange
        _sut.Id = 42;

        _mockClientAdminService
            .Setup(x => x.GetClientForEditAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClientEditViewModel?)null);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldRedirect_WhenEditUpdateSucceeds()
    {
        // Arrange
        _sut.Id = 42;
        _sut.Input = new ClientEditViewModel
        {
            ClientId = "test-client",
            ClientName = "Test Client"
        };

        _mockClientAdminService
            .Setup(x => x.UpdateClientAsync(42, _sut.Input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Clients/Edit");
        redirect.RouteValues!["id"].Should().Be(42);

        _mockTempData.VerifySet(t => t["Success"] = "Client updated successfully", Times.Once);
        _mockClientAdminService.Verify(x => x.UpdateClientAsync(42, _sut.Input, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPageAndRepopulateAvailableOptions_WhenModelStateIsInvalid()
    {
        // Arrange
        _sut.Id = 42;
        _sut.Input = new ClientEditViewModel
        {
            ClientId = "posted-client",
            ClientName = "Posted Client"
        };
        _sut.ModelState.AddModelError("Input.ClientId", "The Client ID field is required.");

        var existingClient = new ClientEditViewModel
        {
            AvailableScopes = ["openid", "profile", "api1"],
            AvailableGrantTypes = ["authorization_code", "client_credentials"]
        };

        _mockClientAdminService
            .Setup(x => x.GetClientForEditAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingClient);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.ClientId.Should().Be("posted-client");
        _sut.Input.ClientName.Should().Be("Posted Client");
        _sut.Input.AvailableScopes.Should().BeEquivalentTo(["openid", "profile", "api1"]);
        _sut.Input.AvailableGrantTypes.Should().BeEquivalentTo(["authorization_code", "client_credentials"]);

        _mockClientAdminService.Verify(x => x.UpdateClientAsync(It.IsAny<int>(), It.IsAny<ClientEditViewModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnNotFound_WhenUpdateFails()
    {
        // Arrange
        _sut.Id = 42;
        _sut.Input = new ClientEditViewModel
        {
            ClientId = "test-client",
            ClientName = "Test Client"
        };

        _mockClientAdminService
            .Setup(x => x.UpdateClientAsync(42, _sut.Input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    // --- RedirectUri handlers ---

    [Fact]
    public void OnPostAddRedirectUri_ShouldAddEmptyString_ToRedirectUris()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            RedirectUris = ["https://example.com/callback"]
        };

        // Act
        var result = _sut.OnPostAddRedirectUri();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.RedirectUris.Should().HaveCount(2);
        _sut.Input.RedirectUris[1].Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveRedirectUri_ShouldRemoveItem_WhenIndexIsValid()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            RedirectUris = ["https://example.com/callback"]
        };

        // Act
        var result = _sut.OnPostRemoveRedirectUri(0);

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.RedirectUris.Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveRedirectUri_ShouldLeaveCollectionUnchanged_WhenIndexIsOutOfRange()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            RedirectUris = ["https://example.com/callback"]
        };

        // Act
        var result = _sut.OnPostRemoveRedirectUri(5);

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.RedirectUris.Should().HaveCount(1);
        _sut.Input.RedirectUris[0].Should().Be("https://example.com/callback");
    }

    // --- PostLogoutRedirectUri handlers ---

    [Fact]
    public void OnPostAddPostLogoutRedirectUri_ShouldAddEmptyString_ToPostLogoutRedirectUris()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            PostLogoutRedirectUris = ["https://example.com/logout"]
        };

        // Act
        var result = _sut.OnPostAddPostLogoutRedirectUri();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.PostLogoutRedirectUris.Should().HaveCount(2);
        _sut.Input.PostLogoutRedirectUris[1].Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemovePostLogoutRedirectUri_ShouldRemoveItem_WhenIndexIsValid()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            PostLogoutRedirectUris = ["https://example.com/logout"]
        };

        // Act
        var result = _sut.OnPostRemovePostLogoutRedirectUri(0);

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.PostLogoutRedirectUris.Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemovePostLogoutRedirectUri_ShouldLeaveCollectionUnchanged_WhenIndexIsOutOfRange()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            PostLogoutRedirectUris = ["https://example.com/logout"]
        };

        // Act
        var result = _sut.OnPostRemovePostLogoutRedirectUri(5);

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.PostLogoutRedirectUris.Should().HaveCount(1);
        _sut.Input.PostLogoutRedirectUris[0].Should().Be("https://example.com/logout");
    }

    // --- AllowedScope handlers ---

    [Fact]
    public void OnPostAddAllowedScope_ShouldAddEmptyString_ToAllowedScopes()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            AllowedScopes = ["openid"]
        };

        // Act
        var result = _sut.OnPostAddAllowedScope();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.AllowedScopes.Should().HaveCount(2);
        _sut.Input.AllowedScopes[1].Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveAllowedScope_ShouldRemoveItem_WhenIndexIsValid()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            AllowedScopes = ["openid"]
        };

        // Act
        var result = _sut.OnPostRemoveAllowedScope(0);

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.AllowedScopes.Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveAllowedScope_ShouldLeaveCollectionUnchanged_WhenIndexIsOutOfRange()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            AllowedScopes = ["openid"]
        };

        // Act
        var result = _sut.OnPostRemoveAllowedScope(5);

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.AllowedScopes.Should().HaveCount(1);
        _sut.Input.AllowedScopes[0].Should().Be("openid");
    }

    // --- GrantType handlers ---

    [Fact]
    public void OnPostAddGrantType_ShouldAddEmptyString_ToAllowedGrantTypes()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            AllowedGrantTypes = ["authorization_code"]
        };

        // Act
        var result = _sut.OnPostAddGrantType();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.AllowedGrantTypes.Should().HaveCount(2);
        _sut.Input.AllowedGrantTypes[1].Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveGrantType_ShouldRemoveItem_WhenIndexIsValid()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            AllowedGrantTypes = ["authorization_code"]
        };

        // Act
        var result = _sut.OnPostRemoveGrantType(0);

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.AllowedGrantTypes.Should().BeEmpty();
    }

    [Fact]
    public void OnPostRemoveGrantType_ShouldLeaveCollectionUnchanged_WhenIndexIsOutOfRange()
    {
        // Arrange
        _sut.Input = new ClientEditViewModel
        {
            AllowedGrantTypes = ["authorization_code"]
        };

        // Act
        var result = _sut.OnPostRemoveGrantType(5);

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.AllowedGrantTypes.Should().HaveCount(1);
        _sut.Input.AllowedGrantTypes[0].Should().Be("authorization_code");
    }
}
