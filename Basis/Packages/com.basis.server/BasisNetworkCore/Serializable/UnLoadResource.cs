using LiteNetLib.Utils;
public static partial class SerializableBasis
{
    public struct UnLoadResource
    {
        /// <summary>
        /// 0 = Game object, 1 = Scene,
        /// </summary>
        public byte Mode;
        public string LoadedNetID;
        public void Deserialize(NetDataReader Writer)
        {
            int Bytes = Writer.AvailableBytes;
            if (Bytes != 0)
            {
                Mode = Writer.GetByte();

                LoadedNetID = Writer.GetString();
            }
            else
            {
                BNL.LogError($"Unable to read Remaing bytes where {Bytes}");
            }
        }
        public void Serialize(NetDataWriter Writer)
        {
            Writer.Put(Mode);
            Writer.Put(LoadedNetID);
        }
    }
}
