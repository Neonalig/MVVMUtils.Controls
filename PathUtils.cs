#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System;
using System.Linq;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using Ookii.Dialogs.Wpf;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// General FileSystem utilities and extensions.
/// </summary>
public static class PathUtils {
	/// <summary>
	/// Attempts to retrieve a new <see cref="FileInfo"/> instance local to the given <paramref name="Directory"/>.
	/// </summary>
	/// <param name="Directory">The parent directory.</param>
	/// <param name="FileName">The name of the local file.</param>
	/// <param name="FI">The resultant <see cref="FileInfo"/> instance.</param>
	/// <returns><see langword="true"/> if the filepath is valid.</returns>
	public static bool TryGetLocalFile( DirectoryInfo Directory, string FileName, out FileInfo FI ) => TryGetFileInfo(Path.Combine(Directory.FullName.TrimEnd('\\') + '\\', FileName), out FI);


	/// <summary>
	/// Attempts to retrieve a new <see cref="FileInfo"/> instance local to the given <paramref name="SpecialFolder"/>.
	/// </summary>
	/// <param name="SpecialFolder">The parent directory.</param>
	/// <param name="FileName">The name of the local file.</param>
	/// <param name="FI">The resultant <see cref="FileInfo"/> instance.</param>
	/// <returns><see langword="true"/> if the filepath is valid.</returns>
	public static bool TryGetLocalFile( Environment.SpecialFolder SpecialFolder, string FileName, out FileInfo FI ) => TryGetFileInfo(Path.Combine(Environment.GetFolderPath(SpecialFolder) + '\\', FileName), out FI);

	/// <summary>
	/// Indicates a <see cref="FileSystemInfo"/>-derived type.
	/// </summary>
	public enum PathType {
		/// <summary>
		/// <inheritdoc cref="FileInfo"/>
		/// </summary>
		File,
		/// <summary>
		/// <inheritdoc cref="DirectoryInfo"/>
		/// </summary>
		Directory,
		/// <summary>
		/// <inheritdoc cref="Directory"/>
		/// </summary>
		Folder = Directory
	}

	/// <summary>
	/// Attempts to convert the given <paramref name="FilePath"/> into a valid <see cref="FileInfo"/> instance.
	/// </summary>
	/// <param name="FilePath">The path to convert.</param>
	/// <param name="File">The returned file.</param>
	/// <returns><see langword="true"/> if conversion was successful.</returns>
	public static bool TryGetFileInfo( string? FilePath, out FileInfo File ) {
		if ( FilePath is null || string.IsNullOrWhiteSpace(FilePath) ) {
			goto Failure;
		}
		try {
			File = new FileInfo(FilePath);
			return true;
		} catch {
			//goto Failure;
		}

		Failure:
		File = default!;
		return false;
	}

	/// <summary>
	/// Attempts to convert the given <paramref name="Path"/> into a valid <see cref="DirectoryInfo"/> instance.
	/// </summary>
	/// <param name="Path">The path to convert.</param>
	/// <param name="Directory">The returned directory.</param>
	/// <returns><see langword="true"/> if conversion was successful.</returns>
	public static bool TryGetDirectoryInfo( string? Path, out DirectoryInfo Directory ) {
		if ( Path is null || string.IsNullOrWhiteSpace(Path) ) {
			goto Failure;
		}
		try {
			Directory = new DirectoryInfo(Path);
			return true;
		} catch {
			//goto Failure;
		}

		Failure:
		Directory = default!;
		return false;
	}

	/// <summary>
	/// Represents a path restriction filter
	/// </summary>
	public readonly struct PathFilter {
		/// <summary>
		/// The filter type (i.e. Audio Files)
		/// </summary>
		public readonly string Descriptor;

		/// <summary>
		/// The filter extensions (i.e. { *.mp3, *.wav, *.ogg })
		/// </summary>
		public readonly ReadOnlyCollection<string> Extensions;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="Descriptor">The filter type (i.e. Audio Files)</param>
		/// <param name="ExtensionsCollection">The filter extensions (i.e. { *.mp3, *.wav, *.ogg })</param>
		public PathFilter( string Descriptor, IList<string> ExtensionsCollection ) {
			this.Descriptor = Descriptor;
			Extensions = new ReadOnlyCollection<string>(ExtensionsCollection);
		}
		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="Descriptor">The filter type (i.e. Audio Files)</param>
		/// <param name="Extensions">The filter extensions (i.e. { *.mp3, *.wav, *.ogg })</param>
		public PathFilter( string Descriptor, params string[] Extensions ) : this(Descriptor, ExtensionsCollection: Extensions) { }

		/// <inheritdoc/>
		public override string ToString() {
			string Ext = string.Join(';', Extensions);
			return $"{Descriptor} ({Ext})|{Ext}";
		}

		/// <summary>
		/// Constructs a new <see cref="PathFilter"/>.
		/// </summary>
		/// <param name="Tuple">The tuple data to construct a <see cref="PathFilter"/> with.</param>
		public static implicit operator PathFilter( (string Descriptor, IList<string> Extensions) Tuple ) => new PathFilter(Tuple.Descriptor, Tuple.Extensions);

		/// <summary>
		/// <see cref="PathFilter"/> allowing any file.
		/// <code><c>Any File (*.*)</c></code>
		/// </summary>
		public static readonly PathFilter AnyFile = new PathFilter("Any File", "*.*");

		/// <summary>
		/// <see cref="PathFilter"/> allowing a json text file.
		/// <code><c>Json File (*.json)</c></code>
		/// </summary>
		public static readonly PathFilter JsonFile = new PathFilter("Json File", "*.json");

		/// <summary>
		/// <see cref="PathFilter"/> allowing most common audio file formats.
		/// <code><c>Audio File (*.aac;*.ape;*.flac;*.m4a;*.mp3;*.ogg;*.wav;*.wma)</c></code>
		/// </summary>
		public static readonly PathFilter AudioFile = new PathFilter("Audio File", @"*.aac", "*.ape", @"*.flac", "*.m4a", "*.mp3", "*.ogg", "*.wav", @"*.wma");

		/// <summary>
		/// <see cref="PathFilter"/> allowing most common video file formats.
		/// <code><c>Video File (*.avi;*.flv;*.m4p;*.m4v;*.mov;*.mp2;*.mp4;*.mpe;*.mpeg;*.mpg;*.mpv;*.ogg;*.qt;*.swf;*.wmv;*.webm)</c></code>
		/// </summary>
		public static readonly PathFilter VideoFile = new PathFilter("Video File", @"*.avi", @"*.flv", "*.m4p", "*.m4v", @"*.mov", "*.mp2", "*.mp4", @"*.mpe", @"*.mpeg", "*.mpg", @"*.mpv", "*.ogg", "*.qt", @"*.swf", @"*.webm", @"*.wmv");

	}

	/// <summary>
	/// Appends the given <paramref name="Enum"/> to the end of the <paramref name="Collection"/>.
	/// </summary>
	/// <typeparam name="T">The collection containing type.</typeparam>
	/// <param name="Collection">The collection to append data to.</param>
	/// <param name="Enum">The enumerable to iterate through and retrieve data to append.</param>
	public static void AddRange<T>( this ICollection<T> Collection, IEnumerable<T> Enum ) {
		foreach ( T Item in Enum ) {
			Collection.Add(Item);
		}
	}

	/// <summary>
	/// Forcefully casts the given enumerable type into the requested type.
	/// </summary>
	/// <typeparam name="TFrom">The original type.</typeparam>
	/// <typeparam name="TTo">The new type.</typeparam>
	/// <param name="Enum">The enumerable to iterate through.</param>
	/// <returns>A cast enumerable.</returns>
	public static IEnumerable<TTo> ForceCast<TFrom, TTo>( this IEnumerable<TFrom> Enum ) => Enum.Select(Item => (TTo)(dynamic)Item!);


	/// <summary>
	/// Opens the user save file browser dialog.
	/// </summary>
	/// <param name="DialogTitle">The name of the dialog.</param>
	/// <param name="InitialSelection">The initial path displayed when the dialog is shown. Must be of type <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>. A <see langword="null"/> value indicates that the dialog will use the default directory.</param>
	/// <param name="EnsureExists">Determines whether the file and/or path must exist beforehand.</param>
	/// <param name="ValidateNames">Indicates whether the dialog box only accepts valid Win32 file names.</param>
	/// <param name="InitialFilterIndex">The index of the first filter to select.</param>
	/// <param name="Filters">The filters used by the dialog.</param>
	/// <returns>The selected file or <see langword="null"/>.</returns>
	public static FileInfo? SaveFileBrowseDialog( string DialogTitle, FileSystemInfo? InitialSelection = null, bool EnsureExists = true, bool ValidateNames = true, int InitialFilterIndex = 0, params PathFilter[] Filters ) {
		VistaSaveFileDialog VSFD = new VistaSaveFileDialog {
			CheckFileExists = EnsureExists,
			CheckPathExists = EnsureExists,
			Filter = string.Join('|', Filters),
			FilterIndex = InitialFilterIndex,
			AddExtension = true,
			RestoreDirectory = true,
			Title = DialogTitle,
			ValidateNames = ValidateNames
		};
		switch ( InitialSelection ) {
			case DirectoryInfo DI:
				VSFD.InitialDirectory = DI.FullName;
				break;
			case FileInfo FI:
				VSFD.FileName = FI.FullName;
				break;
			case null:
				break;
		}
		return VSFD.ShowDialog() switch {
			true when TryGetFileInfo(VSFD.FileName, out FileInfo FI) => EnforceSomeExtension(FI, Filters, VSFD.FilterIndex),
			_                                                        => null
		};
	}

	/// <summary>
	/// Attempts to retrieve the nth item in the given collection.
	/// </summary>
	/// <typeparam name="T">The collection containing type.</typeparam>
	/// <param name="Coll">The collection of items.</param>
	/// <param name="Index">The index to retrieve.</param>
	/// <param name="Found">The found item at the given index.</param>
	/// <returns><see langword="true"/> if found.</returns>
	internal static bool HasAt<T>( IList<T> Coll, int Index, out T Found ) {
		if ( Index >= 0 && Index < Coll.Count ) {
			Found = Coll[Index];
			return true;
		}

		Found = default!;
		return false;
	}

	/// <summary>
	/// Ensures the file path retrieved from a <see cref="VistaOpenFileDialog"/> or <see cref="VistaSaveFileDialog"/> has an extension.
	/// </summary>
	/// <param name="BaseFileInfo">The original returned path.</param>
	/// <param name="Filters">The collection of <see cref="PathFilter"/>s used in the file dialog.</param>
	/// <param name="FilterIndex">The index of the selected filter.</param>
	/// <returns>The same path with the first extension in the selected filter appended if no extension is already provided.</returns>
	internal static FileInfo EnforceSomeExtension( FileInfo BaseFileInfo, PathFilter[] Filters, int FilterIndex ) {
		string FN = BaseFileInfo.FullName;
		if ( !FN.Contains('.') && HasAt(Filters, FilterIndex, out PathFilter F) && F.Extensions.FirstOrDefault() is { } FE ) {
			FN += FE.TrimStart('*');
			return new FileInfo(FN);
			//Filters[FilterIndex].Extensions.FirstOrDefault
		}
		return BaseFileInfo;
	}

	/// <summary>
	/// Opens the user file browser dialog.
	/// </summary>
	/// <param name="DialogTitle">The name of the dialog.</param>
	/// <param name="InitialSelection">The initial path displayed when the dialog is shown. Must be of type <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>. A <see langword="null"/> value indicates that the dialog will use the default directory.</param>
	/// <param name="ShowReadOnly">Controls whether to show ReadOnly items items.</param>
	/// <param name="EnsureExists">Determines whether the file and/or path must exist beforehand.</param>
	/// <param name="ValidateNames">Indicates whether the dialog box only accepts valid Win32 file names.</param>
	/// <param name="InitialFilterIndex">The index of the first filter to select.</param>
	/// <param name="Filters">The filters used by the dialog.</param>
	/// <returns>The selected file or <see langword="null"/>.</returns>
	public static FileInfo? OpenFileBrowseDialog( string DialogTitle, FileSystemInfo? InitialSelection = null, bool ShowReadOnly = false, bool EnsureExists = true, bool ValidateNames = true, int InitialFilterIndex = 0, params PathFilter[] Filters ) {
		VistaOpenFileDialog VOFD = new VistaOpenFileDialog {
			CheckFileExists = EnsureExists,
			CheckPathExists = EnsureExists,
			Filter = string.Join('|', Filters),
			FilterIndex = InitialFilterIndex,
			AddExtension = true,
			Multiselect = false,
			RestoreDirectory = true,
			Title = DialogTitle,
			ValidateNames = ValidateNames,
			ShowReadOnly = ShowReadOnly
		};
		switch ( InitialSelection ) {
			case DirectoryInfo DI:
				VOFD.InitialDirectory = DI.FullName;
				break;
			case FileInfo FI:
				VOFD.FileName = FI.FullName;
				break;
			case null:
				break;
		}
		return VOFD.ShowDialog() switch {
			true when TryGetFileInfo(VOFD.FileName, out FileInfo FI) => EnforceSomeExtension(FI, Filters, VOFD.FilterIndex),
			_                                                        => null
		};
	}

	/// <summary>
	/// Opens the user directory browser dialog.
	/// </summary>
	/// <param name="DialogDescription">The name of the dialog.</param>
	/// <param name="InitialDirectory">The initial path displayed when the dialog is shown. A <see langword="null"/> value indicates that the dialog will use the default directory.</param>
	/// <param name="ShowNewFolderButton">Determines whether to show the 'New Folder' button.</param>
	/// <param name="UseDescriptionForTitle">Determines whether the description is used for the dialog title.</param>
	/// <returns>The selected file or <see langword="null"/>.</returns>
	public static DirectoryInfo? OpenDirectoryBrowseDialog( string DialogDescription, DirectoryInfo? InitialDirectory = null, bool ShowNewFolderButton = false, bool UseDescriptionForTitle = true ) {
		VistaFolderBrowserDialog VFBD = new VistaFolderBrowserDialog {
			ShowNewFolderButton = ShowNewFolderButton,
			Description = DialogDescription,
			UseDescriptionForTitle = UseDescriptionForTitle
		};
		switch ( InitialDirectory ) {
			case { } DI:
				VFBD.SelectedPath = $"{DI.FullName.TrimEnd('\\')}\\";
				break;
			case null:
				break;
		}
		return VFBD.ShowDialog() switch {
			true when TryGetDirectoryInfo(VFBD.SelectedPath, out DirectoryInfo DI) => DI,
			_                                                                      => null
		};
	}

	/// <summary>
	/// Opens the user directory browser dialog.
	/// </summary>
	/// <param name="DialogDescription">The name of the dialog.</param>
	/// <param name="InitialSpecialFolder">The initial unique path displayed when the dialog is shown. A <see langword="null"/> value indicates that the dialog will use the default directory.</param>
	/// <param name="ShowNewFolderButton">Determines whether to show the 'New Folder' button.</param>
	/// <param name="UseDescriptionForTitle">Determines whether the description is used for the dialog title.</param>
	/// <returns>The selected file or <see langword="null"/>.</returns>
	public static DirectoryInfo? OpenDirectoryBrowseDialog( string DialogDescription, Environment.SpecialFolder InitialSpecialFolder, bool ShowNewFolderButton = false, bool UseDescriptionForTitle = true ) {
		VistaFolderBrowserDialog VFBD = new VistaFolderBrowserDialog {
			ShowNewFolderButton = ShowNewFolderButton,
			Description = DialogDescription,
			UseDescriptionForTitle = UseDescriptionForTitle,
			RootFolder = InitialSpecialFolder
		};
		return VFBD.ShowDialog() switch {
			true when TryGetDirectoryInfo(VFBD.SelectedPath, out DirectoryInfo DI) => DI,
			_                                                                      => null
		};
	}
}