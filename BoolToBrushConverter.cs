#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System.Globalization;
using System.Windows.Media;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Converts boolean values to brushes.
/// </summary>
public class BoolToBrushConverter : ValueConverter<bool, Brush> {
	/// <summary>
	/// The brush returned when the converted boolean value is <see langword="true"/>.
	/// </summary>
	public Brush? True { get; set; }

	/// <summary>
	/// The brush returned when the converted boolean value is <see langword="false"/>.
	/// </summary>
	public Brush? False { get; set; }

	/// <inheritdoc />
	public override bool CanReverse => false;

	/// <inheritdoc />
	public override Brush? Forward( bool From, object? Parameter = null, CultureInfo? Culture = null ) => From ? True : False;

	/// <inheritdoc />
	public override bool Reverse( Brush To, object? Parameter = null, CultureInfo? Culture = null ) => false;
}