using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.Pages.Admin.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin;

public class UsersCreateModelTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly CreateModel _sut;

    public UsersCreateModelTests()
    {
        _mockUserManager = TestHelpers.CreateMockUserManager();
        _sut = new CreateModel(_mockUserManager.Object)
        {
            PageContext = new PageContext(),
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public void OnGet_ShouldReturnVoid_AndNotThrow()
    {
        // Act
        var act = () => _sut.OnGet();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPage_WhenModelStateIsInvalid()
    {
        // Arrange
        _sut.ModelState.AddModelError("Error", "Model is invalid");

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ShouldCreateUserWithPassword_WhenPasswordProvided()
    {
        // Arrange
        _sut.Input = new CreateModel.CreateUserInput
        {
            UserName = "alice",
            Email = "alice@example.com",
            Password = "SecurePassword1!"
        };
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "SecurePassword1!"))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((u, p) => u.Id = "new-user-id");

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/Users/Edit");
        redirectResult.RouteValues.Should().ContainKey("userId").WhoseValue.Should().Be("new-user-id");
        _sut.TempData["Success"].Should().NotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task OnPostAsync_ShouldCreateUserWithoutPassword_WhenPasswordEmpty(string? password)
    {
        // Arrange
        _sut.Input = new CreateModel.CreateUserInput
        {
            UserName = "bob",
            Email = "bob@example.com",
            Password = password
        };
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(u => u.Id = "new-user-id");

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<RedirectToPageResult>();
        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPageWithErrors_WhenCreateFails()
    {
        // Arrange
        _sut.Input = new CreateModel.CreateUserInput
        {
            UserName = "alice",
            Password = "SecurePassword1!"
        };
        var failureResult = IdentityResult.Failed(new IdentityError { Description = "Username taken" });
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "SecurePassword1!"))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.Values.SelectMany(v => v.Errors).Should().ContainSingle(e => e.ErrorMessage == "Username taken");
    }

    [Fact]
    public async Task OnPostAsync_ShouldSetUserProperties_FromInput()
    {
        // Arrange
        _sut.Input = new CreateModel.CreateUserInput
        {
            UserName = "charlie",
            Email = "charlie@example.com"
        };
        ApplicationUser? capturedUser = null;
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(u => capturedUser = u);

        // Act
        await _sut.OnPostAsync();

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.UserName.Should().Be("charlie");
        capturedUser.Email.Should().Be("charlie@example.com");
        capturedUser.EmailConfirmed.Should().BeFalse();
    }
}
