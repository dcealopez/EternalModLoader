using System.Collections.Generic;

namespace EternalModLoader
{
    public class PackageMapSpec
    {
        public IList<PackageMapSpecFile> Files;
        public IList<PackageMapSpecMapFileRef> MapFileRefs;
        public IList<PackageMapSpecMap> Maps;
    }

    public class PackageMapSpecFile
    {
        public string Name;
    }

    public class PackageMapSpecMapFileRef
    {
        public int File;
        public int Map;
    }

    public class PackageMapSpecMap
    {
        public string Name;
    }
}
