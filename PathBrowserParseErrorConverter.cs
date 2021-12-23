#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System.Globalization;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Abstract converter for converting parse errors into value types.
/// </summary>
/// <typeparam name="T">The value type to convert into.</typeparam>
public abstract class PathBrowserParseErrorConverter<T> : ValueConverter<PathBrowser_ViewModel.ParseError, T> {

	/// <summary> <typeparamref name="T"/> returned when no flags are present. </summary>
	public T? ErrorNone { get; set; }

	/// <summary> <typeparamref name="T"/> returned when the <see cref="PathBrowser_ViewModel.ParseError.InvalidPath"/> flag is present. May be overriden by <see cref="PathBrowser_ViewModel.ParseError.InvalidParentDirectory"/> if present and <see cref="InvalidParentDirectoryHasPrecedence"/> is <see langword="true"/>.</summary>
	public T? ErrorInvalidPath { get; set; }

	/// <summary> <typeparamref name="T"/> returned when the <see cref="PathBrowser_ViewModel.ParseError.InvalidParentDirectory"/> flag is present. May be overriden by <see cref="PathBrowser_ViewModel.ParseError.InvalidPath"/> if present and <see cref="InvalidParentDirectoryHasPrecedence"/> is <see langword="false"/>.</summary>
	public T? ErrorInvalidParentDirectory { get; set; }

	/// <summary>
	/// Determines whether the <see cref="PathBrowser_ViewModel.ParseError.InvalidParentDirectory"/> flag has precedence over <see cref="PathBrowser_ViewModel.ParseError.InvalidPath"/>.
	/// </summary>
	public bool InvalidParentDirectoryHasPrecedence { get; set; }

	/// <inheritdoc />
	public override bool CanReverse => false;

	/// <inheritdoc />
	public override T? Forward( PathBrowser_ViewModel.ParseError From, object? Parameter = null, CultureInfo? Culture = null ) {
		if ( InvalidParentDirectoryHasPrecedence ) {
			if ( From.HasFlag(PathBrowser_ViewModel.ParseError.InvalidParentDirectory) ) {
				return ErrorInvalidParentDirectory;
			}

			if ( From.HasFlag(PathBrowser_ViewModel.ParseError.InvalidPath) ) {
				return ErrorInvalidPath;
			}
		}

		if ( From.HasFlag(PathBrowser_ViewModel.ParseError.InvalidPath) ) {
			return ErrorInvalidPath;
		}

		// ReSharper disable once ConvertIfStatementToReturnStatement
		if ( From.HasFlag(PathBrowser_ViewModel.ParseError.InvalidParentDirectory) ) {
			return ErrorInvalidParentDirectory;
		}

		return ErrorNone;
	}

	/// <inheritdoc />
	public override PathBrowser_ViewModel.ParseError Reverse( T To, object? Parameter = null, CultureInfo? Culture = null ) => PathBrowser_ViewModel.ParseError.None;
}