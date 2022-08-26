using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Xunit
{
    static class TaskExtensions
    {
        public static async Task<T> TimeoutAfterAsync<T>(this Task<T> task, int millisecondsTimeout)
        {
            if (RunContext.DisableTimeout)
                return await task;

            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)))
            {
                return await task;
            }
            else
            {
                // Ignore errors on faulted but otherwise timed out test.
                _ = task.ContinueWith(_ => { }, default, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                throw new TimeoutException(string.Format("Execution didn't complete within the required maximum {0} seconds.", millisecondsTimeout / 1000));
            }
        }
    }
}
