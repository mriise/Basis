using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management.Devices.OpenXR;
using Basis.Scripts.TransformBinders.BoneControl;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using static BasisBaseMuscleDriver;

namespace Basis.Scripts.Device_Management.Devices.UnityInputSystem
{
    [DefaultExecutionOrder(15001)]
    public class BasisOpenXRControllerInput : BasisInput
    {
        public InputDevice Device;
        public FingerPose FingerCurls;
        public BasisOpenXRInputEye BasisOpenXRInputEye;
        public BasisVirtualSpineDriver BasisVirtualSpine = new BasisVirtualSpineDriver();
        public InputActionProperty Position;
        public InputActionProperty Rotation;
        public InputActionProperty Trigger;
        public InputActionProperty Grip;
        public InputActionProperty PrimaryButton;
        public InputActionProperty SecondaryButton;
        public InputActionProperty MenuButton;
        public InputActionProperty Primary2DAxis;
        public InputActionProperty Secondary2DAxis;

        public void Initialize(InputDevice device, string UniqueID, string UnUniqueID, string subSystems, bool AssignTrackedRole, BasisBoneTrackedRole basisBoneTrackedRole)
        {
            Device = device;
            InitalizeTracking(UniqueID, UnUniqueID, subSystems, AssignTrackedRole, basisBoneTrackedRole);

            switch (basisBoneTrackedRole)
            {
                case BasisBoneTrackedRole.CenterEye:
                    Position = new InputActionProperty(new InputAction("<XRHMD>/centerEyePosition", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
                    Rotation = new InputActionProperty(new InputAction("<XRHMD>/centerEyeRotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
                    Position.action.Enable();
                    Rotation.action.Enable();
                    BasisOpenXRInputEye = gameObject.AddComponent<BasisOpenXRInputEye>();
                    BasisOpenXRInputEye.Initalize();
                    BasisVirtualSpine.Initialize();
                    break;
                case BasisBoneTrackedRole.LeftHand:
                    SetupInputActions("<PalmPose>{LeftHand}", true);
                    break;
                case BasisBoneTrackedRole.RightHand:
                    SetupInputActions("<PalmPose>{RightHand}", true);
                    break;
            }
        }
        private void SetupInputActions(string devicePath,bool HasController)
        {
            Position = new InputActionProperty(new InputAction(devicePath + "/devicePosition", InputActionType.Value, devicePath + "/devicePosition", expectedControlType: "Vector3"));
            Rotation = new InputActionProperty(new InputAction(devicePath + "/deviceRotation", InputActionType.Value, devicePath + "/deviceRotation", expectedControlType: "Quaternion"));
            Position.action.Enable();
            Rotation.action.Enable();
            if (HasController)
            {
                Trigger = new InputActionProperty(new InputAction(devicePath + "/trigger", InputActionType.Value, devicePath + "/trigger", expectedControlType: "Float"));
                Grip = new InputActionProperty(new InputAction(devicePath + "/grip", InputActionType.Value, devicePath + "/grip", expectedControlType: "Float"));
                PrimaryButton = new InputActionProperty(new InputAction(devicePath + "/primaryButton", InputActionType.Button, devicePath + "/primaryButton", expectedControlType: "Button"));
                SecondaryButton = new InputActionProperty(new InputAction(devicePath + "/secondaryButton", InputActionType.Button, devicePath + "/secondaryButton", expectedControlType: "Button"));
                MenuButton = new InputActionProperty(new InputAction(devicePath + "/menuButton", InputActionType.Button, devicePath + "/menuButton", expectedControlType: "Button"));
                Primary2DAxis = new InputActionProperty(new InputAction(devicePath + "/primary2DAxis", InputActionType.Value, devicePath + "/primary2DAxis", expectedControlType: "Vector2"));
                Secondary2DAxis = new InputActionProperty(new InputAction(devicePath + "/secondary2DAxis", InputActionType.Value, devicePath + "/secondary2DAxis", expectedControlType: "Vector2"));
                Trigger.action.Enable();
                Grip.action.Enable();
                PrimaryButton.action.Enable();
                SecondaryButton.action.Enable();
                MenuButton.action.Enable();
                Primary2DAxis.action.Enable();
                Secondary2DAxis.action.Enable();
            }
        }
        private void DisableInputActions()
        {
            Position.action.Disable();
            Rotation.action.Disable();
            Trigger.action.Disable();
            Grip.action.Disable();
            PrimaryButton.action.Disable();
            SecondaryButton.action.Disable();
            MenuButton.action.Disable();
            Primary2DAxis.action.Disable();
            Secondary2DAxis.action.Disable();
        }

        public new void OnDestroy()
        {
            DisableInputActions();
            BasisVirtualSpine.DeInitialize();
            if (BasisOpenXRInputEye != null)
            {
                BasisOpenXRInputEye.Shutdown();
            }
            base.OnDestroy();
        }
        public override void DoPollData()
        {
            if (Position != null && Position.action != null)
            {
                LocalRawPosition = Position.action.ReadValue<Vector3>();
            }

            if (Rotation != null && Rotation.action != null)
            {
                LocalRawRotation = Rotation.action.ReadValue<Quaternion>();
            }
            if (BasisLocalPlayer.Instance != null && BasisLocalPlayer.Instance.CurrentHeight != null)
            {
                FinalPosition = LocalRawPosition * BasisLocalPlayer.Instance.CurrentHeight.EyeRatioAvatarToAvatarDefaultScale;
            }
            else
            {
                FinalPosition = LocalRawPosition;
            }

            FinalRotation = LocalRawRotation;
            if (Primary2DAxis != null && Primary2DAxis.action != null)
            {
                InputState.Primary2DAxis = Primary2DAxis.action.ReadValue<Vector2>();
            }

            if (Secondary2DAxis != null && Secondary2DAxis.action != null)
            {
                InputState.Secondary2DAxis = Secondary2DAxis.action.ReadValue<Vector2>();
            }

            if (Grip != null && Grip.action != null)
            {
                InputState.GripButton = Grip.action.ReadValue<float>() > 0.5f;
                InputState.SecondaryTrigger = Grip.action.ReadValue<float>();
            }

            if (MenuButton != null && MenuButton.action != null)
            {
                InputState.SystemOrMenuButton = MenuButton.action.ReadValue<float>() > 0.5f;
            }

            if (PrimaryButton != null && PrimaryButton.action != null)
            {
                InputState.PrimaryButtonGetState = PrimaryButton.action.ReadValue<float>() > 0.5f;
            }

            if (SecondaryButton != null && SecondaryButton.action != null)
            {
                InputState.SecondaryButtonGetState = SecondaryButton.action.ReadValue<float>() > 0.5f;
            }

            if (Trigger != null && Trigger.action != null)
            {
                InputState.Trigger = Trigger.action.ReadValue<float>();
            }
            if (hasRoleAssigned)
            {
                if (Control.HasTracked != BasisHasTracked.HasNoTracker)
                {
                    // Apply position offset using math.mul for quaternion-vector multiplication
                    Control.IncomingData.position = FinalPosition - math.mul(FinalRotation, AvatarPositionOffset * BasisLocalPlayer.Instance.CurrentHeight.EyeRatioAvatarToAvatarDefaultScale);

                    // Apply rotation offset using math.mul for quaternion multiplication
                    Control.IncomingData.rotation = math.mul(FinalRotation, Quaternion.Euler(AvatarRotationOffset));
                }
            }
            CalculateFingerCurls();
            UpdatePlayerControl();
        }


        private void CalculateFingerCurls()
        {
            FingerCurls.ThumbPercentage = new Vector2(InputState.GripButton ? -1f : 0.7f, 0);
            FingerCurls.IndexPercentage = new Vector2(BasisBaseMuscleDriver.MapValue(InputState.Trigger, 0, 1, -1f, 0.7f), 0);
            FingerCurls.MiddlePercentage = new Vector2(InputState.PrimaryButtonGetState ? -1f : 0.7f, 0);
            FingerCurls.RingPercentage = new Vector2(InputState.SecondaryButtonGetState ? -1f : 0.7f, 0);
            FingerCurls.LittlePercentage = new Vector2(InputState.SystemOrMenuButton ? 1 - 1f : 0.7f, 0);
        }
    }
}
