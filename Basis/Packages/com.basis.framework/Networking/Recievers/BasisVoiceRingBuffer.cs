using System;
using System.Collections.Generic;
using System.Threading;

[System.Serializable]
public struct SequencedVoiceData
{
    public byte SequenceNumber;
    public byte[] Array;
    public int Length;
    public bool IsInsertedSilence;
}
public class BasisVoiceRingBuffer
{
    private readonly float[] buffer;
    private int head; // Points to the next position to write
    private int tail; // Points to the next position to read
    private int size; // Current data size in the buffer
    private readonly object bufferLock = new();
    public Queue<float[]> BufferedReturn = new Queue<float[]>();

    public BasisVoiceRingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException("Capacity must be greater than zero.");
        buffer = new float[capacity];
        head = 0;
        tail = 0;
        size = 0;
    }

    public int Capacity => buffer.Length;

    public bool IsEmpty => Interlocked.CompareExchange(ref size, 0, 0) == 0;
    public bool IsFull => Interlocked.CompareExchange(ref size, 0, 0) == Capacity;
    public void Add(float[] segment, int length)
    {
        if (segment == null || segment.Length == 0)
        {
            throw new ArgumentNullException(nameof(segment));
        }
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be a positive number.");
        }
        if (length > segment.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be less than or equal to the segment's length.");
        }

        lock (bufferLock)
        {
            if (length > Capacity)
            {
                throw new InvalidOperationException("The segment is too large to fit into the buffer.");
            }

            // Remove old data to make room for new data
            int availableSpace = Capacity - Interlocked.CompareExchange(ref size, 0, 0);
            if (length > availableSpace)
            {
                int itemsToRemove = length - availableSpace;
                Interlocked.Add(ref tail, itemsToRemove);
                Interlocked.Add(ref size, -itemsToRemove);
                tail %= Capacity;
                BasisDebug.Log($"Overwriting {itemsToRemove} elements due to lack of space in the Audio buffer.");
            }

            // Add the new segment to the buffer
            int firstPart = Math.Min(length, Capacity - head); // Space till the end of the buffer
            Array.Copy(segment, 0, buffer, head, firstPart);   // Copy first part
            int remaining = length - firstPart;
            if (remaining > 0)
            {
                Array.Copy(segment, firstPart, buffer, 0, remaining); // Copy wrap-around part
            }

            Interlocked.Add(ref head, length);
            head %= Capacity;
            Interlocked.Add(ref size, length);
        }
    }

    public void Remove(int segmentSize, out float[] segment)
    {
        if (segmentSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(segmentSize));

        // Try to reuse an array from the buffer pool
        lock (BufferedReturn)
        {
            if (!BufferedReturn.TryDequeue(out segment) || segment.Length != segmentSize)
            {
                segment = new float[segmentSize];
            }
        }

        lock (bufferLock)
        {
            int currentSize = Interlocked.CompareExchange(ref size, 0, 0);
            int itemsToRemove = Math.Min(segmentSize, currentSize);

            // Remove items in bulk
            int firstPart = Math.Min(itemsToRemove, Capacity - tail); // Items till the end of the buffer
            Array.Copy(buffer, tail, segment, 0, firstPart);          // Copy first part
            int remaining = itemsToRemove - firstPart;
            if (remaining > 0)
            {
                Array.Copy(buffer, 0, segment, firstPart, remaining); // Copy wrap-around part
            }

            Interlocked.Add(ref tail, itemsToRemove);
            tail %= Capacity;
            Interlocked.Add(ref size, -itemsToRemove);

            // Warn if fewer items were available than requested
            if (itemsToRemove < segmentSize)
            {
                BasisDebug.Log($"Warning: Requested {segmentSize} items, but only {itemsToRemove} are available.");
            }
        }
    }
}
