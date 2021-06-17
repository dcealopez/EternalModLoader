﻿using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace EternalModLoader
{
    /// <summary>
    /// Utility class
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Normalizes the filename of a resource in a container
        /// </summary>
        /// <param name="filename">resource filename to normalize</param>
        /// <returns>normalized resource filename</returns>
        public static string NormalizeResourceFilename(string filename)
        {
            // Trim trailing '$'
            int indexOfDollar = filename.IndexOf('$');

            if (indexOfDollar != -1)
            {
                unsafe
                {
                    fixed (char* chars = filename)
                    {
                        *(chars + indexOfDollar) = '\x00';
                        filename = new string(chars);
                    }
                }
            }

            return filename;
        }

        /// <summary>
        /// Checks if the given texture is a Divinity compressed texture
        /// </summary>
        /// <param name="textureDataBuffer">texture data buffer</param>
        /// <param name="divinityMagic">divinity magic to check against</param>
        /// <returns>true if it's a Divinity compressed texture, false otherwise</returns>
        public static bool IsDivinityCompressedTexture(byte[] textureDataBuffer, byte[] divinityMagic)
        {
            if (textureDataBuffer.Length < divinityMagic.Length + 8)
            {
                return false;
            }

            unsafe
            {
                fixed (byte* p1 = textureDataBuffer, p2 = divinityMagic)
                {
                    int remainingBytes = divinityMagic.Length;
                    byte* ptr1 = p1;
                    byte* ptr2 = p2;

                    while (remainingBytes-- > 0)
                    {
                        if (*ptr1++ != *ptr2++)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Fast, unsafe method to check if two byte arrays are equal
        /// </summary>
        /// <param name="b1">first byte array</param>
        /// <param name="b2">second byte array</param>
        /// <returns>true if they are equal, false otherwise</returns>
        public static bool ArraysEqual(byte[] b1, byte[] b2)
        {
            unsafe
            {
                if (b1.Length != b2.Length)
                {
                    return false;
                }

                int n = b1.Length;

                fixed (byte* p1 = b1, p2 = b2)
                {
                    byte* ptr1 = p1;
                    byte* ptr2 = p2;

                    while (n-- > 0)
                    {
                        if (*ptr1++ != *ptr2++)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the free disk space information in a disk
        /// </summary>
        /// <param name="lpRootPathName">root path of the disk</param>
        /// <param name="lpSectorsPerCluster">sectors per cluster</param>
        /// <param name="lpBytesPerSector">bytes per sector</param>
        /// <param name="lpNumberOfFreeClusters">number of free clusters</param>
        /// <param name="lpTotalNumberOfClusters">total number of free clusters</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpace(string lpRootPathName,
           out ulong lpSectorsPerCluster,
           out ulong lpBytesPerSector,
           out ulong lpNumberOfFreeClusters,
           out ulong lpTotalNumberOfClusters);

        /// <summary>
        /// Gets the cluster size of the given drive
        /// </summary>
        /// <param name="driveRootPath"></param>
        /// <returns>the cluster size of the given drive, -1 if it couldn't be determined</returns>
        public static int GetClusterSize(string driveRootPath)
        {
            ulong sectorsPerCluster;
            ulong bytesPerSector;
            ulong numberOfFreeClusters;
            ulong totalNumberOfClusters;
            bool result = GetDiskFreeSpace(driveRootPath, out sectorsPerCluster, out bytesPerSector, out numberOfFreeClusters, out totalNumberOfClusters);

            return result ? (int)(sectorsPerCluster * bytesPerSector) : -1;
        }
    }
}
