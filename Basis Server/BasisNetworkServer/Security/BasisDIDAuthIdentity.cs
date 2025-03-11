using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Basis.Contrib.Auth.DecentralizedIds;
using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
using Basis.Contrib.Crypto;
using Basis.Network.Core;
using Basis.Network.Server.Auth;
using BasisServerHandle;
using LiteNetLib;
using LiteNetLib.Utils;
using Org.BouncyCastle.Asn1.Cmp;
using static Basis.Network.Core.Serializable.SerializableBasis;
using Challenge = Basis.Contrib.Auth.DecentralizedIds.Challenge;
using CryptoRng = System.Security.Cryptography.RandomNumberGenerator;
namespace BasisDidLink
{
    public class BasisDIDAuthIdentity
    {
        /*
        // Server 
        RandomNumberGenerator ServerRNG =
        Config cfg = new Config { Rng = new RandomNumberGenerator() };
        Server server = new(didAuth: new DidAuthentication(cfg));
        ConnectionState conn = server.OnConnection(playerIp);
      // Debug.Assert(conn.RecvDid(playerDid));
        Basis.Contrib.Auth.DecentralizedIds.Challenge challenge = DidAuthIdentity.SendChallenge(playerDid);

        // payload from server to client
        Payload payloadToSign = new Payload(challenge.Nonce.V);
      //  Debug.Assert(Ed25519.Sign(privKey, payloadToSign, out Signature? sig), "signing with a valid privkey should always succeed");
      //  Debug.Assert(sig is not null, "signing with a valid privkey should always succeed");
      //  Debug.Assert(Ed25519.Verify(pubKey, sig, payloadToSign), "sanity check: verifying sig");
        // for simplicity, use an empty fragment since the client only has one pubkey

        //payload from client to server
        Response response = new Response(sig, new DidUrlFragment(string.Empty));

        // Server
        bool isAuthenticated = await DidAuthIdentity.RecvChallengeResponse(response,challenge);
        */
        public class DidAuthIdentity : IAuthIdentity
        {
            internal readonly DidAuthentication DidAuth;
            public ConcurrentDictionary<NetPeer, Did> AuthIdentity = new ConcurrentDictionary<NetPeer, Did>();
            public DidAuthIdentity()
            {
                var rng = CryptoRng.Create();
                Config cfg = new Config { Rng = rng };
                DidAuth = new DidAuthentication(cfg);
                BasisServerHandleEvents.OnAuthReceived += OnAuthReceived;
            }
            /// <summary>
            /// in this case we use this to get the signed challenge
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="peer"></param>
            private async void OnAuthReceived(NetPacketReader reader, NetPeer newPeer)
            {
                BytesMessage BytesMessage = new BytesMessage();
                BytesMessage.Deserialize(reader);
                Signature Sig = new Signature(BytesMessage.bytes);
                DidUrlFragment Fragment = new DidUrlFragment("");//where is this coming from?
                Response response = new Response(Sig, Fragment);
                if (AuthIdentity.TryGetValue(newPeer, out var authIdentity))
                {
                    BytesMessage NonceMessage = new BytesMessage();
                    NonceMessage.Deserialize(reader);
                    Nonce Nonce = new Nonce(NonceMessage.bytes);//where is this conig from?
                    Challenge challenge = new Challenge(authIdentity, Nonce);
                    bool isAuthenticated = await RecvChallengeResponse(response, challenge);
                }
            }

            public void IsUserIdentifiable(BytesMessage msg, NetPeer newPeer, out string UUID)
            {
                PubKey Key = new PubKey(msg.bytes);
                Did playerDid = DidKeyResolver.EncodePubkeyAsDid(Key);

                if (AuthIdentity.TryAdd(newPeer, playerDid))
                {
                     UUID = playerDid.V;
                    Challenge challenge = SendChallenge(playerDid);
                    BytesMessage NetworkMessage = new BytesMessage();
                    NetworkMessage.bytes = challenge.Nonce.V;
                    NetDataWriter Writer = new NetDataWriter();
                    NetworkMessage.Serialize(Writer);
                    newPeer.Send(Writer, BasisNetworkCommons.AuthIdentityMessage, DeliveryMethod.ReliableOrdered);
                }
                else
                {
                    UUID = "ERROR";
                }
            }
            public Challenge SendChallenge(Did Did)
            {
                return DidAuth.MakeChallenge(Did ?? throw new Exception("call RecvDid first"));
            }

            /// Returns false if connection should be terminated
            public async Task<bool> RecvChallengeResponse(Response response,Challenge Challenge)
            {
                if (!response.DidUrlFragment.V.Equals(string.Empty))
                {
                    throw new Exception("multiple pubkeys not yet supported");
                }
                var challenge = Challenge ?? throw new Exception("call SendChallenge first");
                var result = await DidAuth.VerifyResponse(response, challenge);
                return result.IsOk;
            }
        }
    }
}
