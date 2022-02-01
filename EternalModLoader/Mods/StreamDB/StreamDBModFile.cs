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

        /// <summary>
        /// Copies the mod file data stream to the given stream
        /// </summary>
        /// <param name="stream">destination stream</param>
        public void CopyFileDataToStream(Stream stream)
        {
            if (FileData == null)
            {
                return;
            }

            FileData.Position = 0;
            FileData.CopyTo(stream);
        }
    }
}
