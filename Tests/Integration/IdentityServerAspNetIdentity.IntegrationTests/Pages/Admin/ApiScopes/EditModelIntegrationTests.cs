using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.ApiScopes;

public class EditModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetCreatePage_ShouldRenderKnownClaims_WhenClaimsExist()
    {
        var claimType = $"claim-{Guid.NewGuid():N}";
        await SeedUserClaimTypeAsync(claimType);

        var response = await _client.GetAsync("/Admin/ApiScopes/0/Edit");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector("h2")!.TextContent.Should().Contain("Create API Scope");
        document.QuerySelector("#save-before-user-claims-message")!.TextContent.Should().Contain("Save API scope before managing applied user claims.");
        document.QuerySelectorAll("#known-user-claims-list li code")
            .Select(x => x.TextContent.Trim())
            .Should()
            .Contain(claimType);
    }

    [Fact]
    public async Task PostCreate_ShouldReRenderKnownClaims_WhenDuplicateNameRejected()
    {
        var existingName = $"api-scope-{Guid.NewGuid():N}";
        var claimType = $"claim-{Guid.NewGuid():N}";

        await TestDataHelper.SeedApiScopeAsync(_factory, existingName);
        await SeedUserClaimTypeAsync(claimType);

        var page = await _client.GetAndParsePage("/Admin/ApiScopes/0/Edit");
        var form = page.QuerySelector("#edit-api-scope-form").Should().BeAssignableTo<IHtmlFormElement>().Subject;

        var response = await _client.SubmitForm(
            form,
            new Dictionary<string, string>
            {
                ["Input.Name"] = existingName,
                ["Input.DisplayName"] = "Duplicate Scope",
                ["Input.Description"] = "Duplicate description"
            });

        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Body!.TextContent.Should().Contain("An API scope with this name already exists.");
        document.QuerySelectorAll("#known-user-claims-list li code")
            .Select(x => x.TextContent.Trim())
            .Should()
            .Contain(claimType);
    }

    [Fact]
    public async Task GetEditPage_ShouldRenderAppliedAndAvailableClaimPartitions_WhenScopeExists()
    {
        var appliedClaimType = $"claim-applied-{Guid.NewGuid():N}";
        var availableClaimType = $"claim-available-{Guid.NewGuid():N}";
        await SeedUserClaimTypeAsync(appliedClaimType);
        await SeedUserClaimTypeAsync(availableClaimType);
        var scopeId = await TestDataHelper.SeedApiScopeAsync(
            _factory,
            $"api-scope-{Guid.NewGuid():N}",
            userClaims: [appliedClaimType]);

        var response = await _client.GetAsync($"/Admin/ApiScopes/{scopeId}/Edit");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelectorAll("#applied-user-claims-table tbody code")
            .Select(x => x.TextContent.Trim())
            .Should()
            .Contain(appliedClaimType);
        document.QuerySelectorAll("#available-user-claims-select option")
            .Select(x => x.TextContent.Trim())
            .Should()
            .Contain(availableClaimType)
            .And
            .NotContain(appliedClaimType);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task SeedUserClaimTypeAsync(string claimType)
    {
        var username = $"user-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.CreateTestUserAsync(
            _factory,
            username,
            $"{username}@test.local");
        await TestDataHelper.AddUserClaimAsync(_factory, userId, claimType, "value");
    }
}
