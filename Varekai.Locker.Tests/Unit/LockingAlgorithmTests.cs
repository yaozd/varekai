using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Varekai.Locker.Tests.Unit
{
    [TestFixture]
    class LockingAlgorithmTests
    {
        [Test]
        [TestCase(4000, 500, 3500)]
        [TestCase(500, 500, 0)]
        [TestCase(0, 0, 0)]
        [TestCase(4000, 0, 4000)]
        public void CalculateRemainingValidityTimeTests(long expirationTimeMillis, long durationMillis, double expectedRemainingValidity)
        {
            var lid = LockId.CreateNew("Test Resource", Guid.NewGuid(), expirationTimeMillis);

            var timeStart = DateTime.UtcNow;

            Assert.AreEqual(
                expectedRemainingValidity,
                lid.CalculateRemainingValidityTime(timeStart, timeStart.Add(TimeSpan.FromMilliseconds(durationMillis)))
            );
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(4, 3)]
        [TestCase(6, 4)]
        [TestCase(5, 3)]
        [TestCase(7, 4)]
        public void CalculateRemainingValidityTimeTests(int nodesNumber, int expectedQuorum)
        {
            var nodes = new List<LockingNode>();

            for (var i = 0; i < nodesNumber; i++)
                nodes.Add(new LockingNode());

            Assert.AreEqual(
                expectedQuorum,
                nodes.CalculateQuorum()
            );
        }
    }
}