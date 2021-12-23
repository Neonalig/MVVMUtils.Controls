#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System;
using System.Collections.Generic;

using Microsoft.Toolkit.Mvvm.Input;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Represents a <see cref="TagBox"/> tag.
/// </summary>
public class Tag : Reactive, IEquatable<Tag?> {
	/// <summary>
	/// The tag.
	/// </summary>
	public string Value { get; set; }

	/// <summary>
	/// The <see cref="TagBox"/> host.
	/// </summary>
	public TagBox_ViewModel? Host { get; set; }

	/// <summary>
	/// Command invoked when the delete button is pressed.
	/// </summary>
	public RelayCommand DeleteCommand { get; }

	/// <summary>
	/// Default constructor.
	/// </summary>
	/// <param name="Value">The initial tag.</param>
	/// <param name="Host">The host.</param>
	public Tag( string? Value, TagBox_ViewModel? Host = null ) {
		this.Value = Value?.Replace(' ', ' ') ?? string.Empty;
		this.Host = Host;

		void DeleteCommand_Execute() => _ = Host?.Remove(this);
		bool DeleteCommand_CanExecute() => Host is not null;

		DeleteCommand = new RelayCommand(DeleteCommand_Execute, DeleteCommand_CanExecute);
	}

	/// <summary>
	/// Parameterless constructor. Equivalent to <see cref="Tag(string?, TagBox_ViewModel?)"/>, where <see langword="null"/> is passed as the <c>Value</c> and <c>Host</c>.
	/// </summary>
	public Tag() : this(null, null) { }

	/// <summary>
	/// Constructs a new instance of the <see cref="Tag"/> class.
	/// </summary>
	/// <param name="Value">The initial value of the constructed tag.</param>
	public static implicit operator Tag( string Value ) => new Tag(Value);

	/// <summary>
	/// Retrieves the <see cref="Value"/>.
	/// </summary>
	/// <param name="T">The tag.</param>
	public static implicit operator string( Tag T ) => T.Value.Replace(' ', ' ');

	/// <inheritdoc/>
	public override string ToString() => Value;

	#region IEquatable

	/// <inheritdoc/>
	public override bool Equals( object? Obj ) => Equals(Obj as Tag);

	/// <inheritdoc/>
	public bool Equals( Tag? Other ) => Other != null && Value == Other.Value;

	/// <inheritdoc/>
	public override int GetHashCode() => Value.GetHashCode();

	/// <summary>
	/// Implementation of the equality (<c>==</c>) <see langword="operator"/>, equivalent to invoking <see cref="EqualityComparer{T}.Equals(T, T)"/> via <see cref="EqualityComparer{T}.Default"/>. </summary>
	/// <param name="Left">The left item.</param>
	/// <param name="Right">The right item.</param>
	/// <returns><see langword="true"/> if the items are considered equivalent.</returns>
	public static bool operator ==( Tag? Left, Tag? Right ) => EqualityComparer<Tag>.Default.Equals(Left, Right);

	/// <summary>
	/// Implementation of the inequality (<c>!=</c>) <see langword="operator"/>. Functional inverse of the equality operator (<c>!</c> <see cref="operator ==(Tag, Tag)"/>). </summary>
	/// <param name="Left">The left item.</param>
	/// <param name="Right">The right item.</param>
	/// <returns><see langword="true"/> if the items are considered NOT equivalent.</returns>
	public static bool operator !=( Tag? Left, Tag? Right ) => !(Left == Right);

	#endregion
}