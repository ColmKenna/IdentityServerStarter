using IdentityServerAspNetIdentity.Pages.Admin.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using IdentityServerAspNetIdentity.Models;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class UsersCreateModelTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly CreateModel _pageModel;

    public UsersCreateModelTests()
    {
        _mockUserManager = TestHelpers.CreateMockUserManager();
        _pageModel = new CreateModel(_mockUserManager.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    private static CreateModel.CreateUserInput CreateInput(
        string userName = "newuser",
        string email = "newuser@example.com",
        string? password = null)
    {
        return new CreateModel.CreateUserInput
        {
            UserName = userName,
            Email = email,
            Password = password
        };
    }

    private void AssertRedirectToEdit(IActionResult result, string expectedUserId, string expectedMessage)
    {
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Users/Edit");
        redirect.RouteValues!["userId"].Should().Be(expectedUserId);
        _pageModel.TempData["Success"].Should().Be(expectedMessage);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        _pageModel.Input = new CreateModel.CreateUserInput();
        _pageModel.ModelState.AddModelError("Input.UserName", "Required");

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAsync_CreationSuccessWithPassword_RedirectsToEdit()
    {
        _pageModel.Input = CreateInput(password: "Password123!");
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password123!"))
            .Callback<ApplicationUser, string>((user, _) => user.Id = "user-123")
            .ReturnsAsync(IdentityResult.Success);

        var result = await _pageModel.OnPostAsync();

        AssertRedirectToEdit(result, "user-123", "User 'newuser' created successfully");
    }

    [Fact]
    public async Task OnPostAsync_CreationSuccessWithoutPassword_RedirectsToEdit()
    {
        _pageModel.Input = CreateInput();
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(user => user.Id = "user-123")
            .ReturnsAsync(IdentityResult.Success);

        var result = await _pageModel.OnPostAsync();

        AssertRedirectToEdit(result, "user-123", "User 'newuser' created successfully");
    }

    [Fact]
    public async Task OnPostAsync_CreationFails_ReturnsPageWithErrors()
    {
        _pageModel.Input = CreateInput();
        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate username" }));

        var result = await _pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }
}
