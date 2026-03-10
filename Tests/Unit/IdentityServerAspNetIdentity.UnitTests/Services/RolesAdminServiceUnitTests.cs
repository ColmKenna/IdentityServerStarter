using FluentAssertions;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace IdentityServerAspNetIdentity.UnitTests.Services;

public class RolesAdminServiceUnitTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly RolesAdminService _sut;

    public RolesAdminServiceUnitTests()
    {
        // -------------------------------------------------------------
        // DRAWBACK 1: Mocking massive Framework types requires fake dependencies
        // -------------------------------------------------------------
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        
        // UserManager requires 9 constructor arguments to successfully mock
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        
        // RoleManager requires 6 constructor arguments
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null!, null!, null!, null!, null!);

        _sut = new RolesAdminService(_roleManagerMock.Object, _userManagerMock.Object);
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnSuccess_WhenBothExist()
    {
        // Arrange Variables
        var roleId = "role-1";
        var userId = "user-1";
        
        var role = new IdentityRole { Id = roleId, Name = "TestRole" };
        var user = new ApplicationUser { Id = userId, UserName = "test_user" };

        // -------------------------------------------------------------
        // DRAWBACK 2: Testing the mock behavior instead of the service behavior
        // -------------------------------------------------------------
        // We have to explicitly wire up the behavior of the framework to "work"
        _roleManagerMock.Setup(m => m.FindByIdAsync(roleId)).ReturnsAsync(role);
        _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        
        // IdentityResult has to be faked
        _userManagerMock.Setup(m => m.AddToRoleAsync(user, "TestRole")).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.AddUserToRoleAsync(roleId, userId);

        // Assert Application Logic
        result.Status.Should().Be(AddUserToRoleStatus.Success);
        result.UserName.Should().Be("test_user");
        result.RoleName.Should().Be("TestRole");

        // -------------------------------------------------------------
        // DRAWBACK 3: Brittle verification
        // -------------------------------------------------------------
        // If the service is refactored to look up roles by Name instead of ID, this test fails,
        // even though the application still fundamentally functions correctly!
        _roleManagerMock.Verify(m => m.FindByIdAsync(roleId), Times.Once);
        _userManagerMock.Verify(m => m.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(m => m.AddToRoleAsync(user, "TestRole"), Times.Once);
    }
}
