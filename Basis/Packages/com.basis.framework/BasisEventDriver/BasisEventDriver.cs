using Basis.Scripts.Device_Management;
using Basis.Scripts.Eye_Follow;
using UnityEngine;

public class BasisEventDriver : MonoBehaviour
{
    public  float updateInterval = 0.1f; // 100 milliseconds
    public  float timeSinceLastUpdate = 0f;

    public void Update()
    {

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
    }
}
