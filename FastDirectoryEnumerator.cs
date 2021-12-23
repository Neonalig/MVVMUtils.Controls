#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System;
using System.Collections;
using System.Linq;

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

using JetBrains.Annotations;

using Microsoft.Win32.SafeHandles;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Contains information about a file returned by the 
/// <see cref="FastDirectoryEnumerator"/> class.
/// </summary>
[Serializable]
public class FileData {
	/// <summary>
	/// Attributes of the file.
	/// </summary>
	public readonly FileAttributes Attributes;

	/// <summary> <c>File</c> creation time. </summary>
	public DateTime CreationTime => CreationTimeUtc.ToLocalTime();

	/// <summary>
	/// <c>File</c> creation time in UTC.
	/// </summary>
	public readonly DateTime CreationTimeUtc;

	/// <summary>
	/// Gets the last access time in local time.
	/// </summary>
	public DateTime LastAccessTime => LastAccessTimeUtc.ToLocalTime();

	/// <summary>
	/// <c>File</c> last access time in UTC.
	/// </summary>
	public readonly DateTime LastAccessTimeUtc;

	/// <summary>
	/// Gets the last access time in local time.
	/// </summary>
	public DateTime LastWriteTime => LastWriteTimeUtc.ToLocalTime();

	/// <summary>
	/// <c>File</c> last write time in UTC
	/// </summary>
	public readonly DateTime LastWriteTimeUtc;

	/// <summary>
	/// Size of the file in bytes
	/// </summary>
	public readonly long Size;

	/// <summary>
	/// Name of the file
	/// </summary>
	public readonly string Name;

	/// <summary>
	/// Full path to the file.
	/// </summary>
	public readonly string Path;

	/// <summary>
	/// Returns a <see cref="string"/> that represents the current <see cref="object"/>.
	/// </summary>
	/// <returns>
	/// A <see cref="string"/> that represents the current <see cref="object"/>.
	/// </returns>
	public override string ToString() => Name;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileData"/> class.
	/// </summary>
	/// <param name="Dir">The directory that the file is stored at</param>
	/// <param name="FindData">WIN32_FIND_DATA structure that this
	/// object wraps.</param>
	internal FileData( string Dir, Win32_Find_Data FindData ) {
		Attributes = FindData.dwFileAttributes;


		CreationTimeUtc = ConvertDateTime(FindData.ftCreationTime_dwHighDateTime,
			FindData.ftCreationTime_dwLowDateTime);

		LastAccessTimeUtc = ConvertDateTime(FindData.ftLastAccessTime_dwHighDateTime,
			FindData.ftLastAccessTime_dwLowDateTime);

		LastWriteTimeUtc = ConvertDateTime(FindData.ftLastWriteTime_dwHighDateTime,
			FindData.ftLastWriteTime_dwLowDateTime);

		Size = CombineHighLowInts(FindData.nFileSizeHigh, FindData.nFileSizeLow);

		Name = FindData.cFileName;
		Path = System.IO.Path.Combine(Dir, FindData.cFileName);
	}

	/// <summary> </summary>
	static long CombineHighLowInts( uint High, uint Low ) => ((long)High << 0x20) | Low;

	/// <summary> </summary>
	static DateTime ConvertDateTime( uint High, uint Low ) {
		long FileTime = CombineHighLowInts(High, Low);
		return DateTime.FromFileTimeUtc(FileTime);
	}
}

/// <summary>
/// Contains information about the file that is found 
/// by the FindFirstFile or FindNextFile functions.
/// </summary>
[Serializable][StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)][BestFitMapping(false)]
internal class Win32_Find_Data {
	/// <summary> </summary>
	public FileAttributes dwFileAttributes;

	/// <summary> </summary>
	public uint ftCreationTime_dwLowDateTime;

	/// <summary> </summary>
	public uint ftCreationTime_dwHighDateTime;

	/// <summary> </summary>
	public uint ftLastAccessTime_dwLowDateTime;

	/// <summary> </summary>
	public uint ftLastAccessTime_dwHighDateTime;

	/// <summary> </summary>
	public uint ftLastWriteTime_dwLowDateTime;

	/// <summary> </summary>
	public uint ftLastWriteTime_dwHighDateTime;

	/// <summary> </summary>
	public uint nFileSizeHigh;

	/// <summary> </summary>
	public uint nFileSizeLow;

	/// <summary> </summary>
	public int dwReserved0;

	/// <summary> </summary>
	public int dwReserved1;

	/// <summary> </summary>
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
	public string cFileName = string.Empty;

	/// <summary> </summary>
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
	public string cAlternateFileName = string.Empty;

	/// <summary>
	/// Returns a <see cref="string"/> that represents the current <see cref="object"/>.
	/// </summary>
	/// <returns>
	/// A <see cref="string"/> that represents the current <see cref="object"/>.
	/// </returns>
	public override string ToString() => "File name=" + cFileName;
}

/// <summary>
/// A fast enumerator of files in a directory. Use it if you need to get attributes for 
/// all files in a directory.
/// <para/>See: <see href="https://www.codeproject.com/Articles/38959/A-Faster-Directory-Enumerator"/>
/// </summary>
/// <remarks>
/// This enumerator is substantially faster than using <see cref="Directory.GetFiles(string)"/>
/// and then creating a new FileInfo object for each path.  Use this version when you 
/// will need to look at the attibutes of each file returned (for example, you need
/// to check each file in a directory to see if it was modified after a specific date).
/// </remarks>
public static class FastDirectoryEnumerator {
	/// <summary>
	/// Gets <see cref="FileData"/> for all the files in a directory.
	/// </summary>
	/// <param name="Path">The path to search.</param>
	/// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and 
	/// allows you to enumerate the files in the given directory.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="Path"/> is a null reference (Nothing in VB)
	/// </exception>
	public static IEnumerable<FileData> EnumerateFiles( string Path ) => EnumerateFiles(Path, "*");

	/// <summary>
	/// Gets <see cref="FileData"/> for all the files in a directory that match a 
	/// specific filter.
	/// </summary>
	/// <param name="Path">The path to search.</param>
	/// <param name="SearchPattern">The search string to match against files in the path.</param>
	/// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and 
	/// allows you to enumerate the files in the given directory.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="Path"/> is a null reference (Nothing in VB)
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// </exception>
	public static IEnumerable<FileData> EnumerateFiles( string Path, string SearchPattern ) => EnumerateFiles(Path, SearchPattern, SearchOption.TopDirectoryOnly);

	/// <summary>
	/// Gets <see cref="FileData"/> for all the files in a directory that 
	/// match a specific filter, optionally including all sub directories.
	/// </summary>
	/// <param name="Path">The path to search.</param>
	/// <param name="SearchPattern">The search string to match against files in the path.</param>
	/// <param name="SearchOption">
	/// One of the SearchOption values that specifies whether the search 
	/// operation should include all subdirectories or only the current directory.
	/// </param>
	/// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and 
	/// allows you to enumerate the files in the given directory.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="Path"/> is a null reference (Nothing in VB)
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// filter is a null reference (Nothing in VB)
	/// </exception>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="SearchOption"/> is not one of the valid values of the
	/// <see cref="SearchOption"/> enumeration.
	/// </exception>
	public static IEnumerable<FileData> EnumerateFiles( string Path, string SearchPattern, SearchOption SearchOption ) {
		if ( Path == null ) { throw new ArgumentNullException(nameof(Path)); }

		if ( SearchPattern == null ) { throw new ArgumentNullException(nameof(SearchPattern)); }

		if ( SearchOption != SearchOption.TopDirectoryOnly && SearchOption != SearchOption.AllDirectories ) { throw new ArgumentOutOfRangeException(nameof(SearchOption)); }

		string FullPath = System.IO.Path.GetFullPath(Path);

		return new FileEnumerable(FullPath, SearchPattern, SearchOption);
	}

	/// <summary>
	/// Gets <see cref="FileData"/> for all the files in a directory that match a 
	/// specific filter.
	/// </summary>
	/// <param name="Path">The path to search.</param>
	/// <param name="SearchPattern">The search string to match against files in the path.</param>
	/// <param name="SearchOption">SearchOption for the search query</param>
	/// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and 
	/// allows you to enumerate the files in the given directory.</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="Path"/> is a null reference (Nothing in VB)
	/// </exception>
	/// <exception cref="ArgumentNullException">
	/// filter is a null reference (Nothing in VB)
	/// </exception>
	public static FileData[] GetFiles( string Path, string SearchPattern, SearchOption SearchOption ) {
		IEnumerable<FileData> E = EnumerateFiles(Path, SearchPattern, SearchOption);
		List<FileData> List = new List<FileData>(E);

		FileData[] Retval = new FileData[List.Count];
		List.CopyTo(Retval);

		return Retval;
	}

	/// <summary>
	/// Provides the implementation of the 
	/// <see cref="T:System.Collections.Generic.IEnumerable`1"/> interface
	/// </summary>
	class FileEnumerable : IEnumerable<FileData> {
		/// <summary> </summary>
		readonly string _M_Path;

		/// <summary> </summary>
		readonly string _M_Filter;

		/// <summary> </summary>
		readonly SearchOption _M_SearchOption;

		/// <summary>
		/// Initializes a new instance of the <see cref="FileEnumerable"/> class.
		/// </summary>
		/// <param name="Path">The path to search.</param>
		/// <param name="Filter">The search string to match against files in the path.</param>
		/// <param name="SearchOption">
		/// One of the SearchOption values that specifies whether the search 
		/// operation should include all subdirectories or only the current directory.
		/// </param>
		public FileEnumerable( string Path, string Filter, SearchOption SearchOption ) {
			_M_Path = Path;
			_M_Filter = Filter;
			_M_SearchOption = SearchOption;
		}

		#region IEnumerable<FileData> Members

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="IEnumerator{T}"/> that can 
		/// be used to iterate through the collection.
		/// </returns>
		public IEnumerator<FileData> GetEnumerator() => new FileEnumerator(_M_Path, _M_Filter, _M_SearchOption);

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be 
		/// used to iterate through the collection.
		/// </returns>
		IEnumerator IEnumerable.GetEnumerator() => new FileEnumerator(_M_Path, _M_Filter, _M_SearchOption);

		#endregion

	}

	/// <summary>
	/// Wraps a FindFirstFile handle.
	/// </summary>
	[UsedImplicitly]
	sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid {
#pragma warning disable SYSLIB0004 // Type or member is obsolete
#pragma warning disable SYSLIB0003
#pragma warning disable 618

		/// <summary> </summary>
		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
		[DllImport("kernel32.dll")]
		static extern bool FindClose( IntPtr Handle );

		/// <summary>
		/// Initializes a new instance of the <see cref="SafeFindHandle"/> class.
		/// </summary>
		[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
		internal SafeFindHandle()
			: base(true) { }

#pragma warning restore 618
#pragma warning restore SYSLIB0003
#pragma warning restore SYSLIB0004 // Type or member is obsolete

		/// <summary>
		/// When overridden in a derived class, executes the code required to free the handle.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the handle is released successfully; otherwise, in the 
		/// event of a catastrophic failure, <see langword="false"/>. If so, it 
		/// generates a releaseHandleFailed MDA Managed Debugging Assistant.
		/// </returns>
		protected override bool ReleaseHandle() => FindClose(handle);
	}

	/// <summary>
	/// Provides the implementation of the 
	/// <see cref="IEnumerator{T}"/> interface
	/// </summary>
	[SuppressUnmanagedCodeSecurity]
	class FileEnumerator : IEnumerator<FileData> {
		/// <summary> </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern SafeFindHandle FindFirstFile( string FileName,
			[In][Out] Win32_Find_Data Data );

		/// <summary> </summary>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		static extern bool FindNextFile( SafeFindHandle HndFindFile,
			[In][Out][MarshalAs(UnmanagedType.LPStruct)]
			Win32_Find_Data LpFindFileData );

		/// <summary>
		/// Hold context information about where we current are in the directory search.
		/// </summary>
		class SearchContext {
			/// <summary> </summary>
			public readonly string Path;

			/// <summary> </summary>
			public Stack<string>? SubdirectoriesToProcess;

			/// <summary> </summary>
			public SearchContext( string PATH ) => Path = PATH;
		}

#pragma warning disable IDE0044 // Add readonly modifier
		// ReSharper disable FieldCanBeMadeReadOnly.Local
		/// <summary> </summary>
		string _M_Path;

		/// <summary> </summary>
		string _M_Filter;

		/// <summary> </summary>
		SearchOption _M_SearchOption;

		/// <summary> </summary>
		Stack<SearchContext> _M_ContextStack;

		/// <summary> </summary>
		SearchContext _M_CurrentContext;

		/// <summary> </summary>
		SafeFindHandle? _M_HndFindFile;

		/// <summary> </summary>
		Win32_Find_Data _M_Win_Find_Data = new Win32_Find_Data();
		// ReSharper restore FieldCanBeMadeReadOnly.Local
#pragma warning restore IDE0044 // Add readonly modifier

		/// <summary>
		/// Initializes a new instance of the <see cref="FileEnumerator"/> class.
		/// </summary>
		/// <param name="Path">The path to search.</param>
		/// <param name="Filter">The search string to match against files in the path.</param>
		/// <param name="SearchOption">
		/// One of the SearchOption values that specifies whether the search 
		/// operation should include all subdirectories or only the current directory.
		/// </param>
		public FileEnumerator( string Path, string Filter, SearchOption SearchOption ) {
			_M_Path = Path;
			_M_Filter = Filter;
			_M_SearchOption = SearchOption;
			_M_CurrentContext = new SearchContext(Path);

			_M_HndFindFile = null;
			// ReSharper disable once RedundantSuppressNullableWarningExpression
			_M_ContextStack = (_M_SearchOption == SearchOption.AllDirectories ? new Stack<SearchContext>() : default!)!;
		}

		#region IEnumerator<FileData> Members

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The element in the collection at the current position of the enumerator.
		/// </returns>
		public FileData Current => new FileData(_M_Path, _M_Win_Find_Data);

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, 
		/// or resetting unmanaged resources.
		/// </summary>
		public void Dispose() => _M_HndFindFile?.Dispose();

		#endregion

		#region IEnumerator Members

		/// <summary>
		/// Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The element in the collection at the current position of the enumerator.
		/// </returns>
		object IEnumerator.Current => new FileData(_M_Path, _M_Win_Find_Data);

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the enumerator was successfully advanced to the next element; 
		/// <see langword="false"/> if the enumerator has passed the end of the collection.
		/// </returns>
		/// <exception cref="InvalidOperationException">
		/// The collection was modified after the enumerator was created.
		/// </exception>
		public bool MoveNext() {
			bool Retval = false;

			//If the handle is null, this is first call to MoveNext in the current 
			// directory.  In that case, start a new search.
			if ( _M_CurrentContext.SubdirectoriesToProcess is null ) {
				if ( _M_HndFindFile is null ) {

#pragma warning disable SYSLIB0003 // Type or member is obsolete
#pragma warning disable 618
					new FileIOPermission(FileIOPermissionAccess.PathDiscovery, _M_Path).Demand();
#pragma warning restore SYSLIB0003 // Type or member is obsolete
#pragma warning restore 618

					string SearchPath = Path.Combine(_M_Path, _M_Filter);
					_M_HndFindFile = FindFirstFile(SearchPath, _M_Win_Find_Data);
					Retval = !_M_HndFindFile.IsInvalid;
				} else {
					//Otherwise, find the next item.
					Retval = FindNextFile(_M_HndFindFile, _M_Win_Find_Data);
				}
			}

			//If the call to FindNextFile or FindFirstFile succeeded...
			if ( Retval ) {
				if ( (_M_Win_Find_Data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory ) {
					//Ignore folders for now.   We call MoveNext recursively here to 
					// move to the next item that FindNextFile will return.
					// ReSharper disable once TailRecursiveCall
					return MoveNext();
				}
			} else if ( _M_SearchOption == SearchOption.AllDirectories ) {
				//SearchContext context = new SearchContext(m_hndFindFile, m_path);
				//m_contextStack.Push(context);
				//m_path = Path.Combine(m_path, m_win_find_data.cFileName);
				//m_hndFindFile = null;

				if ( _M_CurrentContext.SubdirectoriesToProcess == null ) {
					string[] SubDirectories = Directory.GetDirectories(_M_Path);
					_M_CurrentContext.SubdirectoriesToProcess = new Stack<string>(SubDirectories);
				}

				if ( _M_CurrentContext.SubdirectoriesToProcess.Count > 0 ) {
					string SubDir = _M_CurrentContext.SubdirectoriesToProcess.Pop();

					_M_ContextStack.Push(_M_CurrentContext);
					_M_Path = SubDir;
					_M_HndFindFile = null;
					_M_CurrentContext = new SearchContext(_M_Path);
					// ReSharper disable once TailRecursiveCall
					return MoveNext();
				}

				//If there are no more files in this directory and we are 
				// in a sub directory, pop back up to the parent directory and
				// continue the search from there.
				if ( _M_ContextStack.Count > 0 ) {
					_M_CurrentContext = _M_ContextStack.Pop();
					_M_Path = _M_CurrentContext.Path;
					if ( _M_HndFindFile != null ) {
						_M_HndFindFile.Close();
						_M_HndFindFile = null;
					}

					// ReSharper disable once TailRecursiveCall
					return MoveNext();
				}
			}

			return Retval;
		}

		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		/// <exception cref="InvalidOperationException">
		/// The collection was modified after the enumerator was created.
		/// </exception>
		public void Reset() => _M_HndFindFile = null;

		#endregion

	}
}

/// <summary>
/// General extension methods for the <see cref="FastDirectoryEnumerator"/> class.
/// </summary>
public static class FastDirectoryEnumerator_Extensions {
	/// <inheritdoc cref="FastDirectoryEnumerator.EnumerateFiles(string)"/>
	public static IEnumerable<FileData> FastEnumerateFiles( this DirectoryInfo DI ) => FastDirectoryEnumerator.EnumerateFiles(DI.FullName);

	/// <inheritdoc cref="FastDirectoryEnumerator.EnumerateFiles(string,string)"/>
	public static IEnumerable<FileData> FastEnumerateFiles( this DirectoryInfo DI, string SearchPattern ) => FastDirectoryEnumerator.EnumerateFiles(DI.FullName, SearchPattern);

	/// <inheritdoc cref="FastDirectoryEnumerator.EnumerateFiles(string,string,SearchOption)"/>
	public static IEnumerable<FileData> FastEnumerateFiles( this DirectoryInfo DI, string SearchPattern, SearchOption SearchOption ) => FastDirectoryEnumerator.EnumerateFiles(DI.FullName, SearchPattern, SearchOption);

	/// <inheritdoc cref="FastDirectoryEnumerator.GetFiles(string,string,SearchOption)"/>
	public static FileData[] FastGetFiles( this DirectoryInfo DI, string SearchPattern, SearchOption SearchOption ) => FastDirectoryEnumerator.GetFiles(DI.FullName, SearchPattern, SearchOption);

	/// <summary>
	/// Creates <see cref="FileInfo"/> instances from the given <see cref="FileData"/> paths.
	/// </summary>
	/// <param name="Files">The files to iterate through.</param>
	/// <returns>An enumerable of <see cref="FileInfo"/> instances.</returns>
	public static IEnumerable<FileInfo> GetInfo( this IEnumerable<FileData> Files ) {
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach ( FileData FD in Files ) {
			yield return new FileInfo(FD.Path);
		}
	}

	/// <summary>
	/// Counts the number of files in the given directory.
	/// </summary>
	/// <param name="DI">The parent directory.</param>
	/// <returns>An integer count of files.</returns>
	public static int CountFiles( this DirectoryInfo DI ) => FastDirectoryEnumerator.EnumerateFiles(DI.FullName).Count();

	/// <summary>
	/// <see href="http://www.pinvoke.net/default.aspx/shell32/GetFinalPathNameByHandle.html"/>
	/// </summary>
	[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
	[SuppressMessage("Globalization", "CA2101:Specify marshalling for P/Invoke string arguments")][SuppressMessage("Performance", "CA1838:Avoid 'StringBuilder' for P/Invokes")]
	static extern uint GetFinalPathNameByHandle( SafeFileHandle HFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder LpszFilePath, uint CchFilePath, uint DWFlags );

	/// <summary> </summary>
	const uint File_Name_Normalized = 0x0;

	/// <summary>
	/// Retrieves the final (case-sensitive) file path by the given handle.
	/// </summary>
	/// <param name="FileHandle">The handle to retrieve the path from.</param>
	/// <returns>The final file path.</returns>
	public static string GetFinalPathNameByHandle( this SafeFileHandle FileHandle ) {
		StringBuilder OutPath = new StringBuilder(1024);

		uint Size = GetFinalPathNameByHandle(FileHandle, OutPath, (uint)OutPath.Capacity, File_Name_Normalized);
		if ( Size == 0 || Size > OutPath.Capacity ) {
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		// may be prefixed with \\?\, which we don't want
		return OutPath[0] == '\\' && OutPath[1] == '\\' && OutPath[2] == '?' && OutPath[3] == '\\'
			? OutPath.ToString(4, OutPath.Length - 4)
			: OutPath.ToString();
	}

	/// <summary/>
	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	[SuppressMessage("Globalization", "CA2101:Specify marshalling for P/Invoke string arguments")]
	static extern SafeFileHandle? CreateFile(
		[MarshalAs(UnmanagedType.LPTStr)] string Filename,
		[MarshalAs(UnmanagedType.U4)] FileAccess Access,
		[MarshalAs(UnmanagedType.U4)] FileShare Share,
		IntPtr SecurityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
		[MarshalAs(UnmanagedType.U4)] FileMode CreationDisposition,
		[MarshalAs(UnmanagedType.U4)] FileAttributes FlagsAndAttributes,
		IntPtr TemplateFile );

	/// <summary/>
	const uint File_Flag_Backup_Semantics = 0x02000000;

	/// <summary>
	/// Gets the final (case-sensitive) path name from the given path.
	/// </summary>
	/// <param name="DirtyPath">The path to retrieve the final name of.</param>
	/// <returns>The final (case-sensitive) path.</returns>
	public static string GetFinalPathName( string DirtyPath ) {
		// use 0 for access so we can avoid error on our metadata-only query (see dwDesiredAccess docs on CreateFile)
		// use FILE_FLAG_BACKUP_SEMANTICS for attributes so we can operate on directories (see Directories in remarks section for CreateFile docs)

		using ( SafeFileHandle? DirectoryHandle = CreateFile(
			       DirtyPath, 0, FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, FileMode.Open,
			       (FileAttributes)File_Flag_Backup_Semantics, IntPtr.Zero) ) {
			return DirectoryHandle?.IsInvalid ?? true
				?                 throw new Win32Exception(Marshal.GetLastWin32Error())
				: GetFinalPathNameByHandle(DirectoryHandle);
		}
	}

	/// <summary>
	/// Gets the final (case-sensitive) path name from the given path.
	/// </summary>
	/// <param name="FSI">The path to retrieve the final name of.</param>
	/// <returns>The final (case-sensitive) path.</returns>
	public static string GetCaseSensitiveFullName( this FileSystemInfo FSI ) {
		try {
			return GetFinalPathName(FSI.FullName);
		} catch ( Exception E ) {
			Debug.WriteLine($"Caught {E.GetType().Name} : {E.Message}", "WARNING");
			return FSI.FullName;
		}
	}
}