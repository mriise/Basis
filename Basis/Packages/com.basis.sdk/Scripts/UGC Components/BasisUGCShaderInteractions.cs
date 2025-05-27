using Basis.Scripts.Behaviour;
using LiteNetLib;
using UnityEngine;
namespace Basis.Scripts.UGC.ShaderInteractions
{
    public class BasisUGCShaderInteractions : BasisAvatarMonoBehaviour
    {
        [SerializeField]
        public BasisUGCShaderInteractionsItem[] basisUGCShaderInteractionsItems;

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
        public struct BasisUGCShaderInteractionsItem
        {
            public BasisUGCMenuDescription Description;
            public BasisUGCShaderSettings[] ToggleableGameObjects;
        }
        [System.Serializable]
        public struct BasisUGCShaderSettings
        {
            public Material Material;
            public string MaterialProperty;
        }
    }
}
