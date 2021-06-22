using System;
using System.Runtime.InteropServices;

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
        [DllImport(KernelLibraryName, CharSet = CharSet.Auto)]
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
		[DllImport(KernelLibraryName)]
		public static extern IntPtr LoadLibrary(string dllToLoad);

		/// <summary>
		/// FreeLibrary kernel function
		/// </summary>
		/// <param name="hModule">handle to the module to unload</param>
		/// <returns>true if the unload was sucessful, false otherwise</returns>
		[DllImport(KernelLibraryName)]
		public static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Sets the console mode
        /// </summary>
        /// <param name="hConsoleHandle">handle to the console</param>
        /// <param name="mode">mode to set</param>
        /// <returns>true when successful, false otherwise</returns>
        [DllImport(KernelLibraryName)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, int mode);

        /// <summary>
        /// Gets the console mode
        /// </summary>
        /// <param name="hConsoleHandle">handle to the console</param>
        /// <param name="mode">output integer where the mode will be saved to</param>
        /// <returns>true when successful, false otherwise</returns>
        [DllImport(KernelLibraryName)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int mode);

        /// <summary>
        /// Gets the specified standard output handle
        /// </summary>
        /// <param name="handle">id of the handle to get</param>
        /// <returns>a handle for the specified standard output</returns>
        [DllImport(KernelLibraryName)]
        public static extern IntPtr GetStdHandle(int handle);
    }
}
