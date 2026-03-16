using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Services;

public class RolesAdminServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ApplicationDbContext _appDbContext = default!;
    private UserManager<ApplicationUser> _userManager = default!;
    private RoleManager<IdentityRole> _roleManager = default!;
    private IRolesAdminService _sut = default!;

    public RolesAdminServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _appDbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        _sut = _scope.ServiceProvider.GetRequiredService<IRolesAdminService>();

        _appDbContext.Users.RemoveRange(_appDbContext.Users);
        _appDbContext.Roles.RemoveRange(_appDbContext.Roles);
        _appDbContext.UserRoles.RemoveRange(_appDbContext.UserRoles);
        await _appDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddUserToRoleAsync_ShouldReturnSuccess_WhenBothExist()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "roleuser1", Email = "ru1@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");

        var role = new IdentityRole { Name = "TestRole" };
        await _roleManager.CreateAsync(role);

        // Act
        var result = await _sut.AddUserToRoleAsync(role.Id, user.Id);

        // Assert
        result.Status.Should().Be(AddUserToRoleStatus.Success);
        
        var isInRole = await _userManager.IsInRoleAsync(user, "TestRole");
        isInRole.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoleForEditAsync_ShouldCorrectlySplitAvailableAndAssignedUsers_WhenAccessed()
    {
        // Arrange
        var role = new IdentityRole { Name = "SplitRole" };
        await _roleManager.CreateAsync(role);

        var userInRole = new ApplicationUser { UserName = "in_role_user" };
        await _userManager.CreateAsync(userInRole, "Pass123$");
        await _userManager.AddToRoleAsync(userInRole, "SplitRole");

        var availableUser1 = new ApplicationUser { UserName = "avail_user_1" };
        var availableUser2 = new ApplicationUser { UserName = "avail_user_2" };
        await _userManager.CreateAsync(availableUser1, "Pass123$");
        await _userManager.CreateAsync(availableUser2, "Pass123$");

        // Act
        var result = await _sut.GetRoleForEditAsync(role.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UsersInRole.Should().ContainSingle(u => u.UserName == "in_role_user");
        
        result.AvailableUsers.Should().HaveCount(2);
        result.AvailableUsers.Select(u => u.UserName).Should().BeEquivalentTo("avail_user_1", "avail_user_2");
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnRoles_SortedByName()
    {
        // Arrange
        await _roleManager.CreateAsync(new IdentityRole { Name = "ZebraRole" });
        await _roleManager.CreateAsync(new IdentityRole { Name = "AppleRole" });
        await _roleManager.CreateAsync(new IdentityRole { Name = "MangoRole" });

        // Act
        var result = await _sut.GetRolesAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().ContainInOrder("AppleRole", "MangoRole", "ZebraRole");
    }

    [Fact]
    public async Task RemoveUserFromRoleAsync_ShouldReturnSuccess_WhenBothExistAndUserIsInRole()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "roleuser_remove", Email = "remove@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");

        var role = new IdentityRole { Name = "TestRoleToRemove" };
        await _roleManager.CreateAsync(role);

        await _userManager.AddToRoleAsync(user, "TestRoleToRemove");

        // Act
        var result = await _sut.RemoveUserFromRoleAsync(role.Id, user.Id);

        // Assert
        result.Status.Should().Be(RemoveUserFromRoleStatus.Success);
        
        var isInRole = await _userManager.IsInRoleAsync(user, "TestRoleToRemove");
        isInRole.Should().BeFalse();
    }
}
