namespace EternalModLoader.Mods.StreamDB
{
    /// <summary>
    /// StreamDB entry class
    /// </summary>
    public class StreamDBEntry
    {
        /// <summary>
        /// File Id
        /// </summary>
        public uint FileId;

        /// <summary>
        /// Data offset for this file entry in the streamdb file
        /// </summary>
        public long DataOffset;

        /// <summary>
        /// StreamDB entry constructor
        /// </summary>
        /// <param name="fileId">file id</param>
        /// <param name="dataOffset">data offset</param>
        public StreamDBEntry(uint fileId, long dataOffset)
        {
            FileId = fileId;
            DataOffset = dataOffset;
        }
    }
}
