using Basis.Scripts.BasisSdk.Players;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class BasisUIVolumeSampler : MonoBehaviour
{
    public Image[] MicrophoneSections;
    public int sampleSize = 512; // Number of samples per frame
    public float MicrophoneSectionsLength;

    public float UINormalizerValue = 5; // Scales the RMS value to make the UI more sensitive

    public float[] spectrumData;
    public float rms;
    public float peak;
    public bool HasEvent;
    public BasisRemotePlayer RemotePlayer;

    // Initialization method
    public void Initalize(BasisRemotePlayer Remoteplayer)
    {
        RemotePlayer = Remoteplayer;
        if (RemotePlayer == null || RemotePlayer.NetworkReceiver == null || RemotePlayer.NetworkReceiver.AudioReceiverModule == null || RemotePlayer.NetworkReceiver.AudioReceiverModule.BasisRemoteVisemeAudioDriver == null)
        {
            return;
        }

        MicrophoneSectionsLength = MicrophoneSections.Length;

        // Ensure sample size is a valid power of two
        if (!IsPowerOfTwo(sampleSize) || sampleSize < 64 || sampleSize > 8192)
        {
            Debug.LogError("Sample size must be a power of two between 64 and 8192. Defaulting to 1024.");
            sampleSize = 1024;
        }

        spectrumData = new float[sampleSize];
        RemotePlayer.NetworkReceiver.AudioReceiverModule.BasisRemoteVisemeAudioDriver.AudioData += OnAudio;
        HasEvent = true;
    }

    public void OnDestroy()
    {
        if (HasEvent)
        {
            HasEvent = false;
            RemotePlayer.NetworkReceiver.AudioReceiverModule.BasisRemoteVisemeAudioDriver.AudioData -= OnAudio;
        }
    }

    // Check if a number is a power of two
    private bool IsPowerOfTwo(int value)
    {
        return (value & (value - 1)) == 0 && value > 0;
    }

    // OnAudioFilterRead is called every time the audio filter reads the audio data
    private void OnAudio(float[] data, int channels)
    {
        if (data == null || data.Length == 0)
        {
            return;
        }

        // Copy audio data into spectrumData, ensuring no NaN or infinite values
        System.Array.Copy(data, spectrumData, Mathf.Min(data.Length, spectrumData.Length));

        // Handle NaN or infinity values in spectrum data
        for (int i = 0; i < spectrumData.Length; i++)
        {
            if (float.IsNaN(spectrumData[i]) || float.IsInfinity(spectrumData[i]))
            {
                spectrumData[i] = 0f; // Replace NaN or Infinity with 0
            }
        }

        rms = CalculateSpectrumRMS();
        peak = CalculateSpectrumPeak(); // Calculate peak value
    }

    public void Update()
    {
        // Update the UI with the current RMS and peak values
        UpdateVolumeDisplay(rms, peak);
    }

    // Updates the volume display bars based on the RMS and peak values
    private void UpdateVolumeDisplay(float rms, float peak)
    {
        float normalizedRMS;
        float normalizedPeak;
        // Avoid issues with zero or negative RMS and peak values
        if (rms <= 0f)
        {
            normalizedRMS = 0;
        }
        else
        {
            // Normalize the RMS and peak values
            normalizedRMS = Mathf.Clamp(rms * UINormalizerValue, 0f, 1f);
        }
        if (peak <= 0f)
        {
            normalizedPeak = 0f;
        }
        else
        {
            normalizedPeak = Mathf.Clamp(peak * UINormalizerValue, 0f, 1f);
        }

        if (MicrophoneSections == null || MicrophoneSections.Length == 0)
        {
            Debug.LogError("No microphone sections assigned!");
            return;
        }

        float Maximum = Mathf.Max(normalizedRMS, normalizedPeak);
        // Update each section of the microphone display based on RMS and peak
        for (int i = 0; i < MicrophoneSectionsLength; i++)
        {
            float sectionValue = (float)i / MicrophoneSectionsLength;
            Color barColor = GetColorGradient(sectionValue, Maximum); // Use max of RMS or peak for color
            MicrophoneSections[i].color = barColor;
        }
    }

    // Returns a color based on the volume level (gradient from green → yellow → red)
    private Color GetColorGradient(float sectionValue, float volumeLevel)
    {
        if (sectionValue < Mathf.Max(volumeLevel, 0)) // Prevent totally inactive sections
        {
            if (volumeLevel < 0.3f)
                return Color.Lerp(Color.green, Color.yellow, volumeLevel * 3.33f);
            else if (volumeLevel < 0.7f)
                return Color.Lerp(Color.yellow, new Color(1f, 0.647f, 0f), (volumeLevel - 0.3f) * 3.33f); // Orange color
            else
                return Color.Lerp(new Color(1f, 0.647f, 0f), Color.red, (volumeLevel - 0.7f) * 3.33f); // Orange to Red
        }

        return Color.gray; // Inactive sections
    }

    // Calculate RMS value from the spectrum data
    private float CalculateSpectrumRMS()
    {
        if (spectrumData == null || spectrumData.Length == 0)
        {
            return 0f;
        }

        // Calculate the RMS (Root Mean Square), check for NaN/Infinity
        float sumOfSquares = spectrumData.Sum(value => value * value);
        float rmsValue = Mathf.Sqrt(sumOfSquares / spectrumData.Length);

        // Ensure RMS is not NaN or Infinity
        return float.IsNaN(rmsValue) || float.IsInfinity(rmsValue) ? 0f : rmsValue;
    }

    // Calculate the peak value from the spectrum data
    private float CalculateSpectrumPeak()
    {
        if (spectrumData == null || spectrumData.Length == 0)
        {
            return 0f;
        }

        // Get the peak value (highest value in the spectrum data)
        float peakValue = spectrumData.Max();

        // Ensure peak is not NaN or Infinity
        return float.IsNaN(peakValue) || float.IsInfinity(peakValue) ? 0f : peakValue;
    }
}
