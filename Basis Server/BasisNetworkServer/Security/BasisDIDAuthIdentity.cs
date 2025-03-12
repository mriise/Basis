using Basis.Contrib.Auth.DecentralizedIds;
using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
using Basis.Contrib.Crypto;
using Basis.Network.Core;
using Basis.Network.Server.Auth;
using BasisNetworkCore;
using BasisServerHandle;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using static Basis.Network.Core.Serializable.SerializableBasis;
using static SerializableBasis;
using Challenge = Basis.Contrib.Auth.DecentralizedIds.Challenge;
using CryptoRng = System.Security.Cryptography.RandomNumberGenerator;
namespace BasisDidLink
{
    public class BasisDIDAuthIdentity
    {
        public class DidAuthIdentity : IAuthIdentity
        {
            internal readonly DidAuthentication DidAuth;
            public ConcurrentDictionary<NetPeer, OnAuth> AuthIdentity = new ConcurrentDictionary<NetPeer, OnAuth>();
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
            public static string UnpackString(byte[] compressedBytes)
            {
               return Encoding.UTF8.GetString(compressedBytes, 0, compressedBytes.Length);
            }
            public struct OnAuth
            {
                public ReadyMessage ReadyMessage;
                public Challenge Challenge;
                public Did Did;
            }
            public void ProcessConnection(ConnectionRequest ConnectionRequest, NetPeer newPeer)
            {
                ushort PeerId = (ushort)newPeer.Id;

                BytesMessage authIdentityMessage = new BytesMessage();
                authIdentityMessage.Deserialize(ConnectionRequest.Data);

                string DeCompression = UnpackString(authIdentityMessage.bytes);
                Did playerDid = new Did(DeCompression);


                ReadyMessage readyMessage = ThreadSafeMessagePool<ReadyMessage>.Rent();
                readyMessage.Deserialize(ConnectionRequest.Data, false);
                if (readyMessage.WasDeserializedCorrectly())
                {
                    OnAuth OnAuth = new OnAuth
                    {
                        Did = playerDid,
                        Challenge = MakeChallenge(playerDid),
                        ReadyMessage = readyMessage
                    };
                    if (AuthIdentity.TryAdd(newPeer, OnAuth))
                    {
                        readyMessage.playerMetaDataMessage.playerUUID = playerDid.V;
                        BytesMessage NetworkMessage = new BytesMessage
                        {
                            bytes = OnAuth.Challenge.Nonce.V
                        };
                        NetDataWriter Writer = new NetDataWriter();
                        NetworkMessage.Serialize(Writer);
                        //request from the client its auth
                        newPeer.Send(Writer, BasisNetworkCommons.AuthIdentityMessage, DeliveryMethod.ReliableOrdered);
                    }
                    else
                    {
                        BasisServerHandleEvents.RejectWithReason(newPeer, "Payload Provided was invalid!");
                    }
                }
                else
                {
                    BasisServerHandleEvents.RejectWithReason(newPeer, "Payload Provided was invalid!");
                }
            }
            /// <summary>
            /// response from client with the challenge
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="newPeer"></param>
            private async void OnAuthReceived(NetPacketReader reader, NetPeer newPeer)
            {
                BytesMessage SignatureBytes = new BytesMessage();
                SignatureBytes.Deserialize(reader);
                BytesMessage FragmentBytes = new BytesMessage();
                FragmentBytes.Deserialize(reader);


                Signature Sig = new Signature(SignatureBytes.bytes);

                string FragmentAsString = UnpackString(FragmentBytes.bytes);
                DidUrlFragment Fragment = new DidUrlFragment(FragmentAsString);//where is this coming from?s

                Response response = new Response(Sig, Fragment);
                if (AuthIdentity.TryGetValue(newPeer, out OnAuth authIdentity))
                {
                    Challenge challenge = authIdentity.Challenge;
                    bool isAuthenticated = await RecvChallengeResponse(response, challenge);
                    if (isAuthenticated)
                    {
                        BasisServerHandleEvents.OnNetworkAccepted(newPeer, authIdentity.ReadyMessage, authIdentity.Did.V);
                    }
                    else
                    {
                        BasisServerHandleEvents.RejectWithReason(newPeer, "was unable to authenticate!");
                    }
                }
            }
            public Challenge MakeChallenge(Did ChallengingDID)
            {
                return DidAuth.MakeChallenge(ChallengingDID ?? throw new Exception("call RecvDid first"));
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
