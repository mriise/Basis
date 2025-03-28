using System.Collections.Generic;

[System.Serializable]
public class BasisJitterBuffer
{
    public Dictionary<byte, SequencedVoiceData> voiceData = new Dictionary<byte, SequencedVoiceData>();
    public byte latestInsert;
    public byte LatestRemoved;
    public int MaxBuffer = 5;
    public void Insert(SequencedVoiceData SequencedVoiceData, out string Error)
    {

        latestInsert = SequencedVoiceData.SequenceNumber;
        voiceData[SequencedVoiceData.SequenceNumber] = SequencedVoiceData;

        if (IsAheadOf(LatestRemoved, SequencedVoiceData.SequenceNumber, out Error))
        {
        }
        else
        {
            BasisDebug.LogError(Error);
        }
    }
    public bool Remove(out SequencedVoiceData SequencedVoiceData)
    {
        while (voiceData.Count != 0)
        {
            LatestRemoved++;
            if (LatestRemoved > 63)
            {
                LatestRemoved = 0;
            }
            if (voiceData.Remove(LatestRemoved, out SequencedVoiceData))
            {
                return true;
            }
        }
        SequencedVoiceData = default;
        return false;
    }
    public bool IsBufferFull()
    {
        if(voiceData.Count >= MaxBuffer)
        {
            return true;
        }
        return false;
    }
    public static bool IsAheadOf(byte current, byte next, out string Error)
    {
        // Calculate the difference with proper wraparound
        int diff = (next - current + 64) % 64;

        // Debugging output to help track the difference
        //  Console.WriteLine($"current: {current}, next: {next}, diff: {diff}");

        // Check if next is ahead of current and not too far ahead in the circular buffer
        if (diff > 0 && diff <= 31)
        {
            Error = string.Empty;  // No error, next is ahead
            return true;
        }
        else if (diff == 63)  // Allow diff == 63 as valid ahead (just before wrapping around)
        {
            Error = string.Empty;  // No error, next is ahead
            return true;
        }
        else
        {
            // Determine why it is behind or too far ahead
            if (diff == 0)
            {
                Error = "next is the same as current";
            }
            else if (diff > 31)
            {
                Error = "next is too far ahead (more than halfway around)";
            }
            else
            {
                Error = "next is behind current";
            }

            return false;  // next is behind or too far ahead
        }
    }
}
/*
 * using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Drivers;
using Basis.Scripts.Networking.NetworkedAvatar;
using OpusSharp.Core;
using System;
using UnityEngine;

namespace Basis.Scripts.Networking.Recievers
{
    [System.Serializable]
    public class BasisAudioReceiver
    {
        public BasisRemoteAudioDriver BasisRemoteVisemeAudioDriver;
        public OpusDecoder decoder;
        [SerializeField]
        public AudioSource audioSource;
        [SerializeField]
        public BasisAudioAndVisemeDriver visemeDriver;
        public BasisVoiceRingBuffer InOrderRead;
        public bool IsPlaying = false;
        public float[] pcmBuffer;
        public int pcmLength;
        public bool IsReady;
        public BasisJitterBuffer JitterBuffer = new BasisJitterBuffer();
        public bool userOrder;
        public byte LastSequenceID;
        public float[] silentData;
        /// <summary>
        /// decodes data into the pcm buffer
        /// note that the pcm buffer is always going to have more data then submited.
        /// the pcm length is how much was actually encoded.
        /// </summary>
        /// <param name="data"></param>
        public void OnDecode(byte SequenceNumber, byte[] data, int length)
        {
            if (userOrder)
            {
                bool state = BasisJitterBuffer.IsAheadOf(LastSequenceID, SequenceNumber, out string Error);
                if (state)
                {
                    LastSequenceID = SequenceNumber;
                    pcmLength = decoder.Decode(data, length, pcmBuffer, RemoteOpusSettings.NetworkSampleRate, false);
                    InOrderRead.Add(pcmBuffer, pcmLength);
                }
                else
                {
                    BasisDebug.Log(Error);
                }
            }
            else
            {
                SequencedVoiceData SequencedVoiceData = new SequencedVoiceData()
                {
                    SequenceNumber = SequenceNumber,
                    Array = data,
                    Length = length,
                    IsInsertedSilence = false
                };

                JitterBuffer.Insert(SequencedVoiceData,out string Error);
            }
        }
        public void OnDecodeSilence(byte SequenceNumber)
        {
            if (userOrder)
            {
                bool state = BasisJitterBuffer.IsAheadOf(LastSequenceID, SequenceNumber, out string Error);
                if (state)
                {
                    LastSequenceID = SequenceNumber;
                    InOrderRead.Add(silentData, RemoteOpusSettings.Pcmlength);
                }
                else
                {
                    BasisDebug.Log(Error);
                }
            }
            else
            {
                SequencedVoiceData SequencedVoiceData = new SequencedVoiceData()
                {
                    SequenceNumber = SequenceNumber,
                    Array = null,
                    Length = RemoteOpusSettings.Pcmlength,
                    IsInsertedSilence = true
                };

                JitterBuffer.Insert(SequencedVoiceData, out string Error);
            }
        }
        public void ProvideInOrderData()
        {
            if (JitterBuffer.Remove(out var SVD))
            {
                if (SVD.IsInsertedSilence)
                {
                    InOrderRead.Add(silentData, RemoteOpusSettings.Pcmlength);
                }
                else
                {
                    pcmLength = decoder.Decode(SVD.Array, SVD.Length, pcmBuffer, RemoteOpusSettings.NetworkSampleRate, false);
                    InOrderRead.Add(pcmBuffer, pcmLength);
                }
            }
            else
            {
                InOrderRead.Add(silentData, RemoteOpusSettings.Pcmlength);
            }
        }
        public void OnEnable(BasisNetworkPlayer networkedPlayer)
        {
            if (silentData == null || silentData.Length != RemoteOpusSettings.Pcmlength)
            {
                silentData = new float[RemoteOpusSettings.Pcmlength];
                Array.Fill(silentData, 0f);
            }
            // Initialize settings and audio source
            if (audioSource == null)
            {
                BasisRemotePlayer remotePlayer = (BasisRemotePlayer)networkedPlayer.Player;
                audioSource = BasisHelpers.GetOrAddComponent<AudioSource>(remotePlayer.AudioSourceTransform.gameObject);
            }
            audioSource.spatialize = true;
            audioSource.spatializePostEffects = true; //revist later!
            audioSource.spatialBlend = 1.0f;
            audioSource.dopplerLevel = 0;
            audioSource.volume = 1.0f;
            audioSource.loop = true;
            InOrderRead = new BasisVoiceRingBuffer(RemoteOpusSettings.RecieverLengthCapacity);
            // Create AudioClip
            audioSource.clip = AudioClip.Create($"player [{networkedPlayer.NetId}]", RemoteOpusSettings.RecieverLength, RemoteOpusSettings.Channels, RemoteOpusSettings.PlayBackSampleRate, false, (buf) =>
            {
                Array.Fill(buf, 1.0f);
            });
            // Ensure decoder is initialized and subscribe to events
            pcmLength = RemoteOpusSettings.Pcmlength;
            pcmBuffer = new float[RemoteOpusSettings.SampleLength];
            decoder = new OpusDecoder(RemoteOpusSettings.NetworkSampleRate, RemoteOpusSettings.Channels);
            StartAudio();
            // Perform calibration
            OnCalibration(networkedPlayer);
            IsReady = true;
        }
        public void OnDestroy()
        {
            // Unsubscribe from events on destroy
            if (decoder != null)
            {
                decoder.Dispose();
                decoder = null;
            }
            if (audioSource != null)
            {
                audioSource.Stop();
                GameObject.Destroy(audioSource);
            }
            if (visemeDriver != null)
            {
                GameObject.Destroy(visemeDriver);
            }
        }
        public void OnCalibration(BasisNetworkPlayer networkedPlayer)
        {
            // Ensure viseme driver is initialized for audio processing
            if (visemeDriver == null)
            {
                visemeDriver = BasisHelpers.GetOrAddComponent<BasisAudioAndVisemeDriver>(audioSource.gameObject);
            }
            visemeDriver.TryInitialize(networkedPlayer.Player);
            if (BasisRemoteVisemeAudioDriver == null)
            {
                BasisRemoteVisemeAudioDriver = BasisHelpers.GetOrAddComponent<BasisRemoteAudioDriver>(audioSource.gameObject);
                BasisRemoteVisemeAudioDriver.BasisAudioReceiver = this;
            }
            BasisRemoteVisemeAudioDriver.Initalize(visemeDriver);
        }
        public void StopAudio()
        {
            IsPlaying = false;
            audioSource.Stop();
        }
        public void StartAudio()
        {
            IsPlaying = true;
            audioSource.Play();
        }
        public void OnAudioFilterRead(float[] data, int channels,int length)
        {
            int frames = length / channels; // Number of audio frames
            if (InOrderRead.IsEmpty)
            {
                // No voice data, fill with silence
                BasisDebug.Log("Missing Audio Data! filling with Silence");
                   Array.Fill(data, 0);
                   return;
            }

            int outputSampleRate = RemoteOpusSettings.PlayBackSampleRate;

            if (RemoteOpusSettings.NetworkSampleRate == outputSampleRate)
            {
                ProcessAudioWithoutResampling(data, frames, channels);
            }
            else
            {
                ProcessAudioWithResampling(data, frames, channels, outputSampleRate);
            }
        }

        private void ProcessAudioWithoutResampling(float[] data, int frames, int channels)
        {
            InOrderRead.Remove(frames, out float[] segment);

            for (int i = 0; i < frames; i++)
            {
                float sample = segment[i]; // Single-channel sample from the RingBuffer
                for (int c = 0; c < channels; c++)
                {
                    int index = i * channels + c;
                    data[index] *= sample;
                    data[index] =  Math.Clamp(data[index], -1, 1);
                }
            }
            InOrderRead.BufferedReturn.Enqueue(segment);
        }

        private void ProcessAudioWithResampling(float[] data, int frames, int channels, int outputSampleRate)
        {
            float resampleRatio = (float)RemoteOpusSettings.NetworkSampleRate / outputSampleRate;
            int neededFrames = Mathf.CeilToInt(frames * resampleRatio);

            InOrderRead.Remove(neededFrames, out float[] inputSegment);

            float[] resampledSegment = new float[frames];

            // Resampling using linear interpolation
            for (int i = 0; i < frames; i++)
            {
                float srcIndex = i * resampleRatio;
                int indexLow = Mathf.FloorToInt(srcIndex);
                int indexHigh = Mathf.CeilToInt(srcIndex);
                float frac = srcIndex - indexLow;

                float sampleLow = (indexLow < inputSegment.Length) ? inputSegment[indexLow] : 0;
                float sampleHigh = (indexHigh < inputSegment.Length) ? inputSegment[indexHigh] : 0;

                resampledSegment[i] = Mathf.Lerp(sampleLow, sampleHigh, frac);
            }

            // Apply resampled audio to output buffer
            for (int i = 0; i < frames; i++)
            {
                float sample = resampledSegment[i];
                for (int c = 0; c < channels; c++)
                {
                    int index = i * channels + c;
                    data[index] *= sample;
                }
            }

            InOrderRead.BufferedReturn.Enqueue(inputSegment);
        }
    }
}

 */
