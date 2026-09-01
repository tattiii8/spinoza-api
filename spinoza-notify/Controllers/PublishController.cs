using Dapper;
using Microsoft.AspNetCore.Mvc;
using Spinoza.Data;
using Spinoza.Models;
using Spinoza.Services;
using System.Text.Json;

namespace Spinoza.Controllers;

[ApiController]
[Route("topics/{topicId:guid}/publish")]
public sealed class PublishController(
    DbConnectionFactory dbFactory) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Publish(
        Guid topicId,
        PublishEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(new
            {
                error = "type is required"
            });

        if (string.IsNullOrWhiteSpace(request.Source))
            return BadRequest(new
            {
                error = "source is required"
            });

        await using var db = dbFactory.Create();

        // BeginTransactionAsync() は
        // Open された Connection に対して呼ぶ必要がある。
        await db.OpenAsync();

        await using var tx =
            await db.BeginTransactionAsync();

        try
        {
            var exists =
                await db.ExecuteScalarAsync<bool>(
                    """
                    select exists(
                        select 1
                        from topics
                        where id=@topicId
                    )
                    """,
                    new
                    {
                        topicId
                    },
                    tx);

            if (!exists)
            {
                await tx.RollbackAsync();

                return NotFound(new
                {
                    error = "topic not found"
                });
            }

            var id = Guid.NewGuid();

            var occurredAt =
                request.Time ??
                DateTimeOffset.UtcNow;

            await db.ExecuteAsync(
                """
                insert into events
                (
                    id,
                    topic_id,
                    type,
                    version,
                    source,
                    tenant_id,
                    subject,
                    occurred_at,
                    data,
                    created_at
                )
                values
                (
                    @id,
                    @topicId,
                    @type,
                    @version,
                    @source,
                    @tenantId,
                    @subject,
                    @occurredAt,
                    @data::jsonb,
                    @createdAt
                )
                """,
                new
                {
                    id,
                    topicId,
                    type = request.Type,
                    version = request.Version,
                    source = request.Source,
                    tenantId = request.TenantId,
                    subject = request.Subject,
                    occurredAt,
                    data = JsonSerializer.Serialize(
                        request.Data),
                    createdAt =
                        DateTimeOffset.UtcNow
                },
                tx);

            await DeliveryPlanner.PlanAsync(
                db,
                id,
                topicId,
                request.Type,
                request.TenantId,
                tx);

            await tx.CommitAsync();

            return Accepted(new
            {
                id,
                type = request.Type,
                version = request.Version,
                source = request.Source,
                tenantId = request.TenantId,
                subject = request.Subject,
                time = occurredAt
            });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
