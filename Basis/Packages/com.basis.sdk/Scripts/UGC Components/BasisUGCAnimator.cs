using Basis.Scripts.Behaviour;
using LiteNetLib;
using UnityEngine;
namespace Basis.Scripts.UGC.Animator
{
    public class BasisUGCAnimator : BasisAvatarMonoBehaviour
    {
        [SerializeField]
        public BasisUGCAnimatorItem[] AnimatorItems;

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
    }
}
