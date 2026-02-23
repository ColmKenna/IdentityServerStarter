using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace IdentityServerAspNetIdentity.Pages.TagHelpers;

/// <summary>
/// A TagHelper that renders a multiselect checkbox grid component for selecting items.
/// Uses multiselect-for for type-safe model binding of selected items and automatic name generation.
/// </summary>
[HtmlTargetElement("multiselect-checkbox", TagStructure = TagStructure.NormalOrSelfClosing)]
public class MultiselectCheckboxTagHelper : TagHelper
{
    /// <summary>
    /// The title/label displayed above the checkbox grid.
    /// </summary>
    [HtmlAttributeName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The description text displayed below the title.
    /// </summary>
    [HtmlAttributeName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Type-safe model expression for the selected items. This also determines the input name.
    /// Usage: multiselect-for="Input.AllowedScopes"
    /// </summary>
    [HtmlAttributeName("multiselect-for")]
    public ModelExpression? For { get; set; }

    /// <summary>
    /// Type-safe model expression for the available items to choose from.
    /// Usage: multiselect-items="Input.AvailableScopes"
    /// </summary>
    [HtmlAttributeName("multiselect-items")]
    public ModelExpression? Items { get; set; }

    /// <summary>
    /// The CSS class to apply to the fieldset element.
    /// </summary>
    [HtmlAttributeName("fieldset-class")]
    public string FieldsetClass { get; set; } = "multiselect-fieldset";

    /// <summary>
    /// The ID for the fieldset element.
    /// </summary>
    [HtmlAttributeName("fieldset-id")]
    public string FieldsetId { get; set; } = string.Empty;

    /// <summary>
    /// The prefix for generating checkbox IDs (e.g., "scope-checkbox" generates "scope-checkbox-0", "scope-checkbox-1", etc.).
    /// </summary>
    [HtmlAttributeName("id-prefix")]
    public string IdPrefix { get; set; } = "checkbox";

    /// <summary>
    /// The message displayed when no items are available.
    /// </summary>
    [HtmlAttributeName("empty-message")]
    public string EmptyMessage { get; set; } = "No items available. Contact your administrator.";

    /// <summary>
    /// The CSS class for the outer container div. Default: "form-group"
    /// </summary>
    [HtmlAttributeName("group-class")]
    public string GroupClass { get; set; } = "form-group";

    /// <summary>
    /// The CSS class for the title label. Default: "form-label"
    /// </summary>
    [HtmlAttributeName("label-class")]
    public string LabelClass { get; set; } = "form-label";

    /// <summary>
    /// The CSS class for the description text. Default: "form-text"
    /// </summary>
    [HtmlAttributeName("description-class")]
    public string DescriptionClass { get; set; } = "form-text";


    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Resolve selected items from multiselect-for
        var selectedItems = ResolveItems(For);
        
        // Resolve available items from multiselect-items
        var availableItems = ResolveItems(Items);
        
        // Derive the input name from the multiselect-for expression (e.g., "Input.AllowedScopes")
        var inputName = For?.Name ?? string.Empty;

        // Set the output element to be a div with form-group class
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", GroupClass);

        // Title label
        if (!string.IsNullOrEmpty(Title))
        {
            var label = new TagBuilder("label");
            label.AddCssClass(LabelClass);
            label.InnerHtml.Append(Title);
            output.Content.AppendHtml(label);
        }

        // Description
        if (!string.IsNullOrEmpty(Description))
        {
            var desc = new TagBuilder("div");
            desc.AddCssClass(DescriptionClass);
            desc.InnerHtml.Append(Description);
            output.Content.AppendHtml(desc);
        }

        // Fieldset with checkbox grid
        var fieldset = new TagBuilder("fieldset");
        fieldset.AddCssClass(FieldsetClass);
        if (!string.IsNullOrEmpty(FieldsetId))
        {
            fieldset.Attributes["id"] = FieldsetId;
        }

        var gridDiv = new TagBuilder("div");
        gridDiv.AddCssClass("multiselect-grid");
        gridDiv.Attributes["role"] = "group";
        if (!string.IsNullOrEmpty(FieldsetId))
        {
            gridDiv.Attributes["aria-labelledby"] = $"{FieldsetId}-label";
        }

        // Render each checkbox option
        for (int i = 0; i < availableItems.Count; i++)
        {
            var item = availableItems[i];
            var inputId = $"{IdPrefix}-{i}";
            var isSelected = selectedItems.Contains(item);

            var optionDiv = new TagBuilder("div");
            optionDiv.AddCssClass("multiselect-option");

            // Checkbox input
            var input = new TagBuilder("input");
            input.TagRenderMode = TagRenderMode.SelfClosing;
            input.AddCssClass("multiselect-input");
            input.Attributes["type"] = "checkbox";
            input.Attributes["id"] = inputId;
            input.Attributes["name"] = inputName;
            input.Attributes["value"] = item;
            input.Attributes["aria-describedby"] = $"{inputId}-desc";
            if (isSelected)
            {
                input.Attributes["checked"] = "checked";
            }
            optionDiv.InnerHtml.AppendHtml(input);

            // Label with pill
            var labelTag = new TagBuilder("label");
            labelTag.AddCssClass("multiselect-label");
            labelTag.Attributes["for"] = inputId;
            
            var pill = new TagBuilder("span");
            pill.AddCssClass("multiselect-pill");
            pill.InnerHtml.Append(item);
            labelTag.InnerHtml.AppendHtml(pill);

            optionDiv.InnerHtml.AppendHtml(labelTag);
            gridDiv.InnerHtml.AppendHtml(optionDiv);
        }

        fieldset.InnerHtml.AppendHtml(gridDiv);
        output.Content.AppendHtml(fieldset);

        // Empty message alert
        if (availableItems.Count == 0)
        {
            var alert = new TagBuilder("div");
            alert.AddCssClass("alert alert-info");
            
            var icon = new TagBuilder("i");
            icon.AddCssClass("fas fa-info-circle");
            alert.InnerHtml.AppendHtml(icon);
            alert.InnerHtml.Append($" {EmptyMessage}");
            
            output.Content.AppendHtml(alert);
        }
    }

    /// <summary>
    /// Resolves items from a ModelExpression.
    /// </summary>
    private static IList<string> ResolveItems(ModelExpression? modelExpression)
    {
        if (modelExpression?.Model is IList<string> modelList)
        {
            return modelList;
        }

        if (modelExpression?.Model is IEnumerable<string> modelEnumerable)
        {
            return modelEnumerable.ToList();
        }

        return new List<string>();
    }
}
