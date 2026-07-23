using Zapstar.Api.Endpoints;
using Zapstar.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
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
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("ExtensionOnly");
app.MapTipTargetEndpoints();
app.MapInvoiceEndpoints();
app.MapGet("/", () => Results.Ok(new { status = "Zapstar API is running" }));
app.Run();
