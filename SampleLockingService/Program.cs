using Autofac;
using ServiceInfrastructureHelper;
using Topshelf;
using Varekai.Locking.Adapter.BootstrapHelpers;

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
            
            HostFactory.Run(ctx => ctx.SetupLockingService("HelloWorldVarekaiService", "Hello World Varekai Service", container));
        }
    }
}
