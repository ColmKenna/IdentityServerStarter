using AngleSharp.Html.Dom;

namespace IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

public static class AngleSharpExtensions
{
    public static async Task<IHtmlDocument> GetAndParsePage(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await AngleSharpHelpers.GetDocumentAsync(response);
    }

    public static async Task<HttpResponseMessage> SubmitForm(
        this HttpClient client,
        IHtmlFormElement form,
        IDictionary<string, string>? values = null)
    {
        var submitButton = form.QuerySelector("button[type='submit']")
                        ?? form.QuerySelector("input[type='submit']");

        if (submitButton is not IHtmlElement button)
        {
            throw new InvalidOperationException("No submit button found in the form.");
        }

        return await AngleSharpHelpers.SendFormAsync(client, form, button, values);
    }
}
