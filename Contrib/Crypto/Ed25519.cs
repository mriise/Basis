#nullable enable

using Org.BouncyCastle.Crypto.Parameters;
using Ed25519Signer = Org.BouncyCastle.Crypto.Signers.Ed25519Signer;
using Rfc8032 = Org.BouncyCastle.Math.EC.Rfc8032;

namespace Basis.Contrib.Crypto
{
	/// Ed25519 elliptic-curve signature algorithm.
	public sealed class Ed25519
	{
		public static readonly int PubkeySize = Rfc8032.Ed25519.PublicKeySize;
		public static readonly int PrivkeySize = Rfc8032.Ed25519.SecretKeySize;

		public static bool VerifySignature(
			Pubkey pubkey,
			Signature sig,
			Payload payload
		)
		{
			Ed25519PublicKeyParameters ed25519Params;
			try
			{
				ed25519Params = new Ed25519PublicKeyParameters(pubkey.V);
			}
			catch
			{
				return false;
			}
			var signer = new Ed25519Signer();
			signer.Init(false, ed25519Params);
			signer.BlockUpdate(buf: payload.V, off: 0, len: payload.V.Length);
			return signer.VerifySignature(sig.V);
		}
	}
}
