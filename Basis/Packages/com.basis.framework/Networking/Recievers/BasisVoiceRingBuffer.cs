using System;
using System.Collections.Generic;

public class JitterBuffer
{
    private const int MaxSize = 3; // Maximum buffer size
    private readonly List<SequencedVoiceData> sequencedVoiceDatas = new List<SequencedVoiceData>();
    private readonly object lockObj = new object();

    public byte WritingIndex { get; private set; }
    public byte ReadingIndex { get; private set; }

    public bool Pop(out SequencedVoiceData sequencedVoiceData)
    {
        lock (lockObj)
        {
            if (sequencedVoiceDatas.Count > 0)
            {
                sequencedVoiceData = sequencedVoiceDatas[0];
                sequencedVoiceDatas.RemoveAt(0);
                ReadingIndex = sequencedVoiceData.SequenceNumber; // Move forward
                return true;
            }
            sequencedVoiceData = null;
            return false;
        }
    }

    public void Push(SequencedVoiceData sequencedVoiceData)
    {
        lock (lockObj)
        {
            byte sequence = sequencedVoiceData.SequenceNumber;

            // Check for duplicate sequence number
            if (IsDuplicate(sequence))
            {
                return; // Ignore duplicate packets
            }

            // Only insert if the packet is ahead of the ReadingIndex
            if (IsAheadOfReading(sequence))
            {
                InsertInOrder(sequencedVoiceData);

                if (sequencedVoiceDatas.Count > MaxSize)
                {
                    BasisDebug.Log("dropping Sequence Voice Data");
                    sequencedVoiceDatas.RemoveAt(0); // Remove oldest packet if full
                }

                WritingIndex = sequence; // Update writing index
            }
        }
    }

    private void InsertInOrder(SequencedVoiceData sequencedVoiceData)
    {
        int index = 0;
        while (index < sequencedVoiceDatas.Count && IsAheadOf(sequencedVoiceDatas[index].SequenceNumber, sequencedVoiceData.SequenceNumber))
        {
            index++;
        }
        sequencedVoiceDatas.Insert(index, sequencedVoiceData);
    }

    private bool IsAheadOfReading(byte sequence)
    {
        return IsAheadOf(ReadingIndex, sequence);
    }

    private bool IsAheadOf(byte current, byte next)
    {
        int diff = (next - current + 64) % 64;
        return diff > 0; // Ensures the packet is in the future relative to the given sequence
    }

    private bool IsDuplicate(byte sequence)
    {
        foreach (var data in sequencedVoiceDatas)
        {
            if (data.SequenceNumber == sequence)
            {
                return true;
            }
        }
        return false;
    }

    public void Clear()
    {
        lock (lockObj)
        {
            sequencedVoiceDatas.Clear();
            WritingIndex = ReadingIndex = 0; // Reset indices
        }
    }

    public bool IsBufferFull()
    {
        lock (lockObj)
        {
            return sequencedVoiceDatas.Count >= MaxSize;
        }
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
