using Microsoft.AspNetCore.Razor.TagHelpers;

namespace IdentityServerAspNetIdentity.Tests;

public static class TagHelpersUtils
{
    public static TagHelperOutput Generate(string tagName, Dictionary<string, object> attributes, string content)
    {
        var attributeList = new TagHelperAttributeList();

        foreach (var attribute in attributes)
        {
            attributeList.Add(attribute.Key, attribute.Value);
        }

        return new TagHelperOutput(tagName, attributeList,
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetHtmlContent(content)));
    }
}