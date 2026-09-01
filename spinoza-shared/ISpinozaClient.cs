namespace Spinoza.Shared;

public interface ISpinozaClient
{
    Task PublishAsync<T>(
        Guid topicId,
        SpinozaEvent<T> @event,
        CancellationToken cancellationToken = default);
}