using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using YamlDotNet.RepresentationModel;

namespace Zapstar.Api.Services;

public interface IGitHubResolver
{
    Task<TipTarget> ResolveRepo(string owner, string repo, CancellationToken ct);
    Task<TipTarget> ResolveUser(string username, CancellationToken ct);
}

public class GitHubResolver(HttpClient http, ILnurlResolver lnurlResolver, IMemoryCache cache, ILogger<GitHubResolver> logger) : IGitHubResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);
    private static readonly string[] CandidateBranches = ["main", "master"];

    public async Task<TipTarget> ResolveRepo(string owner, string repo, CancellationToken ct)
    {
        var cacheKey = $"repo:{owner}/{repo}";
        if (cache.TryGetValue(cacheKey, out TipTarget? cached) && cached is not null)
            return cached;

        var result = await ResolveRepoInternal(owner, repo, ct);
        cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<TipTarget> ResolveUser(string username, CancellationToken ct)
    {
        var cacheKey = $"user:{username}";
        if (cache.TryGetValue(cacheKey, out TipTarget? cached) && cached is not null)
            return cached;

        var result = await ResolveUserInternalAsync(username, ct);
        cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    private async Task<TipTarget> ResolveRepoInternal(string owner, string repo, CancellationToken ct)
    {
        foreach (var branch in CandidateBranches)
        {
            var url = $"https://raw.githubusercontent.com/{owner}/{repo}/{branch}/.github/FUNDING.yml";
            try
            {
                var resp = await http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) continue;

                var yamlText = await resp.Content.ReadAsStringAsync(ct);
                var candidate = ExtractLightningKey(yamlText);
                if (candidate is null) continue;

                if (await lnurlResolver.IsValidLightningAddress(candidate, ct))
                {
                    return new TipTarget
                    {
                        HasLightning = true,
                        Address = candidate,
                        Source = "funding.yml",
                        DisplayName = $"{owner}/{repo}"
                    };
                }

                logger.LogDebug("Candidate {Candidate} from FUNDING.yml did not resolve as a valid Lightning Address for {Owner}/{Repo}",
                    candidate, owner, repo);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "FUNDING.yml lookup failed for {Owner}/{Repo} on {Branch}", owner, repo, branch);
            }
        }

        return new TipTarget { HasLightning = false, DisplayName = $"{owner}/{repo}" };
    }

    private static string? ExtractLightningKey(string yamlText)
    {
        try
        {
            var yaml = new YamlStream();
            yaml.Load(new StringReader(yamlText));
            if (yaml.Documents.Count == 0) return null;

            if (yaml.Documents[0].RootNode is not YamlMappingNode root) return null;

            foreach (var entry in root.Children)
            {
                if (entry.Key is YamlScalarNode key &&
                    string.Equals(key.Value, "lightning", StringComparison.OrdinalIgnoreCase) &&
                    entry.Value is YamlScalarNode value &&
                    !string.IsNullOrWhiteSpace(value.Value))
                {
                    return LightningAddressParser.FindCandidate(value.Value) ?? value.Value.Trim();
                }
            }
        }
        catch { }
        return null;
    }

    private async Task<TipTarget> ResolveUserInternalAsync(string username, CancellationToken ct)
    {
        var bioCandidate = await TryBioAsync(username, ct);
        if (bioCandidate is not null && await lnurlResolver.IsValidLightningAddress(bioCandidate, ct))
        {
            return new TipTarget { HasLightning = true, Address = bioCandidate, Source = "bio", DisplayName = username };
        }

        var readmeCandidate = await TryProfileReadmeAsync(username, ct);
        if (readmeCandidate is not null && await lnurlResolver.IsValidLightningAddress(readmeCandidate, ct))
        {
            return new TipTarget { HasLightning = true, Address = readmeCandidate, Source = "profile-readme", DisplayName = username };
        }
        return new TipTarget { HasLightning = false, DisplayName = username };
    }

    private async Task<string?> TryBioAsync(string username, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/users/{username}");
            request.Headers.UserAgent.ParseAdd("Zapstar/1.0");
            var resp = await http.SendAsync(request, ct);
            if (!resp.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            if (doc.RootElement.TryGetProperty("bio", out var bioEl))
            {
                return LightningAddressParser.FindCandidate(bioEl.GetString());
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Bio lookup failed for {Username}", username);
        }
        return null;
    }

    private async Task<string?> TryProfileReadmeAsync(string username, CancellationToken ct)
    {
        foreach (var branch in CandidateBranches)
        {
            var url = $"https://raw.githubusercontent.com/{username}/{username}/{branch}/README.md";
            try
            {
                var resp = await http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) continue;

                var text = await resp.Content.ReadAsStringAsync(ct);
                var candidate = LightningAddressParser.FindCandidate(text);
                if (candidate is not null) return candidate;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Profile README lookup failed for {Username} on {Branch}", username, branch);
            }
        }
        return null;
    }
}