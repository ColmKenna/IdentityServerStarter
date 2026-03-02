using Microsoft.AspNetCore.Mvc;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ApiScopesCreateModelTests
{
    private static IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.CreateModel CreatePageModel()
        => new();

    [Fact]
    public void OnGet_RedirectsToEditWithZeroId()
    {
        var pageModel = CreatePageModel();

        var result = pageModel.OnGet();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(0);
    }

    [Fact]
    public void OnPost_RedirectsToEditWithZeroId()
    {
        var pageModel = CreatePageModel();

        var result = pageModel.OnPost();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(0);
    }
}
