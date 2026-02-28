using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using FluentAssertions;
using Xunit;
using IdentityServerAspNetIdentity.Models;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class RolesEditModelTests
{
    private static Mock<RoleManager<IdentityRole>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object, null!, null!, null!, null!);
    }

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());
        return mgr;
    }

    private IdentityServerAspNetIdentity.Pages.Admin.Roles.EditModel CreatePageModel(
        Mock<RoleManager<IdentityRole>>? roleManager = null,
        Mock<UserManager<ApplicationUser>>? userManager = null)
    {
        roleManager ??= CreateMockRoleManager();
        userManager ??= CreateMockUserManager();
        var model = new IdentityServerAspNetIdentity.Pages.Admin.Roles.EditModel(
            roleManager.Object,
            userManager.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
        return model;
    }

    // ── Step 1: Bare page + GET data display ─────────────────────────────

    [Fact]
    public async Task OnGetAsync_RoleExists_ReturnsPage()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1"))
            .ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(new List<ApplicationUser>());
        mockUserManager.Setup(m => m.Users)
            .Returns(new List<ApplicationUser>().AsQueryable());

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";

        // Act
        var result = await model.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnGetAsync_RoleExists_PopulatesRoleName()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1"))
            .ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(new List<ApplicationUser>());
        mockUserManager.Setup(m => m.Users)
            .Returns(new List<ApplicationUser>().AsQueryable());

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";

        // Act
        await model.OnGetAsync();

        // Assert
        model.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task OnGetAsync_RoleHasUsers_PopulatesUsersInRole()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        var usersInRole = new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" }
        };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1"))
            .ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(usersInRole);
        mockUserManager.Setup(m => m.Users)
            .Returns(usersInRole.AsQueryable());

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";

        // Act
        await model.OnGetAsync();

        // Assert
        model.UsersInRole.Should().HaveCount(2);
        model.UsersInRole.Select(u => u.UserName).Should().Contain("alice");
        model.UsersInRole.Select(u => u.UserName).Should().Contain("bob");
    }

    [Fact]
    public async Task OnGetAsync_UsersNotInRole_PopulatesAvailableUsers()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        var usersInRole = new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" }
        };

        var allUsers = new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" },
            new() { Id = "u3", UserName = "charlie", Email = "charlie@test.com" }
        };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1"))
            .ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(usersInRole);
        mockUserManager.Setup(m => m.Users)
            .Returns(allUsers.AsQueryable());

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";

        // Act
        await model.OnGetAsync();

        // Assert
        model.AvailableUsers.Should().HaveCount(2);
        model.AvailableUsers.Select(u => u.UserName).Should().Contain("bob");
        model.AvailableUsers.Select(u => u.UserName).Should().Contain("charlie");
        model.AvailableUsers.Select(u => u.UserName).Should().NotContain("alice");
    }

    // ── Step 2: GET edge cases ───────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_RoleNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.FindByIdAsync("nonexistent"))
            .ReturnsAsync((IdentityRole?)null);

        var model = CreatePageModel(mockRoleManager);
        model.RoleId = "nonexistent";

        // Act
        var result = await model.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_NoUsersInRole_UsersInRoleIsEmpty()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("EmptyRole") { Id = "role-2" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-2"))
            .ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("EmptyRole"))
            .ReturnsAsync(new List<ApplicationUser>());
        mockUserManager.Setup(m => m.Users)
            .Returns(new List<ApplicationUser>
            {
                new() { Id = "u1", UserName = "alice", Email = "a@t.com" }
            }.AsQueryable());

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-2";

        // Act
        await model.OnGetAsync();

        // Assert
        model.UsersInRole.Should().BeEmpty();
        model.AvailableUsers.Should().HaveCount(1);
    }

    // ── Step 4: POST add user ────────────────────────────────────────────

    [Fact]
    public async Task OnPostAddUserAsync_ValidUserId_CallsAddToRole()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.AddToRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        // Act
        await model.OnPostAddUserAsync();

        // Assert
        mockUserManager.Verify(m => m.AddToRoleAsync(user, "Editor"), Times.Once);
    }

    [Fact]
    public async Task OnPostAddUserAsync_ValidUserId_RedirectsToSelf()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.AddToRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        // Act
        var result = await model.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().ContainKey("roleId");
        redirect.RouteValues!["roleId"].Should().Be("role-1");
    }

    [Fact]
    public async Task OnPostAddUserAsync_RoleNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((IdentityRole?)null);

        var model = CreatePageModel(mockRoleManager);
        model.RoleId = "bad";
        model.SelectedUserId = "u1";

        // Act
        var result = await model.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((ApplicationUser?)null);

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";
        model.SelectedUserId = "bad";

        // Act
        var result = await model.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_NoUserSelected_ReturnsPage()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(new List<ApplicationUser>());
        mockUserManager.Setup(m => m.Users)
            .Returns(new List<ApplicationUser>().AsQueryable());

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";
        model.SelectedUserId = null;

        // Act
        var result = await model.OnPostAddUserAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
    }

    // ── Step 5: POST remove user ─────────────────────────────────────────

    [Fact]
    public async Task OnPostRemoveUserAsync_ValidUserId_CallsRemoveFromRole()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.RemoveFromRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        // Act
        await model.OnPostRemoveUserAsync();

        // Assert
        mockUserManager.Verify(m => m.RemoveFromRoleAsync(user, "Editor"), Times.Once);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ValidUserId_RedirectsToSelf()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.RemoveFromRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        // Act
        var result = await model.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_RoleNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((IdentityRole?)null);

        var model = CreatePageModel(mockRoleManager);
        model.RoleId = "bad";
        model.SelectedUserId = "u1";

        // Act
        var result = await model.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((ApplicationUser?)null);

        var model = CreatePageModel(mockRoleManager, mockUserManager);
        model.RoleId = "role-1";
        model.SelectedUserId = "bad";

        // Act
        var result = await model.OnPostRemoveUserAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
