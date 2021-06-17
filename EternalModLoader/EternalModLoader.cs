using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BlangParser;
using EternalModLoader.Mods;
using EternalModLoader.Mods.Resources;
using EternalModLoader.Mods.Resources.ResourceData;
using EternalModLoader.Mods.Resources.Blang;
using EternalModLoader.Mods.Resources.MapResources;
using EternalModLoader.Mods.Sounds;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;

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
        /// Mod loader version
        /// </summary>
        public const int Version = 9;

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
        /// Slow mod loading mode
        /// Produces slightly lighter files, but it's slower
        /// </summary>
        public static bool SlowMode;

        /// <summary>
        /// Compress uncompressed textures while loading mods?
        /// </summary>
        public static bool CompressTextures;

        /// <summary>
        /// Divinity magic header for compressed texture files
        /// </summary>
        public static byte[] DivinityMagic = new byte[] { 0x44, 0x49, 0x56, 0x49, 0x4E, 0x49, 0x54, 0x59 };

        /// <summary>
        /// Resource list
        /// </summary>
        public static List<ResourceContainer> ResourceList = new List<ResourceContainer>();

        /// <summary>
        /// Sound container list
        /// </summary>
        public static List<SoundContainer> SoundContainerList = new List<SoundContainer>();

        /// <summary>
        /// Resource data dictionary, indexed by file name
        /// </summary>
        public static Dictionary<ulong, ResourceDataEntry> ResourceDataDictionary = new Dictionary<ulong, ResourceDataEntry>();

        /// <summary>
        /// Buffer size for file operations
        /// This will be set to the cluster size of the disk on runtime
        /// Will default to 4096 if the cluster size can't be determined
        /// </summary>
        public static int BufferSize = -1;

        /// <summary>
        /// Reusable buffer for file operations
        /// This will be initialized alongside the buffer size
        /// </summary>
        public static byte[] FileBuffer = null;

        /// <summary>
        /// Reads the resource container from the specified resource container object
        /// </summary>
        /// <param name="resourceContainer">resource container object</param>
        public static void ReadResource(FileStream fileStream, ResourceContainer resourceContainer)
        {
            using (var binaryReader = new BinaryReader(fileStream, Encoding.Default, true))
            {
                fileStream.Position = 0x20;
                int fileCount = binaryReader.ReadInt32();
                int unknownCount = binaryReader.ReadInt32();
                int dummy2Num = binaryReader.ReadInt32(); // Number of TypeIds
                int pathStringCount = binaryReader.ReadInt32();
                fileStream.Read(FileBuffer, 0, 8);
                int stringsSize = binaryReader.ReadInt32(); // Total size of nameOffsets and names
                fileStream.Read(FileBuffer, 0, 4);
                long namesOffset = binaryReader.ReadInt64();
                long namesEnd = binaryReader.ReadInt64();
                long infoOffset = binaryReader.ReadInt64();
                fileStream.Read(FileBuffer, 0, 8);
                long dummy7OffOrg = binaryReader.ReadInt64(); // Offset of TypeIds, needs addition to get offset of nameIds
                long dataOff = binaryReader.ReadInt64();
                fileStream.Read(FileBuffer, 0, 4);
                long idclOff = binaryReader.ReadInt64();

                // Read all the file names now
                fileStream.Position = namesOffset;
                long namesNum = binaryReader.ReadInt64();

                // Skip the name offsets
                fileStream.Position = namesOffset + 8 + (namesNum * 8);

                long namesOffsetEnd = fileStream.Position;
                long namesSize = namesEnd - fileStream.Position;
                List<ResourceName> namesList = new List<ResourceName>();
                char[] nameBuffer = new char[512];
                int charCount = 0;

                for (int i = 0; i < namesSize; i++)
                {
                    byte currentByte = binaryReader.ReadByte();

                    if (currentByte == '\x00' || i == namesSize - 1)
                    {
                        if (charCount == 0)
                        {
                            continue;
                        }

                        nameBuffer[charCount] = '\x00';

                        // Support full filenames and "normalized" filenames (backwards compatibility)
                        string fullFileName;

                        unsafe
                        {
                            fixed (char* name = nameBuffer)
                            {
                                fullFileName = new string(name);
                            }
                        }

                        namesList.Add(new ResourceName()
                        {
                            FullFileName = fullFileName,
                            NormalizedFileName = fullFileName
                        });

                        charCount = 0;
                        continue;
                    }

                    nameBuffer[charCount++] = (char)currentByte;
                }

                // Read path string indexes to associate the offsets to the names
                fileStream.Position = dummy7OffOrg + (dummy2Num * 4) + 0x8;

                for (int i = 1; i < pathStringCount; i += 2)
                {
                    long nameIndex = binaryReader.ReadInt64();
                    binaryReader.ReadInt64();
                    resourceContainer.ResourceNamePathRelativeOffsets.Add(i * 8, namesList[(int)nameIndex]);
                }

                resourceContainer.FileCount = fileCount;
                resourceContainer.TypeCount = dummy2Num;
                resourceContainer.StringsSize = stringsSize;
                resourceContainer.NamesOffset = namesOffset;
                resourceContainer.InfoOffset = infoOffset;
                resourceContainer.Dummy7Offset = dummy7OffOrg;
                resourceContainer.DataOffset = dataOff;
                resourceContainer.IdclOffset = idclOff;
                resourceContainer.UnknownCount = unknownCount;
                resourceContainer.FileCount2 = fileCount * 2;
                resourceContainer.NamesOffsetEnd = namesOffsetEnd;
                resourceContainer.UnknownOffset = namesEnd;
                resourceContainer.UnknownOffset2 = namesEnd;
                resourceContainer.NamesList = namesList;

                ReadChunkInfo(fileStream, binaryReader, resourceContainer);
            }
        }

        /// <summary>
        /// Reads the info of all the chunks in the resource file
        /// </summary>
        /// <param name="fileStream">file stream used to read the resource file</param>
        /// <param name="binaryReader">binary reader used to read the resource file</param>
        /// <param name="resourceContainer">resource container object</param>
        private static void ReadChunkInfo(FileStream fileStream, BinaryReader binaryReader, ResourceContainer resourceContainer)
        {
            fileStream.Seek(resourceContainer.InfoOffset, SeekOrigin.Begin);

            for (int i = 0; i < resourceContainer.FileCount; i++)
            {
                fileStream.Read(FileBuffer, 0, 32);
                ResourceChunk chunk = new ResourceChunk();
                long nameId = binaryReader.ReadInt64();
                fileStream.Read(FileBuffer, 0, 24);
                chunk.SizeOffset = fileStream.Position;
                chunk.FileOffset = chunk.SizeOffset - 8;
                chunk.SizeZ = binaryReader.ReadInt64();
                chunk.Size = binaryReader.ReadInt64();
                chunk.ResourceName = resourceContainer.ResourceNamePathRelativeOffsets[((int)nameId + 1) * 8];
                chunk.ResourceName.NormalizedFileName = Utils.NormalizeResourceFilename(chunk.ResourceName.FullFileName);
                resourceContainer.ChunkList.Add(chunk);
                fileStream.Read(FileBuffer, 0, 64);
            }
        }

        /// <summary>
        /// Find a chunk in a resource container by filename
        /// </summary>
        /// <param name="name">file name</param>
        /// <param name="resourceContainer">resource container object</param>
        /// <returns>the ResourceChunk object, null if it wasn't found</returns>
        public static ResourceChunk GetChunk(string name, ResourceContainer resourceContainer)
        {
            foreach (var chunk in resourceContainer.ChunkList)
            {
                if (chunk.ResourceName.FullFileName == name || chunk.ResourceName.NormalizedFileName == name)
                {
                    return chunk;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets the optimal buffer size for file operations
        /// </summary>
        /// <param name="driveRoot">root of the drive to get the cluster size of</param>
        public static void SetOptimalBufferSize(string driveRoot)
        {
            // Find and set the optimal buffer size
            BufferSize = Utils.GetClusterSize(driveRoot);

            if (BufferSize == -1)
            {
                BufferSize = 4096;
            }

            FileBuffer = new byte[BufferSize];
        }

        /// <summary>
        /// Loads the mods present in the specified resource container object
        /// </summary>
        /// <param name="resourceContainer">resource container object</param>
        public static void LoadMods(ResourceContainer resourceContainer)
        {
            using (var fileStream = new FileStream(resourceContainer.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, BufferSize, FileOptions.SequentialScan))
            {
                // Read the resource file and reset the position afterwards
                ReadResource(fileStream, resourceContainer);
                fileStream.Position = 0;

                // Load the mods
                if (!SlowMode)
                {
                    ReplaceChunks(fileStream, resourceContainer);
                    AddChunks(fileStream, resourceContainer);
                }
                else
                {
                    using (var memoryStream = new MemoryStream((int)fileStream.Length))
                    {
                        // Copy the stream into memory for faster manipulation of the data
                        fileStream.CopyTo(memoryStream);

                        // Load the mods
                        ReplaceChunks(memoryStream, resourceContainer);
                        AddChunks(memoryStream, resourceContainer);

                        // Copy the memory stream into the filestream now
                        fileStream.SetLength(memoryStream.Length);
                        fileStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.CopyTo(fileStream);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the mod file data in the given chunk of the specified container
        /// </summary>
        /// <param name="stream">memory/file stream of the container</param>
        /// <param name="binaryReader">binary reader for the container</param>
        /// <param name="resourceContainer">resource container</param>
        /// <param name="chunk">chunk</param>
        /// <param name="modFile">mod file</param>
        /// <param name="compressedSize">compressed size to set in the file entry</param>
        /// <param name="uncompressedSize">uncompressed size to set in the file entry</param>
        /// <param name="compressionMode">compression mode to use, pass null to not modify it</param>
        public static void SetModFileDataForContainerChunk(
            Stream stream,
            BinaryReader binaryReader,
            ResourceContainer resourceContainer,
            ResourceChunk chunk,
            ResourceModFile modFile,
            long compressedSize,
            long uncompressedSize,
            byte? compressionMode)
        {
            if (!SlowMode)
            {
                // Add the data at the end of the container
                long dataSectionLength = stream.Length - resourceContainer.DataOffset;
                long placement = (0x10 - (dataSectionLength % 0x10)) + 0x30;
                long dataOffset = stream.Length + placement;

                stream.Seek(0, SeekOrigin.End);
                stream.Write(new byte[placement], 0, (int)placement);
                modFile.CopyFileDataToStream(stream);

                // Set the new data offset
                stream.Seek(chunk.FileOffset, SeekOrigin.Begin);
                stream.Write(FastBitConverter.GetBytes(dataOffset), 0, 8);
            }
            else
            {
                stream.Seek(chunk.FileOffset, SeekOrigin.Begin);

                long fileOffset = binaryReader.ReadInt64();
                binaryReader.ReadInt64();
                long sizeDiff = modFile.FileData.Length - chunk.SizeZ;

                // We will need to expand the file if the new size is greater than the old one
                // If its shorter, we will replace all the bytes and zero out the remaining bytes
                if (sizeDiff > 0)
                {
                    var length = stream.Length;

                    // Expand the memory stream so the new file fits
                    stream.SetLength(length + sizeDiff);
                    int toRead;

                    while (length > (fileOffset + chunk.SizeZ))
                    {
                        toRead = length - BufferSize >= (fileOffset + chunk.SizeZ) ? BufferSize : (int)(length - (fileOffset + chunk.SizeZ));
                        length -= toRead;
                        stream.Seek(length, SeekOrigin.Begin);
                        stream.Read(FileBuffer, 0, toRead);
                        stream.Seek(length + sizeDiff, SeekOrigin.Begin);
                        stream.Write(FileBuffer, 0, toRead);
                    }

                    // Write the new file bytes now that the file has been expanded
                    // and there's enough space
                    stream.Seek(fileOffset, SeekOrigin.Begin);
                    modFile.CopyFileDataToStream(stream);
                }
                else
                {
                    stream.Seek(fileOffset, SeekOrigin.Begin);
                    modFile.CopyFileDataToStream(stream);

                    // Zero out the remaining bytes if the file is shorter
                    if (sizeDiff < 0)
                    {
                        stream.Write(new byte[-sizeDiff], 0, (int)-sizeDiff);
                    }
                }

                // If the file was expanded, update file offsets for every file after the one we replaced
                if (sizeDiff > 0)
                {
                    for (int i = resourceContainer.ChunkList.IndexOf(chunk) + 1; i < resourceContainer.ChunkList.Count; i++)
                    {
                        stream.Seek(resourceContainer.ChunkList[i].FileOffset, SeekOrigin.Begin);
                        fileOffset = binaryReader.ReadInt64();
                        stream.Seek(resourceContainer.ChunkList[i].FileOffset, SeekOrigin.Begin);
                        stream.Write(FastBitConverter.GetBytes(fileOffset + sizeDiff), 0, 8);
                    }
                }
            }

            // Update chunk sizes
            chunk.Size = uncompressedSize;
            chunk.SizeZ = compressedSize;

            // Replace the file size data
            if (SlowMode)
            {
                stream.Seek(chunk.SizeOffset, SeekOrigin.Begin);
            }

            stream.Write(FastBitConverter.GetBytes(chunk.SizeZ), 0, 8);
            stream.Write(FastBitConverter.GetBytes(chunk.Size), 0, 8);

            // Clear the compression flag
            if (compressionMode.HasValue)
            {
                stream.Read(FileBuffer, 0, 0x20);
                stream.WriteByte(compressionMode.Value);
            }
        }

        /// <summary>
        /// Replaces the chunks of the files with the modded ones
        /// </summary>
        /// <param name="stream">file/memory stream for the resource file</param>
        /// <param name="resourceContainer">resource container object</param>
        public static void ReplaceChunks(Stream stream, ResourceContainer resourceContainer)
        {
            // For map resources modifications
            ResourceChunk mapResourcesChunk = null;
            MapResourcesFile mapResourcesFile = null;
            byte[] originalDecompressedMapResourcesData = null;
            bool invalidMapResources = false;

            // For .blang file modifications
            Dictionary<string, BlangFileEntry> blangFileEntries = new Dictionary<string, BlangFileEntry>();

            // For packagemapspec JSON modifications
            string packageMapSpecPath = string.Empty;
            PackageMapSpec packageMapSpec = null;
            bool invalidPackageMapSpec = false;
            bool wasPackageMapSpecModified = false;

            int fileCount = 0;

            using (var binaryReader = new BinaryReader(stream, Encoding.Default, true))
            {
                // Load mod files now
                foreach (var modFile in resourceContainer.ModFileList.OrderByDescending(mod => mod.Parent.LoadPriority))
                {
                    ResourceChunk chunk = null;

                    // Handle AssetsInfo JSON files
                    if (modFile.IsAssetsInfoJson && modFile.AssetsInfo != null)
                    {
                        // Add the extra resources to packagemapspec.json if specified
                        if (modFile.AssetsInfo.Resources != null)
                        {
                            // Deserialize the packagemapspec JSON if it hasn't been deserialized yet
                            if (packageMapSpec == null && !invalidPackageMapSpec)
                            {
                                packageMapSpecPath = Path.Combine(BasePath, PackageMapSpecJsonFileName);

                                if (!File.Exists(packageMapSpecPath))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Error.Write("ERROR: ");
                                    Console.ResetColor();
                                    Console.Error.WriteLine($"{packageMapSpecPath} not found while trying to add extra resources for level {resourceContainer.Name}");
                                    invalidPackageMapSpec = true;
                                }
                                else
                                {
                                    var packageMapSpecFileBytes = File.ReadAllBytes(packageMapSpecPath);

                                    try
                                    {
                                        // Try to parse the JSON
                                        var serializerSettings = new JsonSerializerSettings();
                                        serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                        packageMapSpec = JsonConvert.DeserializeObject<PackageMapSpec>(Encoding.UTF8.GetString(packageMapSpecFileBytes), serializerSettings);
                                    }
                                    catch
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.Error.Write("ERROR: ");
                                        Console.ResetColor();
                                        Console.Error.WriteLine($"Failed to parse {packageMapSpecPath} - malformed JSON?");
                                        invalidPackageMapSpec = true;
                                    }
                                }
                            }

                            // Add the extra resources, then rewrite the JSON
                            if (packageMapSpec != null && !invalidPackageMapSpec)
                            {
                                foreach (var extraResource in modFile.AssetsInfo.Resources)
                                {
                                    // First check that the resource trying to be added actually exists
                                    var extraResourcePath = PathToResource(extraResource.Name);

                                    if (extraResourcePath == string.Empty)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.Write("WARNING: ");
                                        Console.ResetColor();
                                        Console.ForegroundColor = ConsoleColor.Yellow;
                                        Console.WriteLine($"Trying to add non-existing extra resource \"{extraResource.Name}\" to \"{resourceContainer.Name}\", skipping");
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

                                    // Special cases for the hubs
                                    string modFileMapName = Path.GetFileNameWithoutExtension(modFile.Name);

                                    if (resourceContainer.Name.StartsWith("dlc_hub"))
                                    {
                                        modFileMapName = "game/dlc/hub/hub";
                                    }
                                    else if (resourceContainer.Name.StartsWith("hub"))
                                    {
                                        modFileMapName = "game/hub/hub";
                                    }

                                    for (int i = 0; i < packageMapSpec.Maps.Count; i++)
                                    {
                                        if (packageMapSpec.Maps[i].Name.EndsWith(modFileMapName))
                                        {
                                            mapIndex = i;
                                            break;
                                        }
                                    }

                                    if (fileIndex == -1)
                                    {
                                        BufferedConsole.Flush();
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.Error.Write("ERROR: ");
                                        Console.ResetColor();
                                        Console.Error.WriteLine($"Invalid extra resource {extraResource.Name}, skipping");
                                        continue;
                                    }

                                    if (mapIndex == -1)
                                    {
                                        BufferedConsole.Flush();
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Console.Error.Write("ERROR: ");
                                        Console.ResetColor();
                                        Console.Error.WriteLine($"Map reference not found for {modFile.Name}, skipping");
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
                                            BufferedConsole.WriteLine($"\tRemoved resource \"{packageMapSpec.Files[fileIndex].Name}\" to be loaded in map \"{packageMapSpec.Maps[mapIndex].Name}\"");
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

                                    // If the resource is already referenced to be loaded in the map, delete it first
                                    // to allow us to move it wherever we want
                                    for (int i = packageMapSpec.MapFileRefs.Count - 1; i >= 0; i--)
                                    {
                                        if (packageMapSpec.MapFileRefs[i].File == fileIndex && packageMapSpec.MapFileRefs[i].Map == mapIndex)
                                        {
                                            packageMapSpec.MapFileRefs.RemoveAt(i);

                                            if (Verbose)
                                            {
                                                Console.WriteLine($"\tResource \"{packageMapSpec.Files[fileIndex].Name}\" being added to map \"{packageMapSpec.Maps[mapIndex].Name}\" already exists. The load order will be modified as specified.");
                                            }

                                            break;
                                        }
                                    }

                                    // Add the extra resource now to the map/file references
                                    // before the resource that normally appears last in the list for the map
                                    int insertIndex = -1;

                                    for (int i = 0; i < packageMapSpec.MapFileRefs.Count; i++)
                                    {
                                        if (packageMapSpec.MapFileRefs[i].Map == mapIndex)
                                        {
                                            // If specified, place the resource as the first resource for the map (highest priority)
                                            if (extraResource.PlaceFirst)
                                            {
                                                insertIndex = i;
                                                break;
                                            }

                                            insertIndex = i + 1;
                                        }
                                    }

                                    // Place the extra resource before or after another (if specified)
                                    if (!string.IsNullOrEmpty(extraResource.PlaceByName) && !extraResource.PlaceFirst)
                                    {
                                        // First check that the placeByName resource actually exists
                                        var placeBeforeResourcePath = PathToResource(extraResource.PlaceByName);

                                        if (placeBeforeResourcePath == string.Empty)
                                        {
                                            BufferedConsole.Flush();
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

                                    BufferedConsole.Write($"\tAdded extra resource \"{packageMapSpec.Files[fileIndex].Name}\" to be loaded in map \"{packageMapSpec.Maps[mapIndex].Name}\"");

                                    if (extraResource.PlaceFirst)
                                    {
                                        BufferedConsole.WriteLine(" with the highest priority.");
                                    }
                                    else if (!string.IsNullOrEmpty(extraResource.PlaceByName) && insertIndex != -1)
                                    {
                                        BufferedConsole.WriteLine($" {(extraResource.PlaceBefore ? "before" : "after")} \"{extraResource.PlaceByName}\"");
                                    }
                                    else
                                    {
                                        BufferedConsole.WriteLine(" with the lowest priority");
                                    }

                                    wasPackageMapSpecModified = true;
                                }
                            }
                        }

                        // Add new assets to .mapresources
                        if (modFile.AssetsInfo.Assets != null || modFile.AssetsInfo.Maps != null || modFile.AssetsInfo.Layers != null)
                        {
                            // First, find, read and deserialize the .mapresources file in this container
                            // If this is a "gameresources" container, only search for "common.mapresources"
                            if (mapResourcesFile == null && !invalidMapResources)
                            {
                                foreach (var file in resourceContainer.ChunkList)
                                {
                                    if (file.ResourceName.NormalizedFileName.EndsWith(".mapresources"))
                                    {
                                        if (resourceContainer.Name.StartsWith("gameresources") && file.ResourceName.NormalizedFileName.EndsWith("init.mapresources"))
                                        {
                                            continue;
                                        }

                                        mapResourcesChunk = file;

                                        // Read the mapresources file data (it should be compressed)
                                        byte[] mapResourcesBytes = new byte[mapResourcesChunk.SizeZ];

                                        stream.Seek(mapResourcesChunk.FileOffset, SeekOrigin.Begin);
                                        long mapResourcesFileOffset = binaryReader.ReadInt64();

                                        stream.Seek(mapResourcesFileOffset, SeekOrigin.Begin);
                                        stream.Read(mapResourcesBytes, 0, (int)mapResourcesChunk.SizeZ);

                                        // Decompress the data
                                        originalDecompressedMapResourcesData = Oodle.Decompress(mapResourcesBytes, mapResourcesChunk.Size);

                                        if (originalDecompressedMapResourcesData == null)
                                        {
                                            invalidMapResources = true;
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Error.Write("ERROR: ");
                                            Console.ResetColor();
                                            Console.Error.WriteLine($"Failed to decompress \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" - are you trying to add assets in the wrong .resources archive?");
                                            break;
                                        }

                                        // Deserialize the decompressed data
                                        mapResourcesFile = MapResourcesFile.Parse(originalDecompressedMapResourcesData);
                                        break;
                                    }
                                }
                            }

                            // Don't add anything if the file was not found or couldn't be decompressed
                            if (mapResourcesFile == null || invalidMapResources)
                            {
                                modFile.DisposeFileData();
                                continue;
                            }

                            // Add layers
                            if (modFile.AssetsInfo.Layers != null)
                            {
                                foreach (var newLayers in modFile.AssetsInfo.Layers)
                                {
                                    if (mapResourcesFile.Layers.Contains(newLayers.Name))
                                    {
                                        if (Verbose)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("WARNING: ");
                                            Console.ResetColor();
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"Trying to add layer \"{newLayers.Name}\" that has already been added in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\", skipping");
                                            Console.ResetColor();
                                        }

                                        continue;
                                    }

                                    mapResourcesFile.Layers.Add(newLayers.Name);
                                    BufferedConsole.WriteLine($"\tAdded layer \"{newLayers.Name}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
                                }
                            }

                            // Add maps
                            if (modFile.AssetsInfo.Maps != null)
                            {
                                foreach (var newMaps in modFile.AssetsInfo.Maps)
                                {
                                    if (mapResourcesFile.Maps.Contains(newMaps.Name))
                                    {
                                        if (Verbose)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("WARNING: ");
                                            Console.ResetColor();
                                            Console.ForegroundColor = ConsoleColor.Yellow;
                                            Console.WriteLine($"Trying to add map \"{newMaps.Name}\" that has already been added in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\", skipping");
                                            Console.ResetColor();
                                        }

                                        continue;
                                    }

                                    mapResourcesFile.Maps.Add(newMaps.Name);
                                    BufferedConsole.WriteLine($"\tAdded map \"{newMaps.Name}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
                                }
                            }

                            // Add assets
                            if (modFile.AssetsInfo.Assets != null)
                            {
                                foreach (var newAsset in modFile.AssetsInfo.Assets)
                                {
                                    if (string.IsNullOrEmpty(newAsset.Name) || string.IsNullOrWhiteSpace(newAsset.Name) ||
                                        string.IsNullOrEmpty(newAsset.MapResourceType) || string.IsNullOrWhiteSpace(newAsset.MapResourceType))
                                    {
                                        if (Verbose)
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Write("WARNING: ");
                                            Console.ResetColor();
                                            Console.WriteLine($"Skipping empty resource declaration in \"{modFile.Name}\"");
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
                                                Console.WriteLine($"Can't remove asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" because it doesn't exist in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\"");
                                            }
                                        }
                                        else
                                        {
                                            mapResourcesFile.Assets.Remove(assetToRemove);
                                            BufferedConsole.WriteLine($"\tRemoved asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" from \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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
                                            Console.WriteLine($"Trying to add asset \"{newAsset.Name}\" that has already been added in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\", skipping");
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

                                        BufferedConsole.WriteLine($"\tAdded asset type \"{newAsset.MapResourceType}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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
                                        BufferedConsole.WriteLine($"\tAsset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" will be added before asset \"{placeByExistingAsset.Name}\" with type \"{mapResourcesFile.AssetTypes[placeByExistingAsset.AssetTypeIndex]}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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

                                    BufferedConsole.WriteLine($"\tAdded asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
                                }
                            }
                        }

                        modFile.DisposeFileData();
                        continue;
                    }
                    else if (modFile.IsBlangJson)
                    {
                        // Handle custom .blang JSON files
                        var modName = modFile.Name;
                        var modFilePathParts = modName.Split('/');
                        var name = modName.Remove(0, modFilePathParts[0].Length + 1);
                        modFile.Name = name.Substring(0, name.Length - 4) + "blang";
                        chunk = GetChunk(modFile.Name, resourceContainer);

                        if (chunk == null)
                        {
                            modFile.DisposeFileData();
                            continue;
                        }
                    }
                    else
                    {
                        chunk = GetChunk(modFile.Name, resourceContainer);

                        if (chunk == null)
                        {
                            // This is a new mod, add it to the new mods list
                            resourceContainer.NewModFileList.Add(modFile);

                            // Get the data to add to mapresources from the resource data file, if available
                            ResourceDataEntry resourceData;

                            if (!ResourceDataDictionary.TryGetValue(ResourceData.CalculateResourceFileNameHash(modFile.Name), out resourceData))
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
                                    Console.WriteLine($"Mapresources data for asset \"{modFile.Name}\" is null, skipping");
                                }

                                continue;
                            }
                            else
                            {
                                if (string.IsNullOrEmpty(resourceData.MapResourceName))
                                {
                                    resourceData.MapResourceName = modFile.Name;
                                }
                            }

                            // First, find, read and deserialize the .mapresources file in this container
                            // If this is a "gameresources" container, only search for "common.mapresources"
                            if (mapResourcesFile == null && !invalidMapResources)
                            {
                                foreach (var file in resourceContainer.ChunkList)
                                {
                                    if (file.ResourceName.NormalizedFileName.EndsWith(".mapresources"))
                                    {
                                        if (resourceContainer.Name.StartsWith("gameresources") && file.ResourceName.NormalizedFileName.EndsWith("init.mapresources"))
                                        {
                                            continue;
                                        }

                                        mapResourcesChunk = file;

                                        // Read the mapresources file data (it should be compressed)
                                        byte[] mapResourcesBytes = new byte[mapResourcesChunk.SizeZ];

                                        stream.Seek(mapResourcesChunk.FileOffset, SeekOrigin.Begin);
                                        long mapResourcesFileOffset = binaryReader.ReadInt64();

                                        stream.Seek(mapResourcesFileOffset, SeekOrigin.Begin);
                                        stream.Read(mapResourcesBytes, 0, (int)mapResourcesChunk.SizeZ);

                                        // Decompress the data
                                        originalDecompressedMapResourcesData = Oodle.Decompress(mapResourcesBytes, mapResourcesChunk.Size);

                                        if (originalDecompressedMapResourcesData == null)
                                        {
                                            invalidMapResources = true;
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Error.Write("ERROR: ");
                                            Console.ResetColor();
                                            Console.Error.WriteLine($"Failed to decompress \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" - are you trying to add assets in the wrong .resources archive?");
                                            break;
                                        }

                                        // Deserialize the decompressed data
                                        mapResourcesFile = MapResourcesFile.Parse(originalDecompressedMapResourcesData);
                                        break;
                                    }
                                }
                            }

                            // Don't add anything if the file was not found or couldn't be decompressed
                            if (mapResourcesFile == null || invalidMapResources)
                            {
                                continue;
                            }

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
                                    Console.WriteLine($"Trying to add asset \"{resourceData.MapResourceName}\" that has already been added in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\", skipping");
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

                                BufferedConsole.WriteLine($"\tAdded asset type \"{resourceData.MapResourceType}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
                            }

                            mapResourcesFile.Assets.Add(new MapAsset()
                            {
                                AssetTypeIndex = assetTypeIndex,
                                Name = resourceData.MapResourceName,
                                UnknownData4 = 128
                            });

                            BufferedConsole.WriteLine($"\tAdded asset \"{resourceData.MapResourceName}\" with type \"{resourceData.MapResourceType}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
                            continue;
                        }
                    }

                    // Parse blang JSON files
                    if (modFile.IsBlangJson)
                    {
                        // Read the .blang file in the container if it hasn't been read yet
                        string blangFilePath = $"strings/{Path.GetFileName(modFile.Name)}";
                        BlangFileEntry blangFileEntry;
                        bool exists = blangFileEntries.TryGetValue(blangFilePath, out blangFileEntry);

                        if (!exists)
                        {
                            stream.Seek(chunk.FileOffset, SeekOrigin.Begin);
                            long fileOffset = binaryReader.ReadInt64();

                            stream.Seek(fileOffset, SeekOrigin.Begin);

                            byte[] blangFileBytes = new byte[chunk.Size];
                            stream.Read(blangFileBytes, 0, (int)chunk.Size);

                            var blangMemoryStream = BlangCrypt.IdCrypt(blangFileBytes, blangFilePath, true);

                            if (blangMemoryStream == null)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Error.Write("ERROR: ");
                                Console.ResetColor();
                                Console.Error.WriteLine($"Failed to parse {resourceContainer.Name}/{modFile.Name}");
                                continue;
                            }

                            try
                            {
                                blangFileEntry = new BlangFileEntry(BlangFile.ParseFromMemory(blangMemoryStream), chunk);
                                blangFileEntries.Add(blangFilePath, blangFileEntry);
                            }
                            catch
                            {
                                blangFileEntries.Add(blangFilePath, null);
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Error.Write("ERROR: ");
                                Console.ResetColor();
                                Console.Error.WriteLine($"Failed to parse {resourceContainer.Name}/{modFile.Name} - are you trying to change strings in the wrong .resources archive?");
                                continue;
                            }
                        }

                        if (blangFileEntry == null || blangFileEntry.BlangFile == null || blangFileEntry.Chunk == null)
                        {
                            continue;
                        }

                        // Read the blang JSON and add the strings to the .blang file
                        BlangJson blangJson;

                        try
                        {
                            var serializerSettings = new JsonSerializerSettings();
                            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                            blangJson = JsonConvert.DeserializeObject<BlangJson>(Encoding.UTF8.GetString(modFile.FileData.GetBuffer()), serializerSettings);

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
                            Console.Error.Write("ERROR: ");
                            Console.ResetColor();
                            Console.Error.WriteLine($"Failed to parse EternalMod/strings/{Path.GetFileNameWithoutExtension(modFile.Name)}.json");
                            continue;
                        }

                        foreach (var blangJsonString in blangJson.Strings)
                        {
                            bool stringFound = false;

                            foreach (var blangString in blangFileEntry.BlangFile.Strings)
                            {
                                if (blangJsonString.Name == blangString.Identifier)
                                {
                                    stringFound = true;
                                    blangString.Text = blangJsonString.Text;
                                    BufferedConsole.WriteLine($"\tReplaced string \"{blangString.Identifier}\" to \"{modFile.Name}\"");
                                    break;
                                }
                            }

                            if (stringFound)
                            {
                                continue;
                            }

                            blangFileEntry.BlangFile.Strings.Add(new BlangString()
                            {
                                Identifier = blangJsonString.Name,
                                Text = blangJsonString.Text,
                            });

                            BufferedConsole.WriteLine($"\tAdded string \"{blangJsonString.Name}\" to \"{modFile.Name}\" in \"{resourceContainer.Name}\"");
                            blangFileEntry.WasModified = true;
                        }

                        continue;
                    }

                    // Replace the mod file data now
                    long compressedSize = modFile.FileData.Length;
                    long uncompressedSize = compressedSize;
                    byte? compressionMode = 0;

                    // If this is a texture, check if it's compressed, or compress it if necessary
                    if (chunk.ResourceName.NormalizedFileName.EndsWith(".tga"))
                    {
                        // Get the texture data buffer
                        var textureDataBuffer = modFile.FileData.GetBuffer();
                        bool isCompressed = Utils.IsDivinityCompressedTexture(textureDataBuffer, DivinityMagic);

                        if (isCompressed)
                        {
                            // This is a compressed texture, read the uncompressed size
                            uncompressedSize = FastBitConverter.ToInt64(textureDataBuffer, 8);

                            // Get the compressed texture data by removing the header from the memory stream buffer
                            Buffer.BlockCopy(textureDataBuffer, 16, textureDataBuffer, 0, textureDataBuffer.Length - 16);
                            modFile.FileData.SetLength(modFile.FileData.Length - 16);
                            compressionMode = 2;

                            if (Verbose)
                            {
                                BufferedConsole.WriteLine($"\tSuccessfully set compressed texture data for file \"{modFile.Name}\"");
                            }
                        }
                        else if (CompressTextures)
                        {
                            // Compress the texture
                            var compressedData = Oodle.Compress(modFile.FileData.GetBuffer(), Oodle.OodleFormat.Kraken, Oodle.OodleCompressionLevel.Normal);
                            modFile.FileData.Dispose();
                            modFile.FileData = new MemoryStream(compressedData, 0, compressedData.Length, false);
                            compressedSize = compressedData.Length;
                            compressionMode = 2;

                            if (Verbose)
                            {
                                BufferedConsole.WriteLine($"\tSuccessfully compressed texture file \"{modFile.Name}\"");
                            }
                        }
                    }

                    SetModFileDataForContainerChunk(stream, binaryReader, resourceContainer, chunk, modFile, compressedSize, uncompressedSize, compressionMode);

                    BufferedConsole.WriteLine(string.Format("\tReplaced {0}", modFile.Name));
                    fileCount++;
                }

                // Modify packagemapspec if needed
                if (packageMapSpec != null && wasPackageMapSpecModified)
                {
                    // Serialize the JSON and replace it
                    var serializerSettings = new JsonSerializerSettings();
                    serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    serializerSettings.Formatting = Formatting.Indented;
                    var newPackageMapSpecJson = JsonConvert.SerializeObject(packageMapSpec, serializerSettings);

                    try
                    {
                        File.Delete(packageMapSpecPath);
                        File.WriteAllText(packageMapSpecPath, newPackageMapSpecJson);
                        BufferedConsole.WriteLine(string.Format("\tModified {0}", packageMapSpecPath));
                    }
                    catch (Exception ex)
                    {
                        BufferedConsole.Flush();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.Write("ERROR: ");
                        Console.ResetColor();
                        Console.Error.WriteLine($"Couldn't write \"{packageMapSpecPath}\"");
                        Console.Error.WriteLine(ex);
                    }
                }

                // Modify the necessary .blang files
                foreach (var blangFileEntry in blangFileEntries)
                {
                    if (blangFileEntry.Value == null
                        || !blangFileEntry.Value.WasModified
                        || blangFileEntry.Value.BlangFile == null
                        || blangFileEntry.Value.Chunk == null)
                    {
                        continue;
                    }

                    byte[] cryptDataBuffer = blangFileEntry.Value.BlangFile.WriteToStream().ToArray();
                    var encryptedDataMemoryStream = BlangCrypt.IdCrypt(cryptDataBuffer, blangFileEntry.Key, false);

                    if (encryptedDataMemoryStream == null)
                    {
                        BufferedConsole.Flush();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.Write("ERROR: ");
                        Console.ResetColor();
                        Console.Error.WriteLine($"Failed to encrypt \"{blangFileEntry.Key}\"");
                        continue;
                    }

                    var blangModFile = new ResourceModFile(null, blangFileEntry.Key);
                    blangModFile.FileData = encryptedDataMemoryStream;

                    SetModFileDataForContainerChunk(stream, binaryReader, resourceContainer, blangFileEntry.Value.Chunk, blangModFile, blangModFile.FileData.Length, blangModFile.FileData.Length, 0);
                    BufferedConsole.WriteLine(string.Format("\tModified {0}", blangFileEntry.Key));
                }

                // Modify the map resources file if needed
                if (mapResourcesFile != null && mapResourcesChunk != null && originalDecompressedMapResourcesData != null)
                {
                    // Serialize the map resources data
                    var decompressedMapResourcesData = mapResourcesFile.ToByteArray();

                    // Only modify the .mapresources file if it has changed
                    if (!Utils.ArraysEqual(decompressedMapResourcesData, originalDecompressedMapResourcesData))
                    {
                        // Compress the data
                        byte[] compressedMapResourcesData = Oodle.Compress(decompressedMapResourcesData, Oodle.OodleFormat.Kraken, Oodle.OodleCompressionLevel.Normal);

                        if (compressedMapResourcesData == null)
                        {
                            BufferedConsole.Flush();
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.Write("ERROR: ");
                            Console.ResetColor();
                            Console.Error.WriteLine($"Failed to compress \"{mapResourcesChunk.ResourceName.NormalizedFileName}\"");
                        }
                        else
                        {
                            var mapResourcesModFile = new ResourceModFile(null, mapResourcesChunk.ResourceName.NormalizedFileName);
                            mapResourcesModFile.FileData = new MemoryStream(compressedMapResourcesData, 0, compressedMapResourcesData.Length, false);

                            SetModFileDataForContainerChunk(stream, binaryReader, resourceContainer, mapResourcesChunk, mapResourcesModFile, compressedMapResourcesData.Length, decompressedMapResourcesData.Length, null);
                            BufferedConsole.WriteLine(string.Format("\tModified {0}", mapResourcesChunk.ResourceName.NormalizedFileName));
                        }
                    }
                }
            }

            BufferedConsole.Flush();

            if (fileCount > 0)
            {
                Console.Write("Number of files replaced: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Format("{0} file(s) ", fileCount));
                Console.ResetColor();
                Console.Write("in ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(resourceContainer.Path);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Adds new file chunks to the resource file
        /// </summary>
        /// <param name="stream">file/memory stream for the resource file</param>
        /// <param name="resourceContainer">resource container object</param>
        public static void AddChunks(Stream stream, ResourceContainer resourceContainer)
        {
            var newModFiles = resourceContainer.NewModFileList.OrderByDescending(mod => mod.Parent.LoadPriority);

            if (!newModFiles.Any())
            {
                return;
            }

            // Copy individual sections
            byte[] header = new byte[resourceContainer.InfoOffset];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(header, 0, header.Length);

            var infoMemoryStream = new MemoryStream((int)(resourceContainer.NamesOffset - resourceContainer.InfoOffset));
            infoMemoryStream.SetLength(infoMemoryStream.Capacity);
            stream.Read(infoMemoryStream.GetBuffer(), 0, infoMemoryStream.Capacity);

            var nameOffsetsMemoryStream = new MemoryStream((int)(resourceContainer.NamesOffsetEnd - resourceContainer.NamesOffset));
            nameOffsetsMemoryStream.SetLength(nameOffsetsMemoryStream.Capacity);
            stream.Read(nameOffsetsMemoryStream.GetBuffer(), 0, nameOffsetsMemoryStream.Capacity);

            var namesMemoryStream = new MemoryStream((int)(resourceContainer.UnknownOffset - resourceContainer.NamesOffsetEnd));
            namesMemoryStream.SetLength(namesMemoryStream.Capacity);
            stream.Read(namesMemoryStream.GetBuffer(), 0, namesMemoryStream.Capacity);

            byte[] unknown = new byte[resourceContainer.Dummy7Offset - resourceContainer.UnknownOffset];
            stream.Read(unknown, 0, unknown.Length);

            long nameIdsOffset = resourceContainer.Dummy7Offset + (resourceContainer.TypeCount * 4);

            byte[] typeIds = new byte[nameIdsOffset - resourceContainer.Dummy7Offset];
            stream.Read(typeIds, 0, typeIds.Length);

            var nameIdsMemoryStream = new MemoryStream((int)(resourceContainer.IdclOffset - nameIdsOffset));
            nameIdsMemoryStream.SetLength(nameIdsMemoryStream.Capacity);
            stream.Read(nameIdsMemoryStream.GetBuffer(), 0, nameIdsMemoryStream.Capacity);

            byte[] idcl = new byte[resourceContainer.DataOffset - resourceContainer.IdclOffset];
            stream.Read(idcl, 0, idcl.Length);

            // Read the data section
            byte[] data = data = new byte[stream.Length - resourceContainer.DataOffset];
            stream.Read(data, 0, data.Length);

            // Load the data section into a memory stream
            var dataMemoryStream = new MemoryStream(data.Length);
            dataMemoryStream.Write(data, 0, data.Length);

            int infoOldLength = infoMemoryStream.GetBuffer().Length;
            int nameIdsOldLength = nameIdsMemoryStream.GetBuffer().Length;
            int newChunksCount = 0;

            // Find the resource data for the new mod files and set them
            foreach (var mod in resourceContainer.ModFileList.OrderByDescending(mod => mod.Parent.LoadPriority))
            {
                if (mod.IsAssetsInfoJson && mod.AssetsInfo != null && mod.AssetsInfo.Assets != null)
                {
                    foreach (var newMod in newModFiles)
                    {
                        foreach (var assetsInfoAssets in mod.AssetsInfo.Assets)
                        {
                            string normalPath = assetsInfoAssets.Name;
                            string declPath = normalPath;

                            if (assetsInfoAssets.MapResourceType != null)
                            {
                                declPath = $"generated/decls/{assetsInfoAssets.MapResourceType.ToLowerInvariant()}/{assetsInfoAssets.Name}.decl";
                            }

                            if (newMod.Name == declPath || newMod.Name == normalPath)
                            {
                                newMod.ResourceType = assetsInfoAssets.ResourceType == null ? "rs_streamfile" : assetsInfoAssets.ResourceType;
                                newMod.Version = assetsInfoAssets.Version;
                                newMod.StreamDbHash = assetsInfoAssets.StreamDbHash;
                                newMod.SpecialByte1 = assetsInfoAssets.SpecialByte1;
                                newMod.SpecialByte2 = assetsInfoAssets.SpecialByte2;
                                newMod.SpecialByte3 = assetsInfoAssets.SpecialByte3;
                                newMod.PlaceBefore = assetsInfoAssets.PlaceBefore;
                                newMod.PlaceByName = assetsInfoAssets.PlaceByName;
                                newMod.PlaceByType = assetsInfoAssets.PlaceByType;

                                if (Verbose)
                                {
                                    BufferedConsole.WriteLine(string.Format("\tSet resource type \"{0}\" (version: {1}, streamdb hash: {2}) for new file: {3}",
                                        newMod.ResourceType,
                                        newMod.Version,
                                        newMod.StreamDbHash,
                                        newMod.Name));
                                }

                                break;
                            }
                        }
                    }

                    continue;
                }
            }

            // Add the new mod files now
            foreach (var mod in newModFiles)
            {
                // Skip custom files
                if (mod.IsAssetsInfoJson || mod.IsBlangJson)
                {
                    continue;
                }

                if (GetChunk(mod.Name, resourceContainer) != null)
                {
                    if (Verbose)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("WARNING: ");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Trying to add resource \"{mod.Name}\" that has already been added to \"{resourceContainer.Name}\", skipping");
                        Console.ResetColor();
                    }

                    mod.DisposeFileData();
                    continue;
                }

                // Retrieve the resource data for this file (if needed & available)
                ResourceDataEntry resourceData;

                if (ResourceDataDictionary.TryGetValue(ResourceData.CalculateResourceFileNameHash(mod.Name), out resourceData))
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

                // Check if the resource type name exists in the current container, add it if it doesn't
                byte[] namesBuffer;

                if (mod.ResourceType != null)
                {
                    if (resourceContainer.NamesList.FirstOrDefault(name => name.NormalizedFileName == mod.ResourceType) == default(ResourceName))
                    {
                        // Add type name
                        namesBuffer = namesMemoryStream.GetBuffer();
                        long typeLastOffset = FastBitConverter.ToInt64(nameOffsetsMemoryStream.GetBuffer(), (int)nameOffsetsMemoryStream.Length - 8);
                        long typeLastNameOffset = 0;

                        for (int i = (int)typeLastOffset; i < namesBuffer.Length; i++)
                        {
                            if (namesBuffer[i] == '\x00')
                            {
                                typeLastNameOffset = i + 1;
                                break;
                            }
                        }

                        byte[] typeNameBytes = Encoding.UTF8.GetBytes(mod.ResourceType);
                        namesMemoryStream.SetLength(namesMemoryStream.Length + typeNameBytes.Length + 1);
                        namesMemoryStream.Position = typeLastNameOffset;
                        namesMemoryStream.Write(typeNameBytes, 0, typeNameBytes.Length);

                        // Add type name offset
                        nameOffsetsMemoryStream.Position = 0;
                        byte[] typeNewCount = FastBitConverter.GetBytes(FastBitConverter.ToInt64(nameOffsetsMemoryStream.GetBuffer(), 0) + 1);
                        nameOffsetsMemoryStream.Write(typeNewCount, 0, 8);
                        nameOffsetsMemoryStream.Seek(0, SeekOrigin.End);
                        nameOffsetsMemoryStream.Write(FastBitConverter.GetBytes(typeLastNameOffset), 0, 8);

                        // Add the type name to the list to keep the indexes in the proper order
                        resourceContainer.NamesList.Add(new ResourceName()
                        {
                            FullFileName = mod.ResourceType,
                            NormalizedFileName = mod.ResourceType
                        });

                        BufferedConsole.WriteLine(string.Format("\tAdded resource type name \"{0}\" to \"{1}\"", mod.ResourceType, resourceContainer.Name));
                    }
                }

                // Add file name
                namesBuffer = namesMemoryStream.GetBuffer();
                long lastOffset = FastBitConverter.ToInt64(nameOffsetsMemoryStream.GetBuffer(), (int)nameOffsetsMemoryStream.Length - 8);
                long lastNameOffset = 0;

                for (int i = (int)lastOffset; i < namesBuffer.Length; i++)
                {
                    if (namesBuffer[i] == '\x00')
                    {
                        lastNameOffset = i + 1;
                        break;
                    }
                }

                byte[] nameBytes = Encoding.UTF8.GetBytes(mod.Name);
                namesMemoryStream.SetLength(namesMemoryStream.Length + nameBytes.Length + 1);
                namesMemoryStream.Position = lastNameOffset;
                namesMemoryStream.Write(nameBytes, 0, nameBytes.Length);

                // Add name offset
                nameOffsetsMemoryStream.Position = 0;
                byte[] newCount = FastBitConverter.GetBytes(FastBitConverter.ToInt64(nameOffsetsMemoryStream.GetBuffer(), 0) + 1);
                nameOffsetsMemoryStream.Write(newCount, 0, 8);
                nameOffsetsMemoryStream.Seek(0, SeekOrigin.End);
                nameOffsetsMemoryStream.Write(FastBitConverter.GetBytes(lastNameOffset), 0, 8);

                // Add the name to the list to keep the indexes in the proper order
                resourceContainer.NamesList.Add(new ResourceName()
                {
                    FullFileName = mod.Name,
                    NormalizedFileName = mod.Name
                });

                // If this is a texture, check if it's compressed, or compress it if necessary
                long compressedSize = mod.FileData.Length;
                long uncompressedSize = mod.FileData.Length;
                byte compressionMode = 0;

                if (mod.Name.Contains(".tga"))
                {
                    // Get the texture data buffer
                    var textureDataBuffer = mod.FileData.GetBuffer();
                    bool isCompressed = Utils.IsDivinityCompressedTexture(textureDataBuffer, DivinityMagic);

                    if (isCompressed)
                    {
                        // This is a compressed texture, read the uncompressed size
                        uncompressedSize = FastBitConverter.ToInt64(textureDataBuffer, 8);

                        // Get the compressed texture data by removing the header from the memory stream buffer
                        Buffer.BlockCopy(textureDataBuffer, 16, textureDataBuffer, 0, textureDataBuffer.Length - 16);
                        mod.FileData.SetLength(mod.FileData.Length - 16);
                        compressionMode = 2;

                        if (Verbose)
                        {
                            BufferedConsole.WriteLine($"\tSuccessfully set compressed texture data for file \"{mod.Name}\"");
                        }
                    }
                    else if (CompressTextures)
                    {
                        // Compress the texture
                        var compressedData = Oodle.Compress(mod.FileData.GetBuffer(), Oodle.OodleFormat.Kraken, Oodle.OodleCompressionLevel.Normal);
                        mod.FileData.Dispose();
                        mod.FileData = new MemoryStream(compressedData, 0, compressedData.Length, false);
                        compressedSize = compressedData.Length;
                        compressionMode = 2;

                        if (Verbose)
                        {
                            BufferedConsole.WriteLine($"\tSuccessfully compressed texture file \"{mod.Name}\"");
                        }
                    }
                }

                // Add the mod file data at the end of the data memory stream
                long placement = (0x10 - (dataMemoryStream.Length % 0x10)) + 0x30;
                long fileOffset = stream.Length + (dataMemoryStream.Length - data.Length) + placement;

                dataMemoryStream.Write(new byte[placement], 0, (int)placement);
                mod.CopyFileDataToStream(dataMemoryStream);

                // Add the asset type nameId and the filename nameId in nameIds
                long nameId = resourceContainer.GetResourceNameId(mod.Name);
                long nameIdOffset = ((nameIdsMemoryStream.Length + 8) / 8) - 1;

                // Find the asset type name id, if it's not found, use zero
                long assetTypeNameId = resourceContainer.GetResourceNameId(mod.ResourceType);

                if (assetTypeNameId == -1)
                {
                    assetTypeNameId = 0;
                }

                // Add the asset type nameId
                nameIdsMemoryStream.Seek(0, SeekOrigin.End);
                nameIdsMemoryStream.Write(FastBitConverter.GetBytes(assetTypeNameId), 0, 8);

                // Add the asset filename nameId
                nameIdsMemoryStream.Write(FastBitConverter.GetBytes(nameId), 0, 8);

                // Create the file info section
                byte[] newFileInfo = new byte[0x90];
                infoMemoryStream.Seek(-0x90, SeekOrigin.End);
                infoMemoryStream.Read(newFileInfo, 0, 0x90);

                Buffer.BlockCopy(FastBitConverter.GetBytes(nameIdOffset), 0, newFileInfo, newFileInfo.Length - 0x70, 8);
                Buffer.BlockCopy(FastBitConverter.GetBytes(fileOffset), 0, newFileInfo, newFileInfo.Length - 0x58, 8);
                Buffer.BlockCopy(FastBitConverter.GetBytes(compressedSize), 0, newFileInfo, newFileInfo.Length - 0x50, 8);
                Buffer.BlockCopy(FastBitConverter.GetBytes(uncompressedSize), 0, newFileInfo, newFileInfo.Length - 0x48, 8);

                // Set the DataMurmurHash
                Buffer.BlockCopy(FastBitConverter.GetBytes(mod.StreamDbHash.Value), 0, newFileInfo, newFileInfo.Length - 0x40, 8);

                // Set the StreamDB resource hash
                Buffer.BlockCopy(FastBitConverter.GetBytes(mod.StreamDbHash.Value), 0, newFileInfo, newFileInfo.Length - 0x30, 8);

                // Set the correct asset version
                Buffer.BlockCopy(FastBitConverter.GetBytes((int)mod.Version.Value), 0, newFileInfo, newFileInfo.Length - 0x28, 4);

                // Set the special byte values
                Buffer.BlockCopy(FastBitConverter.GetBytes((int)mod.SpecialByte1.Value), 0, newFileInfo, newFileInfo.Length - 0x24, 4);
                Buffer.BlockCopy(FastBitConverter.GetBytes((int)mod.SpecialByte2.Value), 0, newFileInfo, newFileInfo.Length - 0x1E, 4);
                Buffer.BlockCopy(FastBitConverter.GetBytes((int)mod.SpecialByte3.Value), 0, newFileInfo, newFileInfo.Length - 0x1D, 4);

                // Clear the compression mode
                newFileInfo[newFileInfo.Length - 0x20] = compressionMode;

                // Set meta entries to use to 0
                Buffer.BlockCopy(FastBitConverter.GetBytes((short)0), 0, newFileInfo, newFileInfo.Length - 0x10, 2);

                // Add the new file info section at the end
                infoMemoryStream.Write(newFileInfo, 0, 0x90);

                BufferedConsole.WriteLine(string.Format("\tAdded {0}", mod.Name));
                mod.DisposeFileData();
                newChunksCount++;
            }

            // Rebuild the entire container now
            long namesOffsetAdd = infoMemoryStream.Length - infoOldLength;
            long newSize = nameOffsetsMemoryStream.Length + namesMemoryStream.Length;
            long unknownAdd = namesOffsetAdd + (newSize - resourceContainer.StringsSize);
            long typeIdsAdd = unknownAdd;
            long nameIdsAdd = typeIdsAdd;
            long idclAdd = nameIdsAdd + (nameIdsMemoryStream.Length - nameIdsOldLength);
            long dataAdd = idclAdd;

            Buffer.BlockCopy(FastBitConverter.GetBytes(resourceContainer.FileCount + newChunksCount), 0, header, 0x20, 4);
            Buffer.BlockCopy(FastBitConverter.GetBytes(resourceContainer.FileCount2 + (newChunksCount * 2)), 0, header, 0x2C, 4);
            Buffer.BlockCopy(FastBitConverter.GetBytes((int)newSize), 0, header, 0x38, 4);
            Buffer.BlockCopy(FastBitConverter.GetBytes(resourceContainer.NamesOffset + namesOffsetAdd), 0, header, 0x40, 8);
            Buffer.BlockCopy(FastBitConverter.GetBytes(resourceContainer.UnknownOffset + unknownAdd), 0, header, 0x48, 8);
            Buffer.BlockCopy(FastBitConverter.GetBytes(resourceContainer.UnknownOffset2 + unknownAdd), 0, header, 0x58, 8);
            Buffer.BlockCopy(FastBitConverter.GetBytes(resourceContainer.Dummy7Offset + typeIdsAdd), 0, header, 0x60, 8);
            Buffer.BlockCopy(FastBitConverter.GetBytes(resourceContainer.DataOffset + dataAdd), 0, header, 0x68, 8);
            Buffer.BlockCopy(FastBitConverter.GetBytes(resourceContainer.IdclOffset + idclAdd), 0, header, 0x74, 8);

            byte[] newOffsetBuffer = new byte[8];

            for (int i = 0, j = (int)infoMemoryStream.Length / 0x90; i < j; i++)
            {
                int fileOffset = 0x38 + (i * 0x90);
                infoMemoryStream.Position = fileOffset;
                infoMemoryStream.Read(newOffsetBuffer, 0, 8);
                infoMemoryStream.Position -= 8;
                infoMemoryStream.Write(FastBitConverter.GetBytes(FastBitConverter.ToInt64(newOffsetBuffer, 0) + dataAdd), 0, 8);
            }

            // Rebuild the container now
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(header, 0, header.Length);

            infoMemoryStream.Position = 0;
            infoMemoryStream.CopyTo(stream);
            infoMemoryStream.Close();
            infoMemoryStream.Dispose();

            nameOffsetsMemoryStream.Position = 0;
            nameOffsetsMemoryStream.CopyTo(stream);
            nameOffsetsMemoryStream.Close();
            nameOffsetsMemoryStream.Dispose();

            namesMemoryStream.Position = 0;
            namesMemoryStream.CopyTo(stream);
            namesMemoryStream.Close();
            namesMemoryStream.Dispose();

            stream.Write(unknown, 0, unknown.Length);
            stream.Write(typeIds, 0, typeIds.Length);

            nameIdsMemoryStream.Position = 0;
            nameIdsMemoryStream.CopyTo(stream);
            nameIdsMemoryStream.Close();
            nameIdsMemoryStream.Dispose();

            stream.Write(idcl, 0, idcl.Length);

            // Copy the data memory stream into the file stream
            dataMemoryStream.Position = 0;
            dataMemoryStream.CopyTo(stream);
            dataMemoryStream.Close();
            dataMemoryStream.Dispose();

            BufferedConsole.Flush();

            if (newChunksCount != 0)
            {
                Console.Write("Number of files added: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Format("{0} file(s) ", newChunksCount));
                Console.ResetColor();
                Console.Write("in ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(resourceContainer.Path);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Loads the sound mods present in the given sound container
        /// </summary>
        /// <param name="soundContainer">sound container to load the sound mods to</param>
        public static void LoadSoundMods(SoundContainer soundContainer)
        {
            using (var fileStream = new FileStream(soundContainer.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            {
                // Read the sound entries in the container
                ReadSoundEntries(fileStream, soundContainer);

                // Load the sound mods
                ReplaceSounds(fileStream, soundContainer);
            }
        }

        /// <summary>
        /// Reads the sound entries in the given sound container
        /// </summary>
        /// <param name="fileStream">sound container file stream</param>
        /// <param name="soundContainer">sound container</param>
        public static void ReadSoundEntries(FileStream fileStream, SoundContainer soundContainer)
        {
            using (var binaryReader = new BinaryReader(fileStream, Encoding.Default, true))
            {
                // Read the info and the header sizes
                binaryReader.ReadUInt32();

                uint infoSize = binaryReader.ReadUInt32();
                uint headerSize = binaryReader.ReadUInt32();

                fileStream.Seek(headerSize, SeekOrigin.Current);

                // Loop through all the sound info entries and add them to our list
                for (uint i = 0, j = (infoSize - headerSize) / 32; i < j; i++)
                {
                    fileStream.Read(FileBuffer, 0, 8);
                    uint soundId = binaryReader.ReadUInt32();
                    soundContainer.SoundEntries.Add(new SoundEntry(soundId, fileStream.Position));

                    // Skip to the next entry
                    fileStream.Read(FileBuffer, 0, 20);
                }
            }
        }

        /// <summary>
        /// Replaces the sound mods present in the specified sound container object
        /// </summary>
        /// <param name="stream">file/memory stream for the sound container file</param>
        /// <param name="soundContainer">sound container info object</param>
        public static void ReplaceSounds(Stream stream, SoundContainer soundContainer)
        {
            int fileCount = 0;

            using (var binaryReader = new BinaryReader(stream, Encoding.Default, true))
            {
                // Load the sound mods
                foreach (var soundMod in soundContainer.ModFiles.OrderByDescending(mod => mod.Parent.LoadPriority))
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
                    int encodedSize = (int)soundMod.FileData.Length;
                    int decodedSize = encodedSize;
                    bool needsEncoding = false;
                    bool needsDecoding = true;
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
                        case ".wav":
                            format = 2;
                            decodedSize = encodedSize + 20;
                            needsDecoding = false;
                            needsEncoding = true;
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
                                soundMod.FileData = new MemoryStream(opusFileData, 0, opusFileData.Length, false);
                                encodedSize = (int)soundMod.FileData.Length;
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
                    else if (format == 2 && needsDecoding)
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

                    // Load the sound mod into the sound container now
                    // Write the sound replacement data at the end of the sound container
                    stream.Seek(0, SeekOrigin.End);
                    uint soundModOffset = (uint)stream.Position;
                    soundMod.CopyFileDataToStream(stream);

                    // Replace the sound info for this sound id
                    var soundEntriesToModify = soundContainer.SoundEntries.Where(entry => entry.SoundId == soundModId);

                    if (soundEntriesToModify == null || !soundEntriesToModify.Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write("WARNING: ");
                        Console.ResetColor();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Couldn't find sound with id \"{soundModId}\" in \"{soundContainer.Name}\", sound will not be replaced");
                        Console.ResetColor();
                        continue;
                    }

                    foreach (var soundEntry in soundEntriesToModify)
                    {
                        // Seek to the info offset (starting at the encoded size value offset) of this sound entry
                        stream.Seek(soundEntry.InfoOffset, SeekOrigin.Begin);

                        // Replace the sound data offset and sizes
                        stream.Write(FastBitConverter.GetBytes(encodedSize), 0, 4);
                        stream.Write(FastBitConverter.GetBytes(soundModOffset), 0, 4);
                        stream.Write(FastBitConverter.GetBytes(decodedSize), 0, 4);
                        ushort currentFormat = binaryReader.ReadUInt16();

                        if (currentFormat != format)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("WARNING: ");
                            Console.ResetColor();
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Format mismatch: sound file \"{soundMod.Name}\" needs to be format {currentFormat} ({(currentFormat == 3 ? ".wem" : string.Join(", ", SoundEncoding.SupportedOggConversionFileFormats))})");
                            Console.WriteLine($"The sound will be replaced but it might not work in-game.");
                            Console.ResetColor();
                            break;
                        }
                    }

                    BufferedConsole.WriteLine(string.Format("\tReplaced sound with id {0} [{1}]", soundModId, soundMod.Name));
                    fileCount++;
                }
            }

            BufferedConsole.Flush();

            if (fileCount > 0)
            {
                Console.Write("Number of sounds replaced: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Format("{0} sound(s) ", fileCount));
                Console.ResetColor();
                Console.Write("in ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(soundContainer.Path);
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
        /// Gets the path to the .snd file for the specified sound container name
        /// </summary>
        /// <param name="name">resource name</param>
        /// <returns>the path to the .snd file for the specified sound container name, empty string if it wasn't found</returns>
        public static string PathToSoundContainer(string name)
        {
            string searchPath = Path.Combine(BasePath, "sound", "soundbanks", "pc");
            string searchPattern = $"{name}.snd";

            try
            {
                DirectoryInfo baseFolder = new DirectoryInfo(searchPath);

                foreach (var soundContainerFile in baseFolder.EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly))
                {
                    return soundContainerFile.FullName;
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
                Console.WriteLine("USAGE: EternalModLoader <game path | --version> [OPTIONS]");
                Console.WriteLine("\t--version - Prints the version number of the mod loader and exits with exit code same as the version number.");
                Console.WriteLine("OPTIONS:");
                Console.WriteLine("\t--list-res - List the .resources files that will be modified and exit.");
                Console.WriteLine("\t--verbose - Print more information during the mod loading process.");
                Console.WriteLine("\t--slow - Slow mod loading mode that produces slightly lighter files.");
                Console.WriteLine("\t--compress-textures - Compress texture files during the mod loading process.");
                return 1;
            }

            if (args[0] == "--version")
            {
                Console.WriteLine(Version);
                return Version;
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
                    if (args[i] == "--list-res")
                    {
                        listResources = true;
                    }
                    else if (args[i] == "--verbose")
                    {
                        Verbose = true;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("INFO: Verbose logging is enabled.");
                        Console.ResetColor();
                    }
                    else if (args[i] == "--slow")
                    {
                        SlowMode = true;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("INFO: Slow mod loading mode is enabled.");
                        Console.ResetColor();
                    }
                    else if (args[i] == "--compress-textures")
                    {
                        CompressTextures = true;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("INFO: Texture compression is enabled.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.Write("ERROR: ");
                        Console.ResetColor();
                        Console.Error.WriteLine(string.Format("Unknown option '{0}'", args[i]));
                        return 1;
                    }
                }
            }

            // Set the optimal buffer size for I/O file operations
            if (BufferSize == -1)
            {
                DirectoryInfo baseDirectoryInfo = null;

                try
                {
                    baseDirectoryInfo = new DirectoryInfo(BasePath);
                    SetOptimalBufferSize(Path.GetPathRoot(baseDirectoryInfo.FullName));
                }
                catch (Exception ex)
                {
                    BufferSize = 4096;

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.Write("ERROR: ");
                    Console.ResetColor();
                    Console.Error.WriteLine($"Error while determining the optimal buffer size, using 4096 as the default: {ex}");
                }
            }

            // Load the compressed resource data file (if we are not going to load mods, this isn't necessary)
            if (!listResources)
            {
                var resourceDataFilePath = Path.Combine(BasePath, ResourceDataFileName);

                if (File.Exists(resourceDataFilePath))
                {
                    try
                    {
                        ResourceDataDictionary = ResourceData.Parse(resourceDataFilePath);
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.Write("ERROR: ");
                        Console.ResetColor();
                        Console.Error.WriteLine($"There was an error while loading \"{ResourceDataFileName}\"");
                        Console.Error.WriteLine(e);
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
            }

            // Find zipped mods
            foreach (string zippedMod in Directory.EnumerateFiles(Path.Combine(args[0], "Mods"), "*.zip", SearchOption.TopDirectoryOnly))
            {
                int zippedModCount = 0;
                List<ZipArchiveEntry> modEntryList = new List<ZipArchiveEntry>();

                using (var zipArchive = ZipFile.OpenRead(zippedMod))
                {
                    foreach (var zipEntry in zipArchive.Entries)
                    {
                        // Skip directories
                        if (zipEntry.CompressedLength == 0)
                        {
                            continue;
                        }

                        modEntryList.Add(zipEntry);
                    }

                    // Mod object for this mod
                    Mod mod = new Mod()
                    {
                        Name = Path.GetFileName(zippedMod),
                    };

                    // Read the mod info from the EternalMod JSON if it exists
                    var eternalModJsonFileEntry = modEntryList.FirstOrDefault(entry => entry.FullName == "EternalMod.json");

                    if (eternalModJsonFileEntry != null)
                    {
                        try
                        {
                            var stream = eternalModJsonFileEntry.Open();
                            byte[] eternalModJsonFileBytes = new byte[eternalModJsonFileEntry.Length];
                            stream.Read(eternalModJsonFileBytes, 0, eternalModJsonFileBytes.Length);

                            // Try to parse the JSON
                            var serializerSettings = new JsonSerializerSettings();
                            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                            mod = JsonConvert.DeserializeObject<Mod>(Encoding.UTF8.GetString(eternalModJsonFileBytes), serializerSettings);

                            // If the mod requires a higher mod loader version, print a warning and don't load the mod
                            if (mod.RequiredVersion > Version)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.Write("WARNING: ");
                                Console.ResetColor();
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Mod \"{zippedMod}\" requires mod loader version {mod.RequiredVersion} but the current mod loader version is {Version}, skipping.");
                                Console.ResetColor();
                                continue;
                            }
                        }
                        catch
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Error.Write("ERROR: ");
                            Console.ResetColor();
                            Console.Error.WriteLine($"Failed to parse {eternalModJsonFileEntry.FullName} - malformed JSON? - using defaults.");
                        }
                    }

                    foreach (var modFileEntry in modEntryList)
                    {
                        bool isSoundMod = false;
                        string modFileName = modFileEntry.FullName;
                        var modFilePathParts = modFileName.Split('/');

                        if (modFilePathParts.Length < 2)
                        {
                            continue;
                        }

                        string resourceName = modFilePathParts[0];

                        // Old mods compatibility
                        if (resourceName == "generated")
                        {
                            resourceName = "gameresources";
                        }
                        else
                        {
                            // Remove the resource name from the path
                            modFileName = modFileEntry.FullName.Remove(0, resourceName.Length + 1);
                        }

                        // Check if this is a sound mod or not
                        var resourcePath = PathToResource(resourceName + ".resources");

                        if (resourcePath == string.Empty)
                        {
                            resourcePath = PathToSoundContainer(resourceName);

                            if (resourcePath != string.Empty)
                            {
                                isSoundMod = true;
                            }
                        }

                        if (isSoundMod)
                        {
                            // Get the sound container info object, create it if it doesn't exist
                            var soundContainer = SoundContainerList.FirstOrDefault(sndBank => sndBank.Name == resourceName);

                            if (soundContainer == null)
                            {
                                soundContainer = new SoundContainer(resourceName, resourcePath);
                                SoundContainerList.Add(soundContainer);
                            }

                            // Create the mod object and read the unzipped files
                            if (!listResources)
                            {
                                // Skip unsupported formats
                                var soundExtension = Path.GetExtension(modFileName);

                                if (!SoundEncoding.SupportedFileFormats.Contains(soundExtension))
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Write("WARNING: ");
                                    Console.ResetColor();
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Unsupported sound mod file format \"{soundExtension}\" for file \"{modFileName}\"");
                                    Console.ResetColor();
                                    continue;
                                }

                                // Load the sound mod
                                SoundModFile soundModFile = new SoundModFile(mod, Path.GetFileName(modFileName));
                                var stream = modFileEntry.Open();
                                soundModFile.FileData = new MemoryStream((int)modFileEntry.Length);
                                stream.CopyTo(soundModFile.FileData);

                                soundContainer.ModFiles.Add(soundModFile);
                                zippedModCount++;
                            }
                        }
                        else
                        {
                            // Get the resource object
                            var resource = ResourceList.FirstOrDefault(res => res.Name == resourceName);

                            if (resource == null)
                            {
                                resource = new ResourceContainer(resourceName, PathToResource(resourceName + ".resources"));
                                ResourceList.Add(resource);
                            }

                            // Create the mod object and read the unzipped files
                            if (!listResources)
                            {
                                ResourceModFile resourceModFile = new ResourceModFile(mod, modFileName);
                                var stream = modFileEntry.Open();
                                resourceModFile.FileData = new MemoryStream((int)modFileEntry.Length);
                                stream.CopyTo(resourceModFile.FileData);

                                // Read the JSON files in 'assetsinfo' under 'EternalMod'
                                if (modFilePathParts[1] == "EternalMod")
                                {
                                    if (modFilePathParts.Length == 4
                                        && modFilePathParts[2] == "assetsinfo"
                                        && Path.GetExtension(modFilePathParts[3]) == ".json")
                                    {
                                        try
                                        {
                                            var serializerSettings = new JsonSerializerSettings();
                                            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                            resourceModFile.AssetsInfo = JsonConvert.DeserializeObject<AssetsInfo>(Encoding.UTF8.GetString(resourceModFile.FileData.GetBuffer()), serializerSettings);
                                            resourceModFile.IsAssetsInfoJson = true;
                                            resourceModFile.FileData = null;
                                        }
                                        catch
                                        {
                                            Console.ForegroundColor = ConsoleColor.Red;
                                            Console.Error.Write("ERROR: ");
                                            Console.ResetColor();
                                            Console.Error.WriteLine($"Failed to parse EternalMod/assetsinfo/{Path.GetFileNameWithoutExtension(resourceModFile.Name)}.json");
                                            continue;
                                        }
                                    }
                                    else if (modFilePathParts.Length == 4
                                        && modFilePathParts[2] == "strings"
                                        && Path.GetExtension(modFilePathParts[3]) == ".json")
                                    {
                                        // Detect custom language files
                                        resourceModFile.IsBlangJson = true;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                resource.ModFileList.Add(resourceModFile);
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

            Mod globalLooseMod = new Mod();
            globalLooseMod.LoadPriority = int.MinValue;

            foreach (var file in Directory.EnumerateFiles(Path.Combine(args[0], "Mods"), "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".zip"))
                {
                    continue;
                }

                string[] modFilePathParts = file.Remove(0, args[0].Length).Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                if (modFilePathParts.Length <= 2)
                {
                    continue;
                }

                string relativePathToFile = string.Join(Path.DirectorySeparatorChar.ToString(), modFilePathParts);
                bool isSoundMod = false;
                string resourceName = modFilePathParts[1];
                string fileName = string.Empty;

                // Old mods compatibility
                if (resourceName.ToLower() == "generated")
                {
                    resourceName = "gameresources";
                    fileName = relativePathToFile.Remove(0, modFilePathParts[0].Length + 1).Replace('\\', '/');
                }
                else
                {
                    fileName = relativePathToFile.Remove(0, modFilePathParts[0].Length + resourceName.Length + 2).Replace('\\', '/');
                }

                // Check if this is a sound mod or not
                var resourcePath = PathToResource(resourceName + ".resources");

                if (resourcePath == string.Empty)
                {
                    resourcePath = PathToSoundContainer(resourceName);

                    if (resourcePath != string.Empty)
                    {
                        isSoundMod = true;
                    }
                }

                if (isSoundMod)
                {
                    // Get the sound container info object, create it if it doesn't exist
                    var soundContainer = SoundContainerList.FirstOrDefault(sndBank => sndBank.Name == resourceName);

                    if (soundContainer == null)
                    {
                        soundContainer = new SoundContainer(resourceName, resourcePath);
                        SoundContainerList.Add(soundContainer);
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
                        SoundModFile soundModFile = new SoundModFile(globalLooseMod, Path.GetFileName(fileName));

                        using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                        {
                            soundModFile.FileData = new MemoryStream((int)fileStream.Length);
                            fileStream.CopyTo(soundModFile.FileData);
                        }

                        soundContainer.ModFiles.Add(soundModFile);
                        unzippedModCount++;
                    }
                }
                else
                {
                    // Get the resource object
                    var resource = ResourceList.FirstOrDefault(res => res.Name == resourceName);

                    if (resource == null)
                    {
                        resource = new ResourceContainer(resourceName, PathToResource(resourceName + ".resources"));
                        ResourceList.Add(resource);
                    }

                    // Create the mod object and read the files
                    if (!listResources)
                    {
                        ResourceModFile mod = new ResourceModFile(globalLooseMod, fileName);

                        using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                        {
                            mod.FileData = new MemoryStream((int)fileStream.Length);
                            fileStream.CopyTo(mod.FileData);
                        }

                        // Read the JSON files in 'assetsinfo' under 'EternalMod'
                        if (modFilePathParts[2] == "EternalMod")
                        {
                            if (modFilePathParts.Length == 5
                                && modFilePathParts[3] == "assetsinfo"
                                && Path.GetExtension(modFilePathParts[4]) == ".json")
                            {
                                try
                                {
                                    var serializerSettings = new JsonSerializerSettings();
                                    serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                                    mod.AssetsInfo = JsonConvert.DeserializeObject<AssetsInfo>(Encoding.UTF8.GetString(mod.FileData.GetBuffer()), serializerSettings);
                                    mod.IsAssetsInfoJson = true;
                                    mod.FileData = null;
                                }
                                catch
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.Error.Write("ERROR: ");
                                    Console.ResetColor();
                                    Console.Error.WriteLine($"Failed to parse EternalMod/assetsinfo/{Path.GetFileNameWithoutExtension(mod.Name)}.json");
                                    continue;
                                }
                            }
                            else if (modFilePathParts.Length == 5
                                && modFilePathParts[3] == "strings"
                                && Path.GetExtension(modFilePathParts[4]) == ".json")
                            {
                                // Detect custom language files
                                mod.IsBlangJson = true;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        resource.ModFileList.Add(mod);
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
                foreach (var soundContainer in SoundContainerList)
                {
                    if (soundContainer.Path == string.Empty)
                    {
                        continue;
                    }

                    if (Path.DirectorySeparatorChar == '\\')
                    {
                        Console.WriteLine($".{soundContainer.Path.Substring(soundContainer.Path.IndexOf("\\base\\"))}");
                    }
                    else
                    {
                        Console.WriteLine($".{soundContainer.Path.Substring(soundContainer.Path.IndexOf("/base/"))}");
                    }
                }

                return 0;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

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
                    Console.Write(string.Format("{0} file(s)", resource.ModFileList.Count));
                    Console.ResetColor();
                    Console.WriteLine("...");
                    continue;
                }

                LoadMods(resource);
            }

            // Load the sound mods
            foreach (var soundContainer in SoundContainerList)
            {
                if (string.IsNullOrEmpty(soundContainer.Path))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("WARNING: ");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(soundContainer.Name + ".snd");
                    Console.ResetColor();
                    Console.Write(" was not found! Skipping ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(string.Format("{0} file(s)", soundContainer.ModFiles.Count));
                    Console.ResetColor();
                    Console.WriteLine("...");
                    continue;
                }

                LoadSoundMods(soundContainer);
            }

            stopwatch.Stop();

            BufferedConsole.Flush();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Finished in {stopwatch.Elapsed}");
            Console.ResetColor();
            return 0;
        }
    }
}