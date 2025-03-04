using Basis.Scripts.Device_Management.Devices;
using Basis.Scripts.TransformBinders.BoneControl;
using UnityEngine;
using static BasisBaseMuscleDriver;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using Basis.Scripts.BasisSdk.Players;
using System;

public class BasisOpenXRHandInput : BasisInput
{
    public FingerPose FingerCurls;
    public InputActionProperty Position;
    public InputActionProperty Rotation;
    public InputActionProperty Trigger;
    public InputActionProperty Grip;
    public InputActionProperty PrimaryButton;
    public InputActionProperty SecondaryButton;
    public InputActionProperty MenuButton;
    public InputActionProperty Primary2DAxis;
    public InputActionProperty Secondary2DAxis;

    public void Initialize(string UniqueID, string UnUniqueID, string subSystems, bool AssignTrackedRole, BasisBoneTrackedRole basisBoneTrackedRole)
    {
        InitalizeTracking(UniqueID, UnUniqueID, subSystems, AssignTrackedRole, basisBoneTrackedRole);
        string devicePath = basisBoneTrackedRole == BasisBoneTrackedRole.LeftHand ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";
        string DevicePalmPath = basisBoneTrackedRole == BasisBoneTrackedRole.LeftHand ? "<PalmPose>{LeftHand}" : "<PalmPose>{RightHand}";
        SetupInputActions(devicePath);
        Position = new InputActionProperty(new InputAction($"{DevicePalmPath}/devicePosition", InputActionType.Value, $"{DevicePalmPath}/devicePosition", expectedControlType: "Vector3"));
        Rotation = new InputActionProperty(new InputAction($"{DevicePalmPath}/deviceRotation", InputActionType.Value, $"{DevicePalmPath}/deviceRotation", expectedControlType: "Quaternion"));

        Position.action.Enable();
        Rotation.action.Enable();
    }
    private void SetupInputActions(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
        {
            Debug.LogError("Device path is null or empty.");
            return;
        }

        Trigger = new InputActionProperty(new InputAction(devicePath + "/trigger", InputActionType.Value, devicePath + "/trigger", expectedControlType: "Float"));
        Grip = new InputActionProperty(new InputAction(devicePath + "/grip", InputActionType.Value, devicePath + "/grip", expectedControlType: "Float"));
        PrimaryButton = new InputActionProperty(new InputAction(devicePath + "/primaryButton", InputActionType.Button, devicePath + "/primaryButton", expectedControlType: "Button"));
        SecondaryButton = new InputActionProperty(new InputAction(devicePath + "/secondaryButton", InputActionType.Button, devicePath + "/secondaryButton", expectedControlType: "Button"));
        MenuButton = new InputActionProperty(new InputAction(devicePath + "/menuButton", InputActionType.Button, devicePath + "/menuButton", expectedControlType: "Button"));
        Primary2DAxis = new InputActionProperty(new InputAction(devicePath + "/primary2DAxis", InputActionType.Value, devicePath + "/primary2DAxis", expectedControlType: "Vector2"));
        Secondary2DAxis = new InputActionProperty(new InputAction(devicePath + "/secondary2DAxis", InputActionType.Value, devicePath + "/secondary2DAxis", expectedControlType: "Vector2"));

        EnableInputActions();
    }

    private void EnableInputActions()
    {
        EnableInputAction(Position);
        EnableInputAction(Rotation);
        EnableInputAction(Trigger);
        EnableInputAction(Grip);
        EnableInputAction(PrimaryButton);
        EnableInputAction(SecondaryButton);
        EnableInputAction(MenuButton);
        EnableInputAction(Primary2DAxis);
        EnableInputAction(Secondary2DAxis);
    }

    private void DisableInputActions()
    {
        DisableInputAction(Position);
        DisableInputAction(Rotation);
        DisableInputAction(Trigger);
        DisableInputAction(Grip);
        DisableInputAction(PrimaryButton);
        DisableInputAction(SecondaryButton);
        DisableInputAction(MenuButton);
        DisableInputAction(Primary2DAxis);
        DisableInputAction(Secondary2DAxis);
    }

    private void EnableInputAction(InputActionProperty actionProperty) => actionProperty.action?.Enable();
    private void DisableInputAction(InputActionProperty actionProperty) => actionProperty.action?.Disable();

    public new void OnDestroy()
    {
        DisableInputActions();
        base.OnDestroy();
    }

    public override void DoPollData()
    {
        LocalRawPosition = Position.action.ReadValue<Vector3>();
         LocalRawRotation = Rotation.action.ReadValue<Quaternion>();

        FinalPosition = BasisLocalPlayer.Instance?.CurrentHeight != null
            ? LocalRawPosition * BasisLocalPlayer.Instance.CurrentHeight.EyeRatioAvatarToAvatarDefaultScale
            : LocalRawPosition;

        FinalRotation = LocalRawRotation;

        InputState.Primary2DAxis = Primary2DAxis.action?.ReadValue<Vector2>() ?? Vector2.zero;
        InputState.Secondary2DAxis = Secondary2DAxis.action?.ReadValue<Vector2>() ?? Vector2.zero;

        InputState.GripButton = Grip.action?.ReadValue<float>() > 0.5f;
        InputState.SecondaryTrigger = Grip.action?.ReadValue<float>() ?? 0f;
        InputState.SystemOrMenuButton = MenuButton.action?.ReadValue<float>() > 0.5f;
        InputState.PrimaryButtonGetState = PrimaryButton.action?.ReadValue<float>() > 0.5f;
        InputState.SecondaryButtonGetState = SecondaryButton.action?.ReadValue<float>() > 0.5f;

        InputState.Trigger = Trigger.action?.ReadValue<float>() ?? 0f;

        if (hasRoleAssigned && Control.HasTracked != BasisHasTracked.HasNoTracker)
        {
            // Apply position offset using math.mul for quaternion-vector multiplication
            Control.IncomingData.position = FinalPosition - math.mul(FinalRotation, AvatarPositionOffset * BasisLocalPlayer.Instance.CurrentHeight.EyeRatioAvatarToAvatarDefaultScale);

            // Apply rotation offset using math.mul for quaternion multiplication
            Control.IncomingData.rotation = math.mul(FinalRotation, Quaternion.Euler(AvatarRotationOffset));
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
