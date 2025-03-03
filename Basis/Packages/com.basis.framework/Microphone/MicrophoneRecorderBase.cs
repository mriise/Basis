using UnityEngine;
using System;
public abstract class MicrophoneRecorderBase : MonoBehaviour
{
    public static Action OnHasAudio;
    public static Action OnHasSilence; // Event triggered when silence is detected
    public AudioClip clip;
    public bool IsInitialize = false;
    public string MicrophoneDevice = null;
    public int ProcessBufferLength;
    public float Volume = 1; // Volume adjustment factor, default to 1 (no adjustment)
    [HideInInspector]
    public float[] microphoneBufferArray;
    [HideInInspector]
    public float[] processBufferArray;
    [HideInInspector]
    public float[] rmsValues;
    public int rmsIndex = 0;
    public float averageRms;
#if !UNITY_ANDROID
    public RNNoise.NET.Denoiser Denoiser = new RNNoise.NET.Denoiser();
#endif
    public void OnDestroy()
    {
#if !UNITY_ANDROID
        Denoiser.Dispose();
#endif
    }
}
