using UnityEngine;
namespace Basis.Scripts.UGC.OnOff
{
    public class BasisUGCOnOff : MonoBehaviour
    {
        [SerializeField]
        public BasisUGCOnOffItem[] OffOnItems;
        [System.Serializable]
        public struct BasisUGCOnOffItem
        {
            public BasisUGCMenuDescription Description;
            public GameObject[] ToggleableGameObjects;
            public Component[] ToggleableComponents;
        }
    }
}
