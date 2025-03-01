using Basis.Scripts.BasisSdk.Players;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Basis.Scripts.UI.UI_Panels
{
    public class BasisUIAvatarSelection : BasisUIBase
    {
        [SerializeField] public List<BasisLoadableBundle> preLoadedBundles = new List<BasisLoadableBundle>();
        [SerializeField] public RectTransform ParentedAvatarButtons;
        [SerializeField] public GameObject ButtonPrefab;

        public const string AvatarSelection = "BasisUIAvatarSelection";

        [SerializeField] public TMP_InputField ConnectorField;
        [SerializeField] public TMP_InputField PasswordField;

        [SerializeField] public Button AddAvatarApply;
        [SerializeField] public BasisProgressReport Report = new BasisProgressReport();
        [SerializeField]
        public List<BasisLoadableBundle> avatarUrlsRuntime = new List<BasisLoadableBundle>();
        [SerializeField]
        public List<GameObject> createdCopies = new List<GameObject>();
        public CancellationToken CancellationToken = new CancellationToken();
        private async void Start()
        {
            BasisDataStoreAvatarKeys.DisplayKeys();
            AddAvatarApply.onClick.AddListener(AddAvatar);
            await Initialize();
        }

        public override void InitalizeEvent()
        {
            BasisCursorManagement.UnlockCursor(AvatarSelection);
            BasisUINeedsVisibleTrackers.Instance.Add(this);
        }

        private async void AddAvatar()
        {
            if (string.IsNullOrEmpty(ConnectorField.text) || string.IsNullOrEmpty(PasswordField.text))
            {
                BasisDebug.LogError("All fields must be filled.");
                return;
            }

            // Check if the avatar key already exists to prevent duplicates
            List<BasisDataStoreAvatarKeys.AvatarKey> activeKeys = BasisDataStoreAvatarKeys.DisplayKeys();
            bool keyExists = activeKeys.Exists(key => key.Url == ConnectorField.text && key.Pass == PasswordField.text);

            if (keyExists)
            {
                Debug.LogWarning("The avatar key with the same URL and Password already exists. No duplicate will be added.");
                return;
            }

            // Avoid duplicate in avatarUrlsRuntime
            if (avatarUrlsRuntime.Exists(b => b.BasisRemoteBundleEncrypted.CombinedURL == ConnectorField.text))
            {
                Debug.LogWarning("Avatar with the same Meta URL already exists in runtime list.");
                return;
            }

            BasisLoadableBundle loadableBundle = new BasisLoadableBundle
            {
                UnlockPassword = PasswordField.text,
                BasisRemoteBundleEncrypted = new BasisRemoteEncyptedBundle
                {
                    CombinedURL = ConnectorField.text
                },
                BasisBundleConnector = new BasisBundleConnector(),
                BasisLocalEncryptedBundle = new BasisStoredEncryptedBundle()
            };

            await BasisLocalPlayer.Instance.CreateAvatar(0, loadableBundle);

            var avatarKey = new BasisDataStoreAvatarKeys.AvatarKey
            {
                Url = ConnectorField.text,
                Pass = PasswordField.text,
            };

            await BasisDataStoreAvatarKeys.AddNewKey(avatarKey);
            await Initialize();
        }

        private async Task Initialize()
        {
            ClearCreatedCopies();
            avatarUrlsRuntime.Clear();
            avatarUrlsRuntime.AddRange(preLoadedBundles);
            await BasisDataStoreAvatarKeys.LoadKeys();
            for (int Index = 0; Index < preLoadedBundles.Count; Index++)
            {
                BasisLoadableBundle loadableBundle = preLoadedBundles[Index];
                BasisDataStoreAvatarKeys.AvatarKey Key = new BasisDataStoreAvatarKeys.AvatarKey() { Pass = loadableBundle.UnlockPassword, Url = loadableBundle.BasisRemoteBundleEncrypted.CombinedURL };

                // Prevent duplicate keys in the store
                if (!BasisDataStoreAvatarKeys.DisplayKeys().Exists(k => k.Url == Key.Url && k.Pass == Key.Pass))
                {
                    await BasisDataStoreAvatarKeys.AddNewKey(Key);
                }
            }

            List<BasisDataStoreAvatarKeys.AvatarKey> activeKeys = BasisDataStoreAvatarKeys.DisplayKeys();
            for (int Index = 0; Index < activeKeys.Count; Index++)
            {
                if (!BasisLoadHandler.IsMetaDataOnDisc(activeKeys[Index].Url, out var info))
                {
                    if (activeKeys[Index].Url != BasisLocalPlayer.DefaultAvatar  || activeKeys[Index].Url != string.Empty)
                    {
                        BasisDebug.LogError("Missing File on Disc For " + activeKeys[Index].Url);
                    }
                    await BasisDataStoreAvatarKeys.RemoveKey(activeKeys[Index]);
                    continue;
                }

                // Prevent duplicates in avatarUrlsRuntime
                if (!avatarUrlsRuntime.Exists(b => b.BasisRemoteBundleEncrypted.CombinedURL == activeKeys[Index].Url))
                {
                    BasisLoadableBundle bundle = new BasisLoadableBundle
                    {
                        BasisRemoteBundleEncrypted = info.StoredRemote,
                        BasisBundleConnector = new BasisBundleConnector
                        {
                            BasisBundleDescription = new BasisBundleDescription(),
                            BasisBundleGenerated = new BasisBundleGenerated[] { new BasisBundleGenerated() },
                            UniqueVersion = ""
                        },
                        BasisLocalEncryptedBundle = info.StoredLocal,
                        UnlockPassword = activeKeys[Index].Pass
                    };
                    avatarUrlsRuntime.Add(bundle);
                }
            }

            await CreateAvatarButtons(activeKeys);
        }

        private async Task CreateAvatarButtons(List<BasisDataStoreAvatarKeys.AvatarKey> activeKeys)
        {
            foreach (var bundle in avatarUrlsRuntime)
            {
                // Ensure no duplicate buttons are created
                if (createdCopies.Exists(copy => copy.name == bundle.BasisRemoteBundleEncrypted.CombinedURL))
                {
                    Debug.LogWarning("Button for this avatar already exists: " + bundle.BasisRemoteBundleEncrypted.CombinedURL);
                    continue;
                }

                var buttonObject = Instantiate(ButtonPrefab, ParentedAvatarButtons);
                buttonObject.name = bundle.BasisRemoteBundleEncrypted.CombinedURL;
                buttonObject.SetActive(true);

                if (buttonObject.TryGetComponent<Button>(out var button))
                {
                    button.onClick.AddListener(() => OnButtonPressed(bundle));

                    TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        BasisTrackedBundleWrapper wrapper = new BasisTrackedBundleWrapper
                        {
                            LoadableBundle = bundle
                        };

                        try
                        {
                            await BasisLoadHandler.HandleBundleAndMetaLoading(wrapper, Report, CancellationToken);
                            buttonText.text = wrapper.LoadableBundle.BasisBundleConnector.BasisBundleDescription.AssetBundleName;
                        }
                        catch (Exception E)
                        {
                            BasisDebug.LogError(E);
                            BasisLoadHandler.RemoveDiscInfo(bundle.BasisRemoteBundleEncrypted.CombinedURL);
                            continue;
                        }
                    }
                }

                createdCopies.Add(buttonObject);
            }
        }

        private void ClearCreatedCopies()
        {
            foreach (var copy in createdCopies)
            {
                Destroy(copy);
            }
            createdCopies.Clear();
        }

        private async void OnButtonPressed(BasisLoadableBundle avatarLoadRequest)
        {
            if (BasisLocalPlayer.Instance != null)
            {
                if (avatarLoadRequest.BasisBundleConnector.GetPlatform(out BasisBundleGenerated platformBundle))
                {
                    string assetMode = platformBundle.AssetMode;
                    byte mode = !string.IsNullOrEmpty(assetMode) && byte.TryParse(assetMode, out var result) ? result : (byte)0;
                    await BasisLocalPlayer.Instance.CreateAvatar(mode, avatarLoadRequest);
                }
                else
                {
                    BasisDebug.LogError("Missing Platform " + Application.platform);
                }
            }
        }
        public override void DestroyEvent()
        {
            BasisCursorManagement.LockCursor(AvatarSelection);
            BasisUINeedsVisibleTrackers.Instance.Remove(this);
        }
    }
}
