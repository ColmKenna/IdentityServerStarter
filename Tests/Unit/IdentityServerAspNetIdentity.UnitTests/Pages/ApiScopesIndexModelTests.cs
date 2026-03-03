using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ApiScopesIndexModelTests
{
    private readonly Mock<IApiScopesAdminService> _mockApiScopesAdminService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.IndexModel _pageModel;

    public ApiScopesIndexModelTests()
    {
        _mockApiScopesAdminService = new Mock<IApiScopesAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.IndexModel(_mockApiScopesAdminService.Object);
    }

    [Fact]
    public async Task OnGetAsync_InitializesApiScopesCollection()
    {
        _mockApiScopesAdminService
            .Setup(service => service.GetApiScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApiScopeListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.ApiScopes.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsScopes_PopulatesApiScopes()
    {
        _mockApiScopesAdminService
            .Setup(service => service.GetApiScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApiScopeListItemDto>
            {
                new()
                {
                    Id = 1,
                    Name = "orders.read",
                    DisplayName = "Orders Read",
                    Description = "Read orders",
                    Enabled = true
                },
                new()
                {
                    Id = 2,
                    Name = "orders.write",
                    DisplayName = "Orders Write",
                    Description = "Write orders",
                    Enabled = false
                }
            });

        await _pageModel.OnGetAsync();

        _pageModel.ApiScopes.Should().HaveCount(2);
        _pageModel.ApiScopes.Select(scope => scope.Name).Should().Contain(new[] { "orders.read", "orders.write" });
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsOrderedData_PreservesOrder()
    {
        _mockApiScopesAdminService
            .Setup(service => service.GetApiScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApiScopeListItemDto>
            {
                new() { Id = 1, Name = "alpha", DisplayName = "Alpha", Description = "A", Enabled = true },
                new() { Id = 2, Name = "middle", DisplayName = "Middle", Description = "M", Enabled = true },
                new() { Id = 3, Name = "zeta", DisplayName = "Zeta", Description = "Z", Enabled = true }
            });

        await _pageModel.OnGetAsync();

        _pageModel.ApiScopes.Select(scope => scope.Name).Should().Equal("alpha", "middle", "zeta");
    }

    [Fact]
    public async Task OnGetAsync_NoScopes_ReturnsEmptyList()
    {
        _mockApiScopesAdminService
            .Setup(service => service.GetApiScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ApiScopeListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.ApiScopes.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_CallsServiceOnce()
    {
        _mockApiScopesAdminService
            .Setup(service => service.GetApiScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ApiScopeListItemDto>());

        await _pageModel.OnGetAsync();

        _mockApiScopesAdminService.Verify(
            service => service.GetApiScopesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
