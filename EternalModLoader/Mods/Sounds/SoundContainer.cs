using System.Collections.Generic;

namespace EternalModLoader.Mods.Sounds
{
    /// <summary>
    /// Sound container class
    /// </summary>
    public class SoundContainer
    {
        /// <summary>
        /// Sound container file name
        /// </summary>
        public string Name;

        /// <summary>
        /// Sound container file path
        /// </summary>
        public string Path;

        /// <summary>
        /// Sound mod file list for this sound archive
        /// </summary>
        public List<SoundModFile> ModFiles;

        /// <summary>
        /// Sound container constructor
        /// </summary>
        /// <param name="name">sound container name</param>
        /// <param name="path">sound container path</param>
        public SoundContainer(string name, string path)
        {
            Name = name;
            Path = path;
            ModFiles = new List<SoundModFile>();
        }
    }
}
