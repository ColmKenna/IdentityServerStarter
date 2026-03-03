using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ClaimsIndexModelTests
{
    private readonly Mock<IClaimsAdminService> _mockClaimsAdminService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Claims.IndexModel _pageModel;

    public ClaimsIndexModelTests()
    {
        _mockClaimsAdminService = new Mock<IClaimsAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Claims.IndexModel(_mockClaimsAdminService.Object);
    }

    [Fact]
    public async Task OnGetAsync_InitializesClaimsCollection()
    {
        _mockClaimsAdminService
            .Setup(service => service.GetClaimsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ClaimTypeListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.Claims.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsClaims_PopulatesClaims()
    {
        _mockClaimsAdminService
            .Setup(service => service.GetClaimsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClaimTypeListItemDto>
            {
                new() { ClaimType = "department" },
                new() { ClaimType = "location" }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Claims.Should().HaveCount(2);
        _pageModel.Claims.Select(claim => claim.ClaimType).Should().Equal("department", "location");
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsOrderedData_PreservesOrder()
    {
        _mockClaimsAdminService
            .Setup(service => service.GetClaimsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClaimTypeListItemDto>
            {
                new() { ClaimType = "alpha" },
                new() { ClaimType = "middle" },
                new() { ClaimType = "zeta" }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Claims.Select(claim => claim.ClaimType).Should().Equal("alpha", "middle", "zeta");
    }

    [Fact]
    public async Task OnGetAsync_CallsServiceOnce_WithCancellationToken()
    {
        _mockClaimsAdminService
            .Setup(service => service.GetClaimsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ClaimTypeListItemDto>());

        await _pageModel.OnGetAsync();

        _mockClaimsAdminService.Verify(
            service => service.GetClaimsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
