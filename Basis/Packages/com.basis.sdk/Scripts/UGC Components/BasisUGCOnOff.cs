using Basis.Scripts.Behaviour;
using LiteNetLib;
using UnityEngine;
namespace Basis.Scripts.UGC.OnOff
{
    public class BasisUGCOnOff : BasisAvatarMonoBehaviour
    {
        [SerializeField]
        public BasisUGCOnOffItem[] OffOnItems;

        public override void OnNetworkChange(byte messageIndex)
        {
            throw new System.NotImplementedException();
        }

        public override void OnNetworkMessageReceived(ushort RemoteUser, byte[] buffer, DeliveryMethod DeliveryMethod)
        {
            throw new System.NotImplementedException();
        }

        public override void OnNetworkMessageServerReductionSystem(byte[] buffer)
        {
            throw new System.NotImplementedException();
        }

        [System.Serializable]
        public struct BasisUGCOnOffItem
        {
            public BasisUGCMenuDescription Description;
            public GameObject[] ToggleableGameObjects;
            public Component[] ToggleableComponents;
        }
    }
}
