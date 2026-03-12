using IdentityServerAspNetIdentity.Pages.Admin.Claims;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Claims;

public class IndexModelTests
{
    private readonly Mock<IClaimsAdminService> _mockClaimsAdminService;
    private readonly IndexModel _sut;

    public IndexModelTests()
    {
        _mockClaimsAdminService = new Mock<IClaimsAdminService>();

        _sut = new IndexModel(_mockClaimsAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnClaims_WhenTheyExist()
    {
        // Arrange
        var expectedClaims = new List<ClaimTypeListItemDto>
        {
            new() { ClaimType = "Role" },
            new() { ClaimType = "Permission" }
        };

        _mockClaimsAdminService
            .Setup(x => x.GetClaimsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedClaims);

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Claims.Should().HaveCount(2);
        _sut.Claims.Should().BeEquivalentTo(expectedClaims);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnEmptyClaims_WhenThereAreNone()
    {
        // Arrange
        _mockClaimsAdminService
            .Setup(x => x.GetClaimsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClaimTypeListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Claims.Should().BeEmpty();
    }
}
