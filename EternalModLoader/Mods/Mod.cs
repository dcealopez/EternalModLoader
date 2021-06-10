namespace EternalModLoader.Mods
{
    /// <summary>
    /// Mod info class
    /// </summary>
    public class Mod
    {
        /// <summary>
        /// Mod name
        /// </summary>
        public string Name;

        /// <summary>
        /// Mod author
        /// </summary>
        public string Author;

        /// <summary>
        /// Mod description
        /// </summary>
        public string Description;

        /// <summary>
        /// Mod version
        /// </summary>
        public string Version;

        /// <summary>
        /// Mod load priority
        /// </summary>
        public int LoadPriority;

        /// <summary>
        /// Required mod loader version to run the mod
        /// </summary>
        public int RequiredVersion;
    }
}
