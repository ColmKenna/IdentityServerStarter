using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using Moq;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class ClaimsAdminServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

    public ClaimsAdminServiceTests()
    {
        _userManagerMock = MockUserManager<ApplicationUser>();
    }

    private static ApplicationDbContext CreateApplicationDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;
            
        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private ClaimsAdminService CreateSut(ApplicationDbContext context) =>
        new(context, _userManagerMock.Object);

    private static Mock<UserManager<TUser>> MockUserManager<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        return new Mock<UserManager<TUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    // -------------------------------------------------------------------------
    // GetClaimsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnDistinctSortedClaims()
    {
        await using var dbContext = CreateApplicationDbContext();

        var testUser = new ApplicationUser { Id = "user1", UserName = "test_user1", Email = "test1@example.com" };
        dbContext.Users.Add(testUser);

        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = "email", ClaimValue = "a" },
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = "role", ClaimValue = "b" },
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = "email", ClaimValue = "c" }, // Duplicate type
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = null!, ClaimValue = "d" }    // Null type should be ignored
        );
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(dbContext);

        var result = await sut.GetClaimsAsync();

        result.Should().HaveCount(2);
        result[0].ClaimType.Should().Be("email");
        result[1].ClaimType.Should().Be("role");
    }

    // -------------------------------------------------------------------------
    // GetForEditAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetForEditAsync_ShouldReturnNull_WhenNoUsersHaveClaim()
    {
        await using var context = CreateApplicationDbContext();
        var sut = CreateSut(context);

        var result = await sut.GetForEditAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForEditAsync_ShouldSetIsLastUserAssignmentCorrectly()
    {
        await using var context = CreateApplicationDbContext();
        
        var testUser1 = new ApplicationUser { Id = "u1", UserName = "Alice", Email = "alice@example.com" };
        var testUser2 = new ApplicationUser { Id = "u2", UserName = "Bob", Email = "bob@example.com" };
        var testUser3 = new ApplicationUser { Id = "u3", UserName = "third", Email = "other@example.com" };
        var testUser4 = new ApplicationUser { Id = "u4", UserName = "Charlie", Email = "charlie@example.com" };
        context.Users.AddRange(testUser1, testUser2, testUser3, testUser4);

        context.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "role", ClaimValue = "admin" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "role", ClaimValue = "user" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "role", ClaimValue = "manager" },
            new IdentityUserClaim<string> { UserId = "u3", ClaimType = "other", ClaimValue = "irrelevant" }
        );
        await context.SaveChangesAsync();

        var users = new List<ApplicationUser>
        {
            testUser1,
            testUser2,
            testUser4 // Available user
        };
        _userManagerMock.Setup(m => m.Users).Returns(users.BuildMock());

        var sut = CreateSut(context);

        // Act
        var result = await sut.GetForEditAsync("role");

        // Assert
        result.Should().NotBeNull();
        
        // u1 has 1 assignment. But uniqueUsersCount is 2 (u1, u2), so u1 is NOT the last total assignment
        var aliceAssignment = result!.UsersInClaim.Single(u => u.UserId == "u1");
        aliceAssignment.IsLastUserAssignment.Should().BeFalse();

        // u2 has 2 assignments. Never the last.
        var bobAssignments = result.UsersInClaim.Where(u => u.UserId == "u2").ToList();
        bobAssignments.Should().HaveCount(2).And.AllSatisfy(a => a.IsLastUserAssignment.Should().BeFalse());
        
        result.AvailableUsers.Should().ContainSingle().Which.UserId.Should().Be("u4");
    }

    [Fact]
    public async Task GetForEditAsync_ShouldSetIsLastUserAssignmentTrue_WhenOnlyOneUserWithOneAssignment()
    {
        await using var context = CreateApplicationDbContext();

        var testUser = new ApplicationUser { Id = "u1", UserName = "Alice", Email = "alice@example.com" };
        context.Users.Add(testUser);

        context.UserClaims.Add(new IdentityUserClaim<string> { UserId = "u1", ClaimType = "role", ClaimValue = "admin" });
        await context.SaveChangesAsync();

        var users = new List<ApplicationUser> { new() { Id = "u1", UserName = "Alice" } };
        _userManagerMock.Setup(m => m.Users).Returns(users.BuildMock());

        var sut = CreateSut(context);

        // Act
        var result = await sut.GetForEditAsync("role");

        // Assert
        result!.UsersInClaim.Single().IsLastUserAssignment.Should().BeTrue();
    }
    
    [Fact]
    public async Task GetForEditAsync_ShouldDefaultNewClaimValueToTrueString_WhenAllValuesAreBooleans()
    {
        await using var context = CreateApplicationDbContext();

        var testUser1 = new ApplicationUser { Id = "u1", UserName = "test_user1", Email = "test1@example.com" };
        var testUser2 = new ApplicationUser { Id = "u2", UserName = "test_user2", Email = "test2@example.com" };
        context.Users.AddRange(testUser1, testUser2);

        context.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "boolClaim", ClaimValue = "true" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "boolClaim", ClaimValue = "false" }
        );
        await context.SaveChangesAsync();

        var users = new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "Alice" },
            new() { Id = "u2", UserName = "Bob" },
        };
        _userManagerMock.Setup(m => m.Users).Returns(users.BuildMock());

        var sut = CreateSut(context);

        var result = await sut.GetForEditAsync("boolClaim", null);

        result!.NewClaimValue.Should().Be("true");
    }

    // -------------------------------------------------------------------------
    // AddUserToClaimAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddUserToClaimAsync_ShouldReturnUserNotFound()
    {
        await using var context = CreateApplicationDbContext();
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync((ApplicationUser?)null);
        var sut = CreateSut(context);

        var result = await sut.AddUserToClaimAsync("role", "u1", "admin");

        result.Status.Should().Be(AddClaimAssignmentStatus.UserNotFound);
    }

    [Fact]
    public async Task AddUserToClaimAsync_ShouldReturnAlreadyAssigned()
    {
        await using var context = CreateApplicationDbContext();

        var testUser = new ApplicationUser { Id = "u1", UserName = "test_user1", Email = "test1@example.com" };
        context.Users.Add(testUser);

        context.UserClaims.Add(new IdentityUserClaim<string> { UserId = "u1", ClaimType = "role", ClaimValue = "admin" });
        await context.SaveChangesAsync();

        var user = new ApplicationUser { Id = "u1", UserName = "Alice" };
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        
        var sut = CreateSut(context);

        var result = await sut.AddUserToClaimAsync("role", "u1", "admin");

        result.Status.Should().Be(AddClaimAssignmentStatus.AlreadyAssigned);
    }

    [Fact]
    public async Task AddUserToClaimAsync_ShouldReturnIdentityFailure_IfUserManagerFails()
    {
        await using var context = CreateApplicationDbContext();
        var user = new ApplicationUser { Id = "u1", UserName = "Alice" };
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        
        _userManagerMock.Setup(m => m.AddClaimAsync(user, It.IsAny<System.Security.Claims.Claim>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error adding claim" }));

        var sut = CreateSut(context);

        var result = await sut.AddUserToClaimAsync("role", "u1", "admin");

        result.Status.Should().Be(AddClaimAssignmentStatus.IdentityFailure);
        result.Errors.Should().ContainSingle().Which.Should().Be("Error adding claim");
    }

    [Fact]
    public async Task AddUserToClaimAsync_ShouldReturnSuccess_WhenValid()
    {
        await using var context = CreateApplicationDbContext();
        var user = new ApplicationUser { Id = "u1", UserName = "Alice" };
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        
        _userManagerMock.Setup(m => m.AddClaimAsync(user, It.Is<System.Security.Claims.Claim>(c => c.Type == "role" && c.Value == "admin")))
            .ReturnsAsync(IdentityResult.Success);

        var sut = CreateSut(context);

        var result = await sut.AddUserToClaimAsync("role", "u1", "admin");

        result.Status.Should().Be(AddClaimAssignmentStatus.Success);
        result.UserName.Should().Be("Alice");
    }

    // -------------------------------------------------------------------------
    // RemoveUserFromClaimAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveUserFromClaimAsync_ShouldReturnAssignmentNotFound()
    {
        await using var context = CreateApplicationDbContext();
        var user = new ApplicationUser { Id = "u1", UserName = "Alice" };
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        
        var sut = CreateSut(context);

        var result = await sut.RemoveUserFromClaimAsync("role", "u1", "admin");

        result.Status.Should().Be(RemoveClaimAssignmentStatus.AssignmentNotFound);
    }

    [Fact]
    public async Task RemoveUserFromClaimAsync_ShouldDetermineHasRemainingAssignments()
    {
        await using var context = CreateApplicationDbContext();

        var testUser1 = new ApplicationUser { Id = "u1", UserName = "test_user1", Email = "test1@example.com" };
        var testUser2 = new ApplicationUser { Id = "u2", UserName = "test_user2", Email = "test2@example.com" };
        context.Users.AddRange(testUser1, testUser2);

        // Setup two assignments, one will be removed
        context.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "role", ClaimValue = "admin" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "role", ClaimValue = "user" }
        );
        await context.SaveChangesAsync();

        var user = new ApplicationUser { Id = "u1", UserName = "Alice" };
        _userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.RemoveClaimAsync(user, It.IsAny<System.Security.Claims.Claim>()))
            .ReturnsAsync(IdentityResult.Success);
            
        var sut = CreateSut(context);

        var result = await sut.RemoveUserFromClaimAsync("role", "u1", "admin");

        result.Status.Should().Be(RemoveClaimAssignmentStatus.Success);
        result.HasRemainingAssignments.Should().BeTrue(); // because u2 still has role="user"
    }
}
