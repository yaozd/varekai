using System.Linq;
using NUnit.Framework;

namespace ServiceInfrastructureHelper.Tests
{
    [TestFixture]
    public class RedisNodesParserTests
    {
        const string JsonNodeList = @"[
            {
                address: 'localhost',
                port: 7001

            },
            {
                address: 'localhost',
                port: 7002

            },
            {
                address: 'localhost',
                port: 7003

            },
            {
                address: 'localhost',
                port: 7004

            },
            {
                address: 'localhost',
                port: 7005

            }
        ]";

        [Test]
        [Description(
            "GIVEN a json list of nodes as string" +
            "WHEN parsed" +
            "THEN the number of locking nodes created is correct")]
        public void GenerateNodesFromJson() 
        {
            var list = JsonNodeList.GenerateLockingNodes();

            Assert.AreEqual(5, list.Count());
        }

        [Test]
        [Description(
            "GIVEN a json list of nodes as string" +
            "WHEN parsed" +
            "THEN the host and port of the parsed nodes are correct")]
        [TestCase(0, "localhost", 7001)]
        [TestCase(1, "localhost", 7002)]
        [TestCase(2, "localhost", 7003)]
        [TestCase(3, "localhost", 7004)]
        [TestCase(4, "localhost", 7005)]
        public void GenerateTheCorrectHostAndPortForTheParsedNodes(
            int index,
            string host,
            int port)
        {
            var list = JsonNodeList
                .GenerateLockingNodes()
                .ToArray();

            Assert.AreEqual(host, list[index].Host);
            Assert.AreEqual(port, list[index].Port);
        }
    }
}