using Newtonsoft.Json;

namespace SampleLockingService
{
    public struct RedisNode
    {
        [JsonProperty("address")]
        public readonly string Address;

        [JsonProperty("port")]
        public readonly int Port;

        public RedisNode(string address, int port) : this()
        {
            Address = address;
            Port = port;
        }
    }
}

