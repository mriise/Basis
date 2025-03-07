using UnityEngine;
using System;
public abstract class MicrophoneRecorderBase : MonoBehaviour
{
    public static Action OnHasAudio;
    public static Action OnHasSilence; // Event triggered when silence is detected
    public static AudioClip clip;
    public static bool IsInitialize = false;
    public static string MicrophoneDevice = null;
    public static int ProcessBufferLength;
    public static float Volume = 1; // Volume adjustment factor, default to 1 (no adjustment)
    [HideInInspector]
    public static float[] microphoneBufferArray;
    [HideInInspector]
    public static float[] processBufferArray;
    [HideInInspector]
    public static float[] rmsValues;
    public static int rmsIndex = 0;
    public static float averageRms;
#if !UNITY_ANDROID
    public static RNNoise.NET.Denoiser Denoiser = new RNNoise.NET.Denoiser();
#endif
    public void OnDestroy()
    {
#if !UNITY_ANDROID
        Denoiser.Dispose();
#endif
    }
}
