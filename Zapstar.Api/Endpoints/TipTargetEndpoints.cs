using Zapstar.Api.Services;

namespace Zapstar.Api.Endpoints;

public static class TipTargetEndpoints
{
    public static void MapTipTargetEndpoints(this WebApplication app)
    {
        app.MapGet("/repo/{owner}/{repo}", async (string owner, string repo, IGitHubResolver resolver, CancellationToken ct) =>
        {
            var result = await resolver.ResolveRepo(owner, repo, ct);
            return Results.Ok(result);
        })
        .WithName("ResolveRepo")
        .Produces(200);

        app.MapGet("/user/{username}", async (string username, IGitHubResolver resolver, CancellationToken ct) =>
        {
            var result = await resolver.ResolveUser(username, ct);
            return Results.Ok(result);
        })
        .WithName("ResolveUser")
        .Produces(200);
    }
}
