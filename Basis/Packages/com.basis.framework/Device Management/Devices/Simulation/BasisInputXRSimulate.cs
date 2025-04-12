using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.TransformBinders.BoneControl;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Basis.Scripts.Device_Management.Devices.Simulation
{
    public class BasisInputXRSimulate : BasisInput
    {
        public Transform FollowMovement;
        public bool AddSomeRandomizedInput = false;
        public float MinMaxOffset = 0.0001f;
        public float LerpAmount = 0.1f;
        public override void DoPollData()
        {
            if (AddSomeRandomizedInput)
            {
                Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-MinMaxOffset, MinMaxOffset), UnityEngine.Random.Range(-MinMaxOffset, MinMaxOffset), UnityEngine.Random.Range(-MinMaxOffset, MinMaxOffset));

                Quaternion randomRotation = UnityEngine.Random.rotation;
                Quaternion lerpedRotation = Quaternion.Lerp(FollowMovement.localRotation, randomRotation, LerpAmount * Time.deltaTime);

                Vector3 originalPosition = FollowMovement.localPosition;
                Vector3 newPosition = Vector3.Lerp(originalPosition, originalPosition + randomOffset, LerpAmount * Time.deltaTime);

                FollowMovement.SetLocalPositionAndRotation(newPosition, lerpedRotation);
            }
            FollowMovement.GetLocalPositionAndRotation(out Vector3 VOut, out Quaternion QOut);
            LocalRawPosition = VOut;
            LocalRawRotation = QOut;

            LocalRawPosition /= BasisLocalPlayer.Instance.CurrentHeight.SelectedPlayerToDefaultScale;

            FinalPosition = LocalRawPosition * BasisLocalPlayer.Instance.CurrentHeight.SelectedPlayerToDefaultScale;
            FinalRotation = LocalRawRotation;
            if (hasRoleAssigned)
            {
                if (Control.HasTracked != BasisHasTracked.HasNoTracker)
                {
                    // Apply the position offset using math.mul for quaternion-vector multiplication
                    Control.IncomingData.position = FinalPosition - math.mul(FinalRotation, AvatarPositionOffset * BasisLocalPlayer.Instance.CurrentHeight.SelectedAvatarToAvatarDefaultScale);
                    Control.IncomingData.rotation = math.mul(FinalRotation, Quaternion.Euler(AvatarRotationOffset));
                }
            }
            UpdatePlayerControl();
        }
        public new void OnDestroy()
        {
            if (FollowMovement != null)
            {
                GameObject.Destroy(FollowMovement.gameObject);
            }
            base.OnDestroy();
        }

        public override void ShowTrackedVisual()
        {
            if (BasisVisualTracker == null && LoadedDeviceRequest == null)
            {
                BasisDeviceMatchSettings Match = BasisDeviceManagement.Instance.BasisDeviceNameMatcher.GetAssociatedDeviceMatchableNames(CommonDeviceIdentifier);
                if (Match.CanDisplayPhysicalTracker)
                {
                    var op = Addressables.LoadAssetAsync<GameObject>(Match.DeviceID);
                    GameObject go = op.WaitForCompletion();
                    GameObject gameObject = Object.Instantiate(go);
                    gameObject.name = CommonDeviceIdentifier;
                    gameObject.transform.parent = this.transform;
                    if (gameObject.TryGetComponent(out BasisVisualTracker))
                    {
                        BasisVisualTracker.Initialization(this);
                    }
                }
                else
                {
                    if (UseFallbackModel())
                    {
                        UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> op = Addressables.LoadAssetAsync<GameObject>(FallbackDeviceID);
                        GameObject go = op.WaitForCompletion();
                        GameObject gameObject = Object.Instantiate(go);
                        gameObject.name = CommonDeviceIdentifier;
                        gameObject.transform.parent = this.transform;
                        if (gameObject.TryGetComponent(out BasisVisualTracker))
                        {
                            BasisVisualTracker.Initialization(this);
                        }
                    }
                }
            }
        }
    }
}
