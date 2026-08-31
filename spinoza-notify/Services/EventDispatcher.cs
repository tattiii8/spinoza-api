using Dapper;
using Spinoza.Data;
using System.Net.Http.Json;
using System.Text.Json;

namespace Spinoza.Services;

public sealed class EventDispatcher(
    DbConnectionFactory dbFactory,
    EmailDeliveryService emailDeliveryService,
    ILogger<EventDispatcher> logger) : BackgroundService
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchDueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Spinoza dispatcher error");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(1),
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DispatchDueAsync(
        CancellationToken ct)
    {
        await using var db = dbFactory.Create();

        var deliveries =
            await db.QueryAsync<DeliveryRow>("""
                select
                    d.id as DeliveryId,
                    d.event_id as EventId,
                    d.subscription_id as SubscriptionId,
                    d.attempt_count as AttemptCount,

                    s.protocol as Protocol,
                    s.endpoint as Endpoint,

                    e.type as Type,
                    e.version as Version,
                    e.source as Source,
                    e.tenant_id as TenantId,
                    e.subject as Subject,
                    e.occurred_at as Time,
                    e.data::text as Data

                from deliveries d

                join subscriptions s
                    on s.id = d.subscription_id

                join events e
                    on e.id = d.event_id

                where d.status = 'pending'
                  and d.next_attempt_at <= now()
                  and s.active = true

                order by d.next_attempt_at

                limit 50
                """);

        foreach (var d in deliveries)
        {
            if (ct.IsCancellationRequested)
                return;

            var payload = new
            {
                id = d.EventId,
                type = d.Type,
                version = d.Version,
                source = d.Source,
                tenantId = d.TenantId,
                subject = d.Subject,
                time = d.Time,
                data = JsonDocument
                    .Parse(d.Data)
                    .RootElement
            };

            try
            {
                switch (d.Protocol.ToLowerInvariant())
                {
                    case "email":
                        await DispatchEmailAsync(
                            d,
                            payload,
                            ct);

                        break;

                    case "http":
                    case "https":
                        await DispatchHttpAsync(
                            d,
                            payload,
                            ct);

                        break;

                    default:
                        await FailAsync(
                            db,
                            d,
                            $"Unsupported protocol: {d.Protocol}",
                            ct);

                        continue;
                }

                await db.ExecuteAsync("""
                    update deliveries
                    set
                        status='delivered',
                        delivered_at=now(),
                        attempt_count=attempt_count+1
                    where id=@DeliveryId
                    """,
                    new
                    {
                        d.DeliveryId
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Delivery failed. DeliveryId={DeliveryId}, Protocol={Protocol}, Endpoint={Endpoint}",
                    d.DeliveryId,
                    d.Protocol,
                    d.Endpoint);

                await FailAsync(
                    db,
                    d,
                    ex.Message,
                    ct);
            }
        }
    }

    private async Task DispatchHttpAsync(
        DeliveryRow d,
        object payload,
        CancellationToken ct)
    {
        using var response =
            await _http.PostAsJsonAsync(
                d.Endpoint,
                payload,
                ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HTTP {(int)response.StatusCode} " +
                $"{response.ReasonPhrase}");
        }
    }

    private async Task DispatchEmailAsync(
        DeliveryRow d,
        object payload,
        CancellationToken ct)
    {
        var subject = !string.IsNullOrWhiteSpace(d.Subject)
            ? d.Subject
            : $"Spinoza Event: {d.Type}";

        var body = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        await emailDeliveryService.SendAsync(
            d.Endpoint,
            subject,
            body,
            ct);
    }

    private static async Task FailAsync(
        Npgsql.NpgsqlConnection db,
        DeliveryRow d,
        string error,
        CancellationToken ct)
    {
        var nextAttempt =
            DateTimeOffset.UtcNow.AddSeconds(
                Math.Min(
                    Math.Pow(
                        2,
                        Math.Min(
                            d.AttemptCount + 1,
                            8)),
                    300));

        await db.ExecuteAsync("""
            update deliveries
            set
                attempt_count=attempt_count+1,
                next_attempt_at=@nextAttempt,
                last_error=@error
            where id=@DeliveryId
            """,
            new
            {
                d.DeliveryId,
                nextAttempt,
                error
            });
    }

    private sealed class DeliveryRow
    {
        public Guid DeliveryId { get; init; }

        public Guid EventId { get; init; }

        public Guid SubscriptionId { get; init; }

        public int AttemptCount { get; init; }

        public string Protocol { get; init; } = "";

        public string Endpoint { get; init; } = "";

        public string Type { get; init; } = "";

        public int Version { get; init; }

        public string Source { get; init; } = "";

        public string? TenantId { get; init; }

        public string? Subject { get; init; }

        public DateTimeOffset Time { get; init; }

        public string Data { get; init; } = "{}";
    }
}