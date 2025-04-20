using Basis.Scripts.Avatar;
using Basis.Scripts.Drivers;
using Basis.Scripts.Networking.Receivers;
using Basis.Scripts.UI.NamePlate;
using System.Threading.Tasks;
using UnityEngine;
using static SerializableBasis;

namespace Basis.Scripts.BasisSdk.Players
{
    [System.Serializable]
    public class BasisRemotePlayer : BasisPlayer
    {
        [SerializeField]
        public BasisRemoteBoneDriver RemoteBoneDriver = new BasisRemoteBoneDriver();
        [SerializeField]
        public BasisRemoteAvatarDriver RemoteAvatarDriver = new BasisRemoteAvatarDriver();
        [SerializeField]
        public BasisNetworkReceiver NetworkReceiver;
        public bool HasEvents = false;
        public bool LockAvatarFromChanging;
        public bool OutOfRangeFromLocal = false;
        public ClientAvatarChangeMessage CACM;
        public Transform NetworkedVoice;
        [HideInInspector]
        public BasisLoadableBundle AlwaysRequestedAvatar;
        public byte AlwaysRequestedMode;
        [SerializeField]
        public BasisRemoteEyeFollow EyeFollow = new BasisRemoteEyeFollow();

        public bool InAvatarRange = true;

        public async Task RemoteInitialize(ClientAvatarChangeMessage cACM, PlayerMetaDataMessage PlayerMetaDataMessage)
        {
            CACM = cACM;
            DisplayName = PlayerMetaDataMessage.playerDisplayName;
            UUID = PlayerMetaDataMessage.playerUUID;
            IsLocal = false;
            RemoteBoneDriver.CreateInitialArrays(this.transform, false);
            RemoteBoneDriver.InitializeRemote();
            if (HasEvents == false)
            {
                RemoteAvatarDriver.CalibrationComplete += RemoteCalibration;
                HasEvents = true;
            }
            await BasisRemoteNamePlate.LoadRemoteNamePlate(this);
        }
        public async Task LoadAvatarFromInitial(ClientAvatarChangeMessage CACM)
        {
            if (BasisAvatar == null)
            {
                this.CACM = CACM;
                BasisLoadableBundle BasisLoadedBundle = BasisBundleConversionNetwork.ConvertNetworkBytesToBasisLoadableBundle(CACM.byteArray);
                AlwaysRequestedAvatar = BasisLoadedBundle;
                BasisPlayerSettingsData BasisPlayerSettingsData = await BasisPlayerSettingsManager.RequestPlayerSettings(UUID);
                if (BasisPlayerSettingsData.AvatarVisible)
                {
                    await BasisAvatarFactory.LoadAvatarRemote(this, CACM.loadMode, BasisLoadedBundle);
                }
                else
                {
                    BasisAvatarFactory.DeleteLastAvatar(this, false);
                    BasisAvatarFactory.LoadLoadingAvatar(this, BasisAvatarFactory.LoadingAvatar.BasisLocalEncryptedBundle.LocalConnectorPath);
                }
            }
        }
        public void OnDestroy()
        {
            if (HasEvents)
            {
                if (RemoteAvatarDriver != null)
                {
                    RemoteAvatarDriver.CalibrationComplete -= RemoteCalibration;
                    HasEvents = false;
                }
            }
            if (FacialBlinkDriver != null)
            {
                FacialBlinkDriver.OnDestroy();
            }
            if(EyeFollow != null)
            {
                EyeFollow.OnDestroy();
            }

        }
        public async void CreateAvatar(byte Mode, BasisLoadableBundle BasisLoadableBundle)
        {
            AlwaysRequestedMode = Mode;
            AlwaysRequestedAvatar = BasisLoadableBundle;
            BasisPlayerSettingsData BasisPlayerSettingsData = await BasisPlayerSettingsManager.RequestPlayerSettings(UUID);
            if (BasisPlayerSettingsData.AvatarVisible && InAvatarRange)
            {
                if (BasisLoadableBundle.BasisLocalEncryptedBundle.LocalConnectorPath == BasisAvatarFactory.LoadingAvatar.BasisLocalEncryptedBundle.LocalConnectorPath)
                {
                    BasisDebug.Log("Avatar Load string was null or empty using fallback!");
                    await BasisAvatarFactory.LoadAvatarRemote(this, BasisPlayer.LoadModeError, BasisLoadableBundle);
                }
                else
                {
                    BasisDebug.Log("loading avatar from " + BasisLoadableBundle.BasisLocalEncryptedBundle.LocalConnectorPath + " with net mode " + Mode);
                    if (LockAvatarFromChanging == false)
                    {
                        await BasisAvatarFactory.LoadAvatarRemote(this, Mode, BasisLoadableBundle);
                    }
                }
            }
            else
            {
                BasisAvatarFactory.DeleteLastAvatar(this,false);
                BasisAvatarFactory.LoadLoadingAvatar(this, BasisAvatarFactory.LoadingAvatar.BasisLocalEncryptedBundle.LocalConnectorPath);
            }
        }
        public void RemoteCalibration()
        {
            RemoteBoneDriver.OnCalibration(this);
        }
    }
}
