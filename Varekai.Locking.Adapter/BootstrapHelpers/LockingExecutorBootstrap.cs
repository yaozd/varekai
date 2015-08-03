using System;
using System.Collections.Generic;
using Autofac;
using Varekai.Locker;
using Varekai.Utils.Logging;

namespace Varekai.Locking.Adapter.BootstrapHelpers
{
    public static class LockingExecutorBootstrap
    {
        public static ContainerBuilder RegisterLockingExecution(this ContainerBuilder builder)
        {
            builder
                .RegisterType<LockingServiceExecutor>()
                .As<ILockingServiceExecution>();

            return builder;
        }

        public static ContainerBuilder RegisterLockingAdapterDependencies(
            this ContainerBuilder builder,
            Func<IComponentContext, ILogger> loggerProvider,
            Func<long> timeProvider,
            Func<IEnumerable<LockingNode>> nodesProvider,
            Func<string> resourceToLockProvider)
        {
            builder
                .Register<Func<long>>(_ => () => timeProvider())
                .AsSelf();

            builder
                .Register(ctx => loggerProvider(ctx))
                .As<ILogger>();

            builder
                .Register(_ => nodesProvider())
                .As<IEnumerable<LockingNode>>()
                .SingleInstance();

            builder
                .Register(_ => LockId.CreateNewFor(resourceToLockProvider()))
                .AsSelf();

            return builder;
        }
    }
}

