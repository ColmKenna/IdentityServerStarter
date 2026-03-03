using FluentAssertions;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class RolesAdminServiceTests
{
    private static Mock<RoleManager<IdentityRole>> CreateMockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(
            store.Object, null!, null!, null!, null!);
    }

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager(
        IReadOnlyList<ApplicationUser>? users = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.SetupGet(m => m.Users)
            .Returns((users ?? Array.Empty<ApplicationUser>()).AsQueryable());
        return mgr;
    }

    private static RolesAdminService CreateService(
        Mock<RoleManager<IdentityRole>> roleManager,
        Mock<UserManager<ApplicationUser>> userManager)
    {
        return new RolesAdminService(roleManager.Object, userManager.Object);
    }

    // ── GetRolesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRolesAsync_NoRoles_ReturnsEmptyList()
    {
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.Roles)
            .Returns(new List<IdentityRole>().AsQueryable());

        var service = CreateService(mockRoleManager, CreateMockUserManager());

        var result = await service.GetRolesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesAsync_RolesExist_ReturnsAllRoles()
    {
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.Roles)
            .Returns(new List<IdentityRole>
            {
                new("Admin") { Id = "1" },
                new("Editor") { Id = "2" },
                new("Viewer") { Id = "3" }
            }.AsQueryable());

        var service = CreateService(mockRoleManager, CreateMockUserManager());

        var result = await service.GetRolesAsync();

        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().Contain(new[] { "Admin", "Editor", "Viewer" });
    }

    [Fact]
    public async Task GetRolesAsync_RolesExist_ReturnedInAscendingOrderByName()
    {
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.Roles)
            .Returns(new List<IdentityRole>
            {
                new("Zebra") { Id = "1" },
                new("Admin") { Id = "2" },
                new("Manager") { Id = "3" }
            }.AsQueryable());

        var service = CreateService(mockRoleManager, CreateMockUserManager());

        var result = await service.GetRolesAsync();

        result.Select(r => r.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetRolesAsync_NullRoleName_ReturnsEmptyString()
    {
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.Roles)
            .Returns(new List<IdentityRole>
            {
                new() { Id = "1", Name = null }
            }.AsQueryable());

        var service = CreateService(mockRoleManager, CreateMockUserManager());

        var result = await service.GetRolesAsync();

        result.Should().ContainSingle().Which.Name.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesAsync_MapsIdCorrectly()
    {
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.Roles)
            .Returns(new List<IdentityRole>
            {
                new("Admin") { Id = "abc-123" }
            }.AsQueryable());

        var service = CreateService(mockRoleManager, CreateMockUserManager());

        var result = await service.GetRolesAsync();

        result.Should().ContainSingle().Which.Id.Should().Be("abc-123");
    }

    // ── GetRoleForEditAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetRoleForEditAsync_RoleNotFound_ReturnsNull()
    {
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.FindByIdAsync("bad"))
            .ReturnsAsync((IdentityRole?)null);

        var service = CreateService(mockRoleManager, CreateMockUserManager());

        var result = await service.GetRoleForEditAsync("bad");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRoleForEditAsync_RoleExists_ReturnsRoleName()
    {
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(new List<ApplicationUser>());

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.GetRoleForEditAsync("role-1");

        result.Should().NotBeNull();
        result!.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task GetRoleForEditAsync_RoleHasUsers_PopulatesUsersInRole()
    {
        var mockRoleManager = CreateMockRoleManager();
        var usersInRole = new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" }
        };
        var mockUserManager = CreateMockUserManager(usersInRole);
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(usersInRole);

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.GetRoleForEditAsync("role-1");

        result!.UsersInRole.Should().HaveCount(2);
        result.UsersInRole.Select(u => u.UserName).Should().Contain("alice");
        result.UsersInRole.Select(u => u.UserName).Should().Contain("bob");
    }

    [Fact]
    public async Task GetRoleForEditAsync_UsersNotInRole_PopulatesAvailableUsers()
    {
        var mockRoleManager = CreateMockRoleManager();
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
        var mockUserManager = CreateMockUserManager(allUsers);
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(usersInRole);

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.GetRoleForEditAsync("role-1");

        result!.AvailableUsers.Should().HaveCount(2);
        result.AvailableUsers.Select(u => u.UserName).Should().Contain("bob");
        result.AvailableUsers.Select(u => u.UserName).Should().Contain("charlie");
        result.AvailableUsers.Select(u => u.UserName).Should().NotContain("alice");
    }

    [Fact]
    public async Task GetRoleForEditAsync_NoUsersInRole_UsersInRoleIsEmpty()
    {
        var mockRoleManager = CreateMockRoleManager();
        var allUsers = new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "a@t.com" }
        };
        var mockUserManager = CreateMockUserManager(allUsers);
        var role = new IdentityRole("EmptyRole") { Id = "role-2" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-2")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("EmptyRole"))
            .ReturnsAsync(new List<ApplicationUser>());

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.GetRoleForEditAsync("role-2");

        result!.UsersInRole.Should().BeEmpty();
        result.AvailableUsers.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRoleForEditAsync_UsersInRole_OrderedByUserName()
    {
        var mockRoleManager = CreateMockRoleManager();
        var usersInRole = new List<ApplicationUser>
        {
            new() { Id = "u2", UserName = "zara", Email = "z@t.com" },
            new() { Id = "u1", UserName = "alice", Email = "a@t.com" }
        };
        var mockUserManager = CreateMockUserManager(usersInRole);
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.GetUsersInRoleAsync("Editor"))
            .ReturnsAsync(usersInRole);

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.GetRoleForEditAsync("role-1");

        result!.UsersInRole.Select(u => u.UserName).Should().BeInAscendingOrder();
    }

    // ── AddUserToRoleAsync ────────────────────────────────────────────────

    [Fact]
    public async Task AddUserToRoleAsync_RoleNotFound_ReturnsRoleNotFound()
    {
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((IdentityRole?)null);

        var service = CreateService(mockRoleManager, CreateMockUserManager());

        var result = await service.AddUserToRoleAsync("bad", "u1");

        result.Status.Should().Be(AddUserToRoleStatus.RoleNotFound);
    }

    [Fact]
    public async Task AddUserToRoleAsync_UserNotFound_ReturnsUserNotFound()
    {
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((ApplicationUser?)null);

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.AddUserToRoleAsync("role-1", "bad");

        result.Status.Should().Be(AddUserToRoleStatus.UserNotFound);
    }

    [Fact]
    public async Task AddUserToRoleAsync_Success_ReturnsSuccessWithNames()
    {
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.AddToRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.AddUserToRoleAsync("role-1", "u1");

        result.Status.Should().Be(AddUserToRoleStatus.Success);
        result.UserName.Should().Be("alice");
        result.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task AddUserToRoleAsync_IdentityFails_ReturnsFailedWithErrors()
    {
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.AddToRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User already in role" }));

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.AddUserToRoleAsync("role-1", "u1");

        result.Status.Should().Be(AddUserToRoleStatus.Failed);
        result.Errors.Should().ContainSingle("User already in role");
    }

    // ── RemoveUserFromRoleAsync ───────────────────────────────────────────

    [Fact]
    public async Task RemoveUserFromRoleAsync_RoleNotFound_ReturnsRoleNotFound()
    {
        var mockRoleManager = CreateMockRoleManager();
        mockRoleManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((IdentityRole?)null);

        var service = CreateService(mockRoleManager, CreateMockUserManager());

        var result = await service.RemoveUserFromRoleAsync("bad", "u1");

        result.Status.Should().Be(RemoveUserFromRoleStatus.RoleNotFound);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_UserNotFound_ReturnsUserNotFound()
    {
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((ApplicationUser?)null);

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.RemoveUserFromRoleAsync("role-1", "bad");

        result.Status.Should().Be(RemoveUserFromRoleStatus.UserNotFound);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_Success_ReturnsSuccessWithNames()
    {
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.RemoveFromRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.RemoveUserFromRoleAsync("role-1", "u1");

        result.Status.Should().Be(RemoveUserFromRoleStatus.Success);
        result.UserName.Should().Be("alice");
        result.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_IdentityFails_ReturnsFailedWithErrors()
    {
        var mockRoleManager = CreateMockRoleManager();
        var mockUserManager = CreateMockUserManager();
        var role = new IdentityRole("Editor") { Id = "role-1" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.RemoveFromRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Not in role" }));

        var service = CreateService(mockRoleManager, mockUserManager);

        var result = await service.RemoveUserFromRoleAsync("role-1", "u1");

        result.Status.Should().Be(RemoveUserFromRoleStatus.Failed);
        result.Errors.Should().ContainSingle("Not in role");
    }
}
