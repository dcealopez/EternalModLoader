using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EternalModLoader.Mods.Resources.ResourceData
{
    /// <summary>
    /// Resource data class
    /// </summary>
    public class ResourceData
    {
        /// <summary>
        /// Parses a compressed resource data file
        /// </summary>
        /// <param name="filename">resource data file name</param>
        /// <returns>a Dictionary indexed by the resource filename's hashcode</returns>
        public static Dictionary<ulong, ResourceDataEntry> Parse(string filename)
        {
            Dictionary<ulong, ResourceDataEntry> resourceData;

            // The data should be compressed, read the whole file into memory first and decompress it
            // Compressed with Oodle Kraken, level 4
            var compressedData = File.ReadAllBytes(filename);
            long decompressedSize = BitConverter.ToInt64(compressedData.Take(8).ToArray(), 0);
            var decompressedData = Oodle.Decompress(compressedData.Skip(8).ToArray(), decompressedSize);

            // Parse the binary data now
            using (var memoryStream = new MemoryStream(decompressedData))
            {
                using (var binaryReader = new BinaryReader(memoryStream, Encoding.Default, true))
                {
                    // Amount of entries
                    ulong amount = binaryReader.ReadUInt64();
                    resourceData = new Dictionary<ulong, ResourceDataEntry>((int)amount);

                    // Read each entry
                    for (ulong i = 0; i < amount; i++)
                    {
                        ulong fileNameHash = binaryReader.ReadUInt64();
                        ulong streamDbHash = binaryReader.ReadUInt64();
                        byte version = binaryReader.ReadByte();
                        byte specialByte1 = binaryReader.ReadByte();
                        byte specialByte2 = binaryReader.ReadByte();
                        byte specialByte3 = binaryReader.ReadByte();
                        ushort typeNameLength = binaryReader.ReadUInt16();
                        string typeName = Encoding.Default.GetString(binaryReader.ReadBytes(typeNameLength));
                        ushort assetTypeLength = binaryReader.ReadUInt16();
                        string assetType = typeName;
                        string assetName = string.Empty;

                        if (assetTypeLength > 0)
                        {
                            assetType = Encoding.Default.GetString(binaryReader.ReadBytes(assetTypeLength));
                            ushort assetNameLength = binaryReader.ReadUInt16();
                            assetName = Encoding.Default.GetString(binaryReader.ReadBytes(assetNameLength));
                        }

                        resourceData.Add(fileNameHash, new ResourceDataEntry()
                        {
                            StreamDbHash = streamDbHash,
                            Version = version,
                            SpecialByte1 = specialByte1,
                            SpecialByte2 = specialByte2,
                            SpecialByte3 = specialByte3,
                            ResourceType = typeName,
                            MapResourceName = assetName,
                            MapResourceType = assetType
                        });
                    }
                }
            }

            return resourceData;
        }

        /// <summary>
        /// Calculates the hash of a resource file name
        /// </summary>
        /// <param name="input">input string</param>
        /// <returns>the hash of a resource file name</returns>
        public static ulong CalculateResourceFileNameHash(string input)
        {
            ulong hashedValue = 3074457345618258791;

            for (int i = 0; i < input.Length; i++)
            {
                hashedValue += input[i];
                hashedValue *= 3074457345618258799;
            }

            return hashedValue;
        }
    }
}
