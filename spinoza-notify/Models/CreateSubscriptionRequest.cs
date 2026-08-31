namespace Spinoza.Models;

public sealed class CreateSubscriptionRequest
{
    /// <summary>
    /// Delivery protocol.
    /// Supported values:
    /// - http
    /// - https
    /// - email
    /// </summary>
    public string Protocol { get; init; } = "https";

    /// <summary>
    /// Delivery endpoint.
    ///
    /// For http/https:
    ///   https://example.com/webhook
    ///
    /// For email:
    ///   user@example.com
    /// </summary>
    public string Endpoint { get; init; } = "";

    public string[]? FilterEventTypes { get; init; }

    public string[]? FilterTenantIds { get; init; }
}
