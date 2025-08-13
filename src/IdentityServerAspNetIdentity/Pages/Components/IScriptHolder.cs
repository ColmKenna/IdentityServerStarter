namespace IdentityServerAspNetIdentity.Pages.Components;

public interface IScriptHolder
{
    public void AddScript(string script);
    public bool HasScript(string script);
}