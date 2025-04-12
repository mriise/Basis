using System.Collections.Generic;
using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Drivers;
using Basis.Scripts.TransformBinders.BoneControl;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
namespace Basis.Scripts.Device_Management.Devices.Desktop
{
    public class BasisAvatarEyeInput : BasisInput
    {
        public Camera Camera;
        public BasisLocalAvatarDriver AvatarDriver;
        public static BasisAvatarEyeInput Instance;
        public float crouchPercentage = 0.5f;
        public float rotationSpeed = 0.1f;
        public float rotationY;
        public float rotationX;
        public float minimumY = -89f;
        public float maximumY = 50f;
        public bool BlockCrouching;
        public float InjectedX = 0;
        public float InjectedZ = 0;
        public bool HasEyeEvents = false;
        [SerializeField]
        private List<string> headPauseRequests = new();
        public void Initalize(string ID = "Desktop Eye", string subSystems = "BasisDesktopManagement")
        {
            BasisDebug.Log("Initializing Avatar Eye", BasisDebug.LogTag.Input);
            if (BasisLocalPlayer.Instance.LocalAvatarDriver != null)
            {
                BasisDebug.Log("Using Configured Height " + BasisLocalPlayer.Instance.CurrentHeight.SelectedPlayerHeight, BasisDebug.LogTag.Input);
                LocalRawPosition = new Vector3(InjectedX, BasisLocalPlayer.Instance.CurrentHeight.SelectedPlayerHeight, InjectedZ);
                LocalRawRotation = Quaternion.identity;
            }
            else
            {
                BasisDebug.Log("Using Fallback Height " + BasisLocalPlayer.FallbackSize, BasisDebug.LogTag.Input);
                LocalRawPosition = new Vector3(InjectedX, BasisLocalPlayer.FallbackSize, InjectedZ);
                LocalRawRotation = Quaternion.identity;
            }
            FinalPosition = LocalRawPosition;
            FinalRotation = LocalRawRotation;
             InitalizeTracking(ID, ID, subSystems, true, BasisBoneTrackedRole.CenterEye);
            if (BasisHelpers.CheckInstance(Instance))
            {
                Instance = this;
            }
            PlayerInitialized();
            BasisCursorManagement.OverrideAbleLock(nameof(BasisAvatarEyeInput));
            if (HasEyeEvents == false)
            {
                BasisLocalPlayer.Instance.OnLocalAvatarChanged += PlayerInitialized;
                BasisLocalPlayer.Instance.OnPlayersHeightChanged += BasisLocalPlayer_OnPlayersHeightChanged;
                BasisLocalPlayer_OnPlayersHeightChanged();
                BasisCursorManagement.OnCursorStateChange += OnCursorStateChange;
                BasisPointRaycaster.UseWorldPosition = false;
                BasisVirtualSpine.Initialize();
                HasEyeEvents = true;
            }
        }
        public void PauseHead(string requestName)
        {
            headPauseRequests.Add(requestName);
        }
        public bool UnPauseHead(string requestName)
        {
            return headPauseRequests.Remove(requestName);
        }
        private void OnCursorStateChange(CursorLockMode cursor, bool newCursorVisible)
        {
            BasisDebug.Log("cursor changed to : " + cursor.ToString() + " | Cursor Visible : " + newCursorVisible, BasisDebug.LogTag.Input);
            if (cursor == CursorLockMode.Locked)
            {
                UnPauseHead(nameof(BasisCursorManagement));
            }
            else
            {
                PauseHead(nameof(BasisCursorManagement));
            }
        }
        public new void OnDestroy()
        {
            if (HasEyeEvents)
            {
                BasisLocalPlayer.Instance.OnLocalAvatarChanged -= PlayerInitialized;
                BasisLocalPlayer.Instance.OnPlayersHeightChanged -= BasisLocalPlayer_OnPlayersHeightChanged;
                BasisCursorManagement.OnCursorStateChange -= OnCursorStateChange;
                HasEyeEvents = false;

                BasisVirtualSpine.DeInitialize();
            }
            base.OnDestroy();
        }
        private void BasisLocalPlayer_OnPlayersHeightChanged()
        {
            BasisLocalPlayer.Instance.CurrentHeight.PlayerEyeHeight = BasisLocalPlayer.Instance.CurrentHeight.AvatarEyeHeight;
        }
        public void PlayerInitialized()
        {
            BasisLocalInputActions.CharacterEyeInput = this;
            AvatarDriver = BasisLocalPlayer.Instance.LocalAvatarDriver;
            Camera = BasisLocalCameraDriver.Instance.Camera;
            BasisDeviceManagement Device = BasisDeviceManagement.Instance;
            int count = Device.BasisLockToInputs.Count;
            for (int Index = 0; Index < count; Index++)
            {
                Device.BasisLockToInputs[Index].FindRole();
            }
        }
        public new void OnDisable()
        {
            BasisLocalPlayer.Instance.OnLocalAvatarChanged -= PlayerInitialized;
            base.OnDisable();
        }
        public void HandleMouseRotation(Vector2 lookVector)
        {
            BasisPointRaycaster.ScreenPoint = Mouse.current.position.value;
            if (!isActiveAndEnabled || headPauseRequests.Count > 0)
            {
                return;
            }
            rotationX += lookVector.x * rotationSpeed;
            rotationY -= lookVector.y * rotationSpeed;
        }
        public float InjectedZRot = 0;
        public override void DoPollData()
        {
            if (hasRoleAssigned)
            {
                if (BasisLocalInputActions.Instance != null)
                {
                    BasisLocalInputActions.Instance.InputState.CopyTo(InputState);
                }
                // InputState.CopyTo(characterInputActions.InputState);
                // Apply modulo operation to keep rotation within 0 to 360 range
                rotationX %= 360f;
                rotationY %= 360f;
                // Clamp rotationY to stay within the specified range
                rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);
                LocalRawRotation = Quaternion.Euler(rotationY, rotationX, InjectedZRot);
                Vector3 adjustedHeadPosition = new Vector3(InjectedX, BasisLocalPlayer.Instance.CurrentHeight.SelectedPlayerHeight, InjectedZ);
                if (BasisLocalInputActions.Crouching)
                {
                    adjustedHeadPosition.y -= Control.TposeLocal.position.y * crouchPercentage;
                }
                LocalRawPosition = adjustedHeadPosition;
                Control.IncomingData.position = LocalRawPosition;
                Control.IncomingData.rotation = LocalRawRotation;
                FinalPosition = LocalRawPosition;
                FinalRotation = LocalRawRotation;
                UpdatePlayerControl();
            }
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

        public BasisVirtualSpineDriver BasisVirtualSpine = new BasisVirtualSpineDriver();
    }
}
