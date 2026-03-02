using System.Security.Claims;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ClaimsEditModelTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        mockUserManager.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        mockUserManager.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());
        return mockUserManager;
    }

    private static IdentityServerAspNetIdentity.Pages.Admin.Claims.EditModel CreatePageModel(
        ApplicationDbContext dbContext,
        Mock<UserManager<ApplicationUser>> mockUserManager)
    {
        return new IdentityServerAspNetIdentity.Pages.Admin.Claims.EditModel(dbContext, mockUserManager.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task OnGetAsync_ValidClaimType_ReturnsPageResult()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "u1",
            ClaimType = "department",
            ClaimValue = "engineering"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" }
        }.AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";

        var result = await pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnGetAsync_ClaimAssignmentsExist_PopulatesUsersInClaim()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "sales" });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" }
        }.AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";

        await pageModel.OnGetAsync();

        pageModel.UsersInClaim.Should().HaveCount(2);
        pageModel.UsersInClaim.Select(u => u.UserName).Should().Contain(new[] { "alice", "bob" });
    }

    [Fact]
    public async Task OnGetAsync_ClaimAssignmentsExist_PopulatesAvailableUsers()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "u1",
            ClaimType = "department",
            ClaimValue = "engineering"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" }
        }.AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";

        await pageModel.OnGetAsync();

        pageModel.AvailableUsers.Should().HaveCount(1);
        pageModel.AvailableUsers[0].UserName.Should().Be("bob");
    }

    [Fact]
    public async Task OnGetAsync_AssignedUsersAreOrderedByUserName()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "sales" });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "zara", Email = "zara@test.com" },
            new() { Id = "u2", UserName = "adam", Email = "adam@test.com" }
        }.AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";

        await pageModel.OnGetAsync();

        pageModel.UsersInClaim.Select(u => u.UserName).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task OnGetAsync_MissingClaimType_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>().AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = string.Empty;

        var result = await pageModel.OnGetAsync();

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task OnGetAsync_ClaimTypeNotFound_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>().AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "unknown-claim";

        var result = await pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_SingleAssignedUser_FlagsLastUserWarning()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "u1",
            ClaimType = "department",
            ClaimValue = "engineering"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" }
        }.AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";

        await pageModel.OnGetAsync();

        pageModel.UsersInClaim.Should().ContainSingle();
        pageModel.UsersInClaim.Single().IsLastUserAssignment.Should().BeTrue();
    }

    [Fact]
    public async Task OnGetAsync_BooleanClaimValues_DefaultsNewClaimValueToTrue()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "feature-enabled", ClaimValue = "true" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "feature-enabled", ClaimValue = "false" });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" },
            new() { Id = "u3", UserName = "charlie", Email = "charlie@test.com" }
        }.AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "feature-enabled";

        await pageModel.OnGetAsync();

        pageModel.NewClaimValue.Should().Be("true");
    }

    [Fact]
    public async Task OnGetAsync_NonBooleanClaimValues_DoesNotDefaultNewClaimValue()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "u1",
            ClaimType = "department",
            ClaimValue = "engineering"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" }
        }.AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";

        await pageModel.OnGetAsync();

        pageModel.NewClaimValue.Should().BeNull();
    }

    [Fact]
    public async Task OnPostAddUserAsync_ValidInput_AddsClaimAndRedirects()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "seed",
            ClaimType = "department",
            ClaimValue = "seed-value"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        var user = new ApplicationUser { Id = "u1", UserName = "alice", Email = "alice@test.com" };

        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser> { user }.AsQueryable());
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.AddClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success);

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";
        pageModel.SelectedUserId = "u1";
        pageModel.NewClaimValue = "engineering";

        var result = await pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        mockUserManager.Verify(
            m => m.AddClaimAsync(user, It.Is<Claim>(c => c.Type == "department" && c.Value == "engineering")),
            Times.Once);
    }

    [Fact]
    public async Task OnPostAddUserAsync_NoUserSelected_ReturnsPageWithModelError()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "seed",
            ClaimType = "department",
            ClaimValue = "seed-value"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>().AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";
        pageModel.NewClaimValue = "engineering";

        var result = await pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.ModelState.Should().ContainKey(nameof(pageModel.SelectedUserId));
    }

    [Fact]
    public async Task OnPostAddUserAsync_MissingClaimValue_ReturnsPageWithModelError()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "seed",
            ClaimType = "department",
            ClaimValue = "seed-value"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>().AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";
        pageModel.SelectedUserId = "u1";
        pageModel.NewClaimValue = string.Empty;

        var result = await pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.ModelState.Should().ContainKey(nameof(pageModel.NewClaimValue));
    }

    [Fact]
    public async Task OnPostAddUserAsync_UserAlreadyAssigned_ReturnsPageWithModelError()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
            new IdentityUserClaim<string> { UserId = "seed", ClaimType = "department", ClaimValue = "seed-value" });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        var user = new ApplicationUser { Id = "u1", UserName = "alice", Email = "alice@test.com" };
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser> { user }.AsQueryable());
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";
        pageModel.SelectedUserId = "u1";
        pageModel.NewClaimValue = "new-value";

        var result = await pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.ModelState.Should().ContainKey(string.Empty);
    }

    [Fact]
    public async Task OnPostAddUserAsync_UserNotFound_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "seed",
            ClaimType = "department",
            ClaimValue = "seed-value"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>().AsQueryable());
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync((ApplicationUser?)null);

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";
        pageModel.SelectedUserId = "u1";
        pageModel.NewClaimValue = "engineering";

        var result = await pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ValidInput_RemovesClaimAndRedirects()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "sales" });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        var user = new ApplicationUser { Id = "u1", UserName = "alice", Email = "alice@test.com" };
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            user,
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" }
        }.AsQueryable());
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.RemoveClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, Claim>((_, claim) =>
            {
                var claimRow = dbContext.UserClaims.FirstOrDefault(c =>
                    c.UserId == "u1" &&
                    c.ClaimType == claim.Type &&
                    c.ClaimValue == claim.Value);

                if (claimRow is not null)
                {
                    dbContext.UserClaims.Remove(claimRow);
                    dbContext.SaveChanges();
                }
            });

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";
        pageModel.RemoveUserId = "u1";
        pageModel.RemoveClaimValue = "engineering";

        var result = await pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        mockUserManager.Verify(
            m => m.RemoveClaimAsync(user, It.Is<Claim>(c => c.Type == "department" && c.Value == "engineering")),
            Times.Once);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_LastUserRemoved_RedirectsToClaimsIndexWithWarning()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "u1",
            ClaimType = "department",
            ClaimValue = "engineering"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        var user = new ApplicationUser { Id = "u1", UserName = "alice", Email = "alice@test.com" };
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser> { user }.AsQueryable());
        mockUserManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        mockUserManager.Setup(m => m.RemoveClaimAsync(user, It.IsAny<Claim>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, Claim>((_, claim) =>
            {
                var claimRow = dbContext.UserClaims.FirstOrDefault(c =>
                    c.UserId == "u1" &&
                    c.ClaimType == claim.Type &&
                    c.ClaimValue == claim.Value);

                if (claimRow is not null)
                {
                    dbContext.UserClaims.Remove(claimRow);
                    dbContext.SaveChanges();
                }
            });

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";
        pageModel.RemoveUserId = "u1";
        pageModel.RemoveClaimValue = "engineering";

        var result = await pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/Claims/Index");
        pageModel.TempData["Warning"].Should().NotBeNull();
        pageModel.TempData["Warning"]!.ToString().Should().Contain("different value");
        pageModel.TempData["Warning"]!.ToString().Should().Contain("keep this claim");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_InvalidInput_ReturnsPageWithModelError()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "u1",
            ClaimType = "department",
            ClaimValue = "engineering"
        });
        await dbContext.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.Users).Returns(new List<ApplicationUser>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" }
        }.AsQueryable());

        var pageModel = CreatePageModel(dbContext, mockUserManager);
        pageModel.ClaimType = "department";
        pageModel.RemoveUserId = string.Empty;
        pageModel.RemoveClaimValue = "engineering";

        var result = await pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.ModelState.Should().ContainKey(string.Empty);
    }
}
