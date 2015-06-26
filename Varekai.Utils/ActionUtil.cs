using System;
using System.Threading;

namespace Varekai.Utils
{
    public static class ActionUtil
    {
        public static void Retry(Action toRetry
			, CancellationTokenSource cancellation
			, int? times = null
			, int intervalBetweenRetriesMillis = 1000
			, Action<Exception> onException = null)
        {
            long counter = 0;
            bool succeded = false;

            while (!succeded 
                && !cancellation.IsCancellationRequested
                && (!times.HasValue || times.Value <= counter))
            {

                try
                {
                    toRetry();
                    succeded = true;
                }
                catch (Exception ex)

                {
                    if (onException != null) onException(ex);

                    Thread.Sleep(intervalBetweenRetriesMillis);
                }

                counter++;
            }
        }
    }
}

