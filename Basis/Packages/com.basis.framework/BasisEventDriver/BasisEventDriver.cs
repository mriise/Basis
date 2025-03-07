using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Device_Management.Devices.Desktop;
using Basis.Scripts.Eye_Follow;
using Basis.Scripts.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

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
        BasisNetworkManagement.SimulateNetwork();
    }
    private void OnBeforeRender()
    {
        if (BasisLocalPlayer.PlayerReady)
        {
            BasisLocalPlayer.Instance.LocalBoneDriver.Simulate();
        }
        BasisLocalInputActions.AfterAvatarChanges?.Invoke();
    }
}
