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
/// Converts an <see langword="object"/> to a boolean representing its nullability.
/// </summary>
public class NullabilityToBooleanConverter : ValueConverter<object?, bool> {

	/// <summary>
	/// The value to return when not <see langword="null"/>.
	/// </summary>
	public bool WhenNotNull { get; set; } = true;

	/// <summary>
	/// The value to return when <see langword="null"/>.
	/// </summary>
	public bool WhenNull { get; set; }

	/// <inheritdoc />
	public override bool CanForwardWhenNull => true;

	/// <inheritdoc />
	public override bool CanReverse => false;

	/// <inheritdoc />
	public override bool CanReverseWhenNull => false;

	/// <inheritdoc />
	public override bool Forward( object? From, object? Parameter = null, CultureInfo? Culture = null ) => WhenNotNull;

	/// <inheritdoc />
	public override bool ForwardWhenNull( object? Parameter = null, CultureInfo? Culture = null ) => WhenNull;

	/// <inheritdoc />
	public override object? Reverse( bool To, object? Parameter = null, CultureInfo? Culture = null ) => null;
}