namespace EternalModLoader
{
    /// <summary>
    /// Mod class
    /// </summary>
    public class Mod
    {
        /// <summary>
        /// Mod name
        /// </summary>
        public string Name;

        /// <summary>
        /// Mod file bytes
        /// </summary>
        public byte[] FileBytes;
        
        /// <summary>
        /// True if the mod file is a .blang json
        /// </summary>
        public bool IsBlangJson;

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
