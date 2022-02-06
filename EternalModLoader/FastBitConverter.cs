using System;
using System.Runtime.InteropServices;

namespace EternalModLoader
{
    /// <summary>
    /// Fast bit converter class
    /// </summary>
    public static class FastBitConverter
    {
        /// <summary>
        /// Int16 struct
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct Int16Struct
        {
            [FieldOffset(0)]
            public byte Byte0;
            [FieldOffset(1)]
            public byte Byte1;

            [FieldOffset(0)]
            public short Int16;
        }

        /// <summary>
        /// Int32 struct
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct Int32Struct
        {
            [FieldOffset(0)]
            public byte Byte0;
            [FieldOffset(1)]
            public byte Byte1;
            [FieldOffset(2)]
            public byte Byte2;
            [FieldOffset(3)]
            public byte Byte3;

            [FieldOffset(0)]
            public int Int32;
        }

        /// <summary>
        /// Int64 struct
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct Int64Struct
        {
            [FieldOffset(0)]
            public byte Byte0;
            [FieldOffset(1)]
            public byte Byte1;
            [FieldOffset(2)]
            public byte Byte2;
            [FieldOffset(3)]
            public byte Byte3;
            [FieldOffset(4)]
            public byte Byte4;
            [FieldOffset(5)]
            public byte Byte5;
            [FieldOffset(6)]
            public byte Byte6;
            [FieldOffset(7)]
            public byte Byte7;

            [FieldOffset(0)]
            public long Int64;
        }

        /// <summary>
        /// UInt16 struct
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct UInt16Struct
        {
            [FieldOffset(0)]
            public byte Byte0;
            [FieldOffset(1)]
            public byte Byte1;

            [FieldOffset(0)]
            public ushort UInt16;
        }

        /// <summary>
        /// UInt32 struct
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct UInt32Struct
        {
            [FieldOffset(0)]
            public byte Byte0;
            [FieldOffset(1)]
            public byte Byte1;
            [FieldOffset(2)]
            public byte Byte2;
            [FieldOffset(3)]
            public byte Byte3;

            [FieldOffset(0)]
            public uint UInt32;
        }

        /// <summary>
        /// UInt64 struct
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct UInt64Struct
        {
            [FieldOffset(0)]
            public byte Byte0;
            [FieldOffset(1)]
            public byte Byte1;
            [FieldOffset(2)]
            public byte Byte2;
            [FieldOffset(3)]
            public byte Byte3;
            [FieldOffset(4)]
            public byte Byte4;
            [FieldOffset(5)]
            public byte Byte5;
            [FieldOffset(6)]
            public byte Byte6;
            [FieldOffset(7)]
            public byte Byte7;

            [FieldOffset(0)]
            public ulong UInt64;
        }

        /// <summary>
        /// Gets the bytes from an Int16
        /// </summary>
        /// <param name="value">int16 value</param>
        /// <param name="reverse">reverse the resulting byte array</param>
        /// <returns>the bytes from the Int16</returns>
        public static byte[] GetBytes(short value, bool reverse = false)
        {
            Int16Struct int16 = new Int16Struct() { Int16 = value };

            if (reverse)
            {
                return new byte[] { int16.Byte1, int16.Byte0 };
            }

            return new byte[] { int16.Byte0, int16.Byte1 };
        }

        /// <summary>
        /// Gets the bytes from an Int32
        /// </summary>
        /// <param name="value">int32 value</param>
        /// <returns>the bytes from the Int32</returns>
        public static byte[] GetBytes(int value, bool reverse = false)
        {
            Int32Struct int32 = new Int32Struct() { Int32 = value };

            if (reverse)
            {
                return new byte[] { int32.Byte3, int32.Byte2, int32.Byte1, int32.Byte0 };
            }

            return new byte[] { int32.Byte0, int32.Byte1, int32.Byte2, int32.Byte3 };
        }

        /// <summary>
        /// Gets the bytes from an Int64
        /// </summary>
        /// <param name="value">int64 value</param>
        /// <returns>the bytes from the Int64</returns>
        public static byte[] GetBytes(long value, bool reverse = false)
        {
            Int64Struct int64 = new Int64Struct() { Int64 = value };

            if (reverse)
            {
                return new byte[] { int64.Byte7, int64.Byte6, int64.Byte5, int64.Byte4, int64.Byte3, int64.Byte2, int64.Byte1, int64.Byte0 };
            }

            return new byte[] { int64.Byte0, int64.Byte1, int64.Byte2, int64.Byte3, int64.Byte4, int64.Byte5, int64.Byte6, int64.Byte7 };
        }

        /// <summary>
        /// Gets the bytes from an UInt16
        /// </summary>
        /// <param name="value">uint16 value</param>
        /// <returns>the bytes from the UInt16</returns>
        public static byte[] GetBytes(ushort value, bool reverse = false)
        {
            UInt16Struct uint16 = new UInt16Struct() { UInt16 = value };

            if (reverse)
            {
                return new byte[] { uint16.Byte1, uint16.Byte0 };
            }

            return new byte[] { uint16.Byte0, uint16.Byte1 };
        }

        /// <summary>
        /// Gets the bytes from an UInt32
        /// </summary>
        /// <param name="value">uint32 value</param>
        /// <returns>the bytes from the UInt32</returns>
        public static byte[] GetBytes(uint value, bool reverse)
        {
            UInt32Struct uint32 = new UInt32Struct() { UInt32 = value };

            if (reverse)
            {
                return new byte[] { uint32.Byte3, uint32.Byte2, uint32.Byte1, uint32.Byte0 };
            }

            return new byte[] { uint32.Byte0, uint32.Byte1, uint32.Byte2, uint32.Byte3 };
        }

        /// <summary>
        /// Gets the bytes from an UInt64
        /// </summary>
        /// <param name="value">uint64 value</param>
        /// <returns>the bytes from the UInt64</returns>
        public static byte[] GetBytes(ulong value, bool reverse = false)
        {
            UInt64Struct uint64 = new UInt64Struct() { UInt64 = value };

            if (reverse)
            {
                return new byte[] { uint64.Byte7, uint64.Byte6, uint64.Byte5, uint64.Byte4, uint64.Byte3, uint64.Byte2, uint64.Byte1, uint64.Byte0 };
            }

            return new byte[] { uint64.Byte0, uint64.Byte1, uint64.Byte2, uint64.Byte3, uint64.Byte4, uint64.Byte5, uint64.Byte6, uint64.Byte7 };
        }

        /// <summary>
        /// Gets an Int16 from the next 2 bytes in a byte array, starting at the specified index
        /// </summary>
        /// <param name="array">byte array</param>
        /// <param name="startIndex">start index</param>
        /// <param name="reverse">reverse the byte array, useful to quickly switch endiannes</param>
        /// <returns>the int16 value</returns>
        public static short ToInt16(byte[] array, int startIndex, bool reverse = false)
        {
            if (array.Length - startIndex < 2)
            {
                throw new ArgumentException("Array must be at least of length 2");
            }

            Int16Struct int16;

            if (reverse)
            {
                int16 = new Int16Struct() { Byte1 = array[startIndex], Byte0 = array[++startIndex] };
            }
            else
            {
                int16 = new Int16Struct() { Byte0 = array[startIndex], Byte1 = array[++startIndex] };
            }

            return int16.Int16;
        }

        /// <summary>
        /// Gets an Int32 from the next 4 bytes in a byte array, starting at the specified index
        /// </summary>
        /// <param name="array">byte array</param>
        /// <param name="startIndex">start index</param>
        /// <param name="reverse">reverse the byte array, useful to quickly switch endiannes</param>
        /// <returns>the int32 value</returns>
        public static int ToInt32(byte[] array, int startIndex, bool reverse = false)
        {
            if (array.Length - startIndex < 4)
            {
                throw new ArgumentException("Array must be at least of length 4");
            }

            Int32Struct int32;

            if (reverse)
            {
                int32 = new Int32Struct() { Byte3 = array[startIndex], Byte2 = array[++startIndex], Byte1 = array[++startIndex], Byte0 = array[++startIndex] };
            }
            else
            {
                int32 = new Int32Struct() { Byte0 = array[startIndex], Byte1 = array[++startIndex], Byte2 = array[++startIndex], Byte3 = array[++startIndex] };
            }

            return int32.Int32;
        }

        /// <summary>
        /// Gets an Int64 from the next 8 bytes in a byte array, starting at the specified index
        /// </summary>
        /// <param name="array">byte array</param>
        /// <param name="startIndex">start index</param>
        /// <param name="reverse">reverse the byte array, useful to quickly switch endiannes</param>
        /// <returns>the int64 value</returns>
        public static long ToInt64(byte[] array, int startIndex, bool reverse = false)
        {
            if (array.Length - startIndex < 8)
            {
                throw new ArgumentException("Array must be at least of length 8");
            }

            Int64Struct int64;

            if (reverse)
            {
                int64 = new Int64Struct() { Byte7 = array[startIndex], Byte6 = array[++startIndex], Byte5 = array[++startIndex], Byte4 = array[++startIndex], Byte3 = array[++startIndex], Byte2 = array[++startIndex], Byte1 = array[++startIndex], Byte0 = array[++startIndex] };
            }
            else
            {
                int64 = new Int64Struct() { Byte0 = array[startIndex], Byte1 = array[++startIndex], Byte2 = array[++startIndex], Byte3 = array[++startIndex], Byte4 = array[++startIndex], Byte5 = array[++startIndex], Byte6 = array[++startIndex], Byte7 = array[++startIndex] };
            }

            return int64.Int64;
        }

        /// <summary>
        /// Gets an UInt16 from the next 2 bytes in a byte array, starting at the specified index
        /// </summary>
        /// <param name="array">byte array</param>
        /// <param name="startIndex">start index</param>
        /// <param name="reverse">reverse the byte array, useful to quickly switch endiannes</param>
        /// <returns>the uint16 value</returns>
        public static ushort ToUInt16(byte[] array, int startIndex, bool reverse = false)
        {
            if (array.Length - startIndex < 2)
            {
                throw new ArgumentException("Array must be at least of length 2");
            }

            UInt16Struct uint16;

            if (reverse)
            {
                uint16 = new UInt16Struct() { Byte1 = array[startIndex], Byte0 = array[++startIndex] };
            }
            else
            {
                uint16 = new UInt16Struct() { Byte0 = array[startIndex], Byte1 = array[++startIndex] };
            }

            return uint16.UInt16;
        }

        /// <summary>
        /// Gets an UInt32 from the next 4 bytes in a byte array, starting at the specified index
        /// </summary>
        /// <param name="array">byte array</param>
        /// <param name="startIndex">start index</param>
        /// <param name="reverse">reverse the byte array, useful to quickly switch endiannes</param>
        /// <returns>the uint32 value</returns>
        public static uint ToUInt32(byte[] array, int startIndex, bool reverse = false)
        {
            if (array.Length - startIndex < 4)
            {
                throw new ArgumentException("Array must be at least of length 4");
            }

            UInt32Struct uint32;

            if (reverse)
            {
                uint32 = new UInt32Struct() { Byte3 = array[startIndex], Byte2 = array[++startIndex], Byte1 = array[++startIndex], Byte0 = array[++startIndex] };
            }
            else
            {
                uint32 = new UInt32Struct() { Byte0 = array[startIndex], Byte1 = array[++startIndex], Byte2 = array[++startIndex], Byte3 = array[++startIndex] };
            }

            return uint32.UInt32;
        }

        /// <summary>
        /// Gets an UInt64 from the next 8 bytes in a byte array, starting at the specified index
        /// </summary>
        /// <param name="array">byte array</param>
        /// <param name="startIndex">start index</param>
        /// <param name="reverse">reverse the byte array, useful to quickly switch endiannes</param>
        /// <returns>the uint64 value</returns>
        public static ulong ToUInt64(byte[] array, int startIndex, bool reverse = false)
        {
            if (array.Length - startIndex < 8)
            {
                throw new ArgumentException("Array must be at least of length 8");
            }

            UInt64Struct uint64;

            if (reverse)
            {
                uint64 = new UInt64Struct() { Byte7 = array[startIndex], Byte6 = array[++startIndex], Byte5 = array[++startIndex], Byte4 = array[++startIndex], Byte3 = array[++startIndex], Byte2 = array[++startIndex], Byte1 = array[++startIndex], Byte0 = array[++startIndex] };
            }
            else
            {
                uint64 = new UInt64Struct() { Byte0 = array[startIndex], Byte1 = array[++startIndex], Byte2 = array[++startIndex], Byte3 = array[++startIndex], Byte4 = array[++startIndex], Byte5 = array[++startIndex], Byte6 = array[++startIndex], Byte7 = array[++startIndex] };
            }

            return uint64.UInt64;
        }
    }
}
