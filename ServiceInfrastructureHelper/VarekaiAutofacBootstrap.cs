using System;
using System.Collections.Generic;
using Autofac;
using Varekai.Locker;
using Varekai.Locking.Adapter;
using Varekai.Utils;
using Varekai.Utils.Logging;
using Varekai.Utils.Logging.Implementations;

namespace ServiceInfrastructureHelper
{
    public static class VarekaiAutofacBootstrap
    {
        public static IContainer SetupVarekaiContainer(
            string applicationName,
            Func<IComponentContext, IServiceOperation> serviceFactory,
            string nodesConfigFilePath,
            string logsPath)
        {
            return WithContainerBuilder()
                .RegisterSerilogConfiguration(applicationName, logsPath)
                .RegisterLockingDependencies(nodesConfigFilePath, applicationName)
                .RegisterLockingEngine()
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

        static ContainerBuilder RegisterLockingDependencies(this ContainerBuilder builder, string nodesConfigFilePath, string applicationName)
        {
            builder
                .Register<Func<long>>(_ => TimeUtils.MonotonicTimeTicksProvider())
                .AsSelf();
            
            builder
                .Register(_ => JsonFileUtils
                    .ReadJsonFromFile(nodesConfigFilePath)
                    .GenerateLockingNodes(operationTimeoutMillis:3000))
                .As<IEnumerable<LockingNode>>()
                .SingleInstance();

            builder
                .Register(_ => LockId.CreateNewFor(applicationName))
                .AsSelf();

            return builder;
        }

        static ContainerBuilder RegisterLockingEngine(this ContainerBuilder builder)
        {
            builder
                .RegisterType<LockingEngine>()
                .AsSelf();

            return builder;
        }

        static ContainerBuilder RegisterSerilogConfiguration(this ContainerBuilder builder, string applicationName, string logsPath)
        {
            builder
                .Register<SerilogRollingFileConfiguration>(
                    ctx => new SerilogRollingFileConfiguration(
                        logsPath + applicationName + "-{Date}.txt",
                        filesToKeep: 50,
                        logLevel: LogLevels.Debug))
                .AsSelf();

            builder
                .Register<Serilog.LoggerConfiguration>(
                    ctx => SerilogLogger.CreateDefaultConfiguration(
                        ctx.Resolve<SerilogRollingFileConfiguration>()))
                .AsSelf();

            builder
                .RegisterType<SerilogLogger>()
                .As<ILogger>();

            return builder;
        }

        static ContainerBuilder RegisterService(this ContainerBuilder builder, Func<IComponentContext, IServiceOperation> serviceFactory)
        {
            builder
                .Register(serviceFactory)
                .As<IServiceOperation>();

            return builder;
        }
    }
}