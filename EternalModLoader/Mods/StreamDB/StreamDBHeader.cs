using System.Collections.Generic;

namespace EternalModLoader.Mods.StreamDB
{
    /// <summary>
    /// streamdb header class
    /// </summary>
    public class StreamDBHeader
    {
        /// <summary>
        /// 0x00
        /// </summary>
        public const ulong Magic = 7045867521639097680;

        /// <summary>
        /// 0x08 - offset in this .streamdb where embedded file data starts (end of header/entries)
        /// </summary>
        public uint DataStartOffset;

        /// <summary>
        /// 0x0C
        /// </summary>
        public const int Padding0 = 0;

        /// <summary>
        /// 0x10
        /// </summary>
        public const int Padding1 = 0;

        /// <summary>
        /// 0x14
        /// </summary>
        public const int Padding2 = 0;

        /// <summary>
        /// 0x18 - number of entries in this .streamdb file
        /// </summary>
        public uint NumEntries;

        /// <summary>
        /// 0x1C
        /// </summary>
        public const int Flags = 3;

        /// <summary>
        /// streamdb header constructor
        /// </summary>
        /// <param name="dataStartOffset">data start offset</param>
        /// <param name="numEntries">number of entries in this .streamdb file</param>
        public StreamDBHeader(uint dataStartOffset, uint numEntries)
        {
            DataStartOffset = dataStartOffset;
            NumEntries = numEntries;
        }
    }
}
