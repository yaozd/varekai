using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Varekai.Locker;

namespace ServiceInfrastructureHelper
{
    public static class NodesConfiguration
    {
        public static IEnumerable<LockingNode> GenerateLockingNodes(this string jsonNodesList)
        {
            return JsonConvert
                .DeserializeObject<List<PhisicalNode>>(jsonNodesList)
                .Select(node => LockingNode.CreateNew(node.Address, node.Port));
        }
    }
}

