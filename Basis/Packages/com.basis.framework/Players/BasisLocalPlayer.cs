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
using static Basis.Scripts.Drivers.BaseBoneDriver;
using Basis.Scripts.Animator_Driver;
namespace Basis.Scripts.BasisSdk.Players
{
    public class BasisLocalPlayer : BasisPlayer
    {
        public static BasisLocalPlayer Instance;
        public static bool PlayerReady = false;
        public static Action OnLocalPlayerCreatedAndReady;
        public static Action OnLocalPlayerCreated;
        public BasisCharacterController.BasisCharacterController LocalMoveDriver;
        public event Action OnLocalAvatarChanged;
        public event Action OnSpawnedEvent;

        public static float DefaultPlayerEyeHeight = 1.64f;
        public static float DefaultAvatarEyeHeight = 1.64f;
        public LocalHeightInformation CurrentHeight;
        public LocalHeightInformation LastHeight;
        public BasisLocalAnimatorDriver AnimatorDriver;
        /// <summary>
        /// the bool when true is the final size
        /// the bool when false is not the final size
        /// use the bool to 
        /// </summary>
        public Action OnPlayersHeightChanged;

        public BasisLocalBoneDriver LocalBoneDriver;
        public BasisLocalAvatarDriver LocalAvatarDriver;
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
        public OrderedDelegate AfterFinalMove = new OrderedDelegate();

        public bool HasCalibrationEvents = false;
        public bool ToggleAvatarSim = false;
        public float currentDistance;
        public float3 direction;
        public float overshoot;
        public float3 correction;
        public float3 output;
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
            LocalMoveDriver.Initialize();
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
            LocalMoveDriver.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            LocalMoveDriver.enabled = true;
            if (AnimatorDriver != null)
            {
                AnimatorDriver.HandleTeleport();
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
            LocalBoneDriver.DeInitializeGizmos();
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
        public void Simulate()
        {
            //moves all bones to where they belong
            LocalBoneDriver.SimulateBonePositions();

            //moves Avatar Transform to where it belongs
          Quaternion Rotation =  MoveAvatar();
            //Simulate Final Destination of IK
            LocalAvatarDriver.SimulateIKDestinations(Rotation);

            //process Animator and IK processes. also handles fingers
            LocalAvatarDriver.SimulateAnimatorAndIk();

            //we move the player at the very end after everything has been processed.
            LocalMoveDriver.SimulateMovement();

            //Apply Animator Weights
            AnimatorDriver.SimulateAnimator();

            //now that everything has been processed jiggles can move.
            if (HasJiggles)
            {
                //we use distance = 0 as the local avatar jiggles should always be processed.
                BasisAvatarStrainJiggleDriver.Simulate(0);
            }
            //now that everything has been processed lets update WorldPosition in BoneDriver.
            //this is so AfterFinalMove can use world position coords. (stops Laggy pickups)
            LocalBoneDriver.PostSimulateBonePositions();

            //now other things can move like UI and NON-CHILDREN OF BASISLOCALPLAYER.
            AfterFinalMove?.Invoke();
        }
        public Quaternion MoveAvatar()
        {
            if (BasisAvatar == null)
            {
                return Quaternion.identity;
            }

            // Current world positions
            Vector3 headPosition = Head.OutgoingWorldData.position;//OutgoingWorldData is out of date here potentially?
            Vector3 hipsPosition = Hips.OutgoingWorldData.position;
            currentDistance = Vector3.Distance(headPosition, hipsPosition);


            if (currentDistance <= LocalAvatarDriver.MaxExtendedDistance)
            {
                // Within range: follow hips freely
                output = -Hips.TposeLocal.position;
            }
            else
            {
                // Direction from hips to head
                direction = (hipsPosition - headPosition).normalized;
                // Head too far: pull hips toward head to reduce the stretch
                overshoot = currentDistance - LocalAvatarDriver.MaxExtendedDistance;

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
            return childWorldRotation;
        }
    }
}
