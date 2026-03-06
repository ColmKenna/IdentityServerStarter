using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class UsersIndexModelTests
{
    private readonly Mock<IUserEditor> _mockUserEditor;
    private readonly IdentityServerAspNetIdentity.Pages.Admin.Users.Index _pageModel;

    public UsersIndexModelTests()
    {
        _mockUserEditor = new Mock<IUserEditor>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Users.Index(_mockUserEditor.Object);
    }

    [Fact]
    public async Task OnGetAsync_InitializesUsersCollection()
    {
        _mockUserEditor
            .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _pageModel.OnGetAsync();

        _pageModel.Users.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ServiceReturnsUsers_PopulatesUsers()
    {
        _mockUserEditor
            .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new()
                {
                    Id = "u1",
                    UserName = "alice",
                    Email = "alice@test.com",
                    EmailConfirmed = true,
                    TwoFactorEnabled = false
                },
                new()
                {
                    Id = "u2",
                    UserName = "bob",
                    Email = "bob@test.com",
                    EmailConfirmed = false,
                    TwoFactorEnabled = true
                }
            ]);

        await _pageModel.OnGetAsync();

        _pageModel.Users.Should().HaveCount(2);
        _pageModel.Users.Select(u => u.UserName).Should().Contain(["alice", "bob"]);
    }

    [Fact]
    public async Task OnGetAsync_NoUsers_ReturnsEmptyList()
    {
        _mockUserEditor
            .Setup(s => s.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _pageModel.OnGetAsync();

        _pageModel.Users.Should().BeEmpty();
    }

}
