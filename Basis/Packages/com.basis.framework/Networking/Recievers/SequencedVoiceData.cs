[System.Serializable]
public struct SequencedVoiceData
{
    public byte SequenceNumber;
    public byte[] Array;
    public int Length;
    public bool IsInsertedSilence;

    public SequencedVoiceData(byte sequenceNumber, byte[] array, int length, bool isInsertedSilence)
    {
        SequenceNumber = sequenceNumber;
        Array = array;
        Length = length;
        IsInsertedSilence = isInsertedSilence;
    }
}
