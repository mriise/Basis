using Basis.Scripts.BasisSdk.Helpers;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Drivers;
using Basis.Scripts.Networking.NetworkedAvatar;
using OpusSharp.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

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
        [SerializeField]
        public JitterBuffer RingBuffer = new JitterBuffer();
        public bool IsPlaying = false;
        public float[] pcmBuffer;
        public int pcmLength;
        public bool IsReady = false;
        /// <summary>
        /// decodes data into the pcm buffer
        /// note that the pcm buffer is always going to have more data then submited.
        /// the pcm length is how much was actually encoded.
        /// </summary>
        /// <param name="data"></param>
        public void OnDecode(byte SequenceNumber, byte[] data, int length)
        {
            if (IsReady)
            {
                SequencedVoiceData SequencedVoiceData = new SequencedVoiceData(SequenceNumber);
                SequencedVoiceData.Array = data;
                SequencedVoiceData.Length = length;
                SequencedVoiceData.IsInsertedSilence = false;

                RingBuffer.Push(SequencedVoiceData);
            }
        }
        public void OnDecodeSilence(byte SequenceNumber)
        {
            if (IsReady)
            {
                SequencedVoiceData SequencedVoiceData = new SequencedVoiceData(SequenceNumber);
                SequencedVoiceData.Array = null;
                SequencedVoiceData.Length = RemoteOpusSettings.Pcmlength;
                SequencedVoiceData.IsInsertedSilence = true;

                RingBuffer.Push(SequencedVoiceData);
            }
        }
        public void OnEnable(BasisNetworkPlayer networkedPlayer)
        {
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
        private List<float> remainingSamples = new List<float>(); // Persistent buffer

        public int OnAudioFilterRead(float[] data, int channels)
        {
            int length = data.Length;
            if (!IsReady)
            {
                Array.Fill(data, 0f);
                return length;
            }

            int frames = length / channels; // Number of audio frames
            int outputSampleRate = RemoteOpusSettings.PlayBackSampleRate;

            if (RemoteOpusSettings.NetworkSampleRate == outputSampleRate)
            {
                ProcessAudioWithBuffering(data, frames, channels);
            }
            else
            {
                BasisDebug.LogError("Samplerate mismatch");
                Array.Fill(data, 0f); // Fill with silence in case of mismatch
            }

            return length;
        }

        private void ProcessAudioWithBuffering(float[] data, int frames, int channels)
        {
            int neededSamples = frames; // Number of mono samples needed
            List<float> outputSamples = new List<float>();

            // Use stored samples first
            if (remainingSamples.Count > 0)
            {
                int toCopy = Math.Min(neededSamples, remainingSamples.Count);
                outputSamples.AddRange(remainingSamples.Take(toCopy));
                remainingSamples.RemoveRange(0, toCopy);
                neededSamples -= toCopy;
            }

            // Decode new data if more samples are needed
            while (neededSamples > 0)
            {
                if (RingBuffer.Pop(out SequencedVoiceData VoiceData) == false || VoiceData.IsInsertedSilence)
                {
                    // Not enough data available, pad with silence
                    outputSamples.AddRange(new float[neededSamples]);
                    break;
                }

                // Decode new data
                int pcmLength = decoder.Decode(VoiceData.Array, VoiceData.Length, pcmBuffer, RemoteOpusSettings.NetworkSampleRate, false);

                if (pcmLength > neededSamples)
                {
                    // More samples than needed; store extra
                    outputSamples.AddRange(pcmBuffer.Take(neededSamples));
                    remainingSamples.AddRange(pcmBuffer.Skip(neededSamples).Take(pcmLength - neededSamples));
                }
                else
                {
                    // Use all decoded samples
                    outputSamples.AddRange(pcmBuffer.Take(pcmLength));
                }

                neededSamples -= pcmLength;
            }

            // Distribute samples to output buffer
            for (int i = 0; i < frames; i++)
            {
                float sample = i < outputSamples.Count ? outputSamples[i] : 0f; // Ensure silence if needed
                for (int c = 0; c < channels; c++)
                {
                    int index = i * channels + c;
                    data[index] = sample; // Set same sample for each channel
                }
            }
        }
    }
}
