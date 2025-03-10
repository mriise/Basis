using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace BasisNetworkServer.Security
{
    public class BasisPlayerModeration
    {
        public ConcurrentDictionary<IPAddress, Did> BannedIps = new ConcurrentDictionary<IPAddress, Did>();
        public HashSet<Did> BannedDids = new();
        public void Ban(LiteNetLib.NetPeer Peer)
        {
            BannedIps.TryAdd(Peer.Address);
            NetworkServer.server.DisconnectPeerForce(Peer);
            BannedDids.Add(connDid);
        }
    }
}
