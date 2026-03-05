using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class IdentityResourcesIndexModelTests
{
    private readonly Mock<IIdentityResourcesAdminService> _mockIdentityResourcesAdminService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel _pageModel;

    public IdentityResourcesIndexModelTests()
    {
        _mockIdentityResourcesAdminService = new Mock<IIdentityResourcesAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel(_mockIdentityResourcesAdminService.Object);
    }

    [Fact]
    public async Task OnGetAsync_InitializesIdentityResourcesCollection()
    {
        _mockIdentityResourcesAdminService
            .Setup(service => service.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdentityResourceListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.IdentityResources.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsResources_PopulatesIdentityResources()
    {
        _mockIdentityResourcesAdminService
            .Setup(service => service.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdentityResourceListItemDto>
            {
                new()
                {
                    Id = 1,
                    Name = "openid",
                    DisplayName = "OpenID Connect",
                    Description = "OpenID Connect scope",
                    Enabled = true
                },
                new()
                {
                    Id = 2,
                    Name = "profile",
                    DisplayName = "Profile",
                    Description = "User profile",
                    Enabled = false
                }
            });

        await _pageModel.OnGetAsync();

        _pageModel.IdentityResources.Should().HaveCount(2);
        _pageModel.IdentityResources.Select(r => r.Name).Should().Contain(new[] { "openid", "profile" });
    }

    [Fact]
    public async Task OnGetAsync_NoResources_ReturnsEmptyList()
    {
        _mockIdentityResourcesAdminService
            .Setup(service => service.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IdentityResourceListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.IdentityResources.Should().BeEmpty();
    }

}
