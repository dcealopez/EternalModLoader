using System.IO;
using System.Collections.Generic;

namespace EternalModLoader.Mods.StreamDB
{
    /// <summary>
    /// StreamDB mod class
    /// </summary>
    public class StreamDBModFile : ModFile
    {
        /// <summary>
        /// StreamDB mod name (file name, either a file number identifier or string)
        /// </summary>
        public string Name;

        /// <summary>
        /// StreamDB mod file ID
        /// </summary>
        public ulong FileId;

        /// <summary>
        /// File data memory stream
        /// </summary>
        public MemoryStream FileData;

        /// <summary>
        /// Number of LODs (level of detail) entries this streamdb mod requires
        /// </summary>
        public int LODCount;

        /// <summary>
        /// Start offsets for each LOD in FileData memorystream
        /// </summary>
        public List<int> LODDataOffset;

        /// <summary>
        /// Length of each LOD in FileData memorystream
        /// </summary>
        public List<int> LODDataLength;

        /// <summary>
        /// File data memory stream for each individual LOD
        /// </summary>
        public List<MemoryStream> LODFileData;

        /// <summary>
        /// StreamDB mod file constructor
        /// </summary>
        /// <param name="parent">parent mod</param>
        /// <param name="name">mod name</param>
        public StreamDBModFile(Mod parent, string name)
        {
            Parent = parent;
            Name = name;
            LODCount = 0;
            LODDataOffset = new List<int>();
            LODDataLength = new List<int>();
            LODFileData = new List<MemoryStream>();
        }
    }
}
