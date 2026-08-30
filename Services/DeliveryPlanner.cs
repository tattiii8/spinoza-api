using Dapper;
using Spinoza.Data;

namespace Spinoza.Services;

public static class DeliveryPlanner
{
    public static async Task PlanAsync(
        Npgsql.NpgsqlConnection db, Guid eventId, Guid topicId,
        string eventType, string? tenantId, Npgsql.NpgsqlTransaction? transaction = null)
    {
        // Reserved for the next iteration: materialize matching subscriptions
        // into deliveries when the event is published.
        // Kept separate so publish and delivery logic remain independently testable.
        await db.ExecuteAsync("""
            insert into deliveries
            (id,event_id,subscription_id,status,attempt_count,next_attempt_at,created_at)
            select gen_random_uuid(), @eventId, s.id, 'pending', 0, now(), now()
            from subscriptions s
            where s.topic_id=@topicId
              and s.active=true
              and (
                s.filter_event_types is null
                or s.filter_event_types = 'null'::jsonb
                or s.filter_event_types ? @eventType
              )
              and (
                s.filter_tenant_ids is null
                or s.filter_tenant_ids = 'null'::jsonb
                or @tenantId is null
                or s.filter_tenant_ids ? @tenantId
              )
            on conflict (event_id, subscription_id) do nothing
            """, new { eventId, topicId, eventType, tenantId }, transaction);
    }
}
