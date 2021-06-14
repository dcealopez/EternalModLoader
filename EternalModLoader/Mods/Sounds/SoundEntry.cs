namespace EternalModLoader.Mods.Sounds
{
    /// <summary>
    /// Sound entry class
    /// </summary>
    public class SoundEntry
    {
        /// <summary>
        /// Sound Id
        /// </summary>
        public uint SoundId;

        /// <summary>
        /// Info offset (starting at the encoded size value offset) for this sound entry
        /// </summary>
        public long InfoOffset;

        /// <summary>
        /// Sound entry constructor
        /// </summary>
        /// <param name="soundId">sound id</param>
        /// <param name="infoOffset">info offset</param>
        public SoundEntry(uint soundId, long infoOffset)
        {
            SoundId = soundId;
            InfoOffset = infoOffset;
        }
    }
}
