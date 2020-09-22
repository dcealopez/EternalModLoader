namespace EternalModLoader
{
    /// <summary>
    /// Resource chunk class
    /// </summary>
    public class ResourceChunk
    {
        /// <summary>
        /// Name of the chunk
        /// </summary>
        public string Name;

        /// <summary>
        /// Name Id in the names list
        /// </summary>
        public long NameId;

        /// <summary>
        /// File offset
        /// </summary>
        public long FileOffset;

        /// <summary>
        /// Sizes offset
        /// </summary>
        public long SizeOffset;

        /// <summary>
        /// Compressed size
        /// </summary>
        public long SizeZ;

        /// <summary>
        /// Uncompressed size
        /// </summary>
        public long Size;

        /// <summary>
        /// Compression mode
        /// </summary>
        public byte CompressionMode;

        /// <summary>
        /// Chunk constructor
        /// </summary>
        /// <param name="name">chunk name</param>
        /// <param name="fileOffset">file offset</param>
        public ResourceChunk(string name, long fileOffset)
        {
            Name = name;
            FileOffset = fileOffset;
        }
    }
}
