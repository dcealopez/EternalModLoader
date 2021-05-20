using System.Collections.Generic;

namespace EternalModLoader
{
    /// <summary>
    /// Sound bank info class
    /// </summary>
    public class SoundBankInfo
    {
        /// <summary>
        /// Sound bank file name
        /// </summary>
        public string Name;

        /// <summary>
        /// Sound bank file path
        /// </summary>
        public string Path;

        /// <summary>
        /// Sound mod list for this sound bank
        /// </summary>
        public List<SoundMod> ModList;
    }
}
