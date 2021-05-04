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
        public IList<Layers> Layers { get; set; }

        /// <summary>
        /// Maps list
        /// </summary>
        public IList<Maps> Maps { get; set; }

        /// <summary>
        /// Resources list
        /// </summary>
        public IList<Resources> Resources { get; set; }
    }

    /// <summary>
    /// Layers object
    /// </summary>
    public class Layers
    {
        /// <summary>
        /// Layer name
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Maps object
    /// </summary>
    public class Maps
    {
        /// <summary>
        /// Map name
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Resources object
    /// </summary>
    public class Resources
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
        public ushort Version;

        /// <summary>
        /// Asset name for .mapresources
        /// </summary>
        public string Name;

        /// <summary>
        /// Asset type for .mapresources
        /// </summary>
        public string MapResourceType;
    }
}
