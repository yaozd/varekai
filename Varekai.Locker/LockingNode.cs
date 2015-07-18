namespace Varekai.Locker
{
    public struct LockingNode
    {
        public readonly string Host;
        public readonly long Port;

        public readonly long ConnectTimeoutMillis;
        public readonly long SyncOperationsTimeoutMillis;

        LockingNode(
            string host,
            long port,
            long operationTimeoutMillis = 50,
            long connectTimeoutMillis = 500) : this()
        {
            Host = host;
            Port = port;
            ConnectTimeoutMillis = connectTimeoutMillis;
            SyncOperationsTimeoutMillis = operationTimeoutMillis;
        }

        public static LockingNode CreateNew(string host, long port)
        {
            return new LockingNode(host, port);
        }

        public static LockingNode CreateNewWithOperationTimeout(string host, long port, long operationTimeoutMillis)
        {
            return new LockingNode(host, port, operationTimeoutMillis);
        }

        public LockingNode ChangeHost(string host)
        {
            return new LockingNode(host, Port);
        }

        public LockingNode ChangePort(long port)
        {
            return new LockingNode(Host, port);
        }

        public string GetNodeName()
        {
            return string.Format("{0}:{1}", Host, Port);
        }
    }
}