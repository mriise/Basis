using System;
using System.Diagnostics;
using System.Text;
using Basis.Contrib.Auth.DecentralizedIds;
using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
using Basis.Contrib.Crypto;
using LiteNetLib;
using LiteNetLib.Utils;
using static Basis.Network.Core.Serializable.SerializableBasis;
using CryptoRng = System.Security.Cryptography.RandomNumberGenerator;

namespace BasisNetworkClient
{
    public static class BasisDIDAuthIdentityClient
    {
        private static (PubKey, PrivKey) Key;
        private static Did DID;
        public static void GetOrSaveDID()
        {
            ClientKeyCreation(out Key, out DID);

        }
        public static bool IdentityMessage(NetPeer peer, NetPacketReader Reader, out NetDataWriter Writer)
        {
            Writer = new NetDataWriter();
            BytesMessage ChallengeBytes = new BytesMessage();

            ChallengeBytes.Deserialize(Reader);
            // Client
            var payloadToSign = new Payload(ChallengeBytes.bytes);
            if (Ed25519.Sign(Key.Item2, payloadToSign, out Signature sig) == false)
            {
                BNL.LogError("Unable to sign Key");
                return false;
            }
            if (Ed25519.Verify(Key.Item1, sig, payloadToSign) == false)
            {
                BNL.LogError("Unable to Very Key");
                return false;
            }
            // for simplicity, use an empty fragment since the client only has one pubkey
            Response response = new Response(sig, new DidUrlFragment(string.Empty));
            BytesMessage SignatureBytes = new BytesMessage();
            SignatureBytes.bytes = response.Signature.V;
            BytesMessage FragmentBytes = new BytesMessage();
            FragmentBytes.bytes = CompressString(response.DidUrlFragment.V);
            SignatureBytes.Serialize(Writer);
            FragmentBytes.Serialize(Writer);
            return true;
        }
        public static (PubKey, PrivKey) RandomKeyPair(CryptoRng rng)
        {
            var privKeyBytes = new byte[Ed25519.PrivkeySize];
            rng.GetBytes(privKeyBytes);
            var privKey = new PrivKey(privKeyBytes);
            var pubKey = Ed25519.ConvertPrivkeyToPubkey(privKey) ?? throw new Exception("privkey was invalid");
            return (pubKey, privKey);
        }
        public static void ClientKeyCreation(out (PubKey, PrivKey) Keys,out Did Did)
        {
            // Client
            CryptoRng rng = CryptoRng.Create();
            Keys = RandomKeyPair(rng);
            Did = DidKeyResolver.EncodePubkeyAsDid(Keys.Item1);
        }
        public static byte[] CompressString(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
