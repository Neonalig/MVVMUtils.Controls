#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using JetBrains.Annotations;

#endregion

#pragma warning disable RXRA004 //Property can be expanded ;; Justification: Handled by Fody.PropertyChanged

namespace MVVMUtils.Controls; 

/// <summary>
/// Represents a user control for file system paths.
/// </summary>
public partial class PathBrowser : INotifyPropertyChanged {
	/// <summary>
	/// Default constructor.
	/// </summary>
	public PathBrowser() {
		InitializeComponent();
		ViewModel = (PathBrowser_ViewModel)DataContext;
		ViewModel.View = this;
		ViewModel.Path = Tag?.ToString();
		PropertyChanged += ( _, E ) => {
			Debug.WriteLine($"Property {E.PropertyName} was changed...");
			// ReSharper disable once ConvertSwitchStatementToSwitchExpression
			switch ( E.PropertyName ) {
				case nameof(Tag):
					ViewModel.Path = Tag?.ToString();
					break;
				case nameof(TestPath):
					ViewModel.Path = TestPath;
					break;
			}
			//if ( E.PropertyName == nameof(Tag) ) {
			//	Debug.WriteLine($"\tTag! (Val: {Tag})");
			//	ViewModel.Path = Tag?.ToString();
			//}
		};
		ViewModel.UpdateSuggestions(string.Empty);
		//Debug.WriteLine($"Provides Autocomplete? {ViewModel.ProvideAutocomplete}");
	}

	/// <summary>
	/// Gets or sets the test path.
	/// </summary>
	/// <value>
	/// The test path.
	/// </value>
	public string TestPath {
		get => (string)GetValue(TestPathProperty);
		set => SetValue(TestPathProperty, value);
	}

	/// <summary>
	/// The test path property
	/// </summary>
	public static readonly DependencyProperty TestPathProperty =
		DependencyProperty.Register(nameof(TestPath), typeof(string), typeof(PathBrowser), new PropertyMetadata(string.Empty));

	/// <summary>
	/// The current viewmodel.
	/// </summary>
	public PathBrowser_ViewModel ViewModel { get; }

	/// <summary>
	/// Event arguments for when the path is changed on a <see cref="PathBrowser"/>.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="NewPath">The raised event arguments.</param>
	public delegate void PathBrowser_PathChangedEventArgs( PathBrowser Sender, string? NewPath );

	/// <summary>
	/// Event raised when the path is changed.
	/// </summary>
	public event PathBrowser_PathChangedEventArgs PathChanged = delegate { };

	/// <summary>
	/// Raises the <see cref="PathChanged"/> event.
	/// </summary>
	/// <param name="NewPath">The new path.</param>
	[PropertyChanged.SuppressPropertyChangedWarnings] public void OnPathChanged( string? NewPath ) => PathChanged.Invoke(this, NewPath);

	/// <summary>
	/// Retrieves the <see cref="object"/> if it is type <typeparamref name="T"/> or <see langword="null"/>, throwing if neither.
	/// </summary>
	/// <typeparam name="T">The requested type.</typeparam>
	/// <param name="Obj">The <see langword="object"/> to check.</param>
	/// <exception cref="NotSupportedException">The <see cref="object"/> is neither of type <typeparamref name="T"/> or <see langword="null"/>.</exception>
	/// <returns>An instance of type <typeparamref name="T"/> or <see langword="null"/>.</returns>
	internal static T? AsOrNull<T>( object? Obj ) => Obj switch {
		null  => default,
		T Val => Val,
		_     => throw new NotSupportedException($"Object must be of type {typeof(T)} or null.")
	};


	/// <summary>
	/// Invoked when the browse button is clicked.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	void BrowseButton_OnClick( object Sender, RoutedEventArgs E ) {
		//ViewModel.
		switch ( ViewModel.Type ) {
			case PathUtils.PathType.File when PathUtils.OpenFileBrowseDialog(Name ?? "Select a file", ViewModel.File) is { } FI:
				ViewModel.File = FI;
				break;
			case PathUtils.PathType.Directory when PathUtils.OpenDirectoryBrowseDialog(Name ?? "Select a directory", ViewModel.Directory) is { } DI:
				ViewModel.Directory = DI;
				break;
		}
	}

	/// <summary>
	/// Invoked when the path <see cref="TextBox"/> is changed. Updates autocomplete suggestions.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	void TextBox_TextChanged( object Sender, TextChangedEventArgs E ) => ViewModel.UpdateSuggestions(((TextBox)Sender).Text);

	/// <summary>
	/// Clears keyboard focus from the current <see cref="Keyboard.FocusedElement"/>.
	/// </summary>
	public static void ClearKeyboardFocus() {
		if ( Keyboard.FocusedElement is { } FE ) {
			FE.RaiseEvent(new RoutedEventArgs(LostFocusEvent));
			Keyboard.ClearFocus();
		}
	}

	/// <summary>
	/// Invoked when a keypress is detected on the textbox. Used to autofill any present autocomplete.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	void TextBox_PreviewKeyDown( object Sender, KeyEventArgs E ) {
		if ( Sender is not TextBox TB || !E.KeyStates.HasFlag(KeyStates.Down) ) { return; }
		//Debug.WriteLine($"Pressed Key: {E.Key}");
		switch ( E.Key ) {
			case Key.Tab:
				//Debug.WriteLine("Autocomplete lol.");
				if ( ViewModel.ProvideAutocomplete ) {
					string CA = ViewModel.ClosestAutocomplete;
					ChangePath(CA);
					if ( ViewModel.Type == PathUtils.PathType.File && !CA.EndsWith('\\') ) {
						//ClearKeyboardFocus();
						E.Handled = false;
						//If the path browser is a file browser, and the currently completed path doesn't end with a '\', it is likely a file. Therefore, defocus the textbox.
					} else {
						E.Handled = true;
					}
					//Debug.WriteLine($"Wants {ViewModel.Type}, has {TB.Text}; CA: {ViewModel.ClosestAutocomplete}");
					//switch ( ViewModel.Type ) {
					//	case PathUtils.PathType.File when PathUtils.TryGetFileInfo(ViewModel.ClosestAutocomplete, out FileInfo FI) && FI.Exists:
					//		string CSFI = FI.GetCaseSensitiveFullName();
					//		if ( CSFI != TB.Text ) {
					//			ViewModel.Path = CSFI;
					//			TB.CaretIndex = CSFI.Length;
					//			E.Handled = true;
					//		}
					//		break;
					//	case PathUtils.PathType.Directory when PathUtils.TryGetDirectoryInfo(ViewModel.ClosestAutocomplete, out DirectoryInfo DI) && DI.Exists:
					//		string CSDI = DI.GetCaseSensitiveFullName();
					//		if ( CSDI != TB.Text ) {
					//			ViewModel.Path = CSDI;
					//			TB.CaretIndex = CSDI.Length;
					//			E.Handled = true;
					//		}
					//		break;
					//}
				} else {
					string CA = ViewModel.ClosestAutocomplete;
					if ( !string.IsNullOrEmpty(CA) ) {
						ViewModel.Path = CA;
						TB.CaretIndex = CA.Length;
						E.Handled = true;
					} else {
						E.Handled = false;
					}
				}

				break;
			case Key.Oem5:
			case Key.OemQuestion:
			case Key.OemBackslash:
				string T = TB.Text.Replace('/', '\\');
				if ( T.EndsWith('\\') ) { break; }

				if ( PathUtils.TryGetDirectoryInfo(T, out DirectoryInfo ADI) && ADI.Exists ) {
					//Last key was a '\', and so is likely a folder
					T = ADI.GetCaseSensitiveFullName().TrimEnd('\\') + '\\';
				}
				if ( T != TB.Text ) {
					ViewModel.Path = T;
					TB.CaretIndex = T.Length;
					E.Handled = true;
				}
				break;
			default:
				E.Handled = false;
				break;
		}
	}

	/// <summary>
	/// Invoked when the <see cref="TextBox"/> loses keyboard focus.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	void TextBox_LostKeyboardFocus( object Sender, KeyboardFocusChangedEventArgs E ) => ViewModel.ConcludeEdit();

	/// <summary>
	/// Manually changes the path, updating the caret position relative to the original position.
	/// </summary>
	/// <param name="NewPath">The new path to use.</param>
	public void ChangePath( string NewPath ) {
		int OldCaretIndex = PathTB.CaretIndex;
		bool MoveCaretToEnd = OldCaretIndex == PathTB.Text.Length;
		ViewModel.Path = NewPath;
		PathTB.CaretIndex = (MoveCaretToEnd ? NewPath.Length : OldCaretIndex).Clamp(0, NewPath.Length);
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged = delegate { };

	/// <summary>
	/// Invokes the <see cref="PropertyChanged"/> event handler.
	/// </summary>
	/// <param name="PropertyName">The name of the property that changed.</param>
	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged( [CallerMemberName] string? PropertyName = null ) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
}