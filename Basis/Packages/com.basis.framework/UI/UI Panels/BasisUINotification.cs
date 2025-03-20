using Basis.Scripts.Addressable_Driver;
using Basis.Scripts.Addressable_Driver.Enums;
using Basis.Scripts.UI.UI_Panels;
using TMPro;

public class BasisUINotification : BasisUIBase
{
    public static string Path = "Packages/com.basis.sdk/Prefabs/UI/BasisUINotification.prefab";
    public static string CursorRequest = "BasisUINotification";
    public TextMeshProUGUI Text;
    public override void DestroyEvent()
    {
        BasisCursorManagement.LockCursor(CursorRequest);
    }
    public static void OpenNotification(string Reason)
    {
        AddressableGenericResource resource = new AddressableGenericResource(Path, AddressableExpectedResult.SingleItem);
        BasisUIBase Base = OpenMenuNow(resource);
        BasisUINotification Notification = (BasisUINotification)Base;
        Notification.Text.text = Reason;
    }
    public override void InitalizeEvent()
    {
        BasisCursorManagement.UnlockCursor(CursorRequest);
    }
}
