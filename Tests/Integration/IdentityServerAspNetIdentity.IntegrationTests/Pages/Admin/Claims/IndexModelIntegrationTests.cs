using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.Claims;

public class IndexModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IndexModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldShowEmptyState_WhenNoClaimsExist()
    {
        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector(".alert-info")!.TextContent.Should().Contain("No claims found.");
        document.QuerySelector("ck-responsive-table").Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldRenderClaimRows_WhenClaimsExist()
    {
        var userId = await TestDataHelper.SeedUserAsync(_factory, "claimuser");
        await TestDataHelper.AddUserClaimAsync(_factory, userId, "Role", "Admin");
        await TestDataHelper.AddUserClaimAsync(_factory, userId, "Permission", "Read");

        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
        rows.Should().HaveCount(2);

        rows.Should().Contain(r => r.TextContent.Contains("Role"));
        rows.Should().Contain(r => r.TextContent.Contains("Permission"));
    }

    [Fact]
    public async Task Get_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        using var nonAdminFactory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();
        using var nonAdminClient = nonAdminFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await nonAdminClient.GetAsync("/Admin/Claims");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        using var unauthFactory = new CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>();
        using var unauthClient = unauthFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await unauthClient.GetAsync("/Admin/Claims");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
