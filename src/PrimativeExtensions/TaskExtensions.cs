namespace PrimativeExtensions;





public static class TaskExtensions
{

    public static Task<T> AsTask<T>(this T value) => Task.FromResult(value);

    public static async Task<R> Then<T, R>(this Task<T> task, Func<T, R> selector, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = await task.ConfigureAwait(false);
        return selector(result);
    }
}


