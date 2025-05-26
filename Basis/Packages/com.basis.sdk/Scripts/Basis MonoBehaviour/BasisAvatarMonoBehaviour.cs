using Basis.Scripts.BasisSdk;
using LiteNetLib;
using UnityEngine;
namespace Basis.Scripts.Behaviour
{
    public abstract class BasisAvatarMonoBehaviour : MonoBehaviour
    {
        public bool IsInitalized = false;
        public byte MessageIndex;
        public BasisAvatar Avatar;
        public void OnNetworkAssign(byte messageIndex)
        {
            MessageIndex = messageIndex;
            IsInitalized = true;
            OnNetworkChange(messageIndex);
        }
        public abstract void OnNetworkChange(byte messageIndex);
        public abstract void OnNetworkMessageReceived(ushort RemoteUser, byte[] buffer, DeliveryMethod DeliveryMethod);
        public abstract void OnNetworkMessageServerReductionSystem(byte[] buffer);
        /// <summary>
        /// this is used for sending Network Messages
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="DeliveryMethod"></param>
        /// <param name="Recipients">if null everyone but self, you can include yourself to make it loop back over the network</param>
        public void NetworkMessageSend(byte[] buffer = null, DeliveryMethod DeliveryMethod = DeliveryMethod.Unreliable, ushort[] Recipients = null)
        {
            if (IsInitalized)
            {
                Avatar.OnNetworkMessageSend?.Invoke(MessageIndex, buffer, DeliveryMethod, Recipients);
            }
            else
            {
                BasisDebug.LogError("Behaviour is not Initalized");
            }
        }
        /// <summary>
        /// this is used for sending Network Messages
        /// </summary>
        /// <param name="DeliveryMethod"></param>
        public void NetworkMessageSend(DeliveryMethod DeliveryMethod = DeliveryMethod.Unreliable)
        {
            if (IsInitalized)
            {
                Avatar.OnNetworkMessageSend?.Invoke(MessageIndex, null, DeliveryMethod);
            }
            else
            {
                BasisDebug.LogError("Behaviour is not Initalized");
            }
        }
        public void ServerReductionSystemMessageSend( byte[] buffer = null)
        {
            if (IsInitalized)
            {
                Avatar.OnServerReductionSystemMessageSend?.Invoke(MessageIndex, buffer);
            }
            else
            {
                BasisDebug.LogError("Behaviour is not Initalized");
            }
        }
    }
}
