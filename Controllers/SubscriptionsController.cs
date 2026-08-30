using Dapper;
using Microsoft.AspNetCore.Mvc;
using Spinoza.Data;

namespace Spinoza.Controllers;

[ApiController]
[Route("subscriptions")]
public sealed class SubscriptionsController(DbConnectionFactory dbFactory) : ControllerBase
{
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await using var db = dbFactory.Create();
        var count = await db.ExecuteAsync(
            "delete from subscriptions where id=@id", new { id });
        return count == 0 ? NotFound() : NoContent();
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id)
    {
        await using var db = dbFactory.Create();
        var count = await db.ExecuteAsync(
            "update subscriptions set active=false where id=@id", new { id });
        return count == 0 ? NotFound() : NoContent();
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id)
    {
        await using var db = dbFactory.Create();
        var count = await db.ExecuteAsync(
            "update subscriptions set active=true where id=@id", new { id });
        return count == 0 ? NotFound() : NoContent();
    }
}
