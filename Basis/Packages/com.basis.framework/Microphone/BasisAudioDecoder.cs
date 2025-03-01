using System;

public class BasisAudioDecoder
{
    public event Action OnDecoded;
    OpusSharp.Core.OpusDecoder decoder;
    public float[] pcmBuffer;
    public int pcmLength;
    public int FakepcmLength;
    public void Initialize()
    {
        FakepcmLength = 2048;
        pcmLength = 2048;
        pcmBuffer = new float[FakepcmLength * (int)BasisOpusSettings.NumChannels];//AudioDecoder.maximumPacketDuration now its 2048
        decoder = new OpusSharp.Core.OpusDecoder(BasisOpusSettings.SampleFreqToInt(), BasisOpusSettings.NumChannels);
    }
    public void Deinitalize()
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
        pcmLength = decoder.Decode(data, length, pcmBuffer, 960, false);
        OnDecoded?.Invoke();
    }
}
