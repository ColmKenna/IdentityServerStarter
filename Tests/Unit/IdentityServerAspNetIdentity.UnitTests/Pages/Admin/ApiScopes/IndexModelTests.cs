using IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.ApiScopes;

public class IndexModelTests
{
    private readonly Mock<IApiScopesAdminService> _mockApiScopesAdminService;
    private readonly IndexModel _sut;

    public IndexModelTests()
    {
        _mockApiScopesAdminService = new Mock<IApiScopesAdminService>();

        _sut = new IndexModel(_mockApiScopesAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateApiScopes_WhenServiceReturnsData()
    {
        // Arrange
        var expectedScopes = new List<ApiScopeListItemDto>
        {
            new() { Id = 1, Name = "api1", DisplayName = "API One", Description = "First API", Enabled = true },
            new() { Id = 2, Name = "api2", DisplayName = "API Two", Description = "Second API", Enabled = false }
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetApiScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedScopes);

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.ApiScopes.Should().HaveCount(2);
        _sut.ApiScopes.Should().BeEquivalentTo(expectedScopes);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnEmptyList_WhenServiceReturnsNoScopes()
    {
        // Arrange
        _mockApiScopesAdminService
            .Setup(x => x.GetApiScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApiScopeListItemDto>());

        // Act
        await _sut.OnGetAsync();

        // Assert
        _sut.ApiScopes.Should().BeEmpty();
    }
}
