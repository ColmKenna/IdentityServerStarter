using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;

namespace IdentityServerAspNetIdentity.Tests;

public static class HtmlTestUtils
{
    public static void AssertHtmlAreEquivelant(string expectedHtmlString, string actualHtmlString)
    {
        var parser = new HtmlParser();
        var expectedDocument = parser.ParseDocument(expectedHtmlString);
        var actualDocument = parser.ParseDocument(actualHtmlString.ToString());
        // test that it contains the expected number of tabs
        Assert.Equal(expectedDocument.Body.ChildElementCount, actualDocument.Body.ChildElementCount);
        // test that the content of each tab is correct
        for (int i = 0; i < expectedDocument.Body.ChildElementCount; i++)
        {
            Assert.Equal(NormalizeWhitespace(expectedDocument.Body.Children[i].InnerHtml),
                NormalizeWhitespace(actualDocument.Body.Children[i].InnerHtml));
            // Check attributes
            foreach (var expectedAttribute in expectedDocument.Body.Children[i].Attributes.Where(x => x.Name != "class"))
            {
                var actualAttribute = actualDocument.Body.Children[i].GetAttribute(expectedAttribute.Name);
                Assert.NotNull(actualAttribute); // Ensure attribute exists
                Assert.Equal(expectedAttribute.Value.Trim(), actualAttribute.Trim()); // Ensure attribute values match
            }

            // Check classlist var expectedClassList = expectedDocument.Body.Children[i].ClassList;
            var actualClassList = actualDocument.Body.Children[i].ClassList;
            var expectedClassList = expectedDocument.Body.Children[i].ClassList;
            Assert.Equal(expectedClassList.Count(), actualClassList.Count());
            foreach (var expectedClass in expectedClassList)
            {
                Assert.Contains(expectedClass, actualClassList);
            }
        }
    }

    public static string NormalizeWhitespace(string input)
    {
        return string.Join(" ", input.Split(new[] { ' ', '\t', '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries));
    }


}

//public class TabNavTagHelperTests
//{

//    private Func<bool, HtmlEncoder, Task<TagHelperContent>> GetChildContent(string childContent)
//    {
//        var content = new DefaultTagHelperContent();
//        var tagHelperContent = content.SetContent(childContent);
//        return (b, encoder) => Task.FromResult(tagHelperContent);
//    }

//    [Fact]
//    public async Task TabNavTagHelper_RendersCorrectHtml()
//    {
//        // Arrange
//        var context = new TagHelperContext(
//            allAttributes: new TagHelperAttributeList(),
//            items: new Dictionary<object, object>(),
//            uniqueId: "test"
//        );

//        var taghelper = new TagHelperOutput("tabnav", new TagHelperAttributeList(), GetChildContent(@"
//                <tab id=""tnav1"" name=""tab1"" active><div>a</div></tab>
//                <tab id=""tnav2"" name=""tab3"" active><div>a</div></tab>
//                <tab id=""tnav3"" name=""tab4"" active><div>a</div></tab>
//            "));

//        var tagHelper = new TabnavTagHelper
//        {
//            SelectedTab = "tnav2" // Set the selected tab as specified in your example
//        };

//        // Act
//        await tagHelper.ProcessAsync(context, taghelper);

//        // Assert
//        var expectedHtml = GetExpectedHtml(); // Replace this with the expected HTML from your example
//        var renderedHtml = taghelper.Content.GetContent();
//        Assert.Equal(expectedHtml, renderedHtml);
//    }

//    private string GetExpectedHtml()
//    {
//        // Replace this with the expected HTML from your example
//        return @"
//                <div class=""nav nav-tabs flex-sm-column "" id=""nav-tab"" role=""tablist"">
//                    <div class=""container"">
//                        <nav>
//                            <div class=""nav nav-tabs"" id=""nav-tab"" role=""tablist"">
//                                <button class=""nav-link "" id=""tnav1_button"" data-bs-toggle=""tab"" data-bs-target=""#tnav1"" type=""button"" role=""tab"" aria-controls=""nav-home"" aria-selected=""false"" tabindex=""-1"">tab1</button>
//                                <button class=""nav-link active"" id=""tnav2_button"" data-bs-toggle=""tab"" data-bs-target=""#tnav2"" type=""button"" role=""tab"" aria-controls=""nav-home"" aria-selected=""true"">tab3</button>
//                                <button class=""nav-link "" id=""tnav3_button"" data-bs-toggle=""tab"" data-bs-target=""#tnav3"" type=""button"" role=""tab"" aria-controls=""nav-home"" aria-selected=""false"" tabindex=""-1"">tab4</button>
//                            </div>
//                        </nav>
//                        <div class=""tab-content"" id=""nav-tabContent"">
//                            <div class=""tab-pane fade "" id=""tnav1"" name=""tab1"" role=""tabpanel"" aria-labelledby=""tnav1-tab""><div>a</div></div>
//                            <div class=""tab-pane fade show active"" id=""tnav2"" name=""tab3"" role=""tabpanel"" aria-labelledby=""tnav2-tab""><div>a</div></div>
//                            <div class=""tab-pane fade "" id=""tnav3"" name=""tab4"" role=""tabpanel"" aria-labelledby=""tnav3-tab""><div>a</div></div>
//                        </div>
//                    </div>
//                </div>
//            ";
//    }
//}
