using System.Collections.Concurrent;
using SuperSocket.SocketBase;

namespace OMKServer
{
    public class ClientSession : AppSession<ClientSession, OMKBinaryRequestInfo>
    {
        public int SessionIndex { get; private set; } = -1;
    }
}