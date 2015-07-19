using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Varekai.Utils
{
    public static class TaskUtils
    {
        public static async Task SilentlyCanceledDelay(int waitInterval, CancellationToken cancellation)
        {
            try
            {
                await Task.Delay(waitInterval, cancellation);
            }
            catch (TaskCanceledException) { /*ignore*/ }
        }

        public static async Task<T[]> SilentlyCanceledWhenAll<T>(IEnumerable<Task<T>> toWait)
        {
            try
            {
                return await Task.WhenAll(toWait);
            }
            catch (TaskCanceledException) { /*ignore*/ }

            return new T[]{ };
        }

        public static async Task SilentlyCanceledWhenAll(IEnumerable<Task> toWait)
        {
            try
            {
                await Task.WhenAll(toWait);
            }
            catch (TaskCanceledException) { /*ignore*/ }
        }
    }
}