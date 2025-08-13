using System.Text;
using System.Text.Encodings.Web;
using IdentityServerAspNetIdentity.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using PrimativeExtensionMethods;
using AngleSharp.Html.Parser;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using AngleSharp.Dom;
using AngleSharp.Browser;

namespace IdentityServerAspNetIdentity.Tests;

public class TabTagHelperTests
{


    [Fact]
    public void TabTagHelper_ShouldRenderNavButton()
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

        var tab = new TabTagHelper
        {
            Id = "tnav1",
            Name = "tab1",
            Active = true
        };

        var output = new TagHelperOutput("tab",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        // Act
        tab.Process(context, output);

        // write output to a text string
        var writer = new StringWriter();
        output.WriteTo(writer, HtmlEncoder.Default);
        var outputString = writer.ToString();


        //Assert
        var expected =
            @"<button class=""nav-link active"" id=""tnav1_button"" data-bs-toggle=""tab"" data-bs-target=""#tnav1"" type=""button"" role=""tab"" aria-controls=""nav-home"" aria-selected=""true"">tab1</button>";
        HtmlTestUtils.AssertHtmlAreEquivelant(expected, outputString);

    }


    [Fact]
    public void MultipleTabTagHelpers_ShouldCombineContentInTabContent()
    {
        // Arrange
        var contextItems = new Dictionary<object, object>
        {
            { "SelectedTab", "tnav2" },
            { "TabContent", new StringBuilder() }
        };

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            contextItems,
            "unique-id");

        var tabs = CreateTestTagHelpers();


        // Act
        for (int i = 0; i < tabs.Count(); i++)
        {
            tabs[i].TagHelper.Process(context, tabs[i].Output);
        }

        // Assert
        var tabContent = context.Items.GetValueOrDefault("TabContent", new StringBuilder());

        var tabContentString = tabContent.ToString();
        var expectedStromg =
            @"<div class=""tab-pane fade"" id=""tnav1"" name=""tab1"" role=""tabpanel"" aria-labelledby=""tnav1-tab""><div>a</div></div>
                                              <div class=""tab-pane fade show active"" id=""tnav2"" name=""tab3"" role=""tabpanel"" aria-labelledby=""tnav2-tab""><div>b</div></div>
                                              <div class=""tab-pane fade"" id=""tnav3"" name=""tab4"" role=""tabpanel"" aria-labelledby=""tnav3-tab""><div>c</div></div>";

        HtmlTestUtils.AssertHtmlAreEquivelant(expectedStromg, tabContentString);
    }
    [Theory]
    [InlineData("tnav1")]
    [InlineData("tnav2")]
    [InlineData("tnav3")]
    public void SelectedTabSetsShowAndActiveClassOnTheCorrectTab(string selectedTab)
    {
        // Arrange
        var contextItems = new Dictionary<object, object>
        {
            { "SelectedTab", selectedTab },
            { "TabContent", new StringBuilder() }
        };

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            contextItems,
            "unique-id");

        var tabs = CreateTestTagHelpers();

        // Act
        for (int i = 0; i < tabs.Count(); i++)
        {
            tabs[i].TagHelper.Process(context, tabs[i].Output);
        }

        // Assert
        var tabContent = context.Items.GetValueOrDefault("TabContent", new StringBuilder());
        var parser = new HtmlParser();
        var actual = parser.ParseDocument(tabContent.ToString());

        foreach (var tab in new[] { "tnav1", "tnav2", "tnav3" })
        {
            var element = actual.GetElementById(tab);
            if (tab == selectedTab)
            {
                Assert.True(element.ClassList.Contains("show"));
                Assert.True(element.ClassList.Contains("active"));
            }
            else
            {
                Assert.False(element.ClassList.Contains("show"));
                Assert.False(element.ClassList.Contains("active"));
            }
        }
    }
    private static IList<(TabTagHelper TagHelper, TagHelperOutput Output)> CreateTestTagHelpers()
    {
        var tabs = new[]
        {
            new { TagHelper = new TabTagHelper { Id = "tnav1", Name = "tab1", Active = true }, Contents = "<div>a</div>" },
            new { TagHelper = new TabTagHelper { Id = "tnav2", Name = "tab3", Active = true }, Contents = "<div>b</div>" },
            new { TagHelper = new TabTagHelper { Id = "tnav3", Name = "tab4", Active = true }, Contents = "<div>c</div>" }
        };

        var tagHelperOutputs = tabs.Select(x =>
        (
            TagHelper: x.TagHelper,
            Output: TagHelpersUtils.Generate("tab",
                new Dictionary<string, object>
                {
                    { "id", x.TagHelper.Id },
                    { "name", x.TagHelper.Name },
                    { "active", x.TagHelper.Active }
                },
                x.Contents)
        )).ToList();

        return tagHelperOutputs;
    }


}