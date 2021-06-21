using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EternalModLoader
{
    public static class KernelWrapper
    {
		public const string KernelLibraryName = "kernel32.dll";

		/// <summary>
		/// GetOEMCP kernel function
		/// </summary>
		/// <returns>Returns the current original equipment manufacturer (OEM) code page identifier for the operating system</returns>
		[DllImport(KernelLibraryName)]
		public static extern uint GetOEMCP();

        /// <summary>
        /// Gets the free disk space information in a disk
        /// </summary>
        /// <param name="lpRootPathName">root path of the disk</param>
        /// <param name="lpSectorsPerCluster">sectors per cluster</param>
        /// <param name="lpBytesPerSector">bytes per sector</param>
        /// <param name="lpNumberOfFreeClusters">number of free clusters</param>
        /// <param name="lpTotalNumberOfClusters">total number of free clusters</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpace(string lpRootPathName,
           out ulong lpSectorsPerCluster,
           out ulong lpBytesPerSector,
           out ulong lpNumberOfFreeClusters,
           out ulong lpTotalNumberOfClusters);

		/// <summary>
		/// LoadLibrary kernel function
		/// </summary>
		/// <param name="dllToLoad">path to the module to load</param>
		/// <returns>handle to the loaded module</returns>
		[DllImport("kernel32.dll")]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		/// <summary>
		/// FreeLibrary kernel function
		/// </summary>
		/// <param name="hModule">handle to the module to unload</param>
		/// <returns>true if the unload was sucessful, false otherwise</returns>
		[DllImport("kernel32.dll")]
		public static extern bool FreeLibrary(IntPtr hModule);
	}
}
