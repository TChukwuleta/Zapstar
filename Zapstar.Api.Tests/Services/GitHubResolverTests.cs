using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Zapstar.Api.Services;
using Zapstar.Api.Tests.TestHelpers;

namespace Zapstar.Api.Tests.Services;

public class GitHubResolverTests
{
    private static GitHubResolver BuildResolver(FakeHttpMessageHandler handler, ILnurlResolver lnurlResolver)
    {
        var httpClient = new HttpClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return new GitHubResolver(httpClient, lnurlResolver, cache, NullLogger<GitHubResolver>.Instance);
    }

    [Fact]
    public async Task ResolveRepo_ReturnsHasLightningTrue_WhenFundingYmlHasValidAddress()
    {
        var handler = new FakeHttpMessageHandler().When("raw.githubusercontent.com/TChukwuleta/Zapstar/main/.github/FUNDING.yml", HttpStatusCode.NotFound, "");
        handler.When("raw.githubusercontent.com/TChukwuleta/Zapstar/master/.github/FUNDING.yml", HttpStatusCode.OK, "lightning: tobses@btcpay.example.com");

        var lnurlResolver = Substitute.For<ILnurlResolver>();
        lnurlResolver.IsValidLightningAddress("tobses@btcpay.example.com", Arg.Any<CancellationToken>()).Returns(true);
        var resolver = BuildResolver(handler, lnurlResolver);
        var result = await resolver.ResolveRepo("TChukwuleta", "Zapstar", CancellationToken.None);
        Assert.True(result.HasLightning);
        Assert.Equal("tobses@btcpay.example.com", result.Address);
        Assert.Equal("funding.yml", result.Source);
    }

    [Fact]
    public async Task ResolveRepo_ReturnsFalse_WhenNoFundingYmlExists()
    {
        var handler = new FakeHttpMessageHandler();
        var lnurlResolver = Substitute.For<ILnurlResolver>();
        var resolver = BuildResolver(handler, lnurlResolver);
        var result = await resolver.ResolveRepo("someowner", "somerepo", CancellationToken.None);
        Assert.False(result.HasLightning);
        Assert.Null(result.Address);
    }

    [Fact]
    public async Task ResolveRepoAsync_ReturnsFalse_WhenCandidateFailsLnurlValidation()
    {
        // Simulates the email-vs-Lightning-Address bug fix: a plain email in FUNDING.yml
        // should not be trusted just because it's present in the file.
        var handler = new FakeHttpMessageHandler().When(
            "raw.githubusercontent.com/owner/repo/main/.github/FUNDING.yml",
            HttpStatusCode.OK, "lightning: not-really-lightning@gmail.com");

        var lnurlResolver = Substitute.For<ILnurlResolver>();
        lnurlResolver.IsValidLightningAddress("not-really-lightning@gmail.com", Arg.Any<CancellationToken>())
            .Returns(false);

        var resolver = BuildResolver(handler, lnurlResolver);
        var result = await resolver.ResolveRepo("owner", "repo", CancellationToken.None);

        Assert.False(result.HasLightning);
    }

    [Fact]
    public async Task ResolveRepoAsync_CachesResult_SoSecondCallDoesNotHitHttpAgain()
    {
        var callCount = 0;
        var handler = new FakeHttpMessageHandler().When(
            "raw.githubusercontent.com/owner/repo/main/.github/FUNDING.yml",
            HttpStatusCode.OK, "lightning: tobses@btcpay.example.com");

        var lnurlResolver = Substitute.For<ILnurlResolver>();
        lnurlResolver.IsValidLightningAddress(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => { callCount++; return Task.FromResult(true); });

        var resolver = BuildResolver(handler, lnurlResolver);

        await resolver.ResolveRepo("owner", "repo", CancellationToken.None);
        await resolver.ResolveRepo("owner", "repo", CancellationToken.None);

        Assert.Equal(1, callCount); // second call should hit cache, not re-validate
    }

    [Fact]
    public async Task ResolveUserAsync_FallsBackToProfileReadme_WhenBioHasNoValidAddress()
    {
        var handler = new FakeHttpMessageHandler()
            .When("api.github.com/users/tobses", HttpStatusCode.OK, """{"bio":"just a dev, no ln address"}""")
            .When("raw.githubusercontent.com/tobses/tobses/main/README.md", HttpStatusCode.OK,
                "Hi, tip me at ⚡ tobses@btcpay.example.com");

        var lnurlResolver = Substitute.For<ILnurlResolver>();
        lnurlResolver.IsValidLightningAddress("tobses@btcpay.example.com", Arg.Any<CancellationToken>())
            .Returns(true);

        var resolver = BuildResolver(handler, lnurlResolver);
        var result = await resolver.ResolveUser("tobses", CancellationToken.None);

        Assert.True(result.HasLightning);
        Assert.Equal("profile-readme", result.Source);
    }
}