using Basis.Contrib.Auth.DecentralizedIds;
using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
using Basis.Contrib.Crypto;
using Basis.Network.Core;
using Basis.Network.Server.Auth;
using BasisNetworkServer.Security;
using BasisServerHandle;
using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static Basis.Network.Core.Serializable.SerializableBasis;
using static SerializableBasis;
using Challenge = Basis.Contrib.Auth.DecentralizedIds.Challenge;
using CryptoRng = System.Security.Cryptography.RandomNumberGenerator;

namespace BasisDidLink
{
    public class BasisDIDAuthIdentity : IAuthIdentity
    {
        internal readonly DidAuthentication DidAuth;
        public ConcurrentDictionary<NetPeer, OnAuth> AuthIdentity = new ConcurrentDictionary<NetPeer, OnAuth>();
        private readonly ConcurrentDictionary<NetPeer, CancellationTokenSource> _timeouts = new ConcurrentDictionary<NetPeer, CancellationTokenSource>();
        public List<string> Admins = new List<string>();
        public const string FilePath = "admins.xml";
        public BasisDIDAuthIdentity()
        {
            Admins = LoadAdmins(FilePath).ToList();
            CryptoRng rng = CryptoRng.Create();
            Config cfg = new Config { Rng = rng };
            DidAuth = new DidAuthentication(cfg);
            BasisServerHandleEvents.OnAuthReceived += OnAuthReceived;
            BNL.Log("DidAuthIdentity initialized.");

           // Admins.Add("did:key:z6Mkt4omnWQ1YTPCCkpfocdXn8X25sVLhwdsgXzfpGgqPyYc");
          //  SaveAdmins(Admins.ToArray(), FilePath);
        }

        public void DeInitalize()
        {
            BasisServerHandleEvents.OnAuthReceived -= OnAuthReceived;
            BNL.Log("DidAuthIdentity deinitialized.");
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
        public int CheckForDuplicates(Did Did)
        {
            int Count = 0;
            foreach (var key in AuthIdentity.Values)
            {
                if (key.Did.V == Did.V)
                {
                    Count++;
                }
            }
            return Count;
        }

        public void ProcessConnection(Configuration Configuration, ConnectionRequest ConnectionRequest, NetPeer newPeer)
        {
            try
            {
                BNL.Log($"Processing connection from peer {newPeer.Id}.");
                ReadyMessage readyMessage = new ReadyMessage();
                readyMessage.Deserialize(ConnectionRequest.Data, false);

                if (readyMessage.WasDeserializedCorrectly())
                {
                    string UUID = readyMessage.playerMetaDataMessage.playerUUID;
                    Did playerDid = new Did(UUID);
                    if (BasisPlayerModeration.IsBanned(UUID))
                    {
                        if (BasisPlayerModeration.GetBannedReason(UUID, out string Reason))
                        {
                            BasisServerHandleEvents.RejectWithReason(newPeer, "Banned User!  Reason " + Reason);

                        }
                        else
                        {
                            BasisServerHandleEvents.RejectWithReason(newPeer, " Banned User!");
                        }
                        return;
                    }
                    if (Configuration.HowManyDuplicateAuthCanExist <= CheckForDuplicates(playerDid))
                    {
                        BasisServerHandleEvents.RejectWithReason(newPeer, "To Many Auths From this DID!");
                        return;
                    }

                    OnAuth OnAuth = new OnAuth
                    {
                        Did = playerDid,
                        Challenge = MakeChallenge(playerDid),
                        ReadyMessage = readyMessage
                    };

                    if (AuthIdentity.TryAdd(newPeer, OnAuth))
                    {
                        readyMessage.playerMetaDataMessage.playerUUID = playerDid.V;
                        BytesMessage NetworkMessage = new BytesMessage { bytes = OnAuth.Challenge.Nonce.V };
                        NetDataWriter Writer = new NetDataWriter();
                        NetworkMessage.Serialize(Writer);
                        newPeer.Send(Writer, BasisNetworkCommons.AuthIdentityMessage, DeliveryMethod.ReliableOrdered);

                        var cts = new CancellationTokenSource();
                        _timeouts[newPeer] = cts;

                        Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(NetworkServer.Configuration.AuthValidationTimeOutMiliseconds, cts.Token);
                                if (!_timeouts.ContainsKey(newPeer)) return;
                                AuthIdentity.TryRemove(newPeer, out _);
                                _timeouts.TryRemove(newPeer, out _);
                                BNL.Log($"Authentication timeout for {UUID}.");
                                BasisServerHandleEvents.RejectWithReason(newPeer, "Authentication timeout");
                                newPeer.Disconnect();
                            }
                            catch (TaskCanceledException) { }
                        });
                    }
                    else
                    {
                        BasisServerHandleEvents.RejectWithReason(newPeer, "Payload Provided was invalid! potential Duplication");
                    }
                }
                else
                {
                    BasisServerHandleEvents.RejectWithReason(newPeer, "Invalid ReadyMessage received.");
                }
            }
            catch (Exception e)
            {
                BNL.Log($"Error processing connection: {e.Message} {e.StackTrace}");
                BasisServerHandleEvents.RejectWithReason(newPeer, $"{e.Message} {e.StackTrace}");
            }
        }

        private async void OnAuthReceived(NetPacketReader reader, NetPeer newPeer)
        {
            try
            {
                BNL.Log($"Authentication response received from {newPeer.Id}.");
                if (_timeouts.TryRemove(newPeer, out var cts))
                {
                    cts.Cancel();
                }

                BytesMessage SignatureBytes = new BytesMessage();
                SignatureBytes.Deserialize(reader);
                BytesMessage FragmentBytes = new BytesMessage();
                FragmentBytes.Deserialize(reader);

                Signature Sig = new Signature(SignatureBytes.bytes);
                string FragmentAsString = UnpackString(FragmentBytes.bytes);
                DidUrlFragment Fragment = new DidUrlFragment(FragmentAsString);
                Response response = new Response(Sig, Fragment);

                if (AuthIdentity.TryGetValue(newPeer, out OnAuth authIdentity))
                {
                    BNL.Log($"Verifying authentication response for {authIdentity.Did.V}.");
                    Challenge challenge = authIdentity.Challenge;
                    bool isAuthenticated = await RecvChallengeResponse(response, challenge);

                    if (isAuthenticated)
                    {
                        BNL.Log($"Authentication successful for {authIdentity.Did.V}.");
                        BasisServerHandleEvents.OnNetworkAccepted(newPeer, authIdentity.ReadyMessage, authIdentity.Did.V);
                    }
                    else
                    {
                        BNL.LogError($"Authentication failed for {authIdentity.Did.V}.");
                        BasisServerHandleEvents.RejectWithReason(newPeer, "was unable to authenticate!");
                    }
                }
            }
            catch (Exception e)
            {
                BNL.Log($"Error during authentication: {e.Message} {e.StackTrace}");
                BasisServerHandleEvents.RejectWithReason(newPeer, $"{e.Message} {e.StackTrace}");
            }
        }
        public Challenge MakeChallenge(Did ChallengingDID)
        {
            return DidAuth.MakeChallenge(ChallengingDID ?? throw new Exception("call RecvDid first"));
        }

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

        public void RemoveConnection(NetPeer NetPeer)
        {
            AuthIdentity.TryRemove(NetPeer, out var authIdentity);
        }
        public bool IsNetPeerAdmin(string UUID)
        {
            if (Admins.Contains(UUID))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddNetPeerAsAdmin(string UUID)
        {
            BNL.Log($"AddNetPeerAsAdmin {UUID}");
            Admins.Add(UUID);
            SaveAdmins(Admins.ToArray(), FilePath);
            return true;
        }
        static void SaveAdmins(string[] admins, string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(string[]));
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, admins);
            }
        }

        static string[] LoadAdmins(string filePath)
        {
            if (File.Exists(filePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(string[]));
                using (StreamReader reader = new StreamReader(filePath))
                {
                    return (string[])serializer.Deserialize(reader);
                }
            }
            else
            {
                string[] Admins = new string[] { };
                SaveAdmins(Admins, filePath);
                return Admins;
            }
        }
        public bool NetIDToUUID(NetPeer Peer, out string UUID)
        {
            if (AuthIdentity.TryGetValue(Peer, out OnAuth OnAuth))
            {
                UUID = OnAuth.Did.V;
                return true;
            }
            UUID = string.Empty;
            return false;
        }

        public bool UUIDToNetID(string UUID, out NetPeer Peer)
        {
            foreach (KeyValuePair<NetPeer, OnAuth> Pair in AuthIdentity)
            {
                if (Pair.Value.Did.V == UUID)
                {
                    Peer = Pair.Key;
                    return true;
                }
            }
            Peer = null;
            return false;
        }

        public bool RemoveNetPeerAsAdmin(string UUID)
        {
            BNL.Log($"RemoveNetPeerAsAdmin {UUID}");
            if (Admins.Remove(UUID))
            {
                SaveAdmins(Admins.ToArray(), FilePath);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
