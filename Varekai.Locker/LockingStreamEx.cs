using System;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace Varekai.Locker
{
    public static class LockingStreamEx
    {
        public static IDisposable CreateLockingStream(this LockingEngine locker, Action<object> onEvent)
        {
            return
                locker
                .CreateStream()
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .ObserveOn(CurrentThreadScheduler.Instance)
                .Subscribe(onEvent);
        }
    }
}

