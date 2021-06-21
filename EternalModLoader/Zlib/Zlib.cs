using System;

namespace EternalModLoader.Zlib
{
	/// <summary>
	/// DllLoader class
	/// </summary>
	public static class DllLoader
	{
		/// <summary>
		/// The name of the Zlib Dll
		/// </summary>
		public const string ZlibDllName = "zlib64.dll";

		/// <summary>
		/// Unloads the Zlib module
		/// </summary>
		public static void UnloadZlibDll()
		{
			// Load the library again and then free it twice to reset the reference counter to zero
			IntPtr dllHandle = KernelWrapper.LoadLibrary(ZlibDllName);

			KernelWrapper.FreeLibrary(dllHandle);
			KernelWrapper.FreeLibrary(dllHandle);
		}
	}
}
