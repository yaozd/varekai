using System;
using Autofac;
using Moq;
using NUnit.Framework;
using Varekai.Locker;
using Varekai.Locking.Adapter;
using Varekai.Utils.Logging;

namespace ServiceInfrastructureHelper.Tests
{
    [TestFixture]
    public class AutofacIntegrationTests
    {
        [Test]
        public void TheServiceContainerContainsARegistrationForATimeProvider()
        {
            var container = SetupContainer();

            Assert.IsNotNull(container.Resolve<Func<long>>());
        }

        [Test]
        public void TheServiceContainerContainsARegistrationForAServiceExecutor()
        {
            var container = SetupContainer();

            Assert.IsNotNull(container.Resolve<IServiceOperation>());
        }

        [Test]
        public void TheServiceContainerContainsARegistrationForAnILogger()
        {
            var container = SetupContainer();

            Assert.IsNotNull(container.Resolve<ILogger>());
        }

        [Test]
        public void TheServiceContainerRegistersASerilogConfiguration()
        {
            var container = SetupContainer();

            Assert.IsNotNull(container.Resolve<Serilog.LoggerConfiguration>());
        }

        [Test]
        public void TheServiceContainerRegistersALockingEngine()
        {
            var container = SetupContainer();

            Assert.IsNotNull(container.Resolve<LockingEngine>());
        }

        static IContainer SetupContainer()
        {
            return
                VarekaiAutofacBootstrap.SetupVarekaiContainer(
                    "TestSetup",
                    _ => Mock.Of<IServiceOperation>(),
                    "../../TestRedisNodesList.txt",
                    "TestLogsPath");
        }
    }
}