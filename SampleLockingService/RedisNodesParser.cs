using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Varekai.Locker;

namespace SampleLockingService
{
    public static class RedisNodesParser
    {
        public static IEnumerable<LockingNode> GenerateLockingNodes(this string jsonNodesList)
        {
            return JsonConvert
                .DeserializeObject<List<RedisNode>>(jsonNodesList)
                .Select(node => LockingNode.CreateNew(node.Address, node.Port));
        }
    }
}

