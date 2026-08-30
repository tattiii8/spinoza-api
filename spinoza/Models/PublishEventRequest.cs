using System.Text.Json;

namespace Spinoza.Models;

public sealed class PublishEventRequest
{
    public string Type { get; init; } = "";
    public int Version { get; init; } = 1;
    public string Source { get; init; } = "";
    public string? TenantId { get; init; }
    public string? Subject { get; init; }
    public DateTimeOffset? Time { get; init; }
    public JsonElement Data { get; init; }
}
