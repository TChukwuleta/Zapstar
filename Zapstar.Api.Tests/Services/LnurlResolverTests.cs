using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Zapstar.Api.Services;
using Zapstar.Api.Tests.TestHelpers;

namespace Zapstar.Api.Tests.Services;

public class LnurlResolverTests
{
    private static LnurlResolver BuildResolver(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new LnurlResolver(httpClient, NullLogger<LnurlResolver>.Instance);
    }

    [Fact]
    public async Task IsValidLightningAddress_ReturnsTrue_ForRealLnurlPayResponse()
    {
        var handler = new FakeHttpMessageHandler().When("/.well-known/lnurlp/tobses", HttpStatusCode.OK, 
            """{"callback":"https://btcpay.example.com/pay/tobses","tag":"payRequest","minSendable":1000,"maxSendable":100000000}""");

        var resolver = BuildResolver(handler);
        var result = await resolver.IsValidLightningAddress("tobses@btcpay.example.com", CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public async Task IsValidLightningAddress_ReturnsFalse_ForPlainEmailWith404()
    {
        var handler = new FakeHttpMessageHandler();
        var resolver = BuildResolver(handler);
        var result = await resolver.IsValidLightningAddress("someone@gmail.com", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task IsValidLightningAddress_ReturnsFalse_WhenTagIsNotPayRequest()
    {
        var handler = new FakeHttpMessageHandler().When("/.well-known/lnurlp/someone", HttpStatusCode.OK,
            """{"callback":"https://example.com/pay","tag":"withdrawRequest","minSendable":1000,"maxSendable":100000000}""");

        var resolver = BuildResolver(handler);
        var result = await resolver.IsValidLightningAddress("someone@example.com", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task IsValidLightningAddress_ReturnsFalse_ForMalformedAddress()
    {
        var handler = new FakeHttpMessageHandler();
        var resolver = BuildResolver(handler);
        var result = await resolver.IsValidLightningAddress("not-an-address", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetInvoice_ReturnsError_WhenAmountBelowMinSendable()
    {
        var handler = new FakeHttpMessageHandler().When("/.well-known/lnurlp/tobses", HttpStatusCode.OK,
            """{"callback":"https://btcpay.example.com/pay/tobses","tag":"payRequest","minSendable":5000,"maxSendable":100000000}""");
        
        var resolver = BuildResolver(handler);
        var result = await resolver.GetInvoice("tobses@btcpay.example.com", amountSats: 1, comment: null, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Contains("between", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetInvoice_ReturnsInvoice_OnSuccess()
    {
        var handler = new FakeHttpMessageHandler()
            .When("/.well-known/lnurlp/tobses", HttpStatusCode.OK, """{"callback":"https://btcpay.example.com/pay/tobses","tag":"payRequest","minSendable":1000,"maxSendable":100000000}""")
            .When("/pay/tobses", HttpStatusCode.OK, """{"pr":"lnbc1u1p...fakeinvoice"}""");

        var resolver = BuildResolver(handler);
        var result = await resolver.GetInvoice("tobses@btcpay.example.com", amountSats: 100, comment: "test", CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal("lnbc1u1p...fakeinvoice", result.Invoice);
    }

    [Fact]
    public async Task GetInvoice_ReturnsError_WhenCallbackReturnsErrorStatus()
    {
        var handler = new FakeHttpMessageHandler()
            .When("/.well-known/lnurlp/tobses", HttpStatusCode.OK, """{"callback":"https://btcpay.example.com/pay/tobses","tag":"payRequest","minSendable":1000,"maxSendable":100000000}""")
            .When("/pay/tobses", HttpStatusCode.OK, """{"status":"ERROR","reason":"Invoice generation failed"}""");

        var resolver = BuildResolver(handler);
        var result = await resolver.GetInvoice("tobses@btcpay.example.com", amountSats: 100, comment: null, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal("Invoice generation failed", result.Error);
    }
}