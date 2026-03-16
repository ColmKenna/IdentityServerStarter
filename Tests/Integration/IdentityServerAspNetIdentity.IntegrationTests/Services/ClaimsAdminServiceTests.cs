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

public class ClaimsAdminServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ApplicationDbContext _appDbContext = default!;
    private UserManager<ApplicationUser> _userManager = default!;
    private IClaimsAdminService _sut = default!;

    public ClaimsAdminServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _appDbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _sut = _scope.ServiceProvider.GetRequiredService<IClaimsAdminService>();

        _appDbContext.Users.RemoveRange(_appDbContext.Users);
        _appDbContext.UserClaims.RemoveRange(_appDbContext.UserClaims);
        await _appDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddUserToClaimAsync_ShouldAddClaim_WhenUserExists()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "claimuser1", Email = "cu1@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");

        // Act
        var result = await _sut.AddUserToClaimAsync("TestClaim", user.Id, "value1");

        // Assert
        result.Status.Should().Be(AddClaimAssignmentStatus.Success);
        
        var claims = await _userManager.GetClaimsAsync(user);
        claims.Should().ContainSingle(c => c.Type == "TestClaim" && c.Value == "value1");
    }

    [Fact]
    public async Task RemoveUserFromClaimAsync_ShouldRemoveClaimAndReportRemainingAssignments()
    {
        // Arrange — two users with the same claim type
        var user1 = new ApplicationUser { UserName = "claimuser_rm1", Email = "rm1@test.com" };
        var user2 = new ApplicationUser { UserName = "claimuser_rm2", Email = "rm2@test.com" };
        await _userManager.CreateAsync(user1, "Pass123$");
        await _userManager.CreateAsync(user2, "Pass123$");
        await _userManager.AddClaimAsync(user1, new Claim("SharedClaim", "val1"));
        await _userManager.AddClaimAsync(user2, new Claim("SharedClaim", "val2"));

        // Act — remove user1's assignment
        var result = await _sut.RemoveUserFromClaimAsync("SharedClaim", user1.Id, "val1");

        // Assert
        result.Status.Should().Be(RemoveClaimAssignmentStatus.Success);
        result.UserName.Should().Be("claimuser_rm1");
        result.HasRemainingAssignments.Should().BeTrue();

        var user1Claims = await _userManager.GetClaimsAsync(user1);
        user1Claims.Should().NotContain(c => c.Type == "SharedClaim");
    }

    [Fact]
    public async Task GetForEditAsync_ShouldFlagLastAssignment_WhenOnlyOneUserHasClaim()
    {
        // Arrange
        var user = new ApplicationUser { UserName = "claimuser2", Email = "cu2@test.com" };
        await _userManager.CreateAsync(user, "Pass123$");
        await _userManager.AddClaimAsync(user, new Claim("UniqueClaim", "val"));

        // Act
        var result = await _sut.GetForEditAsync("UniqueClaim");

        // Assert
        result.Should().NotBeNull();
        result!.UsersInClaim.Should().ContainSingle();
        result.UsersInClaim[0].IsLastUserAssignment.Should().BeTrue();
    }
}
