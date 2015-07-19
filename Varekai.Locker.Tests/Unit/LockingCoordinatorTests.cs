using System;
using System.Collections.Generic;
using System.Linq;
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
                CreateMockRedisClient(
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
                CreateMockRedisClient(
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
                CreateMockRedisClient(
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
        public async Task LockAcquireTimeTest(int lockExpirationTime)
        {
            var coordinator = LockingCoordinator.CreateNewForNodesWithClient(
                CreateNodes(),
                () => DateTime.Now,
                CreateMockRedisClient(
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

        [Test]
        [Description(
            "GIVEN a lock coordinator" +
            "WHEN it is created using a static constructor" +
            "THEN all the redis clients of the cohordinator are connected")]
        public async Task CoordinatorConnectedAtCreation()
        {
            var connectCount = 0;
            var nodes = CreateNodes();

            var coordinator = LockingCoordinator.CreateNewForNodesWithClient(
                nodes,
                () => DateTime.Now,
                CreateMockRedisConnectingClient(() => Task.FromResult(connectCount++)),
                Mock.Of<ILogger>());
            
            Assert.AreEqual(connectCount, nodes.Count());
        }

        static Func<LockingNode, IRedisClient> CreateMockRedisClient(
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

        static Func<LockingNode, IRedisClient> CreateMockRedisConnectingClient(Func<Task> connectCallback)
        {
            var mockClient = new Mock<IRedisClient>();

            mockClient
                .Setup(cli => cli.TryConnect())
                .Returns(connectCallback);
            
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