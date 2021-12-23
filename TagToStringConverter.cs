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
/// Converts <see cref="Tag"/> instances to <see cref="string"/> instances.
/// </summary>
public class TagToStringConverter : ValueConverter<Tag, string> {

	/// <inheritdoc />
	public override bool CanReverse => true;

	/// <inheritdoc />
	public override string Forward( Tag From, object? Parameter = null, CultureInfo? Culture = null ) => From.Value.Replace(' ', ' ');

	/// <inheritdoc />
	public override Tag Reverse( string To, object? Parameter = null, CultureInfo? Culture = null ) => new Tag(To, Parameter as TagBox_ViewModel);
}