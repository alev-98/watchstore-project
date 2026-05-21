namespace WatchStore.IntegrationTests.Data;

public sealed class OutboxMessageProcessedInterceptor(Guid targetMessageId) : SaveChangesInterceptor
{
    private readonly TaskCompletionSource taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitAsync(TimeSpan timeout)
    {
        return Task.WhenAny(taskCompletionSource.Task, Task.Delay(timeout))
                    .ContinueWith(t => taskCompletionSource.Task.IsCompleted
                        ? Task.CompletedTask
                        : throw new TimeoutException("Outbox message was not marked as processed in time."));
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (!taskCompletionSource.Task.IsCompleted && eventData.Context is WatchStoreContext ctx)
        {
            OutboxMessage? message = await ctx.OutboxMessages
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(m => m.Id == targetMessageId, cancellationToken);

            if (message?.ProcessedAt != null)
            {
                taskCompletionSource.TrySetResult();
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
