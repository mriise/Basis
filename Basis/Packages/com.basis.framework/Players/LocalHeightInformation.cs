namespace Basis.Scripts.BasisSdk.Players
{
    [System.Serializable]
    public class LocalHeightInformation
    {
        public string AvatarName;
        public float PlayerEyeHeight = 1.64f;
        public float AvatarEyeHeight = 1.64f;
        public float RatioPlayerToAvatarScale = 1f;
        public float EyeRatioPlayerToDefaultScale = 1f;
        public float EyeRatioAvatarToAvatarDefaultScale = 1f; // should be used for the player

        public void CopyTo(LocalHeightInformation target)
        {
            if (target == null) return;

            target.AvatarName = this.AvatarName;
            target.PlayerEyeHeight = this.PlayerEyeHeight;
            target.AvatarEyeHeight = this.AvatarEyeHeight;
            target.RatioPlayerToAvatarScale = this.RatioPlayerToAvatarScale;
            target.EyeRatioPlayerToDefaultScale = this.EyeRatioPlayerToDefaultScale;
            target.EyeRatioAvatarToAvatarDefaultScale = this.EyeRatioAvatarToAvatarDefaultScale;
        }
    }
}
