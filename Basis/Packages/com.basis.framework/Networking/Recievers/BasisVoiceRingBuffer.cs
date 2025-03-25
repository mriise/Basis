using System.Collections.Generic;

public class JitterBuffer
{
    private const int MaxSize = 3; // Maximum buffer size
    private Queue<SequencedVoiceData> sequencedVoiceDatas = new Queue<SequencedVoiceData>();

    public byte WritingIndex;
    public byte ReadingIndex;

    public bool Pop(out SequencedVoiceData sequencedVoiceData)
    {
        if (sequencedVoiceDatas.TryDequeue(out sequencedVoiceData))
        {
            ReadingIndex = sequencedVoiceData.SequenceNumber; // Move forward
            return true;
        }
        return false;
    }

    public void Push(SequencedVoiceData sequencedVoiceData)
    {
        byte sequence = sequencedVoiceData.SequenceNumber;

        // Only insert if the packet is ahead of the ReadingIndex
        if (IsAheadOfReading(sequence))
        {
            if (sequencedVoiceDatas.Count >= MaxSize)
            {
                sequencedVoiceDatas.Dequeue(); // Remove oldest packet if full
            }

            sequencedVoiceDatas.Enqueue(sequencedVoiceData);
            WritingIndex = sequence; // Update writing index
        }
    }

    private bool IsAheadOfReading(byte sequence)
    {
        int diff = (sequence - ReadingIndex + 64) % 64;
        return diff > 0; // Ensures the packet is in the future relative to ReadingIndex
    }
}

public class SequencedVoiceData
{
    public byte SequenceNumber;
    public byte[] Array;
    public int Length;
    public bool IsInsertedSilence;

    public SequencedVoiceData(byte sequenceNumber)
    {
        SequenceNumber = sequenceNumber;
    }
}
