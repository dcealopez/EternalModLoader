using System;

namespace EternalModLoader.Mods.Resources
{

    /// <summary>
    /// Mod class
    /// </summary>
    public class ResourceModFile
    {
        /// <summary>
        /// Parent mod where this mod file is from
        /// </summary>
        public Mod Parent;

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
        /// Special byte 1 for new resources
        /// </summary>
        public byte? SpecialByte1 = null;

        /// <summary>
        /// Special byte 2 for new resources
        /// </summary>
        public byte? SpecialByte2 = null;

        /// <summary>
        /// Special byte 3 for new resources
        /// </summary>
        public byte? SpecialByte3 = null;

        /// <summary>
        /// Resource mod file constructor
        /// </summary>
        /// <param name="parent">parent mod</param>
        /// <param name="name">mod name</param>
        public ResourceModFile(Mod parent, string name)
        {
            Parent = parent;
            Name = name;
        }
    }
}
