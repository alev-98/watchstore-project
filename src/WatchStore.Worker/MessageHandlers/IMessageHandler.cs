namespace WatchStore.Worker.MessageHandlers;

public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage orderPaid, CancellationToken ct = default);
}
