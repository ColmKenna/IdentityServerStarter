using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.Roles;

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
    public async Task Get_ShouldShowEmptyState_WhenNoRolesExist()
    {
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector(".alert-info")!.TextContent.Should().Contain("No roles found.");
        document.QuerySelector("ck-responsive-table").Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldRenderRoleRowsAndEditLinks_WhenRolesExist()
    {
        var adminRoleName = $"admin-{Guid.NewGuid():N}";
        var operatorRoleName = $"operator-{Guid.NewGuid():N}";
        var adminRoleId = await TestDataHelper.SeedRoleAsync(_factory, adminRoleName);
        var operatorRoleId = await TestDataHelper.SeedRoleAsync(_factory, operatorRoleName);

        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
        rows.Should().HaveCount(2);

        var adminRow = rows.Single(x => x.TextContent.Contains(adminRoleName, StringComparison.Ordinal));
        adminRow.TextContent.Should().Contain(adminRoleName);
        adminRow.QuerySelector("a")!.GetAttribute("href").Should().Be($"/Admin/Roles/{adminRoleId}");

        var operatorRow = rows.Single(x => x.TextContent.Contains(operatorRoleName, StringComparison.Ordinal));
        operatorRow.TextContent.Should().Contain(operatorRoleName);
        operatorRow.QuerySelector("a")!.GetAttribute("href").Should().Be($"/Admin/Roles/{operatorRoleId}");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
