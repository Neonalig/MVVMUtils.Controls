﻿#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System.Globalization;
using System.IO;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Converts (nullable) <see cref="string"/> instances to (nullable) <see cref="DirectoryInfo"/> instances.
/// </summary>
public class StringToDirectoryInfoConverter : ValueConverter<string?, DirectoryInfo?> {

	/// <inheritdoc />
	public override bool CanReverse => true;

	/// <inheritdoc />
	public override bool CanForwardWhenNull => true;

	/// <inheritdoc />
	public override bool CanReverseWhenNull => true;

	/// <inheritdoc />
	public override DirectoryInfo? Forward( string? From, object? Parameter = null, CultureInfo? Culture = null ) => From?.GetDirectoryInfoOrNull();

	/// <inheritdoc />
	public override string? Reverse( DirectoryInfo? To, object? Parameter = null, CultureInfo? Culture = null ) => To?.FullName;
}

/// <summary> Functional inverse of <see cref="StringToDirectoryInfoConverter"/>. </summary>
public class DirectoryInfoToStringConverter : ReverseValueConverter<StringToDirectoryInfoConverter, string?, DirectoryInfo?> { }