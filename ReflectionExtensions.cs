#region Copyright (C) 2017-2021  Starflash Studios
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License (Version 3.0)
// as published by the Free Software Foundation.
// 
// More information can be found here: https://www.gnu.org/licenses/gpl-3.0.en.html
#endregion

#region Using Directives

using System;
using System.Collections;
using System.Linq;
using System.Reflection;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

#endregion

namespace MVVMUtils.Controls; 

/// <summary>XML Documentation retrieval credits: <br/>
/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></summary>
public static class ReflectionExtensions {
	/// <summary>
	/// Retrieves all attributes of type <typeparamref name="T"/> on the given <see cref="Type"/>.
	/// </summary>
	/// <typeparam name="T">The attribute type.</typeparam>
	/// <param name="Tp">The parent <see cref="Type"/> to retrieve the attributes from.</param>
	/// <returns>All attributes of type <typeparamref name="T"/> on the given <see cref="Type"/>.</returns>
	public static T[] GetAttributes<T>( this Type Tp ) where T : Attribute => (T[])Attribute.GetCustomAttributes(Tp, typeof(T));

	/// <summary>
	/// Retrieves all attributes of type <typeparamref name="T"/> on the given <see cref="object"/>.
	/// </summary>
	/// <typeparam name="T">The attribute type.</typeparam>
	/// <param name="Obj">The <see cref="object"/> to retrieve the attributes from.</param>
	/// <returns>All attributes of type <typeparamref name="T"/> on the given <see cref="Type"/>.</returns>
	public static T[] GetAttributes<T>( this object Obj ) where T : Attribute => GetAttributes<T>(Obj.GetType());
		
	/// <summary>
	/// Attempts to retrieve the first attribute of type <typeparamref name="T"/> on the given <see cref="object"/>.
	/// </summary>
	/// <typeparam name="T">The attribute type.</typeparam>
	/// <param name="Tp">The parent <see cref="Type"/> to retrieve the attribute from.</param>
	/// <param name="Found">The found attribute instance.</param>
	/// <returns><see langword="true"/> if any attribute was found.</returns>
	public static bool TryGetAttribute<T>( this Type Tp, out T Found ) where T : Attribute {
		if ( GetAttributes<T>(Tp).FirstOrDefault() is { } F ) {
			Found = F;
			return true;
		}

		Found = default!;
		return false;
	}

	/// <summary>
	/// Attempts to retrieve the first attribute of type <typeparamref name="T"/> on the given <see cref="object"/>.
	/// </summary>
	/// <typeparam name="T">The attribute type.</typeparam>
	/// <param name="Obj">The <see cref="object"/> to retrieve the attribute from.</param>
	/// <param name="Found">The found attribute instance.</param>
	/// <returns><see langword="true"/> if any attribute was found.</returns>
	public static bool TryGetAttribute<T>( this object Obj, out T Found ) where T : Attribute => TryGetAttribute(Obj.GetType(), out Found);

	/// <summary>
	/// Retrieves all the attributes on the given <see langword="enum"/> value.
	/// </summary>
	/// <typeparam name="TEnum">The <see langword="enum"/> type.</typeparam>
	/// <typeparam name="TAttr">The <see cref="Attribute"/> type.</typeparam>
	/// <param name="EnumValue">The current <see langword="enum"/> value.</param>
	/// <returns>An array of all found attributes, or <see langword="null"/>.</returns>
	public static TAttr[] GetAttributes<TEnum, TAttr>( this TEnum EnumValue ) where TEnum : struct, Enum where TAttr : Attribute => EnumValue.GetType().GetMember(EnumValue.ToString()).Select(E => E.GetCustomAttributes<TAttr>(false)).Flatten().ToArray();

	//Type EnumUnderlying = typeof(TEnum).GetEnumUnderlyingType();
	//object? EnumValueForComparison = Cast(EnumValue, EnumUnderlying);
	////EqualityComparer<TEnum?> Comp = EqualityComparer<TEnum?>.Default;
	//IEqualityComparer EnumValComp = EnumUnderlying.GetEqualityComparer() ?? throw new NotSupportedException($"No default IEqualityComparer found for type {EnumUnderlying.Name}");
	//Debug.WriteLine($"Checking for attributes of type {typeof(TAttr)} on enum type {typeof(TEnum)} (Underlying: {EnumUnderlying.Name}; Equality Comparer: {EnumValComp}) -- User wants {EnumValue} ({EnumValueForComparison})...");
	//foreach (FieldInfo FI in typeof(TEnum).GetFields() ) {
	//	//Debug.WriteLine($"\tField: {FI.Name} (Literal: {FI.IsLiteral}, Val: {FI.GetValue(EnumValue)})::");
	//	if ( FI.GetValue(EnumValue) is { } Val && EnumUnderlying.IsAssignableTo(Val.GetType()) ) {
	//		Debug.WriteLine($"\t\tFound possible field {FI.Name} (= {Val} -- {Val.GetType().Name})");
	//		Debug.WriteLine($"\t\tUnderlying assignable ({Val.GetType().Name} is {EnumUnderlying.Name})? {EnumUnderlying.IsAssignableTo(Val.GetType())}");
	//		Debug.WriteLine($"\t\tEquivalent ({Val} == {EnumValueForComparison})? {EnumValComp.Equals(Val, EnumValueForComparison)}");
	//		if ( EnumUnderlying.IsAssignableTo(Val.GetType()) && EnumValComp.Equals(Val, EnumValueForComparison) ) {
	//			TAttr[] Output = (TAttr[])FI.GetCustomAttributes<TAttr>();
	//			Debug.WriteLine("\t\tCustom Attributes:");
	//			foreach(CustomAttributeData CAD in FI.CustomAttributes ) { //<-- Also irrelevant, doesn't contain what we expect (empty?)
	//				Debug.WriteLine($"\t\t\t{CAD.AttributeType.Name}:");
	//				foreach( CustomAttributeNamedArgument Arg in CAD.NamedArguments ) {
	//					Debug.WriteLine($"\t\t\t\tArg Name:{Arg.MemberName}");
	//					Debug.WriteLine($"\t\t\t\t\tField? {Arg.IsField}");
	//					Debug.WriteLine($"\t\t\t\t\tType: {Arg.TypedValue.ArgumentType}");
	//					Debug.WriteLine($"\t\t\t\t\tValue: {Arg.TypedValue.Value}");
	//				}
	//			}
	//			Debug.WriteLine($"\t\tAttr: {FI.Attributes}"); //<-- Irrelevant, different attributes (stuff like public, special name, etc. ; not what we are looking for)
	//			Debug.WriteLine($"\t\tRetrieved Attributes: '{string.Join("', '", Output.ToList())}'");
	//			//TODO: Correct field is being returned, but no attributes are being found.
	//			//TODO: See all returnable information of this FieldInfo, maybe that will help.
	//			//TODO: Perhaps try another approach? -- Iterate Enum.GetValues, check equivalence, go from there
	//			return Output;
	//		}
	//		Debug.WriteLine("\t\t\tNot Underlying type, or not equal to wanted value.");
	//		//Debug.WriteLine($"\t{Raw} ({Raw?.GetType()}) ;; (Enum type? {Raw is TEnum}) ;; (Underling? {EnumUnderlying.IsAssignableTo(Raw!.GetType())}");
	//		//return null;
	//	}
	//	//if (FI.IsLiteral && FI.GetRawConstantValue() is TEnum FIVal && Comp.Equals(FIVal, EnumValue)) {
	//	//	return (TAttr[])FI.GetCustomAttributes<TAttr>();
	//	//}
	//	return null;
	//}
	//return null;
	/// <summary>
	/// Forcefully casts the input <paramref name="Value"/> to the given compile-time generic <see cref="Type"/>.
	/// </summary>
	/// <typeparam name="TIn">The input value type.</typeparam>
	/// <typeparam name="TOut">The desired output type.</typeparam>
	/// <param name="Value">The input value to cast.</param>
	/// <returns>The cast value, or <see langword="default"/>/<see langword="null"/>.</returns>
	public static TOut? Cast<TIn, TOut>( this TIn Value ) => Convert.ChangeType(Value, typeof(TOut)) is TOut Out ? Out : default;


	/// <summary>
	/// Forcefully casts the input <paramref name="Value"/> to the given runtime <see cref="Type"/>.
	/// </summary>
	/// <typeparam name="TIn">The input value type.</typeparam>
	/// <param name="Value">The input value to cast.</param>
	/// <param name="DesiredType">The desired output type.</param>
	/// <returns>The cast value, or <see langword="default"/>/<see langword="null"/>.</returns>
	public static object? Cast<TIn>( this TIn Value, Type DesiredType ) => Convert.ChangeType(Value, DesiredType);

	/// <summary>
	/// Converts the generic method into a non-generic method with runtime-accessible <see cref="Type"/> instances.
	/// </summary>
	/// <param name="MethodInfo">The original generic method.</param>
	/// <param name="Types">The types to input.</param>
	/// <returns>The non-generic method.</returns>
	public static MethodInfo GetNonGeneric( this MethodInfo MethodInfo, params Type[] Types ) => MethodInfo.MakeGenericMethod(Types);

	/// <summary>
	/// Converts the generic type into a non-generic type with runtime-accessible <see cref="Type"/> instances.
	/// </summary>
	/// <param name="Type">The original generic type.</param>
	/// <param name="Types">The types to input.</param>
	/// <returns>The non-generic type.</returns>
	public static Type GetNonGeneric( this Type Type, params Type[] Types ) => Type.MakeGenericType(Types);

	// ReSharper disable once SuspiciousTypeConversion.Global
	/// <summary>
	/// Gets the default <see cref="IEqualityComparer"/> for the given <see cref="Type"/>.
	/// </summary>
	/// <param name="T">The type to retrieve an equality comparer for.</param>
	/// <returns>The default <see cref="IEqualityComparer"/> for the given <see cref="Type"/>.</returns>
	public static IEqualityComparer? GetEqualityComparer( this Type T ) => GetNonGeneric(typeof(EqualityComparer<>), T).GetProperty("Default")?.GetGetMethod()?.Invoke(null, null) as IEqualityComparer;

	/// <summary>
	/// Attempts to retrieve the first attribute on the given <see langword="enum"/> value.
	/// </summary>
	/// <typeparam name="TEnum">The <see langword="enum"/> type.</typeparam>
	/// <typeparam name="TAttr">The <see cref="Attribute"/> type.</typeparam>
	/// <param name="EnumValue">The current <see langword="enum"/> value.</param>
	/// <param name="Found">The found attribute instance.</param>
	/// <returns>The found attribute, or <see langword="null"/>.</returns>
	public static bool TryGetAttribute<TEnum, TAttr>( this TEnum EnumValue, out TAttr Found ) where TEnum : struct, Enum where TAttr : Attribute {
		if ( GetAttributes<TEnum, TAttr>(EnumValue).FirstOrDefault() is { } Attr ) {
			Found = Attr;
			return true;
		}

		Found = default!;
		return false;
	}

	/// <summary>
	/// Provides the documentation comments for a specified field.
	/// </summary>
	/// <param name="FieldInfo">The <paramref name="FieldInfo"/> (reflection data) of the field to find documentation for.</param>
	/// <returns>The XML fragment describing the field.</returns>
	public static XmlElement? GetDocumentation( this FieldInfo FieldInfo ) => XmlFromName(FieldInfo.DeclaringType, 'F', FieldInfo.Name);


	/// <summary>
	/// Provides the documentation comments for a specified property.
	/// </summary>
	/// <param name="PropertyInfo">The <paramref name="PropertyInfo"/> (reflection data) of the property to find documentation for.</param>
	/// <returns>The XML fragment describing the property.</returns>
	public static XmlElement? GetDocumentation( this PropertyInfo PropertyInfo ) => XmlFromName(PropertyInfo.DeclaringType, 'P', PropertyInfo.Name);

	/// <summary>
	/// Provides the documentation comments for a specific method
	/// </summary>
	/// <remarks>Credits: <br/>
	/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
	/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></remarks>
	/// <param name="MethodInfo">The <paramref name="MethodInfo"/> (reflection data) of the member to find documentation for</param>
	/// <returns>The XML fragment describing the method</returns>
	public static XmlElement? GetDocumentation( this MethodInfo MethodInfo ) {
		// Calculate the parameter string as this is in the member name in the XML
		string ParametersString = string.Empty;
		foreach ( ParameterInfo ParameterInfo in MethodInfo.GetParameters() ) {
			if ( ParametersString.Length > 0 ) {
				ParametersString += ",";
			}

			ParametersString += ParameterInfo.ParameterType.FullName;
		}

		//AL: 15.04.2008 ==> BUG-FIX remove “()” if parametersString is empty
		return ParametersString.Length switch {
			> 0 => XmlFromName(MethodInfo.DeclaringType, 'M', MethodInfo.Name + "(" + ParametersString + ")"),
			_   => XmlFromName(MethodInfo.DeclaringType, 'M', MethodInfo.Name)
		};
	}

	/// <summary>
	/// Provides the documentation comments for a specific member
	/// </summary>
	/// <remarks>Credits: <br/>
	/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
	/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></remarks>
	/// <param name="MemberInfo">The <paramref name="MemberInfo"/> (reflection data) or the member to find documentation for</param>
	/// <returns>The XML fragment describing the member</returns>
	public static XmlElement? GetDocumentation( this MemberInfo MemberInfo ) =>
		// First character [0] of member type is prefix character in the name in the XML
		XmlFromName(MemberInfo.DeclaringType, MemberInfo.MemberType.ToString()[0], MemberInfo.Name);

	/// <summary>
	/// Returns the Xml documentation summary comment for the member
	/// </summary>
	/// <remarks>Credits: <br/>
	/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
	/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></remarks>
	/// <param name="MemberInfo"></param>
	/// <returns></returns>
	public static string GetSummary( this MemberInfo MemberInfo ) {
		XmlElement? Element = MemberInfo.GetDocumentation();
		XmlNode? SummaryElm = Element?.SelectSingleNode("summary");
		return SummaryElm switch {
			null => "",
			_    => SummaryElm.InnerText.Trim()
		};
	}

	/// <summary>
	/// Provides the documentation comments for a specific type
	/// </summary>
	/// <remarks>Credits: <br/>
	/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
	/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></remarks>
	/// <param name="Type"><see cref="Type"/> to find the documentation for</param>
	/// <returns>The XML fragment that describes the type</returns>
	public static XmlElement? GetDocumentation( this Type Type ) =>
		XmlFromName(Type, 'T', "");
	// Prefix in type names is T

	/// <summary>
	/// Gets the summary portion of a type's documentation or returns an empty string if not available
	/// </summary>
	/// <remarks>Credits: <br/>
	/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
	/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></remarks>
	/// <param name="Type"></param>
	/// <returns></returns>
	public static string GetSummary( this Type Type ) {
		XmlElement? Element = Type.GetDocumentation();
		XmlNode? SummaryElm = Element?.SelectSingleNode("summary");
		return SummaryElm switch {
			null => "",
			_    => SummaryElm.InnerText.Trim()
		};
	}

	/// <summary>
	/// Obtains the XML Element that describes a reflection element by searching the 
	/// members for a member that has a name that describes the element.
	/// </summary>
	/// <remarks>Credits: <br/>
	/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
	/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></remarks>
	/// <param name="Type">The type or parent type, used to fetch the assembly</param>
	/// <param name="Prefix">The prefix as seen in the name attribute in the documentation XML</param>
	/// <param name="Name">Where relevant, the full name qualifier for the element</param>
	/// <returns>The member that has a name that describes the specified reflection element</returns>
	static XmlElement? XmlFromName( this Type? Type, char Prefix, string Name ) {
		if ( Type is null ) { return null; }

		string FullName = string.IsNullOrEmpty(Name)
			? $"{Prefix}:{Type.FullName}"
			: $"{Prefix}:{Type.FullName}.{Name}";

		XmlDocument XMLDocument = XmlFromAssembly(Type.Assembly);

		XmlElement? MatchedElement = XMLDocument["doc"]?["members"]?.SelectSingleNode($"member[@name='{FullName}']") as XmlElement;

		return MatchedElement;
	}

	/// <summary>
	/// A cache used to remember Xml documentation for assemblies
	/// </summary>
	static readonly Dictionary<Assembly, XmlDocument> _Cache = new Dictionary<Assembly, XmlDocument>();

	/// <summary>
	/// A cache used to store failure exceptions for assembly lookups
	/// </summary>
	static readonly Dictionary<Assembly, Exception> _FailCache = new Dictionary<Assembly, Exception>();

	/// <summary>
	/// Obtains the documentation file for the specified assembly
	/// </summary>
	/// <remarks>This version uses a cache to preserve the assemblies, so that 
	/// the XML file is not loaded and parsed on every single lookup.
	/// <para/>Credits: <br/>
	/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
	/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></remarks>
	/// <param name="Assembly">The assembly to find the XML document for</param>
	/// <returns>The XML document</returns>
	public static XmlDocument XmlFromAssembly( this Assembly Assembly ) {
		if ( _FailCache.ContainsKey(Assembly) ) {
			throw _FailCache[Assembly];
		}

		try {

			if ( !_Cache.ContainsKey(Assembly) ) {
				// load the document into the cache
				_Cache[Assembly] = XmlFromAssemblyNonCached(Assembly);
			}

			return _Cache[Assembly];
		} catch ( Exception Exception ) {
			_FailCache[Assembly] = Exception;
			throw;
		}
	}

	/// <summary>
	/// Loads and parses the documentation file for the specified assembly
	/// </summary>
	/// <remarks>Credits: <br/>
	/// <c>Gripka, B</c>; <c>Rab</c> (2019) Programmatically get Summary comments at runtime (Version 2.0) [Source code].
	/// <br/><see href="https://stackoverflow.com/a/54009314/11519246"/></remarks>
	/// <param name="Assembly">The assembly to find the XML document for</param>
	/// <returns>The XML document</returns>
	static XmlDocument XmlFromAssemblyNonCached( Assembly Assembly ) {
		string AssemblyFilename = Assembly.Location;

		if ( !string.IsNullOrEmpty(AssemblyFilename) ) {
			StreamReader StreamReader;

			try {
				StreamReader = new StreamReader(Path.ChangeExtension(AssemblyFilename, ".xml"));
			} catch ( FileNotFoundException Exception ) {
				throw new Exception("XML documentation not present (make sure it is turned on in project properties when building)", Exception);
			}

			XmlDocument XMLDocument = new XmlDocument();
			XMLDocument.Load(StreamReader);
			return XMLDocument;
		}

		throw new Exception("Could not ascertain assembly filename", null);
	}
}