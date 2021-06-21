using System;
using System.Runtime.InteropServices;

namespace EternalModLoader
{
    /// <summary>
    /// Oodle (de)compression implementation
    /// </summary>
    public static class OodleWrapper
    {
        /// <summary>
        /// Oodle Library Path
        /// </summary>
        public const string OodleLibraryPath = @"..\oo2core_8_win64.dll";

        /// <summary>
        /// Oodle64 Decompression Method
        /// </summary>
        [DllImport(OodleLibraryPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OodleLZ_Decompress(byte[] src, long srcLen, byte[] dst, long dstLen,
            int fuzz, int crc, int verbose, byte[] dstBase, long e, long cb, long cb_ctx, long scratch,
            long scratchSize, int ThreadModule);

        /// <summary>
        /// Oodle64 Compression Method
        /// </summary>
        [DllImport(OodleLibraryPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int OodleLZ_Compress(OodleFormat codec, byte[] src, long srcLen, byte[] dst,
            OodleCompressionLevel level, long opts, long offs, long unused, long scratch, long scratchSize);

        /// <summary>
        /// Oodle compression levels
        /// </summary>
        public enum OodleCompressionLevel : int
        {
            None,
            SuperFast,
            VeryFast,
            Fast,
            Normal,
            Optimal1,
            Optimal2,
            Optimal3,
            Optimal4,
            Optimal5
        }

        /// <summary>
        /// Oodle formats
        /// </summary>
        public enum OodleFormat : int
        {
            LZH,
            LZHLW,
            LZNIB,
            None,
            LZB16,
            LZBLW,
            LZA,
            LZNA,
            Kraken,
            Mermaid,
            BitKnit,
            Selkie,
            Akkorokamui
        }

        /// <summary>
        /// Decompresses a byte array of Oodle compressed data
        /// </summary>
        /// <param name="compressedData">Input compressed data</param>
        /// <param name="decompressedSize">Decompressed size</param>
        /// <returns>Resulting byte array if success, otherwise null</returns>
        public static byte[] Decompress(byte[] compressedData, long decompressedSize)
        {
            byte[] decompressedData = new byte[decompressedSize];
            int decodedSize = OodleLZ_Decompress(compressedData, compressedData.Length, decompressedData, decompressedSize, 1, 1, 0, null, 0, 0, 0, 0, 0, 0);

            return decodedSize == 0 ? null : decompressedData;
        }

        /// <summary>
        /// Compresses a byte array to Oodle compressed data
        /// </summary>
        /// <param name="data">data to compress</param>
        /// <param name="format">Oodle format</param>
        /// <param name="compressionLevel">Oodle compression level</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] data, OodleFormat format, OodleCompressionLevel compressionLevel)
        {
            uint compressedBufferSize = (uint)data.Length + 274 * (((uint)data.Length + 0x3FFFF) / 0x40000);
            byte[] compressedBuffer = new byte[compressedBufferSize];
            int compressedSize = OodleLZ_Compress(format, data, data.Length, compressedBuffer, compressionLevel, 0, 0, 0, 0, 0);

            if (compressedSize < 0)
            {
                return null;
            }

            byte[] outputBuffer = new byte[compressedSize];
            Buffer.BlockCopy(compressedBuffer, 0, outputBuffer, 0, compressedSize);

            return outputBuffer;
        }
    }
}
