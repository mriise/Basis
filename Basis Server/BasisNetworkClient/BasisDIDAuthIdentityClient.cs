using System;
using System.Text;
using Basis.Contrib.Auth.DecentralizedIds;
using Basis.Contrib.Auth.DecentralizedIds.Newtypes;
using Basis.Contrib.Crypto;
using CryptoRng = System.Security.Cryptography.RandomNumberGenerator;

namespace BasisNetworkClient
{
    public static class BasisDIDAuthIdentityClient
    {
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
