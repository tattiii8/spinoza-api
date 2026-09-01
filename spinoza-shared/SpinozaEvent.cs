namespace Spinoza.Shared;

public sealed record SpinozaEvent<T>(
    Guid Id,
    string Type,
    int Version,
    string Source,
    string Subject,
    string TenantId,
    DateTimeOffset Time,
    T Data
);