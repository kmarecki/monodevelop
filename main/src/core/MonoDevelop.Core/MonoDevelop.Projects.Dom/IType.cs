//
// IType.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom
{
	public enum TypeKind 
	{
		Definition,
		GenericInstantiation,
		GenericParameter
	}
	
	[Flags]
	public enum TypeModifier {	
		None                      = 0,
		HasOnlyHiddenConstructors = 1 // used for private & internal constructors in assemblies
	}
	
	public interface IType : ITypeParameterMember, IEquatable<IType>
	{
		string Namespace {
			get;
		}
		
		string DecoratedFullName {
			get;
		}
		
		TypeModifier TypeModifier {
			get;
		}
		
		new ProjectDom SourceProjectDom {
			get;
			set;
		}
		
		SolutionItem SourceProject {
			get;
		}
		
		ICompilationUnit CompilationUnit {
			get;
			set;
		}
		
		ClassType ClassType {
			get;
		}
		
		IReturnType BaseType {
			get;
			set;
		}
		IEnumerable<IReturnType> BaseTypes {
			get;
		}
		
		ReadOnlyCollection<IReturnType> ImplementedInterfaces {
			get;
		}
		
		IEnumerable<IMember> Members {
			get;
		}
		
		IEnumerable<IType> InnerTypes {
			get;
		}

		IEnumerable<IField> Fields {
			get;
		}

		IEnumerable<IProperty> Properties {
			get;
		}

		IEnumerable<IMethod> Methods {
			get;
		}

		IEnumerable<IEvent> Events {
			get;
		}
		
		List<IMember> SearchMember (string name, bool caseSensitive);
		
		/// <value>
		/// Types that are defined across several compilation unit (e.g. files) this returns
		/// the other types that are part of the same super type.
		/// </value>
		IEnumerable<IType> Parts { 
			get; 
		}
		bool HasParts {
			get;
		}
		
		bool HasExtensionMethods {
			get;
		}
		
		int FieldCount {
			get;
		}
		int MethodCount {
			get;
		}
		int ConstructorCount {
			get;
		}
		int IndexerCount {
			get;
		}
		int PropertyCount {
			get;
		}
		int EventCount {
			get;
		}
		int InnerTypeCount {
			get;
		}
		
		bool HasOverriden (IMember member);
		bool IsBaseType (IReturnType type);
		
		IMember GetMemberAt (int line, int column);

		//// <value>
		/// MonoDoc Xml documentation.
		/// </value>
		System.Xml.XmlDocument HelpXml {
			get;
		}
		
		IEnumerable<IMethod> GetAllExtensionMethods (List<IType> accessibleExtensionTypes);
		
		IEnumerable<IMethod> GetExtensionMethods (List<IType> accessibleExtensionTypes, string methodName);
	
		TypeKind Kind {
			get;
		}
	}
	
	public interface IInstantiatedType : IType
	{
		IList<IReturnType> GenericParameters {
			get;
		}
		
		IType UninstantiatedType { 
			get; 
		}
	}
	
	public interface ITypeParameterType : IType, ITypeParameter
	{
	}
}
