using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class RolesEditModelTests
{
    private readonly Mock<IRolesAdminService> _mockRolesAdminService;

    private readonly IdentityServerAspNetIdentity.Pages.Admin.Roles.EditModel _pageModel;

    public RolesEditModelTests()
    {
        _mockRolesAdminService = new Mock<IRolesAdminService>();
        _pageModel = new IdentityServerAspNetIdentity.Pages.Admin.Roles.EditModel(_mockRolesAdminService.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }



    private static RoleEditPageDataDto CreatePageData(
        string roleName = "Editor",
        IReadOnlyList<RoleUserDto>? usersInRole = null,
        IReadOnlyList<RoleUserDto>? availableUsers = null)
    {
        return new RoleEditPageDataDto
        {
            RoleName = roleName,
            UsersInRole = usersInRole ?? Array.Empty<RoleUserDto>(),
            AvailableUsers = availableUsers ?? Array.Empty<RoleUserDto>()
        };
    }

    // ── OnGetAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task OnGetAsync_RoleExists_ReturnsPage()
    {
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        _pageModel.RoleId = "role-1";

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnGetAsync_RoleExists_PopulatesRoleName()
    {
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(roleName: "Editor"));

        _pageModel.RoleId = "role-1";

        await _pageModel.OnGetAsync();

        _pageModel.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task OnGetAsync_RoleHasUsers_PopulatesUsersInRole()
    {
        var usersInRole = new List<RoleUserDto>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" }
        };
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(usersInRole: usersInRole));

        _pageModel.RoleId = "role-1";

        await _pageModel.OnGetAsync();

        _pageModel.UsersInRole.Should().HaveCount(2);
        _pageModel.UsersInRole.Select(u => u.UserName).Should().Contain(new[] { "alice", "bob" });
    }

    [Fact]
    public async Task OnGetAsync_UsersNotInRole_PopulatesAvailableUsers()
    {
        var availableUsers = new List<RoleUserDto>
        {
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" },
            new() { Id = "u3", UserName = "charlie", Email = "charlie@test.com" }
        };
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(availableUsers: availableUsers));

        _pageModel.RoleId = "role-1";

        await _pageModel.OnGetAsync();

        _pageModel.AvailableUsers.Should().HaveCount(2);
        _pageModel.AvailableUsers.Select(u => u.UserName).Should().Contain(new[] { "bob", "charlie" });
    }

    [Fact]
    public async Task OnGetAsync_RoleNotFound_ReturnsNotFound()
    {
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleEditPageDataDto?)null);

        _pageModel.RoleId = "nonexistent";

        var result = await _pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_NoUsersInRole_UsersInRoleIsEmpty()
    {
        var availableUsers = new List<RoleUserDto>
        {
            new() { Id = "u1", UserName = "alice", Email = "a@t.com" }
        };
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(roleName: "EmptyRole", availableUsers: availableUsers));

        _pageModel.RoleId = "role-2";

        await _pageModel.OnGetAsync();

        _pageModel.UsersInRole.Should().BeEmpty();
        _pageModel.AvailableUsers.Should().HaveCount(1);
    }

    // ── OnPostAddUserAsync ────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAddUserAsync_Success_RedirectsWithTempData()
    {
        _mockRolesAdminService.Setup(s => s.AddUserToRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Success,
                UserName = "alice",
                RoleName = "Editor"
            });

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = "u1";

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().ContainKey("roleId");
        redirect.RouteValues!["roleId"].Should().Be("role-1");
        _pageModel.TempData["Success"].Should().Be("User 'alice' added to role 'Editor'");
    }

    [Fact]
    public async Task OnPostAddUserAsync_RoleNotFound_ReturnsNotFound()
    {
        _mockRolesAdminService.Setup(s => s.AddUserToRoleAsync("bad", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult { Status = AddUserToRoleStatus.RoleNotFound });

        _pageModel.RoleId = "bad";
        _pageModel.SelectedUserId = "u1";

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_UserNotFound_ReturnsNotFound()
    {
        _mockRolesAdminService.Setup(s => s.AddUserToRoleAsync("role-1", "bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult { Status = AddUserToRoleStatus.UserNotFound });

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = "bad";

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_NoUserSelected_ReturnsPage()
    {
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = null;

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_NoUserSelected_AddsModelStateError()
    {
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = null;

        await _pageModel.OnPostAddUserAsync();

        _pageModel.ModelState[nameof(_pageModel.SelectedUserId)]!.Errors
            .Should().ContainSingle(e => e.ErrorMessage == "Please select a user");
    }

    [Fact]
    public async Task OnPostAddUserAsync_IdentityFails_ReturnsPageWithErrors()
    {
        _mockRolesAdminService.Setup(s => s.AddUserToRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Failed,
                Errors = new[] { "User already in role" }
            });
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = "u1";

        var result = await _pageModel.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState[string.Empty]!.Errors
            .Should().ContainSingle(e => e.ErrorMessage == "User already in role");
    }

    // ── OnPostRemoveUserAsync ─────────────────────────────────────────────

    [Fact]
    public async Task OnPostRemoveUserAsync_Success_RedirectsWithTempData()
    {
        _mockRolesAdminService.Setup(s => s.RemoveUserFromRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult
            {
                Status = RemoveUserFromRoleStatus.Success,
                UserName = "alice",
                RoleName = "Editor"
            });

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = "u1";

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        _pageModel.TempData["Success"].Should().Be("User 'alice' removed from role 'Editor'");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_RoleNotFound_ReturnsNotFound()
    {
        _mockRolesAdminService.Setup(s => s.RemoveUserFromRoleAsync("bad", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult { Status = RemoveUserFromRoleStatus.RoleNotFound });

        _pageModel.RoleId = "bad";
        _pageModel.SelectedUserId = "u1";

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_UserNotFound_ReturnsNotFound()
    {
        _mockRolesAdminService.Setup(s => s.RemoveUserFromRoleAsync("role-1", "bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult { Status = RemoveUserFromRoleStatus.UserNotFound });

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = "bad";

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_NoUserSelected_ReturnsPageWithModelError()
    {
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = null;

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState[nameof(_pageModel.SelectedUserId)]!.Errors
            .Should().ContainSingle(e => e.ErrorMessage == "Please select a user");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_IdentityFails_ReturnsPageWithErrors()
    {
        _mockRolesAdminService.Setup(s => s.RemoveUserFromRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult
            {
                Status = RemoveUserFromRoleStatus.Failed,
                Errors = new[] { "Cannot remove last admin" }
            });
        _mockRolesAdminService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        _pageModel.RoleId = "role-1";
        _pageModel.SelectedUserId = "u1";

        var result = await _pageModel.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState[string.Empty]!.Errors
            .Should().ContainSingle(e => e.ErrorMessage == "Cannot remove last admin");
    }
}
