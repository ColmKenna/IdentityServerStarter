using System.Net;
using System.Security.Claims;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class UsersEditIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersEditIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> SeedUserAsync(string usernamePrefix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var user = new ApplicationUser
        {
            UserName = $"{usernamePrefix}-{uniqueSuffix}",
            Email = $"{usernamePrefix}-{uniqueSuffix}@test.local",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Pass123$");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user.Id;
    }

    private async Task AddUserClaimAsync(string userId, string claimType, string claimValue)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException($"User '{userId}' not found.");
        var result = await userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add claim: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    private async Task AddUserToRoleAsync(string userId, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException($"User '{userId}' not found.");
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role: {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add user to role: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
        }
    }

    [Fact]
    public async Task Get_ClaimsTab_WhenAvailableClaimsExist_RendersAvailableClaimsList()
    {
        var targetUserId = await SeedUserAsync("target-user");
        var otherUser1Id = await SeedUserAsync("other-user");
        var otherUser2Id = await SeedUserAsync("other-user");

        await AddUserClaimAsync(targetUserId, "role", "admin");
        await AddUserClaimAsync(otherUser1Id, "department", "engineering");
        await AddUserClaimAsync(otherUser1Id, "role", "auditor");
        await AddUserClaimAsync(otherUser2Id, "location", "dublin");

        var response = await _client.GetAsync($"/Admin/Users/{targetUserId}?tab=claims");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await AngleSharpHelpers.GetDocumentAsync(response);
        var availableClaimsList = document.QuerySelector("#available-claims-list");
        availableClaimsList.Should().NotBeNull();

        var availableClaimPills = document.QuerySelectorAll("#available-claims-list .available-claim-pill");
        availableClaimPills.Should().NotBeEmpty();

        var availableClaimTypes = availableClaimPills
            .Select(c => c.GetAttribute("data-claim-type"))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToList();
        availableClaimTypes.Should().Contain("department");
        availableClaimTypes.Should().Contain("location");
        availableClaimTypes.Should().NotContain("role");

        availableClaimPills.Select(c => c.TextContent.Trim()).Should().Contain("department");
        availableClaimPills.Select(c => c.TextContent.Trim()).Should().Contain("location");
        document.QuerySelector("#no-available-claims-message").Should().BeNull();
    }

    [Fact]
    public async Task Get_ClaimsTab_WhenNoAvailableClaimsExist_RendersEmptyStateMessage()
    {
        var targetUserId = await SeedUserAsync("target-user");
        var otherUserId = await SeedUserAsync("other-user");

        await AddUserClaimAsync(targetUserId, "department", "engineering");
        await AddUserClaimAsync(otherUserId, "department", "sales");

        var response = await _client.GetAsync($"/Admin/Users/{targetUserId}?tab=claims");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await AngleSharpHelpers.GetDocumentAsync(response);
        document.QuerySelector("#available-claims-list").Should().BeNull();
        var emptyMessage = document.QuerySelector("#no-available-claims-message");
        emptyMessage.Should().NotBeNull();
        emptyMessage!.TextContent.Should().Contain("No available claims");
    }

    [Fact]
    public async Task Get_ClaimsTab_WhenWritableAndClaimsAvailable_RendersPillAndModalContract()
    {
        var targetUserId = await SeedUserAsync("target-user");
        var otherUserId = await SeedUserAsync("other-user");
        await AddUserClaimAsync(otherUserId, "department", "engineering");

        var response = await _client.GetAsync($"/Admin/Users/{targetUserId}?tab=claims");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.QuerySelector("#available-claims-list .available-claim-pill").Should().NotBeNull();
        document.QuerySelector("#add-claim-modal").Should().NotBeNull();
        document.QuerySelector("#NewClaimType").Should().NotBeNull();
    }

    [Fact]
    public async Task Get_ClaimsTab_WhenClaimsExist_RendersClaimMetadataForRemoveConfirmation()
    {
        var targetUserId = await SeedUserAsync("target-user");
        await AddUserClaimAsync(targetUserId, "department", "engineering");

        var response = await _client.GetAsync($"/Admin/Users/{targetUserId}?tab=claims");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var claimCheckbox = document.QuerySelector(".claim-checkbox");
        claimCheckbox.Should().NotBeNull();
        claimCheckbox!.GetAttribute("data-claim-type").Should().Be("department");
        claimCheckbox.GetAttribute("data-claim-value").Should().Be("engineering");
        document.QuerySelector("#remove-selected-btn").Should().NotBeNull();
    }

    [Fact]
    public async Task Get_RolesTab_WhenAssignedRolesExist_RendersRoleMetadataForRemoveConfirmation()
    {
        var targetUserId = await SeedUserAsync("target-user");
        await AddUserToRoleAsync(targetUserId, "Operator");

        var response = await _client.GetAsync($"/Admin/Users/{targetUserId}?tab=roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var roleCheckbox = document.QuerySelector(".role-checkbox");
        roleCheckbox.Should().NotBeNull();
        roleCheckbox!.GetAttribute("data-role-name").Should().Be("Operator");
        roleCheckbox.GetAttribute("value").Should().Be("Operator");
        document.QuerySelector("#remove-roles-btn").Should().NotBeNull();
    }
}
