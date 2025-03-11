using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BasisNetworkServer.Security
{
    public class BasisPlayerModeration
    {
        private readonly ConcurrentDictionary<IPAddress, Did> BannedIps = new();
        private readonly HashSet<Did> BannedDids = new();

        /// <summary>
        /// Bans a player based on their NetPeer and Decentralized ID.
        /// </summary>
        public void Ban(LiteNetLib.NetPeer peer, Did connDid, string reason)
        {
            if (peer == null || connDid == null)
                throw new ArgumentNullException("Peer or Did cannot be null.");

            if (IsBanned(peer.Address, connDid))
                return; // Already banned, no need to process again.

            BannedIps.TryAdd(peer.Address, connDid);
            BannedDids.Add(connDid);

            byte[] reasonBytes = Encoding.UTF8.GetBytes(reason);
            NetworkServer.server.DisconnectPeer(peer, reasonBytes);

            LogBan(peer.Address, connDid, reason);
        }

        /// <summary>
        /// Checks if an IP address or DID is banned.
        /// </summary>
        public bool IsBanned(IPAddress ip, Did did)
        {
            return BannedIps.ContainsKey(ip) || BannedDids.Contains(did);
        }

        /// <summary>
        /// Unbans a player by IP address.
        /// </summary>
        public bool Unban(IPAddress ip)
        {
            return BannedIps.TryRemove(ip, out _);
        }

        /// <summary>
        /// Unbans a player by DID.
        /// </summary>
        public bool Unban(Did did)
        {
            return BannedDids.Remove(did);
        }

        /// <summary>
        /// Logs ban details (Extend to use logging system).
        /// </summary>
        private void LogBan(IPAddress ip, Did did, string reason)
        {
            Console.WriteLine($"[BAN] IP: {ip}, DID: {did}, Reason: {reason}");
        }
    }
}
