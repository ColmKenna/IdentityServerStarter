using System.Security.Claims;
using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class ClaimsAdminServiceTests
{
    private sealed class SqliteApplicationDb : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public SqliteApplicationDb()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext(_options);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager(
        IReadOnlyList<ApplicationUser>? users = null)
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        mockUserManager.SetupGet(manager => manager.Users)
            .Returns(new TestAsyncEnumerable<ApplicationUser>(users ?? []));

        return mockUserManager;
    }

    private static ClaimsAdminService CreateService(
        ApplicationDbContext dbContext,
        Mock<UserManager<ApplicationUser>> mockUserManager)
    {
        return new ClaimsAdminService(dbContext, mockUserManager.Object);
    }

    private static ApplicationUser CreateUser(string id, string userName, string? email = null)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email?.ToUpperInvariant()
        };
    }

    [Fact]
    public async Task GetClaimsAsync_NoClaims_ReturnsEmpty()
    {
        using var sqliteDb = new SqliteApplicationDb();
        await using var dbContext = sqliteDb.CreateContext();
        var service = CreateService(dbContext, CreateMockUserManager());

        var result = await service.GetClaimsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetClaimsAsync_DistinctOrdered_ExcludesNullAndEmpty()
    {
        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.AddRange(
                CreateUser("u1", "user1"),
                CreateUser("u2", "user2"),
                CreateUser("u3", "user3"));
            seedContext.UserClaims.AddRange(
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = null, ClaimValue = "x" },
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = string.Empty, ClaimValue = "x" },
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "zeta", ClaimValue = "x" },
                new IdentityUserClaim<string> { UserId = "u2", ClaimType = "alpha", ClaimValue = "x" },
                new IdentityUserClaim<string> { UserId = "u3", ClaimType = "alpha", ClaimValue = "y" });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var service = CreateService(dbContext, CreateMockUserManager());

        var result = await service.GetClaimsAsync();

        result.Select(item => item.ClaimType).Should().Equal("alpha", "zeta");
    }

    [Fact]
    public async Task GetForEditAsync_ClaimTypeMissing_ReturnsNull()
    {
        using var sqliteDb = new SqliteApplicationDb();
        await using var dbContext = sqliteDb.CreateContext();
        var service = CreateService(dbContext, CreateMockUserManager());

        var result = await service.GetForEditAsync("department");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForEditAsync_MapsUsersInClaim_AvailableUsers_OrderAndFiltering()
    {
        var user1 = CreateUser("u1", "zara", "zara@test.com");
        var user2 = CreateUser("u2", "adam", "adam@test.com");
        var user3 = CreateUser("u3", "bob", "bob@test.com");
        var user4 = CreateUser("u4", "charlie", "charlie@test.com");
        var user5 = CreateUser("u5", "hidden", "hidden@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.AddRange(user1, user2, user3, user4, user5);
            seedContext.UserClaims.AddRange(
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
                new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "sales" },
                new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "alpha" },
                new IdentityUserClaim<string> { UserId = "u5", ClaimType = "department", ClaimValue = "ignored-value" },
                new IdentityUserClaim<string> { UserId = "u3", ClaimType = "location", ClaimValue = "dublin" });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager([user1, user2, user3, user4]);
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.GetForEditAsync("department");

        result.Should().NotBeNull();
        result!.UsersInClaim.Select(item => (item.UserName, item.ClaimValue))
            .Should().Equal(
                ("adam", "alpha"),
                ("adam", "sales"),
                ("zara", "engineering"));
        result.AvailableUsers.Select(item => item.UserName).Should().Equal("bob", "charlie");
    }

    [Fact]
    public async Task GetForEditAsync_SingleUniqueAssignment_SetsIsLastUserAssignmentTrue()
    {
        var user = CreateUser("u1", "alice", "alice@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.Add(user);
            seedContext.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = "u1",
                ClaimType = "department",
                ClaimValue = "engineering"
            });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var service = CreateService(dbContext, CreateMockUserManager([user]));

        var result = await service.GetForEditAsync("department");

        result.Should().NotBeNull();
        result!.UsersInClaim.Should().ContainSingle();
        result.UsersInClaim[0].IsLastUserAssignment.Should().BeTrue();
    }

    [Fact]
    public async Task GetForEditAsync_MultipleAssignmentsForSameUser_DoesNotSetLastUserAssignment()
    {
        var user = CreateUser("u1", "alice", "alice@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.Add(user);
            seedContext.UserClaims.AddRange(
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "sales" });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var service = CreateService(dbContext, CreateMockUserManager([user]));

        var result = await service.GetForEditAsync("department");

        result.Should().NotBeNull();
        result!.UsersInClaim.Should().HaveCount(2);
        result.UsersInClaim.Should().OnlyContain(item => item.IsLastUserAssignment == false);
    }

    [Fact]
    public async Task GetForEditAsync_BooleanAssignments_DefaultsNewClaimValueToTrue_WhenInputNullOrWhitespace()
    {
        var user1 = CreateUser("u1", "alice", "alice@test.com");
        var user2 = CreateUser("u2", "bob", "bob@test.com");
        var user3 = CreateUser("u3", "charlie", "charlie@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.AddRange(user1, user2, user3);
            seedContext.UserClaims.AddRange(
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "feature-enabled", ClaimValue = "true" },
                new IdentityUserClaim<string> { UserId = "u2", ClaimType = "feature-enabled", ClaimValue = "false" });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var service = CreateService(dbContext, CreateMockUserManager([user1, user2, user3]));

        var resultWithNull = await service.GetForEditAsync("feature-enabled", null);
        var resultWithWhitespace = await service.GetForEditAsync("feature-enabled", "   ");

        resultWithNull.Should().NotBeNull();
        resultWithWhitespace.Should().NotBeNull();
        resultWithNull!.NewClaimValue.Should().Be("true");
        resultWithWhitespace!.NewClaimValue.Should().Be("true");
    }

    [Fact]
    public async Task GetForEditAsync_NonBooleanAssignments_DoesNotDefaultNewClaimValue()
    {
        var user1 = CreateUser("u1", "alice", "alice@test.com");
        var user2 = CreateUser("u2", "bob", "bob@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.AddRange(user1, user2);
            seedContext.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = "u1",
                ClaimType = "department",
                ClaimValue = "engineering"
            });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var service = CreateService(dbContext, CreateMockUserManager([user1, user2]));

        var result = await service.GetForEditAsync("department", null);

        result.Should().NotBeNull();
        result!.NewClaimValue.Should().BeNull();
    }

    [Fact]
    public async Task GetForEditAsync_NonWhitespaceInput_PreservesInputNewClaimValue()
    {
        var user1 = CreateUser("u1", "alice", "alice@test.com");
        var user2 = CreateUser("u2", "bob", "bob@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.AddRange(user1, user2);
            seedContext.UserClaims.AddRange(
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "feature-enabled", ClaimValue = "true" },
                new IdentityUserClaim<string> { UserId = "u2", ClaimType = "feature-enabled", ClaimValue = "false" });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var service = CreateService(dbContext, CreateMockUserManager([user1, user2]));

        var result = await service.GetForEditAsync("feature-enabled", "  custom-value  ");

        result.Should().NotBeNull();
        result!.NewClaimValue.Should().Be("  custom-value  ");
    }

    [Fact]
    public async Task AddUserToClaimAsync_UserNotFound_ReturnsUserNotFound()
    {
        using var sqliteDb = new SqliteApplicationDb();
        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.AddUserToClaimAsync("department", "u1", "engineering");

        result.Status.Should().Be(AddClaimAssignmentStatus.UserNotFound);
    }

    [Fact]
    public async Task AddUserToClaimAsync_AlreadyAssignedByType_ReturnsAlreadyAssigned()
    {
        var user = CreateUser("u1", "alice", "alice@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.Add(user);
            seedContext.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = "u1",
                ClaimType = "department",
                ClaimValue = "engineering"
            });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager([user]);
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync(user);
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.AddUserToClaimAsync("department", "u1", "different-value");

        result.Status.Should().Be(AddClaimAssignmentStatus.AlreadyAssigned);
        mockUserManager.Verify(
            manager => manager.AddClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()),
            Times.Never);
    }

    [Fact]
    public async Task AddUserToClaimAsync_IdentityFailure_ReturnsIdentityFailureWithErrorDescriptions()
    {
        var user = CreateUser("u1", "alice", "alice@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager([user]);
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync(user);
        mockUserManager.Setup(manager => manager.AddClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "error-1" },
                new IdentityError { Description = "error-2" }));
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.AddUserToClaimAsync("department", "u1", "engineering");

        result.Status.Should().Be(AddClaimAssignmentStatus.IdentityFailure);
        result.Errors.Should().Equal("error-1", "error-2");
    }

    [Fact]
    public async Task AddUserToClaimAsync_Success_AddsClaimWithExactTypeAndValue_AndReturnsUserName()
    {
        var user = CreateUser("u1", "alice", "alice@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager([user]);
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync(user);
        mockUserManager.Setup(manager => manager.AddClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.AddUserToClaimAsync("department", "u1", "engineering");

        result.Status.Should().Be(AddClaimAssignmentStatus.Success);
        result.UserName.Should().Be("alice");
        mockUserManager.Verify(
            manager => manager.AddClaimAsync(
                user,
                It.Is<Claim>(claim => claim.Type == "department" && claim.Value == "engineering")),
            Times.Once);
    }

    [Fact]
    public async Task RemoveUserFromClaimAsync_UserNotFound_ReturnsUserNotFound()
    {
        using var sqliteDb = new SqliteApplicationDb();
        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync((ApplicationUser?)null);
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.RemoveUserFromClaimAsync("department", "u1", "engineering");

        result.Status.Should().Be(RemoveClaimAssignmentStatus.UserNotFound);
    }

    [Fact]
    public async Task RemoveUserFromClaimAsync_AssignmentMissing_ReturnsAssignmentNotFound()
    {
        var user = CreateUser("u1", "alice", "alice@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.Add(user);
            seedContext.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = "u1",
                ClaimType = "department",
                ClaimValue = "sales"
            });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager([user]);
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync(user);
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.RemoveUserFromClaimAsync("department", "u1", "engineering");

        result.Status.Should().Be(RemoveClaimAssignmentStatus.AssignmentNotFound);
        mockUserManager.Verify(
            manager => manager.RemoveClaimAsync(It.IsAny<ApplicationUser>(), It.IsAny<Claim>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveUserFromClaimAsync_IdentityFailure_ReturnsIdentityFailureWithErrorDescriptions()
    {
        var user = CreateUser("u1", "alice", "alice@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.Add(user);
            seedContext.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = "u1",
                ClaimType = "department",
                ClaimValue = null
            });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager([user]);
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync(user);
        mockUserManager.Setup(manager => manager.RemoveClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "error-1" },
                new IdentityError { Description = "error-2" }));
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.RemoveUserFromClaimAsync("department", "u1", string.Empty);

        result.Status.Should().Be(RemoveClaimAssignmentStatus.IdentityFailure);
        result.Errors.Should().Equal("error-1", "error-2");
    }

    [Fact]
    public async Task RemoveUserFromClaimAsync_Success_WithRemainingAssignments_ReturnsHasRemainingAssignmentsTrue()
    {
        var user1 = CreateUser("u1", "alice", "alice@test.com");
        var user2 = CreateUser("u2", "bob", "bob@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.AddRange(user1, user2);
            seedContext.UserClaims.AddRange(
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
                new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "sales" });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager([user1, user2]);
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync(user1);
        mockUserManager.Setup(manager => manager.RemoveClaimAsync(user1, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, Claim>((_, claim) =>
            {
                var claimRow = dbContext.UserClaims.FirstOrDefault(c =>
                    c.UserId == "u1" &&
                    c.ClaimType == claim.Type &&
                    (c.ClaimValue ?? string.Empty) == claim.Value);

                if (claimRow is not null)
                {
                    dbContext.UserClaims.Remove(claimRow);
                    dbContext.SaveChanges();
                }
            });
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.RemoveUserFromClaimAsync("department", "u1", "engineering");

        result.Status.Should().Be(RemoveClaimAssignmentStatus.Success);
        result.UserName.Should().Be("alice");
        result.HasRemainingAssignments.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromClaimAsync_Success_LastAssignmentRemoved_ReturnsHasRemainingAssignmentsFalse()
    {
        var user = CreateUser("u1", "alice", "alice@test.com");

        using var sqliteDb = new SqliteApplicationDb();
        await using (var seedContext = sqliteDb.CreateContext())
        {
            seedContext.Users.Add(user);
            seedContext.UserClaims.Add(new IdentityUserClaim<string>
            {
                UserId = "u1",
                ClaimType = "department",
                ClaimValue = "engineering"
            });
            await seedContext.SaveChangesAsync();
        }

        await using var dbContext = sqliteDb.CreateContext();
        var mockUserManager = CreateMockUserManager([user]);
        mockUserManager.Setup(manager => manager.FindByIdAsync("u1"))
            .ReturnsAsync(user);
        mockUserManager.Setup(manager => manager.RemoveClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, Claim>((_, claim) =>
            {
                var claimRow = dbContext.UserClaims.FirstOrDefault(c =>
                    c.UserId == "u1" &&
                    c.ClaimType == claim.Type &&
                    (c.ClaimValue ?? string.Empty) == claim.Value);

                if (claimRow is not null)
                {
                    dbContext.UserClaims.Remove(claimRow);
                    dbContext.SaveChanges();
                }
            });
        var service = CreateService(dbContext, mockUserManager);

        var result = await service.RemoveUserFromClaimAsync("department", "u1", "engineering");

        result.Status.Should().Be(RemoveClaimAssignmentStatus.Success);
        result.UserName.Should().Be("alice");
        result.HasRemainingAssignments.Should().BeFalse();
    }
}
