using System;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Basis.Contrib.Auth.DecentralizedIds;
using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
using Basis.Contrib.Crypto;
using Basis.Network.Core;
using Basis.Network.Server.Auth;
using BasisServerHandle;
using LiteNetLib;
using LiteNetLib.Utils;
using static Basis.Network.Core.Serializable.SerializableBasis;
using Challenge = Basis.Contrib.Auth.DecentralizedIds.Challenge;
using CryptoRng = System.Security.Cryptography.RandomNumberGenerator;
namespace BasisDidLink
{
    public class BasisDIDAuthIdentity
    {
        public class DidAuthIdentity : IAuthIdentity
        {
            internal readonly DidAuthentication DidAuth;
            public ConcurrentDictionary<NetPeer, Did> AuthIdentity = new ConcurrentDictionary<NetPeer, Did>();
            public ConcurrentDictionary<NetPeer, Nonce> Nonces = new ConcurrentDictionary<NetPeer, Nonce>();
            public DidAuthIdentity()
            {
                var rng = CryptoRng.Create();
                Config cfg = new Config { Rng = rng };
                DidAuth = new DidAuthentication(cfg);
                BasisServerHandleEvents.OnAuthReceived += OnAuthReceived;
            }
            public void DeInitalize()
            {
                BasisServerHandleEvents.OnAuthReceived -= OnAuthReceived;
            }
            public static string DecompressString(byte[] compressedBytes)
            {
                using (var inputStream = new MemoryStream(compressedBytes))
                using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                using (var outputStream = new MemoryStream())
                {
                    gzipStream.CopyTo(outputStream);
                    return Encoding.UTF8.GetString(outputStream.ToArray());
                }
            }
            private async void OnAuthReceived(NetPacketReader reader, NetPeer newPeer)
            {
                BytesMessage SignatureBytes = new BytesMessage();
                SignatureBytes.Deserialize(reader);
                Signature Sig = new Signature(SignatureBytes.bytes);

                BytesMessage NonceMessage = new BytesMessage();
                NonceMessage.Deserialize(reader);
                string FragmentAsString = DecompressString(NonceMessage.bytes);
                DidUrlFragment Fragment = new DidUrlFragment(FragmentAsString);//where is this coming from?s

                Response response = new Response(Sig, Fragment);
                if (AuthIdentity.TryGetValue(newPeer, out Did authIdentity))
                {
                    if (Nonces.TryGetValue(newPeer, out Nonce Nonce))
                    {
                        Challenge challenge = new Challenge(authIdentity, Nonce);
                        bool isAuthenticated = await RecvChallengeResponse(response, challenge);
                        if (isAuthenticated)
                        {
                            BasisServerHandleEvents.OnNetworkAccepted(newPeer, ConReq, UUID, HasAuthID);
                        }
                    }
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
            public async Task<bool> RecvChallengeResponse(Response response, Challenge Challenge)
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
