using Basis.Scripts.Behaviour;
using LiteNetLib;
using System.Collections.Generic;
using UnityEngine;
namespace Basis.Scripts.UGC.BlendShapes
{
    public class BasisUGCBlendShapes : BasisAvatarMonoBehaviour
    {
        public SkinnedMeshRenderer BlendShapeRenderer;
        [SerializeField]
        public List<BasisUGCBlendShapesItem> basisUGCBlendShapesItems;
        [System.Serializable]
        public struct BasisUGCBlendShapesItem
        {
            public BasisUGCMenuDescription Description;
            public List<BasisUGCBlendShapesItem> BlendShapeSettings;
            public BasisBlendShapeMode Mode;
        }
        [System.Serializable]
        public struct BasisUGCBlendShapeSettings 
        {
            public string BlendShapeName;
            [Range(0,100)]
            public float Value;
        }
        public enum BasisBlendShapeMode
        {
            SetTo,
            Slider
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
