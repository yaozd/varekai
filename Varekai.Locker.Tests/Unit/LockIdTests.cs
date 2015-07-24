using System;
using NUnit.Framework;
using Varekai.Utils;

namespace Varekai.Locker.Tests.Unit
{
    [TestFixture]
    public class LockIdTests
    {
        [Test]
        [TestCase("testResource", 5000)]
        public void GetSetScriptParameters(
            string resource,
            long lockExpirationTimeMillis)
        {
            var session = Guid.NewGuid();

            dynamic parameters = LockId
                .CreateNew(resource, session, lockExpirationTimeMillis)
                .GetSetScriptParameters();
            
            Assert.AreEqual(resource, parameters.resource);
            Assert.AreEqual(session.ToString(), parameters.session);
            Assert.AreEqual(lockExpirationTimeMillis, parameters.ttl);
        }

        [Test]
        [TestCase("testResource", 5000)]
        public void GetConfirmScriptParameters(
            string resource,
            long lockExpirationTimeMillis)
        {
            var session = Guid.NewGuid();

            dynamic parameters = LockId
                .CreateNew(resource, session, lockExpirationTimeMillis)
                .GetConfirmScriptParameters();
            
            Assert.AreEqual(resource, parameters.resource);
            Assert.AreEqual(session.ToString(), parameters.session);
            Assert.AreEqual(lockExpirationTimeMillis.ToCompleteSeconds(), parameters.ttl);
        }

        [Test]
        [TestCase("testResource", 5000)]
        public void GetReleaseScriptParameters(
            string resource,
            long lockExpirationTimeMillis)
        {
            var session = Guid.NewGuid();

            dynamic parameters = LockId
                .CreateNew(resource, session, lockExpirationTimeMillis)
                .GetReleaseScriptParameters();
            
            Assert.AreEqual(resource, parameters.resource);
            Assert.AreEqual(session.ToString(), parameters.session);
        }

        [Test]
        [TestCase("testResource", 5000)]
        public void GetSetCommand(
            string resource,
            long lockExpirationTimeMillis)
        {
            var session = Guid.NewGuid();

            var command = LockId
                .CreateNew(resource, session, lockExpirationTimeMillis)
                .GetSetCommand();

            Assert.AreEqual(6, command.Length);
            Assert.AreEqual("SET", command[0]);
            Assert.AreEqual(resource, command[1]);
            Assert.AreEqual(session, command[2]);
            Assert.AreEqual("NX", command[3]);
            Assert.AreEqual("PX", command[4]);
            Assert.AreEqual(lockExpirationTimeMillis, command[5]);
        }

        [Test]
        [TestCase("testResource", 5000)]
        public void GetConfirmCommand(
            string resource,
            long lockExpirationTimeMillis)
        {
            var session = Guid.NewGuid();

            var command = LockId
                .CreateNew(resource, session, lockExpirationTimeMillis)
                .GetConfirmCommand();

            Assert.AreEqual(5, command.Length);
            Assert.AreEqual("EVAL", command[0]);
            Assert.AreEqual(resource, command[2]);
            Assert.AreEqual(session, command[3]);
            Assert.AreEqual(lockExpirationTimeMillis.ToCompleteSeconds(), command[4]);
        }

        [Test]
        [TestCase("testResource", 5000)]
        public void GetReleaseCommand(
            string resource,
            long lockExpirationTimeMillis)
        {
            var session = Guid.NewGuid();

            var command = LockId
                .CreateNew(resource, session, lockExpirationTimeMillis)
                .GetReleaseCommand();

            Assert.AreEqual(4, command.Length);
            Assert.AreEqual("EVAL", command[0]);
            Assert.AreEqual(resource, command[2]);
            Assert.AreEqual(session, command[3]);
        }
    }
}

