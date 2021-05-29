namespace EternalModLoader.Mods.Resources.MapResources
{
    /// <summary>
    /// Map Asset class
    /// </summary>
    public class MapAsset
    {
        /// <summary>
        /// Asset Type index (big-endian)
        /// </summary>
        public int AssetTypeIndex;

        /// <summary>
        /// Asset Type name
        /// </summary>
        public string Name;

        /// <summary>
        /// Unknown Data 1
        /// </summary>
        public int UnknownData1;

        /// <summary>
        /// Unknown Data 2 (big-endian)
        /// </summary>
        public int UnknownData2;

        /// <summary>
        /// Unknown Data 3 (big-endian)
        /// </summary>
        public long UnknownData3;

        /// <summary>
        /// Unknown Data 4 (big-endian)
        /// </summary>
        public long UnknownData4;
    }
}
