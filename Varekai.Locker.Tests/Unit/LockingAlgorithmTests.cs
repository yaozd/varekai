using System;
using System.Collections.Generic;
using NUnit.Framework;
using Varekai.Utils;

namespace Varekai.Locker.Tests.Unit
{
    [TestFixture]
    public class LockingAlgorithmTests
    {
        [Test]
        [Description(
            "GIVEN a lock expiration interval" +
            "WHEN an amount of time has passed" +
            "THEN the remaining validity of the lock is correct")]
        [TestCase(4000, 500, 3500)]
        [TestCase(500, 499.05, 0.95)]
        [TestCase(500, 500, 0)]
        [TestCase(0, 0, 0)]
        [TestCase(4000, 0, 4000)]
        [TestCase(400, 5000, 0)]
        [TestCase(500, 500.05, 0)]
        public void CalculateRemainingValidityTimeTests(long expirationTimeMillis, double durationMillis, double expectedRemainingValidity)
        {
            var lid = LockId.CreateNew("Test Resource", Guid.NewGuid(), expirationTimeMillis);

            var timeStart = TimeUtils.MonotonicTimeTicksProvider()();
            var timeEnd = timeStart + (long)(durationMillis * TimeSpan.TicksPerMillisecond);

            Assert.AreEqual(
                expectedRemainingValidity,
                lid.CalculateRemainingValidityTime(timeStart, timeEnd),
                0.000001
            );
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(4, 3)]
        [TestCase(6, 4)]
        [TestCase(5, 3)]
        [TestCase(7, 4)]
        public void CalculateQuorum(int nodesNumber, int expectedQuorum)
        {
            var nodes = new List<LockingNode>();

            for (var i = 0; i < nodesNumber; i++)
                nodes.Add(new LockingNode());

            Assert.AreEqual(
                expectedQuorum,
                nodes.CalculateQuorum()
            );
        }

        [Test]
        [TestCase(3000, 2000, false)]
        [TestCase(3000, 1000, true)]
        [TestCase(3000, 3000, false)]
        [TestCase(3000, 1970, true)]
        [TestCase(3000, 1971, false)]
        [TestCase(3000, 3001, false)]
        public void IsTimeLeftEnoughToUseTheLockTests(long expirationTimeMillis, double durationMillis, bool expectedTimeValidity)
        {
            var lid = LockId.CreateNew("Test Resource", Guid.NewGuid(), expirationTimeMillis);

            var timeStart = TimeUtils.MonotonicTimeTicksProvider()();
            var timeEnd = timeStart + (long)(durationMillis * TimeSpan.TicksPerMillisecond);

            Assert.AreEqual(
                expectedTimeValidity,
                LockingAlgorithm.IsTimeLeftEnoughToUseTheLock(timeStart, timeEnd, lid)
            );
        }
    }
}