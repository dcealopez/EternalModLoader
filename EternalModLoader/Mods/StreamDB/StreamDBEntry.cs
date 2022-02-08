using System.IO;

namespace EternalModLoader.Mods.StreamDB
{
    /// <summary>
    /// StreamDB entry class
    /// </summary>
    public class StreamDBEntry
    {
        /// <summary>
        /// 0x00 - File Id
        /// </summary>
        public ulong FileId;

        /// <summary>
        /// 0x08 - Data offset for this file entry in the streamdb file - multiply by 16 for real offset
        /// </summary>
        public uint DataOffset16;

        /// <summary>
        /// 0x0C - Compressed file length
        /// </summary>
        public uint DataLength;

        /// <summary>
        /// StreamDB mod file name
        /// </summary>
        public string Name;

        /// <summary>
        /// File data memory stream
        /// </summary>
        public MemoryStream FileData;

        /// <summary>
        /// StreamDB entry constructor
        /// </summary>
        /// <param name="fileId">file id</param>
        /// <param name="dataOffset16">data offset for this entry, divided by 16</param>
        /// <param name="dataLength">data length</param>
        public StreamDBEntry(ulong fileId, uint dataOffset16, uint dataLength, string name, MemoryStream fileData)
        {
            FileId = fileId;
            DataOffset16 = dataOffset16;
            DataLength = dataLength;
            Name = name;
            FileData = fileData;
        }

        /// <summary>
        /// Copies the mod file data stream to the given stream
        /// </summary>
        /// <param name="stream">destination stream</param>
        public void CopyFileDataToStream(Stream stream)
        {
            if (FileData == null)
            {
                return;
            }

            FileData.Position = 0;
            FileData.CopyTo(stream);
        }
    }
}