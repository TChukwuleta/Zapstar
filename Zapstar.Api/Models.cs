namespace Zapstar.Api;


public record TipTarget
{
    public bool HasLightning { get; init; }
    public string? Address { get; init; }
    public string? Source { get; init; }
    public string? DisplayName { get; init; }
}

public record LnurlPayResponse
{
    public string Callback { get; init; } = default!;
    public long MinSendable { get; init; } 
    public long MaxSendable { get; init; }
    public string Metadata { get; init; } = default!;
    public string Tag { get; init; } = default!;
    public bool AllowsNostr { get; init; }
    public string? NostrPubkey { get; init; }
}


public record LnurlInvoiceResponse
{
    public string Pr { get; init; } = default!;
    public string? Verify { get; init; }
    public string? Status { get; init; }
    public string? Reason { get; init; }
}

public record InvoiceRequest
{
    public string Address { get; init; } = default!; 
    public long AmountSats { get; init; }
    public string? Comment { get; init; }
}

public record InvoiceResult
{
    public bool Success { get; init; }
    public string? Invoice { get; init; }
    public string? VerifyUrl { get; init; }
    public string? Error { get; init; }
}

public record PaymentStatusResult
{
    public bool Settled { get; init; }
    public string? Error { get; init; }
}
