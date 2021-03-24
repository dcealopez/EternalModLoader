namespace EternalModLoader
{
    /// <summary>
    /// BlangString class
    /// </summary>
    public class BlangString
    {

        /// <summary>
        /// The string's hash
        /// </summary>
        public uint Hash { get; set; }

        /// <summary>
        /// String identifier
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// The string's text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Unknown string data
        /// </summary>
        public string Unknown { get; set; }
    }
}