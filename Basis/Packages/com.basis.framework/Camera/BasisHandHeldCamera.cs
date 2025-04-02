using System.IO;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Drivers;

public class BasisHandHeldCamera : BasisHandHeldCameraInteractable
{
    public UniversalAdditionalCameraData CameraData;
    public Camera captureCamera;
    public RenderTexture renderTexture;

    public int captureWidth = 3840;
    public int captureHeight = 2160;

    public int PreviewCaptureWidth = 1920;
    public int PreviewCaptureHeight = 1080;

    public BasisMeshRendererCheck BasisMeshRendererCheck;
    public MeshRenderer Renderer;
    public Material Material;
    public Material actualMaterial;
    public string captureFormat = "EXR";
    public int msaaLevel = 2;
    public string picturesFolder;
    public int InstanceID;
    public int depth = 24;

    [SerializeField]
    public BasisHandHeldCameraUI HandHeld = new BasisHandHeldCameraUI();
    public BasisHandHeldCameraMetaData MetaData = new BasisHandHeldCameraMetaData();
    public async void OnEnable()
    {
        actualMaterial = Instantiate(Material);
        captureCamera.forceIntoRenderTexture = true;
        captureCamera.allowHDR = true;
        captureCamera.allowMSAA = true;
        captureCamera.useOcclusionCulling = true;
        captureCamera.usePhysicalProperties = true;
        captureCamera.targetTexture = renderTexture;
        HandHeld.Initalize(this);
        if (MetaData.Profile.TryGet(out MetaData.tonemapping))
        {
            ToggleToneMapping(TonemappingMode.Neutral);
        }
        SetResolution(PreviewCaptureWidth, PreviewCaptureHeight, AntialiasingQuality.Low);
        CameraData.allowHDROutput = true;
        CameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        CameraData.antialiasingQuality = AntialiasingQuality.High;
        await HandHeld.SaveSettings();
    }

    public new void OnDestroy()
    {
        base.OnDestroy();
        BasisMeshRendererCheck.Check -= VisibilityFlag;
    }

    public void CapturePhoto()
    {
        StartCoroutine(TakeScreenshot());
    }

    public void SetResolution(int width, int height, AntialiasingQuality AQ)
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
        renderTexture = new RenderTexture(width, height, depth, RenderTextureFormat.ARGBFloat)
        {
            antiAliasing = msaaLevel
        };
        renderTexture.Create();
        captureCamera.targetTexture = renderTexture;
        CameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        CameraData.antialiasingQuality = AQ;
        actualMaterial.SetTexture("_MainTex", renderTexture);
        actualMaterial.mainTexture = renderTexture;
        Renderer.sharedMaterial = actualMaterial;
        BasisDebug.Log($"Resolution set to {width}x{height}, Format: {captureFormat}, MSAA: {msaaLevel}");
    }

    public void ChangeResolution(int index)
    {
        if (index >= 0 && index < MetaData.resolutions.Length)
        {
            (captureWidth, captureHeight) = MetaData.resolutions[index];
        }
    }

    public void ChangeFormat(int index)
    {
        captureFormat = MetaData.formats[index];
        BasisDebug.Log($"Capture format changed to {captureFormat}");
    }

    public void ChangeMSAA(int index)
    {
        msaaLevel = MetaData.MSAALevels[index];
        BasisDebug.Log($"MSAA level changed to {msaaLevel}");
    }

    public IEnumerator TakeScreenshot()
    {
        SetResolution(captureWidth, captureHeight, AntialiasingQuality.High);
        yield return new WaitForEndOfFrame();
        BasisLocalCameraDriver.Instance.ScaleHeadToNormal();
        ToggleToneMapping(TonemappingMode.ACES);
        captureCamera.Render();
        RenderTexture.active = renderTexture;

        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height,
            captureFormat == "EXR" ? TextureFormat.RGBAFloat : TextureFormat.RGBA32, false);
        screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenshot.Apply();

        RenderTexture.active = null;
        SaveScreenshot(screenshot);
        ToggleToneMapping(TonemappingMode.Neutral);
        BasisLocalCameraDriver.Instance.ScaleheadToZero();
        SetResolution(PreviewCaptureWidth, PreviewCaptureHeight, AntialiasingQuality.Low);
    }

    public async void SaveScreenshot(Texture2D screenshot)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Screenshot_{timestamp}_{captureWidth}x{captureHeight}.{(captureFormat == "EXR" ? "exr" : "png")}";
        string path = GetSavePath(filename);
        try
        {
            byte[] bytes = captureFormat == "EXR" ? screenshot.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat) : screenshot.EncodeToPNG();
            await File.WriteAllBytesAsync(path, bytes);
            BasisDebug.Log("Screenshot saved to: " + path);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save screenshot: " + ex.Message);
        }
    }

    public string GetSavePath(string filename)
    {
#if UNITY_STANDALONE_WIN
        return Path.Combine(picturesFolder, filename);
#else
        return Path.Combine(Application.persistentDataPath, filename);
#endif
    }

    public void ToggleToneMapping(TonemappingMode MappingMode)
    {
        MetaData.tonemapping.mode.value = MappingMode;
    }
    public bool LastVisibilityState = false;
    private void VisibilityFlag(bool IsVisible)
    {
        if (IsVisible)
        {
            if (LastVisibilityState != IsVisible)
            {
                if (BasisLocalPlayer.Instance != null)
                {
                    captureCamera.enabled = true;
                    LastVisibilityState = true;
                    BasisLocalPlayer.Instance.AvatarDriver.TryActiveMatrixOverride(InstanceID);
                }
            }
        }
        else
        {
            if (LastVisibilityState != IsVisible)
            {
                if (BasisLocalPlayer.Instance != null)
                {
                    captureCamera.enabled = false;
                    LastVisibilityState = false;
                    BasisLocalPlayer.Instance.AvatarDriver.RemoveActiveMatrixOverride(InstanceID);
                }
            }
        }
    }
}
