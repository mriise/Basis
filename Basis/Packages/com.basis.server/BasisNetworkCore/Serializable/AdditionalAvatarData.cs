using LiteNetLib.Utils;
using System;

public static partial class SerializableBasis
{
    public struct AdditionalAvatarData
    {
        public byte messageIndex;
        public byte[] array;

        public void Deserialize(NetDataReader reader)
        {
            int bytesAvailable = reader.AvailableBytes;
            if (bytesAvailable > 0)
            {
                messageIndex = reader.GetByte();

                byte payloadSize = reader.GetByte();
                if (reader.AvailableBytes >= payloadSize)
                {
                    if (payloadSize > 0)
                    {
                        if (array == null || array.Length != payloadSize)
                        {
                            array = new byte[payloadSize];
                        }
                        reader.GetBytes(array, payloadSize);
                    }
                    else
                    {
                        array = new byte[0]; // Ensure it's not null
                    }
                }
                else
                {
                    BNL.LogError($"Unable to read remaining bytes, available: {reader.AvailableBytes}");
                }
            }
            else
            {
                BNL.LogError($"Unable to read remaining bytes, available: {bytesAvailable}");
            }
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(messageIndex);

            byte size = (array != null) ? (byte)array.Length : (byte)0;
            writer.Put(size);

            if (size > 0)
            {
                writer.Put(array);
            }
        }
    }
}
