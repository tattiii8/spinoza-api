using System.Net.Http.Json;

namespace Spinoza.Shared;

public sealed class SpinozaClient(HttpClient httpClient)
    : ISpinozaClient
{
    public async Task PublishAsync<T>(
        Guid topicId,
        SpinozaEvent<T> @event,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/topics/{topicId}/publish",
            @event,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }
}