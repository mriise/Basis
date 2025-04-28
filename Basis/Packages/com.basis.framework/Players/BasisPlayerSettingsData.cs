using UnityEngine;

[System.Serializable]
public class BasisPlayerSettingsData
{
    public string UUID;
    public float VolumeLevel;
    public bool AvatarVisible;

    public BasisPlayerSettingsData(string uuid, float volume, bool avatarVisible)
    {
        UUID = uuid;
        VolumeLevel = volume;
        AvatarVisible = avatarVisible;
    }
}
