using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Moq;
using FluentAssertions;
using Xunit;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class RolesIndexModelTests
{
    private static Mock<RoleManager<IdentityRole>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object, null!, null!, null!, null!);
    }

    // ── Step 1: Bare page rendering ──────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_ReturnsPageResult()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var roles = new List<IdentityRole>().AsQueryable();

        mockRoleManager.Setup(m => m.Roles)
            .Returns(roles);

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel(mockRoleManager.Object);

        // Act
        await pageModel.OnGetAsync();

        // Assert — page model should not throw and should be in a valid state
        pageModel.Roles.Should().NotBeNull();
    }

    // ── Step 2: GET data display ─────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_PopulatesRolesFromRoleManager()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var roles = new List<IdentityRole>
        {
            new IdentityRole("Admin") { Id = "1" },
            new IdentityRole("Editor") { Id = "2" },
            new IdentityRole("Viewer") { Id = "3" }
        }.AsQueryable();

        mockRoleManager.Setup(m => m.Roles)
            .Returns(roles);

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel(mockRoleManager.Object);

        // Act
        await pageModel.OnGetAsync();

        // Assert
        pageModel.Roles.Should().HaveCount(3);
    }

    [Fact]
    public async Task OnGetAsync_RolesContainCorrectNames()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var roles = new List<IdentityRole>
        {
            new IdentityRole("SuperAdmin") { Id = "1" },
            new IdentityRole("User") { Id = "2" }
        }.AsQueryable();

        mockRoleManager.Setup(m => m.Roles)
            .Returns(roles);

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel(mockRoleManager.Object);

        // Act
        await pageModel.OnGetAsync();

        // Assert
        pageModel.Roles.Select(r => r.Name).Should().Contain("SuperAdmin");
        pageModel.Roles.Select(r => r.Name).Should().Contain("User");
    }

    // ── Step 3: GET edge cases ───────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_NoRolesExist_ReturnsEmptyList()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var roles = new List<IdentityRole>().AsQueryable();

        mockRoleManager.Setup(m => m.Roles)
            .Returns(roles);

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel(mockRoleManager.Object);

        // Act
        await pageModel.OnGetAsync();

        // Assert
        pageModel.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_RolesAreOrderedByName()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var roles = new List<IdentityRole>
        {
            new IdentityRole("Zebra") { Id = "1" },
            new IdentityRole("Admin") { Id = "2" },
            new IdentityRole("Manager") { Id = "3" }
        }.AsQueryable();

        mockRoleManager.Setup(m => m.Roles)
            .Returns(roles);

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Roles.IndexModel(mockRoleManager.Object);

        // Act
        await pageModel.OnGetAsync();

        // Assert
        pageModel.Roles.Select(r => r.Name).Should().BeInAscendingOrder();
    }
}
