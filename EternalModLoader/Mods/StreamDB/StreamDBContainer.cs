using System.Collections.Generic;

namespace EternalModLoader.Mods.StreamDB
{
    /// <summary>
    /// streamdb container class
    /// </summary>
    public class StreamDBContainer
    {
        /// <summary>
        /// streamdb container file name
        /// </summary>
        public string Name;

        /// <summary>
        /// streamdb container file path
        /// </summary>
        public string Path;

        /// <summary>
        /// Mod file list for this streamdb archive
        /// </summary>
        public List<StreamDBModFile> ModFiles;

        /// <summary>
        /// List of file entries in this streamdb container
        /// </summary>
        public List<StreamDBEntry> StreamDBEntries;

        /// <summary>
        /// Sound container constructor
        /// </summary>
        /// <param name="name">sound container name</param>
        /// <param name="path">sound container path</param>
        public StreamDBContainer(string name, string path)
        {
            Name = name;
            Path = path;
            ModFiles = new List<StreamDBModFile>();
            StreamDBEntries = new List<StreamDBEntry>();
        }
    }
}
