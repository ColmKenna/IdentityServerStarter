using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace IdentityServerAspNetIdentity.IntegrationTests.Services;

public class UserEditorTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ApplicationDbContext _appDbContext = default!;
    private UserManager<ApplicationUser> _userManager = default!;
    private IUserEditor _sut = default!;

    public UserEditorTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _appDbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _sut = _scope.ServiceProvider.GetRequiredService<IUserEditor>();

        _appDbContext.Users.RemoveRange(_appDbContext.Users);
        _appDbContext.UserClaims.RemoveRange(_appDbContext.UserClaims);
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
    public async Task UpdateUserFromEditPostAsync_ShouldUpdateProfileFields_WhenValidDataProvided()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "oldname", Email = "old@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            Profile = new UserProfileEditViewModel
            {
                UserId = user.Id,
                Username = "newname",
                Email = "new@test.com",
                EmailConfirmed = true,
                PhoneNumber = "12345",
                PhoneNumberConfirmed = false
            }
        };

        // Act
        var result = await _sut.UpdateUserFromEditPostAsync(request);

        // Assert
        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();

        var updatedUser = await _userManager.FindByIdAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.UserName.Should().Be("newname");
        updatedUser.Email.Should().Be("new@test.com");
        updatedUser.EmailConfirmed.Should().BeTrue();
        updatedUser.PhoneNumber.Should().Be("12345");
    }

    [Fact]
    public async Task UpdateUserFromEditPostAsync_ShouldRemoveAndAddPassword_WhenNewPasswordRequested()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "pwduser", Email = "p@test.com" };
        await _userManager.CreateAsync(user, "OldPass123$");

        var request = new UserEditPostUpdateRequest
        {
            UserId = user.Id,
            NewPassword = "NewPass123$"
        };

        // Act
        var result = await _sut.UpdateUserFromEditPostAsync(request);

        // Assert
        result.UserFound.Should().BeTrue();
        result.Result.Succeeded.Should().BeTrue();

        var pwdCheckOld = await _userManager.CheckPasswordAsync(user, "OldPass123$");
        var pwdCheckNew = await _userManager.CheckPasswordAsync(user, "NewPass123$");
        
        pwdCheckOld.Should().BeFalse();
        pwdCheckNew.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserEditPageDataAsync_ShouldRetrieveAllData_WhenFlagsAreTrue()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "datauser", Email = "d@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.AddClaimAsync(user, new Claim("TestClaim", "DataVal"));
        
        var request = new UserEditPageDataRequest
        {
            UserId = user.Id,
            IncludeUserTabData = true,
            IncludeClaims = true,
            IncludeRoles = true,
            IncludeGrants = true,
            IncludeSessions = true
        };

        // Act
        var result = await _sut.GetUserEditPageDataAsync(request);

        // Assert
        result.Should().NotBeNull();
        result!.Profile.Should().NotBeNull();
        result.Profile.Username.Should().Be("datauser");
        
        result.Claims.Should().ContainSingle(c => c.Type == "TestClaim");
        result.HasPassword.Should().BeTrue();
        result.AccountStatus.Should().Be("Active");
    }
}
