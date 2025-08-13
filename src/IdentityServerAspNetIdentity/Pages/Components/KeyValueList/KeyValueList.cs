using Microsoft.AspNetCore.Mvc;

namespace IdentityServerAspNetIdentity.Pages.Components.KeyValueList;


public class KeyValueListViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(IList<KeyValuePair<string,string>> Properties , string ComponentID, string Name, IList<string> keyOptions = null )
    {
        var model = new KeyValueListViewModel()
        {
            Properties = Properties,
            ComponentID = ComponentID,
            Name = Name,
            KeyOptions = keyOptions
        };
        return View(model);
    }
    

}

public class KeyValueListViewModel  
{
    public IList<KeyValuePair<string,string>> Properties { get; set; }
    public string ComponentID { get; set; }
    public string Name { get; set; }
    public IList<string> KeyOptions { get; set; }
    
}
