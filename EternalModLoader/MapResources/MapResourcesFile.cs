using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EternalModLoader.MapResources
{
    /// <summary>
    /// Map Resources file class
    /// </summary>
    public class MapResourcesFile
    {
        /// <summary>
        /// Maybe a timestamp?
        /// </summary>
        public int Magic;

        /// <summary>
        /// Layer list
        /// The amount of layers should be written in big-endian
        /// </summary>
        public List<string> Layers = new List<string>();

        /// <summary>
        /// Asset Type list
        /// The amount of layers should be written in big-endian
        /// </summary>
        public List<string> AssetTypes = new List<string>();

        /// <summary>
        /// Asset list
        /// The amount of layers should be written in big-endian
        /// </summary>
        public List<MapAsset> Assets = new List<MapAsset>();

        /// <summary>
        /// Map list
        /// The amount of layers should be written in big-endian
        /// </summary>
        public List<string> Maps = new List<string>();

        /// <summary>
        /// Converts the map resources object to a byte array
        /// </summary>
        /// <returns>the map resources object as a byte array</returns>
        public byte[] ToByteArray()
        {
            using (var memoryStream = new MemoryStream())
            {
                // Write magic
                memoryStream.Write(BitConverter.GetBytes(Magic), 0, 4);

                // Write layer count (big-endian)
                var layerCountBytes = BitConverter.GetBytes(Layers.Count);
                Array.Reverse(layerCountBytes);
                memoryStream.Write(layerCountBytes, 0, 4);

                // Write layers
                foreach (var layer in Layers)
                {
                    var layerNameBytes = Encoding.UTF8.GetBytes(layer);
                    memoryStream.Write(BitConverter.GetBytes(layerNameBytes.Length), 0, 4);
                    memoryStream.Write(layerNameBytes, 0, layerNameBytes.Length);
                }

                // Write asset type count (big-endian)
                var assetTypeCountBytes = BitConverter.GetBytes((long)AssetTypes.Count);
                Array.Reverse(assetTypeCountBytes);
                memoryStream.Write(assetTypeCountBytes, 0, 8);

                // Write asset types
                foreach (var assetType in AssetTypes)
                {
                    var assetTypeBytes = Encoding.UTF8.GetBytes(assetType);
                    memoryStream.Write(BitConverter.GetBytes(assetTypeBytes.Length), 0, 4);
                    memoryStream.Write(assetTypeBytes, 0, assetTypeBytes.Length);
                }

                // Write asset count (big-endian)
                var assetCountBytes = BitConverter.GetBytes(Assets.Count);
                Array.Reverse(assetCountBytes);
                memoryStream.Write(assetCountBytes, 0, 4);

                // Write assets
                foreach (var asset in Assets)
                {
                    var assetNameBytes = Encoding.UTF8.GetBytes(asset.Name);

                    // Write asset type index (big-endian)
                    var assetTypeIndexBytes = BitConverter.GetBytes(asset.AssetTypeIndex);
                    Array.Reverse(assetTypeIndexBytes);
                    memoryStream.Write(assetTypeIndexBytes, 0, 4);

                    memoryStream.Write(BitConverter.GetBytes(assetNameBytes.Length), 0, 4);
                    memoryStream.Write(assetNameBytes, 0, assetNameBytes.Length);

                    // Write unknown data 1
                    memoryStream.Write(BitConverter.GetBytes(asset.UnknownData1), 0, 4);

                    // Write the remaining unknown data (big-endian)
                    var unknownData2Bytes = BitConverter.GetBytes(asset.UnknownData2);
                    Array.Reverse(unknownData2Bytes);
                    memoryStream.Write(unknownData2Bytes, 0, 4);

                    var unknownData3Bytes = BitConverter.GetBytes(asset.UnknownData3);
                    Array.Reverse(unknownData3Bytes);
                    memoryStream.Write(unknownData3Bytes, 0, 8);

                    var unknownData4Bytes = BitConverter.GetBytes(asset.UnknownData4);
                    Array.Reverse(unknownData4Bytes);
                    memoryStream.Write(unknownData4Bytes, 0, 8);
                }

                // Write map count (big-endian)
                var mapCountBytes = BitConverter.GetBytes(Maps.Count);
                Array.Reverse(mapCountBytes);
                memoryStream.Write(mapCountBytes, 0, 4);

                // Write maps
                foreach (var map in Maps)
                {
                    var mapBytes = Encoding.UTF8.GetBytes(map);
                    memoryStream.Write(BitConverter.GetBytes(mapBytes.Length), 0, 4);
                    memoryStream.Write(mapBytes, 0, mapBytes.Length);
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// Creates a MapResources object from a byte array
        /// </summary>
        /// <param name="rawData">raw map resources file data</param>
        /// <returns>the parsed MapResources object</returns>
        public static MapResourcesFile Parse(byte[] rawData)
        {
            MapResourcesFile mapResourcesFile = new MapResourcesFile();

            using (var memoryStream = new MemoryStream(rawData))
            {
                using (var binaryReader = new BinaryReader(memoryStream, Encoding.Default, true))
                {
                    // Read the magic
                    mapResourcesFile.Magic = binaryReader.ReadInt32();

                    // Read layer count (big-endian)
                    var layerCountBytes = binaryReader.ReadBytes(4);
                    Array.Reverse(layerCountBytes);
                    int layerCount = BitConverter.ToInt32(layerCountBytes, 0);

                    // Read layers
                    for (int i = 0; i < layerCount; i++)
                    {
                        int stringLength = binaryReader.ReadInt32();
                        mapResourcesFile.Layers.Add(Encoding.UTF8.GetString(binaryReader.ReadBytes(stringLength)));
                    }

                    // Read asset type count (big-endian)
                    var assetTypeCountBytes = binaryReader.ReadBytes(8);
                    Array.Reverse(assetTypeCountBytes);
                    long assetTypeCount = BitConverter.ToInt64(assetTypeCountBytes, 0);

                    // Read asset types
                    for (int i = 0; i < assetTypeCount; i++)
                    {
                        int stringLength = binaryReader.ReadInt32();
                        mapResourcesFile.AssetTypes.Add(Encoding.UTF8.GetString(binaryReader.ReadBytes(stringLength)));
                    }

                    // Read assets count (big-endian)
                    var assetCountBytes = binaryReader.ReadBytes(4);
                    Array.Reverse(assetCountBytes);
                    int assetCount = BitConverter.ToInt32(assetCountBytes, 0);

                    // Read assets
                    for (int i = 0; i < assetCount; i++)
                    {
                        var mapAsset = new MapAsset();
                        var assetTypeIndexBytes = binaryReader.ReadBytes(4);
                        Array.Reverse(assetTypeIndexBytes);
                        mapAsset.AssetTypeIndex = BitConverter.ToInt32(assetTypeIndexBytes, 0);

                        int stringLength = binaryReader.ReadInt32();
                        mapAsset.Name = Encoding.UTF8.GetString(binaryReader.ReadBytes(stringLength));

                        // Read unknown data
                        mapAsset.UnknownData1 = binaryReader.ReadInt32();

                        // Read the remaining unknown data (big-endian)
                        var unknownData2Bytes = binaryReader.ReadBytes(4);
                        Array.Reverse(unknownData2Bytes);
                        mapAsset.UnknownData2 = BitConverter.ToInt32(unknownData2Bytes, 0);

                        var unknownData3Bytes = binaryReader.ReadBytes(8);
                        Array.Reverse(unknownData3Bytes);
                        mapAsset.UnknownData3 = BitConverter.ToInt64(unknownData3Bytes, 0);

                        var unknownData4Bytes = binaryReader.ReadBytes(8);
                        Array.Reverse(unknownData4Bytes);
                        mapAsset.UnknownData4 = BitConverter.ToInt64(unknownData4Bytes, 0);

                        mapResourcesFile.Assets.Add(mapAsset);
                    }

                    // Read map count (big-endian)
                    var mapCountBytes = binaryReader.ReadBytes(4);
                    Array.Reverse(mapCountBytes);
                    int mapCount = BitConverter.ToInt32(mapCountBytes, 0);

                    // Read asset types
                    for (int i = 0; i < mapCount; i++)
                    {
                        int stringLength = binaryReader.ReadInt32();
                        mapResourcesFile.Maps.Add(Encoding.UTF8.GetString(binaryReader.ReadBytes(stringLength)));
                    }
                }
            }

            return mapResourcesFile;
        }
    }
}
