using Dapper;
using Microsoft.AspNetCore.Mvc;
using Spinoza.Data;
using Spinoza.Models;
using System.Text.Json;

namespace Spinoza.Controllers;

[ApiController]
[Route("topics")]
public sealed class TopicsController(DbConnectionFactory dbFactory) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateTopicRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "name is required" });

        await using var db = dbFactory.Create();
        var topic = new Topic(Guid.NewGuid(), request.Name.Trim(), DateTimeOffset.UtcNow);

        try
        {
            await db.ExecuteAsync(
                "insert into topics(id,name,created_at) values (@Id,@Name,@CreatedAt)",
                topic);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            return Conflict(new { error = "topic already exists" });
        }

        return Created($"/topics/{topic.Id}", topic);
    }

    [HttpGet]
    public async Task<IEnumerable<Topic>> List()
    {
        await using var db = dbFactory.Create();
        return await db.QueryAsync<Topic>(
            "select id, name, created_at as CreatedAt from topics order by created_at desc");
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        await using var db = dbFactory.Create();
        var topic = await db.QuerySingleOrDefaultAsync<Topic>(
            "select id, name, created_at as CreatedAt from topics where id=@id", new { id });

        return topic is null ? NotFound() : Ok(topic);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await using var db = dbFactory.Create();
        var count = await db.ExecuteAsync("delete from topics where id=@id", new { id });
        return count == 0 ? NotFound() : NoContent();
    }

    [HttpPost("{id:guid}/subscriptions")]
    public async Task<IActionResult> Subscribe(Guid id, CreateSubscriptionRequest request)
    {
        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var uri) ||
            (request.Protocol.Equals("https", StringComparison.OrdinalIgnoreCase) && uri.Scheme != "https") ||
            (request.Protocol.Equals("http", StringComparison.OrdinalIgnoreCase) && uri.Scheme != "http"))
            return BadRequest(new { error = "endpoint must be a valid http/https URL" });

        await using var db = dbFactory.Create();
        var exists = await db.ExecuteScalarAsync<bool>(
            "select exists(select 1 from topics where id=@id)", new { id });
        if (!exists) return NotFound();

        var subscription = new Subscription(
            Guid.NewGuid(), id, request.Protocol.ToLowerInvariant(), request.Endpoint,
            request.FilterEventTypes, request.FilterTenantIds, true, DateTimeOffset.UtcNow);

        await db.ExecuteAsync("""
            insert into subscriptions
            (id,topic_id,protocol,endpoint,filter_event_types,filter_tenant_ids,active,created_at)
            values (@Id,@TopicId,@Protocol,@Endpoint,@FilterEventTypes::jsonb,@FilterTenantIds::jsonb,@Active,@CreatedAt)
            """,
            new {
                subscription.Id,
                subscription.TopicId,
                subscription.Protocol,
                subscription.Endpoint,
                FilterEventTypes = JsonSerializer.Serialize(subscription.FilterEventTypes),
                FilterTenantIds = JsonSerializer.Serialize(subscription.FilterTenantIds),
                subscription.Active,
                subscription.CreatedAt
            });

        return Created($"/subscriptions/{subscription.Id}", subscription);
    }

    [HttpGet("{id:guid}/subscriptions")]
    public async Task<IActionResult> ListSubscriptions(Guid id)
    {
        await using var db = dbFactory.Create();
        var rows = await db.QueryAsync<dynamic>("""
            select id, topic_id, protocol, endpoint,
                   filter_event_types::text as filter_event_types,
                   filter_tenant_ids::text as filter_tenant_ids,
                   active, created_at
            from subscriptions
            where topic_id=@id
            order by created_at desc
            """, new { id });

        var result = rows.Select(x => new
        {
            id = (Guid)x.id,
            topicId = (Guid)x.topic_id,
            protocol = (string)x.protocol,
            endpoint = (string)x.endpoint,
            filterEventTypes = ParseJsonArray((string?)x.filter_event_types),
            filterTenantIds = ParseJsonArray((string?)x.filter_tenant_ids),
            active = (bool)x.active,
            createdAt = (DateTimeOffset)x.created_at
        });

        return Ok(result);
    }

    private static string[]? ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return null;

        return System.Text.Json.JsonSerializer.Deserialize<string[]>(json);
    }
}
