using System.Collections.Generic;

namespace EternalModLoader
{
    /// <summary>
    /// Package map spec class
    /// </summary>
    public class PackageMapSpec
    {
        /// <summary>
        /// File list
        /// </summary>
        public IList<PackageMapSpecFile> Files;

        /// <summary>
        /// Map-file references
        /// </summary>
        public IList<PackageMapSpecMapFileRef> MapFileRefs;

        /// <summary>
        /// Map list
        /// </summary>
        public IList<PackageMapSpecMap> Maps;
    }

    /// <summary>
    /// Package map spec file class
    /// </summary>
    public class PackageMapSpecFile
    {
        /// <summary>
        /// File name
        /// </summary>
        public string Name;
    }

    /// <summary>
    /// Package map spec map-file reference class
    /// </summary>
    public class PackageMapSpecMapFileRef
    {
        /// <summary>
        /// File index
        /// </summary>
        public int File;

        /// <summary>
        /// Map index
        /// </summary>
        public int Map;
    }

    /// <summary>
    /// Package map spec map class
    /// </summary>
    public class PackageMapSpecMap
    {
        /// <summary>
        /// Map name
        /// </summary>
        public string Name;
    }
}
