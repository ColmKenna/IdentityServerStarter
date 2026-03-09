using IdentityServerAspNetIdentity.Pages.Admin.IdentityResources;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.IdentityResources;

public class IndexModelTests
{
    private readonly Mock<IIdentityResourcesAdminService> _identityResourcesAdminService = new();

    [Fact]
    public async Task OnGetAsync_ShouldPopulateIdentityResources_WhenServiceReturnsResults()
    {
        var expectedResources = new List<IdentityResourceListItemDto>
        {
            new() { Id = 3, Name = "email", DisplayName = "Email", Description = "Email address scope", Enabled = true },
            new() { Id = 7, Name = "profile", DisplayName = string.Empty, Description = string.Empty, Enabled = false }
        };

        _identityResourcesAdminService.Setup(x => x.GetIdentityResourcesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResources);

        var sut = CreateSut();

        await sut.OnGetAsync();

        sut.IdentityResources.Should().BeEquivalentTo(expectedResources);
    }

    [Fact]
    public async Task OnGetAsync_ShouldPassRequestAbortedToken_WhenHttpContextHasCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var sut = CreateSut();
        sut.PageContext.HttpContext.RequestAborted = cts.Token;

        _identityResourcesAdminService.Setup(x => x.GetIdentityResourcesAsync(cts.Token))
            .ReturnsAsync([]);

        await sut.OnGetAsync();

        _identityResourcesAdminService.Verify(x => x.GetIdentityResourcesAsync(cts.Token), Times.Once);
    }

    private IndexModel CreateSut()
    {
        return new IndexModel(_identityResourcesAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
