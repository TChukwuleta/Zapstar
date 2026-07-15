using Zapstar.Api.Endpoints;
using Zapstar.Api.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddMemoryCache();

// Named client for GitHub calls - a PAT bumps the rate limit from 60/hr to 5,000/hr.
// Set GitHub:Token in appsettings/user-secrets/env var once you register a token.
builder.Services.AddHttpClient<IGitHubResolver, GitHubResolver>((sp, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    var token = builder.Configuration["GitHub:Token"];
    if (!string.IsNullOrWhiteSpace(token))
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
});

builder.Services.AddHttpClient<ILnurlResolver, LnurlResolver>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ExtensionOnly", policy =>
    {
        // Browser extensions call from a chrome-extension:// / moz-extension:// origin.
        // Locking this down to specific extension IDs happens once you have published IDs;
        // AllowAnyOrigin is fine for local dev since the API holds no user credentials/state.
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});




var app = builder.Build();

app.UseCors("ExtensionOnly");

app.MapTipTargetEndpoints();
app.MapInvoiceEndpoints();

app.MapGet("/", () => Results.Ok(new { status = "Zapstar API is running" }));

app.Run();
