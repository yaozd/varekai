using System;
using System.Collections.Generic;
using Autofac;
using Varekai.Locking.Adapter;
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

        public static ContainerBuilder RegisterAllServiceDependencies(this ContainerBuilder builder)
        {
            return builder
                .RegisterService()
                .RegisterSerilogConfiguration()
                .RegisterLockingAdapterDependencies();
        }

        public static ContainerBuilder RegisterService(this ContainerBuilder builder)
        {
            builder
                .RegisterType<SampleServiceImplementation>()
                .As<IServiceExecution>();
            
            return builder;
        }

        public static ContainerBuilder RegisterSerilogConfiguration(this ContainerBuilder builder)
        {
            builder
                .Register<SerilogRollingFileConfiguration>(
                    ctx => new SerilogRollingFileConfiguration("../../../../Logs/Varekai-Sample-{Date}.txt", filesToKeep:50))
                .AsSelf();

            builder
                .Register<Serilog.LoggerConfiguration>(
                    ctx => SerilogLogger.CreateConfiguration(ctx.Resolve<SerilogRollingFileConfiguration>()))
                .AsSelf();

            return builder;
        }

        public static ContainerBuilder RegisterLockingAdapterDependencies(this ContainerBuilder builder)
        {
            builder
                .Register<Func<DateTime>>(ctx => () => DateTime.Now)
                .AsSelf();

            builder
                .Register(ctx => new SerilogLogger(ctx.Resolve<SerilogRollingFileConfiguration>()))
                .As<ILogger>();

            builder
                .Register(ctx => new List<string>())
                .As<IEnumerable<string>>()
                .SingleInstance();

            return builder;
        }

        public static IContainer CreateContainer(this ContainerBuilder builder)
        {
            return builder.Build();
        }
    }
}

