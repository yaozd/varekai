using System;
using Autofac;
using Varekai.Locking.Adapter;
using Varekai.Locking.Adapter.BootstrapHelpers;
using Varekai.Utils;
using Varekai.Utils.Logging.Implementations;

namespace SampleLockingService
{
    public static class Bootstrapp
    {
        const string ApplicationPrefix = "Varekai_Sample_Service";
        
        public static ContainerBuilder WithContainerBuilder()
        {
            return new ContainerBuilder();
        }

        public static ContainerBuilder RegisterAllServiceDependencies(this ContainerBuilder builder)
        {
            return builder
                .RegisterService()
                .RegisterSerilogConfiguration()
                .RegisterSingleLockAdapterDependencies(
                    ctx => new SerilogLogger(ctx.Resolve<SerilogRollingFileConfiguration>()),
                    () => DateTime.Now,
                    () => 
                        JsonFileReadEx
                        .ReadJsonFromFile("../../RedisNodes.txt")
                        .GenerateLockingNodes(),
                    () => ApplicationPrefix);
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
                    ctx => new SerilogRollingFileConfiguration("" +
                        "../../../../Logs/" + ApplicationPrefix + "-{Date}.txt",
                        filesToKeep:50))
                .AsSelf();

            builder
                .Register<Serilog.LoggerConfiguration>(
                    ctx => SerilogLogger.CreateDefaultConfiguration(ctx.Resolve<SerilogRollingFileConfiguration>()))
                .AsSelf();

            return builder;
        }

        public static IContainer CreateContainer(this ContainerBuilder builder)
        {
            return builder.Build();
        }
    }
}