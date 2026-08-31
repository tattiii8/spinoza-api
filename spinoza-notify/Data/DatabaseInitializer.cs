using Dapper;

namespace Spinoza.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
        await using var db = factory.Create();
        await db.OpenAsync();

        await db.ExecuteAsync("create extension if not exists pgcrypto;");

        await db.ExecuteAsync("""
            create table if not exists topics (
                id uuid primary key,
                name varchar(200) not null unique,
                created_at timestamptz not null
            );

            create table if not exists subscriptions (
                id uuid primary key,
                topic_id uuid not null references topics(id) on delete cascade,
                protocol varchar(20) not null default 'https',
                endpoint text not null,
                filter_event_types jsonb null,
                filter_tenant_ids jsonb null,
                active boolean not null default true,
                created_at timestamptz not null
            );

            create index if not exists ix_subscriptions_topic_active
                on subscriptions(topic_id, active);

            create table if not exists events (
                id uuid primary key,
                topic_id uuid not null references topics(id) on delete cascade,
                type varchar(300) not null,
                version integer not null default 1,
                source varchar(200) not null,
                tenant_id varchar(200) null,
                subject varchar(500) null,
                occurred_at timestamptz not null,
                data jsonb not null,
                created_at timestamptz not null
            );

            create index if not exists ix_events_topic_created
                on events(topic_id, created_at desc);

            create table if not exists deliveries (
                id uuid primary key,
                event_id uuid not null references events(id) on delete cascade,
                subscription_id uuid not null references subscriptions(id) on delete cascade,
                status varchar(30) not null default 'pending',
                attempt_count integer not null default 0,
                next_attempt_at timestamptz not null,
                last_error text null,
                delivered_at timestamptz null,
                created_at timestamptz not null,
                unique(event_id, subscription_id)
            );

            create index if not exists ix_deliveries_due
                on deliveries(status, next_attempt_at);
            """);
    }
}
