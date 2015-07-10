using Newtonsoft.Json;

namespace ServiceInfrastructureHelper
{
    public struct PhisicalNode
    {
        [JsonProperty("address")]
        public readonly string Address;

        [JsonProperty("port")]
        public readonly int Port;

        public PhisicalNode(string address, int port) : this()
        {
            Address = address;
            Port = port;
        }
    }
}

