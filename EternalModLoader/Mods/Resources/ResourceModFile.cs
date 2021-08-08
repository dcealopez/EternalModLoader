using System.IO;

namespace EternalModLoader.Mods.Resources
{

    /// <summary>
    /// Mod class
    /// </summary>
    public class ResourceModFile : ModFile
    {
        /// <summary>
        /// Mod name
        /// </summary>
        public string Name;

        /// <summary>
        /// Resource this mod belongs to
        /// </summary>
        public string ResourceName;

        /// <summary>
        /// Mod file data memory stream
        /// </summary>
        public MemoryStream FileData;

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
        /// Indicates whether or not the asset should be placed
        /// before or after the asset with PlaceByName name and PlaceByType type
        /// </summary>
        public bool PlaceBefore;

        /// <summary>
        /// Place by (before/after) name
        /// </summary>
        public string PlaceByName;

        /// <summary>
        /// (Optional) Place by (before/after) type
        /// Used in conjuction with PlaceByName, since multiple assets
        /// can have the same name in map resources
        /// </summary>
        public string PlaceByType;

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
        /// Whether or not to announce this mod file as added/replaced
        /// </summary>
        public bool Announce = true;

        /// <summary>
        /// Resource mod file constructor
        /// </summary>
        /// <param name="parent">parent mod</param>
        /// <param name="name">mod name</param>
        /// <param name="resourceName">name of the resource file this mods belongs in</param>
        public ResourceModFile(Mod parent, string name, string resourceName, bool announce = true)
        {
            Parent = parent;
            Name = name;
            ResourceName = resourceName;
            Announce = announce;
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
