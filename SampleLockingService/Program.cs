using Autofac;
using ServiceInfrastructureHelper;
using Topshelf;
using Varekai.Utils.Logging;

namespace SampleLockingService
{
    class MainClass
    {
        const string ApplicationPrefix = "Varekai_Sample_Service";

        public static void Main(string[] args)
        {
            var container = 
                VarekaAutofacBootstrap.SetupVarekaiContainer(
                    ApplicationPrefix,
                    ctx => new SampleServiceImplementation(ctx.Resolve<ILogger>())
                );

            HostFactory
                .Run(
                    ctx => ctx.SetupLockingService(
                        "HelloWorldVarekaiService",
                        "Hello World Varekai Service",
                        container));
        }
    }
}
