using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using IdentityServerServices;
using IdentityServerServices.ViewModels;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class RolesEditModelTests
{
    private readonly Mock<IRolesAdminService> _mockService;

    public RolesEditModelTests()
    {
        _mockService = new Mock<IRolesAdminService>();
    }

    private IdentityServerAspNetIdentity.Pages.Admin.Roles.EditModel CreatePageModel()
    {
        return new IdentityServerAspNetIdentity.Pages.Admin.Roles.EditModel(_mockService.Object)
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
        _mockService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var model = CreatePageModel();
        model.RoleId = "role-1";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnGetAsync_RoleExists_PopulatesRoleName()
    {
        _mockService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(roleName: "Editor"));

        var model = CreatePageModel();
        model.RoleId = "role-1";

        await model.OnGetAsync();

        model.RoleName.Should().Be("Editor");
    }

    [Fact]
    public async Task OnGetAsync_RoleHasUsers_PopulatesUsersInRole()
    {
        var usersInRole = new List<RoleUserDto>
        {
            new() { Id = "u1", UserName = "alice", Email = "alice@test.com" },
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" }
        };
        _mockService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(usersInRole: usersInRole));

        var model = CreatePageModel();
        model.RoleId = "role-1";

        await model.OnGetAsync();

        model.UsersInRole.Should().HaveCount(2);
        model.UsersInRole.Select(u => u.UserName).Should().Contain("alice");
        model.UsersInRole.Select(u => u.UserName).Should().Contain("bob");
    }

    [Fact]
    public async Task OnGetAsync_UsersNotInRole_PopulatesAvailableUsers()
    {
        var availableUsers = new List<RoleUserDto>
        {
            new() { Id = "u2", UserName = "bob", Email = "bob@test.com" },
            new() { Id = "u3", UserName = "charlie", Email = "charlie@test.com" }
        };
        _mockService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(availableUsers: availableUsers));

        var model = CreatePageModel();
        model.RoleId = "role-1";

        await model.OnGetAsync();

        model.AvailableUsers.Should().HaveCount(2);
        model.AvailableUsers.Select(u => u.UserName).Should().Contain("bob");
        model.AvailableUsers.Select(u => u.UserName).Should().Contain("charlie");
    }

    [Fact]
    public async Task OnGetAsync_RoleNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetRoleForEditAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleEditPageDataDto?)null);

        var model = CreatePageModel();
        model.RoleId = "nonexistent";

        var result = await model.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_NoUsersInRole_UsersInRoleIsEmpty()
    {
        var availableUsers = new List<RoleUserDto>
        {
            new() { Id = "u1", UserName = "alice", Email = "a@t.com" }
        };
        _mockService.Setup(s => s.GetRoleForEditAsync("role-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData(roleName: "EmptyRole", availableUsers: availableUsers));

        var model = CreatePageModel();
        model.RoleId = "role-2";

        await model.OnGetAsync();

        model.UsersInRole.Should().BeEmpty();
        model.AvailableUsers.Should().HaveCount(1);
    }

    // ── OnPostAddUserAsync ────────────────────────────────────────────────

    [Fact]
    public async Task OnPostAddUserAsync_Success_RedirectsToSelf()
    {
        _mockService.Setup(s => s.AddUserToRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Success,
                UserName = "alice",
                RoleName = "Editor"
            });

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        var result = await model.OnPostAddUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.RouteValues.Should().ContainKey("roleId");
        redirect.RouteValues!["roleId"].Should().Be("role-1");
    }

    [Fact]
    public async Task OnPostAddUserAsync_Success_SetsTempDataSuccess()
    {
        _mockService.Setup(s => s.AddUserToRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Success,
                UserName = "alice",
                RoleName = "Editor"
            });

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        await model.OnPostAddUserAsync();

        model.TempData["Success"].Should().Be("User 'alice' added to role 'Editor'");
    }

    [Fact]
    public async Task OnPostAddUserAsync_RoleNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.AddUserToRoleAsync("bad", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult { Status = AddUserToRoleStatus.RoleNotFound });

        var model = CreatePageModel();
        model.RoleId = "bad";
        model.SelectedUserId = "u1";

        var result = await model.OnPostAddUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_UserNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.AddUserToRoleAsync("role-1", "bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult { Status = AddUserToRoleStatus.UserNotFound });

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = "bad";

        var result = await model.OnPostAddUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_NoUserSelected_ReturnsPage()
    {
        _mockService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = null;

        var result = await model.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_NoUserSelected_AddsModelStateError()
    {
        _mockService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = null;

        await model.OnPostAddUserAsync();

        model.ModelState[nameof(model.SelectedUserId)]!.Errors
            .Should().ContainSingle(e => e.ErrorMessage == "Please select a user");
    }

    [Fact]
    public async Task OnPostAddUserAsync_IdentityFails_ReturnsPageWithErrors()
    {
        _mockService.Setup(s => s.AddUserToRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Failed,
                Errors = new[] { "User already in role" }
            });
        _mockService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        var result = await model.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        model.ModelState[string.Empty]!.Errors
            .Should().ContainSingle(e => e.ErrorMessage == "User already in role");
    }

    // ── OnPostRemoveUserAsync ─────────────────────────────────────────────

    [Fact]
    public async Task OnPostRemoveUserAsync_Success_RedirectsToSelf()
    {
        _mockService.Setup(s => s.RemoveUserFromRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult
            {
                Status = RemoveUserFromRoleStatus.Success,
                UserName = "alice",
                RoleName = "Editor"
            });

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        var result = await model.OnPostRemoveUserAsync();

        result.Should().BeOfType<RedirectToPageResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_Success_SetsTempDataSuccess()
    {
        _mockService.Setup(s => s.RemoveUserFromRoleAsync("role-1", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult
            {
                Status = RemoveUserFromRoleStatus.Success,
                UserName = "alice",
                RoleName = "Editor"
            });

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = "u1";

        await model.OnPostRemoveUserAsync();

        model.TempData["Success"].Should().Be("User 'alice' removed from role 'Editor'");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_RoleNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.RemoveUserFromRoleAsync("bad", "u1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult { Status = RemoveUserFromRoleStatus.RoleNotFound });

        var model = CreatePageModel();
        model.RoleId = "bad";
        model.SelectedUserId = "u1";

        var result = await model.OnPostRemoveUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_UserNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.RemoveUserFromRoleAsync("role-1", "bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult { Status = RemoveUserFromRoleStatus.UserNotFound });

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = "bad";

        var result = await model.OnPostRemoveUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_NoUserSelected_ReturnsPage()
    {
        _mockService.Setup(s => s.GetRoleForEditAsync("role-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePageData());

        var model = CreatePageModel();
        model.RoleId = "role-1";
        model.SelectedUserId = null;

        var result = await model.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
    }
}
