using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Common;
using Basis.Scripts.Drivers;
using System.Threading.Tasks;
using UnityEngine;

public static class BasisHeightDriver
{
    public static string FileNameAndExtension = "SavedHeight.BAS";
    /// <summary>
    /// Adjusts the player's eye height after allowing all devices and systems to reset to their native size. 
    /// This method waits for 4 frames (including asynchronous frames) to ensure the final positions are updated.
    /// </summary>
    public static void SetPlayersEyeHeight(BasisLocalPlayer BasisLocalPlayer)
    {
        if (BasisLocalPlayer == null)
        {
            BasisDebug.LogError("BasisPlayer is null. Cannot set player's eye height.");
            return;
        }
        BasisLocalPlayer.CurrentHeight.CopyTo(BasisLocalPlayer.LastHeight);
        BasisLocalPlayer.CurrentHeight.AvatarName = BasisLocalPlayer.BasisAvatar.name;
        // Retrieve the player's eye height from the input device
        CapturePlayerHeight();
        // Retrieve the active avatar's eye height
        BasisLocalPlayer.CurrentHeight.AvatarEyeHeight = BasisLocalPlayer.LocalAvatarDriver?.ActiveAvatarEyeHeight() ?? 0;
        BasisDebug.Log($"Avatar eye height: {BasisLocalPlayer.CurrentHeight.AvatarEyeHeight}, Player eye height: {BasisLocalPlayer.CurrentHeight.PlayerEyeHeight}", BasisDebug.LogTag.Avatar);

        // Handle potential issues with height data
        if (BasisLocalPlayer.CurrentHeight.PlayerEyeHeight <= 0 || BasisLocalPlayer.CurrentHeight.AvatarEyeHeight <= 0)
        {
            BasisLocalPlayer.CurrentHeight.RatioPlayerToAvatarScale = 1;
            if (BasisLocalPlayer.CurrentHeight.PlayerEyeHeight <= 0)
            {
                BasisLocalPlayer.CurrentHeight.PlayerEyeHeight = BasisLocalPlayer.DefaultPlayerEyeHeight; // Set a default eye height if invalid
                Debug.LogWarning("Player eye height was invalid. Set to default: 1.64f.");
            }

            BasisDebug.LogError("Invalid height data. Scaling ratios set to defaults.");
        }
        else
        {
            // Calculate scaling ratios
            BasisLocalPlayer.CurrentHeight.RatioPlayerToAvatarScale = BasisLocalPlayer.CurrentHeight.AvatarEyeHeight / BasisLocalPlayer.CurrentHeight.PlayerEyeHeight;
        }

        // Calculate other scaling ratios
        BasisLocalPlayer.CurrentHeight.EyeRatioAvatarToAvatarDefaultScale = BasisLocalPlayer.CurrentHeight.AvatarEyeHeight / BasisLocalPlayer.DefaultAvatarEyeHeight;
        BasisLocalPlayer.CurrentHeight.EyeRatioPlayerToDefaultScale = BasisLocalPlayer.CurrentHeight.PlayerEyeHeight / BasisLocalPlayer.DefaultPlayerEyeHeight;

        // Notify listeners that height recalculation is complete
        BasisDebug.Log($"Final Player Eye Height: {BasisLocalPlayer.CurrentHeight.PlayerEyeHeight}", BasisDebug.LogTag.Avatar);
        BasisLocalPlayer.OnPlayersHeightChanged?.Invoke();
    }
    public static void CapturePlayerHeight()
    {
        Basis.Scripts.TransformBinders.BasisLockToInput basisLockToInput = BasisLocalCameraDriver.Instance?.BasisLockToInput;
        if (basisLockToInput?.AttachedInput != null)
        {
            BasisLocalPlayer.Instance.CurrentHeight.PlayerEyeHeight = basisLockToInput.AttachedInput.LocalRawPosition.y;
            BasisDebug.Log($"Player's raw eye height recalculated: {BasisLocalPlayer.Instance.CurrentHeight.PlayerEyeHeight}", BasisDebug.LogTag.Avatar);
        }
        else
        {
            BasisDebug.LogWarning("No attached input found for BasisLockToInput. Using the avatars height.", BasisDebug.LogTag.Avatar);
            BasisLocalPlayer.Instance.CurrentHeight.PlayerEyeHeight = BasisLocalPlayer.Instance.CurrentHeight.AvatarEyeHeight; // Set a reasonable default
        }
    }

    public static float GetDefaultOrLoadPlayerHeight()
    {
        float DefaultHeight = BasisLocalPlayer.DefaultPlayerEyeHeight;
        if (BasisDataStore.LoadFloat(FileNameAndExtension, DefaultHeight, out float FoundHeight))
        {
            return FoundHeight;
        }
        else
        {
            SaveHeight(FoundHeight);
            return FoundHeight;
        }
    }
    public static void SaveHeight()
    {
        float DefaultHeight = BasisLocalPlayer.DefaultPlayerEyeHeight;
        SaveHeight(DefaultHeight);
    }
    public static void SaveHeight(float EyeHeight)
    {
        BasisDataStore.SaveFloat(EyeHeight, FileNameAndExtension);
    }
}
