using IdentityServerAspNetIdentity.Pages.Admin.Roles;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.Roles;

public class EditModelTests
{
    private readonly Mock<IRolesAdminService> _rolesAdminService = new();

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        var sut = CreateSut();
        sut.RoleId = "missing-role";

        _rolesAdminService.Setup(x => x.GetRoleForEditAsync("missing-role", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleEditPageDataDto?)null);

        var result = await sut.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulatePageDataAndReturnPage_WhenRoleExists()
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        var pageData = CreatePageData(
            roleName: "Administrators",
            usersInRole:
            [
                CreateUser("member-1", "alice"),
                CreateUser("member-2", "brad")
            ],
            availableUsers:
            [
                CreateUser("candidate-1", "charlie")
            ]);

        _rolesAdminService.Setup(x => x.GetRoleForEditAsync("role-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var result = await sut.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        sut.RoleName.Should().Be("Administrators");
        sut.UsersInRole.Should().BeEquivalentTo(pageData.UsersInRole);
        sut.AvailableUsers.Should().BeEquivalentTo(pageData.AvailableUsers);
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldRehydratePageDataAndReturnPage_WhenSelectedUserIdIsMissing()
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        sut.SelectedUserId = " ";
        var pageData = CreatePageData(
            roleName: "Administrators",
            usersInRole:
            [
                CreateUser("member-1", "alice")
            ],
            availableUsers:
            [
                CreateUser("candidate-1", "charlie")
            ]);

        _rolesAdminService.Setup(x => x.GetRoleForEditAsync("role-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var result = await sut.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Should().ContainKey(nameof(EditModel.SelectedUserId));
        sut.ModelState[nameof(EditModel.SelectedUserId)]!.Errors.Should().ContainSingle(
            x => x.ErrorMessage == "Please select a user");
        sut.RoleName.Should().Be("Administrators");
        sut.UsersInRole.Should().BeEquivalentTo(pageData.UsersInRole);
        sut.AvailableUsers.Should().BeEquivalentTo(pageData.AvailableUsers);
    }

    [Theory]
    [InlineData(AddUserToRoleStatus.RoleNotFound)]
    [InlineData(AddUserToRoleStatus.UserNotFound)]
    public async Task OnPostAddUserAsync_ShouldReturnNotFound_WhenRoleOrUserDoesNotExist(AddUserToRoleStatus status)
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        sut.SelectedUserId = "user-id";

        _rolesAdminService.Setup(x => x.AddUserToRoleAsync("role-id", "user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult { Status = status });

        var result = await sut.OnPostAddUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldAddErrorsRehydratePageDataAndReturnPage_WhenServiceFails()
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        sut.SelectedUserId = "user-id";
        var pageData = CreatePageData(
            roleName: "Administrators",
            usersInRole:
            [
                CreateUser("member-1", "alice")
            ],
            availableUsers:
            [
                CreateUser("candidate-1", "charlie")
            ]);

        _rolesAdminService.Setup(x => x.AddUserToRoleAsync("role-id", "user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Failed,
                Errors = ["User is already assigned.", "Retry later."]
            });
        _rolesAdminService.Setup(x => x.GetRoleForEditAsync("role-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var result = await sut.OnPostAddUserAsync();

        result.Should().BeOfType<PageResult>();
        sut.RoleName.Should().Be("Administrators");
        sut.UsersInRole.Should().BeEquivalentTo(pageData.UsersInRole);
        sut.AvailableUsers.Should().BeEquivalentTo(pageData.AvailableUsers);
        sut.ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .Should()
            .Contain(["User is already assigned.", "Retry later."]);
    }

    [Fact]
    public async Task OnPostAddUserAsync_ShouldSetSuccessMessageAndRedirect_WhenServiceSucceeds()
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        sut.SelectedUserId = "user-id";

        _rolesAdminService.Setup(x => x.AddUserToRoleAsync("role-id", "user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AddUserToRoleResult
            {
                Status = AddUserToRoleStatus.Success,
                RoleName = "Administrators",
                UserName = "alice"
            });

        var result = await sut.OnPostAddUserAsync();

        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.RouteValues.Should().ContainKey("roleId").WhoseValue.Should().Be("role-id");
        sut.TempData["Success"].Should().Be("User 'alice' added to role 'Administrators'");
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldRehydratePageDataAndReturnPage_WhenSelectedUserIdIsMissing()
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        sut.SelectedUserId = null;
        var pageData = CreatePageData(
            roleName: "Administrators",
            usersInRole:
            [
                CreateUser("member-1", "alice")
            ],
            availableUsers:
            [
                CreateUser("candidate-1", "charlie")
            ]);

        _rolesAdminService.Setup(x => x.GetRoleForEditAsync("role-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var result = await sut.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
        sut.ModelState.Should().ContainKey(nameof(EditModel.SelectedUserId));
        sut.ModelState[nameof(EditModel.SelectedUserId)]!.Errors.Should().ContainSingle(
            x => x.ErrorMessage == "Please select a user");
        sut.RoleName.Should().Be("Administrators");
        sut.UsersInRole.Should().BeEquivalentTo(pageData.UsersInRole);
        sut.AvailableUsers.Should().BeEquivalentTo(pageData.AvailableUsers);
    }

    [Theory]
    [InlineData(RemoveUserFromRoleStatus.RoleNotFound)]
    [InlineData(RemoveUserFromRoleStatus.UserNotFound)]
    public async Task OnPostRemoveUserAsync_ShouldReturnNotFound_WhenRoleOrUserDoesNotExist(RemoveUserFromRoleStatus status)
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        sut.SelectedUserId = "user-id";

        _rolesAdminService.Setup(x => x.RemoveUserFromRoleAsync("role-id", "user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult { Status = status });

        var result = await sut.OnPostRemoveUserAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldAddErrorsRehydratePageDataAndReturnPage_WhenServiceFails()
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        sut.SelectedUserId = "user-id";
        var pageData = CreatePageData(
            roleName: "Administrators",
            usersInRole:
            [
                CreateUser("member-1", "alice")
            ],
            availableUsers:
            [
                CreateUser("candidate-1", "charlie")
            ]);

        _rolesAdminService.Setup(x => x.RemoveUserFromRoleAsync("role-id", "user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult
            {
                Status = RemoveUserFromRoleStatus.Failed,
                Errors = ["User is not in this role.", "Retry later."]
            });
        _rolesAdminService.Setup(x => x.GetRoleForEditAsync("role-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageData);

        var result = await sut.OnPostRemoveUserAsync();

        result.Should().BeOfType<PageResult>();
        sut.RoleName.Should().Be("Administrators");
        sut.UsersInRole.Should().BeEquivalentTo(pageData.UsersInRole);
        sut.AvailableUsers.Should().BeEquivalentTo(pageData.AvailableUsers);
        sut.ModelState.Values
            .SelectMany(x => x.Errors)
            .Select(x => x.ErrorMessage)
            .Should()
            .Contain(["User is not in this role.", "Retry later."]);
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldSetSuccessMessageAndRedirect_WhenServiceSucceeds()
    {
        var sut = CreateSut();
        sut.RoleId = "role-id";
        sut.SelectedUserId = "user-id";

        _rolesAdminService.Setup(x => x.RemoveUserFromRoleAsync("role-id", "user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RemoveUserFromRoleResult
            {
                Status = RemoveUserFromRoleStatus.Success,
                RoleName = "Administrators",
                UserName = "alice"
            });

        var result = await sut.OnPostRemoveUserAsync();

        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.RouteValues.Should().ContainKey("roleId").WhoseValue.Should().Be("role-id");
        sut.TempData["Success"].Should().Be("User 'alice' removed from role 'Administrators'");
    }

    private EditModel CreateSut()
    {
        var httpContext = new DefaultHttpContext();
        var modelState = new ModelStateDictionary();
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor(),
            modelState);
        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), modelState);

        return new EditModel(_rolesAdminService.Object)
        {
            PageContext = new PageContext(actionContext) { ViewData = viewData },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    private static RoleEditPageDataDto CreatePageData(
        string roleName,
        IReadOnlyList<RoleUserDto>? usersInRole = null,
        IReadOnlyList<RoleUserDto>? availableUsers = null)
    {
        return new RoleEditPageDataDto
        {
            RoleName = roleName,
            UsersInRole = usersInRole ?? [],
            AvailableUsers = availableUsers ?? []
        };
    }

    private static RoleUserDto CreateUser(string id, string userName)
    {
        return new RoleUserDto
        {
            Id = id,
            UserName = userName,
            Email = $"{userName}@test.local"
        };
    }
}
