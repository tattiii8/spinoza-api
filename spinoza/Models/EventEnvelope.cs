using System.Text.Json;

namespace Spinoza.Models;

public sealed record EventEnvelope(
    Guid Id,
    string Type,
    int Version,
    string Source,
    string? TenantId,
    string? Subject,
    DateTimeOffset Time,
    JsonElement Data);
