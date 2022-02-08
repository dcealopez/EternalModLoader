using System.Collections.Generic;

namespace EternalModLoader.Mods.StreamDB
{
    /// <summary>
    /// streamdb header class - size 0x20 bytes
    /// </summary>
    public class StreamDBHeader
    {
        /// <summary>
        /// 0x00
        /// </summary>
        public ulong Magic = 7045867521639097680;

        /// <summary>
        /// 0x08 - offset in this .streamdb where embedded file data starts (end of header/entries)
        /// </summary>
        public int DataStartOffset = 0;

        /// <summary>
        /// 0x0C
        /// </summary>
        public int Padding0 = 0;

        /// <summary>
        /// 0x10
        /// </summary>
        public int Padding1 = 0;

        /// <summary>
        /// 0x14
        /// </summary>
        public int Padding2 = 0;

        /// <summary>
        /// 0x18 - number of entries in this .streamdb file
        /// </summary>
        public int NumEntries = 0;

        /// <summary>
        /// 0x1C
        /// </summary>
        public int Flags = 3;

        /// <summary>
        /// streamdb header constructor
        /// </summary>
        /// <param name="dataStartOffset">data start offset</param>
        /// <param name="numEntries">number of entries in this .streamdb file</param>
        public StreamDBHeader(int dataStartOffset, int numEntries)
        {
            DataStartOffset = dataStartOffset;
            NumEntries = numEntries;
        }
    }
}
