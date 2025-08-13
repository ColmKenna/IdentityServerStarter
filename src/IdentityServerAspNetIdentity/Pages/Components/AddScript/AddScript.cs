using Microsoft.AspNetCore.Mvc;

namespace IdentityServerAspNetIdentity.Pages.Components.AddScript;

public class AddScriptViewComponent : ViewComponent
{

    IScriptHolder _scriptHolder;

    public AddScriptViewComponent(IScriptHolder scriptHolder)
    {
        _scriptHolder = scriptHolder;
    }

    public IViewComponentResult Invoke(string script )
    {
        if (_scriptHolder.HasScript(script))
            return Content("");
        
        
        _scriptHolder.AddScript(script);
        var model = new AddScriptViewModel()
        {
            Script = script,
            NeedToAdd = true
        };
        
        return View(model);
    }

}