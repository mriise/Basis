using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Eye_Follow;
using Basis.Scripts.Networking;
using Basis.Scripts.Networking.Transmitters;
using UnityEngine;
using UnityEngine.InputSystem;
using static Basis.Scripts.BasisSdk.Players.BasisLocalPlayer;
using static Basis.Scripts.BasisSdk.Players.BasisPlayer;
using static Basis.Scripts.Drivers.BaseBoneDriver;
using static Basis.Scripts.Drivers.BasisLocalBoneDriver;

public class BasisEventDriver : MonoBehaviour
{
    public float updateInterval = 0.1f; // 100 milliseconds
    public float timeSinceLastUpdate = 0f;
    public void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }
    public void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }
    public void Update()
    {
        BasisNetworkManagement.SimulateNetworkCompute();
        InputSystem.Update();
        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateInterval) // Use '>=' to avoid small errors
        {
            timeSinceLastUpdate -= updateInterval; // Subtract interval instead of resetting to zero
            BasisConsoleLogger.QueryLogDisplay();
        }
        if (!BasisDeviceManagement.hasPendingActions) return;

        while (BasisDeviceManagement.mainThreadActions.TryDequeue(out System.Action action))
        {
            action.Invoke();
        }

        // Reset flag once all actions are executed
        BasisDeviceManagement.hasPendingActions = !BasisDeviceManagement.mainThreadActions.IsEmpty;
    }

    public void LateUpdate()
    {
        if (BasisLocalEyeFollowBase.RequiresUpdate())
        {
            BasisLocalEyeFollowBase.Instance.Simulate();
        }
        MicrophoneRecorder.MicrophoneUpdate();
        BasisNetworkManagement.SimulateNetworkApply();
    }
    private void OnBeforeRender()
    {
        if (BasisLocalPlayer.PlayerReady)
        {
            BasisLocalPlayer LocalPlayer = BasisLocalPlayer.Instance;
            LocalPlayer.LocalBoneDriver.SimulateBonePositions();
            LocalPlayer.MoveAvatar();
            LocalPlayer.AvatarDriver.SimulateIKDesinations();

            LocalPlayer.AppliedBones?.Invoke();
            LocalPlayer.Move.SimulateMovement();
            if (LocalPlayer.HasJiggles)
            {
                LocalPlayer.BasisAvatarStrainJiggleDriver.Simulate(0);
            }
        }
        BasisNetworkTransmitter.AfterAvatarChanges?.Invoke();
    }
}
