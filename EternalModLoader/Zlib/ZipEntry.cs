using System;
using System.IO;
using System.Text;

namespace EternalModLoader.Zlib
{
	/// <summary>
	/// Represents an entry in a Zip file
	/// </summary>
	public class ZipEntry
	{
		/// <summary>
		/// The full name of the entry
		/// </summary>
		public string Name = string.Empty;

		/// <summary>
		/// The uncompressed length of the entry
		/// </summary>
		public long UncompressedLength;

		/// <summary>
		/// Whether or not this entry is a directory
		/// </summary>
		public bool IsDirectory;

		/// <summary>
		/// Use UTF8 for name and comment
		/// </summary>
		private bool _utf8Encoding;

		/// <summary>
		/// The file attributes for the zip entry
		/// </summary>
		private FileAttributes _fileAttributes;

		/// <summary>
		/// Initializes a instance of the <see cref="ZipEntry"/> class with the given name
		/// </summary>
		/// <param name="name">The name of the entry stored in the directory of the zip file</param>
		public ZipEntry(string name, bool isDirectory)
		{
			Name = name;
			IsDirectory = isDirectory;
		}

		/// <summary>
		/// Initializes a instance of the <see cref="ZipEntry"/> class with the given name
		/// </summary>
		/// <param name="name">The name of entry stored in the directory of the zip file</param>
		public ZipEntry(string name)
			: this(name, false)
		{

		}

		/// <summary>
		/// Creates a new Zip file entry reading values from a zip file
		/// </summary>
		internal ZipEntry(IntPtr handle)
		{
			var entryInfo = new ZipEntryInfo64();
			int result = ZlibWrapper.ZlibUnzGetCurrentFileInfo64(handle, out entryInfo, null, 0, null, 0, null, 0);

			if (result != 0)
			{
				throw new ZipException($"Could not read entry from zip file \"{Name}\"", result);
			}

			byte[] extraField = new byte[entryInfo.ExtraFieldLength];
			byte[] entryNameBuffer = new byte[entryInfo.FileNameLength];
			byte[] commentBuffer = new byte[entryInfo.CommentLength];

			result = ZlibWrapper.ZlibUnzGetCurrentFileInfo64(handle, out entryInfo,
				entryNameBuffer, (uint)entryNameBuffer.Length,
				extraField, (uint)extraField.Length,
				commentBuffer, (uint)commentBuffer.Length);

			if (result != 0)
			{
				throw new ZipException($"Could not read entry from zip file \"{Name}\"", result);
			}

			_utf8Encoding = (entryInfo.Flag & ZipEntryFlag.UTF8) == ZipEntryFlag.UTF8;
			Encoding encoding = _utf8Encoding ? Encoding.UTF8 : ZlibWrapper.OEMEncoding;

			Name = encoding.GetString(entryNameBuffer);
			UncompressedLength = (long)entryInfo.UncompressedSize;
			_fileAttributes = (FileAttributes)entryInfo.ExternalFileAttributes;
			IsDirectory = (_fileAttributes & FileAttributes.Directory) != 0;
		}
	}
}
