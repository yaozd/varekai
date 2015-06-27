using NUnit.Framework;
using System.Linq;

namespace SampleLockingService.Tests
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
        public void Given_a_json_list_of_nodes_when_parsed_then_the_correct_number_of_locking_nodes_is_created() 
        {
            var list = JsonNodeList.GenerateLockingNodes();

            Assert.AreEqual(5, list.Count());
        }

        [Test]
        [TestCase(0, "localhost", 7001)]
        [TestCase(1, "localhost", 7002)]
        [TestCase(2, "localhost", 7003)]
        [TestCase(3, "localhost", 7004)]
        [TestCase(4, "localhost", 7005)]
        public void Given_a_json_list_of_nodes_when_parsed_then_the_nodes_parsed_have_the_correct_host_and_port(
            int index,
            string host,
            int port
        )
        {
            var list = JsonNodeList
                .GenerateLockingNodes()
                .ToArray();

            Assert.AreEqual(host, list[index].Host);
            Assert.AreEqual(port, list[index].Port);
        }
    }
}