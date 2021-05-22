using System.Collections.Generic;

namespace EternalModLoader
{
    /// <summary>
    /// AssetsInfo JSON object class
    /// </summary>
    public class AssetsInfo
    {
        /// <summary>
        /// Layers list
        /// </summary>
        public IList<AssetsInfoLayer> Layers { get; set; }

        /// <summary>
        /// Maps list
        /// </summary>
        public IList<AssetsInfoMap> Maps { get; set; }

        /// <summary>
        /// Extra resource files to load in the map
        /// </summary>
        public IList<AssetsInfoResource> ExtraResources { get; set; }

        /// <summary>
        /// New assets list
        /// </summary>
        public IList<AssetsInfoAsset> NewAssets { get; set; }
    }

    /// <summary>
    /// Layers object
    /// </summary>
    public class AssetsInfoLayer
    {
        /// <summary>
        /// Layer name
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Maps object
    /// </summary>
    public class AssetsInfoMap
    {
        /// <summary>
        /// Map name
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Extra resource file class
    /// </summary>
    public class AssetsInfoResource
    {
        /// <summary>
        /// File name of the extra resource file to load on this map
        /// </summary>
        public string Name;

        /// <summary>
        /// Indicates wheter or not the specified resource file should be removed
        /// so that it doesn't get loaded on this map
        /// </summary>
        public bool Remove;
    }

    /// <summary>
    /// Assets object
    /// </summary>
    public class AssetsInfoAsset
    {
        /// <summary>
        /// Path to the resource in the container
        /// </summary>
        public string Path;

        /// <summary>
        /// The hash for the resource in StreamDb for .resources
        /// </summary>
        public ulong StreamDbHash;

        /// <summary>
        /// Resource type for .resources
        /// </summary>
        public string ResourceType;

        /// <summary>
        /// Version
        /// </summary>
        public byte Version;

        /// <summary>
        /// Asset name for .mapresources
        /// </summary>
        public string Name;

        /// <summary>
        /// Asset type for .mapresources
        /// </summary>
        public string MapResourceType;

        /// <summary>
        /// Indicates wheter or not the asset should be removed
        /// from the container's map resources
        /// </summary>
        public bool Remove;

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
        /// Special byte 1
        /// </summary>
        public byte SpecialByte1;

        /// <summary>
        /// Special byte 2
        /// </summary>
        public byte SpecialByte2;

        /// <summary>
        /// Special byte 3
        /// </summary>
        public byte SpecialByte3;
    }
}
