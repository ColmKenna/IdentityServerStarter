using IdentityServerAspNetIdentity.Pages.Admin.IdentityResources;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.IdentityResources;

public class IndexModelTests
{
    private readonly Mock<IIdentityResourcesAdminService> _mockIdentityResourcesAdminService;
    private readonly IndexModel _sut;

    public IndexModelTests()
    {
        _mockIdentityResourcesAdminService = new Mock<IIdentityResourcesAdminService>();

        _sut = new IndexModel(_mockIdentityResourcesAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateIdentityResources_WhenServiceReturnsData()
    {
        // Arrange
        var expectedResources = new List<IdentityResourceListItemDto>
        {
            new() { Id = 1, Name = "email", DisplayName = "Email", Description = "Your email address", Enabled = true },
            new() { Id = 2, Name = "profile", DisplayName = "User profile", Description = "Your user profile data", Enabled = false }
        };

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResources);

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.IdentityResources.Should().HaveCount(2);
        _sut.IdentityResources.Should().BeEquivalentTo(expectedResources);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnEmptyList_WhenServiceReturnsNoResources()
    {
        // Arrange
        _mockIdentityResourcesAdminService
            .Setup(x => x.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdentityResourceListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.IdentityResources.Should().BeEmpty();
    }
}
