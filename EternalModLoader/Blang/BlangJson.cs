using System.Collections.Generic;

namespace EternalModLoader
{
    /// <summary>
    /// BlangJson class for JSON deserialization
    /// </summary>
    public class BlangJson
    {
        /// <summary>
        /// List of StringObjects for JSON deserialization
        /// </summary>
        public IList<StringObject> Strings { get; set; }
    }

    /// <summary>
    /// StringObject class used by BlangJson class
    /// </summary>
    public class StringObject
    {
        /// <summary>
        /// String identifier
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// String text
        /// </summary>
        public string Text { get; set; }
    }
}