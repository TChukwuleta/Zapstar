using Zapstar.Api.Services;

namespace Zapstar.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this WebApplication app)
    {
        app.MapPost("/invoice", async (InvoiceRequest req, ILnurlResolver resolver, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Address) || req.AmountSats <= 0)
                return Results.BadRequest(new { error = "A valid address and positive amountSats are required." });

            if (req.AmountSats > 1_000_000)
                return Results.BadRequest(new { error = "Amount exceeds maximum allowed (1,000,000 sats)." });

            var result = await resolver.GetInvoice(req.Address, req.AmountSats, req.Comment, ct);
            return result.Success ? Results.Ok(result) : Results.BadRequest(result);
        })
        .WithName("CreateInvoice")
        .Produces<InvoiceResult>(200)
        .Produces(400);

        app.MapGet("/invoice/status", async (string verifyUrl, ILnurlResolver resolver, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(verifyUrl))
                return Results.BadRequest(new { error = "verifyUrl is required." });

            var result = await resolver.CheckPaymentStatus(verifyUrl, ct);
            return Results.Ok(result);
        })
        .WithName("CheckInvoiceStatus")
        .Produces<PaymentStatusResult>(200)
        .Produces(400);
    }
}
