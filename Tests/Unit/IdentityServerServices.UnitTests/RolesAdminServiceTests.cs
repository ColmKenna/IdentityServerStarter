using FluentAssertions;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class RolesAdminServiceTests
{
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly RolesAdminService _service;

    public RolesAdminServiceTests()
    {
        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null!, null!, null!, null!);

        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockUserManager.SetupGet(m => m.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>([]));

        _service = new RolesAdminService(_mockRoleManager.Object, _mockUserManager.Object);
    }

    private void SetupRoles(params IdentityRole[] roles)
    {
        _mockRoleManager.Setup(m => m.Roles)
            .Returns(new TestAsyncEnumerable<IdentityRole>(roles));
    }

    private void SetupAllUsers(params ApplicationUser[] users)
    {
        _mockUserManager.SetupGet(m => m.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>(users));
    }

    private void ArrangeRoleWithUsers(
        IdentityRole role,
        IList<ApplicationUser> usersInRole,
        IReadOnlyList<ApplicationUser>? allUsers = null)
    {
        _mockRoleManager.Setup(m => m.FindByIdAsync(role.Id)).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.GetUsersInRoleAsync(role.Name!))
            .ReturnsAsync(usersInRole);
        if (allUsers != null)
            SetupAllUsers([.. allUsers]);
    }

    private static IdentityRole CreateRole(string name = "Editor", string id = "role-1")
    {
        return new IdentityRole(name) { Id = id };
    }

    private static ApplicationUser CreateUser(string id, string userName, string? email = null)
    {
        return new ApplicationUser { Id = id, UserName = userName, Email = email ?? $"{userName}@t.com" };
    }

    [Fact]
    public async Task GetRolesAsync_NoRoles_ReturnsEmptyList()
    {
        SetupRoles();

        var result = await _service.GetRolesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesAsync_RolesExist_ReturnsAllRoles()
    {
        SetupRoles(
            new IdentityRole("Admin") { Id = "1" },
            new IdentityRole("Editor") { Id = "2" },
            new IdentityRole("Viewer") { Id = "3" });

        var result = await _service.GetRolesAsync();

        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().Contain(["Admin", "Editor", "Viewer"]);
    }

    [Fact]
    public async Task GetRolesAsync_RolesExist_ReturnedInAscendingOrderByName()
    {
        SetupRoles(
            new IdentityRole("Zebra") { Id = "1" },
            new IdentityRole("Admin") { Id = "2" },
            new IdentityRole("Manager") { Id = "3" });

        var result = await _service.GetRolesAsync();

        result.Select(r => r.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetRolesAsync_NullRoleName_ReturnsEmptyString()
    {
        SetupRoles(new IdentityRole { Id = "1", Name = null });

        var result = await _service.GetRolesAsync();

        result.Should().ContainSingle().Which.Name.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesAsync_MapsIdCorrectly()
    {
        SetupRoles(new IdentityRole("Admin") { Id = "abc-123" });

        var result = await _service.GetRolesAsync();

        result.Should().ContainSingle().Which.Id.Should().Be("abc-123");
    }

    [Fact]
    public async Task GetRoleForEditAsync_RoleNotFound_ReturnsNull()
    {
        _mockRoleManager.Setup(m => m.FindByIdAsync("bad"))
            .ReturnsAsync((IdentityRole?)null);

        var result = await _service.GetRoleForEditAsync("bad");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRoleForEditAsync_RoleExists_ReturnsRoleName()
    {
        var role = CreateRole();
        ArrangeRoleWithUsers(role, []);

        var result = await _service.GetRoleForEditAsync("role-1");

        result.Should().NotBeNull();
        result!.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task GetRoleForEditAsync_RoleHasUsers_PopulatesUsersInRole()
    {
        var role = CreateRole();
        var usersInRole = new List<ApplicationUser>
        {
            CreateUser("u1", "alice"),
            CreateUser("u2", "bob")
        };
        ArrangeRoleWithUsers(role, usersInRole, usersInRole);

        var result = await _service.GetRoleForEditAsync("role-1");

        result!.UsersInRole.Should().HaveCount(2);
        result.UsersInRole.Select(u => u.UserName).Should().Contain("alice");
        result.UsersInRole.Select(u => u.UserName).Should().Contain("bob");
    }

    [Fact]
    public async Task GetRoleForEditAsync_UsersNotInRole_PopulatesAvailableUsers()
    {
        var role = CreateRole();
        var alice = CreateUser("u1", "alice");
        var usersInRole = new List<ApplicationUser> { alice };
        var allUsers = new List<ApplicationUser>
        {
            alice,
            CreateUser("u2", "bob"),
            CreateUser("u3", "charlie")
        };
        ArrangeRoleWithUsers(role, usersInRole, allUsers);

        var result = await _service.GetRoleForEditAsync("role-1");

        result!.AvailableUsers.Should().HaveCount(2);
        result.AvailableUsers.Select(u => u.UserName).Should().Contain("bob");
        result.AvailableUsers.Select(u => u.UserName).Should().Contain("charlie");
        result.AvailableUsers.Select(u => u.UserName).Should().NotContain("alice");
    }

    [Fact]
    public async Task GetRoleForEditAsync_NoUsersInRole_UsersInRoleIsEmpty()
    {
        var role = CreateRole("EmptyRole", "role-2");
        var allUsers = new List<ApplicationUser> { CreateUser("u1", "alice") };
        ArrangeRoleWithUsers(role, [], allUsers);

        var result = await _service.GetRoleForEditAsync("role-2");

        result!.UsersInRole.Should().BeEmpty();
        result.AvailableUsers.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRoleForEditAsync_UsersInRole_OrderedByUserName()
    {
        var role = CreateRole();
        var usersInRole = new List<ApplicationUser>
        {
            CreateUser("u2", "zara"),
            CreateUser("u1", "alice")
        };
        ArrangeRoleWithUsers(role, usersInRole, usersInRole);

        var result = await _service.GetRoleForEditAsync("role-1");

        result!.UsersInRole.Select(u => u.UserName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetRoleForEditAsync_AvailableUsers_OrderedByUserName()
    {
        var role = CreateRole();
        var alice = CreateUser("u1", "alice");
        var allUsers = new List<ApplicationUser>
        {
            alice,
            CreateUser("u4", "zara"),
            CreateUser("u2", "bob"),
            CreateUser("u3", "charlie")
        };
        ArrangeRoleWithUsers(role, [alice], allUsers);

        var result = await _service.GetRoleForEditAsync("role-1");

        result!.AvailableUsers.Select(u => u.UserName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetRoleForEditAsync_NullRoleName_MapsToEmptyString()
    {
        var role = new IdentityRole { Id = "role-1", Name = null };
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.GetUsersInRoleAsync(null!)).ReturnsAsync([]);

        var result = await _service.GetRoleForEditAsync("role-1");

        result!.RoleName.Should().BeEmpty();
    }

    [Fact]
    public async Task AddUserToRoleAsync_RoleNotFound_ReturnsRoleNotFound()
    {
        _mockRoleManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((IdentityRole?)null);

        var result = await _service.AddUserToRoleAsync("bad", "u1");

        result.Status.Should().Be(AddUserToRoleStatus.RoleNotFound);
    }

    [Fact]
    public async Task AddUserToRoleAsync_UserNotFound_ReturnsUserNotFound()
    {
        var role = CreateRole();
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((ApplicationUser?)null);

        var result = await _service.AddUserToRoleAsync("role-1", "bad");

        result.Status.Should().Be(AddUserToRoleStatus.UserNotFound);
    }

    [Fact]
    public async Task AddUserToRoleAsync_Success_ReturnsSuccessWithNames()
    {
        var role = CreateRole();
        var user = CreateUser("u1", "alice");
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.AddToRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.AddUserToRoleAsync("role-1", "u1");

        result.Status.Should().Be(AddUserToRoleStatus.Success);
        result.UserName.Should().Be("alice");
        result.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task AddUserToRoleAsync_IdentityFails_ReturnsFailedWithErrors()
    {
        var role = CreateRole();
        var user = CreateUser("u1", "alice");
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.AddToRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User already in role" }));

        var result = await _service.AddUserToRoleAsync("role-1", "u1");

        result.Status.Should().Be(AddUserToRoleStatus.Failed);
        result.Errors.Should().ContainSingle("User already in role");
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_RoleNotFound_ReturnsRoleNotFound()
    {
        _mockRoleManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((IdentityRole?)null);

        var result = await _service.RemoveUserFromRoleAsync("bad", "u1");

        result.Status.Should().Be(RemoveUserFromRoleStatus.RoleNotFound);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_UserNotFound_ReturnsUserNotFound()
    {
        var role = CreateRole();
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.FindByIdAsync("bad")).ReturnsAsync((ApplicationUser?)null);

        var result = await _service.RemoveUserFromRoleAsync("role-1", "bad");

        result.Status.Should().Be(RemoveUserFromRoleStatus.UserNotFound);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_Success_ReturnsSuccessWithNames()
    {
        var role = CreateRole();
        var user = CreateUser("u1", "alice");
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.RemoveFromRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _service.RemoveUserFromRoleAsync("role-1", "u1");

        result.Status.Should().Be(RemoveUserFromRoleStatus.Success);
        result.UserName.Should().Be("alice");
        result.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_IdentityFails_ReturnsFailedWithErrors()
    {
        var role = CreateRole();
        var user = CreateUser("u1", "alice");
        _mockRoleManager.Setup(m => m.FindByIdAsync("role-1")).ReturnsAsync(role);
        _mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.RemoveFromRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Not in role" }));

        var result = await _service.RemoveUserFromRoleAsync("role-1", "u1");

        result.Status.Should().Be(RemoveUserFromRoleStatus.Failed);
        result.Errors.Should().ContainSingle("Not in role");
    }
}
