namespace Spinoza.Models;

public sealed record Subscription(
    Guid Id,
    Guid TopicId,
    string Protocol,
    string Endpoint,
    string[]? FilterEventTypes,
    string[]? FilterTenantIds,
    bool Active,
    DateTimeOffset CreatedAt);
