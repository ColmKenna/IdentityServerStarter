using FluentAssertions;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using MockQueryable;
using Moq;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class RolesAdminServiceTests
{
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

    public RolesAdminServiceTests()
    {
        _roleManagerMock = MockRoleManager<IdentityRole>();
        _userManagerMock = MockUserManager<ApplicationUser>();
    }

    private RolesAdminService CreateSut() =>
        new(_roleManagerMock.Object, _userManagerMock.Object);

    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        return new Mock<UserManager<TUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
    {
        var store = new Mock<IRoleStore<TRole>>();
        return new Mock<RoleManager<TRole>>(store.Object, null!, null!, null!, null!);
    }

    // -------------------------------------------------------------------------
    // GetRolesAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetRolesAsync_ShouldReturnEmptyList_WhenNoRolesExist()
    {
        // Arrange
        var roles = new List<IdentityRole>();
        _roleManagerMock.Setup(m => m.Roles).Returns(roles.BuildMock());

        var sut = CreateSut();

        // Act
        var result = await sut.GetRolesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnRolesSortedByName()
    {
        // Arrange
        var roles = new List<IdentityRole>
        {
            new() { Id = "3", Name = "Zebra" },
            new() { Id = "1", Name = "Admin" },
            new() { Id = "2", Name = "Manager" }
        };
        _roleManagerMock.Setup(m => m.Roles).Returns(roles.BuildMock());

        var sut = CreateSut();

        // Act
        var result = await sut.GetRolesAsync();

        // Assert
        result.Select(r => r.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetRolesAsync_ShouldMapRoleIdAndNameToDto()
    {
        // Arrange
        var roles = new List<IdentityRole>
        {
            new() { Id = "role-123", Name = "Admin" }
        };
        _roleManagerMock.Setup(m => m.Roles).Returns(roles.BuildMock());

        var sut = CreateSut();

        // Act
        var result = await sut.GetRolesAsync();

        // Assert
        result.Should().ContainSingle().Which.Should().BeEquivalentTo(new RoleListItemDto
        {
            Id = "role-123",
            Name = "Admin"
        });
    }

    // -------------------------------------------------------------------------
    // GetRoleForEditAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetRoleForEditAsync_ShouldReturnNull_WhenRoleNotFound()
    {
        // Arrange
        _roleManagerMock.Setup(m => m.FindByIdAsync("missing"))
            .ReturnsAsync((IdentityRole?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetRoleForEditAsync("missing");

        // Assert
        result.Should().BeNull();
        _userManagerMock.Verify(m => m.GetUsersInRoleAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetRoleForEditAsync_ShouldReturnRoleNameFromRole()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);
        _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync([]);
        _userManagerMock.Setup(m => m.Users)
            .Returns(new List<ApplicationUser>().BuildMock());

        var sut = CreateSut();

        // Act
        var result = await sut.GetRoleForEditAsync("r1");

        // Assert
        result!.RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task GetRoleForEditAsync_ShouldPartitionUsersCorrectly_IntoUsersInRoleAndAvailableUsers()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);

        var userA = new ApplicationUser { Id = "A", UserName = "Alice", Email = "alice@test.com" };
        var userB = new ApplicationUser { Id = "B", UserName = "Bob", Email = "bob@test.com" };
        var userC = new ApplicationUser { Id = "C", UserName = "Carol", Email = "carol@test.com" };

        _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync([userA, userB]);
        _userManagerMock.Setup(m => m.Users)
            .Returns(new List<ApplicationUser> { userA, userB, userC }.BuildMock());

        var sut = CreateSut();

        // Act
        var result = await sut.GetRoleForEditAsync("r1");

        // Assert
        result!.UsersInRole.Should().HaveCount(2)
            .And.Contain(u => u.Id == "A")
            .And.Contain(u => u.Id == "B");

        result.AvailableUsers.Should().ContainSingle()
            .Which.Id.Should().Be("C");
    }

    [Fact]
    public async Task GetRoleForEditAsync_ShouldSortUsersInRoleByUserName()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);

        var usersInRole = new List<ApplicationUser>
        {
            new() { Id = "2", UserName = "Zelda", Email = "z@test.com" },
            new() { Id = "1", UserName = "Alice", Email = "a@test.com" }
        };

        _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync(usersInRole);
        _userManagerMock.Setup(m => m.Users)
            .Returns(usersInRole.BuildMock());

        var sut = CreateSut();

        // Act
        var result = await sut.GetRoleForEditAsync("r1");

        // Assert
        result!.UsersInRole.Select(u => u.UserName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetRoleForEditAsync_ShouldSortAvailableUsersByUserName()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);

        var availableUsers = new List<ApplicationUser>
        {
            new() { Id = "2", UserName = "Zelda", Email = "z@test.com" },
            new() { Id = "1", UserName = "Alice", Email = "a@test.com" }
        };

        _userManagerMock.Setup(m => m.GetUsersInRoleAsync("Admin"))
            .ReturnsAsync([]);
        _userManagerMock.Setup(m => m.Users)
            .Returns(availableUsers.BuildMock());

        var sut = CreateSut();

        // Act
        var result = await sut.GetRoleForEditAsync("r1");

        // Assert
        result!.AvailableUsers.Select(u => u.UserName).Should().BeInAscendingOrder();
    }

    // -------------------------------------------------------------------------
    // AddUserToRoleAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnRoleNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        _roleManagerMock.Setup(m => m.FindByIdAsync("r1"))
            .ReturnsAsync((IdentityRole?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.AddUserToRoleAsync("r1", "u1");

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.RoleNotFound);
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);
        _userManagerMock.Setup(m => m.FindByIdAsync("u1"))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.AddUserToRoleAsync("r1", "u1");

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.UserNotFound);
        _userManagerMock.Verify(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnFailed_WithErrors_WhenAddToRoleAsyncFails()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Already in role" }));

        var sut = CreateSut();

        // Act
        var result = await sut.AddUserToRoleAsync("r1", "u1");

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.Failed);
        result.Errors.Should().ContainSingle().Which.Should().Be("Already in role");
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnSuccess_WithUserAndRoleNames_WhenOperationSucceeds()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateSut();

        // Act
        var result = await sut.AddUserToRoleAsync("r1", "u1");

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.Success);
        result.UserName.Should().Be("alice");
        result.RoleName.Should().Be("Admin");
        _userManagerMock.Verify(m => m.AddToRoleAsync(user, "Admin"), Times.Once);
    }

    // -------------------------------------------------------------------------
    // RemoveUserFromRoleAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnRoleNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        _roleManagerMock.Setup(m => m.FindByIdAsync("r1"))
            .ReturnsAsync((IdentityRole?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.RemoveUserFromRoleAsync("r1", "u1");

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.RoleNotFound);
        _userManagerMock.Verify(m => m.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);
        _userManagerMock.Setup(m => m.FindByIdAsync("u1"))
            .ReturnsAsync((ApplicationUser?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.RemoveUserFromRoleAsync("r1", "u1");

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.UserNotFound);
        _userManagerMock.Verify(m => m.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnFailed_WithErrors_WhenRemoveFromRoleAsyncFails()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.RemoveFromRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Not in role" }));

        var sut = CreateSut();

        // Act
        var result = await sut.RemoveUserFromRoleAsync("r1", "u1");

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.Failed);
        result.Errors.Should().ContainSingle().Which.Should().Be("Not in role");
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnSuccess_WithUserAndRoleNames_WhenOperationSucceeds()
    {
        // Arrange
        var role = new IdentityRole { Id = "r1", Name = "Admin" };
        var user = new ApplicationUser { Id = "u1", UserName = "alice" };

        _roleManagerMock.Setup(m => m.FindByIdAsync("r1")).ReturnsAsync(role);
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.RemoveFromRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateSut();

        // Act
        var result = await sut.RemoveUserFromRoleAsync("r1", "u1");

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.Success);
        result.UserName.Should().Be("alice");
        result.RoleName.Should().Be("Admin");
        _userManagerMock.Verify(m => m.RemoveFromRoleAsync(user, "Admin"), Times.Once);
    }
}
