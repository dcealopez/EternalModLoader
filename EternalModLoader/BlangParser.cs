using System;
using System.Collections.Generic;
using System.IO;

namespace EternalModLoader
{
    /// <summary>
    /// BlangFile class
    /// </summary>
    public class BlangFile
    {
        /// <summary>
        /// Unknown data
        /// </summary>
        public long UnknownData;

        /// <summary>
        /// The strings in this file
        /// </summary>
        public List<BlangString> Strings;

        /// <summary>
        /// Parses the given Blang file into a BlangFile object
        /// </summary>
        /// <param name="memoryStream">stream containing the Blang file</param>
        /// <returns>parsed Blang file in a BlangFile object</returns>
        public static BlangFile ParseFromMemory(MemoryStream memoryStream)
        {
            var blangFile = new BlangFile();
            
            using (var binaryReader = new BinaryReader(memoryStream))
            {
                var blangStrings = new List<BlangString>();

                // Read unknown data (big-endian 64 bit integer)
                memoryStream.Seek(0x0, SeekOrigin.Begin);
                byte[] unknownDataBytes = binaryReader.ReadBytes(8);
                Array.Reverse(unknownDataBytes, 0, unknownDataBytes.Length);
                blangFile.UnknownData = BitConverter.ToInt64(unknownDataBytes, 0);

                // Read the amount of strings (big-endian 32 bit integer)
                byte[] stringAmountBytes = binaryReader.ReadBytes(4);
                Array.Reverse(stringAmountBytes, 0, stringAmountBytes.Length);
                int stringAmount = BitConverter.ToInt32(stringAmountBytes, 0);

                // Parse each string
                for (int i = 0; i < stringAmount; i++)
                {
                    // Read string hash
                    uint hash = binaryReader.ReadUInt32();

                    // Read string identifier
                    int identifierBytes = binaryReader.ReadInt32();
                    string identifier = System.Text.Encoding.UTF8.GetString(binaryReader.ReadBytes(identifierBytes));

                    // Read string
                    int textBytes = binaryReader.ReadInt32();
                    string text = System.Text.Encoding.UTF8.GetString(binaryReader.ReadBytes(textBytes));

                    // Read unknown data
                    int unknownBytes = binaryReader.ReadInt32();
                    string unknown = System.Text.Encoding.UTF8.GetString(binaryReader.ReadBytes(unknownBytes));

                    blangStrings.Add(new BlangString()
                    {
                        Hash = hash,
                        Identifier = identifier,
                        Text = text,
                        Unknown = unknown
                    });
                }

                blangFile.Strings = blangStrings;
            }

            return blangFile;
        }

        /// <summary>
        /// Writes the current BlangFile object to a byte array
        /// </summary>
        /// <returns>byte array containing a .blang file</returns>
        public MemoryStream WriteToMemory()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    // Delete invalid strings first
                    // Strings must have a valid identifier
                    for (int i = Strings.Count - 1; i >= 0; i--)
                    {
                        if (string.IsNullOrEmpty(Strings[i].Identifier) || string.IsNullOrWhiteSpace(Strings[i].Identifier))
                        {
                            Strings.RemoveAt(i);
                        }
                    }

                    // Write unknown data in big-endian
                    byte[] unknownDataBytes = BitConverter.GetBytes(UnknownData);
                    Array.Reverse(unknownDataBytes);
                    binaryWriter.Write(unknownDataBytes);

                    // Write string amount in big-endian
                    byte[] stringsAmount = BitConverter.GetBytes(Strings.Count);
                    Array.Reverse(stringsAmount);
                    binaryWriter.Write(stringsAmount);

                    // Write each string
                    foreach (var blangString in Strings)
                    {
                        // Calculate the hash of the identifier string (FNV1A32)
                        var identifierBytes = System.Text.Encoding.UTF8.GetBytes(blangString.Identifier.ToLowerInvariant());
                        uint fnvPrime = 0x01000193;
                        blangString.Hash = 0x811C9DC5;

                        for (int i = 0; i < identifierBytes.Length; i++)
                        {
                            unchecked
                            {
                                blangString.Hash ^= identifierBytes[i];
                                blangString.Hash *= fnvPrime;
                            }
                        }

                        // Convert to little endian
                        byte[] hashBytes = BitConverter.GetBytes(blangString.Hash);
                        Array.Reverse(hashBytes);
                        blangString.Hash = BitConverter.ToUInt32(hashBytes, 0);

                        // Write the hash (little-endian)
                        binaryWriter.Write(blangString.Hash);

                        // Write identifier (don't convert to lower-case this time)
                        identifierBytes = System.Text.Encoding.UTF8.GetBytes(blangString.Identifier);
                        binaryWriter.Write(identifierBytes.Length);
                        binaryWriter.Write(identifierBytes);

                        // Write text
                        // Null or empty strings are permitted
                        if (string.IsNullOrEmpty(blangString.Text) || string.IsNullOrWhiteSpace(blangString.Text))
                        {
                            blangString.Text = "";
                        }

                        // Remove carriage returns
                        blangString.Text = blangString.Text.Replace("\r", "");

                        var textBytes = System.Text.Encoding.UTF8.GetBytes(blangString.Text);
                        binaryWriter.Write(textBytes.Length);
                        binaryWriter.Write(textBytes);

                        // Write unknown data
                        if (string.IsNullOrEmpty(blangString.Unknown) || string.IsNullOrWhiteSpace(blangString.Unknown))
                        {
                            binaryWriter.Write(new byte[4]);
                        }
                        else
                        {
                            var unknownBytes = System.Text.Encoding.UTF8.GetBytes(blangString.Unknown);
                            binaryWriter.Write(unknownBytes.Length);
                            binaryWriter.Write(unknownBytes);
                        }
                    }
                }

                return memoryStream;
            }
        }
    }
}