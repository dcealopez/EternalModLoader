using System;
using System.IO;
using System.Text;

namespace EternalModLoader
{
    /// <summary>
    /// Buffered console class
    /// </summary>
    public class BufferedConsole
    {
        /// <summary>
        /// Foreground console color codes
        /// </summary>
        public static class ForegroundColorCode
        {
            /// <summary>
            /// Default color code
            /// </summary>
            public const string Default = "\x1b[38;5;255m";

            /// <summary>
            /// Blue color code
            /// </summary>
            public const string Blue = "\x1b[38;5;27m";

            /// <summary>
            /// Yellow color code
            /// </summary>
            public const string Yellow = "\x1b[38;5;229m";

            /// <summary>
            /// Dark green color code
            /// </summary>
            public const string DarkGreen = "\x1b[38;5;22m";

            /// <summary>
            /// Green color code
            /// </summary>
            public const string Green = "\x1b[38;5;34m";

            /// <summary>
            /// Red color code
            /// </summary>
            public const string Red = "\x1b[38;5;1m";
        }

        /// <summary>
        /// The current console foreground color
        /// </summary>
        public string ForegroundColor = ForegroundColorCode.Default;

        /// <summary>
        /// Buffered stream for the console (standard)
        /// </summary>
        private BufferedStream _bufferedStream;

        /// <summary>
        /// Buffered console initialization method
        /// </summary>
        public BufferedConsole()
        {
            // Unicode encoding
            Console.OutputEncoding = Encoding.Unicode;

            // Avoid special "ShadowBuffer" for hard-coded size 0x14000
            _bufferedStream = new BufferedStream(Console.OpenStandardOutput(), 0x15000);
        }

        /// <summary>
        /// Writes a line break to the buffer stream
        /// </summary>
        public void WriteLine()
        {
            Write($"\r\n");
        }

        /// <summary>
        /// Writes a line to the buffer stream
        /// </summary>
        /// <param name="text">text to write</param>
        public void WriteLine(string text)
        {
            Write($"{ForegroundColor}{text}\r\n");
        }

        /// <summary>
        /// Writes text to the buffer stream
        /// </summary>
        /// <param name="text">text to write</param>
        public void Write(string text)
        {
            text = $"{ForegroundColor}{text}";

            // Avoid endless 'GetByteCount' dithering in 'Encoding.Unicode.GetBytes(s)'
            var rgb = new byte[text.Length << 1];
            Encoding.Unicode.GetBytes(text, 0, text.Length, rgb, 0);

            _bufferedStream.Write(rgb, 0, rgb.Length);
        }

        /// <summary>
        /// Writes the current foreground color to the buffer stream
        /// </summary>
        /// <param name="text">text to write</param>
        private void WriteCurrentForegroundColor()
        {
            // Avoid endless 'GetByteCount' dithering in 'Encoding.Unicode.GetBytes(s)'
            var rgb = new byte[ForegroundColor.Length << 1];
            Encoding.Unicode.GetBytes(ForegroundColor, 0, ForegroundColor.Length, rgb, 0);

            _bufferedStream.Write(rgb, 0, rgb.Length);
        }

        /// <summary>
        /// Resets the console foreground color
        /// </summary>
        public void ResetColor()
        {
            ForegroundColor = ForegroundColorCode.Default;
            WriteCurrentForegroundColor();
        }

        /// <summary>
        /// Flushes the buffered stream
        /// </summary>
        public void Flush()
        {
            _bufferedStream.Flush();
        }
    }
}
