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
using System.Diagnostics;
using System.IO;

using FuzzySharp;

using PropertyChanged;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Viewmodel for the <see cref="PathBrowser"/> control.
/// </summary>
public class PathBrowser_ViewModel : Reactive {

	/// <summary>
	/// The current view.
	/// </summary>
	public PathBrowser View { get; set; } = null!;

	/// <summary>
	/// The currently chosen path.
	/// </summary>
	public string? Path { get; set; }

	/// <summary>
	/// The browser discovery type (files or folders).
	/// </summary>
	public PathUtils.PathType Type { get; set; }

	/// <summary>
	/// The parent directory of the current path.
	/// </summary>
	public DirectoryInfo? ParentDirectory { get; set; } = DriveInfo.GetDrives().First().RootDirectory;

	/// <summary>
	/// Indicates whether to provide autocompletion results or not.
	/// </summary>
	public bool ProvideAutocomplete { get; set; } = true;

	/// <summary>
	/// Default Constructor.
	/// </summary>
	public PathBrowser_ViewModel() {
		PropertyChanged += ( _, E ) => {
			//Debug.WriteLine($"VM Prop Changed {E.PropertyName}");
			switch ( E.PropertyName ) {
				case nameof(Path):
					View.OnPathChanged(Path);
					break;
			}
		};
	}

	#region Autocomplete

	/// <summary>
	/// Possible autocomplete results sorted via fuzzy ranking.
	/// </summary>
	public ObservableCollection<FileSystemInfo> Autocomplete { get; set; } = new ObservableCollection<FileSystemInfo>();

	/// <summary>
	/// The most likely autocomplete the user intended.
	/// </summary>
	public string ClosestAutocomplete { get; set; } = string.Empty;

	/// <summary>
	/// Regenerates autocomplete suggestions based on the current <see cref="ParentDirectory"/>.
	/// </summary>
	public void GenerateSuggestions() {
		static IEnumerable<DirectoryInfo> SuggestDrives( /*string? ChildSearch*/ ) {
			// ReSharper disable once LoopCanBePartlyConvertedToQuery
			foreach ( DriveInfo Drive in DriveInfo.GetDrives() ) {
				/*DirectoryInfo D = Drive.RootDirectory;
				if ( ChildSearch is null || D.FullName.StartsWith(ChildSearch, StringComparison.InvariantCultureIgnoreCase) ) {
					yield return D;
				}*/
				yield return Drive.RootDirectory;
			}
		}

		static IEnumerable<DirectoryInfo> SuggestFolders( DirectoryInfo Parent ) => Parent.EnumerateDirectories();

		static IEnumerable<FileInfo> SuggestFiles( DirectoryInfo Parent ) => Parent.FastEnumerateFiles().GetInfo();

		//If PartialPath empty: SuggestDrives
		//If PartialPath semi-filled: SuggestFolders (+SuggestFiles if File Browser)

		PossibleSuggestions.Clear();
		if ( ParentDirectory is null ) {
			//PossibleSuggestions.Clear();
			PossibleSuggestions.AddRange(SuggestDrives());
		} else {
			//PossibleSuggestions.Clear();
			PossibleSuggestions.AddRange(SuggestFolders(ParentDirectory).UpTo(MaxDirectorySuggestions));
			PossibleSuggestions.AddRange(SuggestFiles(ParentDirectory).UpTo(MaxFileSuggestions));
		}
	}

	/// <summary>
	/// The maximum amount of autocomplete suggestions to provide for files.
	/// </summary>
	public int MaxFileSuggestions { get; set; } = 150;

	/// <summary>
	/// The maximum amount of autocomplete suggestions to provide for directories.
	/// </summary>
	public int MaxDirectorySuggestions { get; set; } = 50;

	/// <summary>
	/// <see cref="ObservableCollection{T}"/> of the current (unranked) possible autocomplete suggestions.
	/// </summary>
	public ObservableCollection<FileSystemInfo> PossibleSuggestions { get; } = new ObservableCollection<FileSystemInfo>();

	/// <summary>
	/// Updates the autocomplete suggestions related to the current (partially-complete) path.
	/// </summary>
	/// <param name="PartialPath">The partially-complete path.</param>
	public void UpdateSuggestions( string PartialPath ) {
		CurrentError = ParseError.None; //Clear any current errors

		switch ( Type ) {
			case PathUtils.PathType.File when PathUtils.TryGetFileInfo(PartialPath, out FileInfo FI) && FI.Exists:
				PossibleSuggestions.Clear();
				Autocomplete.Clear();
				DirectoryInfo? FIParent = FI.Directory;
				if ( !ParentDirectory.PathEquals(FIParent) ) {
					ParentDirectory = FIParent;
				}

				string FIFN = FI.GetCaseSensitiveFullName();
				ClosestAutocomplete = FIFN;
				View.ChangePath(FIFN);
				Path = FIFN;
				return;
			case PathUtils.PathType.Directory when PathUtils.TryGetDirectoryInfo(PartialPath, out DirectoryInfo DI) && DI.Exists:
				PossibleSuggestions.Clear();
				Autocomplete.Clear();
				DirectoryInfo? DIParent = DI.Parent;
				if ( !ParentDirectory.PathEquals(DIParent) ) {
					ParentDirectory = DIParent;
				}

				string DIFN = DI.GetCaseSensitiveFullName();
				ClosestAutocomplete = DIFN;
				View.ChangePath(DIFN);
				return;
		}

		if ( !ProvideAutocomplete ) { return; }

		static void SplitPartial( string? Partial, out DirectoryInfo? Parent, out string? ChildSearch ) {
			Parent = null; ChildSearch = null;
			if ( Partial is null || string.IsNullOrWhiteSpace(Partial) ) { return; }
			(string? DirStr, string? Child) = Partial.SplitOnEnd('\\', true);
			if ( Child is null ) {
				//Parent = null;
				ChildSearch = DirStr;
			} else {
				Parent = PathUtils.TryGetDirectoryInfo(DirStr, out DirectoryInfo Pa) && Pa.Exists ? Pa : null;
				ChildSearch = Child;
			}
		}

		SplitPartial(PartialPath, out DirectoryInfo? Parent, out string? ChildSearch);
		if ( !ParentDirectory.PathEquals(Parent) ) { //Update autocomplete suggestions on parent change.
			ParentDirectory = Parent;
			Debug.WriteLine($"Parent directory changed to {Parent}, updating suggestions.");
			if ( Parent is null && PartialPath.Count('\\') >= 1 ) {
				CurrentError |= ParseError.InvalidParentDirectory;
			}
			//Debug.WriteLine($"\tNull? {Parent is null} ; Path: '{PartialPath}' ; \\ count: {PartialPath.Count('\\')}");
			Debug.WriteLine($"\tParent directory {(CurrentError.HasFlag(ParseError.InvalidParentDirectory) ? "is not" : "is")} valid.");
			//If the parent directory does not exist and the currently entered path is clearly indicating some directory, enter error mode
			GenerateSuggestions();
		}

		static IEnumerable<KeyValuePair<int, FileSystemInfo>> Rank( string? ChildSearch, IEnumerable<FileSystemInfo> Possible ) {
			if ( ChildSearch is null || string.IsNullOrEmpty(ChildSearch) ) {
				Debug.WriteLine("No ChildSearch, ranking alphabetically...");
				//If no partial addition is present, return possible in order of appearance (should be: Directories [A-Z], Files [A-Z])
				FileSystemInfo[] Arr = Possible.ToArray();
				double L = Arr.Length; //Use double for decimal division
				for ( int I = 0; I < L; I++ ) {
					int S = (int)Math.Round(100.0 * (1 - I / L));
					yield return new KeyValuePair<int, FileSystemInfo>(S, Arr[I]);
				}
			} else {
				Debug.WriteLine($"Using ChildSearch '{ChildSearch}'");
				foreach ( FileSystemInfo P in Possible ) {
					//Change fuzzy algorithm here.
					string Pn = P.Name.ToLowerInvariant();
					yield return new KeyValuePair<int, FileSystemInfo>(Equals(ChildSearch, Pn) ? 100 : Fuzz.Ratio(ChildSearch.ToLowerInvariant(), Pn), P);
				}
			}
		}

		SortedList<int, FileSystemInfo> Results = new SortedList<int, FileSystemInfo>(_RevNoCollIntegerComp);
		Results.AddRange(Rank(ChildSearch?.ToLowerInvariant(), PossibleSuggestions));
		Debug.WriteLine("--------");
		foreach ( (int I, FileSystemInfo F) in Results ) {
			Debug.WriteLine($"[{I}] {F.FullName}");
		}
		Debug.WriteLine("--------");
		Autocomplete.Clear();
		Autocomplete.AddRange(Results.Values);
		ClosestAutocomplete = Autocomplete.SafeFirst(FSI => FSI switch {
			DirectoryInfo => FSI.GetCaseSensitiveFullName().TrimEnd('\\') + '\\',
			_             => FSI.GetCaseSensitiveFullName()
		}, string.Empty);
		Path = PartialPath;
	}

	//Possible optimisation: if found files or folders exceeds count (i.e. 50) start using wildcard restrictions to cut down on collection size and iteration speed. Does make fuzzy search less forgiving, but still works to a degree and doesn't lag out majorly when typing.

	///// <summary>
	///// Static instance of the <see cref="NonCollidingIntegerComparer"/> class. For usage in SortedDictionaries.
	///// </summary>
	//static readonly NonCollidingIntegerComparer _NoCollIntegerComp = new NonCollidingIntegerComparer();

	///// <summary>
	///// Integer comparer that doesn't return <c>0</c>.
	///// </summary>
	//internal class NonCollidingIntegerComparer : IComparer<int> {
	//	/// <inheritdoc/>
	//	public int Compare( int Left, int Right ) => Right > Left ? -1 : 1;
	//	// No zero results to allow SortedDictionary usage.
	//}

	/// <summary>
	/// Static instance of the <see cref="ReverseNonCollidingIntegerComparer"/> class. For usage in SortedDictionaries.
	/// </summary>
	static readonly ReverseNonCollidingIntegerComparer _RevNoCollIntegerComp = new ReverseNonCollidingIntegerComparer();

	/// <summary>
	/// Reversed integer comparer that doesn't return <c>0</c>.
	/// </summary>
	internal class ReverseNonCollidingIntegerComparer : IComparer<int> {
		/// <inheritdoc/>
		public int Compare( int Left, int Right ) => Right > Left ? 1 : -1;
		// No zero results to allow SortedDictionary usage.
	}

	#endregion

	/// <summary>
	/// Concludes the current editing session. Used when the textbox loses keyboard focus for updating <see cref="CurrentError"/> flags.
	/// </summary>
	public void ConcludeEdit() {
		Debug.WriteLine($"Concluding edit with '{Path}'...");
		switch ( Type ) {
			case PathUtils.PathType.File when PathUtils.TryGetFileInfo(Path, out FileInfo FI) && FI.Exists:
				break;
			case PathUtils.PathType.Directory when PathUtils.TryGetDirectoryInfo(Path, out DirectoryInfo DI) && DI.Exists:
				break;
			default:
				CurrentError |= ParseError.InvalidPath;
				break;
		}
	}

	/// <summary>
	/// The current parse errors present.
	/// </summary>
	public ParseError CurrentError { get; set; } = ParseError.None;

	/// <summary>
	/// <see cref="FileInfo"/> variant of the current <see cref="Path"/>.
	/// </summary>
	/// <exception cref="NotSupportedException">The current <see cref="Type"/> is not <see cref="PathUtils.PathType.File"/>.</exception>
	[DependsOn(nameof(Path), nameof(Type))]
	public FileInfo? File {
		get => Type switch {
			PathUtils.PathType.File => PathUtils.TryGetFileInfo(Path, out FileInfo FI) ? FI : null,
			_                       => throw new NotSupportedException($"PathBrowser is designated for {Type} types only, not files.")
		};
		set {
			Path = Type switch {
				PathUtils.PathType.File => value?.FullName,
				_                       => throw new NotSupportedException($"PathBrowser is designated for {Type} types only, not files.")
			};
		}
	}
	/// <summary>
	/// <see cref="DirectoryInfo"/> variant of the current <see cref="Path"/>.
	/// </summary>
	/// <exception cref="NotSupportedException">The current <see cref="Type"/> is not <see cref="PathUtils.PathType.Directory"/>.</exception>
	[DependsOn(nameof(Path), nameof(Type))]
	public DirectoryInfo? Directory {
		get => Type switch {
			PathUtils.PathType.Directory => PathUtils.TryGetDirectoryInfo(Path, out DirectoryInfo FI) ? FI : null,
			_                            => throw new NotSupportedException($"PathBrowser is designated for {Type} types only, not directories.")
		};
		set {
			Path = Type switch {
				PathUtils.PathType.Directory => value?.FullName,
				_                            => throw new NotSupportedException($"PathBrowser is designated for {Type} types only, not directories.")
			};
		}
	}

	/// <summary>
	/// Returns <see langword="true"/> if the <see cref="CurrentError"/> is equivalent to <see cref="ParseError.None"/>.
	/// </summary>
	public bool IsValid => CurrentError == ParseError.None;
	//public bool 

	/// <summary>
	/// Represents any of the possible parse errors that may be thrown.
	/// </summary>
	[Flags]
	public enum ParseError {
		/// <summary> No error. </summary>
		None = 0,
		/// <summary> Chosen path is invalid/malformed. </summary>
		InvalidPath = 1,
		/// <summary> Parent directory is invalid/malformed. </summary>
		InvalidParentDirectory = 2
	}
}