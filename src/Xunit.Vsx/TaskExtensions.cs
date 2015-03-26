using System;
using System.Threading.Tasks;

namespace Xunit
{
	static class TaskExtensions
	{
		public static async Task<T> TimeoutAfter<T> (this Task<T> task, int millisecondsTimeout)
		{
			if (task == await Task.WhenAny (task, Task.Delay(millisecondsTimeout)))
				return await task;
			else
				throw new TimeoutException (string.Format("Task didn't complete within the required maximum {0} seconds.", millisecondsTimeout / 1000));
		}
	}
}
