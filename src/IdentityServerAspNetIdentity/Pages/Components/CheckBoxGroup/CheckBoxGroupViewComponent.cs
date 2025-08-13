using System.Collections;
using Duende.IdentityServer.Models;
using IdentityServerAspNetIdentity.Pages.Components.List;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerAspNetIdentity.Pages.Components.CheckBoxGroup;

public class CheckboxViewModel
{
    public string Value { get; set; }
    public string Text { get; set; }
    public bool Selected { get; set; }
}

public class CheckBoxGroupViewComponent : ViewComponent
{

    public IViewComponentResult Invoke(string name, List<CheckboxViewModel> grantTypes, string label="")
    {
        return View(new CheckBoxGroupViewwModel { Label = label, Items = grantTypes, Name = name});
    }
    
}

public class CheckBoxGroupViewwModel    
{
    public string Label { get; set; }
    public string Name { get; set; }
    public List<CheckboxViewModel> Items { get; set; }
}