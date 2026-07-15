using System.Text.Json;

namespace Zapstar.Api.Services;

public interface ILnurlResolver
{
    Task<bool> IsValidLightningAddress(string candidateAddress, CancellationToken ct);
    Task<InvoiceResult> GetInvoice(string lightningAddress, long amountSats, string? comment, CancellationToken ct);
}


public class LnurlResolver(HttpClient http, ILogger<LnurlResolver> logger) : ILnurlResolver
{
    public async Task<InvoiceResult> GetInvoice(string lightningAddress, long amountSats, string? comment, CancellationToken ct)
    {
        var payParams = await TryResolvePayParams(lightningAddress, ct);
        if (payParams is null)
            return new InvoiceResult { Success = false, Error = "Could not resolve this Lightning Address." };

        var amountMsats = amountSats * 1000;
        if (amountMsats < payParams.MinSendable || amountMsats > payParams.MaxSendable)
        {
            var minSats = payParams.MinSendable / 1000;
            var maxSats = payParams.MaxSendable / 1000;
            return new InvoiceResult { Success = false, Error = $"Amount must be between {minSats} and {maxSats} sats for this recipient." };
        }

        try
        {
            var callbackUri = new UriBuilder(payParams.Callback);
            var query = System.Web.HttpUtility.ParseQueryString(callbackUri.Query);
            query["amount"] = amountMsats.ToString();
            if (!string.IsNullOrWhiteSpace(comment))
                query["comment"] = comment;
            callbackUri.Query = query.ToString();

            var invoiceResp = await http.GetAsync(callbackUri.Uri, ct);
            using var invoiceDoc = JsonDocument.Parse(await invoiceResp.Content.ReadAsStringAsync(ct));
            var invoiceRoot = invoiceDoc.RootElement;

            if (invoiceRoot.TryGetProperty("status", out var statusEl) &&
                string.Equals(statusEl.GetString(), "ERROR", StringComparison.OrdinalIgnoreCase))
            {
                var reason = invoiceRoot.TryGetProperty("reason", out var r) ? r.GetString() : "Unknown error";
                return new InvoiceResult { Success = false, Error = reason };
            }

            var pr = invoiceRoot.GetProperty("pr").GetString();
            return new InvoiceResult { Success = true, Invoice = pr };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "LNURL callback failed for {Address}", lightningAddress);
            return new InvoiceResult { Success = false, Error = "Failed to generate invoice." };
        }
    }

    public async Task<bool> IsValidLightningAddress(string candidateAddress, CancellationToken ct)
    {
        var payParams = await TryResolvePayParams(candidateAddress, ct);
        return payParams is not null && string.Equals(payParams.Tag, "payRequest", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(payParams.Callback);
    }

    private async Task<LnurlPayResponse?> TryResolvePayParams(string address, CancellationToken ct)
    {
        var parts = address.Split('@');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
            return null;

        var (user, domain) = (parts[0], parts[1]);
        var wellKnownUrl = $"https://{domain}/.well-known/lnurlp/{user}";

        try
        {
            var resp = await http.GetAsync(wellKnownUrl, ct);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            if (!root.TryGetProperty("callback", out var callbackEl) || !root.TryGetProperty("minSendable", out var minEl) || !root.TryGetProperty("maxSendable", out var maxEl))
            {
                return null;
            }

            return new LnurlPayResponse
            {
                Callback = callbackEl.GetString()!,
                MinSendable = minEl.GetInt64(),
                MaxSendable = maxEl.GetInt64(),
                Metadata = root.TryGetProperty("metadata", out var m) ? m.GetString() ?? "" : "",
                Tag = root.TryGetProperty("tag", out var t) ? t.GetString() ?? "" : ""
            };
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "LNURL well-known lookup failed for {Address}", address);
            return null;
        }
    }
}
