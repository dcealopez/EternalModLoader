using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using BlangParser;
using EternalModLoader.Mods;
using EternalModLoader.Mods.Resources;
using EternalModLoader.Mods.Resources.ResourceData;
using EternalModLoader.Mods.Resources.Blang;
using EternalModLoader.Mods.Resources.MapResources;
using EternalModLoader.Mods.Sounds;
using EternalModLoader.Zlib;
using System.Threading.Tasks;

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
        public const int Version = 10;

        /// <summary>
        /// Resource data file name
        /// </summary>
        private const string ResourceDataFileName = "rs_data";

        /// <summary>
        /// Package Map Spec JSON file name
        /// </summary>
        private const string PackageMapSpecJsonFileName = "packagemapspec.json";

        /// <summary>
        /// Mods folder name
        /// </summary>
        private const string ModsFolderName = "Mods";

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
        /// Use multi-threading?
        /// </summary>
        public static bool MultiThreading = true;

        /// <summary>
        /// Only load online-safe mods?
        /// </summary>
        public static bool LoadOnlineSafeOnlyMods = false;

        /// <summary>
        /// Global flag that determines if the mods being loaded are safe for online play or not
        /// </summary>
        public static bool AreModsSafeForOnline = true;

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
        /// List of all the game's .resources and .streamdb file paths
        /// </summary>
        public static List<string> ResourceContainerPathList = new List<string>();

        /// <summary>
        /// List of all the game's .snd archive file paths
        /// </summary>
        public static List<string> SoundContainerPathList = new List<string>();

        /// <summary>
        /// For packageMapSpec JSON modifications
        /// </summary>
        public static PackageMapSpecInfo PackageMapSpecInfo = new PackageMapSpecInfo();

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
        /// Global buffered console, used for non-threaded, sequential writes
        /// </summary>
        public static BufferedConsole BufferedConsole;

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
            // Buffered console for this operation
            var bufferedConsole = new BufferedConsole();

            using (var fileStream = new FileStream(resourceContainer.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, BufferSize, FileOptions.SequentialScan))
            {
                // Read the resource file and reset the position afterwards
                ReadResource(fileStream, resourceContainer);
                fileStream.Position = 0;

                // Load the mods
                if (!SlowMode)
                {
                    ReplaceChunks(fileStream, resourceContainer, bufferedConsole);
                    AddChunks(fileStream, resourceContainer, bufferedConsole);
                }
                else
                {
                    using (var memoryStream = new MemoryStream((int)fileStream.Length))
                    {
                        // Copy the stream into memory for faster manipulation of the data
                        fileStream.CopyTo(memoryStream);

                        // Load the mods
                        ReplaceChunks(memoryStream, resourceContainer, bufferedConsole);
                        AddChunks(memoryStream, resourceContainer, bufferedConsole);

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
                long sizeDiff = modFile.FileData.Length - chunk.SizeZ;

                // We will need to expand the file if the new size is greater than the old one
                // If its shorter, we will replace all the bytes and zero out the remaining bytes
                if (sizeDiff > 0)
                {
                    var buffer = new byte[BufferSize];
                    var length = stream.Length;

                    // Expand the memory stream so the new file fits
                    stream.SetLength(length + sizeDiff);
                    int toRead;

                    while (length > (fileOffset + chunk.SizeZ))
                    {
                        toRead = length - BufferSize >= (fileOffset + chunk.SizeZ) ? BufferSize : (int)(length - (fileOffset + chunk.SizeZ));
                        length -= toRead;
                        stream.Seek(length, SeekOrigin.Begin);
                        stream.Read(buffer, 0, toRead);
                        stream.Seek(length + sizeDiff, SeekOrigin.Begin);
                        stream.Write(buffer, 0, toRead);
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
                        stream.Seek(-8, SeekOrigin.Current);
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
        public static void ReplaceChunks(Stream stream, ResourceContainer resourceContainer, BufferedConsole bufferedConsole)
        {
            // For map resources modifications
            ResourceChunk mapResourcesChunk = null;
            MapResourcesFile mapResourcesFile = null;
            byte[] originalDecompressedMapResourcesData = null;
            bool invalidMapResources = false;

            // For .blang file modifications
            Dictionary<string, BlangFileEntry> blangFileEntries = new Dictionary<string, BlangFileEntry>();

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
                            // Prevent multiple threads from accessing the global package map spec object
                            lock (PackageMapSpecInfo)
                            {
                                // Deserialize the packagemapspec JSON if it hasn't been deserialized yet
                                if (PackageMapSpecInfo.PackageMapSpec == null && !PackageMapSpecInfo.InvalidPackageMapSpec)
                                {
                                    PackageMapSpecInfo.PackageMapSpecPath = Path.Combine(BasePath, PackageMapSpecJsonFileName);

                                    if (!File.Exists(PackageMapSpecInfo.PackageMapSpecPath))
                                    {
                                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                        bufferedConsole.Write("ERROR: ");
                                        bufferedConsole.ResetColor();
                                        bufferedConsole.WriteLine($"{PackageMapSpecInfo.PackageMapSpecPath} not found while trying to add extra resources for level {resourceContainer.Name}");
                                        PackageMapSpecInfo.InvalidPackageMapSpec = true;
                                    }
                                    else
                                    {
                                        var packageMapSpecFileBytes = File.ReadAllBytes(PackageMapSpecInfo.PackageMapSpecPath);

                                        try
                                        {
                                            // Try to parse the JSON
                                            PackageMapSpecInfo.PackageMapSpec = PackageMapSpec.FromJson(Encoding.UTF8.GetString(packageMapSpecFileBytes));
                                        }
                                        catch
                                        {
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("ERROR: ");
                                            bufferedConsole.ResetColor();
                                            bufferedConsole.WriteLine($"Failed to parse {PackageMapSpecInfo.PackageMapSpecPath} - malformed JSON?");
                                            PackageMapSpecInfo.InvalidPackageMapSpec = true;
                                        }
                                    }
                                }

                                // Add the extra resources, then rewrite the JSON
                                if (PackageMapSpecInfo.PackageMapSpec != null && !PackageMapSpecInfo.InvalidPackageMapSpec)
                                {
                                    foreach (var extraResource in modFile.AssetsInfo.Resources)
                                    {
                                        // First check that the resource trying to be added actually exists
                                        var extraResourcePath = PathToResource(extraResource.Name);

                                        if (extraResourcePath == null)
                                        {
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("WARNING: ");
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                            bufferedConsole.WriteLine($"Trying to add non-existing extra resource \"{extraResource.Name}\" to \"{resourceContainer.Name}\", skipping");
                                            bufferedConsole.ResetColor();
                                            continue;
                                        }

                                        // Add the extra resources before all the original resources the level loads
                                        // Find the necessary map and file indexes
                                        int fileIndex = -1;
                                        int mapIndex = -1;

                                        for (int i = 0; i < PackageMapSpecInfo.PackageMapSpec.Files.Count; i++)
                                        {
                                            if (PackageMapSpecInfo.PackageMapSpec.Files[i].Name.Contains(extraResource.Name))
                                            {
                                                fileIndex = i;
                                                break;
                                            }
                                        }

                                        // Special cases for the hubs
                                        string modFileMapName = Path.GetFileNameWithoutExtension(modFile.Name);

                                        if (resourceContainer.Name.StartsWith("dlc_hub", StringComparison.Ordinal))
                                        {
                                            modFileMapName = "game/dlc/hub/hub";
                                        }
                                        else if (resourceContainer.Name.StartsWith("hub", StringComparison.Ordinal))
                                        {
                                            modFileMapName = "game/hub/hub";
                                        }

                                        for (int i = 0; i < PackageMapSpecInfo.PackageMapSpec.Maps.Count; i++)
                                        {
                                            if (PackageMapSpecInfo.PackageMapSpec.Maps[i].Name.EndsWith(modFileMapName, StringComparison.Ordinal))
                                            {
                                                mapIndex = i;
                                                break;
                                            }
                                        }

                                        if (fileIndex == -1)
                                        {
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("ERROR: ");
                                            bufferedConsole.ResetColor();
                                            bufferedConsole.WriteLine($"Invalid extra resource {extraResource.Name}, skipping");
                                            continue;
                                        }

                                        if (mapIndex == -1)
                                        {
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("ERROR: ");
                                            bufferedConsole.ResetColor();
                                            bufferedConsole.WriteLine($"Map reference not found for {modFile.Name}, skipping");
                                            continue;
                                        }

                                        // Remove the extra resource, if specified
                                        if (extraResource.Remove)
                                        {
                                            bool mapFileRefRemoved = false;

                                            // Find the map file reference to remove
                                            for (int i = PackageMapSpecInfo.PackageMapSpec.MapFileRefs.Count - 1; i >= 0; i--)
                                            {
                                                if (PackageMapSpecInfo.PackageMapSpec.MapFileRefs[i].File == fileIndex
                                                    && PackageMapSpecInfo.PackageMapSpec.MapFileRefs[i].Map == mapIndex)
                                                {
                                                    PackageMapSpecInfo.PackageMapSpec.MapFileRefs.RemoveAt(i);
                                                    mapFileRefRemoved = true;
                                                    break;
                                                }
                                            }

                                            if (mapFileRefRemoved)
                                            {
                                                bufferedConsole.WriteLine($"\tRemoved resource \"{PackageMapSpecInfo.PackageMapSpec.Files[fileIndex].Name}\" to be loaded in map \"{PackageMapSpecInfo.PackageMapSpec.Maps[mapIndex].Name}\"");
                                            }
                                            else
                                            {
                                                if (Verbose)
                                                {
                                                    bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                                    bufferedConsole.Write("WARNING: ");
                                                    bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                                    bufferedConsole.WriteLine($"Resource \"{extraResource.Name}\" for map \"{PackageMapSpecInfo.PackageMapSpec.Maps[mapIndex].Name}\" set to be removed was not found");
                                                    bufferedConsole.ResetColor();
                                                }
                                            }

                                            continue;
                                        }

                                        // If the resource is already referenced to be loaded in the map, delete it first
                                        // to allow us to move it wherever we want
                                        for (int i = PackageMapSpecInfo.PackageMapSpec.MapFileRefs.Count - 1; i >= 0; i--)
                                        {
                                            if (PackageMapSpecInfo.PackageMapSpec.MapFileRefs[i].File == fileIndex && PackageMapSpecInfo.PackageMapSpec.MapFileRefs[i].Map == mapIndex)
                                            {
                                                PackageMapSpecInfo.PackageMapSpec.MapFileRefs.RemoveAt(i);

                                                if (Verbose)
                                                {
                                                    bufferedConsole.WriteLine($"\tResource \"{PackageMapSpecInfo.PackageMapSpec.Files[fileIndex].Name}\" being added to map \"{PackageMapSpecInfo.PackageMapSpec.Maps[mapIndex].Name}\" already exists. The load order will be modified as specified.");
                                                }

                                                break;
                                            }
                                        }

                                        // Add the extra resource now to the map/file references
                                        // before the resource that normally appears last in the list for the map
                                        int insertIndex = -1;

                                        for (int i = 0; i < PackageMapSpecInfo.PackageMapSpec.MapFileRefs.Count; i++)
                                        {
                                            if (PackageMapSpecInfo.PackageMapSpec.MapFileRefs[i].Map == mapIndex)
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

                                            if (placeBeforeResourcePath == null)
                                            {
                                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                                bufferedConsole.Write("WARNING: ");
                                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                                bufferedConsole.WriteLine($"placeByName resource \"{extraResource.PlaceByName}\" not found for extra resource entry \"{extraResource.Name}\", using normal placement");
                                                bufferedConsole.ResetColor();
                                            }
                                            else
                                            {
                                                // Find placement resource index
                                                int placeBeforeFileIndex = -1;

                                                for (int i = 0; i < PackageMapSpecInfo.PackageMapSpec.Files.Count; i++)
                                                {
                                                    if (PackageMapSpecInfo.PackageMapSpec.Files[i].Name.Contains(extraResource.PlaceByName))
                                                    {
                                                        placeBeforeFileIndex = i;
                                                        break;
                                                    }
                                                }

                                                // Find placement resource map-file reference
                                                for (int i = 0; i < PackageMapSpecInfo.PackageMapSpec.MapFileRefs.Count; i++)
                                                {
                                                    if (PackageMapSpecInfo.PackageMapSpec.MapFileRefs[i].Map == mapIndex && PackageMapSpecInfo.PackageMapSpec.MapFileRefs[i].File == placeBeforeFileIndex)
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

                                        if (insertIndex == -1 || insertIndex >= PackageMapSpecInfo.PackageMapSpec.MapFileRefs.Count)
                                        {
                                            PackageMapSpecInfo.PackageMapSpec.MapFileRefs.Add(mapFileRef);
                                        }
                                        else
                                        {
                                            PackageMapSpecInfo.PackageMapSpec.MapFileRefs.Insert(insertIndex, mapFileRef);
                                        }

                                        bufferedConsole.Write($"\tAdded extra resource \"{PackageMapSpecInfo.PackageMapSpec.Files[fileIndex].Name}\" to be loaded in map \"{PackageMapSpecInfo.PackageMapSpec.Maps[mapIndex].Name}\"");

                                        if (extraResource.PlaceFirst)
                                        {
                                            bufferedConsole.WriteLine(" with the highest priority.");
                                        }
                                        else if (!string.IsNullOrEmpty(extraResource.PlaceByName) && insertIndex != -1)
                                        {
                                            bufferedConsole.WriteLine($" {(extraResource.PlaceBefore ? "before" : "after")} \"{extraResource.PlaceByName}\"");
                                        }
                                        else
                                        {
                                            bufferedConsole.WriteLine(" with the lowest priority");
                                        }

                                        PackageMapSpecInfo.WasPackageMapSpecModified = true;
                                    }
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
                                    if (file.ResourceName.NormalizedFileName.EndsWith(".mapresources", StringComparison.Ordinal))
                                    {
                                        if (resourceContainer.Name.StartsWith("gameresources", StringComparison.Ordinal)
                                            && file.ResourceName.NormalizedFileName.EndsWith("init.mapresources", StringComparison.Ordinal))
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
                                        originalDecompressedMapResourcesData = OodleWrapper.Decompress(mapResourcesBytes, mapResourcesChunk.Size);

                                        if (originalDecompressedMapResourcesData == null)
                                        {
                                            invalidMapResources = true;
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("ERROR: ");
                                            bufferedConsole.ResetColor();
                                            bufferedConsole.WriteLine($"Failed to decompress \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" - are you trying to add assets in the wrong .resources archive?");
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

                            // Add layers
                            if (modFile.AssetsInfo.Layers != null)
                            {
                                foreach (var newLayers in modFile.AssetsInfo.Layers)
                                {
                                    if (mapResourcesFile.Layers.Contains(newLayers.Name))
                                    {
                                        if (Verbose)
                                        {
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("WARNING: ");
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                            bufferedConsole.WriteLine($"Trying to add layer \"{newLayers.Name}\" that has already been added in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\", skipping");
                                            bufferedConsole.ResetColor();
                                        }

                                        continue;
                                    }

                                    mapResourcesFile.Layers.Add(newLayers.Name);
                                    bufferedConsole.WriteLine($"\tAdded layer \"{newLayers.Name}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("WARNING: ");
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                            bufferedConsole.WriteLine($"Trying to add map \"{newMaps.Name}\" that has already been added in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\", skipping");
                                            bufferedConsole.ResetColor();
                                        }

                                        continue;
                                    }

                                    mapResourcesFile.Maps.Add(newMaps.Name);
                                    bufferedConsole.WriteLine($"\tAdded map \"{newMaps.Name}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("WARNING: ");
                                            bufferedConsole.ResetColor();
                                            bufferedConsole.WriteLine($"Skipping empty resource declaration in \"{modFile.Name}\"");
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
                                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                                bufferedConsole.Write("WARNING: ");
                                                bufferedConsole.ResetColor();
                                                bufferedConsole.WriteLine($"Can't remove asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" because it doesn't exist in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\"");
                                            }
                                        }
                                        else
                                        {
                                            mapResourcesFile.Assets.Remove(assetToRemove);
                                            bufferedConsole.WriteLine($"\tRemoved asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" from \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("WARNING: ");
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                            bufferedConsole.WriteLine($"Trying to add asset \"{newAsset.Name}\" that has already been added in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\", skipping");
                                            bufferedConsole.ResetColor();
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

                                        bufferedConsole.WriteLine($"\tAdded asset type \"{newAsset.MapResourceType}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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
                                        bufferedConsole.WriteLine($"\tAsset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" will be added before asset \"{placeByExistingAsset.Name}\" with type \"{mapResourcesFile.AssetTypes[placeByExistingAsset.AssetTypeIndex]}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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

                                    bufferedConsole.WriteLine($"\tAdded asset \"{newAsset.Name}\" with type \"{newAsset.MapResourceType}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
                                }
                            }
                        }

                        continue;
                    }
                    else if (modFile.IsBlangJson)
                    {
                        // Handle custom .blang JSON files
                        var modName = modFile.Name;
                        var modFilePathParts = modName.Split('/');
                        var name = modName.Substring(modFilePathParts[0].Length + 1, modName.Length - (modFilePathParts[0].Length + 1));
                        modFile.Name = name.Substring(0, name.Length - 4) + "blang";
                        chunk = GetChunk(modFile.Name, resourceContainer);

                        if (chunk == null)
                        {
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
                                    bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                    bufferedConsole.Write("WARNING: ");
                                    bufferedConsole.ResetColor();
                                    bufferedConsole.WriteLine($"Mapresources data for asset \"{modFile.Name}\" is null, skipping");
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
                                    if (file.ResourceName.NormalizedFileName.EndsWith(".mapresources", StringComparison.Ordinal))
                                    {
                                        if (resourceContainer.Name.StartsWith("gameresources", StringComparison.Ordinal)
                                            && file.ResourceName.NormalizedFileName.EndsWith("init.mapresources", StringComparison.Ordinal))
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
                                        originalDecompressedMapResourcesData = OodleWrapper.Decompress(mapResourcesBytes, mapResourcesChunk.Size);

                                        if (originalDecompressedMapResourcesData == null)
                                        {
                                            invalidMapResources = true;
                                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            bufferedConsole.Write("ERROR: ");
                                            bufferedConsole.ResetColor();
                                            bufferedConsole.WriteLine($"Failed to decompress \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" - are you trying to add assets in the wrong .resources archive?");
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
                                    bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                    bufferedConsole.Write("WARNING: ");
                                    bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                    bufferedConsole.WriteLine($"Trying to add asset \"{resourceData.MapResourceName}\" that has already been added in \"{mapResourcesChunk.ResourceName.NormalizedFileName}\", skipping");
                                    bufferedConsole.ResetColor();
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

                                bufferedConsole.WriteLine($"\tAdded asset type \"{resourceData.MapResourceType}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
                            }

                            mapResourcesFile.Assets.Add(new MapAsset()
                            {
                                AssetTypeIndex = assetTypeIndex,
                                Name = resourceData.MapResourceName,
                                UnknownData4 = 128
                            });

                            bufferedConsole.WriteLine($"\tAdded asset \"{resourceData.MapResourceName}\" with type \"{resourceData.MapResourceType}\" to \"{mapResourcesChunk.ResourceName.NormalizedFileName}\" in \"{resourceContainer.Name}\"");
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
                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                bufferedConsole.Write("ERROR: ");
                                bufferedConsole.ResetColor();
                                bufferedConsole.WriteLine($"Failed to parse {resourceContainer.Name}/{modFile.Name}");
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
                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                bufferedConsole.Write("ERROR: ");
                                bufferedConsole.ResetColor();
                                bufferedConsole.WriteLine($"Failed to parse {resourceContainer.Name}/{modFile.Name} - are you trying to change strings in the wrong .resources archive?");
                                continue;
                            }
                        }

                        if (blangFileEntry == null || blangFileEntry.BlangFile == null || blangFileEntry.Chunk == null)
                        {
                            continue;
                        }

                        if (!blangFileEntry.Announce && modFile.Announce)
                        {
                            blangFileEntry.Announce = true;
                        }

                        // Read the blang JSON and add the strings to the .blang file
                        BlangJson blangJson;

                        try
                        {
                            blangJson = BlangJson.FromJson(Encoding.UTF8.GetString(modFile.FileData.GetBuffer()));

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
                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                            bufferedConsole.Write("ERROR: ");
                            bufferedConsole.ResetColor();
                            bufferedConsole.WriteLine($"Failed to parse EternalMod/strings/{Path.GetFileNameWithoutExtension(modFile.Name)}.json");
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
                                    blangFileEntry.WasModified = true;

                                    if (modFile.Announce)
                                    {
                                        bufferedConsole.WriteLine($"\tReplaced string \"{blangString.Identifier}\" in \"{modFile.Name}\" in \"{resourceContainer.Name}\"");
                                    }

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

                            if (modFile.Announce)
                            {
                                bufferedConsole.WriteLine($"\tAdded string \"{blangJsonString.Name}\" to \"{modFile.Name}\" in \"{resourceContainer.Name}\"");
                            }

                            blangFileEntry.WasModified = true;
                        }

                        continue;
                    }

                    // Replace the mod file data now
                    long compressedSize = modFile.FileData.Length;
                    long uncompressedSize = compressedSize;
                    byte? compressionMode = 0;

                    // If this is a texture, check if it's compressed, or compress it if necessary
                    if (chunk.ResourceName.NormalizedFileName.EndsWith(".tga", StringComparison.Ordinal))
                    {
                        // Get the texture data buffer, check if it's a DIVINITY compressed texture
                        var textureDataBuffer = modFile.FileData.GetBuffer();

                        if (Utils.IsDivinityCompressedTexture(textureDataBuffer, DivinityMagic))
                        {
                            // This is a compressed texture, read the uncompressed size
                            uncompressedSize = FastBitConverter.ToInt64(textureDataBuffer, 8);

                            // Set the compressed texture data, skipping the DIVINITY header (16 bytes)
                            Buffer.BlockCopy(textureDataBuffer, 16, textureDataBuffer, 0, textureDataBuffer.Length - 16);
                            modFile.FileData.SetLength(textureDataBuffer.Length - 16);
                            compressedSize -= 16;
                            compressionMode = 2;

                            if (Verbose)
                            {
                                bufferedConsole.WriteLine($"\tSuccessfully set compressed texture data for file \"{modFile.Name}\"");
                            }
                        }
                        else if (CompressTextures)
                        {
                            // Compress the texture
                            var compressedData = OodleWrapper.Compress(modFile.FileData.GetBuffer(), OodleWrapper.OodleFormat.Kraken, OodleWrapper.OodleCompressionLevel.Normal);
                            modFile.FileData = new MemoryStream(compressedData, 0, compressedData.Length, false);
                            compressedSize = compressedData.Length;
                            compressionMode = 2;

                            if (Verbose)
                            {
                                bufferedConsole.WriteLine($"\tSuccessfully compressed texture file \"{modFile.Name}\"");
                            }
                        }
                    }

                    SetModFileDataForContainerChunk(stream, binaryReader, resourceContainer, chunk, modFile, compressedSize, uncompressedSize, compressionMode);

                    if (modFile.Announce)
                    {
                        bufferedConsole.WriteLine(string.Format("\tReplaced {0}", modFile.Name));
                        fileCount++;
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
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        bufferedConsole.Write("ERROR: ");
                        bufferedConsole.ResetColor();
                        bufferedConsole.WriteLine($"Failed to encrypt \"{blangFileEntry.Key}\"");
                        continue;
                    }

                    var blangModFile = new ResourceModFile(null, blangFileEntry.Key, resourceContainer.Name);
                    blangModFile.FileData = encryptedDataMemoryStream;

                    SetModFileDataForContainerChunk(stream, binaryReader, resourceContainer, blangFileEntry.Value.Chunk, blangModFile, blangModFile.FileData.Length, blangModFile.FileData.Length, 0);

                    if (blangFileEntry.Value.Announce)
                    {
                        bufferedConsole.WriteLine(string.Format("\tModified {0}", blangFileEntry.Key));
                        fileCount++;
                    }
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
                        byte[] compressedMapResourcesData = OodleWrapper.Compress(decompressedMapResourcesData, OodleWrapper.OodleFormat.Kraken, OodleWrapper.OodleCompressionLevel.Normal);

                        if (compressedMapResourcesData == null)
                        {
                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                            bufferedConsole.Write("ERROR: ");
                            bufferedConsole.ResetColor();
                            bufferedConsole.WriteLine($"Failed to compress \"{mapResourcesChunk.ResourceName.NormalizedFileName}\"");
                        }
                        else
                        {
                            var mapResourcesModFile = new ResourceModFile(null, mapResourcesChunk.ResourceName.NormalizedFileName, resourceContainer.Name);
                            mapResourcesModFile.FileData = new MemoryStream(compressedMapResourcesData, 0, compressedMapResourcesData.Length, false);

                            SetModFileDataForContainerChunk(stream, binaryReader, resourceContainer, mapResourcesChunk, mapResourcesModFile, compressedMapResourcesData.Length, decompressedMapResourcesData.Length, null);
                            bufferedConsole.WriteLine(string.Format("\tModified {0}", mapResourcesChunk.ResourceName.NormalizedFileName));
                            fileCount++;
                        }
                    }
                }
            }

            if (fileCount > 0)
            {
                bufferedConsole.Write("Number of files replaced: ");
                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Green;
                bufferedConsole.Write(string.Format("{0} file(s) ", fileCount));
                bufferedConsole.ResetColor();
                bufferedConsole.Write("in ");
                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                bufferedConsole.WriteLine(resourceContainer.Path);
                bufferedConsole.ResetColor();
            }

            bufferedConsole.Flush();
        }

        /// <summary>
        /// Modifies the packageMapSpec JSON file if needed
        /// </summary>
        public static void ModifyPackageMapSpec()
        {
            var bufferedConsole = new BufferedConsole();

            if (PackageMapSpecInfo.PackageMapSpec != null && PackageMapSpecInfo.WasPackageMapSpecModified)
            {
                try
                {
                    // Write the new JSON
                    PackageMapSpecInfo.PackageMapSpec.WriteToAsJson(PackageMapSpecInfo.PackageMapSpecPath);
                    bufferedConsole.WriteLine(string.Format("\tModified {0}", PackageMapSpecInfo.PackageMapSpecPath));
                }
                catch (Exception ex)
                {
                    bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                    bufferedConsole.Write("ERROR: ");
                    bufferedConsole.ResetColor();
                    bufferedConsole.WriteLine($"Couldn't write \"{PackageMapSpecInfo.PackageMapSpecPath}\"");
                    bufferedConsole.WriteLine(ex.ToString());
                }
                finally
                {
                    bufferedConsole.Flush();
                }
            }
        }

        /// <summary>
        /// Adds new file chunks to the resource file
        /// </summary>
        /// <param name="stream">file/memory stream for the resource file</param>
        /// <param name="resourceContainer">resource container object</param>
        public static void AddChunks(Stream stream, ResourceContainer resourceContainer, BufferedConsole bufferedConsole)
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
            int addedCount = 0;

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
                                    bufferedConsole.WriteLine(string.Format("\tSet resource type \"{0}\" (version: {1}, streamdb hash: {2}) for new file: {3}",
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
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        bufferedConsole.Write("WARNING: ");
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        bufferedConsole.WriteLine($"Trying to add resource \"{mod.Name}\" that has already been added to \"{resourceContainer.Name}\", skipping");
                        bufferedConsole.ResetColor();
                    }

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

                    if (Verbose && !mod.Name.EndsWith(".decl"))
                    {
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        bufferedConsole.Write("WARNING: ");
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        bufferedConsole.WriteLine($"No resource data found for file: {mod.Name}");
                        bufferedConsole.ResetColor();
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

                        bufferedConsole.WriteLine(string.Format("\tAdded resource type name \"{0}\" to \"{1}\"", mod.ResourceType, resourceContainer.Name));
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
                    // Get the texture data buffer, check if it's a DIVINITY compressed texture
                    var textureDataBuffer = mod.FileData.GetBuffer();

                    if (Utils.IsDivinityCompressedTexture(textureDataBuffer, DivinityMagic))
                    {
                        // This is a compressed texture, read the uncompressed size
                        uncompressedSize = FastBitConverter.ToInt64(textureDataBuffer, 8);

                        // Set the compressed texture data, skipping the DIVINITY header (16 bytes)
                        Buffer.BlockCopy(textureDataBuffer, 16, textureDataBuffer, 0, textureDataBuffer.Length - 16);
                        mod.FileData.SetLength(textureDataBuffer.Length - 16);
                        compressedSize -= 16;
                        compressionMode = 2;

                        if (Verbose)
                        {
                            bufferedConsole.WriteLine($"\tSuccessfully set compressed texture data for file \"{mod.Name}\"");
                        }
                    }
                    else if (CompressTextures)
                    {
                        // Compress the texture
                        var compressedData = OodleWrapper.Compress(mod.FileData.GetBuffer(), OodleWrapper.OodleFormat.Kraken, OodleWrapper.OodleCompressionLevel.Normal);
                        mod.FileData = new MemoryStream(compressedData, 0, compressedData.Length, false);
                        compressedSize = compressedData.Length;
                        compressionMode = 2;

                        if (Verbose)
                        {
                            bufferedConsole.WriteLine($"\tSuccessfully compressed texture file \"{mod.Name}\"");
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

                if (mod.Announce)
                {
                    bufferedConsole.WriteLine(string.Format("\tAdded {0}", mod.Name));
                    addedCount++;
                }

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

            nameOffsetsMemoryStream.Position = 0;
            nameOffsetsMemoryStream.CopyTo(stream);

            namesMemoryStream.Position = 0;
            namesMemoryStream.CopyTo(stream);

            stream.Write(unknown, 0, unknown.Length);
            stream.Write(typeIds, 0, typeIds.Length);

            nameIdsMemoryStream.Position = 0;
            nameIdsMemoryStream.CopyTo(stream);

            stream.Write(idcl, 0, idcl.Length);

            // Copy the data memory stream into the file stream
            dataMemoryStream.Position = 0;
            dataMemoryStream.CopyTo(stream);

            if (addedCount != 0)
            {
                bufferedConsole.Write("Number of files added: ");
                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Green;
                bufferedConsole.Write(string.Format("{0} file(s) ", addedCount));
                bufferedConsole.ResetColor();
                bufferedConsole.Write("in ");
                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                bufferedConsole.WriteLine(resourceContainer.Path);
                bufferedConsole.ResetColor();
            }

            bufferedConsole.Flush();
        }

        /// <summary>
        /// Loads the sound mods present in the given sound container
        /// </summary>
        /// <param name="soundContainer">sound container to load the sound mods to</param>
        public static void LoadSoundMods(SoundContainer soundContainer)
        {
            // Buffered console for this operation
            var bufferedConsole = new BufferedConsole();

            using (var fileStream = new FileStream(soundContainer.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            {
                // Read the sound entries in the container
                ReadSoundEntries(fileStream, soundContainer);

                // Load the sound mods
                ReplaceSounds(fileStream, soundContainer, bufferedConsole);
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
        public static void ReplaceSounds(Stream stream, SoundContainer soundContainer, BufferedConsole bufferedConsole)
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
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        bufferedConsole.Write("WARNING: ");
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        bufferedConsole.WriteLine($"Bad file name for sound file \"{soundMod.Name}\" - sound file names should be named after the sound id, or have the sound id at the end of the filename with format \"_id#{{id here}}\", skipping");
                        bufferedConsole.WriteLine($"Examples of valid sound file names:");
                        bufferedConsole.WriteLine($"icon_music_boss_end_2_id#347947739.ogg");
                        bufferedConsole.WriteLine($"347947739.ogg");
                        bufferedConsole.ResetColor();
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
                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                bufferedConsole.Write("WARNING: ");
                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                bufferedConsole.WriteLine($"Couldn't find \"{opusEncPath}\" to encode \"{soundMod.Name}\", skipping");
                                bufferedConsole.ResetColor();
                                continue;
                            }

                            var opusFileData = SoundEncoding.EncodeSoundModFileToOpus(opusEncPath, soundMod);

                            if (opusFileData != null)
                            {
                                soundMod.FileData = new MemoryStream(opusFileData, 0, opusFileData.Length, false, true);
                                encodedSize = (int)soundMod.FileData.Length;
                                format = 2;
                            }
                        }
                        catch (Exception ex)
                        {
                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                            bufferedConsole.Write("ERROR: ");
                            bufferedConsole.ResetColor();
                            bufferedConsole.WriteLine($"While loading sound mod file {soundMod.Name}: {ex}");
                            continue;
                        }
                    }

                    if (format == -1)
                    {
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        bufferedConsole.Write("WARNING: ");
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        bufferedConsole.WriteLine($"Couldn't determine the sound file format for \"{soundMod.Name}\", skipping");
                        bufferedConsole.ResetColor();
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
                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                bufferedConsole.Write("WARNING: ");
                                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                bufferedConsole.WriteLine($"Couldn't find \"{opusDecPath}\" to decode \"{soundMod.Name}\", skipping");
                                bufferedConsole.ResetColor();
                                continue;
                            }

                            decodedSize = SoundEncoding.GetDecodedOpusSoundModFileSize(opusDecPath, soundMod);
                        }
                        catch (Exception ex)
                        {
                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                            bufferedConsole.Write("ERROR: ");
                            bufferedConsole.ResetColor();
                            bufferedConsole.WriteLine($"While loading sound mod file {soundMod.Name}: {ex}");
                            continue;
                        }
                    }

                    if (decodedSize == -1 || encodedSize == -1)
                    {
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        bufferedConsole.Write("WARNING: ");
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        bufferedConsole.WriteLine($"Unsupported sound mod file format for file \"{soundMod.Name}\", skipping");

                        if (soundExtension == ".ogg")
                        {
                            bufferedConsole.WriteLine($".ogg files must be in the Ogg Opus format, Ogg Vorbis is not supported");
                        }

                        bufferedConsole.WriteLine($"Supported sound mod file formats are: {string.Join(", ", SoundEncoding.SupportedFileFormats)}");
                        bufferedConsole.ResetColor();
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
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        bufferedConsole.Write("WARNING: ");
                        bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        bufferedConsole.WriteLine($"Couldn't find sound with id \"{soundModId}\" in \"{soundContainer.Name}\", sound will not be replaced");
                        bufferedConsole.ResetColor();
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
                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                            bufferedConsole.Write("WARNING: ");
                            bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                            bufferedConsole.WriteLine($"Format mismatch: sound file \"{soundMod.Name}\" needs to be format {currentFormat} ({(currentFormat == 3 ? ".wem" : string.Join(", ", SoundEncoding.SupportedOggConversionFileFormats))})");
                            bufferedConsole.WriteLine($"The sound will be replaced but it might not work in-game.");
                            bufferedConsole.ResetColor();
                            break;
                        }
                    }

                    bufferedConsole.WriteLine(string.Format("\tReplaced sound with id {0} [{1}]", soundModId, soundMod.Name));
                    fileCount++;
                }
            }

            if (fileCount > 0)
            {
                bufferedConsole.Write("Number of sounds replaced: ");
                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Green;
                bufferedConsole.Write(string.Format("{0} sound(s) ", fileCount));
                bufferedConsole.ResetColor();
                bufferedConsole.Write("in ");
                bufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                bufferedConsole.WriteLine(soundContainer.Path);
                bufferedConsole.ResetColor();
            }

            bufferedConsole.Flush();
        }

        public static void FillContainerPathList()
        {
            DirectoryInfo searchDirectory = new DirectoryInfo(BasePath);

            foreach (var resourceFile in searchDirectory.EnumerateFiles("*.resources", SearchOption.TopDirectoryOnly))
            {
                ResourceContainerPathList.Add(resourceFile.FullName);

                if (Path.DirectorySeparatorChar == '\\')
                {
                    ResourceContainerPathList.Add(resourceFile.FullName.Replace('\\', '/'));
                }
            }

            foreach (var resourceFile in searchDirectory.EnumerateFiles("*.streamdb", SearchOption.TopDirectoryOnly))
            {
                ResourceContainerPathList.Add(resourceFile.FullName);

                if (Path.DirectorySeparatorChar == '\\')
                {
                    ResourceContainerPathList.Add(resourceFile.FullName.Replace('\\', '/'));
                }
            }

            searchDirectory = new DirectoryInfo(Path.Combine(BasePath, "game"));

            foreach (var resourceFile in searchDirectory.EnumerateFiles("*.resources", SearchOption.AllDirectories))
            {
                ResourceContainerPathList.Add(resourceFile.FullName);

                if (Path.DirectorySeparatorChar == '\\')
                {
                    ResourceContainerPathList.Add(resourceFile.FullName.Replace('\\', '/'));
                }
            }

            searchDirectory = new DirectoryInfo(Path.Combine(BasePath, "sound", "soundbanks", "pc"));

            foreach (var resourceFile in searchDirectory.EnumerateFiles("*.snd", SearchOption.TopDirectoryOnly))
            {
                SoundContainerPathList.Add(resourceFile.FullName);
            }
        }

        /// <summary>
        /// Gets the path to the .resources file for the specified resource name
        /// </summary>
        /// <param name="name">resource name</param>
        /// <returns>the path to the .resources file for the specified resource name, empty string if it wasn't found</returns>
        public static string PathToResource(string name)
        {
            if (name.StartsWith("dlc_hub", StringComparison.Ordinal))
            {
                var dlcHubFileName = name.Substring(4, name.Length - 4);
                name = $"game{Path.DirectorySeparatorChar}dlc{Path.DirectorySeparatorChar}hub{Path.DirectorySeparatorChar}{dlcHubFileName}";
            }
            else if (name.StartsWith("hub", StringComparison.Ordinal))
            {
                name = $"game{Path.DirectorySeparatorChar}hub{Path.DirectorySeparatorChar}{name}";
            }

            return ResourceContainerPathList.FirstOrDefault(p => p.EndsWith(name, StringComparison.Ordinal));
        }

        /// <summary>
        /// Gets the path to the .snd file for the specified sound container name
        /// </summary>
        /// <param name="name">resource name</param>
        /// <returns>the path to the .snd file for the specified sound container name, empty string if it wasn't found</returns>
        public static string PathToSoundContainer(string name)
        {
            return SoundContainerPathList.FirstOrDefault(p => p.EndsWith(name, StringComparison.Ordinal));
        }

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">program args</param>
        /// <returns>0 if no errors occured, 1 if errors occured</returns>
        public static int Main(string[] args)
        {
            // Enable ENABLE_VIRTUAL_TERMINAL_PROCESSING in the console, to use ANSI/VT100 color codes
            int consoleMode;
            var consoleHandle = KernelWrapper.GetStdHandle(-11);
            KernelWrapper.GetConsoleMode(consoleHandle, out consoleMode);
            KernelWrapper.SetConsoleMode(consoleHandle, consoleMode | 0x4);

            // Initialize the global buffered console
            BufferedConsole = new BufferedConsole();

            // Parse arguments
            if (args.Length == 0)
            {
                BufferedConsole.WriteLine("Loads DOOM Eternal mods from ZIPs or loose files in 'Mods' folder into the game installation specified in the game path");
                BufferedConsole.WriteLine("USAGE: EternalModLoader <game path | --version> [OPTIONS]");
                BufferedConsole.WriteLine("\t--version - Prints the version number of the mod loader and exits with exit code same as the version number.");
                BufferedConsole.WriteLine("OPTIONS:");
                BufferedConsole.WriteLine("\t--list-res - List the game files that will be modified and exit.");
                BufferedConsole.WriteLine("\t--verbose - Print more information during the mod loading process.");
                BufferedConsole.WriteLine("\t--slow - Slow mod loading mode that produces slightly smaller .resources files.");
                BufferedConsole.WriteLine("\t--online-safe - Only load online-safe mods.");
                BufferedConsole.WriteLine("\t--compress-textures - Compress texture files during the mod loading process.");
                BufferedConsole.WriteLine("\t--disable-multithreading - Disables multi-threaded mod loading.");
                BufferedConsole.Flush();
                return 1;
            }

            if (args[0] == "--version")
            {
                BufferedConsole.WriteLine(Version.ToString());
                BufferedConsole.Flush();
                return Version;
            }

            BasePath = Path.Combine(args[0], "base");

            if (!Directory.Exists(BasePath))
            {
                BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                BufferedConsole.Write("ERROR: ");
                BufferedConsole.ResetColor();
                BufferedConsole.WriteLine("Game directory does not exist!");
                BufferedConsole.Flush();
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
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        BufferedConsole.WriteLine("INFO: Verbose logging is enabled.");
                        BufferedConsole.ResetColor();
                    }
                    else if (args[i] == "--slow")
                    {
                        SlowMode = true;
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        BufferedConsole.WriteLine("INFO: Slow mod loading mode is enabled.");
                        BufferedConsole.ResetColor();
                    }
                    else if (args[i] == "--online-safe")
                    {
                        LoadOnlineSafeOnlyMods = true;
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        BufferedConsole.WriteLine("INFO: Only online-safe mods will be loaded.");
                        BufferedConsole.ResetColor();
                    }
                    else if (args[i] == "--compress-textures")
                    {
                        CompressTextures = true;
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        BufferedConsole.WriteLine("INFO: Texture compression is enabled.");
                        BufferedConsole.ResetColor();
                    }
                    else if (args[i] == "--disable-multithreading")
                    {
                        MultiThreading = false;
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        BufferedConsole.WriteLine("INFO: Multi-threading is disabled.");
                        BufferedConsole.ResetColor();
                    }
                    else
                    {
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        BufferedConsole.Write("ERROR: ");
                        BufferedConsole.ResetColor();
                        BufferedConsole.WriteLine(string.Format("Unknown option '{0}'", args[i]));
                        BufferedConsole.Flush();
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
                    BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                    BufferedConsole.Write("ERROR: ");
                    BufferedConsole.ResetColor();
                    BufferedConsole.WriteLine($"Error while determining the optimal buffer size, using 4096 as the default: {ex}");
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
                    catch (Exception ex)
                    {
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        BufferedConsole.Write("ERROR: ");
                        BufferedConsole.ResetColor();
                        BufferedConsole.WriteLine($"There was an error while loading \"{ResourceDataFileName}\"");
                        BufferedConsole.WriteLine(ex.ToString());
                    }
                }
                else
                {
                    if (Verbose)
                    {
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                        BufferedConsole.Write("WARNING: ");
                        BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        BufferedConsole.Write(ResourceDataFileName);
                        BufferedConsole.ResetColor();
                        BufferedConsole.WriteLine(" was not found! There will be issues when adding existing new assets to containers...");
                    }
                }
            }

            BufferedConsole.Flush();

            // Read all the necessary game file paths
            FillContainerPathList();

            // Find and read zipped mods
            var fileLoadBufferedConsole = new BufferedConsole();
            var notFoundContainerList = new List<string>();
            var zippedModsTaskList = new List<Task>();
            int totalZippedModCount = 0;

            var zippedStopwatch = new Stopwatch();
            zippedStopwatch.Start();

            foreach (string zippedMod in Directory.EnumerateFiles(Path.Combine(args[0], ModsFolderName), "*.zip", SearchOption.TopDirectoryOnly))
            {
                int zippedModCount = 0;

                var task = Task.Run(() =>
                {
                    // Mod object for this mod
                    Mod mod = new Mod();

                    using (var zipReader = new ZipReader(zippedMod))
                    {
                        foreach (var zipEntry in zipReader)
                        {
                            // Skip directories
                            if (zipEntry.IsDirectory)
                            {
                                continue;
                            }

                            // Read the mod info from the EternalMod JSON if it exists
                            if (!listResources && zipEntry.Name == "EternalMod.json")
                            {
                                try
                                {
                                    var fileMemoryStream = new MemoryStream((int)zipEntry.UncompressedLength);
                                    zipReader.ReadCurrentEntry(fileMemoryStream);

                                    // Try to parse the JSON
                                    Mod.ReadValuesFromJson(mod, Encoding.UTF8.GetString(fileMemoryStream.GetBuffer()));

                                    // If the mod requires a higher mod loader version, print a warning and don't load the mod
                                    if (mod.RequiredVersion > Version)
                                    {
                                        lock (fileLoadBufferedConsole)
                                        {
                                            fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            fileLoadBufferedConsole.Write("WARNING: ");
                                            fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                            fileLoadBufferedConsole.WriteLine($"Mod \"{zippedMod}\" requires mod loader version {mod.RequiredVersion} but the current mod loader version is {Version}, skipping.");
                                            fileLoadBufferedConsole.ResetColor();
                                        }

                                        continue;
                                    }
                                }
                                catch
                                {
                                    lock (fileLoadBufferedConsole)
                                    {
                                        fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                        fileLoadBufferedConsole.Write("ERROR: ");
                                        fileLoadBufferedConsole.ResetColor();
                                        fileLoadBufferedConsole.WriteLine($"Failed to parse {zipEntry.Name} - malformed JSON? - using defaults.");
                                    }
                                }

                                continue;
                            }

                            // Determine the game container for each mod file
                            bool isSoundMod = false;
                            string modFileName = zipEntry.Name;
                            var firstForwardSlash = modFileName.IndexOf('/');

                            if (firstForwardSlash == -1)
                            {
                                continue;
                            }

                            string resourceName = modFileName.Substring(0, firstForwardSlash);

                            // Old mods compatibility
                            if (resourceName == "generated")
                            {
                                resourceName = "gameresources";
                            }
                            else
                            {
                                // Remove the resource name from the name
                                modFileName = modFileName.Substring(firstForwardSlash + 1);
                            }

                            // Check if this is a sound mod or not
                            var resourcePath = PathToResource($"{resourceName}.resources");

                            if (resourcePath == null)
                            {
                                resourcePath = PathToSoundContainer($"{resourceName}.snd");

                                if (resourcePath != null)
                                {
                                    isSoundMod = true;
                                }
                                else
                                {
                                    lock (notFoundContainerList)
                                    {
                                        if (!notFoundContainerList.Contains(resourceName))
                                        {
                                            notFoundContainerList.Add(resourceName);
                                        }
                                    }

                                    return;
                                }
                            }

                            if (isSoundMod)
                            {
                                // Get the sound container info object, create it if it doesn't exist
                                lock (SoundContainerList)
                                {
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
                                            fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            fileLoadBufferedConsole.Write("WARNING: ");
                                            fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                            fileLoadBufferedConsole.WriteLine($"Unsupported sound mod file format \"{soundExtension}\" for file \"{modFileName}\"");
                                            fileLoadBufferedConsole.ResetColor();
                                            continue;
                                        }

                                        // Load the sound mod
                                        SoundModFile soundModFile = new SoundModFile(mod, Path.GetFileName(modFileName));
                                        soundModFile.FileData = new MemoryStream((int)zipEntry.UncompressedLength);
                                        zipReader.ReadCurrentEntry(soundModFile.FileData);

                                        soundContainer.ModFiles.Add(soundModFile);
                                        zippedModCount++;
                                    }
                                }
                            }
                            else
                            {
                                // Get the resource object
                                lock (ResourceList)
                                {
                                    var resource = ResourceList.FirstOrDefault(res => res.Name == resourceName);

                                    if (resource == null)
                                    {
                                        resource = new ResourceContainer(resourceName, PathToResource(resourceName + ".resources"));
                                        ResourceList.Add(resource);
                                    }

                                    // Create the mod object and read the unzipped files
                                    ResourceModFile resourceModFile = new ResourceModFile(mod, modFileName, resourceName);

                                    if (!listResources)
                                    {
                                        resourceModFile.FileData = new MemoryStream((int)zipEntry.UncompressedLength);
                                        zipReader.ReadCurrentEntry(resourceModFile.FileData);
                                    }

                                    // Read the JSON files in 'assetsinfo' under 'EternalMod'
                                    if (modFileName.EndsWith(".json", StringComparison.Ordinal))
                                    {
                                        if (modFileName.StartsWith($"EternalMod/assetsinfo/", StringComparison.Ordinal))
                                        {
                                            try
                                            {
                                                // If we are just listing resources, read this JSON file only
                                                if (listResources)
                                                {
                                                    resourceModFile.FileData = new MemoryStream((int)zipEntry.UncompressedLength);
                                                    zipReader.ReadCurrentEntry(resourceModFile.FileData);
                                                }

                                                resourceModFile.AssetsInfo = AssetsInfo.FromJson(Encoding.UTF8.GetString(resourceModFile.FileData.GetBuffer()));
                                                resourceModFile.IsAssetsInfoJson = true;
                                                resourceModFile.FileData = null;
                                            }
                                            catch
                                            {
                                                fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                                fileLoadBufferedConsole.Write("ERROR: ");
                                                fileLoadBufferedConsole.ResetColor();
                                                fileLoadBufferedConsole.WriteLine($"Failed to parse EternalMod/assetsinfo/{Path.GetFileNameWithoutExtension(resourceModFile.Name)}.json");
                                                continue;
                                            }
                                        }
                                        else if (modFileName.StartsWith($"EternalMod/strings/", StringComparison.Ordinal))
                                        {
                                            // Detect custom localization files
                                            resourceModFile.IsBlangJson = true;
                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }

                                    // Determine if the mod is safe for online play
                                    if (mod.IsSafeForOnline)
                                    {
                                        if (!OnlineSafety.IsModSafeForOnline(resourceModFile))
                                        {
                                            AreModsSafeForOnline = false;
                                            mod.IsSafeForOnline = false;
                                        }
                                    }

                                    if (!LoadOnlineSafeOnlyMods || (LoadOnlineSafeOnlyMods && mod.IsSafeForOnline))
                                    {
                                        resource.ModFileList.Add(resourceModFile);
                                    }

                                    zippedModCount++;
                                }
                            }
                        }
                    }

                    totalZippedModCount += zippedModCount;

                    lock (fileLoadBufferedConsole)
                    {
                        if (zippedModCount > 0 && !listResources)
                        {
                            if ((LoadOnlineSafeOnlyMods && mod.IsSafeForOnline) || !LoadOnlineSafeOnlyMods)
                            {
                                fileLoadBufferedConsole.Write("Found ");
                                fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Blue;
                                fileLoadBufferedConsole.Write(string.Format("{0} file(s) ", zippedModCount));
                                fileLoadBufferedConsole.ResetColor();
                                fileLoadBufferedConsole.Write("in archive ");
                                fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                fileLoadBufferedConsole.Write(zippedMod);
                                fileLoadBufferedConsole.ResetColor();
                                fileLoadBufferedConsole.WriteLine();

                                if (!mod.IsSafeForOnline)
                                {
                                    fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                    fileLoadBufferedConsole.Write("WARNING: ");
                                    fileLoadBufferedConsole.WriteLine($"Mod \"{zippedMod}\" is not safe for online play, multiplayer will be disabled");
                                    fileLoadBufferedConsole.ResetColor();
                                }
                            }
                            else
                            {
                                fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                fileLoadBufferedConsole.Write("WARNING: ");
                                fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                fileLoadBufferedConsole.WriteLine($"Mod \"{zippedMod}\" is not safe for online play, skipping");
                                fileLoadBufferedConsole.ResetColor();
                            }
                        }
                    }

                });

                if (!MultiThreading)
                {
                    task.Wait();
                }
                else
                {
                    zippedModsTaskList.Add(task);
                }
            }

            // Wait for the loading of zipped mods to complete
            if (MultiThreading)
            {
                Task.WaitAll(zippedModsTaskList.ToArray());
            }

            fileLoadBufferedConsole.Flush();
            zippedStopwatch.Stop();

            // Unload Zlib now that we don't need it anymore, if it was loaded
            if (zippedModsTaskList.Count > 0)
            {
                DllLoader.UnloadZlibDll();
            }

            // Find and read unzipped mods
            int unzippedModCount = 0;
            var unzippedModsTaskList = new List<Task>();
            var looseStopwatch = new Stopwatch();
            looseStopwatch.Start();

            Mod globalLooseMod = new Mod();
            globalLooseMod.LoadPriority = int.MinValue;

            foreach (var file in Directory.EnumerateFiles(Path.Combine(args[0], ModsFolderName), "*", SearchOption.AllDirectories))
            {
                var task = Task.Run(() =>
                {
                    if (file.EndsWith(".zip", StringComparison.Ordinal))
                    {
                        return;
                    }

                    string[] modFilePathParts = file.Substring(args[0].Length + 1 + ModsFolderName.Length, file.Length - (args[0].Length + 1 + ModsFolderName.Length)).Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

                    if (modFilePathParts.Length < 2)
                    {
                        return;
                    }

                    string modFileName = string.Join(Path.DirectorySeparatorChar.ToString(), modFilePathParts).Replace('\\', '/');

                    // Determine the game container for each mod file
                    bool isSoundMod = false;
                    var firstForwardSlash = modFileName.IndexOf('/');

                    if (firstForwardSlash == -1)
                    {
                        return;
                    }

                    string resourceName = modFileName.Substring(0, firstForwardSlash);

                    // Old mods compatibility
                    if (resourceName == "generated")
                    {
                        resourceName = "gameresources";
                    }
                    else
                    {
                        // Remove the resource name from the path
                        modFileName = modFileName.Substring(firstForwardSlash + 1);
                    }

                    // Check if this is a sound mod or not
                    var resourcePath = PathToResource($"{resourceName}.resources");

                    if (resourcePath == null)
                    {
                        resourcePath = PathToSoundContainer($"{resourceName}.snd");

                        if (resourcePath != null)
                        {
                            isSoundMod = true;
                        }
                        else
                        {
                            lock (notFoundContainerList)
                            {
                                if (!notFoundContainerList.Contains(resourceName))
                                {
                                    notFoundContainerList.Add(resourceName);
                                }
                            }

                            return;
                        }
                    }

                    if (isSoundMod)
                    {
                        lock (SoundContainerList)
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
                                    lock (fileLoadBufferedConsole)
                                    {
                                        fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                        fileLoadBufferedConsole.Write("WARNING: ");
                                        fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                                        fileLoadBufferedConsole.WriteLine($"Unsupported sound mod file format \"{soundExtension}\" for file \"{modFileName}\"");
                                        fileLoadBufferedConsole.ResetColor();
                                    }

                                    return;
                                }

                                // Load the sound mod
                                SoundModFile soundModFile = new SoundModFile(globalLooseMod, Path.GetFileName(modFileName));

                                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                                {
                                    soundModFile.FileData = new MemoryStream((int)fileStream.Length);
                                    fileStream.CopyTo(soundModFile.FileData);
                                }

                                soundContainer.ModFiles.Add(soundModFile);
                                unzippedModCount++;
                            }
                        }
                    }
                    else
                    {
                        lock (ResourceList)
                        {
                            // Get the resource object
                            var resource = ResourceList.FirstOrDefault(res => res.Name == resourceName);

                            if (resource == null)
                            {
                                resource = new ResourceContainer(resourceName, PathToResource(resourceName + ".resources"));
                                ResourceList.Add(resource);
                            }

                            // Create the mod object and read the files
                            ResourceModFile mod = new ResourceModFile(globalLooseMod, modFileName, resourceName);

                            if (!listResources)
                            {
                                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                                {
                                    mod.FileData = new MemoryStream((int)fileStream.Length);
                                    fileStream.CopyTo(mod.FileData);
                                }
                            }

                            // Read the JSON files in 'assetsinfo' under 'EternalMod'
                            if (modFileName.EndsWith(".json", StringComparison.Ordinal))
                            {
                                if (modFileName.StartsWith($"EternalMod/assetsinfo/", StringComparison.Ordinal))
                                {
                                    try
                                    {
                                        // Read this JSON only if we are listing resources
                                        if (listResources)
                                        {
                                            using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
                                            {
                                                mod.FileData = new MemoryStream((int)fileStream.Length);
                                                fileStream.CopyTo(mod.FileData);
                                            }
                                        }

                                        mod.AssetsInfo = AssetsInfo.FromJson(Encoding.UTF8.GetString(mod.FileData.GetBuffer()));
                                        mod.IsAssetsInfoJson = true;
                                        mod.FileData = null;
                                    }
                                    catch
                                    {
                                        lock (fileLoadBufferedConsole)
                                        {
                                            fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                                            fileLoadBufferedConsole.Write("ERROR: ");
                                            fileLoadBufferedConsole.ResetColor();
                                            fileLoadBufferedConsole.WriteLine($"Failed to parse EternalMod/assetsinfo/{Path.GetFileNameWithoutExtension(mod.Name)}.json");
                                        }

                                        return;
                                    }
                                }
                                else if (modFileName.StartsWith($"EternalMod/strings/", StringComparison.Ordinal))
                                {
                                    // Detect custom language files
                                    mod.IsBlangJson = true;
                                }
                                else
                                {
                                    return;
                                }
                            }

                            // Determine if the mod is safe for online play
                            if (globalLooseMod.IsSafeForOnline)
                            {
                                if (!OnlineSafety.IsModSafeForOnline(mod))
                                {
                                    AreModsSafeForOnline = false;
                                    globalLooseMod.IsSafeForOnline = false;
                                }
                            }

                            if (!LoadOnlineSafeOnlyMods || (LoadOnlineSafeOnlyMods && globalLooseMod.IsSafeForOnline))
                            {
                                resource.ModFileList.Add(mod);
                            }

                            unzippedModCount++;
                        }
                    }
                });

                if (!MultiThreading)
                {
                    task.Wait();
                }
                else
                {
                    unzippedModsTaskList.Add(task);
                }
            }

            // Wait for the unzipped mod loading to finish
            if (MultiThreading)
            {
                Task.WaitAll(unzippedModsTaskList.ToArray());
            }

            fileLoadBufferedConsole.Flush();
            looseStopwatch.Stop();

            if (unzippedModCount > 0 && !listResources)
            {
                if (LoadOnlineSafeOnlyMods && !globalLooseMod.IsSafeForOnline)
                {
                    fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                    fileLoadBufferedConsole.Write("WARNING: ");
                    fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                    fileLoadBufferedConsole.WriteLine("Loose mod files are not safe for online play, skipping");
                    fileLoadBufferedConsole.ResetColor();
                }
                else
                {
                    BufferedConsole.Write("Found ");
                    BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Blue;
                    BufferedConsole.Write(string.Format("{0} file(s) ", unzippedModCount));
                    BufferedConsole.ResetColor();
                    BufferedConsole.Write("in ");
                    BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                    BufferedConsole.Write("'Mods' ");
                    BufferedConsole.ResetColor();
                    BufferedConsole.WriteLine("folder...");

                    if (!globalLooseMod.IsSafeForOnline)
                    {
                        fileLoadBufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                        fileLoadBufferedConsole.Write("WARNING: ");
                        fileLoadBufferedConsole.WriteLine($"Loose mode files are not safe for online play, multiplayer will be disabled");
                        fileLoadBufferedConsole.ResetColor();
                    }
                }
            }

            foreach (var notFoundContainer in notFoundContainerList)
            {
                BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Red;
                BufferedConsole.Write("WARNING: ");
                BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Yellow;
                BufferedConsole.Write($"{notFoundContainer}.resources");
                BufferedConsole.ResetColor();
                BufferedConsole.WriteLine(" was not found! Skipping...");
            }

            BufferedConsole.Flush();

            // Remove resources from the list if they have no mods to load
            for (int i = ResourceList.Count - 1; i >= 0; i--)
            {
                if (ResourceList[i].ModFileList == null || ResourceList[i].ModFileList.Count == 0)
                {
                    ResourceList.RemoveAt(i);
                }
            }

            // Disable multiplayer if needed
            if (!AreModsSafeForOnline && !LoadOnlineSafeOnlyMods)
            {
                foreach (ResourceModFile mod in OnlineSafety.MultiplayerDisablerMod)
                {
                    var resource = ResourceList.FirstOrDefault(res => res.Name == mod.ResourceName);

                    if (resource == null)
                    {
                        continue;
                    }

                    resource.ModFileList.Add(mod);
                }
            }

            // List the resources that will be modified
            if (listResources)
            {
                // We need to set the console encoding to ASCII here to avoid problems with
                // the mod injector parsing the resources list
                Console.OutputEncoding = Encoding.ASCII;

                // Resource file mods
                foreach (var resource in ResourceList)
                {
                    bool shouldListResource = false;

                    if (resource.Path == string.Empty)
                    {
                        continue;
                    }

                    var assetsInfoJsonModFiles = resource.ModFileList.Where(mod => mod.IsAssetsInfoJson);

                    // If this resource only has assetsinfo JSON files, only print this resource if necessary
                    if (assetsInfoJsonModFiles.Count() == resource.ModFileList.Count)
                    {
                        foreach (var assetsInfoJsonFile in assetsInfoJsonModFiles)
                        {
                            if (assetsInfoJsonFile.AssetsInfo == null)
                            {
                                continue;
                            }

                            if (assetsInfoJsonFile.AssetsInfo.Assets != null
                                || assetsInfoJsonFile.AssetsInfo.Layers != null
                                || assetsInfoJsonFile.AssetsInfo.Maps != null)
                            {
                                shouldListResource = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        shouldListResource = true;
                    }

                    if (!shouldListResource)
                    {
                        continue;
                    }

                    if (Path.DirectorySeparatorChar == '\\')
                    {
                        Console.WriteLine($".{resource.Path.Substring(resource.Path.IndexOf("\\base\\", StringComparison.Ordinal))}");
                    }
                    else
                    {
                        Console.WriteLine($".{resource.Path.Substring(resource.Path.IndexOf("/base/", StringComparison.Ordinal))}");
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
                        Console.WriteLine($".{soundContainer.Path.Substring(soundContainer.Path.IndexOf("\\base\\", StringComparison.Ordinal))}");
                    }
                    else
                    {
                        Console.WriteLine($".{soundContainer.Path.Substring(soundContainer.Path.IndexOf("/base/", StringComparison.Ordinal))}");
                    }
                }

                return 0;
            }

            var processStopwatch = new Stopwatch();
            processStopwatch.Start();

            // Task list for each mod loading process
            // We will create a task for each every container that will be modified
            var modLoadingTaskList = new List<Task>();

            // Load the resource file mods
            foreach (var resource in ResourceList)
            {
                var task = Task.Run(() =>
                {
                    LoadMods(resource);
                });

                if (!MultiThreading)
                {
                    task.Wait();
                }
                else
                {
                    modLoadingTaskList.Add(task);
                }
            }

            // Load the sound mods
            foreach (var soundContainer in SoundContainerList)
            {
                var task = Task.Run(() =>
                {
                    LoadSoundMods(soundContainer);
                });

                if (!MultiThreading)
                {
                    task.Wait();
                }
                else
                {
                    modLoadingTaskList.Add(task);
                }
            }

            BufferedConsole.Flush();

            // Wait for all the mod loading tasks to complete
            if (MultiThreading)
            {
                Task.WaitAll(modLoadingTaskList.ToArray());
            }

            // Modify packageMapSpec JSON if needed
            ModifyPackageMapSpec();
            processStopwatch.Stop();

            // Print metrics
            if (totalZippedModCount > 0 || unzippedModCount > 0)
            {
                BufferedConsole.WriteLine();
                BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.DarkGreen;

                if (totalZippedModCount > 0 && Verbose)
                {
                    BufferedConsole.WriteLine($"> Packed mods loaded in {zippedStopwatch.Elapsed}");
                }

                if (unzippedModCount > 0 && Verbose)
                {
                    BufferedConsole.WriteLine($"> Loose mods loaded in {looseStopwatch.Elapsed}");
                }

                if (Verbose)
                {
                    BufferedConsole.WriteLine($"> Injection finished in {processStopwatch.Elapsed}");
                }

                BufferedConsole.ForegroundColor = BufferedConsole.ForegroundColorCode.Green;
                BufferedConsole.WriteLine($"> Total time taken: {processStopwatch.Elapsed + zippedStopwatch.Elapsed + looseStopwatch.Elapsed}");
                BufferedConsole.ResetColor();
                BufferedConsole.Flush();
            }

            return 0;
        }
    }
}