namespace Spinoza.Models;

public sealed class CreateSubscriptionRequest
{
    public string Protocol { get; init; } = "https";
    public string Endpoint { get; init; } = "";
    public string[]? FilterEventTypes { get; init; }
    public string[]? FilterTenantIds { get; init; }
}
