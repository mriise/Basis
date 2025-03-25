using UnityEngine;
using LiteNetLib;
using Basis.Scripts.Networking.NetworkedAvatar;
using Basis.Scripts.BasisSdk.Players;
using Basis.Scripts.Profiler;
using static SerializableBasis;
using LiteNetLib.Utils;
using Basis.Network.Core;
using OpusSharp.Core;

namespace Basis.Scripts.Networking.Transmitters
{
    [System.Serializable]
    public class BasisAudioTransmission
    {
        public OpusEncoder encoder;
        public BasisNetworkPlayer NetworkedPlayer;
        public BasisLocalPlayer Local;
        public MicrophoneRecorder Recorder;

        public bool IsInitalized = false;
        public bool HasEvents = false;

        public AudioSegmentDataMessage AudioSegmentData = new AudioSegmentDataMessage();
        public AudioSegmentDataMessage audioSilentSegmentData = new AudioSegmentDataMessage();
        public void OnEnable(BasisNetworkPlayer networkedPlayer)
        {
            if (!IsInitalized)
            {
                // Assign the networked player and base network send functionality
                NetworkedPlayer = networkedPlayer;


                // Initialize the Opus encoder with the retrieved settings
                encoder = new OpusEncoder(LocalOpusSettings.MicrophoneSampleRate, LocalOpusSettings.Channels, LocalOpusSettings.OpusApplication);
                //  encoder.Ctl(EncoderCTL.OPUS_SET_COMPLEXITY, ref Complexity);
                //  bool VBR = true;
                //encoder.Ctl(EncoderCTL.OPUS_SET_VBR,ref VBR);
                // Cast the networked player to a local player to access the microphone recorder
                Local = (BasisLocalPlayer)networkedPlayer.Player;
                Recorder = Local.MicrophoneRecorder;

                // If there are no events hooked up yet, attach them
                if (!HasEvents)
                {
                    if (Recorder != null)
                    {
                        // Hook up the event handlers
                        MicrophoneRecorder.OnHasAudio += OnAudioReady;
                        MicrophoneRecorder.OnHasSilence += SendSilenceOverNetwork;
                        HasEvents = true;
                        // Ensure the output buffer is properly initialized and matches the packet size
                        if (MicrophoneRecorder.PacketSize != AudioSegmentData.TotalLength)
                        {
                            AudioSegmentData = new AudioSegmentDataMessage(new byte[MicrophoneRecorder.PacketSize]);
                        }
                    }
                }

                IsInitalized = true;
            }
        }
        public void OnDisable()
        {
            if (HasEvents)
            {
                MicrophoneRecorder.OnHasAudio -= OnAudioReady;
                MicrophoneRecorder.OnHasSilence -= SendSilenceOverNetwork;
                HasEvents = false;
            }
            if (Recorder != null)
            {
                GameObject.Destroy(Recorder.gameObject);
            }
            encoder.Dispose();
            encoder = null;
        }
        public const DeliveryMethod AudioSendMethod = DeliveryMethod.Unreliable;
        public byte sequenceNumber = 0;
        public const int Complexity = 10;
        public void OnAudioReady()
        {
            if (NetworkedPlayer.HasReasonToSendAudio)
            {
                // UnityEngine.BasisDebug.Log("Sending out Audio");
                if (MicrophoneRecorder.PacketSize != AudioSegmentData.TotalLength)
                {
                    AudioSegmentData = new AudioSegmentDataMessage(new byte[MicrophoneRecorder.PacketSize]);
                }
                // Encode the audio data from the microphone recorder's buffer
                AudioSegmentData.LengthUsed = encoder.Encode(MicrophoneRecorder.processBufferArray,MicrophoneRecorder.SampleRate, AudioSegmentData.buffer, AudioSegmentData.TotalLength);

                NetDataWriter writer = GenerateWriter();
                AudioSegmentData.Serialize(writer);
                BasisNetworkProfiler.AudioSegmentDataMessageCounter.Sample(AudioSegmentData.LengthUsed);
                SendOutVoice(writer);
                Local.AudioReceived?.Invoke(true);
            }
            else
            {
                //  UnityEngine.BasisDebug.Log("Rejecting out going Audio");
            }
        }
        public NetDataWriter GenerateWriter()
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put(BasisNetworkCommons.VoiceChannel);
            sequenceNumber = (byte)((sequenceNumber + 1) & 0x3F);
            writer.Put(sequenceNumber);
            return writer;
        }
        private void SendSilenceOverNetwork()
        {
            if (NetworkedPlayer.HasReasonToSendAudio)
            {
                NetDataWriter writer = GenerateWriter();
                audioSilentSegmentData.LengthUsed = 0;
                audioSilentSegmentData.Serialize(writer);
                BasisNetworkProfiler.AudioSegmentDataMessageCounter.Sample(writer.Length);
                SendOutVoice(writer);
                Local.AudioReceived?.Invoke(false);
            }
        }
        public void SendOutVoice(NetDataWriter writer)
        {
            BasisNetworkManagement.LocalPlayerPeer.Send(writer, BasisNetworkCommons.FallChannel, AudioSendMethod);
        }
    }
}
