using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class AccountsIndexIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AccountsIndexIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // Phase 1 — Page loads
    [Fact]
    public async Task should_return_success_status_when_get_accounts_index()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Accounts");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // Phase 2 — Displays user data
    [Fact]
    public async Task should_display_user_list_when_users_exist()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = $"integrationuser-{Guid.NewGuid():N}",
                Email = $"integration-{Guid.NewGuid():N}@test.com",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(user, "Pass123$");
        }

        // Act
        var document = await _client.GetAndParsePage("/Admin/Accounts");

        // Assert
        var listItems = document.QuerySelectorAll("ul li");
        listItems.Length.Should().BeGreaterThan(0);
    }

    // Phase 5 — DOM structure
    [Fact]
    public async Task should_contain_heading_when_get_accounts_index()
    {
        // Act
        var document = await _client.GetAndParsePage("/Admin/Accounts");

        // Assert
        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("All Users");
    }

    // Phase 5 — DOM: user emails in list
    [Fact]
    public async Task should_display_user_emails_in_list_items()
    {
        // Arrange
        var email = $"emailtest-{Guid.NewGuid():N}@test.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await userManager.CreateAsync(new ApplicationUser
            {
                UserName = $"emailtestuser-{Guid.NewGuid():N}",
                Email = email,
                EmailConfirmed = true
            }, "Pass123$");
        }

        // Act
        var document = await _client.GetAndParsePage("/Admin/Accounts");

        // Assert — check the full page body content since layout may have other <ul> elements
        var bodyContent = document.Body?.TextContent ?? "";
        bodyContent.Should().Contain(email);
    }
}
