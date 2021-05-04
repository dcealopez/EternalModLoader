namespace EternalModLoader
{
    /// <summary>
    /// Utility class
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Normalizes the filename of a resource in a container
        /// </summary>
        /// <param name="filename">resource filename to normalize</param>
        /// <returns>normalized resource filename</returns>
        public static string NormalizeResourceFilename(string filename)
        {
            // Trim trailing '$'
            int indexOfDollar = filename.IndexOf('$');

            if (indexOfDollar != -1)
            {
                filename = filename.Substring(0, indexOfDollar);
            }

            // Trim trailing '#'
            int indexOfHashTrail = filename.LastIndexOf('#');

            if (indexOfHashTrail != -1)
            {
                filename = filename.Substring(0, indexOfHashTrail);
            }

            // Trim leading '#'
            int indexOfHash = filename.IndexOf('#');

            if (indexOfHash != -1)
            {
                filename = filename.Substring(indexOfHash + 1);
            }

            return filename;
        }
    }
}
