using System.Collections;

namespace IdentityServerAspNetIdentity.Pages.Components.List;

public class ListViewModel
{
    public IEnumerable Items { get; set; } // The list of current Items
    public string Name { get; set; } // The name of the array to post
    public string AddFunction { get; set; } // The name of the function to use on the add button click event
    public string RemoveFunction { get; set; } // The name of the function to use on the remove button click event
    public string Label { get; set; } // The label for the list
    public string InputID { get; set; }
    public string ClassName { get; set; }
}