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

        /// <summary>
        /// Fast, unsafe method to check if two byte arrays are equal
        /// </summary>
        /// <param name="b1">first byte array</param>
        /// <param name="b2">second byte array</param>
        /// <returns>true if they are equal, false otherwise</returns>
        public static bool ArraysEqual(byte[] b1, byte[] b2)
        {
            unsafe
            {
                if (b1.Length != b2.Length)
                {
                    return false;
                }

                int n = b1.Length;

                fixed (byte* p1 = b1, p2 = b2)
                {
                    byte* ptr1 = p1;
                    byte* ptr2 = p2;

                    while (n-- > 0)
                    {
                        if (*ptr1++ != *ptr2++)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }
    }
}
