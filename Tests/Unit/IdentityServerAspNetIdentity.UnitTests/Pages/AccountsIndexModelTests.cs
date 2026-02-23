using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.Pages.Admin.Accounts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MockQueryable;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class AccountsIndexModelTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManager;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Accounts.Index _pageModel;

    public AccountsIndexModelTests()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Accounts.Index(_userManager.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    // Phase 1 — Page loads
    [Fact]
    public async Task should_populate_users_when_get()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { UserName = "alice", Email = "alice@test.com" },
            new() { UserName = "bob", Email = "bob@test.com" },
        };
        _userManager.Setup(m => m.Users).Returns(users.AsQueryable().BuildMock());

        // Act
        await _pageModel.OnGetAsync();

        // Assert
        _pageModel.Users.Should().HaveCount(2);
        _pageModel.Users.Should().Contain(u => u.UserName == "alice");
        _pageModel.Users.Should().Contain(u => u.UserName == "bob");
    }

    // Phase 2 — Empty state
    [Fact]
    public async Task should_return_empty_list_when_no_users_exist()
    {
        // Arrange
        var users = new List<ApplicationUser>();
        _userManager.Setup(m => m.Users).Returns(users.AsQueryable().BuildMock());

        // Act
        await _pageModel.OnGetAsync();

        // Assert
        _pageModel.Users.Should().BeEmpty();
    }

    // Phase 2 — Data correctness
    [Fact]
    public async Task should_include_email_for_all_users_when_get()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { UserName = "user1", Email = "user1@test.com" },
            new() { UserName = "user2", Email = "user2@test.com" },
            new() { UserName = "user3", Email = "user3@test.com" },
        };
        _userManager.Setup(m => m.Users).Returns(users.AsQueryable().BuildMock());

        // Act
        await _pageModel.OnGetAsync();

        // Assert
        _pageModel.Users.Should().HaveCount(3);
        _pageModel.Users.Select(u => u.Email).Should().AllSatisfy(email =>
            email.Should().NotBeNullOrEmpty());
    }
}
