using Basis.Scripts.Device_Management;
using UnityEngine;

namespace Basis.Scripts.Drivers
{
    public class BasisLocalBoneDriver : BaseBoneDriver
    {
        public void Start()
        {
            BasisDeviceManagement.Instance.OnBootModeChanged += OnBootModeChanged;
            OnBootModeChanged(BasisDeviceManagement.Instance.CurrentMode);
        }

        private void OnBootModeChanged(string mode)
        {
        }
        public float DeltaTime;
        public void Simulate()
        {
            DeltaTime = Time.deltaTime;
            if (float.IsNaN(DeltaTime))
            {
                return;
            }

            SimulateAndApply(DeltaTime);
        }
        public void OnDestroy()
        {
            BasisDeviceManagement.Instance.OnBootModeChanged -= OnBootModeChanged;
        }
    }
}
