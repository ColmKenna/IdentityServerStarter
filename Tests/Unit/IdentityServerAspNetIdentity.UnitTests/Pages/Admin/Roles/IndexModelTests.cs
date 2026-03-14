using IdentityServerAspNetIdentity.Pages.Admin.Roles;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using FluentAssertions;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Roles;

public class IndexModelTests
{
    private readonly Mock<IRolesAdminService> _mockRolesAdminService;
    private readonly IndexModel _sut;

    public IndexModelTests()
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
    public async Task OnGetAsync_ShouldPopulateRoles_WhenServiceReturnsData()
    {
        // Arrange
        var expectedRoles = new List<RoleListItemDto>
        {
            new() { Id = "1", Name = "Admin" },
            new() { Id = "2", Name = "User" }
        };

        _mockRolesAdminService
            .Setup(x => x.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRoles);

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Roles.Should().HaveCount(2);
        _sut.Roles.Should().BeEquivalentTo(expectedRoles);
    }

    [Fact]
    public async Task OnGetAsync_ShouldHandleEmptyRoles_WhenServiceReturnsEmpty()
    {
        // Arrange
        _mockRolesAdminService
            .Setup(x => x.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.Roles.Should().BeEmpty();
    }
}
