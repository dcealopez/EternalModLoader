using System;

namespace EternalModLoader
{

    /// <summary>
    /// Mod class
    /// </summary>
    public class Mod
    {
        /// <summary>
        /// Load priority
        /// </summary>
        public int Priority;

        /// <summary>
        /// Mod name
        /// </summary>
        public string Name;

        /// <summary>
        /// The uncompressed size of the .mapresources file
        /// This is used only for AssetsInfo JSON files
        /// </summary>
        public long UncompressedSize;

        /// <summary>
        /// Mod file bytes
        /// </summary>
        public byte[] FileBytes;

        /// <summary>
        /// True if the mod file is a .blang JSON
        /// </summary>
        public bool IsBlangJson;

        /// <summary>
        /// True if the mod file is an AssetsInfo JSON
        /// </summary>
        public bool IsAssetsInfoJson;

        /// <summary>
        /// In case the mod file is an AssetsInfo JSON, this contains the deserialized object
        /// </summary>
        public AssetsInfo AssetsInfo;

        /// <summary>
        /// StreamDb Hash for new resources
        /// </summary>
        public ulong? StreamDbHash = null;

        /// <summary>
        /// Resource type for new resources
        /// </summary>
        public string ResourceType = null;

        /// <summary>
        /// Version for new resources
        /// </summary>
        public ushort? Version = null;

        /// <summary>
        /// Mod constructor
        /// </summary>
        /// <param name="name">mod name</param>
        public Mod(string name)
        {
            Name = name;
        }
    }
}
