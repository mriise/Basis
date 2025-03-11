using LiteNetLib;
using System.Threading.Tasks;
using BytesMessage = Basis.Network.Core.Serializable.SerializableBasis.BytesMessage;

namespace Basis.Network.Server.Auth
{
    /// <summary>
    /// class use to see if we can authenticate.
    /// (password correct)
    /// </summary>
    public interface IAuth
    {
        public bool IsAuthenticated(BytesMessage msg);
    }
    public interface IAuthIdentity
    {
        /// <summary>
        /// class we use to get the users identity
        /// the UUID of a player will become this.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public void IsUserIdentifiable(BytesMessage msg, NetPeer NetPeer, out string UUID);
    }
}
