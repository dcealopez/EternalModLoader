using BlangParser;

namespace EternalModLoader.Mods.Resources.Blang
{
    /// <summary>
    /// Blang file entry class
    /// </summary>
    public class BlangFileEntry
    {
        /// <summary>
        /// Deserialized .blang file
        /// </summary>
        public BlangFile BlangFile;

        /// <summary>
        /// Chunk of the .blang file in the resources container file
        /// </summary>
        public ResourceChunk Chunk;

        /// <summary>
        /// Whether or not the .blang file was modified
        /// </summary>
        public bool WasModified;

        /// <summary>
        /// Wheter or not to announce the file as modified
        /// </summary>
        public bool Announce;

        /// <summary>
        /// Blang file entry constructor
        /// </summary>
        /// <param name="blangFile">deserialized .blang file</param>
        /// <param name="chunk">resource chunk</param>
        public BlangFileEntry(BlangFile blangFile, ResourceChunk chunk)
        {
            BlangFile = blangFile;
            Chunk = chunk;
        }
    }
}
