using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BasisNetworkServer.Security
{
    public class BasisPlayerModeration
    {
        private readonly ConcurrentDictionary<IPAddress, string> BannedIps = new ConcurrentDictionary<IPAddress, string>();
        private readonly HashSet<string> BannedDids = new HashSet<string>();

        /// <summary>
        /// Bans a player based on their NetPeer and Decentralized ID.
        /// </summary>
        public void Ban(LiteNetLib.NetPeer peer, string UUID, string reason)
        {
            if (peer == null || UUID == null)
                throw new ArgumentNullException("Peer or Did cannot be null.");

            if (IsBanned(peer.Address, UUID))
                return; // Already banned, no need to process again.

            BannedIps.TryAdd(peer.Address, UUID);
            BannedDids.Add(UUID);

            byte[] reasonBytes = Encoding.UTF8.GetBytes(reason);
            NetworkServer.server.DisconnectPeer(peer, reasonBytes);

            LogBan(peer.Address, UUID, reason);
        }

        /// <summary>
        /// Checks if an IP address or DID is banned.
        /// </summary>
        public bool IsBanned(IPAddress ip, string did)
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
        public bool Unban(string did)
        {
            return BannedDids.Remove(did);
        }

        /// <summary>
        /// Logs ban details (Extend to use logging system).
        /// </summary>
        private void LogBan(IPAddress ip, string did, string reason)
        {
            BNL.Log($"[BAN] IP: {ip}, DID: {did}, Reason: {reason}");
        }
    }
}
