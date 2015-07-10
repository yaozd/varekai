using System;
using Autofac;
using Varekai.Locking.Adapter;
using Varekai.Locking.Adapter.BootstrapHelpers;
using Varekai.Utils;
using Varekai.Utils.Logging.Implementations;

namespace ServiceInfrastructureHelper
{
    public static class VarekaAutofacBootstrap
    {
        public static IContainer SetupVarekaiContainer(string applicationName, Func<IComponentContext, IServiceExecution> serviceFactory)
        {
            return WithContainerBuilder()
                .RegisterSerilogConfiguration(applicationName)
                .RegisterSingleLockAdapterDependencies(
                    ctx => new SerilogLogger(ctx.Resolve<SerilogRollingFileConfiguration>()),
                    () => DateTime.Now,
                    () => 
                        JsonFileReadEx
                        .ReadJsonFromFile("../../RedisNodes.txt")
                        .GenerateLockingNodes(),
                    () => applicationName)
                .RegisterLockingExecution()
                .RegisterService(serviceFactory)
                .CreateContainer();
        }

        static ContainerBuilder WithContainerBuilder()
        {
            return new ContainerBuilder();
        }

        static IContainer CreateContainer(this ContainerBuilder builder)
        {
            return builder.Build();
        }

        static ContainerBuilder RegisterSerilogConfiguration(this ContainerBuilder builder, string applicationName)
        {
            builder
                .Register<SerilogRollingFileConfiguration>(
                    ctx => new SerilogRollingFileConfiguration("" +
                        "../../../../Logs/" + applicationName + "-{Date}.txt",
                        filesToKeep:50))
                .AsSelf();

            builder
                .Register<Serilog.LoggerConfiguration>(
                    ctx => SerilogLogger.CreateDefaultConfiguration(ctx.Resolve<SerilogRollingFileConfiguration>()))
                .AsSelf();

            return builder;
        }

        static ContainerBuilder RegisterService(this ContainerBuilder builder, Func<IComponentContext, IServiceExecution> serviceFactory)
        {
            builder
                .Register(ctx => serviceFactory(ctx))
                .As<IServiceExecution>();

            return builder;
        }
    }
}

