#nullable enable
namespace BasisDidLink
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Threading.Tasks;
    using Basis.Contrib.Auth.DecentralizedIds;
    using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
    using Basis.Contrib.Crypto;
    using Basis.Network.Core.Serializable;
    using Basis.Network.Server.Auth;
    using LiteNetLib;
    using CryptoRng = System.Security.Cryptography.RandomNumberGenerator;
    public class BasisDIDAuthIdentity
    {
        public static (PubKey, PrivKey) RandomKeyPair(CryptoRng rng)
        {
            var privKeyBytes = new byte[Ed25519.PrivkeySize];
            rng.GetBytes(privKeyBytes);
            var privKey = new PrivKey(privKeyBytes);
            var pubKey = Ed25519.ConvertPrivkeyToPubkey(privKey) ?? throw new Exception("privkey was invalid");
            return (pubKey, privKey);
        }
        public static void ClientKeyCreation()
        {
            // Client
            CryptoRng rng = CryptoRng.Create();
            (PubKey pubKey, PrivKey privKey) = RandomKeyPair(rng);
            Basis.Contrib.Auth.DecentralizedIds.Newtypes.Did playerDid = DidKeyResolver.EncodePubkeyAsDid(pubKey);
            IPAddress playerIp = IPAddress.Loopback;
        }
        public async Task FullProcess()
        {
            // Client
            var rng = CryptoRng.Create();
            var (pubKey, privKey) = RandomKeyPair(rng);
            var playerDid = DidKeyResolver.EncodePubkeyAsDid(pubKey);
            var playerIp = IPAddress.Loopback;

            // Server
            var cfg = new Config { Rng = rng };
            Server server = new(didAuth: new DidAuthentication(cfg));
            ConnectionState conn = server.OnConnection(playerIp);
            Debug.Assert(conn.RecvDid(playerDid));
            Basis.Contrib.Auth.DecentralizedIds.Challenge challenge = conn.SendChallenge();

            // Client
            var payloadToSign = new Payload(challenge.Nonce.V);
            Debug.Assert(
                Ed25519.Sign(privKey, payloadToSign, out Signature? sig),
                "signing with a valid privkey should always succeed"
            );
            Debug.Assert(
                sig is not null,
                "signing with a valid privkey should always succeed"
            );
            Debug.Assert(
                Ed25519.Verify(pubKey, sig, payloadToSign),
                "sanity check: verifying sig"
            );
            // for simplicity, use an empty fragment since the client only has one pubkey
            var response = new Response(sig, new DidUrlFragment(string.Empty));

            // Server
            var isAuthenticated = await conn.RecvChallengeResponse(response);
            Debug.Assert(isAuthenticated, "the response should have been valid");

            // Next we ban the player
            server.Ban(playerIp);

            // Client tries to connect again, but from a different IP
            var bannedConn = server.OnConnection(
                new IPAddress(new byte[] { 192, 168, 1, 1 })
            );
            // Connection terminated when DID matches ban list
            Debug.Assert(!bannedConn.RecvDid(playerDid));
        }
        class ConnectionState
        {
            readonly Server Server;
            Did? Did;
            Basis.Contrib.Auth.DecentralizedIds.Challenge? Challenge = null;

            public ConnectionState(Server server)
            {
                Server = server;
            }

            public Did? Player => Did;

            /// Returns false if connection should be terminated
            public bool RecvDid(Did playerDid)
            {
                Did = playerDid;
                return !Server.BannedDids.Contains(playerDid);
            }

            public Basis.Contrib.Auth.DecentralizedIds.Challenge SendChallenge()
            {
                Challenge = Server.DidAuth.MakeChallenge(
                    Did ?? throw new Exception("call RecvDid first")
                );
                return Challenge;
            }

            /// Returns false if connection should be terminated
            public async Task<bool> RecvChallengeResponse(Response response)
            {
                if (!response.DidUrlFragment.V.Equals(string.Empty))
                {
                    throw new Exception("multiple pubkeys not yet supported");
                }
                var challenge =
                    Challenge ?? throw new Exception("call SendChallenge first");
                var result = await Server.DidAuth.VerifyResponse(response, challenge);
                return result.IsOk;
            }
        }
        class Server
        {
            internal readonly DidAuthentication DidAuth;
            readonly Dictionary<IPAddress, ConnectionState> Connections = new();

            public Server(DidAuthentication didAuth)
            {
                DidAuth = didAuth;
            }

            /// Returns a challenge that is sent to player, or else null if player is banned.
            public ConnectionState OnConnection(IPAddress remoteAddr)
            {
                var connectionState = new ConnectionState(this);
                Connections.Add(remoteAddr, connectionState);
                return connectionState;
            }
        }
        public class DidAuthIdentity : IAuthIdentity
        {
            public ConcurrentDictionary<NetPeer, Did> AuthIdentity = new ConcurrentDictionary<NetPeer, Did>();
            public DidAuthIdentity()
            {

            }

            public bool IsUserIdentifiable(SerializableBasis.BytesMessage msg, NetPeer NetPeer, out string UUID)
            {
                AuthIdentity.TryAdd(NetPeer,);
                UUID = "";
                return true;
            }
        }
    }
}
