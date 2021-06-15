using System;
using System.IO;
using System.Text;

namespace EternalModLoader
{
    /// <summary>
    /// Buffered console class
    /// </summary>
    public static class BufferedConsole
    {
        /// <summary>
        /// Buffered stream for the console
        /// </summary>
        private static readonly BufferedStream BufferedStream;

        /// <summary>
        /// Static constructor
        /// </summary>
        static BufferedConsole()
        {
            Console.OutputEncoding = Encoding.Unicode;

            // Avoid special "ShadowBuffer" for hard-coded size 0x14000 in 'BufferedStream'
            BufferedStream = new BufferedStream(Console.OpenStandardOutput(), 0x15000);
        }

        /// <summary>
        /// Writes a line to the buffer stream
        /// </summary>
        /// <param name="s">text to write</param>
        public static void WriteLine(string s)
        {
            Write(s + "\r\n");
        }

        /// <summary>
        /// Writes text to the buffer stream
        /// </summary>
        /// <param name="s">text to write</param>
        public static void Write(string s)
        {
            // Avoid endless 'GetByteCount' dithering in 'Encoding.Unicode.GetBytes(s)'
            var rgb = new byte[s.Length << 1];
            Encoding.Unicode.GetBytes(s, 0, s.Length, rgb, 0);

            BufferedStream.Write(rgb, 0, rgb.Length);
        }

        /// <summary>
        /// Flushes the buffered stream
        /// </summary>
        public static void Flush()
        {
            BufferedStream.Flush();
        }
    }
}
