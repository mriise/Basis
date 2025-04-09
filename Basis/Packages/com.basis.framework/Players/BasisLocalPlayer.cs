using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Basis.Scripts.Drivers;
using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Avatar;
using Basis.Scripts.Common;
using System.Collections.Generic;
using Basis.Scripts.UI.UI_Panels;
using Basis.Scripts.TransformBinders.BoneControl;
using Unity.Mathematics;
namespace Basis.Scripts.BasisSdk.Players
{
    public class BasisLocalPlayer : BasisPlayer
    {
        public static BasisLocalPlayer Instance;
        public static bool PlayerReady = false;
        public static Action OnLocalPlayerCreatedAndReady;
        public static Action OnLocalPlayerCreated;
        public BasisCharacterController.BasisCharacterController Move;
        public event Action OnLocalAvatarChanged;
        public event Action OnSpawnedEvent;

        public static float DefaultPlayerEyeHeight = 1.64f;
        public static float DefaultAvatarEyeHeight = 1.64f;
        public LocalHeightInformation CurrentHeight;
        public LocalHeightInformation LastHeight;
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

        /// <summary>
        /// the bool when true is the final size
        /// the bool when false is not the final size
        /// use the bool to 
        /// </summary>
        public Action OnPlayersHeightChanged;

        public BasisLocalBoneDriver LocalBoneDriver;
        public BasisLocalAvatarDriver AvatarDriver;
        //   public BasisFootPlacementDriver FootPlacementDriver;
        public BasisAudioAndVisemeDriver VisemeDriver;
        public BasisLocalCameraDriver CameraDriver;

        [SerializeField]
        public LayerMask GroundMask;
        public static string LoadFileNameAndExtension = "LastUsedAvatar.BAS";
        public bool HasEvents = false;
        public MicrophoneRecorder MicrophoneRecorder;
        public bool SpawnPlayerOnSceneLoad = true;
        public const string DefaultAvatar = "LoadingAvatar";
        public BasisBoneControl Head;
        public BasisBoneControl Hips;
        public async Task LocalInitialize()
        {
            if (BasisHelpers.CheckInstance(Instance))
            {
                Instance = this;
            }
            MicrophoneRecorder.OnPausedAction += OnPausedEvent;
            OnLocalPlayerCreated?.Invoke();
            IsLocal = true;
            LocalBoneDriver.CreateInitialArrays(LocalBoneDriver.transform, true);
            LocalBoneDriver.FindBone(out Head, BasisBoneTrackedRole.Head);
            LocalBoneDriver.FindBone(out Hips, BasisBoneTrackedRole.Hips);
            LocalBoneDriver.FindBone(out Mouth, BasisBoneTrackedRole.Mouth);

            BasisDeviceManagement.Instance.InputActions.Initialize(this);
            CameraDriver.gameObject.SetActive(true);
            //  FootPlacementDriver = BasisHelpers.GetOrAddComponent<BasisFootPlacementDriver>(this.gameObject);
            //  FootPlacementDriver.Initialize();
            Move.Initialize();
            if (HasEvents == false)
            {
                OnLocalAvatarChanged += OnCalibration;
                SceneManager.sceneLoaded += OnSceneLoadedCallback;
                HasEvents = true;
            }
            bool LoadedState = BasisDataStore.LoadAvatar(LoadFileNameAndExtension, DefaultAvatar, BasisPlayer.LoadModeLocal, out BasisDataStore.BasisSavedAvatar LastUsedAvatar);
            if (LoadedState)
            {
                await LoadInitialAvatar(LastUsedAvatar);
            }
            else
            {
                await CreateAvatar(BasisPlayer.LoadModeLocal, BasisAvatarFactory.LoadingAvatar);
            }
            if (MicrophoneRecorder == null)
            {
                MicrophoneRecorder = BasisHelpers.GetOrAddComponent<MicrophoneRecorder>(this.gameObject);
            }
            MicrophoneRecorder.TryInitialize();
            PlayerReady = true;
            OnLocalPlayerCreatedAndReady?.Invoke();
            BasisSceneFactory BasisSceneFactory = FindFirstObjectByType<BasisSceneFactory>(FindObjectsInactive.Exclude);
            if (BasisSceneFactory != null)
            {
                BasisScene BasisScene = FindFirstObjectByType<BasisScene>(FindObjectsInactive.Exclude);
                if (BasisScene != null)
                {
                    BasisSceneFactory.Initalize(BasisScene);
                }
                else
                {
                    BasisDebug.LogError("Cant Find Basis Scene");
                }
            }
            else
            {
                BasisDebug.LogError("Cant Find Scene Factory");
            }
            BasisUILoadingBar.Initalize();

        }
        public async Task LoadInitialAvatar(BasisDataStore.BasisSavedAvatar LastUsedAvatar)
        {
            if (BasisLoadHandler.IsMetaDataOnDisc(LastUsedAvatar.UniqueID, out OnDiscInformation info))
            {
                await BasisDataStoreAvatarKeys.LoadKeys();
                List<BasisDataStoreAvatarKeys.AvatarKey> activeKeys = BasisDataStoreAvatarKeys.DisplayKeys();
                foreach (BasisDataStoreAvatarKeys.AvatarKey Key in activeKeys)
                {
                    if (Key.Url == LastUsedAvatar.UniqueID)
                    {
                        BasisLoadableBundle bundle = new BasisLoadableBundle
                        {
                            BasisRemoteBundleEncrypted = info.StoredRemote,
                            BasisBundleConnector = new BasisBundleConnector("1", new BasisBundleDescription("Loading Avatar", "Loading Avatar"), new BasisBundleGenerated[] { new BasisBundleGenerated() }),
                            BasisLocalEncryptedBundle = info.StoredLocal,
                            UnlockPassword = Key.Pass
                        };
                        BasisDebug.Log("loading previously loaded avatar");
                        await CreateAvatar(LastUsedAvatar.loadmode, bundle);
                        return;
                    }
                }
                BasisDebug.Log("failed to load last used : no key found to load but was found on disc");
                await CreateAvatar(BasisPlayer.LoadModeLocal, BasisAvatarFactory.LoadingAvatar);
            }
            else
            {
                BasisDebug.Log("failed to load last used : url was not found on disc");
                await CreateAvatar(BasisPlayer.LoadModeLocal, BasisAvatarFactory.LoadingAvatar);
            }
        }
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            BasisAvatarStrainJiggleDriver.PrepareTeleport();
            BasisDebug.Log("Teleporting");
            Move.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            Move.enabled = true;
            if (AvatarDriver != null && AvatarDriver.AnimatorDriver != null)
            {
                AvatarDriver.AnimatorDriver.HandleTeleport();
            }
            BasisAvatarStrainJiggleDriver.FinishTeleport();
            OnSpawnedEvent?.Invoke();
        }
        public void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
        {
            if (BasisSceneFactory.Instance != null && SpawnPlayerOnSceneLoad)
            {
                //swap over to on scene load
                BasisSceneFactory.Instance.SpawnPlayer(this);
            }
        }
        public async Task CreateAvatar(byte mode, BasisLoadableBundle BasisLoadableBundle)
        {
            await BasisAvatarFactory.LoadAvatarLocal(this, mode, BasisLoadableBundle);
            BasisDataStore.SaveAvatar(BasisLoadableBundle.BasisRemoteBundleEncrypted.CombinedURL, mode, LoadFileNameAndExtension);
            OnLocalAvatarChanged?.Invoke();
        }
        public void OnCalibration()
        {
            if (VisemeDriver == null)
            {
                VisemeDriver = BasisHelpers.GetOrAddComponent<BasisAudioAndVisemeDriver>(this.gameObject);
            }
            VisemeDriver.TryInitialize(this);
            if (HasCalibrationEvents == false)
            {
                MicrophoneRecorderBase.OnHasAudio += DriveAudioToViseme;
                MicrophoneRecorderBase.OnHasSilence += DriveAudioToViseme;
                HasCalibrationEvents = true;
            }
        }
        public bool HasCalibrationEvents = false;
        public void OnDestroy()
        {
            if (HasEvents)
            {
                OnLocalAvatarChanged -= OnCalibration;
                SceneManager.sceneLoaded -= OnSceneLoadedCallback;
                HasEvents = false;
            }
            if (HasCalibrationEvents)
            {
                MicrophoneRecorderBase.OnHasAudio -= DriveAudioToViseme;
                MicrophoneRecorderBase.OnHasSilence -= DriveAudioToViseme;
                HasCalibrationEvents = false;
            }
            if (VisemeDriver != null)
            {
                GameObject.Destroy(VisemeDriver);
            }
            MicrophoneRecorder.OnPausedAction -= OnPausedEvent;
            LocalBoneDriver.DeInitalzeGizmos();
            BasisUILoadingBar.DeInitalize();
        }
        public void DriveAudioToViseme()
        {
            VisemeDriver.ProcessAudioSamples(MicrophoneRecorder.processBufferArray, 1, MicrophoneRecorder.processBufferArray.Length);
        }
        private void OnPausedEvent(bool IsPaused)
        {
            if (IsPaused)
            {
                if (VisemeDriver.uLipSyncBlendShape != null)
                {
                    VisemeDriver.uLipSyncBlendShape.maxVolume = 0;
                    VisemeDriver.uLipSyncBlendShape.minVolume = 0;
                }
            }
            else
            {
                if (VisemeDriver.uLipSyncBlendShape != null)
                {
                    VisemeDriver.uLipSyncBlendShape.maxVolume = -1.5f;
                    VisemeDriver.uLipSyncBlendShape.minVolume = -2.5f;
                }
            }
        }
        public bool ToggleAvatarSim = false;
        public float currentDistance;
        public float3 direction;
        public float overshoot;
        public float3 correction;
        public float3 output;
        public void SimulateAvatar()
        {
            if (BasisAvatar == null)
            {
                return;
            }


            // Current world positions
            Vector3 headPosition = Head.OutgoingWorldData.position;
            Vector3 hipsPosition = Hips.OutgoingWorldData.position;
            currentDistance = Vector3.Distance(headPosition, hipsPosition);


            if (currentDistance <= AvatarDriver.MaxExtendedDistance)
            {
                // Within range: follow hips freely
                output = -Hips.TposeLocal.position;
            }
            else
            {
                // Direction from hips to head
                direction = (hipsPosition - headPosition).normalized;
                // Head too far: pull hips toward head to reduce the stretch
                overshoot = currentDistance - AvatarDriver.MaxExtendedDistance;

                // Move hips slightly toward head to restore default distance
                correction = direction * overshoot;

                // Apply correction to T-pose hips
                float3 correctedHips = Hips.TposeLocal.position + correction;

                // Negate to match transform.localPosition logic
                output = -correctedHips;
            }

            //    BasisAvatar.transform.position = Hips.OutgoingWorldData.position + output;

            Vector3 parentWorldPosition = Hips.OutgoingWorldData.position;
            Quaternion parentWorldRotation = Hips.OutgoingWorldData.rotation;

            Vector3 childWorldPosition = parentWorldPosition + parentWorldRotation * output;
            Quaternion childWorldRotation = parentWorldRotation * quaternion.identity;
            BasisAvatar.transform.SetPositionAndRotation(childWorldPosition, childWorldRotation);

            AvatarDriver.Simulate();
            if (HasJiggles)
            {
                BasisAvatarStrainJiggleDriver.Simulate();
            }
        }
    }
}
