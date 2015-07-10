using Autofac;
using Serilog;
using Topshelf;
using Topshelf.Autofac;
using Topshelf.HostConfigurators;
using Varekai.Locking.Adapter;

namespace ServiceInfrastructureHelper
{
    public static class VarekaiService
    {
        public static void SetupLockingService(
            this HostConfigurator configurator,
            string name,
            string description,
            IContainer autofacContainer)
        {
            configurator.UseLinuxIfAvailable();
            configurator.UseSerilog(autofacContainer.Resolve<LoggerConfiguration>());
            configurator.UseAutofacContainer(autofacContainer);

            configurator.Service<ILockingServiceExecution>(s =>
                {
                    s.ConstructUsingAutofacContainer();

                    s.WhenStarted(async lckService => await lckService.LockedStart());
                    s.WhenStopped(lckService => lckService.ReleasedStop());
                });

            configurator.RunAsLocalService();

            configurator.SetDescription(description);
            configurator.SetDisplayName(description);
            configurator.SetServiceName(name);

            configurator.EnableServiceRecovery(rc => rc.RestartService(0));
        }
    }
}