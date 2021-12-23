#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows.Controls;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Viewmodel logic for <see cref="TagBox"/>.xaml.cs
/// </summary>
public class TagBox_ViewModel : ViewModel<TagBox> {
	/// <summary>
	/// The collection of current tags.
	/// </summary>
	public ObservableCollection<Tag> Tags { get; }

	/// <summary>
	/// A collection of current tags and an 'Add' button. For XAML purposes only.
	/// </summary>
	public ObservableCollection<object> TagsAndButton { get; } = new ObservableCollection<object>();

	/// <summary>
	/// Whether to ignore the next NotifyCollectionChanged event.
	/// </summary>
	bool _IgnoreChange;

	/// <summary>
	/// Default constructor.
	/// </summary>
	public TagBox_ViewModel() {
		Tags = new ObservableCollection<Tag> {
			new Tag("help", this),
			new Tag("lol", this),
			new Tag("the egg", this),
			new Tag("requires", this),
			new Tag("sustenance", this)
		};

		void Tags_CollectionChanged( object? Sender, NotifyCollectionChangedEventArgs Args ) {
			if ( _IgnoreChange ) { return; }
			_IgnoreChange = true;
			View.OnTagsChanged(View, Tags);
			lock ( TagsAndButton ) {
				TagsAndButton.Clear();
				TagsAndButton.AddRange(Tags);
				if ( View is null ) {
					Debug.WriteLine("View has not yet been set.", "WARNING");
				} else {
					TagsAndButton.Add(View.FindResource<Button>("AddButtonRes"));
				}
				//TagsAndButton.Add(new Button { Content = "Add" });
			}
			_IgnoreChange = false;
		}

		Tags.CollectionChanged += Tags_CollectionChanged;

		void TagsAndButton_CollectionChanged( object? Sender, NotifyCollectionChangedEventArgs Args ) {
			if ( _IgnoreChange ) { return; }
			_IgnoreChange = true;
			lock ( Tags ) {
				Tags.Clear();
				foreach ( object O in TagsAndButton ) {
					switch ( O ) {
						case Tag T:
							Tags.Add(T);
							break;
					}
				}
			}
			_IgnoreChange = false;
		}

		TagsAndButton.CollectionChanged += TagsAndButton_CollectionChanged;

		PropertyChanged += ( _, E ) => {
			switch ( E.PropertyName ) {
				case nameof(View):
					Debug.WriteLine("View was updated.", "SUCCESS");
					Tags.Add(new Tag("PING", null)); //<-- This should never be visible
					Tags.RemoveAt(Tags.Count - 1);
					break;
			}
		};

		//Below is used to force update the TagsAndButton collection
		//Tags.RemoveAt(0);
	}

	/// <summary>
	/// Removes the first occurrence of the specified tag from the <see cref="Tags"/> collection.
	/// </summary>
	/// <param name="T">The tag to attempt to remove.</param>
	/// <returns><see langword="true"/> if successfully removed.</returns>
	public bool Remove( Tag T ) {
		Debug.WriteLine($"Removing tag {T}...");
		return Tags.Remove(T);
	}

	/// <summary>
	/// The default text used for new tags.
	/// </summary>
	public string DefaultTagValue { get; set; } = string.Empty;
}