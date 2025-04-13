using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management;

namespace Basis.Scripts.Drivers
{
    public class BasisLocalBoneDriver : BaseBoneDriver
    {
        public void SimulateBonePositions(float DeltaTime)
        {
            SimulateAndApply(BasisLocalPlayer.Instance,DeltaTime);
            
        }
        public void PostSimulateBonePositions()
        {
            SimulateWorldDestinations();
        }
    }
}
