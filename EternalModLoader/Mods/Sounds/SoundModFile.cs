using System.IO;

namespace EternalModLoader.Mods.Sounds
{
    /// <summary>
    /// Sound mod class
    /// </summary>
    public class SoundModFile
    {
        /// <summary>
        /// Parent mod where this mod file is from
        /// </summary>
        public Mod Parent;

        /// <summary>
        /// Sound mod name (file name, either a sound number identifier or string)
        /// </summary>
        public string Name;

        /// <summary>
        /// Sound file data memory stream
        /// </summary>
        public MemoryStream FileData;

        /// <summary>
        /// Sound mod file constructor
        /// </summary>
        /// <param name="parent">parent mod</param>
        /// <param name="name">mod name</param>
        public SoundModFile(Mod parent, string name)
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
