using Microsoft.AspNetCore.Mvc;

namespace IdentityServerAspNetIdentity.Pages.Components.List;

public class ListViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IEnumerable<string> items, string name, string label="", string className="", string addFunction="", string removeFunction="")
    {
        // Create a view model object with the parameters
        
        var model = new ListViewModel
        {
            Items = items,
            Name = name,
            AddFunction = addFunction,
            RemoveFunction = removeFunction,
            Label = label ,
            InputID = Guid.NewGuid().ToString(),
            ClassName = className
        };

        // Call the view component and pass the view model
        return View(model);
    }
}