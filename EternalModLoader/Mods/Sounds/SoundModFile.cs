namespace EternalModLoader.Mods.Sounds
{
    /// <summary>
    /// Sound mod class
    /// </summary>
    public class SoundModFile
    {
        /// <summary>
        /// Parent mod where this mod file is from
        /// </summary>
        public Mod Parent;

        /// <summary>
        /// Sound mod name (file name, either a sound number identifier or string)
        /// </summary>
        public string Name;

        /// <summary>
        /// Sound file bytes
        /// </summary>
        public byte[] FileBytes;

        /// <summary>
        /// Sound mod file constructor
        /// </summary>
        /// <param name="parent">parent mod</param>
        /// <param name="name">mod name</param>
        public SoundModFile(Mod parent, string name)
        {
            Parent = parent;
            Name = name;
        }
    }
}
