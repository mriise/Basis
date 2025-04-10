using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Eye_Follow;
using Basis.Scripts.Networking;
using Basis.Scripts.Networking.Transmitters;
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
            //moves all bones to where they belong
            LocalPlayer.LocalBoneDriver.SimulateBonePositions();
            //moves Avatar Transform to where it belongs
            LocalPlayer.MoveAvatar();

            //Simulate Final Destination of IK
            LocalPlayer.AvatarDriver.SimulateIKDesinations();

            //process Animator and IK processes.
            LocalPlayer.AvatarDriver.SimulateAnimatorAndIk();

            //camera, physical controllers and other get moved here. MUST BE CHILDREN OF BASISLOCALPLAYER.
            LocalPlayer.AfterIkSimulation?.Invoke();

            //we move the player at the very end after everything has been processed.
            LocalPlayer.Move.SimulateMovement();

            //now that everything has been processed jiggles can move.
            if (LocalPlayer.HasJiggles)
            {
                LocalPlayer.BasisAvatarStrainJiggleDriver.Simulate(0);
            }

            //now other things can move like UI
            LocalPlayer.AfterFinalMove?.Invoke();

            //send out avatar
            BasisNetworkTransmitter.AfterAvatarChanges?.Invoke();
        }
    }
}
