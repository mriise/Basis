using OpusSharp.Core;
using System;

public class BasisAudioDecoder
{
    public event Action OnDecoded;
    OpusDecoder decoder;
    public float[] pcmBuffer;
    public int pcmLength;
    public int FakepcmLength;
    public void Initialize()
    {
        FakepcmLength = 2048;
        pcmLength = 2048;
        pcmBuffer = new float[FakepcmLength * BasisOpusSettings.NumChannels];
        decoder = new OpusDecoder(BasisOpusSettings.SampleFreqToInt(), BasisOpusSettings.NumChannels);
    }
    public void DeInitalize()
    {
        decoder.Dispose();
        decoder = null;
    }

    /// <summary>
    /// decodes data into the pcm buffer
    /// note that the pcm buffer is always going to have more data then submited.
    /// the pcm length is how much was actually encoded.
    /// </summary>
    /// <param name="data"></param>
    public void OnDecode(byte[] data, int length)
    {
        //960 20ms
        pcmLength = decoder.Decode(data, length, pcmBuffer, BasisOpusSettings.SampleRate, false);
        OnDecoded?.Invoke();
    }
}
