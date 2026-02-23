using IdentityServerAspNetIdentity.Pages.Admin.Clients;
using IdentityServerServices;
using Microsoft.AspNetCore.Http;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ClientsEditModelTests : IDisposable
{
    private readonly EditModel _pageModel;
    private readonly Mock<IClientEditor> _mockClientEditor;

    public ClientsEditModelTests()
    {
        _mockClientEditor = new Mock<IClientEditor>();

        _pageModel = new EditModel(_mockClientEditor.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    public void Dispose()
    {
        // Cleanup
    }

    // ============================================================================
    // Page Model Tests - Testing core page flow with mocks
    // ============================================================================

    [Fact]
    public async Task should_return_not_found_when_client_editor_returns_null()
    {
        // Arrange
        _mockClientEditor.Setup(x => x.GetClientForEditAsync(It.IsAny<int>()))
            .ReturnsAsync((IdentityServerServices.ViewModels.ClientEditViewModel?)null);

        // Act
        var result = await _pageModel.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task should_have_valid_input_when_client_editor_returns_viewmodel()
    {
        // Arrange
        var viewModel = new IdentityServerServices.ViewModels.ClientEditViewModel
        {
            ClientId = "test-client",
            ClientName = "Test Client",
            Description = "A test client",
            Enabled = true
        };
        _mockClientEditor.Setup(x => x.GetClientForEditAsync(1))
            .ReturnsAsync(viewModel);
        _pageModel.Id = 1;

        // Act
        await _pageModel.OnGetAsync();

        // Assert
        _pageModel.Input.Should().NotBeNull();
        _pageModel.Input.ClientId.Should().Be("test-client");
    }

    [Fact]
    public void should_initialize_with_client_editor()
    {
        // Assert - verify mock was injected
        _mockClientEditor.Should().NotBeNull();
        _pageModel.Should().NotBeNull();
    }
}


