namespace EternalModLoader.Mods.Resources.ResourceData
{
    /// <summary>
    /// Resource data entry
    /// </summary>
    public class ResourceDataEntry
    {
        /// <summary>
        /// Asset stream DB hash
        /// </summary>
        public ulong StreamDbHash;

        /// <summary>
        /// Asset resource type
        /// </summary>
        public string ResourceType;

        /// <summary>
        /// Asset version
        /// </summary>
        public byte Version;

        /// <summary>
        /// Asset map resource name
        /// </summary>
        public string MapResourceName;

        /// <summary>
        /// Asset map resource type
        /// </summary>
        public string MapResourceType;

        /// <summary>
        /// Asset special byte 1
        /// </summary>
        public byte SpecialByte1;

        /// <summary>
        /// Asset special byte 2
        /// </summary>
        public byte SpecialByte2;

        /// <summary>
        /// Asset special byte 3
        /// </summary>
        public byte SpecialByte3;
    }
}
