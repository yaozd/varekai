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
    }
}

