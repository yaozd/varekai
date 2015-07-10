using Autofac;
using ServiceInfrastructureHelper;
using Topshelf;
using Varekai.Utils.Logging;

namespace SampleLockingService
{
    class MainClass
    {
        const string ApplicationPrefix = "Varekai_Hello_World_Service";
        const string PhisicalNodesConfigFile = "../../RedisNodes.txt";
        const string LogsPath = "../../../../Logs/";

        public static void Main(string[] args)
        {
            var container = 
                VarekaAutofacBootstrap.SetupVarekaiContainer(
                    ApplicationPrefix,
                    ctx => new HelloWorldService(ctx.Resolve<ILogger>()),
                    PhisicalNodesConfigFile,
                    LogsPath
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
