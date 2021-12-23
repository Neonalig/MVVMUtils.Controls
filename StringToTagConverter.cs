#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

namespace MVVMUtils.Controls; 

/// <summary>
/// Inverse of <see cref="TagToStringConverter"/> (which '<inheritdoc cref="TagToStringConverter"/>')
/// </summary>
public class StringToTagConverter : ReverseValueConverter<TagToStringConverter, Tag, string> { }