using Autofac;

namespace Varekai.Locking.Adapter
{
    public static class LockingExecutorBootstrapp
    {
        public static ContainerBuilder RegisterLockingExecution(this ContainerBuilder builder)
        {
            builder
                .RegisterType<LockingServiceExecutor>()
                .As<ILockingServiceExecution>();

            return builder;
        }
    }
}

