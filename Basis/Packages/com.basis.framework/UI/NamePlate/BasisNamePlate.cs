using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Device_Management;
using Basis.Scripts.Networking;
using Basis.Scripts.TransformBinders.BoneControl;
using System.Collections;
using TMPro;
using UnityEngine;
namespace Basis.Scripts.UI.NamePlate
{
    public abstract class BasisNamePlate : MonoBehaviour
    {
        public Vector3 dirToCamera;
        public BasisBoneControl HipTarget;
        public BasisBoneControl MouthTarget;
        public TextMeshPro Text;
        public SpriteRenderer Loadingbar;
        public MeshFilter Filter;
        public TextMeshPro Loadingtext;
        public float YHeightMultiplier = 1.25f;
        public BasisRemotePlayer BasisRemotePlayer;
        public SpriteRenderer namePlateImage;
        public Color NormalColor;
        public Color IsTalkingColor;
        public Color OutOfRangeColor;
        [SerializeField]
        public float transitionDuration = 0.3f;
        [SerializeField]
        public float returnDelay = 0.4f;
        public Coroutine colorTransitionCoroutine;
        public Coroutine returnToNormalCoroutine;
        public Vector3 cachedDirection;
        public Quaternion cachedRotation;
        public bool HasRendererCheckWiredUp = false;
        public bool IsVisible = true;
        public bool HasProgressBarVisible = false;
        public Mesh bakedMesh;
        private WaitForSeconds cachedReturnDelay;
        private WaitForEndOfFrame cachedEndOfFrame;
        /// <summary>
        /// can only be called once after that the text is nuked and a mesh render is just used with a filter
        /// </summary>
        /// <param name="hipTarget"></param>
        /// <param name="basisRemotePlayer"></param>
        public void Initalize(BasisBoneControl hipTarget, BasisRemotePlayer basisRemotePlayer)
        {
            cachedReturnDelay = new WaitForSeconds(returnDelay);
            cachedEndOfFrame = new WaitForEndOfFrame();
            BasisRemotePlayer = basisRemotePlayer;
            HipTarget = hipTarget;
            MouthTarget = BasisRemotePlayer.MouthControl;
            Text.text = BasisRemotePlayer.DisplayName;
            BasisRemotePlayer.ProgressReportAvatarLoad.OnProgressReport += ProgressReport;
            BasisRemotePlayer.AudioReceived += OnAudioReceived;
            BasisRemotePlayer.OnAvatarSwitched += RebuildRenderCheck;
            BasisRemotePlayer.OnAvatarSwitchedFallBack += RebuildRenderCheck;
            RemoteNamePlateDriver.Instance.AddNamePlate(this);
            Loadingtext.enableVertexGradient = false;
            // Text.enableCulling = true;
            // Text.enableAutoSizing = false;
            GenerateText();
            GameObject.Destroy(Text.gameObject);
            if (BasisDeviceManagement.IsMobile())
            {
                NormalColor.a = 1;
                IsTalkingColor.a = 1;
                OutOfRangeColor.a = 1;
            }
        }
        public void GenerateText()
        {
            // Force update to ensure the mesh is generated
            Text.ForceMeshUpdate();
            // Store the generated mesh
            bakedMesh = Mesh.Instantiate(Text.mesh);
            Filter.sharedMesh = bakedMesh;
        }
        public void RebuildRenderCheck()
        {
            if (HasRendererCheckWiredUp)
            {
                DeInitalizeCallToRender();
            }
            HasRendererCheckWiredUp = false;
            if (BasisRemotePlayer != null && BasisRemotePlayer.FaceRenderer != null)
            {
                BasisDebug.Log("Wired up Renderer Check For Blinking");
                BasisRemotePlayer.FaceRenderer.Check += UpdateFaceVisibility;
                BasisRemotePlayer.FaceRenderer.DestroyCalled += AvatarUnloaded;
                UpdateFaceVisibility(BasisRemotePlayer.FaceisVisible);
                HasRendererCheckWiredUp = true;
            }
        }

        private void AvatarUnloaded()
        {
            UpdateFaceVisibility(true);
        }

        private void UpdateFaceVisibility(bool State)
        {
            IsVisible = State;
            gameObject.SetActive(State);
            if (IsVisible == false)
            {
                if (returnToNormalCoroutine != null)
                {
                    StopCoroutine(returnToNormalCoroutine);
                }
                if (colorTransitionCoroutine != null)
                {
                    StopCoroutine(colorTransitionCoroutine);
                }
            }
        }
        public void OnAudioReceived(bool hasRealAudio)
        {
            if (IsVisible)
            {
                Color targetColor;
                if (BasisRemotePlayer.OutOfRangeFromLocal)
                {
                    targetColor = hasRealAudio ? OutOfRangeColor : NormalColor;
                }
                else
                {
                    targetColor = hasRealAudio ? IsTalkingColor : NormalColor;
                }
                BasisNetworkManagement.MainThreadContext.Post(_ =>
                {
                    if (this != null)
                    {
                        if (isActiveAndEnabled)
                        {
                            if (colorTransitionCoroutine != null)
                            {
                                StopCoroutine(colorTransitionCoroutine);
                            }
                            if (targetColor != CurrentColor)
                            {
                                colorTransitionCoroutine = StartCoroutine(TransitionColor(targetColor));
                            }
                        }
                    }
                }, null);
            }
        }
        public Color CurrentColor;
        private IEnumerator TransitionColor(Color targetColor)
        {
            CurrentColor = namePlateImage.color;
            float elapsedTime = 0f;

            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float lerpProgress = Mathf.Clamp01(elapsedTime / transitionDuration);
                namePlateImage.color = Color.Lerp(CurrentColor, targetColor, lerpProgress);
                yield return cachedEndOfFrame;
            }

            namePlateImage.color = targetColor;
            CurrentColor = targetColor;
            colorTransitionCoroutine = null;

            if (targetColor == IsTalkingColor)
            {
                if (returnToNormalCoroutine != null)
                {
                    StopCoroutine(returnToNormalCoroutine);
                }
                returnToNormalCoroutine = StartCoroutine(DelayedReturnToNormal());
            }
        }

        private IEnumerator DelayedReturnToNormal()
        {
            yield return cachedReturnDelay;
            yield return StartCoroutine(TransitionColor(NormalColor));
            returnToNormalCoroutine = null;
        }
        public void OnDestroy()
        {
            BasisRemotePlayer.ProgressReportAvatarLoad.OnProgressReport -= ProgressReport;
            BasisRemotePlayer.AudioReceived -= OnAudioReceived;
            DeInitalizeCallToRender();
            RemoteNamePlateDriver.Instance.RemoveNamePlate(this);
        }
        public void DeInitalizeCallToRender()
        {
            if (HasRendererCheckWiredUp && BasisRemotePlayer != null && BasisRemotePlayer.FaceRenderer != null)
            {
                BasisRemotePlayer.FaceRenderer.Check -= UpdateFaceVisibility;
                BasisRemotePlayer.FaceRenderer.DestroyCalled -= AvatarUnloaded;
            }
        }
        public void ProgressReport(string UniqueID, float progress, string info)
        {
            BasisDeviceManagement.EnqueueOnMainThread(() =>
              {
                  if (progress == 100)
                  {
                      Loadingtext.gameObject.SetActive(false);
                      Loadingbar.gameObject.SetActive(false);
                      HasProgressBarVisible = false;
                  }
                  else
                  {
                      if (HasProgressBarVisible == false)
                      {
                          Loadingbar.gameObject.SetActive(true);
                          Loadingtext.gameObject.SetActive(true);
                          HasProgressBarVisible = true;
                      }

                      if (Loadingtext.text != info)
                      {
                          Loadingtext.text = info;
                      }
                      UpdateProgressBar(UniqueID, progress);
                  }
              });
        }
        public void UpdateProgressBar(string UniqueID,float progress)
        {
            Vector2 scale = Loadingbar.size;
            float NewX = progress / 2;
            if (scale.x != NewX)
            {
                scale.x = NewX;
                Loadingbar.size = scale;
            }
        }
    }
}
