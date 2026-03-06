using System.Text.Json;
using IdentityModel.Client;

var authority = Environment.GetEnvironmentVariable("OIDC_AUTHORITY") ?? "https://localhost:5001";
var clientId = Environment.GetEnvironmentVariable("OIDC_CLIENT_ID") ?? "client";
var clientSecret = Environment.GetEnvironmentVariable("OIDC_CLIENT_SECRET")
    ?? throw new InvalidOperationException("OIDC_CLIENT_SECRET environment variable must be set.");

// discover endpoints from metadata
var client = new HttpClient();
var disco = await client.GetDiscoveryDocumentAsync(authority);
if (disco.IsError)
{
    Console.WriteLine(disco.Error);
    return;
}

// request token
var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
{
    Address = disco.TokenEndpoint,

    ClientId = clientId,
    ClientSecret = clientSecret,
    Scope = "api1",
});

if (tokenResponse.IsError)
{
    Console.WriteLine(tokenResponse.Error);
    Console.WriteLine(tokenResponse.ErrorDescription);
    return;
}

Console.WriteLine(tokenResponse.AccessToken);


// call api
var apiClient = new HttpClient();
apiClient.SetBearerToken(tokenResponse.AccessToken);

var response = await apiClient.GetAsync("https://localhost:6001/identity");
if (!response.IsSuccessStatusCode)
{
    Console.WriteLine(response.StatusCode);
}
else
{
    var content = await response.Content.ReadAsStringAsync();

    var parsed = JsonDocument.Parse(content);
    var formatted = JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });

    Console.WriteLine(formatted);
}
