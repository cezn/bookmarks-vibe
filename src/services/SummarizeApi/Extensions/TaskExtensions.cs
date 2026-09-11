namespace SummarizeApi.Extensions;

public static class TaskExtensions
{
    extension(Task)
    {
        public static async Task<(T1, T2)> WhenAll<T1, T2>(Task<T1> t1, Task<T2> t2)
        {
            await Task.WhenAll(t1, t2);
            return (t1.Result, t2.Result);
        }
    }
}
