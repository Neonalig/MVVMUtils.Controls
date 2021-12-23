#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System.Linq;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using JetBrains.Annotations;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Interaction logic for TagBox.xaml
/// </summary>
public partial class TagBox : INotifyPropertyChanged {
	/// <summary>
	/// Default constructor.
	/// </summary>
	public TagBox() {
		InitializeComponent();
		ViewModel = (TagBox_ViewModel)DataContext;
		ViewModel.View = this;
		PropertyChanged += ( _, E ) => {
			if ( E.PropertyName == nameof(TagsImpure) ) {
#pragma warning disable 8604
				string[] NewTags = TagsImpure.ToArray();
#pragma warning restore 8604
				Debug.WriteLine($"Updating tags changed through XAML bindings... (Old Value: '{string.Join("', '", ViewModel.Tags)}' ;; New Value: '{string.Join("', '", NewTags)}')");
				ViewModel.Tags.FakeSet(NewTags.Select(S => new Tag(S, ViewModel)));
			}
			//get => new ObservableCollection<string>(ViewModel.Tags.Select(T => T.Value));
			//set => ViewModel.Tags.FakeSet(value.Select(Str => new Tag(Str, ViewModel)));
		};
	}

	/// <summary>
	/// Event arguments for when tags are changed.
	/// </summary>
	/// <param name="TB">The sender.</param>
	/// <param name="Tags">The current collection of tags at the invoked point in time.</param>
#pragma warning disable CA1711 //Identifiers should not have incorrect suffix
	public delegate void TagsChangedEventArgs( TagBox TB, IList<Tag> Tags );
#pragma warning restore CA1711 //Identifiers should not have incorrect suffix

	/// <summary>
	/// Invoked when tags are changed.
	/// </summary>
	public event TagsChangedEventArgs TagsChanged = delegate { };

	/// <summary>
	/// Invokes the <see cref="TagsChanged"/> event.
	/// </summary>
	/// <param name="TB">The sender.</param>
	/// <param name="Tags">The current collection of tags at the invoked point in time.</param>
	[PropertyChanged.SuppressPropertyChangedWarnings] public void OnTagsChanged( TagBox TB, IList<Tag> Tags ) => TagsChanged.Invoke(TB, Tags);

	/// <summary>
	/// The viewmodel.
	/// </summary>
	public TagBox_ViewModel ViewModel { get; }


	/// <inheritdoc cref="TagBox_ViewModel.DefaultTagValue"/>
	public string DefaultTagValue {
		get => ViewModel.DefaultTagValue;
		set => ViewModel.DefaultTagValue = value;
	}

	///// <inheritdoc cref="TagBox_ViewModel.Tags"/>
	//public ObservableCollection<string> TagsImpure { get; set; }

	/// <inheritdoc cref="TagBox_ViewModel.Tags"/>
	public IList<string> TagsImpure {
		get => (IList<string>)GetValue(TagsImpureProperty);
		set => SetValue(TagsImpureProperty, value);
	}

	/// <summary>
	/// Invoked when the <see cref="TagsImpureProperty"/> is changed.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	static void TagsImpureProperty_PropertyChanged( DependencyObject Sender, DependencyPropertyChangedEventArgs E ) {
		//Debug.WriteLine($"Changed from sender {Sender} with args {E}");
		if ( Sender is not TagBox TB || E.NewValue is not IList<string> StrLs ) {
			Debug.WriteLine("Unexpected.");
			return;
		}
		TB.ViewModel.Tags.FakeSet(StrLs.Select(S => new Tag(S, TB.ViewModel)));
		//((TagBox)Sender).ViewModel.Tags.FakeSet((ObservableCollection<string>)E.NewValue);
	}

	/// <summary>
	/// <see cref="DependencyProperty"/> for the TagsImpure collection.
	/// </summary>
	public static readonly DependencyProperty TagsImpureProperty = DependencyProperty.Register(nameof(TagsImpure), typeof(IList<string>), typeof(TagBox), new PropertyMetadata(new List<string>(), TagsImpureProperty_PropertyChanged));

	//public ObservableCollection<Tag> TagsImpure {
	//	get => ViewModel.TagsImpure;
	//	set => ViewModel.TagsImpure.FakeSet(value);
	//}

	/// <summary>
	/// A collection of characters to split tags upon. For a smart separation upon space (' '), see: <see cref="SeparateOnSpace"/>.
	/// </summary>
	public ObservableCollection<char> SeparateCharacters = new ObservableCollection<char>();

	/// <summary>
	/// Whether to separate tags upon space (' '), keeping quotation depth in mind.
	/// </summary>
	public bool SeparateOnSpace { get; set; }

	/// <summary>
	/// Raised when the text is being changed within a tag.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	void Tag_OnTextChanged( object Sender, TextChangedEventArgs E ) {
		Debug.WriteLine("TextChanged");

		if ( Sender is not TextBox TB ) { return; }
		string TBText = TB.Text;
		//if ( _OldTagText is null ) { _OldTagText = TBText; return; }

		if ( TB.TemplatedParent is not ContentPresenter { Content: Tag OrigTag } ) {
			Debug.WriteLine("No old tag found. No changes will be made.", "WARNING");
			return;
		}

		int OrigTagIndex = ViewModel.Tags.IndexOf(OrigTag);

		Debug.WriteLine("Checking for smart split requirements...");
		Out<bool> SmartSplitNeeded = false.AsOut();
		string[] Splits = SmartSpaceInternalSplit(TBText, SmartSplitNeeded).ToArray();
		if ( SmartSplitNeeded.Value ) {
			Debug.WriteLine($"Required. Made splits were '{string.Join("','", Splits)}'");
			int I = OrigTagIndex;
			ViewModel.Tags.RemoveAt(I);
			foreach ( string Spl in Splits ) {
				ViewModel.Tags.Insert(I, new Tag(Spl, ViewModel));
				I++;
			}
			FocusNthTagTextBox(I - 1, ForcedCaretPosition.Start);
		} else {
			Debug.WriteLine("Not required.");
		}
	}

	/// <summary>
	/// Attempts to gain keyboard focus on the nth tag's textbox.
	/// </summary>
	/// <param name="Index">The index to focus.</param>
	/// <param name="CaretPos">The desired position of the caret.</param>
	internal void FocusNthTagTextBox( int Index, ForcedCaretPosition CaretPos ) {
		if ( Index < 0 || Index >= ViewModel.Tags.Count ) {
			Debug.WriteLine($"Attempt to focus Tag#{Index} would result in OOB. Ignored.");
			return;
		}
		//Debug.WriteLine("Iterating through visual children for focus...");
		int I = 0;
		foreach ( Border TagHolder in LV.GetElementsOfType<Border>() ) { //<-- parent holding the tag
			if ( I < Index ) {
				//Debug.WriteLine($"Skip {I}");
				I++;
				continue;
			}
			//Debug.WriteLine($"Check {I}");

			if ( TagHolder.Child is not DockPanel DP ) { Debug.WriteLine("Uh oh..."); return; }
			if ( DP.TryFindFirstVisualChild(out TextBox DPTB) ) {
				Debug.WriteLine($"Focus: {DPTB}");
				_ = DPTB.Focus();
				DPTB.MoveCaret(CaretPos);
			} else {
				Debug.WriteLine("Huh.");
			}
			break;
		}
	}

	/// <summary>
	/// Removes the focus from any <see cref="Tag"/> TextBoxes.
	/// </summary>
	internal void DefocusTagTextBox() => Keyboard.ClearFocus();

	/// <summary>
	/// Invoked when the 'Add' button is pressed.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	void AddButton_Click( object Sender, RoutedEventArgs E ) {
		ViewModel.Tags.Add(new Tag(DefaultTagValue, ViewModel));
		FocusNthTagTextBox(ViewModel.Tags.Count - 1, ForcedCaretPosition.End);
	}

	/// <summary>
	/// Splits the string into multiple strings dependent on the presence of spaces in the middle of the text.
	/// </summary>
	/// <param name="Input">The input text to split.</param>
	/// <param name="ChangesNeeded">Whether any splits were actually made.</param>
	/// <returns>The split strings. Ignore if <see cref="Out{T}.Value"/> is <see langword="false"/>.</returns>
	static IEnumerable<string> SmartSpaceInternalSplit( string Input, Out<bool> ChangesNeeded ) {
		//Input = Input.Trim(' ');
		ChangesNeeded.Value = false;
		int L = Input.Length, NextStart = 0;
		bool InQuotes = false;
		Debug.WriteLine($"Smart splitting '{Input}'.");
		for ( int I = 0; I < L; I++ ) {
			char C = Input[I];
			Debug.WriteLine($"\t{I}:'{C}'");
			switch ( C ) {
				case '"':
					Debug.WriteLine($"\t\tNow In Quotes: {InQuotes}");
					InQuotes = !InQuotes;
					break;
				case ' ':
					if ( !InQuotes ) {
						Debug.WriteLine($"\t\tSpace not in quotes (split from {NextStart} to {I}).");
						ChangesNeeded.Value = true;
						yield return Input[NextStart..I];
						NextStart = I + 1; //+1 to skip the ' '
					} else {
						Debug.WriteLine("\t\tSpace, but ignored (in quotes).");
					}
					break;
				default:
					continue;
			}
		}
		if ( NextStart < L ) {
			Debug.WriteLine($"\tFinishing up (return from {NextStart} to {L - 1})");
			yield return Input[NextStart..];
		} else if ( NextStart == L ) {
			yield return string.Empty; //Space at end needs empty split
		}
	}

	/// <summary>
	/// Invoked when a key is pressed in the tag <see cref="TextBox"/>.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	void Tag_PreviewKeyDown( object Sender, KeyEventArgs E ) { //If backspace is pressed and the tag is empty, delete it.
		E.Handled = false;
		if ( Sender is not TextBox TB ) { return; }
		bool Empty = string.IsNullOrEmpty(TB.Text);
		if ( TB.TemplatedParent is not ContentPresenter { Content: Tag T } ) { return; }
		switch ( E.Key ) {
			case Key.Delete:
			case Key.Back:
				if ( Empty ) {
					RemoveEmpty();
				}
				break;
			case Key.Enter:
				//case Key.Return:
				if ( Empty ) {
					RemoveEmpty();
				} else {
					FinaliseFilled();
				}
				break;
			case Key.Space when Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift):
				TB.Text += ' '; //<-- Not actually a classic space
				TB.MoveCaret(ForcedCaretPosition.End);
				E.Handled = true;
				break;
		}

		void RemoveEmpty() {
			Debug.WriteLine("Deleting empty tag on user input.");
			int I = ViewModel.Tags.IndexOf(T);
			ViewModel.Tags.RemoveAt(I);
			FocusNthTagTextBox(I - 1, ForcedCaretPosition.End);
			E.Handled = true;
		}

		void FinaliseFilled() {
			Debug.WriteLine("Finalising user input.");
			int I = ViewModel.Tags.IndexOf(T);
			ViewModel.Tags[I] = new Tag(TB.Text, T.Host);
			//DefocusTagTextBox();
			E.Handled = true;
		}
	}

	/// <summary>
	/// Invoked when the tag <see cref="TextBox"/> loses keyboard focus.
	/// </summary>
	/// <param name="Sender">The event raiser.</param>
	/// <param name="E">The raised event arguments.</param>
	void Tag_LostKeyboardFocus( object Sender, KeyboardFocusChangedEventArgs E ) {
		if ( Sender is TextBox TB ) {
			if ( !string.IsNullOrEmpty(TB.Text) ) { return; }
			if ( TB.TemplatedParent is ContentPresenter { Content: Tag T } ) {
				Debug.WriteLine("Deleting new blank tag on user inaction.");
				E.Handled = ViewModel.Tags.Remove(T);
				//FocusNthTagTextBox(I - 1, ForcedCaretPosition.End);
				//E.Handled = true;
			}
		}
	}

	/// <inheritdoc/>
	public event PropertyChangedEventHandler? PropertyChanged = delegate { };

	/// <summary>
	/// Invokes the <see cref="PropertyChanged"/> event handler.
	/// </summary>
	/// <param name="PropertyName">The name of the changed property.</param>
	[NotifyPropertyChangedInvocator]
	protected virtual void OnPropertyChanged( [CallerMemberName] string? PropertyName = null ) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
}