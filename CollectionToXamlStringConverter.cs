#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System.Collections;
using System.Globalization;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Stores a collection as a string.
/// </summary>
public class CollectionToXamlStringConverter : ValueConverter<IList, string> {

	/// <summary>
	/// The character used for element separation purposes.
	/// </summary>
	public char SeparationCharacter { get; set; } = (char)30;

	/// <inheritdoc />
	public override bool CanReverse => true;

	/// <inheritdoc />
	public override string? Forward( IList From, object? Parameter = null, CultureInfo? Culture = null ) => string.Join(SeparationCharacter, From);

	/// <inheritdoc />
	public override IList? Reverse( string To, object? Parameter = null, CultureInfo? Culture = null ) => To.Split(SeparationCharacter);
}