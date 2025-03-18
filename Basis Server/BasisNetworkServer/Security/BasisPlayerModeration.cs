using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BasisNetworkServer.Security
{
    public static class BasisPlayerModeration
    {
        private static readonly ConcurrentDictionary<IPAddress, string> BannedIps = new ConcurrentDictionary<IPAddress, string>();
        private static readonly HashSet<string> BannedDids = new HashSet<string>();

        /// <summary>
        /// Bans a player by UUID (does not IP ban).
        /// </summary>
        public static string Ban(string UUID, string reason)
        {
            if (peer == null || UUID == null)
                throw new ArgumentNullException("Peer or UUID cannot be null.");

            if (IsBanned(peer.Address, UUID))
                return "Already banned!";

            BannedDids.Add(UUID); // Only banning the UUID.

            byte[] reasonBytes = Encoding.UTF8.GetBytes(reason);
            NetworkServer.server.DisconnectPeer(peer, reasonBytes);

            LogBan(peer.Address, UUID, reason);
            return "Banned successfully!";
        }

        /// <summary>
        /// Bans a player by both IP and UUID.
        /// </summary>
        public static string IpBan(string UUID, string reason)
        {
            if (peer == null || UUID == null)
                throw new ArgumentNullException("Peer or UUID cannot be null.");

            if (IsBanned(peer.Address, UUID))
                return "Already banned!";

            BannedIps.TryAdd(peer.Address, UUID);
            BannedDids.Add(UUID);

            byte[] reasonBytes = Encoding.UTF8.GetBytes(reason);
            NetworkServer.server.DisconnectPeer(peer, reasonBytes);

            LogBan(peer.Address, UUID, reason);
            return "IP and UUID banned successfully!";
        }

        /// <summary>
        /// Kicks a player without banning.
        /// </summary>
        public static string Kick(string UUID, string reason)
        {
            if (peer == null)
            {
                throw new ArgumentNullException("Peer cannot be null.");
            }
            byte[] reasonBytes = Encoding.UTF8.GetBytes(reason);
            NetworkServer.server.DisconnectPeer(peer, reasonBytes);

            return "Player kicked.";
        }

        /// <summary>
        /// Checks if an IP address or UUID is banned.
        /// </summary>
        public static bool IsBanned(IPAddress ip, string did)
        {
            return BannedIps.ContainsKey(ip) || BannedDids.Contains(did);
        }

        /// <summary>
        /// Unbans a player by IP.
        /// </summary>
        public static bool Unban(IPAddress ip)
        {
            return BannedIps.TryRemove(ip, out _);
        }

        /// <summary>
        /// Unbans a player by UUID.
        /// </summary>
        public static bool Unban(string did)
        {
            return BannedDids.Remove(did);
        }

        /// <summary>
        /// Logs ban details.
        /// </summary>
        private static void LogBan(IPAddress ip, string did, string reason)
        {
            BNL.Log($"[BAN] IP: {ip}, DID: {did}, Reason: {reason}");
        }
    }
}
