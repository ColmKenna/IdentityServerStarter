using FluentAssertions;
using IdentityServerAspNetIdentity.Pages.Admin.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Moq;
using IdentityServerAspNetIdentity.Models;
using Xunit;

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

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        // Arrange
        _pageModel.Input = new CreateModel.CreateUserInput();
        _pageModel.ModelState.AddModelError("Input.UserName", "Required");

        // Act
        var result = await _pageModel.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAsync_CreationSuccessWithPassword_RedirectsToEdit()
    {
        // Arrange
        _pageModel.Input = new CreateModel.CreateUserInput
        {
            UserName = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), "Password123!"))
            .Callback<ApplicationUser, string>((user, _) => user.Id = "user-123")
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _pageModel.OnPostAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Users/Edit");
        redirect.RouteValues!["userId"].Should().Be("user-123");
        _pageModel.TempData["Success"].Should().Be("User 'newuser' created successfully");
    }

    [Fact]
    public async Task OnPostAsync_CreationSuccessWithoutPassword_RedirectsToEdit()
    {
        // Arrange
        _pageModel.Input = new CreateModel.CreateUserInput
        {
            UserName = "newuser",
            Email = "newuser@example.com",
            Password = null
        };

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(user => user.Id = "user-123")
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _pageModel.OnPostAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/Users/Edit");
        redirect.RouteValues!["userId"].Should().Be("user-123");
        _pageModel.TempData["Success"].Should().Be("User 'newuser' created successfully");
    }

    [Fact]
    public async Task OnPostAsync_CreationFails_ReturnsPageWithErrors()
    {
        // Arrange
        _pageModel.Input = new CreateModel.CreateUserInput
        {
            UserName = "newuser",
            Email = "newuser@example.com"
        };

        _mockUserManager
            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate username" }));

        // Act
        var result = await _pageModel.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _pageModel.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }
}
