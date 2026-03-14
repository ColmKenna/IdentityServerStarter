using System.Net;
using FluentAssertions;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using AngleSharp.Html.Dom;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.Roles;

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
    public async Task Get_ShouldReturnNotFound_WhenRoleIdInvalid()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Roles/invalid-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldShowRoleDetails_WhenRoleIdValid()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roleName = "Editor_" + Guid.NewGuid().ToString("N");
        var role = new IdentityRole(roleName);
        await roleManager.CreateAsync(role);

        var user = new ApplicationUser { UserName = "editor_user_" + Guid.NewGuid().ToString("N"), Email = "ed@test.com" };
        await userManager.CreateAsync(user, "Pass123$");
        await userManager.AddToRoleAsync(user, roleName);

        var availUser = new ApplicationUser { UserName = "avail_user_" + Guid.NewGuid().ToString("N"), Email = "av@test.com" };
        await userManager.CreateAsync(availUser, "Pass123$");

        // Act
        var document = await _client.GetAndParsePage($"/Admin/Roles/{role.Id}");

        // Assert
        document.QuerySelector("h2")!.TextContent.Should().Contain(roleName);
        document.QuerySelector("#users-in-role-table")!.TextContent.Should().Contain(user.UserName);
        document.QuerySelector("#SelectedUserId")!.InnerHtml.Should().Contain(availUser.UserName);
    }

    [Fact]
    public async Task PostAddUser_ShouldAddUserToRole_WhenValid()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roleName = "AddRole_" + Guid.NewGuid().ToString("N");
        var role = new IdentityRole(roleName);
        await roleManager.CreateAsync(role);

        var user = new ApplicationUser { UserName = "to_add_" + Guid.NewGuid().ToString("N"), Email = "add@test.com" };
        await userManager.CreateAsync(user, "Pass123$");

        var document = await _client.GetAndParsePage($"/Admin/Roles/{role.Id}");
        var form = document.QuerySelector<IHtmlFormElement>("#add-user-form")!;

        // Act
        var response = await _client.SubmitForm(form, new Dictionary<string, string>
        {
            ["SelectedUserId"] = user.Id
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);
        resultDocument.QuerySelector(".alert-success")!.TextContent.Should().Contain("added to role");

        // Verify in DB with fresh scope
        using var verifyScope = _factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await verifyUserManager.FindByIdAsync(user.Id);
        var isInRole = await verifyUserManager.IsInRoleAsync(updatedUser!, roleName);
        isInRole.Should().BeTrue();
    }

    [Fact]
    public async Task PostRemoveUser_ShouldRemoveUserFromRole_WhenValid()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var roleName = "RemoveRole_" + Guid.NewGuid().ToString("N");
        var role = new IdentityRole(roleName);
        await roleManager.CreateAsync(role);

        var user = new ApplicationUser { UserName = "to_remove_" + Guid.NewGuid().ToString("N"), Email = "rem@test.com" };
        await userManager.CreateAsync(user, "Pass123$");
        await userManager.AddToRoleAsync(user, roleName);

        var document = await _client.GetAndParsePage($"/Admin/Roles/{role.Id}");
        var form = document.QuerySelector<IHtmlFormElement>($"#remove-user-form-{user.Id}")!;

        // Act
        var response = await _client.SubmitForm(form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);
        resultDocument.QuerySelector(".alert-success")!.TextContent.Should().Contain("removed from role");

        // Verify in DB with fresh scope to avoid stale state
        using var verifyScope = _factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await verifyUserManager.FindByIdAsync(user.Id);
        var isInRole = await verifyUserManager.IsInRoleAsync(updatedUser!, roleName);
        isInRole.Should().BeFalse();
    }

    [Fact]
    public async Task PostAddUser_ShouldShowError_WhenNoUserSelected()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var role = new IdentityRole("ErrorRole_" + Guid.NewGuid().ToString("N"));
        await roleManager.CreateAsync(role);

        // Seed an available user so the #add-user-form is rendered
        var availUser = new ApplicationUser { UserName = "avail_user_" + Guid.NewGuid().ToString("N"), Email = "av@test.com" };
        await userManager.CreateAsync(availUser, "Pass123$");

        var document = await _client.GetAndParsePage($"/Admin/Roles/{role.Id}");
        var form = document.QuerySelector<IHtmlFormElement>("#add-user-form")!;

        // Act - submit without selecting a user
        var response = await _client.SubmitForm(form, new Dictionary<string, string>
        {
            ["SelectedUserId"] = ""
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);
        
        var validationSpan = resultDocument.QuerySelector("[data-valmsg-for='SelectedUserId']");
        validationSpan.Should().NotBeNull();
        validationSpan!.TextContent.Should().Contain("Please select a user");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
