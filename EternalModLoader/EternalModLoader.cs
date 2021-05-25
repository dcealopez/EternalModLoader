using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BlangParser;
using EternalModLoader.Blang;
using EternalModLoader.MapResources;
using EternalModLoader.ResourceData;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EternalModLoader
{
    /// <summary>
    /// EternalModLoader main class
    /// Original version made by SutandoTsukai181
    /// Ported to C# by proteh
    /// </summary>
    public class EternalModLoader
    {
        /// <summary>
        /// Resource data file name
        /// </summary>
        private const string ResourceDataFileName = "rs_data";

        /// <summary>
        /// Package Map Spec JSON file name
        /// </summary>
        private const string PackageMapSpecJsonFileName = "packagemapspec.json";

        /// <summary>
        /// Game base path
        /// </summary>
        public static string BasePath;

        /// <summary>
        /// Verbose logging
        /// </summary>
        public static bool Verbose;

        /// <summary>
        /// Resource list
        /// </summary>
        public static List<ResourceInfo> ResourceList = new List<ResourceInfo>();

        /// <summary>
        /// Sound bank list
        /// </summary>
        public static List<SoundBankInfo> SoundBankList = new List<SoundBankInfo>();

        /// <summary>
        /// Resource data dictionary, indexed by file name
        /// </summary>
        public static Dictionary<ulong, ResourceDataEntry> ResourceDataDictionary = new Dictionary<ulong, ResourceDataEntry>();

        /// <summary>
        /// Reads the resource info from the specified resource info object
        /// </summary>
        /// <param name="resourceInfo">resource info object</param>
        public static void ReadResource(ResourceInfo resourceInfo)
        {
            using (var fileStream = new FileStream(resourceInfo.Path, FileMode.Open, FileAccess.Read))
            {
                using (var binaryReader = new BinaryReader(fileStream))
                {
                    fileStream.Seek(0x20, SeekOrigin.Begin);
                    int fileCount = binaryReader.ReadInt32();

                    fileStream.Seek(0x24, SeekOrigin.Begin);
                    int unknownCount = binaryReader.ReadInt32();

                    fileStream.Seek(0x28, SeekOrigin.Begin);
                    int dummy2Num = binaryReader.ReadInt32(); // Number of TypeIds

                    fileStream.Seek(0x38, SeekOrigin.Begin);
                    int stringsSize = binaryReader.ReadInt32(); // Total size of nameOffsets and names

                    fileStream.Seek(0x40, SeekOrigin.Begin);
                    long namesOffset = binaryReader.ReadInt64();

                    fileStream.Seek(0x48, SeekOrigin.Begin);
                    long namesEnd = binaryReader.ReadInt64();

                    fileStream.Seek(0x50, SeekOrigin.Begin);
                    long infoOffset = binaryReader.ReadInt64();

                    fileStream.Seek(0x60, SeekOrigin.Begin);
                    long dummy7OffOrg = binaryReader.ReadInt64(); // Offset of TypeIds, needs addition to get offset of nameIds

                    fileStream.Seek(0x68, SeekOrigin.Begin);
                    long dataOff = binaryReader.ReadInt64();

                    fileStream.Seek(0x74, SeekOrigin.Begin);
                    long idclOff = binaryReader.ReadInt64();

                    // Read all the file names now
                    fileStream.Seek(namesOffset, SeekOrigin.Begin);
                    long namesNum = binaryReader.ReadInt64();

                    // Skip the name offsets
                    fileStream.Seek(namesOffset + 8 + (namesNum * 8), SeekOrigin.Begin);

                    long namesOffsetEnd = fileStream.Position;
                    long namesSize = namesEnd - fileStream.Position;
                    List<ResourceName> namesList = new List<ResourceName>();
                    List<byte> currentNameBytes = new List<byte>();

                    for (int i = 0; i < namesSize; i++)
                    {
                        byte currentByte = binaryReader.ReadByte();

                        if (currentByte == '\x00' || i == namesSize - 1)
                        {
                            if (currentNameBytes.Count == 0)
                            {
                                continue;
                            }

                            // Support full filenames and "normalized" filenames (backwards compatibility)
                            string fullFileName = Encoding.UTF8.GetString(currentNameBytes.ToArray());
                            string normalizedFileName = Utils.NormalizeResourceFilename(fullFileName);

                            namesList.Add(new ResourceName()
                            {
                                FullFileName = fullFileName,
                                NormalizedFileName = normalizedFileName
                            });

                            currentNameBytes.Clear();
                            continue;
                        }

                        currentNameBytes.Add(currentByte);
                    }

                    resourceInfo.FileCount = fileCount;
                    resourceInfo.TypeCount = dummy2Num;
                    resourceInfo.StringsSize = stringsSize;
                    resourceInfo.NamesOffset = namesOffset;
                    resourceInfo.InfoOffset = infoOffset;
                    resourceInfo.Dummy7Offset = dummy7OffOrg;
                    resourceInfo.DataOffset = dataOff;
                    resourceInfo.IdclOffset = idclOff;
                    resourceInfo.UnknownCount = unknownCount;
                    resourceInfo.FileCount2 = fileCount * 2;
                    resourceInfo.NamesOffsetEnd = namesOffsetEnd;
                    resourceInfo.UnknownOffset = namesEnd;
                    resourceInfo.UnknownOffset2 = namesEnd;
                    resourceInfo.NamesList = namesList;

                    ReadChunkInfo(fileStream, binaryReader, resourceInfo);
                }
            }
        }

        /// <summary>
        /// Reads the info of all the chunks in the resource file
        /// </summary>
        /// <param name="fileStream">file stream used to read the resource file</param>
        /// <param name="binaryReader">binary reader used to read the resource file</param>
        /// <param name="resourceInfo">resource info object</param>
        private static void ReadChunkInfo(FileStream fileStream, BinaryReader binaryReader, ResourceInfo resourceInfo)
        {
            fileStream.Seek(resourceInfo.Dummy7Offset + (resourceInfo.TypeCount * 4), SeekOrigin.Begin);
            long dummy7Off = fileStream.Position;

            for (int i = 0; i < resourceInfo.FileCount; i++)
            {
                fileStream.Seek(0x20 + resourceInfo.InfoOffset + (0x90 * i), SeekOrigin.Begin);
                long nameId = binaryReader.ReadInt64();

                fileStream.Seek(0x38 + resourceInfo.InfoOffset + (0x90 * i), SeekOrigin.Begin);
                long fileOffset = binaryReader.ReadInt64();
                long sizeOffset = fileStream.Position;
                long sizeZ = binaryReader.ReadInt64();
                long size = binaryReader.ReadInt64();

                fileStream.Seek(0x70 + resourceInfo.InfoOffset + (0x90 * i), SeekOrigin.Begin);
                byte compressionMode = binaryReader.ReadByte();

                nameId = ((nameId + 1) * 8) + dummy7Off;
                fileStream.Seek(nameId, SeekOrigin.Begin);
                nameId = binaryReader.ReadInt64();
                var name = resourceInfo.NamesList[(int)nameId];

                var chunk = new ResourceChunk(name, fileOffset)
                {
                    FileOffset = sizeOffset - 8,
                    SizeOffset = sizeOffset,
                    SizeZ = sizeZ,
                    Size = size,
                    CompressionMode = compressionMode
                };

                resourceInfo.ChunkList.Add(chunk);
            }
        }

        /// <summary>
        /// Find a chunk in a resource info object by filename
        /// </summary>
        /// <param name="name">file name</param>
        /// <param name="resourceInfo">resource info object</param>
        /// <returns>the ResourceChunk object, null if it wasn't found</returns>
        public static ResourceChunk GetChunk(string name, ResourceInfo resourceInfo)
        {
            foreach (var chunk in resourceInfo.ChunkList)
            {
                if (chunk.ResourceName.FullFileName == name || chunk.ResourceName.NormalizedFileName == name)
                {
                    return chunk;
                }
            }

            return null;
        }

        /// <summary>
        /// Loads the mods present in the specified resource info object
        /// </summary>
        /// <param name="resourceInfo">resource info object</param>
        public static void LoadMods(ResourceInfo resourceInfo)
        {
            using (var fileStream = new FileStream(resourceInfo.Path, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Copy the stream into memory for faster manipulation of the data
                    fileStream.CopyTo(memoryStream);

                    // Load the mods
                    ReplaceChunks(memoryStream, resourceInfo);
                    AddChunks(memoryStream, resourceInfo);

                    // Copy the memory stream into the filestream now
                    fileStream.SetLength(memoryStream.Length);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }
        }

        /// <summary>
        /// Replaces the chunks of the files with the modded ones
        /// </summary>
        /// <param name="memoryStream">memory stream where the resource file is stored</param>
        /// <param name="resourceInfo">resource info object</param>
        public static void ReplaceChunks(MemoryStream memoryStream, ResourceInfo resourceInfo)
        {
            int fileCount = 0;
            const int BufferSize = 4096; // For file expansion when we need to add bytes and shift files
            var buffer = new byte[BufferSize];

            using (var binaryReader = new BinaryReader(memoryStream, Encoding.Default, true))
            {
                foreach (var mod in resourceInfo.ModList)
                {
                    ResourceChunk chunk = null;

                    // Handle AssetsInfo JSON files
                    if (mod.IsAssetsInfoJson && mod.AssetsInfo != null)
                    {
                        // Add the extra resources to packagemapspec.json if specified
                        if (mod.AssetsInfo.ExtraResources != null)
                        {
                            var packageMapSpecPath = Path.Combine(BasePath, PackageMapSpecJsonFileName);

                            if (!File.Exists(packageMapSpecPath))
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("ERROR: ");
                                Console.ResetColor();
                                Console.WriteLine($"{packageMapSpecPath} not found while trying to add extra resources for level {resourceInfo.Name}");
                            }
                            else
                            {
                                var packageMapSpecFile = File.ReadAllBytes(packageMapSpecPath);
                                PackageMapSpec packageMapSpec = null;

                                try
                                {
                                    // Try to parse the JSON
                                    var serializerSettings = new JsonSerializerSettings();
                                    serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                    packageMapSpec = JsonConvert.DeserializeObject<PackageMapSpec>(Encoding.UTF8.GetString(packageMapSpecFile), serializerSettings);
                                }
                                catch
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("ERROR: ");
                                    Console.ResetColor();
                                    Console.WriteLine($"Failed to parse {packageMapSpecPath} - malformed JSON?");
                                }

                                // Add the extra resources, then rewrite the JSON
                                if (packageMapSpec != null)
                                {
                                    foreach (var extraResource in mod.AssetsInfo.ExtraResources)
                                    {
                                        // First check that the resource trying to be added actually exists
                                        var extraResourcePath = PathToResource(extraResource.Name);

                                        if (extraResourcePath == string.Empty)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("WARNING: ");
                                            Console.ResetColor();
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"Trying to add non-existing extra resource \"{extraResource.Name}\" to \"{resourceInfo.Name}\", skipping");
                                            Console.ResetColor();
                                            continue;
                                        }

                                        // Add the extra resources before all the original resources the level loads
                                        // Find the necessary map and file indexes
                                        int fileIndex = -1;
                                        int mapIndex = -1;

                                        for (int i = 0; i < packageMapSpec.Files.Count; i++)
                                        {
                                            if (packageMapSpec.Files[i].Name.Contains(extraResource.Name))
                                            {
                                                fileIndex = i;
                                                break;
                                            }
                                        }

                                        for (int i = 0; i < packageMapSpec.Maps.Count; i++)
                                        {
                                            if (packageMapSpec.Maps[i].Name.Contains(Path.GetFileNameWithoutExtension(mod.Name)))
                                            {
                                                mapIndex = i;
                                                break;
                                            }
                                        }

                                        if (fileIndex == -1)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("ERROR: ");
                                            Console.ResetColor();
                                            Console.WriteLine($"Invalid extra resource {extraResource.Name}, skipping");
                                            continue;
                                        }

                                        if (mapIndex == -1)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("ERROR: ");
                                            Console.ResetColor();
                                            Console.WriteLine($"Map reference not found for {mod.Name}, skipping");
                                            continue;
                                        }

                                        // Remove the extra resource, if specified
                                        if (extraResource.Remove)
                                        {
                                            bool mapFileRefRemoved = false;

                                            // Find the map file reference to remove
                                            for (int i = packageMapSpec.MapFileRefs.Count - 1; i >= 0; i--)
                                            {
                                                if (packageMapSpec.MapFileRefs[i].File == fileIndex && packageMapSpec.MapFileRefs[i].Map == mapIndex)
                                                {
                                                    packageMapSpec.MapFileRefs.RemoveAt(i);
                                                    mapFileRefRemoved = true;
                                                    break;
                                                }
                                            }

                                            if (mapFileRefRemoved)
                                            {
                                                Console.WriteLine($"\tRemoved resource \"{packageMapSpec.Files[fileIndex].Name}\" to be loaded in map \"{packageMapSpec.Maps[mapIndex].Name}\" in \"{packageMapSpecPath}\"");
                                            }
                                            else
                                            {
                                                if (Verbose)
                                                {
                                                    Console.ForegroundColor = ConsoleColor.Red;
                                                    Console.Write("WARNING: ");
                                                    Console.ResetColor();
                                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                                    Console.WriteLine($"Resource \"{extraResource.Name}\" for map \"{packageMapSpec.Maps[mapIndex].Name}\" set to be removed was not found");
                                                    Console.ResetColor();
                                                }
                                            }

                                            continue;
                                        }

                                        // Add the extra resource now to the map/file references
                                        // before the resource that is normally loaded first
                                        int insertIndex = -1;
                                        bool alreadyExists = false;

                                        for (int i = 0; i < packageMapSpec.MapFileRefs.Count; i++)
                                        {
                                            if (packageMapSpec.MapFileRefs[i].Map == mapIndex)
                                            {
                                                insertIndex = i + 1;

                                                if (packageMapSpec.MapFileRefs[i].File == fileIndex)
                                                {
                                                    alreadyExists = true;
                                                    break;
                                                }
                                            }
                                        }

                                        // Prevent adding the same map file reference multiple times
                                        if (alreadyExists)
                                        {
                                            if (Verbose)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.Write("WARNING: ");
                                                Console.ResetColor();
                                                Console.ForegroundColor = ConsoleColor.Yellow;
                                                Console.WriteLine($"Extra resource \"{extraResource.Name}\" for map \"{packageMapSpec.Maps[mapIndex].Name}\" was already added, skipping");
                                                Console.ResetColor();
                                            }

                                            continue;
                                        }

                                        // Place the extra resource before or after another (if specified)
                                        if (!string.IsNullOrEmpty(extraResource.PlaceByName))
                                        {
                                            // First check that the resource trying to be added actually exists
                                            var placeBeforeResourcePath = PathToResource(extraResource.Name);

                                            if (placeBeforeResourcePath == string.Empty)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.Write("WARNING: ");
                                                Console.ResetColor();
                                                Console.ForegroundColor = ConsoleColor.Yellow;
                                                Console.WriteLine($"placeByName resource \"{extraResource.PlaceByName}\" not found for extra resource entry \"{extraResource.Name}\", using normal placement");
                                                Console.ResetColor();
                                            }
                                            else
                                            {
                                                // Find placement resource index
                                                int placeBeforeFileIndex = -1;

                                                for (int i = 0; i < packageMapSpec.Files.Count; i++)
                                                {
                                                    if (packageMapSpec.Files[i].Name.Contains(extraResource.PlaceByName))
                                                    {
                                                        placeBeforeFileIndex = i;
                                                        break;
                                                    }
                                                }

                                                // Find placement resource map-file reference
                                                for (int i = 0; i < packageMapSpec.MapFileRefs.Count; i++)
                                                {
                                                    if (packageMapSpec.MapFileRefs[i].Map == mapIndex && packageMapSpec.MapFileRefs[i].File == placeBeforeFileIndex)
                                                    {
                                                        insertIndex = i + (!extraResource.PlaceBefore ? 1 : 0);
                                                        break;
                                                    }
                                                }
                                            }
                                        }

                                        // Create the map-file reference and add it in the proper position
                                        var mapFileRef = new PackageMapSpecMapFileRef()
                                        {
                                            File = fileIndex,
                                            Map = mapIndex
                                        };

                                        if (insertIndex == -1 || insertIndex >= packageMapSpec.MapFileRefs.Count)
                                        {
                                            packageMapSpec.MapFileRefs.Add(mapFileRef);
                                        }
                                        else
                                        {
                                            packageMapSpec.MapFileRefs.Insert(insertIndex, mapFileRef);
                                        }

                                        // Serialize the JSON and replace it
                                        var serializerSettings = new JsonSerializerSettings();
                                        serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                        serializerSettings.Formatting = Formatting.Indented;
                                        var newPackageMapSpecJson = JsonConvert.SerializeObject(packageMapSpec, serializerSettings);

                                        try
                                        {
                                            File.Delete(packageMapSpecPath);
                                            File.WriteAllText(packageMapSpecPath, newPackageMapSpecJson);
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("ERROR: ");
                                            Console.ResetColor();
                                            Console.WriteLine($"Couldn't replace {packageMapSpecPath}");
                                            Console.WriteLine(ex);
                                            continue;
                                        }

                                        Console.WriteLine($"\tAdded extra resource \"{packageMapSpec.Files[fileIndex].Name}\" to be loaded in map \"{packageMapSpec.Maps[mapIndex].Name}\" in \"{packageMapSpecPath}\"");
                                }
                                }
                            }
                        }

                        // Find the .mapresources file this JSON file wants to edit, if needed
                        if (mod.AssetsInfo.NewAssets != null || mod.AssetsInfo.Maps != null || mod.AssetsInfo.Layers != null)
                        {
                            var assetsInfoFilenameParts = mod.Name.Split('/');
                            var mapResourcesFilename = assetsInfoFilenameParts[assetsInfoFilenameParts.Length - 1];
                            mapResourcesFilename = mapResourcesFilename.Substring(0, mapResourcesFilename.Length - 4) + "mapresources";

                            foreach (var file in resourceInfo.ChunkList)
                            {
                                var nameParts = file.ResourceName.FullFileName.Split('/');

                                if (nameParts[nameParts.Length - 1] == mapResourcesFilename)
                                {
                                    chunk = file;
                                    break;
                                }
                            }

                            if (chunk == null)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("ERROR: ");
                                Console.ResetColor();
                                Console.WriteLine($"Failed to find the .mapresources counterpart for AssetsInfo file: {mod.Name} - please check that the name for the AssetsInfo file is correct");
                                continue;
                            }

                            // Read the mapresources file data (it should be compressed)
                            byte[] mapResourcesBytes = new byte[chunk.SizeZ];

                            memoryStream.Seek(chunk.FileOffset, SeekOrigin.Begin);
                            long mapResourcesFileOffset = binaryReader.ReadInt64();

                            memoryStream.Seek(mapResourcesFileOffset, SeekOrigin.Begin);
                            memoryStream.Read(mapResourcesBytes, 0, (int)chunk.SizeZ);

                            // Decompress the data
                            byte[] decompressedMapResources = Oodle.Decompress(mapResourcesBytes, chunk.Size);

                            if (decompressedMapResources == null)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("ERROR: ");
                                Console.ResetColor();
                                Console.WriteLine($"Failed to decompress \"{chunk.ResourceName.NormalizedFileName}\" - are you trying to add assets in the wrong .resources archive?");
                                continue;
                            }

                            // Deserialize the decompressed data and add the new assets
                            var mapResourcesFile = MapResourcesFile.Parse(decompressedMapResources);

                            // Add layers
                            if (mod.AssetsInfo.Layers != null)
                            {
                                foreach (var newLayers in mod.AssetsInfo.Layers)
                                {
                                    if (mapResourcesFile.Layers.Contains(newLayers.Name))
                                    {
                                        if (Verbose)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("WARNING: ");
                                            Console.ResetColor();
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"Trying to add layer \"{newLayers.Name}\" that has already been added in \"{chunk.ResourceName.NormalizedFileName}\", skipping");
                                            Console.ResetColor();
                                        }

                                        continue;
                                    }

                                    mapResourcesFile.Layers.Add(newLayers.Name);
                                    Console.WriteLine($"\tAdded layer \"{newLayers.Name}\" to \"{chunk.ResourceName.NormalizedFileName}\" in \"{resourceInfo.Name}\"");
                                }
                            }

                            // Add maps
                            if (mod.AssetsInfo.Maps != null)
                            {
                                foreach (var newMaps in mod.AssetsInfo.Maps)
                                {
                                    if (mapResourcesFile.Maps.Contains(newMaps.Name))
                                    {
                                        if (Verbose)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("WARNING: ");
                                            Console.ResetColor();
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"Trying to add map \"{newMaps.Name}\" that has already been added in \"{chunk.ResourceName.NormalizedFileName}\", skipping");
                                            Console.ResetColor();
                                        }

                                        continue;
                                    }

                                    mapResourcesFile.Maps.Add(newMaps.Name);
                                    Console.WriteLine($"\tAdded map \"{newMaps.Name}\" to \"{chunk.ResourceName.NormalizedFileName}\" in \"{resourceInfo.Name}\"");
                                }
                            }

                            // Add assets
                            if (mod.AssetsInfo.NewAssets != null)
                            {
                                foreach (var newAsset in mod.AssetsInfo.NewAssets)
                                {
                                    if (string.IsNullOrEmpty(newAsset.Name) || string.IsNullOrWhiteSpace(newAsset.Name) ||
                                        string.IsNullOrEmpty(newAsset.MapResourceType) || string.IsNullOrWhiteSpace(newAsset.MapResourceType))
                                    {
                                        if (Verbose)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("WARNING: ");
                                            Console.ResetColor();
                                            Console.WriteLine($"Skipping empty resource declaration in \"{mod.Name}\"");
                                        }

                                        continue;
                                    }

                                    // Remove the asset if specified
                                    if (newAsset.Remove)
                                    {
                                        var newAssetTypeIndex = mapResourcesFile.AssetTypes.IndexOf(newAsset.MapResourceType);
                                        var assetToRemove = mapResourcesFile.Assets.FirstOrDefault(asset => asset.Name == newAsset.Name && asset.AssetTypeIndex == newAssetTypeIndex);

                                        if (assetToRemove == null)
                                        {
                                            if (Verbose)
                                            {
                                                Console.ForegroundColor = ConsoleColor.Red;
                                                Console.Write("WARNING: ");
                                                Console.ResetColor();
                                                Console.WriteLine($"Can't remove asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" because it doesn't exist in \"{chunk.ResourceName.NormalizedFileName}\"");
                                            }
                                        }
                                        else
                                        {
                                            mapResourcesFile.Assets.Remove(assetToRemove);
                                            Console.WriteLine($"\tRemoved asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" from \"{chunk.ResourceName.NormalizedFileName}\" in \"{resourceInfo.Name}\"");
                                        }

                                        continue;
                                    }

                                    bool alreadyExists = false;

                                    foreach (var existingAsset in mapResourcesFile.Assets)
                                    {
                                        if (existingAsset.Name == newAsset.Name && mapResourcesFile.AssetTypes[existingAsset.AssetTypeIndex] == newAsset.MapResourceType)
                                        {
                                            alreadyExists = true;
                                            break;
                                        }
                                    }

                                    if (alreadyExists)
                                    {
                                        if (Verbose)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("WARNING: ");
                                            Console.ResetColor();
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"Trying to add asset \"{newAsset.Name}\" that has already been added in \"{chunk.ResourceName.NormalizedFileName}\", skipping");
                                            Console.ResetColor();
                                        }

                                        continue;
                                    }

                                    // Find the asset type index
                                    int assetTypeIndex = mapResourcesFile.AssetTypes.FindIndex(type => type == newAsset.MapResourceType);

                                    // If not found, add the asset type at the end
                                    if (assetTypeIndex == -1)
                                    {
                                        mapResourcesFile.AssetTypes.Add(newAsset.MapResourceType);
                                        assetTypeIndex = mapResourcesFile.AssetTypes.Count - 1;

                                        Console.WriteLine($"\tAdded asset type \"{newAsset.MapResourceType}\" to \"{chunk.ResourceName.NormalizedFileName}\" in \"{resourceInfo.Name}\"");
                                    }

                                    // Determine where to place this new asset in map resources
                                    MapAsset placeByExistingAsset = null;
                                    int assetPosition = mapResourcesFile.Assets.Count;

                                    if (newAsset.PlaceByName != null)
                                    {
                                        if (newAsset.PlaceByType != null)
                                        {
                                            int placeByTypeIndex = mapResourcesFile.AssetTypes.IndexOf(newAsset.PlaceByType);

                                            if (placeByTypeIndex != -1)
                                            {
                                                placeByExistingAsset = mapResourcesFile.Assets.FirstOrDefault(asset => asset.Name == newAsset.PlaceByName && asset.AssetTypeIndex == placeByTypeIndex);
                                            }
                                        }
                                        else
                                        {
                                            placeByExistingAsset = mapResourcesFile.Assets.FirstOrDefault(asset => asset.Name == newAsset.PlaceByName);
                                        }

                                        if (placeByExistingAsset != null)
                                        {
                                            assetPosition = mapResourcesFile.Assets.IndexOf(placeByExistingAsset);

                                            if (!newAsset.PlaceBefore)
                                            {
                                                assetPosition++;
                                            }
                                        }
                                    }

                                    if (Verbose && placeByExistingAsset != null)
                                    {
                                        Console.WriteLine($"\tAsset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" will be added before asset \"{placeByExistingAsset.Name}\" with type \"{mapResourcesFile.AssetTypes[placeByExistingAsset.AssetTypeIndex]}\" to \"{chunk.ResourceName.NormalizedFileName}\" in \"{resourceInfo.Name}\"");
                                    }

                                    var newMapAsset = new MapAsset()
                                    {
                                        AssetTypeIndex = assetTypeIndex,
                                        Name = newAsset.Name,
                                        UnknownData4 = 128
                                    };

                                    if (assetPosition == mapResourcesFile.Assets.Count)
                                    {
                                        mapResourcesFile.Assets.Add(newMapAsset);
                                    }
                                    else
                                    {
                                        mapResourcesFile.Assets.Insert(assetPosition, newMapAsset);
                                    }

                                    Console.WriteLine($"\tAdded asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" to \"{chunk.ResourceName.NormalizedFileName}\" in \"{resourceInfo.Name}\"");
                                }
                            }

                            // Serialize the map resources data
                            decompressedMapResources = mapResourcesFile.ToByteArray();

                            // Compress the data
                            byte[] compressedMapResources = Oodle.Compress(decompressedMapResources, Oodle.OodleFormat.Kraken, Oodle.OodleCompressionLevel.Normal);

                            if (compressedMapResources == null)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("ERROR: ");
                                Console.ResetColor();
                                Console.WriteLine($"Failed to compress \"{chunk.ResourceName.NormalizedFileName}\"");
                                continue;
                            }

                            // Set the necessary info for the map resources
                            chunk.Size = decompressedMapResources.Length;
                            chunk.SizeZ = compressedMapResources.Length;
                            mod.UncompressedSize = decompressedMapResources.Length;
                            mod.FileBytes = compressedMapResources;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else if (mod.IsBlangJson)
                    {
                        // Handle custom .blang JSON files
                        var modName = mod.Name;
                        var modFilePathParts = modName.Split('/');
                        var name = modName.Remove(0, modFilePathParts[0].Length + 1);
                        mod.Name = name.Substring(0, name.Length - 4) + "blang";
                        chunk = GetChunk(mod.Name, resourceInfo);

                        if (chunk == null)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        chunk = GetChunk(mod.Name, resourceInfo);

                        if (chunk == null)
                        {
                            // This is a new mod, create a copy of it and add it to the new mods list
                            Mod newMod = new Mod(mod.Name);
                            newMod.FileBytes = new byte[mod.FileBytes.Length];

                            Array.Copy(mod.FileBytes, newMod.FileBytes, mod.FileBytes.Length);
                            resourceInfo.ModListNew.Add(newMod);

                            // Get the data to add to mapresources from the resource data file, if available
                            ResourceDataEntry resourceData;

                            if (!ResourceDataDictionary.TryGetValue(ResourceData.ResourceData.CalculateResourceFileNameHash(mod.Name), out resourceData))
                            {
                                continue;
                            }

                            if (string.IsNullOrEmpty(resourceData.MapResourceName) && string.IsNullOrWhiteSpace(resourceData.MapResourceType))
                            {
                                if (Verbose)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("WARNING: ");
                                    Console.ResetColor();
                                    Console.WriteLine($"Mapresources data for asset \"{mod.Name}\" is null, skipping");
                                }

                                continue;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(resourceData.MapResourceName))
                                {
                                    resourceData.MapResourceName = mod.Name;
                                }
                            }

                            // Find the mapresources chunk in the current container
                            // If this is a "gameresources" container, only search for "common.mapresources"
                            foreach (var file in resourceInfo.ChunkList)
                            {
                                if (file.ResourceName.NormalizedFileName.EndsWith(".mapresources"))
                                {
                                    if (resourceInfo.Name.StartsWith("gameresources") && file.ResourceName.NormalizedFileName.EndsWith("init.mapresources"))
                                    {
                                        continue;
                                    }

                                    chunk = file;
                                    break;
                                }
                            }

                            if (chunk == null)
                            {
                                continue;
                            }

                            // Read the mapresources file data (it should be compressed)
                            byte[] mapResourcesBytes = new byte[chunk.SizeZ];

                            memoryStream.Seek(chunk.FileOffset, SeekOrigin.Begin);
                            long mapResourcesFileOffset = binaryReader.ReadInt64();

                            memoryStream.Seek(mapResourcesFileOffset, SeekOrigin.Begin);
                            memoryStream.Read(mapResourcesBytes, 0, (int)chunk.SizeZ);

                            // Decompress the data
                            byte[] decompressedMapResources = Oodle.Decompress(mapResourcesBytes, chunk.Size);

                            if (decompressedMapResources == null)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("ERROR: ");
                                Console.ResetColor();
                                Console.WriteLine($"Failed to decompress \"{chunk.ResourceName.NormalizedFileName}\" - are you trying to add assets in the wrong .resources archive?");
                                continue;
                            }

                            // Deserialize the decompressed data and add the new asset
                            var mapResourcesFile = MapResourcesFile.Parse(decompressedMapResources);

                            // Add the asset info
                            bool alreadyExists = false;

                            foreach (var existingAsset in mapResourcesFile.Assets)
                            {
                                if (existingAsset.Name == resourceData.MapResourceName && mapResourcesFile.AssetTypes[existingAsset.AssetTypeIndex] == resourceData.MapResourceType)
                                {
                                    alreadyExists = true;
                                    break;
                                }
                            }

                            if (alreadyExists)
                            {
                                if (Verbose)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("WARNING: ");
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Trying to add asset \"{resourceData.MapResourceName}\" that has already been added in \"{chunk.ResourceName.NormalizedFileName}\", skipping");
                                    Console.ResetColor();
                                }

                                continue;
                            }

                            // Find the asset type index
                            int assetTypeIndex = mapResourcesFile.AssetTypes.FindIndex(type => type == resourceData.MapResourceType);

                            // If not found, add the asset type at the end
                            if (assetTypeIndex == -1)
                            {
                                mapResourcesFile.AssetTypes.Add(resourceData.MapResourceType);
                                assetTypeIndex = mapResourcesFile.AssetTypes.Count - 1;

                                Console.WriteLine($"\tAdded asset type \"{resourceData.MapResourceType}\" to \"{chunk.ResourceName.NormalizedFileName}\" in \"{resourceInfo.Name}\"");
                            }

                            mapResourcesFile.Assets.Add(new MapAsset()
                            {
                                AssetTypeIndex = assetTypeIndex,
                                Name = resourceData.MapResourceName,
                                UnknownData4 = 128
                            });

                            Console.WriteLine($"\tAdded asset \"{resourceData.MapResourceName}\" with type \"{resourceData.MapResourceType}\" to \"{chunk.ResourceName.NormalizedFileName}\" in \"{resourceInfo.Name}\"");

                            // Serialize the map resources data
                            decompressedMapResources = mapResourcesFile.ToByteArray();

                            // Compress the data
                            byte[] compressedMapResources = Oodle.Compress(decompressedMapResources, Oodle.OodleFormat.Kraken, Oodle.OodleCompressionLevel.Normal);

                            if (compressedMapResources == null)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("ERROR: ");
                                Console.ResetColor();
                                Console.WriteLine($"Failed to compress \"{chunk.ResourceName.NormalizedFileName}\"");
                                continue;
                            }

                            // Set the necessary info for the map resources
                            chunk.Size = decompressedMapResources.Length;
                            chunk.SizeZ = compressedMapResources.Length;

                            // Modify this mod object to "fake" it as if it was an AssetsInfo JSON file
                            mod.IsAssetsInfoJson = true;
                            mod.UncompressedSize = decompressedMapResources.Length;
                            mod.FileBytes = compressedMapResources;
                        }
                    }

                    memoryStream.Seek(chunk.FileOffset, SeekOrigin.Begin);

                    long fileOffset = binaryReader.ReadInt64();
                    long size = binaryReader.ReadInt64();
                    long sizeDiff = mod.FileBytes.Length - size;

                    // If the mod is a blang JSON file, modify the .blang file
                    if (mod.IsBlangJson)
                    {
                        BlangJson blangJson;

                        try
                        {
                            var serializerSettings = new JsonSerializerSettings();
                            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                            blangJson = JsonConvert.DeserializeObject<BlangJson>(Encoding.UTF8.GetString(mod.FileBytes), serializerSettings);

                            if (blangJson == null || blangJson.Strings.Count == 0)
                            {
                                throw new Exception();
                            }

                            foreach (var blangJsonString in blangJson.Strings)
                            {
                                if (blangJsonString == null || blangJsonString.Name == null || blangJsonString.Text == null)
                                {
                                    throw new Exception();
                                }
                            }
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("ERROR: ");
                            Console.ResetColor();
                            Console.WriteLine($"Failed to parse EternalMod/strings/{Path.GetFileNameWithoutExtension(mod.Name)}.json");
                            continue;
                        }

                        memoryStream.Seek(fileOffset, SeekOrigin.Begin);

                        byte[] blangFileBytes = new byte[size];
                        memoryStream.Read(blangFileBytes, 0, (int)size);

                        int res = BlangCrypt.IdCrypt(ref blangFileBytes, $"strings/{Path.GetFileName(mod.Name)}", true);

                        if (res != 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("ERROR: ");
                            Console.ResetColor();
                            Console.WriteLine($"Failed to parse {resourceInfo.Name}/{mod.Name}");
                            continue;
                        }

                        BlangFile blangFile;

                        using (var blangMemoryStream = new MemoryStream(blangFileBytes))
                        {
                            try
                            {
                                blangFile = BlangFile.ParseFromMemory(blangMemoryStream);
                            }
                            catch
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("ERROR: ");
                                Console.ResetColor();
                                Console.WriteLine($"Failed to parse {resourceInfo.Name}/{mod.Name} - are you trying to change strings in the wrong .resources archive?");
                                continue;
                            }

                        }

                        foreach (var blangJsonString in blangJson.Strings)
                        {
                            bool stringFound = false;

                            foreach (var blangString in blangFile.Strings)
                            {
                                if (blangJsonString.Name.Equals(blangString.Identifier))
                                {
                                    stringFound = true;
                                    blangString.Text = blangJsonString.Text;
                                    Console.WriteLine($"\tReplaced string \"{blangString.Identifier}\" to \"{mod.Name}\"");
                                    break;
                                }
                            }

                            if (stringFound)
                            {
                                continue;
                            }

                            blangFile.Strings.Add(new BlangString()
                            {
                                Identifier = blangJsonString.Name,
                                Text = blangJsonString.Text,
                            });

                            Console.WriteLine($"\tAdded string \"{blangJsonString.Name}\" to \"{mod.Name}\" in \"{resourceInfo.Name}\"");
                        }

                        byte[] cryptDataBuffer = blangFile.WriteToStream().ToArray();
                        res = BlangCrypt.IdCrypt(ref cryptDataBuffer, $"strings/{Path.GetFileName(mod.Name)}", false);

                        if (res != 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("ERROR: ");
                            Console.ResetColor();
                            Console.WriteLine($"Failed to parse {resourceInfo.Name}/{mod.Name}");
                            continue;
                        }

                        mod.FileBytes = cryptDataBuffer;
                    }

                    // We will need to expand the file if the new size is greater than the old one
                    // If its shorter, we will replace all the bytes and zero out the remaining bytes
                    if (sizeDiff > 0)
                    {
                        var length = memoryStream.Length;

                        // Expand the memory stream so the new file fits
                        memoryStream.SetLength(length + sizeDiff);
                        int toRead;

                        while (length > (fileOffset + size))
                        {
                            toRead = length - BufferSize >= (fileOffset + size) ? BufferSize : (int)(length - (fileOffset + size));
                            length -= toRead;
                            memoryStream.Seek(length, SeekOrigin.Begin);
                            memoryStream.Read(buffer, 0, toRead);
                            memoryStream.Seek(length + sizeDiff, SeekOrigin.Begin);
                            memoryStream.Write(buffer, 0, toRead);
                        }

                        // Write the new file bytes now that the file has been expanded
                        // and there's enough space
                        memoryStream.Seek(fileOffset, SeekOrigin.Begin);
                        memoryStream.Write(mod.FileBytes, 0, mod.FileBytes.Length);
                    }
                    else
                    {
                        memoryStream.Seek(fileOffset, SeekOrigin.Begin);
                        memoryStream.Write(mod.FileBytes, 0, mod.FileBytes.Length);

                        // Zero out the remaining bytes if the file is shorter
                        if (sizeDiff < 0)
                        {
                            memoryStream.Write(new byte[-sizeDiff], 0, (int)-sizeDiff);
                        }
                    }

                    // Replace the file size data
                    memoryStream.Seek(chunk.SizeOffset, SeekOrigin.Begin);
                    memoryStream.Write(BitConverter.GetBytes((long)mod.FileBytes.Length), 0, 8);

                    // Write the uncompressed size if we are modifying a map resources file
                    bool isMapResources = mod.IsAssetsInfoJson && mod.UncompressedSize != 0 && mod.FileBytes != null;
                    memoryStream.Write(BitConverter.GetBytes(isMapResources ? mod.UncompressedSize : (long)mod.FileBytes.Length), 0, 8);

                    // Clear the compression flag if needed
                    memoryStream.Seek(chunk.SizeOffset + 0x30, SeekOrigin.Begin);
                    memoryStream.WriteByte(isMapResources ? chunk.CompressionMode : (byte)0);

                    // If the file was expanded, update file offsets for every file after the one we replaced
                    if (sizeDiff > 0)
                    {
                        for (int i = resourceInfo.ChunkList.IndexOf(chunk) + 1; i < resourceInfo.ChunkList.Count; i++)
                        {
                            memoryStream.Seek(resourceInfo.ChunkList[i].FileOffset, SeekOrigin.Begin);
                            fileOffset = binaryReader.ReadInt64();
                            memoryStream.Seek(resourceInfo.ChunkList[i].FileOffset, SeekOrigin.Begin);
                            memoryStream.Write(BitConverter.GetBytes(fileOffset + sizeDiff), 0, 8);
                        }
                    }

                    if (!mod.IsBlangJson && !mod.IsAssetsInfoJson)
                    {
                        Console.WriteLine(string.Format("\tReplaced {0}", mod.Name));
                        fileCount++;
                    }
                }
            }

            if (fileCount > 0)
            {
                Console.Write("Number of files replaced: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Format("{0} file(s) ", fileCount));
                Console.ResetColor();
                Console.Write("in ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(resourceInfo.Path);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Adds new file chunks to the resource file
        /// </summary>
        /// <param name="memoryStream">memory stream where the resource file is stored</param>
        /// <param name="resourceInfo">resource info object</param>
        public static void AddChunks(MemoryStream memoryStream, ResourceInfo resourceInfo)
        {
            if (resourceInfo.ModListNew.Count == 0)
            {
                return;
            }

            // Copy individual sections
            byte[] header = new byte[resourceInfo.InfoOffset];
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Read(header, 0, header.Length);

            byte[] info = new byte[resourceInfo.NamesOffset - resourceInfo.InfoOffset];
            memoryStream.Seek(resourceInfo.InfoOffset, SeekOrigin.Begin);
            memoryStream.Read(info, 0, info.Length);

            byte[] nameOffsets = new byte[resourceInfo.NamesOffsetEnd - resourceInfo.NamesOffset];
            memoryStream.Seek(resourceInfo.NamesOffset, SeekOrigin.Begin);
            memoryStream.Read(nameOffsets, 0, nameOffsets.Length);

            byte[] names = new byte[resourceInfo.UnknownOffset - resourceInfo.NamesOffsetEnd];
            memoryStream.Seek(resourceInfo.NamesOffsetEnd, SeekOrigin.Begin);
            memoryStream.Read(names, 0, names.Length);

            byte[] unknown = new byte[resourceInfo.Dummy7Offset - resourceInfo.UnknownOffset];
            memoryStream.Seek(resourceInfo.UnknownOffset, SeekOrigin.Begin);
            memoryStream.Read(unknown, 0, unknown.Length);

            long nameIdsOffset = resourceInfo.Dummy7Offset + (resourceInfo.TypeCount * 4);

            byte[] typeIds = new byte[nameIdsOffset - resourceInfo.Dummy7Offset];
            memoryStream.Seek(resourceInfo.Dummy7Offset, SeekOrigin.Begin);
            memoryStream.Read(typeIds, 0, typeIds.Length);

            byte[] nameIds = new byte[resourceInfo.IdclOffset - nameIdsOffset];
            memoryStream.Seek(nameIdsOffset, SeekOrigin.Begin);
            memoryStream.Read(nameIds, 0, nameIds.Length);

            byte[] idcl = new byte[resourceInfo.DataOffset - resourceInfo.IdclOffset];
            memoryStream.Seek(resourceInfo.IdclOffset, SeekOrigin.Begin);
            memoryStream.Read(idcl, 0, idcl.Length);

            byte[] data = new byte[memoryStream.Length - resourceInfo.DataOffset];
            memoryStream.Seek(resourceInfo.DataOffset, SeekOrigin.Begin);
            memoryStream.Read(data, 0, data.Length);

            int infoOldLength = info.Length;
            int nameIdsOldLength = nameIds.Length;
            int newChunksCount = 0;

            // Find the resource data for the new mod files and set them
            foreach (var mod in resourceInfo.ModList)
            {
                if (mod.IsAssetsInfoJson && mod.AssetsInfo != null && mod.AssetsInfo.NewAssets != null)
                {
                    foreach (var newMod in resourceInfo.ModListNew)
                    {
                        foreach (var assetsInfoAssets in mod.AssetsInfo.NewAssets)
                        {
                            if (string.IsNullOrEmpty(assetsInfoAssets.Path) || string.IsNullOrWhiteSpace(assetsInfoAssets.Path))
                            {
                                continue;
                            }

                            if (assetsInfoAssets.Path == newMod.Name)
                            {
                                newMod.ResourceType = assetsInfoAssets.ResourceType;
                                newMod.Version = assetsInfoAssets.Version;
                                newMod.StreamDbHash = assetsInfoAssets.StreamDbHash;
                                newMod.SpecialByte1 = assetsInfoAssets.SpecialByte1;
                                newMod.SpecialByte2 = assetsInfoAssets.SpecialByte2;
                                newMod.SpecialByte3 = assetsInfoAssets.SpecialByte3;
                                Console.WriteLine(string.Format("\tSet resource type \"{0}\" (version: {1}, streamdb hash: {2}) for new file: {3}",
                                    newMod.ResourceType,
                                    newMod.Version,
                                    newMod.StreamDbHash,
                                    newMod.Name));
                                break;
                            }
                        }
                    }

                    continue;
                }
            }

            // Add the new mod files now
            foreach (var mod in resourceInfo.ModListNew)
            {
                if (resourceInfo.ContainsResourceWithName(mod.Name))
                {
                    if (Verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("WARNING: ");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Trying to add resource \"{mod.Name}\" that has already been added to \"{resourceInfo.Name}\", skipping");
                        Console.ResetColor();
                    }

                    continue;
                }

                // Skip custom files
                if (mod.IsAssetsInfoJson || mod.IsBlangJson)
                {
                    continue;
                }

                // Retrieve the resource data for this file (if needed & available)
                ResourceDataEntry resourceData;

                if (ResourceDataDictionary.TryGetValue(ResourceData.ResourceData.CalculateResourceFileNameHash(mod.Name), out resourceData))
                {
                    mod.ResourceType = mod.ResourceType == null ? resourceData.ResourceType : mod.ResourceType;
                    mod.Version = mod.Version == null ? resourceData.Version : mod.Version;
                    mod.StreamDbHash = mod.StreamDbHash == null ? resourceData.StreamDbHash : mod.StreamDbHash;
                    mod.SpecialByte1 = mod.SpecialByte1 == null ? resourceData.SpecialByte1 : mod.SpecialByte1;
                    mod.SpecialByte2 = mod.SpecialByte2 == null ? resourceData.SpecialByte2 : mod.SpecialByte2;
                    mod.SpecialByte3 = mod.SpecialByte3 == null ? resourceData.SpecialByte3 : mod.SpecialByte3;
                }

                // Use rs_streamfile by default if no data was found or specified
                if (mod.ResourceType == null && mod.Version == null && mod.StreamDbHash == null)
                {
                    mod.ResourceType = mod.ResourceType == null ? "rs_streamfile" : mod.ResourceType;
                    mod.Version = mod.Version == null ? 0 : mod.Version;
                    mod.StreamDbHash = mod.StreamDbHash == null ? 0 : mod.StreamDbHash;
                    mod.SpecialByte1 = 0;
                    mod.SpecialByte2 = 0;
                    mod.SpecialByte3 = 0;

                    if (Verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("WARNING: ");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"No resource data found for file: {mod.Name}");
                        Console.ResetColor();
                    }
                }

                // Check if the resource type name exists in the current container, add if it doesn't
                if (mod.ResourceType != null)
                {
                    if (resourceInfo.NamesList.FirstOrDefault(name => name.NormalizedFileName == mod.ResourceType) == default(ResourceName))
                    {
                        // Add type name
                        long typeLastOffset = BitConverter.ToInt64(nameOffsets.Skip(nameOffsets.Length - 8).Take(8).ToArray(), 0);
                        long typeLastNameOffset = 0;

                        for (int i = (int)typeLastOffset; i < names.Length; i++)
                        {
                            if (names[i] == '\x00')
                            {
                                typeLastNameOffset = i + 1;
                                break;
                            }
                        }

                        byte[] typeNameBytes = Encoding.UTF8.GetBytes(mod.ResourceType);
                        Array.Resize(ref names, names.Length + typeNameBytes.Length + 1);
                        Buffer.BlockCopy(typeNameBytes, 0, names, (int)typeLastNameOffset, typeNameBytes.Length);

                        // Add type name offset
                        byte[] typeNewCount = BitConverter.GetBytes(BitConverter.ToInt64(nameOffsets.Take(8).ToArray(), 0) + 1);
                        Buffer.BlockCopy(typeNewCount, 0, nameOffsets, 0, 8);
                        Array.Resize(ref nameOffsets, nameOffsets.Length + 8);
                        Buffer.BlockCopy(BitConverter.GetBytes(typeLastNameOffset), 0, nameOffsets, nameOffsets.Length - 8, 8);

                        // Add the type name to the list to keep the indexes in the proper order
                        resourceInfo.NamesList.Add(new ResourceName()
                        {
                            FullFileName = mod.ResourceType,
                            NormalizedFileName = mod.ResourceType
                        });

                        Console.WriteLine(string.Format("\tAdded resource type name \"{0}\" to \"{1}\"", mod.ResourceType, resourceInfo.Name));
                    }
                }

                // Add file name
                long lastOffset = BitConverter.ToInt64(nameOffsets.Skip(nameOffsets.Length - 8).Take(8).ToArray(), 0);
                long lastNameOffset = 0;

                for (int i = (int)lastOffset; i < names.Length; i++)
                {
                    if (names[i] == '\x00')
                    {
                        lastNameOffset = i + 1;
                        break;
                    }
                }

                byte[] nameBytes = Encoding.UTF8.GetBytes(mod.Name);
                Array.Resize(ref names, names.Length + nameBytes.Length + 1);
                Buffer.BlockCopy(nameBytes, 0, names, (int)lastNameOffset, nameBytes.Length);

                // Add name offset
                byte[] newCount = BitConverter.GetBytes(BitConverter.ToInt64(nameOffsets.Take(8).ToArray(), 0) + 1);
                Buffer.BlockCopy(newCount, 0, nameOffsets, 0, 8);
                Array.Resize(ref nameOffsets, nameOffsets.Length + 8);
                Buffer.BlockCopy(BitConverter.GetBytes(lastNameOffset), 0, nameOffsets, nameOffsets.Length - 8, 8);

                // Add the name to the list to keep the indexes in the proper order
                resourceInfo.NamesList.Add(new ResourceName()
                {
                    FullFileName = mod.Name,
                    NormalizedFileName = mod.Name
                });

                // Add data
                long fileOffset = 0;
                long placement = (0x10 - (data.Length % 0x10)) + 0x30;
                Array.Resize(ref data, (int)(data.Length + placement));
                fileOffset = data.Length + resourceInfo.DataOffset;
                Array.Resize(ref data, data.Length + mod.FileBytes.Length);
                Buffer.BlockCopy(mod.FileBytes, 0, data, data.Length - mod.FileBytes.Length, mod.FileBytes.Length);

                // Add the asset type nameId and the filename nameId in nameIds
                long nameId = 0;
                long nameIdOffset = 0;
                nameId = resourceInfo.GetResourceNameId(mod.Name);
                Array.Resize(ref nameIds, nameIds.Length + 8);
                nameIdOffset = (nameIds.Length / 8) - 1;
                Array.Resize(ref nameIds, nameIds.Length + 8);

                // Find the asset type name id, if it's not found, use zero
                long assetTypeNameId = resourceInfo.GetResourceNameId(mod.ResourceType);

                if (assetTypeNameId == -1)
                {
                    assetTypeNameId = 0;
                }

                // Add the asset type nameId
                Buffer.BlockCopy(BitConverter.GetBytes(assetTypeNameId), 0, nameIds, nameIds.Length - 16, 8);

                // Add the asset filename nameId
                Buffer.BlockCopy(BitConverter.GetBytes(nameId), 0, nameIds, nameIds.Length - 8, 8);

                // Add info
                byte[] lastInfo = info.Skip(info.Length - 0x90).ToArray();
                Array.Resize(ref info, info.Length + 0x90);
                Buffer.BlockCopy(lastInfo, 0, info, info.Length - 0x90, lastInfo.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(nameIdOffset), 0, info, info.Length - 0x70, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(fileOffset), 0, info, info.Length - 0x58, 8);
                Buffer.BlockCopy(BitConverter.GetBytes((long)mod.FileBytes.Length), 0, info, info.Length - 0x50, 8);
                Buffer.BlockCopy(BitConverter.GetBytes((long)mod.FileBytes.Length), 0, info, info.Length - 0x48, 8);

                // Set the DataMurmurHash
                Buffer.BlockCopy(BitConverter.GetBytes(mod.StreamDbHash.Value), 0, info, info.Length - 0x40, 8);

                // Set the StreamDB resource hash
                Buffer.BlockCopy(BitConverter.GetBytes(mod.StreamDbHash.Value), 0, info, info.Length - 0x30, 8);

                // Set the correct asset version
                Buffer.BlockCopy(BitConverter.GetBytes((int)mod.Version.Value), 0, info, info.Length - 0x28, 4);

                // Set the special byte values
                Buffer.BlockCopy(BitConverter.GetBytes((int)mod.SpecialByte1.Value), 0, info, info.Length - 0x24, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((int)mod.SpecialByte2.Value), 0, info, info.Length - 0x1E, 4);
                Buffer.BlockCopy(BitConverter.GetBytes((int)mod.SpecialByte3.Value), 0, info, info.Length - 0x1D, 4);

                // Clear the compression mode
                info[info.Length - 0x20] = 0;

                Console.WriteLine(string.Format("\tAdded {0}", mod.Name));
                newChunksCount++;
            }

            // Rebuild the entire container now
            long namesOffsetAdd = info.Length - infoOldLength;
            long newSize = nameOffsets.Length + names.Length;
            long unknownAdd = namesOffsetAdd + (newSize - resourceInfo.StringsSize);
            long typeIdsAdd = unknownAdd;
            long nameIdsAdd = typeIdsAdd;
            long idclAdd = nameIdsAdd + (nameIds.Length - nameIdsOldLength);
            long dataAdd = idclAdd;

            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.FileCount + newChunksCount), 0, header, 0x20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.FileCount2 + (newChunksCount * 2)), 0, header, 0x2C, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((int)newSize), 0, header, 0x38, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.NamesOffset + namesOffsetAdd), 0, header, 0x40, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.UnknownOffset + unknownAdd), 0, header, 0x48, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.UnknownOffset2 + unknownAdd), 0, header, 0x58, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.Dummy7Offset + typeIdsAdd), 0, header, 0x60, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.DataOffset + dataAdd), 0, header, 0x68, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.IdclOffset + idclAdd), 0, header, 0x74, 8);

            byte[] newOffsetBuffer = new byte[8];

            for (int i = 0, j = info.Length / 0x90; i < j; i++)
            {
                int fileOffset = 0x38 + (i * 0x90);
                Buffer.BlockCopy(info, fileOffset, newOffsetBuffer, 0, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToInt64(newOffsetBuffer, 0) + dataAdd), 0, info, fileOffset, 8);
            }

            long newContainerLength = header.Length + info.Length + nameOffsets.Length + names.Length + unknown.Length + typeIds.Length + nameIds.Length + idcl.Length + data.Length;
            memoryStream.SetLength(newContainerLength);
            memoryStream.Seek(0, SeekOrigin.Begin);
            memoryStream.Write(header, 0, header.Length);
            memoryStream.Write(info, 0, info.Length);
            memoryStream.Write(nameOffsets, 0, nameOffsets.Length);
            memoryStream.Write(names, 0, names.Length);
            memoryStream.Write(unknown, 0, unknown.Length);
            memoryStream.Write(typeIds, 0, typeIds.Length);
            memoryStream.Write(nameIds, 0, nameIds.Length);
            memoryStream.Write(idcl, 0, idcl.Length);
            memoryStream.Write(data, 0, data.Length);

            if (newChunksCount != 0)
            {
                Console.Write("Number of files added: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Format("{0} file(s) ", newChunksCount));
                Console.ResetColor();
                Console.Write("in ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(resourceInfo.Path);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Loads the sound mods present in the specified sound bank info object
        /// </summary>
        /// <param name="soundBankInfo">sound bank info object</param>
        public static void LoadSoundMods(SoundBankInfo soundBankInfo)
        {
            int fileCount = 0;

            using (var fileStream = new FileStream(soundBankInfo.Path, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var memoryStream = new MemoryStream())
                {
                    // Copy the stream into memory for faster manipulation of the data
                    fileStream.CopyTo(memoryStream);

                    // Load the sound mods
                    foreach (var soundMod in soundBankInfo.ModList)
                    {
                        // Parse the identifier of the sound we want to replace
                        var soundFileNameWithoutExtension = Path.GetFileNameWithoutExtension(soundMod.Name);
                        int soundModId;

                        // First, assume that the file name (without extension) is the sound id
                        if (!int.TryParse(soundFileNameWithoutExtension, out soundModId))
                        {
                            // If this is not the case, try to find the id at the end of the filename
                            // Format: _#id{id here}
                            var splittedName = soundFileNameWithoutExtension.Split('_');
                            var idString = splittedName[splittedName.Length - 1];
                            var idStringData = idString.Split('#');

                            if (idStringData.Length == 2 && idStringData[0] == "id")
                            {
                                int.TryParse(idStringData[1], out soundModId);
                            }
                        }

                        if (soundModId == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("WARNING: ");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Bad file name for sound file \"{soundMod.Name}\" - sound file names should be named after the sound id, or have the sound id at the end of the filename with format \"_id#{{id here}}\", skipping");
                            Console.WriteLine($"Examples of valid sound file names:");
                            Console.WriteLine($"icon_music_boss_end_2_id#347947739.ogg");
                            Console.WriteLine($"347947739.ogg");
                            Console.ResetColor();
                            continue;
                        }

                        // Determine the sound format by extension
                        var soundExtension = Path.GetExtension(soundMod.Name);
                        int encodedSize = soundMod.FileBytes.Length;
                        int decodedSize = encodedSize;
                        bool needsEncoding = false;
                        short format = -1;

                        switch (soundExtension)
                        {
                            case ".wem":
                                format = 3;
                                break;
                            case ".ogg":
                            case ".opus":
                                format = 2;
                                break;
                            default:
                                needsEncoding = true;
                                break;
                        }

                        // If the file needs to be encoded, encode it using opusenc first
                        if (needsEncoding)
                        {
                            try
                            {
                                var opusEncPath = Path.Combine(BasePath, "opusenc.exe");
                                encodedSize = -1;

                                if (!File.Exists(opusEncPath))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("WARNING: ");
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Couldn't find \"{opusEncPath}\" to encode \"{soundMod.Name}\", skipping");
                                    Console.ResetColor();
                                    continue;
                                }

                                var opusFileData = SoundEncoding.EncodeSoundModFileToOpus(opusEncPath, soundMod);

                                if (opusFileData != null)
                                {
                                    soundMod.FileBytes = opusFileData;
                                    encodedSize = soundMod.FileBytes.Length;
                                    format = 2;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Error.Write("ERROR: ");
                                Console.ResetColor();
                                Console.Error.WriteLine($"While loading sound mod file {soundMod.Name}: {ex}");
                                continue;
                            }
                        }

                        if (format == -1)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("WARNING: ");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Couldn't determine the sound file format for \"{soundMod.Name}\", skipping");
                            Console.ResetColor();
                            continue;
                        }
                        else if (format == 2)
                        {
                            try
                            {
                                // Determine the decoded size of the sound file
                                // if the format is .ogg or .opus
                                var opusDecPath = Path.Combine(BasePath, "opusdec.exe");

                                if (!File.Exists(opusDecPath))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("WARNING: ");
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Couldn't find \"{opusDecPath}\" to decode \"{soundMod.Name}\", skipping");
                                    Console.ResetColor();
                                    continue;
                                }

                                decodedSize = SoundEncoding.GetDecodedOpusSoundModFileSize(opusDecPath, soundMod);
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Error.Write("ERROR: ");
                                Console.ResetColor();
                                Console.Error.WriteLine($"While loading sound mod file {soundMod.Name}: {ex}");
                                continue;
                            }
                        }

                        if (decodedSize == -1 || encodedSize == -1)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("WARNING: ");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Unsupported sound mod file format for file \"{soundMod.Name}\", skipping");

                            if (soundExtension == ".ogg")
                            {
                                Console.WriteLine($".ogg files must be in the Ogg Opus format, Ogg Vorbis is not supported");
                            }

                            Console.WriteLine($"Supported sound mod file formats are: {string.Join(", ", SoundEncoding.SupportedFileFormats)}");

                            Console.ResetColor();
                            continue;
                        }

                        // Load the sound mod into the sound bank
                        bool soundFound = false;

                        using (var binaryReader = new BinaryReader(memoryStream, Encoding.Default, true))
                        {
                            // Write the sound replacement data at the end of the sound bank
                            memoryStream.Seek(0, SeekOrigin.End);
                            uint soundModOffset = (uint)memoryStream.Position;
                            memoryStream.Write(soundMod.FileBytes, 0, soundMod.FileBytes.Length);

                            // Read the info and the header sizes
                            memoryStream.Seek(4, SeekOrigin.Begin);

                            uint infoSize = binaryReader.ReadUInt32();
                            uint headerSize = binaryReader.ReadUInt32();

                            memoryStream.Seek(headerSize, SeekOrigin.Current);

                            // Loop through all the sound info to find the sound we want to replace
                            for (uint i = 0, j = (infoSize - headerSize) / 32; i < j; i++)
                            {
                                memoryStream.Seek(8, SeekOrigin.Current);
                                uint soundId = binaryReader.ReadUInt32();

                                if (soundId != soundModId)
                                {
                                    memoryStream.Seek(20, SeekOrigin.Current);
                                    continue;
                                }

                                soundFound = true;

                                // Replace the sound info offset and sizes
                                memoryStream.Write(BitConverter.GetBytes(encodedSize), 0, 4);
                                memoryStream.Write(BitConverter.GetBytes(soundModOffset), 0, 4);
                                memoryStream.Write(BitConverter.GetBytes(decodedSize), 0, 4);
                                ushort currentFormat = binaryReader.ReadUInt16();

                                // Skip the last 6 bytes that we don't need
                                memoryStream.Seek(6, SeekOrigin.Current);

                                if (currentFormat != format)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("WARNING: ");
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Format mismatch: sound file \"{soundMod.Name}\" needs to be format {currentFormat} ({(currentFormat == 3 ? ".wem" : string.Join(", ", SoundEncoding.SupportedOggConversionFileFormats))})");
                                    Console.WriteLine($"The sound will be replaced but it might not work in-game.");
                                    Console.ResetColor();

                                    // To avoid showing this warning multiple times, in case we find
                                    // another sound with the same id
                                    format = (short)currentFormat;
                                }

                                // We don't to break here, since some sounds are duplicated
                                // and we don't know which one the game uses, so it's better to
                                // replace all of their occurences
                            }
                        }

                        if (!soundFound)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("WARNING: ");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Couldn't find sound with id \"{soundModId}\" in \"{soundBankInfo.Name}\", sound will not be replaced");
                            Console.ResetColor();
                            continue;
                        }

                        Console.WriteLine(string.Format("\tReplaced sound with id {0} [{1}]", soundModId, soundMod.Name));
                        fileCount++;
                    }

                    // Copy the memory stream into the filestream now
                    fileStream.SetLength(memoryStream.Length);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }

            if (fileCount > 0)
            {
                Console.Write("Number of sounds replaced: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Format("{0} sound(s) ", fileCount));
                Console.ResetColor();
                Console.Write("in ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(soundBankInfo.Path);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Gets the path to the .resources file for the specified resource name
        /// </summary>
        /// <param name="name">resource name</param>
        /// <returns>the path to the .resources file for the specified resource name, empty string if it wasn't found</returns>
        public static string PathToResource(string name)
        {
            SearchOption searchOption = SearchOption.AllDirectories;
            string searchPath = BasePath;
            string searchPattern;

            // Support for DLC1 hub resources files
            // It has the same name as the base game hub resources file, so we will need
            // to adjust the search pattern to find the one we want depending on the folder name of the mod
            if (name.ToLower().StartsWith("dlc_hub"))
            {
                string dlcHubFileName = name.Substring(4, name.Length - 4);
                searchPattern = Path.Combine("game", "dlc", "hub", $"{dlcHubFileName}");
            }
            else if (name.ToLower().StartsWith("hub"))
            {
                searchPattern = Path.Combine("game", "hub", $"{name}");
            }
            else
            {
                searchPattern = name;

                if (name.Contains("gameresources") || name.Contains("warehouse") || name.Contains("meta") || name.Contains(".streamdb"))
                {
                    searchOption = SearchOption.TopDirectoryOnly;
                }
                else
                {
                    searchPath = Path.Combine(BasePath, "game");
                }
            }

            try
            {
                DirectoryInfo searchDirectory = new DirectoryInfo(searchPath);

                foreach (var resourceFile in searchDirectory.EnumerateFiles(searchPattern, searchOption))
                {
                    return resourceFile.FullName;
                }

                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the path to the .snd file for the specified sound bank name
        /// </summary>
        /// <param name="name">resource name</param>
        /// <returns>the path to the .snd file for the specified sound bank name, empty string if it wasn't found</returns>
        public static string PathToSoundBank(string name)
        {
            string searchPath = Path.Combine(BasePath, "sound", "soundbanks", "pc");
            string searchPattern = $"{name}.snd";

            try
            {
                DirectoryInfo baseFolder = new DirectoryInfo(searchPath);

                foreach (var soundBankFile in baseFolder.EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly))
                {
                    return soundBankFile.FullName;
                }

                return string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">program args</param>
        /// <returns>0 if no errors occured, 1 if errors occured</returns>
        public static int Main(string[] args)
        {
            // Parse arguments
            if (args.Length == 0)
            {
                Console.WriteLine("Loads DOOM Eternal mods from ZIPs or loose files in 'Mods' folder into the game installation specified in the game path");
                Console.WriteLine("USAGE: EternalModLoader <game path> [OPTIONS]");
                Console.WriteLine("OPTIONS:");
                Console.WriteLine("\t--list-res - List the .resources files that will be modified and exit.");
                Console.WriteLine("\t--verbose - Print more information during the mod loading process.");
                return 1;
            }

            BasePath = Path.Combine(args[0], "base");

            if (!Directory.Exists(BasePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write("ERROR: ");
                Console.ResetColor();
                Console.Error.WriteLine("Game directory does not exist!");
                return 1;
            }

            bool listResources = false;

            if (args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i].Equals("--list-res"))
                    {
                        listResources = true;
                    }
                    else if (args[i].Equals("--verbose"))
                    {
                        Verbose = true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("ERROR: ");
                        Console.ResetColor();
                        Console.Error.WriteLine(string.Format("Unknown option '{0}'", args[i]));
                        return 1;
                    }
                }
            }

            // Load the compressed resource data file
            var resourceDataFilePath = Path.Combine(BasePath, ResourceDataFileName);

            if (File.Exists(resourceDataFilePath))
            {
                try
                {
                    ResourceDataDictionary = ResourceData.ResourceData.Parse(resourceDataFilePath);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("ERROR: ");
                    Console.ResetColor();
                    Console.WriteLine($"There was an error while loading \"{ResourceDataFileName}\"");
                    Console.WriteLine(e);
                }
            }
            else
            {
                if (Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("WARNING: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(ResourceDataFileName);
                    Console.ResetColor();
                    Console.WriteLine(" was not found! There will be issues when adding existing new assets to containers...");
                }
            }

            // Find zipped mods
            foreach (string zippedMod in Directory.EnumerateFiles("Mods", "*.zip", SearchOption.TopDirectoryOnly))
            {
                int zippedModCount = 0;
                List<string> modFileNameList = new List<string>();

                using (var zipArchive = ZipFile.OpenRead(zippedMod))
                {
                    foreach (var zipEntry in zipArchive.Entries)
                    {
                        // Skip directories
                        if (zipEntry.Name.Equals("") && zipEntry.FullName.EndsWith("/"))
                        {
                            continue;
                        }

                        modFileNameList.Add(zipEntry.FullName);
                    }

                    foreach (string modFileName in modFileNameList)
                    {
                        bool isSoundMod = false;
                        string modFile = modFileName;
                        var modFilePathParts = modFile.Split('/');

                        if (modFilePathParts.Length < 2)
                        {
                            continue;
                        }

                        string resourceName = modFilePathParts[0];

                        // Old mods compatibility
                        if (resourceName.Equals("generated"))
                        {
                            resourceName = "gameresources";
                        }
                        else
                        {
                            // Remove the resource name from the path
                            modFile = modFileName.Remove(0, resourceName.Length + 1);
                        }

                        // Check if this is a sound mod or not
                        var resourcePath = PathToResource(resourceName + ".resources");

                        if (resourcePath == string.Empty)
                        {
                            resourcePath = PathToSoundBank(resourceName);

                            if (resourcePath != string.Empty)
                            {
                                isSoundMod = true;
                            }
                        }

                        if (isSoundMod)
                        {
                            // Get the sound bank info object, create it if it doesn't exist
                            var soundBankInfo = SoundBankList.FirstOrDefault(sndBank => sndBank.Name.Equals(resourceName));

                            if (soundBankInfo == null)
                            {
                                soundBankInfo = new SoundBankInfo()
                                {
                                    ModList = new List<SoundMod>(),
                                    Name = resourceName,
                                    Path = resourcePath
                                };

                                SoundBankList.Add(soundBankInfo);
                            }

                            // Create the mod object and read the unzipped files
                            if (!listResources)
                            {
                                // Skip unsupported formats
                                var soundExtension = Path.GetExtension(modFile);

                                if (!SoundEncoding.SupportedFileFormats.Contains(soundExtension))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("WARNING: ");
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Unsupported sound mod file format \"{soundExtension}\" for file \"{modFile}\"");
                                    Console.ResetColor();
                                    continue;
                                }

                                // Load the sound mod
                                SoundMod sndMod = new SoundMod();
                                sndMod.Name = Path.GetFileName(modFile);

                                var stream = zipArchive.GetEntry(modFileName).Open();

                                using (var memoryStream = new MemoryStream())
                                {
                                    stream.CopyTo(memoryStream);
                                    sndMod.FileBytes = memoryStream.ToArray();
                                }

                                soundBankInfo.ModList.Add(sndMod);
                                zippedModCount++;
                            }
                        }
                        else
                        {
                            // Get the resource object
                            var resource = ResourceList.FirstOrDefault(res => res.Name.Equals(resourceName));

                            if (resource == null)
                            {
                                resource = new ResourceInfo(resourceName, PathToResource(resourceName + ".resources"));
                                ResourceList.Add(resource);
                            }

                            // Create the mod object and read the unzipped files
                            if (!listResources)
                            {
                                Mod mod = new Mod(modFile);
                                var stream = zipArchive.GetEntry(modFileName).Open();

                                using (var memoryStream = new MemoryStream())
                                {
                                    stream.CopyTo(memoryStream);
                                    mod.FileBytes = memoryStream.ToArray();
                                }

                                // Read the JSON files in 'assetsinfo' under 'EternalMod'
                                if (modFilePathParts[1].Equals("EternalMod", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (modFilePathParts.Length == 4
                                        && modFilePathParts[2].Equals("assetsinfo", StringComparison.InvariantCultureIgnoreCase)
                                        && Path.GetExtension(modFilePathParts[3]).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        try
                                        {
                                            var serializerSettings = new JsonSerializerSettings();
                                            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                            mod.AssetsInfo = JsonConvert.DeserializeObject<AssetsInfo>(Encoding.UTF8.GetString(mod.FileBytes), serializerSettings);
                                            mod.IsAssetsInfoJson = true;
                                            mod.FileBytes = null;
                                        }
                                        catch
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("ERROR: ");
                                            Console.ResetColor();
                                            Console.WriteLine($"Failed to parse EternalMod/assetsinfo/{Path.GetFileNameWithoutExtension(mod.Name)}.json");
                                            continue;
                                        }
                                    }
                                    else if (modFilePathParts.Length == 4
                                        && modFilePathParts[2].Equals("strings", StringComparison.InvariantCultureIgnoreCase)
                                        && Path.GetExtension(modFilePathParts[3]).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // Detect custom language files
                                        mod.IsBlangJson = true;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                resource.ModList.Add(mod);
                                zippedModCount++;
                            }
                        }
                    }
                }

                if (zippedModCount > 0 && !listResources)
                {
                    Console.Write("Found ");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write(string.Format("{0} file(s) ", zippedModCount));
                    Console.ResetColor();
                    Console.Write("in archive ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(zippedMod);
                    Console.ResetColor();
                    Console.WriteLine();
                }
            }

            // Find unzipped mods
            int unzippedModCount = 0;

            foreach (var file in Directory.EnumerateFiles("Mods", "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".zip"))
                {
                    continue;
                }

                string[] modFilePathParts = file.Split(Path.DirectorySeparatorChar);

                if (modFilePathParts.Length <= 2)
                {
                    continue;
                }

                bool isSoundMod = false;
                string resourceName = modFilePathParts[1];
                string fileName = string.Empty;

                // Old mods compatibility
                if (resourceName.ToLower().Equals("generated"))
                {
                    resourceName = "gameresources";
                    fileName = file.Remove(0, modFilePathParts[0].Length + 1).Replace('\\', '/');
                }
                else
                {
                    fileName = file.Remove(0, modFilePathParts[0].Length + resourceName.Length + 2).Replace('\\', '/');
                }

                // Check if this is a sound mod or not
                var resourcePath = PathToResource(resourceName + ".resources");

                if (resourcePath == string.Empty)
                {
                    resourcePath = PathToSoundBank(resourceName);

                    if (resourcePath != string.Empty)
                    {
                        isSoundMod = true;
                    }
                }

                if (isSoundMod)
                {
                    // Get the sound bank info object, create it if it doesn't exist
                    var soundBankInfo = SoundBankList.FirstOrDefault(sndBank => sndBank.Name.Equals(resourceName));

                    if (soundBankInfo == null)
                    {
                        soundBankInfo = new SoundBankInfo()
                        {
                            ModList = new List<SoundMod>(),
                            Name = resourceName,
                            Path = resourcePath
                        };

                        SoundBankList.Add(soundBankInfo);
                    }

                    // Create the mod object and read the unzipped files
                    if (!listResources)
                    {
                        // Skip unsupported formats
                        var soundExtension = Path.GetExtension(fileName);

                        if (!SoundEncoding.SupportedFileFormats.Contains(soundExtension))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("WARNING: ");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Unsupported sound mod file format \"{soundExtension}\" for file \"{fileName}\"");
                            Console.ResetColor();
                            continue;
                        }

                        // Load the sound mod
                        SoundMod sndMod = new SoundMod();
                        sndMod.Name = Path.GetFileName(fileName);

                        using (var streamReader = new StreamReader(file))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                streamReader.BaseStream.CopyTo(memoryStream);
                                sndMod.FileBytes = memoryStream.ToArray();
                            }
                        }

                        soundBankInfo.ModList.Add(sndMod);
                        unzippedModCount++;
                    }
                }
                else
                {
                    // Get the resource object
                    var resource = ResourceList.FirstOrDefault(res => res.Name.Equals(resourceName));

                    if (resource == null)
                    {
                        resource = new ResourceInfo(resourceName, PathToResource(resourceName + ".resources"));
                        ResourceList.Add(resource);
                    }

                    // Create the mod object and read the files
                    if (!listResources)
                    {
                        Mod mod = new Mod(fileName);

                        using (var streamReader = new StreamReader(file))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                streamReader.BaseStream.CopyTo(memoryStream);
                                mod.FileBytes = memoryStream.ToArray();
                            }
                        }

                        // Read the JSON files in 'assetsinfo' under 'EternalMod'
                        if (modFilePathParts[2].Equals("EternalMod", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (modFilePathParts.Length == 5
                                && modFilePathParts[3].Equals("assetsinfo", StringComparison.InvariantCultureIgnoreCase)
                                && Path.GetExtension(modFilePathParts[4]).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                            {
                                try
                                {
                                    var serializerSettings = new JsonSerializerSettings();
                                    serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                    mod.AssetsInfo = JsonConvert.DeserializeObject<AssetsInfo>(Encoding.UTF8.GetString(mod.FileBytes), serializerSettings);
                                    mod.IsAssetsInfoJson = true;
                                    mod.FileBytes = null;
                                }
                                catch
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("ERROR: ");
                                    Console.ResetColor();
                                    Console.WriteLine($"Failed to parse EternalMod/assetsinfo/{Path.GetFileNameWithoutExtension(mod.Name)}.json");
                                    continue;
                                }
                            }
                            else if (modFilePathParts.Length == 5
                                && modFilePathParts[3].Equals("strings", StringComparison.InvariantCultureIgnoreCase)
                                && Path.GetExtension(modFilePathParts[4]).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                            {
                                // Detect custom language files
                                mod.IsBlangJson = true;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        resource.ModList.Add(mod);
                        unzippedModCount++;
                    }
                }
            }

            if (unzippedModCount > 0 && !listResources)
            {
                Console.Write("Found ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.Write(string.Format("{0} file(s) ", unzippedModCount));
                Console.ResetColor();
                Console.Write("in ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("'Mods' ");
                Console.ResetColor();
                Console.WriteLine("folder...");
            }

            // List the resources that will be modified
            if (listResources)
            {
                // Resource file mods
                foreach (var resource in ResourceList)
                {
                    if (resource.Path == string.Empty)
                    {
                        continue;
                    }

                    if (Path.DirectorySeparatorChar == '\\')
                    {
                        Console.WriteLine($".{resource.Path.Substring(resource.Path.IndexOf("\\base\\"))}");
                    }
                    else
                    {
                        Console.WriteLine($".{resource.Path.Substring(resource.Path.IndexOf("/base/"))}");
                    }
                }

                // Sound mods
                foreach (var soundBank in SoundBankList)
                {
                    if (soundBank.Path == string.Empty)
                    {
                        continue;
                    }

                    if (Path.DirectorySeparatorChar == '\\')
                    {
                        Console.WriteLine($".{soundBank.Path.Substring(soundBank.Path.IndexOf("\\base\\"))}");
                    }
                    else
                    {
                        Console.WriteLine($".{soundBank.Path.Substring(soundBank.Path.IndexOf("/base/"))}");
                    }
                }

                return 0;
            }

            // Load the resource file mods
            foreach (var resource in ResourceList)
            {
                if (string.IsNullOrEmpty(resource.Path))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("WARNING: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(resource.Name + ".resources");
                    Console.ResetColor();
                    Console.Write(" was not found! Skipping ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(string.Format("{0} file(s)", resource.ModList.Count));
                    Console.ResetColor();
                    Console.WriteLine("...");
                    continue;
                }

                ReadResource(resource);
                LoadMods(resource);
            }

            // Load the sound mods
            foreach (var soundBank in SoundBankList)
            {
                if (string.IsNullOrEmpty(soundBank.Path))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("WARNING: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(soundBank.Name + ".snd");
                    Console.ResetColor();
                    Console.Write(" was not found! Skipping ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(string.Format("{0} file(s)", soundBank.ModList.Count));
                    Console.ResetColor();
                    Console.WriteLine("...");
                    continue;
                }

                LoadSoundMods(soundBank);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished.");
            Console.ResetColor();
            return 0;
        }
    }
}