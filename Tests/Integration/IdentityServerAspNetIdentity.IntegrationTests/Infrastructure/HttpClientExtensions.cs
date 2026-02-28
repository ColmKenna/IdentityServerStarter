using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;

namespace IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

public static class AngleSharpHelpers
{
    public static async Task<IHtmlDocument> GetDocumentAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        var document = await context.OpenAsync(req =>
        {
            req.Content(content);
            req.Address(response.RequestMessage?.RequestUri?.ToString() ?? "http://localhost");
        });

        return (IHtmlDocument)document;
    }

    public static async Task<HttpResponseMessage> SendFormAsync(
        HttpClient client,
        IHtmlFormElement form,
        IHtmlElement submitButton,
        IDictionary<string, string>? formValues = null)
    {
        foreach (var kvp in formValues ?? new Dictionary<string, string>())
        {
            var element = form.Elements[kvp.Key];
            if (element is IHtmlInputElement input)
            {
                input.Value = kvp.Value;
            }
            else if (element is IHtmlTextAreaElement textarea)
            {
                textarea.Value = kvp.Value;
            }
            else if (element is IHtmlSelectElement select)
            {
                select.Value = kvp.Value;
            }
        }

        var documentRequest = form.GetSubmission(submitButton);

        var requestUrl = documentRequest?.Target.Href ?? form.Action;
        var requestMethod = documentRequest?.Method.ToString() ?? form.Method;
        if (string.IsNullOrWhiteSpace(requestUrl))
        {
            throw new InvalidOperationException("Could not determine form action URL.");
        }

        if (string.IsNullOrWhiteSpace(requestMethod))
        {
            requestMethod = "POST";
        }

        var request = new HttpRequestMessage(
            new System.Net.Http.HttpMethod(requestMethod),
            requestUrl);

        if (requestMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var formData = new List<KeyValuePair<string, string>>();

            // Collect all form field values
            foreach (var element in form.Elements)
            {
                if (element is IHtmlInputElement inputEl)
                {
                    if (inputEl.Type.Equals("checkbox", StringComparison.OrdinalIgnoreCase))
                    {
                        if (inputEl.IsChecked)
                        {
                            formData.Add(new KeyValuePair<string, string>(inputEl.Name ?? "", inputEl.Value ?? "true"));
                        }
                    }
                    else if (inputEl.Type.Equals("radio", StringComparison.OrdinalIgnoreCase))
                    {
                        if (inputEl.IsChecked)
                        {
                            formData.Add(new KeyValuePair<string, string>(inputEl.Name ?? "", inputEl.Value ?? ""));
                        }
                    }
                    else if (!string.IsNullOrEmpty(inputEl.Name))
                    {
                        formData.Add(new KeyValuePair<string, string>(inputEl.Name, inputEl.Value ?? ""));
                    }
                }
                else if (element is IHtmlTextAreaElement textareaEl && !string.IsNullOrEmpty(textareaEl.Name))
                {
                    formData.Add(new KeyValuePair<string, string>(textareaEl.Name, textareaEl.Value ?? ""));
                }
                else if (element is IHtmlSelectElement selectEl && !string.IsNullOrEmpty(selectEl.Name))
                {
                    formData.Add(new KeyValuePair<string, string>(selectEl.Name, selectEl.Value ?? ""));
                }
            }

            // Override with provided values
            if (formValues != null)
            {
                var overriddenNames = new HashSet<string>(formValues.Keys);
                formData = formData.Where(f => !overriddenNames.Contains(f.Key)).ToList();
                formData.AddRange(formValues.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)));
            }

            request.Content = new FormUrlEncodedContent(formData);
        }

        return await client.SendAsync(request);
    }
}
