using Autofac;
using Serilog;
using Topshelf;
using Topshelf.Autofac;
using Varekai.Locking.Adapter;

namespace SampleLockingService
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var container = Bootstrapp
                .WithContainerBuilder()
                .RegisterAllServiceDependencies()
                .RegisterLockingExecution()
                .CreateContainer();
            
            HostFactory.Run(ctx =>
                {
                    ctx.UseLinuxIfAvailable();
                    ctx.UseSerilog(container.Resolve<LoggerConfiguration>());
                    ctx.UseAutofacContainer(container);


                    ctx.Service<ILockingServiceExecution>(s =>
                        {
                            s.ConstructUsingAutofacContainer();

                            s.WhenStarted(lckService => lckService.LockedStart());
                            s.WhenStopped(lckService => lckService.ReleasedStop());
                        });
                    
                    ctx.RunAsLocalService();

                    ctx.SetDescription("Sample Locking Service");
                    ctx.SetDisplayName("Sample Locking Service");
                    ctx.SetServiceName("SampleLockingService");

                    ctx.EnableServiceRecovery(rc => rc.RestartService(0));
                });
        }
    }
}
