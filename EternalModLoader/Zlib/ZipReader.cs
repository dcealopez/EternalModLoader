using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EternalModLoader.Zlib
{
	/// <summary>
	/// ZipReader class
	/// </summary>
	public class ZipReader : IEnumerable<ZipEntry>, IDisposable
	{
		/// <summary>
		/// ZipFile handle to read data from
		/// </summary>
		private IntPtr ZipFileHandle = IntPtr.Zero;

		/// <summary>
		/// Name of zip file
		/// </summary>
		public string FileName = null;

		/// <summary>
		/// Current zip entry open for reading
		/// </summary>
		private ZipEntry CurrentZipEntry = null;

		/// <summary>
		/// Initializes a instance of the <see cref="ZipReader"/> class for reading the zip file with the given name
		/// </summary>
		/// <param name="fileName">The path to the zip file that will be read</param>
		public ZipReader(string fileName)
		{
			FileName = fileName;
			ZipFileHandle = ZlibWrapper.ZlibUnzOpen(fileName);

			if (ZipFileHandle == IntPtr.Zero)
			{
				throw new ZipException(string.Format("Could not open zip file '{0}'", fileName));
			}
		}

		/// <summary>
		/// Cleans up the resources used by this zip file
		/// </summary>
		~ZipReader()
		{
			CloseFile();
		}

		/// <summary>
		/// Dispose function to close the Zip file and release the allocated resources
		/// </summary>
		/// <remarks>Dispose is synonym for Close</remarks>
		void IDisposable.Dispose()
		{
			Close();
		}

		/// <summary>
		/// Closes the zip file and releases any resources
		/// </summary>
		public void Close()
		{
			// Free unmanaged resources
			CloseFile();

			// Request the system not call the finalizer method for this object
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Gets the enumerator for the Zip entries inside the Zip file
		/// </summary>
		/// <returns></returns>
		public IEnumerator<ZipEntry> GetEnumerator()
		{
			if (CurrentZipEntry != null)
			{
				throw new InvalidOperationException("Entry already open/enumeration already in progress");
			}

			return new ZipEntryEnumerator(this);
		}

		/// <summary>
		/// Gets the enumerator for the zip entries
		/// </summary>
		/// <returns>the enumerator for the zip entries</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Advances the enumerator to the next element of the collection
		/// </summary>
		/// <summary>Sets <see cref="CurrentZipEntry"/> to the next zip entry</summary>
		/// <returns>true if the next entry is not null, otherwise false</returns>
		private bool MoveNext()
		{
			int result;

			if (CurrentZipEntry == null)
			{
				result = ZlibWrapper.ZlibUnzGoToFirstFile(ZipFileHandle);
			}
			else
			{
				CloseCurrentEntry();
				result = ZlibWrapper.ZlibUnzGoToNextFile(ZipFileHandle);
			}

			if (result == ZipReturnCode.EndOfListOfFile)
			{
				// No more entries
				CurrentZipEntry = null;
			}
			else if (result < 0)
			{
				throw new ZipException("MoveNext failed.", result);
			}
			else
			{
				// Entry found,m open it
				OpenCurrentEntry();
			}

			return CurrentZipEntry != null;
		}

		/// <summary>
		/// Move to just before the first entry in the zip directory
		/// </summary>
		private void Reset()
		{
			CloseCurrentEntry();
		}

		/// <summary>
		/// Closes the current Zip entry
		/// </summary>
		private void CloseCurrentEntry()
		{
			if (CurrentZipEntry == null)
			{
				return;
			}

			int result = ZlibWrapper.ZlibUnzCloseCurrentFile(ZipFileHandle);

			if (result < 0)
			{
				throw new ZipException("Could not close zip entry.", result);
			}

			CurrentZipEntry = null;
		}

		/// <summary>
		/// Opens the current selected Zip entry
		/// </summary>
		private void OpenCurrentEntry()
		{
			CurrentZipEntry = new ZipEntry(ZipFileHandle);
			int result = ZlibWrapper.ZlibUnzOpenCurrentFile(ZipFileHandle);

			if (result < 0)
			{
				CurrentZipEntry = null;
				throw new ZipException("Could not open entry for reading.", result);
			}
		}

		/// <summary>
		/// Reads the current entry into the specified memory stream
		/// </summary>
		/// <remarks>
		/// The data is put directly into the memory stream buffer
		/// No write operations are performed on the memory stream
		/// </remarks>
		/// <param name="memoryStream">memory stream</param>
		public void ReadCurrentEntry(MemoryStream memoryStream)
		{
			if (memoryStream.Capacity < CurrentZipEntry.UncompressedLength)
			{
				throw new ArgumentException("Memory stream doesn't have enough capacity to hold the uncompressed zip entry data");
			}

			byte[] memBuffer = memoryStream.GetBuffer();
			int bytesRead = 0;
			int offset = 0;

			unsafe
			{
				fixed (byte* p = memBuffer)
				{
					for (; ; )
					{
						bytesRead = ZlibWrapper.ZlibUnzReadCurrentFile(ZipFileHandle, (IntPtr)p + offset, 4096);
						offset += bytesRead;

						if (bytesRead <= 0)
						{
							break;
						}
					}
				}
			}
		}

		/// <summary>
		/// Closes the Zip file and releases the unmanaged resources
		/// </summary>
		private void CloseFile()
		{
			if (ZipFileHandle == IntPtr.Zero)
			{
				return;
			}

			try
			{
				CloseCurrentEntry();
			}
			finally
			{
				int result = ZlibWrapper.ZlibUnzClose(ZipFileHandle);

				if (result < 0)
				{
					throw new ZipException("Could not close zip file.", result);
				}

				ZipFileHandle = IntPtr.Zero;
			}
		}

		/// <summary>
		/// Zip entry enumerator class
		/// </summary>
		private class ZipEntryEnumerator : IEnumerator<ZipEntry>
		{
			/// <summary>
			/// Zip reader for the entries
			/// </summary>
			ZipReader ZipReader;

			/// <summary>
			/// Construtor for a Zip entry enumerator
			/// </summary>
			/// <param name="zipReader">ZipReader for the zip entries</param>
			public ZipEntryEnumerator(ZipReader zipReader)
			{
				ZipReader = zipReader;
			}

			/// <summary>
			/// Gets the current ZipEntry
			/// </summary>
			public ZipEntry Current
			{
				get
				{
					return ZipReader.CurrentZipEntry;
				}
			}

			/// <summary>
			/// Closes and releases the unmanaged resources for the current entry
			/// </summary>
			public void Dispose()
			{
				ZipReader.CloseCurrentEntry();
			}

			/// <summary>
			/// Gets the current ZipEntry
			/// </summary>
			object IEnumerator.Current
			{
				get
				{
					return Current;
				}
			}

			/// <summary>
			/// Moves the enumerator to the next ZipEntry
			/// </summary>
			/// <returns>true if an entry was found, false otherwise</returns>
			public bool MoveNext()
			{
				return ZipReader.MoveNext();
			}

			/// <summary>
			/// Resets the enumerator back to the first ZipEntry
			/// </summary>
			public void Reset()
			{
				ZipReader.Reset();
			}
		}
	}
}
