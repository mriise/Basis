using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Drivers;
using Basis.Scripts.Networking.NetworkedAvatar;
using OpusSharp.Core;
using System;
using UnityEngine;

namespace Basis.Scripts.Networking.Receivers
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
        public float[] silentData;
        public byte lastReadIndex = 0;
        public void OnDecode(byte SequenceNumber, byte[] data, int length)
        {
            pcmLength = decoder.Decode(data, length, pcmBuffer, RemoteOpusSettings.NetworkSampleRate, false);
            InOrderRead.Add(pcmBuffer, pcmLength);
        }
        public void OnDecodeSilence(byte SequenceNumber)
        {
            InOrderRead.Add(silentData, RemoteOpusSettings.FrameSize);
        }
        public void ChangeRemotePlayersVolumeSettings(float Volume = 1.0f,float dopplerLevel = 0,float spatialBlend = 1.0f, bool spatialize = true,bool spatializePostEffects = true)
        {
            audioSource.spatialize = spatialize;
            audioSource.spatializePostEffects = spatializePostEffects; //revist later!
            audioSource.spatialBlend = spatialBlend;
            audioSource.dopplerLevel = dopplerLevel;
            audioSource.volume = Volume;
        }
        public async void OnEnable(BasisNetworkPlayer networkedPlayer)
        {
            if (silentData == null || silentData.Length != RemoteOpusSettings.FrameSize)
            {
                silentData = new float[RemoteOpusSettings.FrameSize];
                Array.Fill(silentData, 0f);
            }
            // Initialize settings and audio source
            if (audioSource == null)
            {
                BasisRemotePlayer remotePlayer = (BasisRemotePlayer)networkedPlayer.Player;
                audioSource = BasisHelpers.GetOrAddComponent<AudioSource>(remotePlayer.AudioSourceTransform.gameObject);
            }
            audioSource.loop = true;
            BasisPlayerSettingsData BasisPlayerSettingsData = await BasisPlayerSettingsManager.RequestPlayerSettings(networkedPlayer.Player.UUID);
            ChangeRemotePlayersVolumeSettings(BasisPlayerSettingsData.VolumeLevel);
            InOrderRead = new BasisVoiceRingBuffer();
            // Create AudioClip
            audioSource.clip = AudioClip.Create($"player [{networkedPlayer.NetId}]", RemoteOpusSettings.FrameSize * (2 * 2), RemoteOpusSettings.Channels, RemoteOpusSettings.PlayBackSampleRate, false, (buf) =>
            {
                Array.Fill(buf, 1.0f);
            });
            // Ensure decoder is initialized and subscribe to events
            pcmLength = RemoteOpusSettings.FrameSize;
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
        public void OnAudioFilterRead(float[] data, int channels, int length)
        {
            int frames = length / channels; // Number of audio frames
            if (InOrderRead.IsEmpty)
            {
                // No voice data, fill with silence
              //  BasisDebug.Log("Missing Audio Data! filling with Silence");
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
            ProcessSegment(segment,data,frames,channels);
            InOrderRead.BufferedReturn.Enqueue(segment);
        }
        public float[] resampledSegment;
        private void ProcessAudioWithResampling(float[] data, int frames, int channels, int outputSampleRate)
        {
            float resampleRatio = (float)RemoteOpusSettings.NetworkSampleRate / outputSampleRate;
            int neededFrames = Mathf.CeilToInt(frames * resampleRatio);

            InOrderRead.Remove(neededFrames, out float[] inputSegment);

            if (resampledSegment.Length != frames)
            {
                resampledSegment = new float[frames];
            }
            // Resampling using linear interpolation
            for (int FrameIndex = 0; FrameIndex < frames; FrameIndex++)
            {
                float srcIndex = FrameIndex * resampleRatio;
                int indexLow = Mathf.FloorToInt(srcIndex);
                int indexHigh = Mathf.CeilToInt(srcIndex);
                float frac = srcIndex - indexLow;

                int InputSegmentLength = inputSegment.Length;
                float sampleLow = (indexLow < InputSegmentLength) ? inputSegment[indexLow] : 0;
                float sampleHigh = (indexHigh < InputSegmentLength) ? inputSegment[indexHigh] : 0;

                resampledSegment[FrameIndex] = Mathf.Lerp(sampleLow, sampleHigh, frac);
            }

            ProcessSegment(resampledSegment, data, frames, channels);

            InOrderRead.BufferedReturn.Enqueue(inputSegment);
        }
        private void ProcessSegment(float[] segment, float[] data, int frames, int channels)
        {
            for (int frameIndex = 0; frameIndex < frames; frameIndex++)
            {
                float sample = segment[frameIndex];
                int baseIndex = frameIndex * channels;

                for (int channelIndex = 0; channelIndex < channels; channelIndex++)
                {
                    int index = baseIndex + channelIndex;
                    float value = data[index] * sample;
                    data[index] = Math.Clamp(value, -1f, 1f);
                }
            }
        }
    }
}
