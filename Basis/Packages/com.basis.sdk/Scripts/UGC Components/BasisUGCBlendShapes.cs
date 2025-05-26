using Basis.Scripts.Behaviour;
using LiteNetLib;
using UnityEngine;
namespace Basis.Scripts.UGC.BlendShapes
{
    public class BasisUGCBlendShapes : BasisAvatarMonoBehaviour
    {
        [SerializeField]
        public BasisUGCBlendShapesItem[] basisUGCBlendShapesItems;
        [System.Serializable]
        public struct BasisUGCBlendShapesItem
        {
            public BasisUGCMenuDescription Description;
            public SkinnedMeshRenderer BlendShapeRenderer;
            public BasisUGCBlendShapeSettings[] BlendShapeSettings;
        }
        [System.Serializable]
        public struct BasisUGCBlendShapeSettings 
        {
            public string BlendShapeName;
            [Range(0,100)]
            public float Value;
        }
        public override void OnNetworkMessageReceived(ushort RemoteUser, byte[] buffer, DeliveryMethod DeliveryMethod)
        {

        }

        public override void OnNetworkMessageServerReductionSystem(byte[] buffer)
        {

        }

        public override void OnNetworkChange(byte messageIndex)
        {

        }
    }
}
