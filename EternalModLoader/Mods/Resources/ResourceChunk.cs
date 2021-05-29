namespace EternalModLoader.Mods.Resources
{
    /// <summary>
    /// Resource chunk class
    /// </summary>
    public class ResourceChunk
    {
        /// <summary>
        /// Name of the chunk
        /// </summary>
        public ResourceName ResourceName;

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
        public ResourceChunk(ResourceName name, long fileOffset)
        {
            ResourceName = name;
            FileOffset = fileOffset;
        }
    }
}
