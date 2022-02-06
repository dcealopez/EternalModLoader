using System.IO;

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
        /// StreamDB mod file constructor
        /// </summary>
        /// <param name="parent">parent mod</param>
        /// <param name="name">mod name</param>
        public StreamDBModFile(Mod parent, string name)
        {
            Parent = parent;
            Name = name;
        }
    }
}
