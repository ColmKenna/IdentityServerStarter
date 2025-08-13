namespace IdentityServerAspNetIdentity.Pages.Components;

class ScriptHolder : IScriptHolder
{
    private readonly IList<string> _scripts;

    public ScriptHolder()
    {
        _scripts = new List<string>();
        
    }

    public void AddScript(string script)
    {
        if(HasScript(script)) 
            return;
            
        _scripts.Add(script);
    }
    
    public bool HasScript(string script)
    {
        return _scripts.Contains(script);
    }
    
}