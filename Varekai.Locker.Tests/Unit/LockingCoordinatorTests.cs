using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Varekai.Utils.Logging;

namespace Varekai.Locker.Tests.Unit
{
    [TestFixture]
    public class LockingCoordinatorTests
    {
        [Test]
        [TestCase("OK", true)]
        [TestCase("ok", true)]
        [TestCase("AA", false)]
        [TestCase(null, false)]
        public async Task TryAcquireLockTests(string acquireRsult, bool expectedResult)
        {
            var coordinator = LockingCoordinator.CreateNewForNodesWithClient(
                CreateNodes(),
                () => DateTime.Now,
                CreateClientFactory(
                    _ => Task.FromResult(acquireRsult),
                    _ => Task.FromResult("OK"),
                    _ => Task.FromResult("OK")
                ),
                Mock.Of<ILogger>());

            var id = LockId.CreateNewFor("resource");

            Assert.AreEqual(expectedResult, await coordinator.TryAcquireLock(id));
        }

        [Test]
        [TestCase("OK", true)]
        [TestCase("ok", true)]
        [TestCase("AA", false)]
        [TestCase(null, false)]
        public async Task TryConfirmLockTests(string confirmRsult, bool expectedResult)
        {
            var coordinator = LockingCoordinator.CreateNewForNodesWithClient(
                CreateNodes(),
                () => DateTime.Now,
                CreateClientFactory(
                    _ => Task.FromResult("OK"),
                    _ => Task.FromResult("OK"),
                    _ => Task.FromResult(confirmRsult)
                ),
                Mock.Of<ILogger>());

            var id = LockId.CreateNewFor("resource");

            Assert.AreEqual(expectedResult, await coordinator.TryConfirmTheLock(id));
        }

        [Test]
        [TestCase("OK", true)]
        [TestCase("ok", true)]
        [TestCase("AA", false)]
        [TestCase(null, false)]
        public async Task TryReleaseLockTests(string releaseRsult, bool expectedResult)
        {
            var coordinator = LockingCoordinator.CreateNewForNodesWithClient(
                CreateNodes(),
                () => DateTime.Now,
                CreateClientFactory(
                    _ => Task.FromResult("OK"),
                    _ => Task.FromResult(releaseRsult),
                    _ => Task.FromResult("OK")
                ),
                Mock.Of<ILogger>());

            var id = LockId.CreateNewFor("resource");

            Assert.AreEqual(expectedResult, await coordinator.TryReleaseTheLock(id));
        }

        [Test]
        [TestCase(1000)]
        [TestCase(100)]
        [TestCase(10)]
        [TestCase(1)]
        [Description(
            "GIVEN a lock coordinator" +
            "WHEN the time to set the lock in the redis client is bigger than the lock expiration" +
            "THEN the lock is not acquired")]
        public async Task Timeout(int lockExpirationTime)
        {
            var coordinator = LockingCoordinator.CreateNewForNodesWithClient(
                CreateNodes(),
                () => DateTime.Now,
                CreateClientFactory(
                    async lockId => 
                    {
                        await Task.Delay((int)lockId.ExpirationTimeMillis + 1);
                        return "OK";
                    },
                    _ => Task.FromResult("OK"),
                    _ => Task.FromResult("OK")
                ),
                Mock.Of<ILogger>());

            var id = LockId.CreateNew("resource", Guid.NewGuid(), lockExpirationTime);

            Assert.AreEqual(false, await coordinator.TryAcquireLock(id));
        }

        static Func<LockingNode, IRedisClient> CreateClientFactory(
            Func<LockId, Task<string>> setCalback,
            Func<LockId, Task<string>> releaseCalback,
            Func<LockId, Task<string>> confirmCalback)
        {
            var mockClient = new Mock<IRedisClient>();

            mockClient
                .Setup(cli => cli.Set(It.IsAny<LockId>()))
                .Returns<LockId>(setCalback);

            mockClient
                .Setup(cli => cli.Release(It.IsAny<LockId>()))
                .Returns<LockId>(releaseCalback);

            mockClient
                .Setup(cli => cli.Confirm(It.IsAny<LockId>()))
                .Returns<LockId>(confirmCalback);
            
            return node => mockClient.Object;
        }

        static IEnumerable<LockingNode> CreateNodes()
        {
            return new [] {
                LockingNode.CreateNew("localhost", 7001),
                LockingNode.CreateNew("localhost", 7002),
                LockingNode.CreateNew("localhost", 7003)
            };
        }
    }
}