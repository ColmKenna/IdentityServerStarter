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
            .ReturnsAsync([]);

        await _pageModel.OnGetAsync();

        _pageModel.Claims.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsClaims_PopulatesClaims()
    {
        _mockClaimsAdminService
            .Setup(service => service.GetClaimsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { ClaimType = "department" },
                new() { ClaimType = "location" }
            ]);

        await _pageModel.OnGetAsync();

        _pageModel.Claims.Should().HaveCount(2);
        _pageModel.Claims.Select(claim => claim.ClaimType).Should().Equal("department", "location");
    }

}
