using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class RolesIndexModelTests
{
    private readonly Mock<IRolesAdminService> _mockRolesAdminService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel _pageModel;

    public RolesIndexModelTests()
    {
        _mockRolesAdminService = new Mock<IRolesAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel(_mockRolesAdminService.Object);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsPageResult()
    {
        _mockRolesAdminService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_PopulatesRolesFromService()
    {
        _mockRolesAdminService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Id = "1", Name = "Admin" },
                new() { Id = "2", Name = "Editor" },
                new() { Id = "3", Name = "Viewer" }
            ]);

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Should().HaveCount(3);
    }

    [Fact]
    public async Task OnGetAsync_RolesContainCorrectNames()
    {
        _mockRolesAdminService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new() { Id = "1", Name = "SuperAdmin" },
                new() { Id = "2", Name = "User" }
            ]);

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Select(r => r.Name).Should().Contain(["SuperAdmin", "User"]);
    }

    [Fact]
    public async Task OnGetAsync_NoRolesExist_ReturnsEmptyList()
    {
        _mockRolesAdminService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Should().BeEmpty();
    }

}
