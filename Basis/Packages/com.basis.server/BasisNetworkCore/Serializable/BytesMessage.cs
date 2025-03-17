#nullable enable

using LiteNetLib.Utils;

namespace Basis.Network.Core.Serializable
{
    public static partial class SerializableBasis
    {
        /// <summary>
        /// Consists of a ushort length, followed by a byte array (of the same length).
        /// </summary>
        [System.Serializable]
        public struct BytesMessage
        {
            public byte[] bytes;
            public void Deserialize(NetDataReader Reader)
            {
                if (Reader.TryGetUShort(out ushort msgLength))
                {
                    if (msgLength == 0)
                    {
                        bytes = System.Array.Empty<byte>(); // Assign an empty array instead of null
                        return;
                    }

                    if (bytes == null || bytes.Length != msgLength)
                    {
                        bytes = new byte[msgLength];
                    }
                    Reader.GetBytes(bytes, msgLength);
                }
                else
                {
                    BNL.LogError("Missing Message Length!");
                }
            }

            public readonly void Serialize(NetDataWriter Writer)
            {
                if (bytes == null || bytes.Length == 0)
                {
                    Writer.Put((ushort)0);
                    return;
                }
                ushort Length = (ushort)bytes.Length;
                Writer.Put(Length);
                Writer.Put(bytes);
            }
        }
    }
}
