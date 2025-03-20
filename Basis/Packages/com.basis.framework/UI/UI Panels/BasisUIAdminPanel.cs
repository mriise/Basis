using Basis.Scripts.UI.UI_Panels;
using UnityEngine;

public class BasisUIAdminPanel : BasisUIBase
{
    public static string Path = "Packages/com.basis.sdk/Prefabs/UI/BasisUIAdminPanel.prefab";
    public static string CursorRequest = "BasisUIAdminPanel";
    public override void DestroyEvent()
    {
        BasisCursorManagement.LockCursor(CursorRequest);
    }

    public override void InitalizeEvent()
    {
        BasisCursorManagement.UnlockCursor(CursorRequest);
    }
}
