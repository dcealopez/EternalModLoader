using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

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
        /// Game base path
        /// </summary>
        public static string BasePath;

        /// <summary>
        /// Resource list
        /// </summary>
        public static List<ResourceInfo> ResourceList = new List<ResourceInfo>();

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
                    List<long> namesOffsetList = new List<long>();

                    fileStream.Seek(namesOffset, SeekOrigin.Begin);
                    long namesNum = binaryReader.ReadInt64();

                    for (int i = 0; i < namesNum; i++)
                    {
                        namesOffsetList.Add(binaryReader.ReadInt64());
                    }

                    long namesOffsetEnd = fileStream.Position;
                    long namesSize = namesEnd - namesOffsetEnd;
                    List<string> namesList = new List<string>();
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

                            string name = System.Text.Encoding.UTF8.GetString(currentNameBytes.ToArray());

                            // Trim trailing '$'
                            int indexOfDollar = name.IndexOf('$');

                            if (indexOfDollar != -1)
                            {
                                name = name.Substring(0, indexOfDollar);
                            }

                            // Trim trailing '#'
                            int indexOfHashTrail = name.LastIndexOf('#');

                            if (indexOfHashTrail != -1)
                            {
                                name = name.Substring(0, indexOfHashTrail);
                            }

                            // Trim leading '#'
                            int indexOfHash = name.IndexOf('#');

                            if (indexOfHash != -1)
                            {
                                name = name.Substring(indexOfHash + 1);
                            }

                            namesList.Add(name);
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
                string name = resourceInfo.NamesList[(int)nameId];

                var chunk = new ResourceChunk(name, fileOffset)
                {
                    NameId = nameId,
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
                if (chunk.Name.Equals(name))
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
                    ResourceChunk chunk;

                    if (mod.isBlangJson)
                    {
                        var modName = mod.Name;
                        var modFilePathParts = modName.Split('/');
                        var name = modName.Remove(0, modFilePathParts[0].Length + 1);
                        mod.Name = name.Substring(0, name.Length - 4) + "blang";
                        chunk = GetChunk(mod.Name, resourceInfo);

                        if (chunk == null)
                            continue;
                    }
                    else
                    {
                        chunk = GetChunk(mod.Name, resourceInfo);
                        
                        if (chunk == null)
                        {
                            resourceInfo.ModListNew.Add(mod);
                            continue;
                        }
                    }

                    memoryStream.Seek(chunk.FileOffset, SeekOrigin.Begin);
                    long fileOffset = binaryReader.ReadInt64();
                    long size = binaryReader.ReadInt64();
                    long sizeDiff = mod.FileBytes.Length - size;

                    // If the mod is a blang file json, modify the .blang file
                    if (mod.isBlangJson)
                    {
                        BlangJson blangJson;
                        try
                        {
                            blangJson = JsonConvert.DeserializeObject<BlangJson>(Encoding.UTF8.GetString(mod.FileBytes));
                            if (blangJson == null || blangJson.strings.Count == 0)
                                throw new Exception();
                            foreach (var blangJsonString in blangJson.strings)
                            {
                                if (blangJsonString == null || blangJsonString.name == null || blangJsonString.text == null)
                                    throw new Exception();
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
                            continue;
                        }

                        BlangFile blangFile;
                        using (var blangMemoryStream = new MemoryStream(blangFileBytes))
                        {
                            blangFile = BlangFile.Parse(blangMemoryStream);

                        }

                        foreach (var blangJsonString in blangJson.strings)
                        {
                            bool stringFound = false;
                            
                            foreach (var blangString in blangFile.Strings)
                            {
                                if (blangJsonString.name.Equals(blangString.Identifier))
                                {
                                    stringFound = true;
                                    blangString.Text = blangJsonString.text;
                                    Console.WriteLine($"\tReplaced {blangString.Identifier} in {mod.Name}");
                                    break;
                                }
                            }

                            if (stringFound)
                                continue;
                            
                            blangFile.Strings.Add(new BlangString()
                            {
                                Identifier = blangJsonString.name,
                                Text = blangJsonString.text,
                            });
                            Console.WriteLine($"\tAdded {blangJsonString.name} in {mod.Name}");
                        }

                        byte[] cryptDataBuffer = blangFile.WriteToBytes();
                        res = BlangCrypt.IdCrypt(ref cryptDataBuffer, $"strings/{Path.GetFileName(mod.Name)}", false);
                        if (res != 0)
                        {
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
                    memoryStream.Write(BitConverter.GetBytes((long)mod.FileBytes.Length), 0, 8);

                    // Clear the compression flag
                    memoryStream.Seek(chunk.SizeOffset + 0x30, SeekOrigin.Begin);
                    memoryStream.WriteByte(0);

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

                    Console.WriteLine(string.Format("\tReplaced {0}", mod.Name));
                    fileCount++;
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

            List<ResourceChunk> newChunks = new List<ResourceChunk>();

            // Add the new mod files now
            foreach (var mod in resourceInfo.ModListNew)
            {
                // Add data
                long fileOffset = 0;
                long placement = (0x10 - (data.Length % 0x10)) + 0x30;
                Array.Resize(ref data, (int)(data.Length + placement));
                fileOffset = data.Length + resourceInfo.DataOffset;
                Array.Resize(ref data, data.Length + mod.FileBytes.Length);
                Buffer.BlockCopy(mod.FileBytes, 0, data, data.Length - mod.FileBytes.Length, mod.FileBytes.Length);

                // Add nameId in dummy7 nameIds
                long nameId = 0;
                long nameIdOffset = 0;
                nameId = resourceInfo.NamesList.Count;
                Array.Resize(ref nameIds, nameIds.Length + 8);
                nameIdOffset = (nameIds.Length / 8) - 1;
                Array.Resize(ref nameIds, nameIds.Length + 8);
                Buffer.BlockCopy(BitConverter.GetBytes(nameId), 0, nameIds, nameIds.Length - 8, 8);

                // Add name
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

                byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(mod.Name);
                Array.Resize(ref names, names.Length + nameBytes.Length + 1);
                Buffer.BlockCopy(nameBytes, 0, names, (int)lastNameOffset, nameBytes.Length);

                // Add name offset
                byte[] newCount = BitConverter.GetBytes(BitConverter.ToInt64(nameOffsets.Take(8).ToArray(), 0) + 1);
                Buffer.BlockCopy(newCount, 0, nameOffsets, 0, 8);
                Array.Resize(ref nameOffsets, nameOffsets.Length + 8);
                Buffer.BlockCopy(BitConverter.GetBytes(lastNameOffset), 0, nameOffsets, nameOffsets.Length - 8, 8);

                // Add info
                byte[] lastInfo = info.Skip(info.Length - 0x90).ToArray();
                Array.Resize(ref info, info.Length + 0x90);
                Buffer.BlockCopy(lastInfo, 0, info, info.Length - 0x90, lastInfo.Length);
                Buffer.BlockCopy(BitConverter.GetBytes(nameIdOffset), 0, info, info.Length - 0x70, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(fileOffset), 0, info, info.Length - 0x58, 8);
                Buffer.BlockCopy(BitConverter.GetBytes((long)mod.FileBytes.Length), 0, info, info.Length - 0x50, 8);
                Buffer.BlockCopy(BitConverter.GetBytes((long)mod.FileBytes.Length), 0, info, info.Length - 0x48, 8);
                info[info.Length - 0x20] = 0;

                // Create the new chunk object now
                var newChunk = new ResourceChunk(mod.Name, fileOffset)
                {
                    NameId = nameId,
                    Size = mod.FileBytes.Length,
                    SizeZ = mod.FileBytes.Length,
                    CompressionMode = 0
                };

                Console.WriteLine(string.Format("\tAdded {0}", mod.Name));
                resourceInfo.NamesList.Add(mod.Name);
                newChunks.Add(newChunk);
            }

            // Rebuild the entire container now
            long namesOffsetAdd = info.Length - infoOldLength;
            long newSize = nameOffsets.Length + names.Length;
            long unknownAdd = namesOffsetAdd + (newSize - resourceInfo.StringsSize);
            long typeIdsAdd = unknownAdd;
            long nameIdsAdd = typeIdsAdd;
            long idclAdd = nameIdsAdd + (nameIds.Length - nameIdsOldLength);
            long dataAdd = idclAdd;

            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.FileCount + newChunks.Count), 0, header, 0x20, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(resourceInfo.FileCount2 + (newChunks.Count * 2)), 0, header, 0x2C, 4);
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

            if (newChunks.Count != 0)
            {
                Console.Write("Number of files added: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(string.Format("{0} file(s) ", newChunks.Count));
                Console.ResetColor();
                Console.Write("in ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(resourceInfo.Path);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Gets the path to the .resources file for the specified resource name
        /// </summary>
        /// <param name="name">resource name</param>
        /// <returns>the path to the .resources file for the specified resource name, empty string if it wasn't found</returns>
        public static string PathToRes(string name)
        {
            string searchPattern;

            // Support for DLC1 hub resources files
            // It has the same name as the base game hub resources file, so we will need
            // to adjust the search pattern to find the one we want depending on the folder name of the mod
            if (name.ToLower().StartsWith("dlc_hub"))
            {
                string dlcHubFileName = name.Substring(4, name.Length - 4);
                searchPattern = Path.Combine("game", "dlc", "hub", $"{dlcHubFileName}.resources");
            }
            else if (name.ToLower().StartsWith("hub"))
            {
                searchPattern = Path.Combine("game", "hub", $"{name}.resources");
            }
            else
            {
                searchPattern = name + ".resources";
            }

            try
            {
                DirectoryInfo baseFolder = new DirectoryInfo(BasePath);
                return baseFolder.GetFiles(searchPattern, SearchOption.AllDirectories).FirstOrDefault().FullName;
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
            if (args.Length == 0 || args.Length > 2)
            {
                Console.WriteLine("Loads mods from ZIPs or loose files in 'Mods' folder into the .resources files in the specified directory");
                Console.WriteLine("USAGE: EternalModLoader <game path> [OPTIONS]");
                Console.WriteLine("OPTIONS:");
                Console.WriteLine("\t--list-res - List the .resources files that will be modified and exit.");
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

            if (args.Length == 2)
            {
                if (args[1].Equals("--list-res"))
                {
                    listResources = true;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("ERROR: ");
                    Console.ResetColor();
                    Console.Error.WriteLine(string.Format("Unknown option '{0}'", args[1]));
                    return 1;
                }
            }

            // Find zipped mods
            foreach (string zippedMod in Directory.GetFiles("Mods", "*.zip", SearchOption.TopDirectoryOnly))
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

                        // Get the resource object
                        ResourceInfo resource = null;

                        foreach (var res in ResourceList)
                        {
                            if (res.Name.Equals(resourceName))
                            {
                                resource = res;
                                break;
                            }
                        }

                        if (resource == null)
                        {
                            resource = new ResourceInfo(resourceName, PathToRes(resourceName));
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

                            if (modFilePathParts[1].Equals("EternalMod", StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (modFilePathParts.Length == 4
                                    && modFilePathParts[2].Equals("strings", StringComparison.InvariantCultureIgnoreCase)
                                    && Path.GetExtension(modFilePathParts[3]).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    mod.isBlangJson = true;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                mod.isBlangJson = false;
                            }
                            
                            resource.ModList.Add(mod);
                            zippedModCount++;
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

            foreach (var file in Directory.GetFiles("Mods", "*", SearchOption.AllDirectories))
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

                // Get the resource object
                ResourceInfo resource = null;

                foreach (var res in ResourceList)
                {
                    if (res.Name.Equals(resourceName))
                    {
                        resource = res;
                        break;
                    }
                }

                if (resource == null)
                {
                    resource = new ResourceInfo(resourceName, PathToRes(resourceName));
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
                    
                    if (modFilePathParts[2].Equals("EternalMod", StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (modFilePathParts.Length == 5
                            && modFilePathParts[3].Equals("strings", StringComparison.InvariantCultureIgnoreCase)
                            && Path.GetExtension(modFilePathParts[4]).Equals(".json", StringComparison.InvariantCultureIgnoreCase))
                        {
                            mod.isBlangJson = true;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        mod.isBlangJson = false;
                    }

                    resource.ModList.Add(mod);
                    unzippedModCount++;
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
                foreach (var resource in ResourceList)
                {
                    if (string.IsNullOrEmpty(resource.Path))
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

                return 0;
            }

            // Load the mods
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

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished.");
            Console.ResetColor();
            return 0;
        }
    }
}
