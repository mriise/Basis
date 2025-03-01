#nullable enable

namespace Basis.Contrib.Crypto
{
	public record Payload(byte[] V);

	public record Signature(byte[] V);

	/// Public asymmetric key
	public record Pubkey(byte[] V);

	/// Private (secret) asymmetric key
	public record Privkey(byte[] V);

	/// Private (secret) symmetric key
	public record SharedSecretKey(byte[] V);

	/// The full set of SigningAlgorithms we support
	public enum SigningAlgorithm
	{
		Ed25519,
	}
}
