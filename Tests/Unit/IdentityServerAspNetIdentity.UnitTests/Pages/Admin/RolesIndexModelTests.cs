using IdentityServerAspNetIdentity.Pages.Admin.Roles;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin;

public class RolesIndexModelTests
{
    private readonly Mock<IRolesAdminService> _mockRolesAdminService;
    private readonly IndexModel _sut;

    public RolesIndexModelTests()
    {
        _mockRolesAdminService = new Mock<IRolesAdminService>();
        _sut = new IndexModel(_mockRolesAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateRolesList_FromAdminService()
    {
        // Arrange
        var expectedRoles = new List<RoleListItemDto>
        {
            new() { Id = "1", Name = "Admin" },
            new() { Id = "2", Name = "User" }
        };
        _mockRolesAdminService.Setup(x => x.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRoles);

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Roles.Should().BeEquivalentTo(expectedRoles);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnEmptyList_WhenServiceReturnsEmpty()
    {
        // Arrange
        _mockRolesAdminService.Setup(x => x.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPassCancellationToken_ToService()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _sut.PageContext.HttpContext.RequestAborted = cts.Token;
        
        _mockRolesAdminService.Setup(x => x.GetRolesAsync(cts.Token))
            .ReturnsAsync(new List<RoleListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _mockRolesAdminService.Verify(x => x.GetRolesAsync(cts.Token), Times.Once);
    }
}
