using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace EternalModLoader.Zlib
{
	/// <summary>
	/// ZlibWrapper class
	/// Zlib support methods for uncompressing zip files
	/// </summary>
	internal static class ZlibWrapper
	{
		/// <summary>
		/// OEMEncoding, returned by <see cref="ZlibUnzOpenCurrentFile"/>
		/// </summary>
		public static Encoding OEMEncoding = Encoding.GetEncoding((int)KernelWrapper.GetOEMCP());

		/// <summary>
		/// Sets the Unicode encoding to open a zip file
		/// </summary>
		/// <param name="openUnicode">boolean, 1 to open as unicode</param>
		/// <returns></returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "setOpenUnicode", ExactSpelling = true)]
		static extern int ZlibSetOpenUnicode(int openUnicode);

		/// <summary>
		/// Opens a zip file for reading
		/// </summary>
		/// <param name="fileName">the path of the zip to open</param>
		/// <returns>
		/// A handle usable with other functions of the ZipLib class
		/// Otherwise IntPtr.Zero if the zip file could not be opened (file doesn't exist or is not valid)
		/// </returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzOpen64", ExactSpelling = true, CharSet = CharSet.Unicode)]
		static extern IntPtr ZlibUnzOpen64(string fileName);

		/// <summary>
		/// Opens a zip file for reading with Unicode encoding
		/// </summary>
		/// <param name="fileName">the path of the zip to open</param>
		/// <returns>
		/// A handle usable with other functions of the ZipLib class
		/// Otherwise IntPtr.Zero if the zip file could not be opened (file doesn't exist or is not valid)
		/// </returns>
		public static IntPtr ZlibUnzOpen(string fileName)
		{
			ZlibSetOpenUnicode(1);

			return ZlibUnzOpen64(fileName);
		}

		/// <summary>
		/// Closes a zip file opened with <see cref="ZlibUnzOpen"/>
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <remarks>
		/// If there are files inside the zip file opened with <see cref="ZlibUnzOpenCurrentFile"/>
		/// these files must be closed with <see cref="ZlibUnzCloseCurrentFile"/> before calling ZlibUnzClose
		/// </remarks>
		/// <returns>Zero if there was no error, otherwise a value less than zero. See <see cref="ZipReturnCode"/></returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzClose", ExactSpelling = true)]
		public static extern int ZlibUnzClose(IntPtr handle);

		/// <summary>
		/// Get global information about the zip file
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <param name="globalInfoPtr">An address of a <see cref="ZipFileInfo"/> struct to hold the information. No preparation of the structure is needed</param>
		/// <returns>Zero if there was no error, otherwise a value less than zero. See <see cref="ZipReturnCode"/></returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzGetGlobalInfo", ExactSpelling = true)]
		public static extern int ZlibUnzGetGlobalInfo(IntPtr handle, out ZipFileInfo globalInfoPtr);

		/// <summary>
		/// Get the comment associated with the entire zip file
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <param name="commentBuffer">The buffer to hold the comment</param>
		/// <param name="commentBufferLength">The length of the buffer in bytes (8 bit characters)</param>
		/// <returns>
		/// The number of characters in the comment if there was no error
		/// Otherwise a value less than zero. See <see cref="ZipReturnCode"/>
		/// </returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzGetGlobalComment", ExactSpelling = true)]
		public static extern int ZlibUnzGetGlobalComment(IntPtr handle, byte[] commentBuffer, uint commentBufferLength);

		/// <summary>
		/// Set the current file of the zip file to the first file
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <returns> Zero if there was no error, otherwise a value less than zero. See <see cref="ZipReturnCode"/></returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzGoToFirstFile", ExactSpelling = true)]
		public static extern int ZlibUnzGoToFirstFile(IntPtr handle);

		/// <summary>
		/// Set the current file of the zip file to the next file
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <returns>Zero if there was no error, otherwise <see cref="ZipReturnCode.EndOfListOfFile"/> if there are no more entries</returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzGoToNextFile", ExactSpelling = true)]
		public static extern int ZlibUnzGoToNextFile(IntPtr handle);

		/// <summary>
		/// Get information about the current entry in the zip file
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <param name="entryInfoPtr">A ZipEntryInfo struct to hold information about the entry or null</param>
		/// <param name="entryNameBuffer">An array of sbyte characters to hold the entry name or null</param>
		/// <param name="entryNameBufferLength">The length of the entryNameBuffer in bytes</param>
		/// <param name="extraField">An array to hold the extra field data for the entry or null</param>
		/// <param name="extraFieldLength">The length of the extraField array in bytes</param>
		/// <param name="commentBuffer">An array of sbyte characters to hold the entry name or null</param>
		/// <param name="commentBufferLength">The length of the commentBuffer in bytes</param>
		/// <remarks>
		/// If entryInfoPtr is not null the structure will contain information about the current file
		/// If entryNameBuffer is not null the name of the entry will be copied into it
		/// If extraField is not null the extra field data of the entry will be copied into it
		/// If commentBuffer is not null the comment of the entry will be copied into it
		/// </remarks>
		/// <returns>Zero if there was no error, otherwise a value less than zero. See <see cref="ZipReturnCode"/></returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzGetCurrentFileInfo64", ExactSpelling = true)]
		public static extern int ZlibUnzGetCurrentFileInfo64(
			IntPtr handle,
			out ZipEntryInfo64 entryInfoPtr,
			byte[] entryNameBuffer,
			uint entryNameBufferLength,
			byte[] extraField,
			uint extraFieldLength,
			byte[] commentBuffer,
			uint commentBufferLength);

		/// <summary>
		/// Open the zip file entry for reading
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <returns>Zero if there was no error, otherwise a value less than zero. See <see cref="ZipReturnCode"/></returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzOpenCurrentFile", ExactSpelling = true)]
		public static extern int ZlibUnzOpenCurrentFile(IntPtr handle);

		/// <summary>
		/// Close the file entry opened by <see cref="ZlibUnzOpenCurrentFile"/>
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <returns>
		/// Zero if there was no error
		/// <see cref="ZipReturnCode.CrcError"/> if the file was read but the Crc does not match
		/// Otherwise a value different than zero. See <see cref="ZipReturnCode"/>
		/// </returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzCloseCurrentFile", ExactSpelling = true)]
		public static extern int ZlibUnzCloseCurrentFile(IntPtr handle);

		/// <summary>
		/// Read bytes from the current zip file entry
		/// </summary>
		/// <param name="handle">The zip file handle opened by <see cref="ZlibUnzOpenCurrentFile"/></param>
		/// <param name="buffer">Buffer to store the uncompressed data into</param>
		/// <param name="count">Number of bytes to write from <paramref name="buffer"/></param>
		/// <returns>
		/// The number of byte copied if somes bytes are copied
		/// Zero if the end of file was reached
		/// Less than zero with error code if there is an error. See <see cref="ZipReturnCode"/> for a list of possible error codes
		/// </returns>
		[DllImport(DllLoader.ZlibDllName, EntryPoint = "unzReadCurrentFile", ExactSpelling = true)]
		public static extern int ZlibUnzReadCurrentFile(IntPtr handle, IntPtr buffer, uint count);
	}

	/// <summary>
	/// Zip entry flags class
	/// </summary>
	internal static class ZipEntryFlag
	{
		/// <summary>
		/// 1 << 11
		/// </summary>
		internal const uint UTF8 = 0x800;
	}

	/// <summary>
	/// Global information about the zip file
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct ZipFileInfo
	{
		/// <summary>
		/// The number of entries in the directory
		/// </summary>
		public UInt32 EntryCount;

		/// <summary>
		/// Length of zip file comment in bytes (8 bit characters)
		/// </summary>
		public UInt32 CommentLength;
	}

	/// <summary>
	/// Custom ZipLib date time structure
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct ZipDateTimeInfo
	{
		/// <summary>
		/// Seconds after the minute - [0,59]
		/// </summary>
		public UInt32 Seconds;

		/// <summary>
		/// Minutes after the hour - [0,59]
		/// </summary>
		public UInt32 Minutes;

		/// <summary>
		/// Hours since midnight - [0,23]
		/// </summary>
		public UInt32 Hours;

		/// <summary>
		/// Day of the month - [1,31]
		/// </summary>
		public UInt32 Day;

		/// <summary>
		/// Months since January - [0,11]
		/// </summary>
		public UInt32 Month;

		/// <summary>
		/// Years - [1980..2044]
		/// </summary>
		public UInt32 Year;

		/// <summary>
		/// Implicit conversion from DateTime to ZipDateTimeInfo
		/// </summary>
		/// <param name="date">DateTime date</param>
		public static implicit operator ZipDateTimeInfo(DateTime date)
		{
			ZipDateTimeInfo d;
			d.Seconds = (uint)date.Second;
			d.Minutes = (uint)date.Minute;
			d.Hours = (uint)date.Hour;
			d.Day = (uint)date.Day;
			d.Month = (uint)date.Month - 1;
			d.Year = (uint)date.Year;

			return d;
		}

		/// <summary>
		/// Implicit conversion from DateTime to ZipDateTimeInfo
		/// </summary>
		/// <param name="date">ZipDateTimeInfo date</param>
		public static implicit operator DateTime(ZipDateTimeInfo date)
		{
			DateTime dt = new DateTime(
				(int)date.Year,
				(int)date.Month + 1,
				(int)date.Day,
				(int)date.Hours,
				(int)date.Minutes,
				(int)date.Seconds);

			return dt;
		}
	}

	/// <summary>
	/// Information stored in zip file directory about an entry
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	internal struct ZipEntryInfo64
	{
		/// <summary>
		/// Version made by (2 bytes)
		/// </summary>
		public UInt32 Version;

		/// <summary>
		/// Version needed to extract (2 bytes)
		/// </summary>
		public UInt32 VersionNeeded;

		/// <summary>
		/// General purpose bit flag (2 bytes)
		/// </summary>
		public UInt32 Flag;

		/// <summary>
		/// Compression method (2 bytes)
		/// </summary>
		public UInt32 CompressionMethod;

		/// <summary>
		/// Last mod file date in Dos fmt (4 bytes)
		/// </summary>
		public UInt32 DosDate;

		/// <summary>
		/// Crc-32 (4 bytes)
		/// </summary>
		public UInt32 Crc;

		/// <summary>
		/// Compressed size (8 bytes)
		/// </summary>
		public UInt64 CompressedSize;

		/// <summary>
		/// Uncompressed size (8 bytes)
		/// </summary>
		public UInt64 UncompressedSize;

		/// <summary>
		/// Filename length (2 bytes)
		/// </summary>
		public UInt32 FileNameLength;

		/// <summary>
		/// Extra field length (2 bytes)
		/// </summary>
		public UInt32 ExtraFieldLength;

		/// <summary>
		/// File comment length (2 bytes)
		/// </summary>
		public UInt32 CommentLength;

		/// <summary>
		/// Disk number start (2 bytes)
		/// </summary>
		public UInt32 DiskStartNumber;

		/// <summary>
		/// Internal file attributes (2 bytes)
		/// </summary>
		public UInt32 InternalFileAttributes;

		/// <summary>
		/// External file attributes (4 bytes)
		/// </summary>
		public UInt32 ExternalFileAttributes;

		/// <summary>
		/// File modification date of entry
		/// </summary>
		public ZipDateTimeInfo ZipDateTime;
	}

	/// <summary>
	/// List of possible return codes
	/// </summary>
	internal static class ZipReturnCode
	{
		/// <summary>
		/// No error
		/// </summary>
		internal const int Ok = 0;

		/// <summary>
		/// Unknown error
		/// </summary>
		internal const int Error = -1;

		/// <summary>
		/// Last entry in directory reached
		/// </summary>
		internal const int EndOfListOfFile = -100;

		/// <summary>
		/// Parameter error
		/// </summary>
		internal const int ParameterError = -102;

		/// <summary>
		/// Zip file is invalid
		/// </summary>
		internal const int BadZipFile = -103;

		/// <summary>
		/// Internal program error
		/// </summary>
		internal const int InternalError = -104;

		/// <summary>
		/// Crc values do not match
		/// </summary>
		internal const int CrcError = -105;

		/// <summary>
		/// Returns an error message for the specified return code
		/// </summary>
		/// <param name="returnCode">return code</param>
		/// <returns>an error message for the specified return code</returns>
		public static string GetMessage(int returnCode)
		{
			switch (returnCode)
			{
				case Ok:
					return "No error";
				case Error:
					return "Unknown error";
				case EndOfListOfFile:
					return "Last entry in directory reached";
				case ParameterError:
					return "Parameter error";
				case BadZipFile:
					return "Zip file is invalid";
				case InternalError:
					return "Internal program error";
				case CrcError:
					return "Crc values do not match";
				default:
					return $"Unknown error: {returnCode}";
			}
		}
	}

	/// <summary>
	/// Thrown whenever an error occurs during a Zlib operation
	/// </summary>
	[Serializable]
	public class ZipException : ApplicationException
	{
		/// <summary>
		/// Constructs an exception with no descriptive information
		/// </summary>
		public ZipException()
			: base()
		{

		}

		/// <summary>
		/// Constructs an exception with a descriptive message
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception</param>
		public ZipException(string message)
			: base(message)
		{

		}

		/// <summary>
		/// Constructs an exception with a descriptive message and error code
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception</param>
		/// <param name="errorCode">The error code for the exception reason</param>
		public ZipException(string message, int errorCode)
			: base($"{message} (${ZipReturnCode.GetMessage(errorCode)})")
		{

		}

		/// <summary>
		/// Constructs an exception with a descriptive message and a reference
		/// to the instance of the Exception that is the root cause of the this exception
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception</param>
		/// <param name="innerException">
		/// An instance of Exception that is the cause of the current Exception.
		/// If <paramref name="innerException"/> is non-null, then the current Exception is raised in a catch block handling innerException
		/// </param>
		public ZipException(string message, Exception innerException)
			: base(message, innerException)
		{

		}

		/// <summary>
		/// Initializes a new instance of the BuildException class with serialized data
		/// </summary>
		/// <param name="info">The object that holds the serialized object data</param>
		/// <param name="context">The contextual information about the source or destination</param>
		public ZipException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{

		}
	}
}
