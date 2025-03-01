using OpusSharp.Core;
using UnityEngine;
public static class BasisOpusSettings
{
    public static int RecordingFullLength = 1;
    public static int NumChannels = 1;
    public static OpusPredefinedValues OpusApplication = OpusPredefinedValues.OPUS_APPLICATION_AUDIO;
    public static float DesiredDurationInSeconds = 0.02f;
    public static int GetSampleFreq()
    {
        return SampleFreqToInt();
    }
    public static int CalculateDesiredTime()
    {
        return Mathf.CeilToInt(DesiredDurationInSeconds * GetSampleFreq());
    }
    public static float[] CalculateProcessBuffer()
    {
        return new float[CalculateDesiredTime()];
    }
    public static int SampleFreqToInt()
    {
        return 48000;
    }
}
