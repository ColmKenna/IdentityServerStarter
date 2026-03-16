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
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly RolesAdminService _sut;

    public RolesAdminServiceTests()
    {
        _mockRoleManager = MockRoleManager();
        _mockUserManager = MockUserManager();
        _sut = new RolesAdminService(_mockRoleManager.Object, _mockUserManager.Object);
    }

    private static Mock<RoleManager<IdentityRole>> MockRoleManager()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        return new Mock<RoleManager<IdentityRole>>(store.Object, null!, null!, null!, null!);
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnRolesMappedAndSorted()
    {
        // Arrange
        var roles = new List<IdentityRole>
        {
            new IdentityRole { Id = "2", Name = "Zebra" },
            new IdentityRole { Id = "1", Name = "Apple" }
        };
        _mockRoleManager.Setup(x => x.Roles).Returns(roles.BuildMock());

        // Act
        var result = await _sut.GetRolesAsync();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Apple");
        result[1].Name.Should().Be("Zebra");
    }

    [Fact]
    public async Task GetRoleForEditAsync_ShouldReturnNull_WhenRoleDoesNotExist()
    {
        // Arrange
        _mockRoleManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityRole?)null);

        // Act
        var result = await _sut.GetRoleForEditAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRoleForEditAsync_ShouldSeparateUsersIntoBuckets()
    {
        // Arrange
        var role = new IdentityRole { Id = "role1", Name = "Admin" };
        _mockRoleManager.Setup(x => x.FindByIdAsync("role1")).ReturnsAsync(role);

        var alice = new ApplicationUser { Id = "u1", UserName = "Alice", Email = "alice@test.com" };
        var bob = new ApplicationUser { Id = "u2", UserName = "Bob", Email = "bob@test.com" };
        var charlie = new ApplicationUser { Id = "u3", UserName = "Charlie", Email = "charlie@test.com" };

        var allUsers = new List<ApplicationUser> { bob, alice, charlie };
        _mockUserManager.Setup(x => x.Users).Returns(allUsers.BuildMock());

        // Users already in the role
        _mockUserManager.Setup(x => x.GetUsersInRoleAsync("Admin")).ReturnsAsync(new List<ApplicationUser> { alice });

        // Act
        var result = await _sut.GetRoleForEditAsync("role1");

        // Assert
        result.Should().NotBeNull();
        result!.RoleName.Should().Be("Admin");
        
        result.UsersInRole.Should().ContainSingle(u => u.UserName == "Alice");
        
        result.AvailableUsers.Should().HaveCount(2);
        // Checking sorting logic too
        result.AvailableUsers[0].UserName.Should().Be("Bob");
        result.AvailableUsers[1].UserName.Should().Be("Charlie");
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnRoleNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        _mockRoleManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityRole?)null);

        // Act
        var result = await _sut.AddUserToRoleAsync("nonexistent", "userId");

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.RoleNotFound);
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var role = new IdentityRole { Id = "role1", Name = "Admin" };
        _mockRoleManager.Setup(x => x.FindByIdAsync("role1")).ReturnsAsync(role);
        _mockUserManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.AddUserToRoleAsync("role1", "nonexistent");

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.UserNotFound);
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnFailed_WhenManagerFails()
    {
        // Arrange
        var role = new IdentityRole { Id = "role1", Name = "Admin" };
        var user = new ApplicationUser { Id = "user1", UserName = "Alice" };

        _mockRoleManager.Setup(x => x.FindByIdAsync("role1")).ReturnsAsync(role);
        _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        var identityResult = IdentityResult.Failed(new IdentityError { Description = "Add failed" });
        _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Admin")).ReturnsAsync(identityResult);

        // Act
        var result = await _sut.AddUserToRoleAsync("role1", "user1");

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.Failed);
        result.Errors.Should().Contain("Add failed");
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnSuccess_WhenAddedSuccessfully()
    {
        // Arrange
        var role = new IdentityRole { Id = "role1", Name = "Admin" };
        var user = new ApplicationUser { Id = "user1", UserName = "Alice" };

        _mockRoleManager.Setup(x => x.FindByIdAsync("role1")).ReturnsAsync(role);
        _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.AddToRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.AddUserToRoleAsync("role1", "user1");

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.Success);
        result.UserName.Should().Be("Alice");
        result.RoleName.Should().Be("Admin");
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnRoleNotFound_WhenRoleDoesNotExist()
    {
        // Arrange
        _mockRoleManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((IdentityRole?)null);

        // Act
        var result = await _sut.RemoveUserFromRoleAsync("nonexistent", "userId");

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.RoleNotFound);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnUserNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var role = new IdentityRole { Id = "role1", Name = "Admin" };
        _mockRoleManager.Setup(x => x.FindByIdAsync("role1")).ReturnsAsync(role);
        _mockUserManager.Setup(x => x.FindByIdAsync("nonexistent")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.RemoveUserFromRoleAsync("role1", "nonexistent");

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.UserNotFound);
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnFailed_WhenManagerFails()
    {
        // Arrange
        var role = new IdentityRole { Id = "role1", Name = "Admin" };
        var user = new ApplicationUser { Id = "user1", UserName = "Alice" };

        _mockRoleManager.Setup(x => x.FindByIdAsync("role1")).ReturnsAsync(role);
        _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        var identityResult = IdentityResult.Failed(new IdentityError { Description = "Remove failed" });
        _mockUserManager.Setup(x => x.RemoveFromRoleAsync(user, "Admin")).ReturnsAsync(identityResult);

        // Act
        var result = await _sut.RemoveUserFromRoleAsync("role1", "user1");

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.Failed);
        result.Errors.Should().Contain("Remove failed");
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnSuccess_WhenRemovedSuccessfully()
    {
        // Arrange
        var role = new IdentityRole { Id = "role1", Name = "Admin" };
        var user = new ApplicationUser { Id = "user1", UserName = "Alice" };

        _mockRoleManager.Setup(x => x.FindByIdAsync("role1")).ReturnsAsync(role);
        _mockUserManager.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.RemoveFromRoleAsync(user, "Admin")).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.RemoveUserFromRoleAsync("role1", "user1");

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.Success);
        result.UserName.Should().Be("Alice");
        result.RoleName.Should().Be("Admin");
    }
}
