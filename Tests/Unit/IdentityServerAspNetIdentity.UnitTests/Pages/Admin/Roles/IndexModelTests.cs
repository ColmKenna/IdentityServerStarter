using IdentityServerAspNetIdentity.Pages.Admin.Roles;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Roles;

public class IndexModelTests
{
    private readonly Mock<IRolesAdminService> _rolesAdminService = new();

    [Fact]
    public async Task OnGetAsync_ShouldPopulateRoles_WhenServiceReturnsRoles()
    {
        var sut = CreateSut();
        var roles = new List<RoleListItemDto>
        {
            new() { Id = "role-1", Name = "Administrators" },
            new() { Id = "role-2", Name = "Operators" }
        };

        _rolesAdminService.Setup(x => x.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        await sut.OnGetAsync();

        sut.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task OnGetAsync_ShouldLeaveRolesEmpty_WhenServiceReturnsNoRoles()
    {
        var sut = CreateSut();

        _rolesAdminService.Setup(x => x.GetRolesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await sut.OnGetAsync();

        sut.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPassRequestAbortedTokenToService()
    {
        var sut = CreateSut();
        using var cts = new CancellationTokenSource();
        sut.PageContext.HttpContext.RequestAborted = cts.Token;

        _rolesAdminService.Setup(x => x.GetRolesAsync(cts.Token))
            .ReturnsAsync([]);

        await sut.OnGetAsync();

        _rolesAdminService.Verify(x => x.GetRolesAsync(cts.Token), Times.Once);
    }

    private IndexModel CreateSut()
    {
        return new IndexModel(_rolesAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
