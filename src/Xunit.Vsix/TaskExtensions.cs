﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Xunit
{
    static class TaskExtensions
    {
        public static async Task<T> TimeoutAfter<T>(this Task<T> task, int millisecondsTimeout)
        {
            var timeout = bool.TryParse(Environment.GetEnvironmentVariable(Constants.DebugTimeoutsEnvironmentVariable), out var shouldTimeout) && shouldTimeout;
            // Never timeout if a debugger is attached.
            if (Debugger.IsAttached && timeout != true)
                return await task;

            if (task == await Task.WhenAny(task, Task.Delay(millisecondsTimeout)))
            {
                return await task;
            }
            else
            {
                // Ignore errors on faulted but otherwise timed out test.
                _ = task.ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
                throw new TimeoutException(string.Format("Execution didn't complete within the required maximum {0} seconds.", millisecondsTimeout / 1000));
            }
        }
    }
}
