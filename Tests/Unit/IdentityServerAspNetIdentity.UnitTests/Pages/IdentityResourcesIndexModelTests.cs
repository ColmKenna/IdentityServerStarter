using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class IdentityResourcesIndexModelTests
{
    private readonly Mock<IIdentityResourcesAdminService> _mockService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel _pageModel;

    public IdentityResourcesIndexModelTests()
    {
        _mockService = new Mock<IIdentityResourcesAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel(_mockService.Object);
    }

    [Fact]
    public async Task OnGetAsync_InitializesIdentityResourcesCollection()
    {
        _mockService
            .Setup(service => service.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdentityResourceListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.IdentityResources.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsResources_PopulatesIdentityResources()
    {
        _mockService
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
    public async Task OnGetAsync_ServiceReturnsOrderedData_PreservesOrder()
    {
        _mockService
            .Setup(service => service.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IdentityResourceListItemDto>
            {
                new() { Id = 1, Name = "address", DisplayName = "Address", Description = "A", Enabled = true },
                new() { Id = 2, Name = "openid", DisplayName = "OpenID", Description = "O", Enabled = true },
                new() { Id = 3, Name = "profile", DisplayName = "Profile", Description = "P", Enabled = true }
            });

        await _pageModel.OnGetAsync();

        _pageModel.IdentityResources.Select(r => r.Name).Should().Equal("address", "openid", "profile");
    }

    [Fact]
    public async Task OnGetAsync_NoResources_ReturnsEmptyList()
    {
        _mockService
            .Setup(service => service.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IdentityResourceListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.IdentityResources.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_CallsServiceOnce()
    {
        _mockService
            .Setup(service => service.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<IdentityResourceListItemDto>());

        await _pageModel.OnGetAsync();

        _mockService.Verify(
            service => service.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
