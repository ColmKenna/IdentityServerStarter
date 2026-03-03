namespace PrimativeExtensions;





public static class TaskExtensions
{
    
    public static Task<T> AsTask<T>(this T value) => Task.FromResult(value);
    
    public static Task<R> Then<T, R>(this Task<T> task, Func<T, R> selector, CancellationToken cancellationToken = default)
        => task.ContinueWith(t => selector(t.Result), cancellationToken);
}


