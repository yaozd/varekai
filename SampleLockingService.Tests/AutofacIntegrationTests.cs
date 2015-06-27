using System;
using Autofac;
using NUnit.Framework;
using Varekai.Locking.Adapter;
using Varekai.Utils.Logging;

namespace SampleLockingService.Tests
{
    [TestFixture]
    public class AutofacIntegrationTests
    {
        [Test]
        public void TheServiceContainerContainsARegistrationForATimeProvider()
        {
            var container = SetupContainer();

            Assert.IsNotNull(container.Resolve<Func<DateTime>>());
        }

        [Test]
        public void TheServiceContainerContainsARegistrationForAServiceExecutor()
        {
            var container = SetupContainer();

            Assert.IsNotNull(container.Resolve<IServiceExecution>());
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

        static IContainer SetupContainer()
        {
            return Bootstrapp
                .WithContainerBuilder()
                .RegisterAllServiceDependencies()
                .CreateContainer();
        }
    }
}

