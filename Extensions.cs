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
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Expression = System.Linq.Expressions.Expression;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// General extensions methods.
/// </summary>
public static class Extensions {


	/// <summary>
	/// Attempts to get the index of the item in the collection, returning <see langword="true"/> if
	/// successful.
	/// </summary>
	/// <typeparam name="T">The collection containing type.</typeparam>
	/// <param name="Coll">The collection to search through.</param>
	/// <param name="Find">The item to search for.</param>
	/// <param name="I">The index of the item if found. Defaults to <c>-1</c>.
	/// </param>
	/// <returns><see langword="true"/> if successfully found, otherwise 
	/// <see langword="false"/>.</returns>
	public static bool TryGetIndex<T>( this IList<T>? Coll, T? Find, out int I ) {
		if ( Coll is null || Find is null ) {
			I = -1;
			return false;
		}

		I = Coll.IndexOf(Find);
		return I >= 0;
	}

	/// <summary>
	/// 'Sets' the observable collection to the given enumerable by first clearing the pre-existing data, then adding the new data. Maintains event handlers during process. Additionally invokes CollectionChanged handlers for original data on the <paramref name="NewObvColl"/> collection.
	/// </summary>
	/// <typeparam name="T">The contained type.</typeparam>
	/// <param name="ObvColl">The original (observed) collection.</param>
	/// <param name="NewObvColl">The new (observed) collection.</param>
	public static void FakeSet<T>( this ObservableCollection<T> ObvColl, ObservableCollection<T> NewObvColl ) {
		lock ( NewObvColl ) {
			T[] NewColl = NewObvColl.ToArray();
			NewObvColl.AddRange(ObvColl);
			lock ( ObvColl ) {
				ObvColl.Clear();
				ObvColl.AddRange(NewColl);
			}
		}
	}

	/// <summary>
	/// 'Sets' the observable collection to the given enumerable by first clearing the pre-existing data, then adding the new data. Maintains event handlers during process.
	/// </summary>
	/// <typeparam name="T">The contained type.</typeparam>
	/// <param name="ObvColl">The original (observed) collection.</param>
	/// <param name="NewColl">The new collection.</param>
	public static void FakeSet<T>( this ObservableCollection<T> ObvColl, IEnumerable<T> NewColl ) {
		lock ( ObvColl ) {
			ObvColl.Clear();
			ObvColl.AddRange(NewColl);
		}
	}

	/// <summary>
	/// Searches for a resource with the specified key and type, returning the resource when found.
	/// <para/>
	/// For a safer approach, See: <seealso cref="TryFindResource{T}(FrameworkElement, string, out T)"/>
	/// </summary>
	/// <typeparam name="T">The type the resource is expected to be.</typeparam>
	/// <param name="FE">The element to retrieve to resource from.</param>
	/// <param name="ResourceKey">The key to search for.</param>
	/// <exception cref="ResourceReferenceKeyNotFoundException">The resource with the given key was not found, or was of type (or castable to) <typeparamref name="T"/>.</exception>
	/// <returns>The found resource.</returns>
	public static T FindResource<T>( this FrameworkElement FE, string ResourceKey ) => TryFindResource(FE, ResourceKey, out T Found)
		? Found
		: throw new ResourceReferenceKeyNotFoundException($"The resource was not found, or was the incorrect type. (Expecting a resource with key '{ResourceKey}' and type '{typeof(T)})'", ResourceKey);


	/// <summary>
	/// Flattens the collection of collections into a single dimension.
	/// </summary>
	/// <typeparam name="T">The data type.</typeparam>
	/// <param name="EnumInEnum">The collection of collections to flatten.</param>
	/// <returns>The flattened collection of values.</returns>
	public static IEnumerable<T> Flatten<T>( this IEnumerable<IEnumerable<T>> EnumInEnum ) {
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach ( IEnumerable<T> Enum in EnumInEnum ) {
			foreach ( T Item in Enum ) {
				yield return Item;
			}
		}
	}

	/// <summary>
	/// Searches for a resource with the specified key and type, returning <see langword="true"/> if found.
	/// </summary>
	/// <typeparam name="T">The type the resource is expected to be.</typeparam>
	/// <param name="FE">The element to retrieve to resource from.</param>
	/// <param name="ResourceKey">The key to search for.</param>
	/// <param name="Found">The found resource.</param>
	/// <returns><see langword="true"/> if successfully found.</returns>
	public static bool TryFindResource<T>( this FrameworkElement FE, string ResourceKey, out T Found ) {
		if ( FE.TryFindResource(ResourceKey) is T F ) {
			Found = F;
			return true;
		}
		Found = default!;
		return false;
	}

	/// <summary>
	/// Dynamically-compiled form of the pipe ('|') <see langword="operator"/> (OR).
	/// </summary>
	/// <typeparam name="T">The value types.</typeparam>
	/// <param name="A">The first value.</param>
	/// <param name="B">The second value.</param>
	/// <returns>Value equivalent to: <code><paramref name="A"/> | <paramref name="B"/></code></returns>
	internal static T Pipe<T>( T A, T B ) {
		ParameterExpression
			ParamA = Expression.Parameter(typeof(T), nameof(A)),
			ParamB = Expression.Parameter(typeof(T), nameof(B));

		BinaryExpression Body = Expression.Or(ParamA, ParamB);

		Func<T, T, T> Or = Expression.Lambda<Func<T, T, T>>(Body, ParamA, ParamB).Compile();

		return Or(A, B);
	}
		
	/// <summary>
	/// Dynamically-compiled form of the tilde ('~') <see langword="operator"/> (NOT).
	/// </summary>
	/// <typeparam name="T">The value types.</typeparam>
	/// <param name="A">The value to invert.</param>
	/// <returns>Value equivalent to: <code>~<paramref name="A"/></code></returns>
	internal static T Tilde<T>( T A ) {
		ParameterExpression Param = Expression.Parameter(typeof(T), nameof(A));

		UnaryExpression Body = Expression.Not(Param);

		Func<T, T> Not = Expression.Lambda<Func<T, T>>(Body, Param).Compile();

		return Not(A);
	}

	/// <summary>
	/// Dynamically-compiled form of the ampersand ('&amp;') <see langword="operator"/> (AND).
	/// </summary>
	/// <typeparam name="T">The value types.</typeparam>
	/// <param name="A">The first value.</param>
	/// <param name="B">The second value.</param>
	/// <returns>Value equivalent to: <code><paramref name="A"/> &amp; <paramref name="B"/></code></returns>
	internal static T Ampersand<T>( T A, T B ) {
		ParameterExpression
			ParamA = Expression.Parameter(typeof(T), nameof(A)),
			ParamB = Expression.Parameter(typeof(T), nameof(B));

		BinaryExpression Body = Expression.And(ParamA, ParamB);

		Func<T, T, T> And = Expression.Lambda<Func<T, T, T>>(Body, ParamA, ParamB).Compile();

		return And(A, B);
	}

	/// <summary>
	/// Returns a new <see langword="enum"/> instance equivalent to: <code><paramref name="Enum"/> | <paramref name="Flag"/></code>.
	/// </summary>
	/// <typeparam name="T">The <see langword="enum"/> type.</typeparam>
	/// <param name="Enum">The <see langword="enum"/>.</param>
	/// <param name="Flag">The flag to add.</param>
	/// <returns>A new <see langword="enum"/> instance.</returns>
	public static T WithFlag<T>( this T Enum, T Flag ) where T : Enum => Pipe(Enum, Flag);

	/// <summary>
	/// Returns a new <see langword="enum"/> instance equivalent to: <code><paramref name="Enum"/> &amp; ~<paramref name="Flag"/></code>.
	/// </summary>
	/// <typeparam name="T">The <see langword="enum"/> type.</typeparam>
	/// <param name="Enum">The <see langword="enum"/>.</param>
	/// <param name="Flag">The flag to remove.</param>
	/// <returns>A new <see langword="enum"/> instance.</returns>
	public static T WithoutFlag<T>( this T Enum, T Flag ) where T : Enum => Ampersand(Enum, Tilde(Flag));

	/// <summary>
	/// Appends <paramref name="Flag"/> to the given <paramref name="Enum"/>.
	/// </summary>
	/// <typeparam name="T">The <see langword="enum"/> type.</typeparam>
	/// <param name="Enum">The <see langword="enum"/> to edit.</param>
	/// <param name="Flag">The flag to append.</param>
	public static T AddFlag<T>( this ref T Enum, T Flag ) where T : struct, Enum => Enum = WithFlag(Enum, Flag);

	/// <summary>
	/// Truncates <paramref name="Flag"/> from the given <paramref name="Enum"/>.
	/// </summary>
	/// <typeparam name="T">The <see langword="enum"/> type.</typeparam>
	/// <param name="Enum">The <see langword="enum"/> to edit.</param>
	/// <param name="Flag">The flag to remove.</param>
	public static T RemoveFlag<T>( this ref T Enum, T Flag ) where T : struct, Enum => Enum = WithoutFlag(Enum, Flag);

	/// <summary>
	/// Safe-housed version of a method utilising <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>.
	/// </summary>
	/// <typeparam name="TEn">The enumerable type.</typeparam>
	/// <typeparam name="TRet">The return type.</typeparam>
	/// <param name="Enum">The enumerable to check.</param>
	/// <param name="With">The function invoked when the first element was found.</param>
	/// <param name="Without">The function invoked when the first element was not found.</param>
	/// <returns>The returned value from either <paramref name="With"/> or <paramref name="Without"/>, depending on if the first value of the enumerable was found.</returns>
	public static TRet SafeFirst<TEn, TRet>( this IEnumerable<TEn>? Enum, Func<TEn, TRet> With, Func<TRet> Without ) =>
		Enum switch {
			{ } E when E.FirstOrDefault() is { } F => With.Invoke(F),
			_                                      => Without.Invoke()
		};

	/// <summary>
	/// Safe-housed version of a method utilising <see cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource})"/>.
	/// </summary>
	/// <typeparam name="TEn">The enumerable type.</typeparam>
	/// <typeparam name="TRet">The return type.</typeparam>
	/// <param name="Enum">The enumerable to check.</param>
	/// <param name="With">The function invoked when the first element was found.</param>
	/// <param name="Without">The value returned when the first element was not found.</param>
	/// <returns>The returned value from either <paramref name="With"/> or <paramref name="Without"/>, depending on if the first value of the enumerable was found.</returns>
	public static TRet SafeFirst<TEn, TRet>( this IEnumerable<TEn>? Enum, Func<TEn, TRet> With, TRet Without ) =>
		Enum switch {
			{ } E when E.FirstOrDefault() is { } F => With.Invoke(F),
			_                                      => Without
		};

	/// <summary>
	/// Counts the number of character occurrences in the given string.
	/// </summary>
	/// <param name="Str">The string to count occurrences within.</param>
	/// <param name="C">The character to count for.</param>
	/// <returns>The number of <paramref name="C"/> in <paramref name="Str"/></returns>
	public static int Count( this string? Str, char? C ) => Str is null || C is null ? 0 : Str.Count(C.Value.Equals);

	/// <summary>
	/// Returns whether the paths point to the same file or directory.
	/// </summary>
	/// <typeparam name="T">The path type (i.e. <see cref="FileInfo"/>, <see cref="DirectoryInfo"/>)</typeparam>
	/// <param name="A">The first path.</param>
	/// <param name="B">The second path.</param>
	/// <returns><see langword="true"/> if <paramref name="A"/> and <paramref name="B"/> are both null, or if <see cref="FileSystemInfo.FullName"/> is equivalent for both paths.</returns>
	public static bool PathEquals<T>( this T? A, T? B ) where T : FileSystemInfo =>
		A switch {
			null => B is null,
			_    => B is not null && A.FullName.Equals(B.FullName, StringComparison.OrdinalIgnoreCase)
		};



	/// <summary>
	/// Splits the string upon the last occurrence of the given character.
	/// </summary>
	/// <param name="Str">The string to split.</param>
	/// <param name="Split">The character to split on.</param>
	/// <param name="IncludeCharOnStart">Whether to include the split character with the returned Start value.</param>
	/// <param name="IncludeCharOnEnd">Whether to include the split character with the returned End value.</param>
	/// <returns>A string tuple containing the text before and after the split character.</returns>
	public static (string? Start, string? End) SplitOnEnd( this string? Str, char Split, bool IncludeCharOnStart = false, bool IncludeCharOnEnd = false ) {
		if ( Str is null ) { return (null, null); }
		int I = Str.LastIndexOf(Split);
		return I switch {
			< 0 => (Str, null),
			_   => (Str[..(I + (IncludeCharOnStart ? 1 : 0))], Str[(I + (IncludeCharOnEnd ? 0 : 1))..])
		};
	}

	/// <summary>
	/// Returns the first n items of the enumerable.
	/// </summary>
	/// <typeparam name="T">The enumerable containing type.</typeparam>
	/// <param name="Enum">The enumerable to iterate through.</param>
	/// <param name="MaxReturnCount">The maximum amount of items to return.</param>
	/// <returns>A truncated enumerable.</returns>
	public static IEnumerable<T> UpTo<T>( this IEnumerable<T> Enum, int MaxReturnCount ) {
		int I = 0;
		// ReSharper disable once LoopCanBePartlyConvertedToQuery
		foreach ( T Item in Enum ) {
			if ( I >= MaxReturnCount ) {
				yield break;
			}
			yield return Item;
			I++;
		}
	}

	/// <summary>
	/// Returns the first n items of the enumerable.
	/// </summary>
	/// <typeparam name="T">The enumerable containing type.</typeparam>
	/// <param name="Enum">The enumerable to iterate through.</param>
	/// <param name="MaxReturnCount">The maximum amount of items to return.</param>
	/// <param name="LeftEarly">Output variable reference indicating whether the limit was reached or not.</param>
	/// <returns>A (possibly-)truncated enumerable.</returns>
	public static IEnumerable<T> UpTo<T>( this IEnumerable<T> Enum, int MaxReturnCount, Out<bool> LeftEarly ) {
		int I = 0;
		LeftEarly.Value = false;
		// ReSharper disable once LoopCanBePartlyConvertedToQuery
		foreach ( T Item in Enum ) {
			if ( I >= MaxReturnCount ) {
				LeftEarly.Value = true;
				yield break;
			}
			yield return Item;
			I++;
		}
	}

	/// <summary>
	/// Returns the first n items of the enumerable.
	/// </summary>
	/// <typeparam name="T">The enumerable containing type.</typeparam>
	/// <param name="Enum">The enumerable to iterate through.</param>
	/// <param name="MaxReturnCount">The maximum amount of items to return.</param>
	/// <param name="LeftEarly">Indicates whether the limit was reached or not.</param>
	/// <returns>A (possibly-)truncated enumerable.</returns>
	public static IEnumerable<T> UpTo<T>( this IEnumerable<T> Enum, int MaxReturnCount, out bool LeftEarly ) {
		Out<bool> LE = new Out<bool>(false);
		IEnumerable<T> Return = UpTo(Enum, MaxReturnCount, LE);
		LeftEarly = LE.Value;
		return Return;
	}

	/// <summary>
	/// Attempts to retrieve the file from the given name and subdirectory.
	/// </summary>
	/// <param name="DI">The parent directory to search within.</param>
	/// <param name="FileName">The file name to search for.</param>
	/// <param name="Found">The found file.</param>
	/// <returns><see langword="true"/> if successful.</returns>
	public static bool TryGetFile( this DirectoryInfo DI, string FileName, out FileInfo Found ) => PathUtils.TryGetFileInfo(Path.Combine(DI.FullName.TrimEnd('\\') + '\\', FileName), out Found);

	/// <inheritdoc cref="PathUtils.TryGetFileInfo(string?, out FileInfo)"/>
	public static bool TryGetFileInfo( this string? FilePath, out FileInfo File ) => PathUtils.TryGetFileInfo(FilePath, out File);

	/// <summary>
	/// Attempts to convert the given <paramref name="Path"/> into a valid <see cref="FileSystemInfo"/> instance.
	/// </summary>
	/// <param name="Path">The path to convert.</param>
	/// <param name="FS">The returned <see cref="FileSystemInfo"/> instance.</param>
	/// <returns><see langword="true"/> if conversion was successful.</returns>
	public static bool TryGetFileSystemInfo( this string? Path, out FileSystemInfo FS ) {
		switch ( Path ) {
			case null:
				FS = default!;
				return false;
			//case { } Path:
			default:
				if ( TakeAllAfterLast(Path, '\\')?.Contains('.') ?? false ) {
					//There is a '.' after the last '\', therefore path is for a file.
					bool FSuccess = TryGetFileInfo(Path, out FileInfo FI);
					FS = FI;
					return FSuccess;
				} else {
					//Assume directory (Note: this isn't always correct, i.e. when a file has no extension)

					bool DSuccess = TryGetDirectoryInfo(Path, out DirectoryInfo DI);
					FS = DI;
					return DSuccess;
				}
		}
	}

	/// <summary>
	/// Returns all characters after the given character.
	/// </summary>
	/// <param name="Str">The string to apply a substring to.</param>
	/// <param name="X">The character to take all characters after.</param>
	/// <param name="IncludeChar">Whether to include the split character.</param>
	/// <returns>All characters after the given character in a given <see cref="string"/>.</returns>
	internal static string? TakeAllAfterLast( string Str, char X, bool IncludeChar = false ) {
		int I = Str.LastIndexOf(X);
		if ( I < 0 ) { return null; }
		if ( !IncludeChar ) { I++; }
		return I < Str.Length ? Str[I..] : string.Empty;
	}

	/// <inheritdoc cref="PathUtils.TryGetDirectoryInfo(string?, out DirectoryInfo)"/>
	public static bool TryGetDirectoryInfo( this string? Path, out DirectoryInfo Directory ) => PathUtils.TryGetDirectoryInfo(Path, out Directory);

	/// <summary>
	/// Retrieves a <see cref="FileInfo"/> to the desired <paramref name="FilePath"/>, returning <see langword="null"/> on failure.
	/// </summary>
	/// <param name="FilePath">The path to the file.</param>
	/// <returns>A new <see cref="FileInfo"/> instance, or <see langword="null"/>.</returns>
	public static FileInfo? GetFileInfoOrNull( this string? FilePath ) => TryGetFileInfo(FilePath, out FileInfo File) ? File : null;

	/// <summary>
	/// Retrieves a <see cref="DirectoryInfo"/> to the desired <paramref name="DirectoryPath"/>, returning <see langword="null"/> on failure.
	/// </summary>
	/// <param name="DirectoryPath">The path to the Directory.</param>
	/// <returns>A new <see cref="DirectoryInfo"/> instance, or <see langword="null"/>.</returns>
	public static DirectoryInfo? GetDirectoryInfoOrNull( this string? DirectoryPath ) => TryGetDirectoryInfo(DirectoryPath, out DirectoryInfo Directory) ? Directory : null;

	/// <summary>
	/// Retrieves a <see cref="FileSystemInfo"/> to the desired <paramref name="Path"/>, returning <see langword="null"/> on failure.
	/// </summary>
	/// <param name="Path">The path to the FileSystem.</param>
	/// <returns>A new <see cref="FileSystemInfo"/> instance, or <see langword="null"/>.</returns>
	public static FileSystemInfo? GetFileSystemInfoOrNull( this string? Path ) => TryGetFileSystemInfo(Path, out FileSystemInfo FS) ? FS : null;

	/// <summary>
	/// Forcefully retrieves a <see cref="FileInfo"/> to the desired <paramref name="FilePath"/>, throwing an <see cref="ArgumentNullException"/> on failure.
	/// </summary>
	/// <param name="FilePath">The path to the file.</param>
	/// <exception cref="ArgumentNullException">No FileInfo instance could be created.</exception>
	/// <returns>A new <see cref="FileInfo"/> instance.</returns>
	public static FileInfo GetFileInfo( this string? FilePath ) => TryGetFileInfo(FilePath, out FileInfo File) ? File : throw new ArgumentNullException($"No file was found for the given path '{FilePath}'");

	/// <summary>
	/// Forcefully retrieves a <see cref="DirectoryInfo"/> to the desired <paramref name="DirectoryPath"/>, throwing an <see cref="ArgumentNullException"/> on failure.
	/// </summary>
	/// <param name="DirectoryPath">The path to the Directory.</param>
	/// <exception cref="ArgumentNullException">No DirectoryInfo instance could be created.</exception>
	/// <returns>A new <see cref="DirectoryInfo"/> instance.</returns>
	public static DirectoryInfo GetDirectoryInfo( this string? DirectoryPath ) => TryGetDirectoryInfo(DirectoryPath, out DirectoryInfo Directory) ? Directory : throw new ArgumentNullException($"No Directory was found for the given path '{DirectoryPath}'");

	/// <summary>
	/// Forcefully retrieves a <see cref="FileSystemInfo"/> to the desired <paramref name="Path"/>, throwing an <see cref="ArgumentNullException"/> on failure.
	/// </summary>
	/// <param name="Path">The path to the FileSystem.</param>
	/// <exception cref="ArgumentNullException">No FileSystemInfo instance could be created.</exception>
	/// <returns>A new <see cref="FileSystemInfo"/> instance.</returns>
	public static FileSystemInfo GetFileSystemInfo( this string? Path ) => TryGetFileSystemInfo(Path, out FileSystemInfo FS) ? FS : throw new ArgumentNullException($"No File or Directory was found for the given path '{Path}'");

	/// <summary>
	/// Templates a new, local <see cref="FileInfo"/> without checking for path existence, etc.
	/// </summary>
	/// <param name="Directory">The parent directory.</param>
	/// <param name="FileName">The name of the local file. (i.e. 'SomeFile.mp3')</param>
	/// <returns>A new <see cref="FileInfo"/> instance relative to the parent directory.</returns>
	public static FileInfo DraftFile( this DirectoryInfo Directory, string FileName ) => GetFileInfo(Path.Combine(Directory.FullName.TrimEnd('\\') + '\\', FileName));

	/// <summary>
	/// Templates a new, local <see cref="FileInfo"/> without checking for path existence, etc.
	/// </summary>
	/// <param name="Directory">The parent directory.</param>
	/// <param name="FileName">The name of the local file. (i.e. 'SomeFile')</param>
	/// <param name="Extension">The extension to use for the local file. (i.e. '*.mp3')</param>
	/// <param name="ExtensionIndex">The index of the specific extension to use. (i.e. '0')</param>
	/// <returns>A new <see cref="FileInfo"/> instance relative to the parent directory.</returns>
	public static FileInfo DraftFile( this DirectoryInfo Directory, string FileName, PathUtils.PathFilter Extension, int ExtensionIndex = 0 ) => GetFileInfo(Path.Combine(Directory.FullName.TrimEnd('\\') + '\\', FileName + Extension.Extensions[ExtensionIndex].Trim('*')));

	/// <summary>
	/// Clamps the given value to the specified minimum and maximum values.
	/// </summary>
	/// <param name="V">The value to clamp.</param>
	/// <param name="Min">The minimum returned value.</param>
	/// <param name="Max">The maximum returned value.</param>
	/// <returns>The value clamped between <paramref name="Min"/> and <paramref name="Max"/>.</returns>
	public static int Clamp( this int V, int Min, int Max ) => V < Min ? Min : V > Max ? Max : V;

	/// <summary>
	/// Attempts to retrieve the nth item from the collection.
	/// </summary>
	/// <typeparam name="T">The collection containing type.</typeparam>
	/// <param name="List">The collection to retrieve the item from.</param>
	/// <param name="Index">The index to attempt to retrieve. Fails if negative or greater than/equal to the length of the collection.</param>
	/// <param name="Found">The found item. Returns <see langword="default"/>/<see langword="null"/> on failure (ignore).</param>
	/// <returns><see langword="true"/> if successfully found.</returns>
	public static bool TryGet<T>( this IList<T> List, int Index, out T Found ) {
		if ( Index >= 0 && Index < List.Count ) {
			Found = List[Index];
			return true;
		}

		Found = default!;
		return false;
	}

	/// <summary>
	/// Attempts to find the index of the first occurrence of a given item, given a custom equality comparison function.
	/// </summary>
	/// <typeparam name="T">The collection containing type.</typeparam>
	/// <param name="List">The collection to iterate through.</param>
	/// <param name="EqualityComparer">The method invoked to check item equality.</param>
	/// <returns>The index of the first equality occurrence, or <c>-1</c>.</returns>
	public static int IndexOf<T>( this IList<T> List, Func<T, bool> EqualityComparer ) {
		int I = 0;
		foreach ( T Item in List ) {
			if ( EqualityComparer(Item) ) {
				return I;
			}
			I++;
		}

		return -1;
	}

	/// <summary>
	/// Retrieves all generated elements of the given type within the specified <paramref name="ItemsControl"/>.
	/// </summary>
	/// <typeparam name="TElement">The type to return.</typeparam>
	/// <param name="ItemsControl">The parent view.</param>
	/// <returns>All child controls of type <typeparamref name="TElement"/>.</returns>
	public static IEnumerable<TElement> GetElementsOfType<TElement>( this ItemsControl ItemsControl ) where TElement : FrameworkElement {
		ItemContainerGenerator Generator = ItemsControl.ItemContainerGenerator;

		foreach ( object Item in ItemsControl.Items ) {
			if ( Generator.ContainerFromItem(Item) is FrameworkElement FEContainer && GetDescendant(FEContainer, FE => FE is TElement) is TElement Element ) {
				yield return Element;
			}
		}
	}

	/// <summary>
	/// Returns the first descendent matching the specified search criteria.
	/// </summary>
	/// <param name="Element">The element to search for descendants within.</param>
	/// <param name="Predicate">The search predicate.</param>
	/// <returns>The first found match, or <see langword="null"/>.</returns>
	public static FrameworkElement? GetDescendant( this FrameworkElement? Element, Func<FrameworkElement, bool> Predicate ) {
		if ( Element == null ) {
			return null;
		}

		if ( Predicate.Invoke(Element) ) {
			return Element;
		}

		_ = Element.ApplyTemplate();

		for ( int I = 0; I < VisualTreeHelper.GetChildrenCount(Element); I++ ) {
			if ( VisualTreeHelper.GetChild(Element, I) is FrameworkElement FEChild && GetDescendant(FEChild, Predicate) is { } Match ) {
				return Match;
			}
		}

		return null;
	}

	/// <summary>
	/// Attempts to retrieve the first visual child of type <typeparamref name="T"/> (may not be the first *visually*) of the given <see cref="DependencyObject"/>.
	/// </summary>
	/// <typeparam name="T">The type to search for.</typeparam>
	/// <param name="DO">The parent to search within.</param>
	/// <param name="Found">The found child.</param>
	/// <returns><see langword="true"/> if successful.</returns>
	public static bool TryFindFirstVisualChild<T>( this DependencyObject DO, out T Found ) where T : DependencyObject {
		if ( DO.FindVisualChildren<T>().FirstOrDefault() is { } Child ) {
			Found = Child;
			return true;
		}

		Found = default!;
		return false;
	}

	/// <summary>
	/// Moves the caret to the given position.
	/// </summary>
	/// <param name="TB">The textbox to move the caret within.</param>
	/// <param name="Pos">The requested caret position.</param>
	public static void MoveCaret( this TextBox TB, ForcedCaretPosition Pos ) {
		switch ( Pos ) {
			case ForcedCaretPosition.Leave:
				return;
			case ForcedCaretPosition.Start:
				TB.CaretIndex = 0;
				break;
			case ForcedCaretPosition.End:
				TB.CaretIndex = TB.Text.Length/* - 1*/;
				break;
		}
	}

	/// <summary>
	/// Swaps the output reference and the return value.
	/// </summary>
	/// <typeparam name="TOldOut">The original return value type.</typeparam>
	/// <typeparam name="TOutVal">The output reference value type.</typeparam>
	/// <param name="Value">The original return value.</param>
	/// <param name="Output">The output reference value to return instead.</param>
	/// <param name="OriginalOutput">An inline output of the original return value.</param>
	/// <returns><see cref="Out{T}.Value"/> of <paramref name="Output"/>.</returns>
	public static TOutVal Swap<TOldOut, TOutVal>( this TOldOut Value, Out<TOutVal> Output, out TOldOut OriginalOutput ) {
		OriginalOutput = Value;
		return Output.Value;
	}

	/// <summary>
	/// Constructs a new output with the given initial value.
	/// </summary>
	/// <typeparam name="T">The stored value type.</typeparam>
	/// <param name="Value">The initial value for the output.</param>
	/// <returns>A new instance of type <see cref="Out{T}"/>.</returns>
	public static Out<T> AsOut<T>( this T Value ) => new Out<T>(Value);

	/// <inheritdoc cref="string.Join(char, object?[])"/>
	/// <param name="Values">A collection that contains the objects to concatenate.</param>
	/// <param name="Separator">The character to include between concatenated objects.</param>
	/// <returns>A concatenated string.</returns>
	public static string Join( this IEnumerable<string?>? Values, char Separator ) => Values is null ? string.Empty : string.Join(Separator, Values);

	/// <inheritdoc cref="string.Join(string, object?[])"/>
	/// <param name="Values">A collection that contains the objects to concatenate.</param>
	/// <param name="Separator">The string to include between concatenated objects.</param>
	/// <returns>A concatenated string.</returns>
	public static string Join( this IEnumerable<string?>? Values, string Separator ) => Values is null ? string.Empty : string.Join(Separator, Values);

	/// <summary>
	/// Asynchronously awaits process closure.
	/// </summary>
	/// <param name="P">The process to wait for exit.</param>
	/// <returns>A task that waits for a process to successfully close.</returns>
	public static async Task WaitForExitAsync( this Process P ) {
		TaskCompletionSource TCS = new TaskCompletionSource();

		void ProcessExited( object? Sender, EventArgs E ) {
			TCS.TrySetResult();
		}

		P.EnableRaisingEvents = true;
		P.Exited += ProcessExited;

		await TCS.Task;
	}

	/// <summary>
	/// Asynchronously awaits process closure.
	/// </summary>
	/// <param name="P">The process to wait for exit.</param>
	/// <param name="TimeoutMS">The amount of time to wait (in milliseconds) before giving up. If <see langword="null"/>, the task will await indefinitely until the process closes.</param>
	/// <param name="CT">The cancellation token. Ignored if <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the process was successfully closed, otherwise <see langword="false"/> if the timeout was reached or the task was cancelled.</returns>
	public static async Task<bool> WaitForExitAsync( this Process P, int? TimeoutMS, CancellationToken? CT ) {
		if ( P.HasExited ) { return false; }

		TaskCompletionSource TCS = new TaskCompletionSource();

		void ProcessExited( object? Sender, EventArgs E ) {
			TCS.TrySetResult();
		}

		P.EnableRaisingEvents = true;
		P.Exited += ProcessExited;

		return await TCS.Task.Timeout(TimeoutMS, CT);
	}

	/// <summary>
	/// Asynchronously invokes the task, returning <see langword="true"/> if successfully ran in time, or <see langword="false"/> if the the given timeout (in milliseconds) was reached instead.
	/// </summary>
	/// <param name="T">The task to attempt to run.</param>
	/// <param name="TimeoutMS">The amount of time to wait (in milliseconds) before returning. If <see langword="null"/>, the task is just awaited normally.</param>
	/// <returns><see langword="true"/> if the original task successfully completed, otherwise <see langword="false"/> if the timeout was reached.</returns>
	public static async Task<bool> Timeout( this Task T, int? TimeoutMS ) {
		if ( TimeoutMS is not { } TMS ) { await T; return true; }
		return await Task.WhenAny(T, Task.Delay(TMS)) == T;
	}

	/// <summary>
	/// Asynchronously invokes the task, returning <see langword="true"/> if successfully ran in time, or <see langword="false"/> if the the given timeout (in milliseconds) was reached / the cancellation token was used.
	/// </summary>
	/// <param name="T">The task to attempt to run.</param>
	/// <param name="TimeoutMS">The amount of time to wait (in milliseconds) before returning. If <see langword="null"/>, the task is just awaited normally, only returning <see langword="false"/> if the cancellation token was used.</param>
	/// <param name="CT">The cancellation token. Ignored if <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the original task successfully completed, otherwise <see langword="false"/> if the timeout was reached or the cancellation token was used.</returns>
	public static async Task<bool> Timeout( this Func<CancellationToken?, Task> T, int? TimeoutMS, CancellationToken? CT ) {
		if (CT?.IsCancellationRequested ?? false) { return false; } //Return immediately if a cancellation is requested.
		if (TimeoutMS is { } TMS ) {
			Task WaitTask = CT is { } Token ? Task.Delay(TMS, Token) : Task.Delay(TMS);
			return await Task.WhenAny(T(CT), WaitTask) != WaitTask && !(CT?.IsCancellationRequested ?? false);
		}

		await T(CT);
		return !(CT?.IsCancellationRequested ?? false);
	}

	/// <summary>
	/// Asynchronously invokes the task, returning <see langword="true"/> if successfully ran in time, or <see langword="false"/> if the the given timeout (in milliseconds) was reached / the cancellation token was used.
	/// </summary>
	/// <param name="T">The task to attempt to run.</param>
	/// <param name="TimeoutMS">The amount of time to wait (in milliseconds) before returning. If <see langword="null"/>, the task is just awaited normally, only returning <see langword="false"/> if the cancellation token was used.</param>
	/// <param name="CT">The cancellation token. Ignored if <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if the original task successfully completed, otherwise <see langword="false"/> if the timeout was reached or the cancellation token was used.</returns>
	public static async Task<bool> Timeout( this Task T, int? TimeoutMS, CancellationToken? CT ) {
		if ( CT?.IsCancellationRequested ?? false ) { return false; } //Return immediately if a cancellation is requested.
		if ( TimeoutMS is { } TMS ) {
			return await Task.WhenAny(T, CT is { } Token ? Task.Delay(TMS, Token) : Task.Delay(TMS)) == T && !(CT?.IsCancellationRequested ?? false);
		}

		await T;
		return !(CT?.IsCancellationRequested ?? false);
	}

}

/// <summary>
/// Basic class reference to allow values to be passed through iterator methods.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
public class Out<T> {
	/// <summary>
	/// The value.
	/// </summary>
	public T Value { get; set; }

	/// <summary>
	/// Default constructor.
	/// </summary>
	/// <param name="InitialValue">The initial value.</param>
	public Out( T InitialValue ) => Value = InitialValue;

	/// <summary>
	/// Retrieves the current value of the given reference.
	/// </summary>
	/// <param name="O">The output reference to extract from.</param>
	public static implicit operator T( Out<T> O ) => O.Value;

	/// <summary>
	/// Constructs a new output reference.
	/// </summary>
	/// <param name="V">The value.</param>
	public static implicit operator Out<T>( T V ) => new Out<T>(V);
}