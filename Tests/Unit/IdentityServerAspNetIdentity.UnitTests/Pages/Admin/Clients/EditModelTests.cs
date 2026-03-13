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
}
