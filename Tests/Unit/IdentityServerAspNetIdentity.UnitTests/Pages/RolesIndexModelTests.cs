using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class RolesIndexModelTests
{
    private readonly Mock<IRolesAdminService> _mockService;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel _pageModel;

    public RolesIndexModelTests()
    {
        _mockService = new Mock<IRolesAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel(_mockService.Object);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsPageResult()
    {
        _mockService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_PopulatesRolesFromService()
    {
        _mockService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleListItemDto>
            {
                new() { Id = "1", Name = "Admin" },
                new() { Id = "2", Name = "Editor" },
                new() { Id = "3", Name = "Viewer" }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Should().HaveCount(3);
    }

    [Fact]
    public async Task OnGetAsync_RolesContainCorrectNames()
    {
        _mockService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleListItemDto>
            {
                new() { Id = "1", Name = "SuperAdmin" },
                new() { Id = "2", Name = "User" }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Select(r => r.Name).Should().Contain("SuperAdmin");
        _pageModel.Roles.Select(r => r.Name).Should().Contain("User");
    }

    [Fact]
    public async Task OnGetAsync_NoRolesExist_ReturnsEmptyList()
    {
        _mockService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RoleListItemDto>());

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsOrderedData_PreservesOrder()
    {
        _mockService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleListItemDto>
            {
                new() { Id = "2", Name = "Admin" },
                new() { Id = "3", Name = "Manager" },
                new() { Id = "1", Name = "Zebra" }
            });

        await _pageModel.OnGetAsync();

        _pageModel.Roles.Select(r => r.Name).Should().Equal("Admin", "Manager", "Zebra");
    }

    [Fact]
    public async Task OnGetAsync_CallsServiceOnce()
    {
        _mockService
            .Setup(s => s.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RoleListItemDto>());

        await _pageModel.OnGetAsync();

        _mockService.Verify(
            s => s.GetRolesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
