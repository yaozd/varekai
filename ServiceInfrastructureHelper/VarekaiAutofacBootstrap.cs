using System;
using Autofac;
using Varekai.Locking.Adapter;
using Varekai.Locking.Adapter.BootstrapHelpers;
using Varekai.Utils;
using Varekai.Utils.Logging.Implementations;
using Varekai.Utils.Logging;

namespace ServiceInfrastructureHelper
{
    public static class VarekaiAutofacBootstrap
    {
        public static IContainer SetupVarekaiContainer(
            string applicationName,
            Func<IComponentContext, IServiceExecution> serviceFactory,
            string nodesConfigFilePath,
            string logsPath)
        {
            return WithContainerBuilder()
                .RegisterSerilogConfiguration(applicationName, logsPath)
                .RegisterLockingAdapterDependencies(
                    ctx => new SerilogLogger(ctx.Resolve<SerilogRollingFileConfiguration>()),
                    TimeUtils.MonotonicTimeTicksProvider(),
                    () => 
                        JsonFileReadUtil
                        .ReadJsonFromFile(nodesConfigFilePath)
                        .GenerateLockingNodes(operationTimeoutMillis:3000),
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

        static ContainerBuilder RegisterSerilogConfiguration(this ContainerBuilder builder, string applicationName, string logsPath)
        {
            builder
                .Register<SerilogRollingFileConfiguration>(
                    ctx => new SerilogRollingFileConfiguration(
                        logsPath + applicationName + "-{Date}.txt",
                        filesToKeep: 50,
                        logLevel: LogLevels.Information))
                .AsSelf();

            builder
                .Register<Serilog.LoggerConfiguration>(
                    ctx => SerilogLogger.CreateDefaultConfiguration(
                        ctx.Resolve<SerilogRollingFileConfiguration>()))
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