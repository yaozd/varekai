using Autofac;
using Varekai.Locking.Adapter;
using System;
using Varekai.Utils.Logging;
using Varekai.Utils.Logging.Implementations;

namespace SampleLockingService
{
    public static class Bootstrapp
    {
        public static ContainerBuilder WithContainerBuilder()
        {
            return new ContainerBuilder();
        }

        public static ContainerBuilder RegisterService(this ContainerBuilder builder)
        {
            builder
                .Register<Func<DateTime>>(ctx => () => DateTime.Now)
                .AsSelf();

            builder
                .Register(ctx => new Log4NetLogger("SampleLockingService"))
                .As<ILogger>();

            builder
                .RegisterType<SampleServiceImplementation>()
                .As<IServiceExecution>();
            
            return builder;
        }

        public static IContainer CreateContainer(this ContainerBuilder builder)
        {
            return builder.Build();
        }
    }
}

