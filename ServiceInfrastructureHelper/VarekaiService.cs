using Autofac;
using Serilog;
using Topshelf;
using Topshelf.Autofac;
using Topshelf.HostConfigurators;

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

            configurator.Service<IServiceOperation>(s =>
                {
                    s.ConstructUsingAutofacContainer();

                    s.WhenStarted(lckService => lckService.Start());
                    s.WhenStopped(lckService => lckService.Stop());
                });

            configurator.RunAsLocalService();

            configurator.SetDescription(description);
            configurator.SetDisplayName(description);
            configurator.SetServiceName(name);

            configurator.EnableServiceRecovery(rc => rc.RestartService(0));
        }
    }
}