using System.Text;
using System.Text.Encodings.Web;
using IdentityServerAspNetIdentity.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace IdentityServerAspNetIdentity.Tests;

public class TabNavTagHelperTests
{
    [Fact]
    public void TabNavTagHelper_Process_ShouldSetOuterStructure()
    {
        // Arrange
        var context = new TagHelperContext(
            new TagHelperAttributeList { { "selected-tab", "tnav2" } },
            new Dictionary<object, object>(),
            "unique-id");

        var output = new TagHelperOutput("tabnav",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        var tagHelper = new TabNavTagHelper();
        tagHelper.Init(context);


        // Act
        tagHelper.Process(context, output);

        // Assert
        // write output to a text string
        var writer = new StringWriter();
        output.WriteTo(writer, HtmlEncoder.Default);
        var outputString = writer.ToString();

        var expected =
            @"<div class=""nav nav-tabs flex-sm-column"" id=""nav-tab"" role=""tablist""><div class=""container""><nav><div class=""nav nav-tabs"" id=""nav-tab"" role=""tablist""></div></nav><div class=""tab-content"" id=""nav-tabContent""></div></div></div>";

        HtmlTestUtils.AssertHtmlAreEquivelant(expected, outputString);

    }


    [Fact]
    public void TabNavTagHelper_WithOnTabSelectedProperty_ShouldRenderJavaScript()
    {
        // Arrange
        var contextItems = new Dictionary<object, object>
        {
            { "SelectedTab", "tnav1" },
            { "TabContent", new StringBuilder() }
        };

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            contextItems,
            "unique-id");

        var tabNav = new TabNavTagHelper
        {
            SelectedTab = "tnav1",
            OnTabSelected = "myJavaScriptFunction"
        };

        var output = new TagHelperOutput("tabnav",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        // Act
        tabNav.Process(context, output);

        // Assert
        var postContent = output.PostContent.GetContent();
        var aa = HtmlTestUtils.NormalizeWhitespace(postContent);
        Assert.Contains("$('#nav-tab a[data-bs-toggle=\"tab\"]').on('shown.bs.tab', function (e) { myJavaScriptFunction(e); });", postContent);
    }

}