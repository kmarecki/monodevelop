// 
// CodeMetricsServices.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2009 Nikhil Sarda
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
//using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
//using MonoDevelop.Projects.Dom.Parser;
//using MonoDevelop.Projects.Dom.Output;
using Mono.TextEditor;

using ICSharpCode.OldNRefactory;
using ICSharpCode.OldNRefactory.Ast;
using ICSharpCode.OldNRefactory.AstBuilder;

//add reference to configure.in file

namespace MonoDevelop.CodeMetrics
{
	public class ClassProperties : IProperties
	{
		private IType cls;
		
		public Dictionary<string, ClassProperties> InnerClasses {
			get; private set;
		}
		
		public Dictionary<string, EnumProperties> InnerEnums {
			get; private set;
		}
		
		public Dictionary<string, StructProperties> InnerStructs {
			get; private set;
		}
		
		public Dictionary<string, DelegateProperties> InnerDelegates {
			get; private set;
		}
		
		public Dictionary<string, InterfaceProperties> InnerInterfaces {
			get; private set;
		}
		
		public Dictionary<string, MethodProperties> Methods {
			get; private set;
		}
		
		public Dictionary<string, FieldProperties> Fields {
			get; private set;
		}
		
		internal IType Class {
			get { return cls; }
		}
		
		public ClassProperties ParentClass {
			get; internal set;
		}
		
		public NamespaceProperties ParentNamespace {
			get; internal set;
		}
		
		public int StartLine {
			get; private set;
		}
		
		public int EndLine {
			get; private set;
		}
		
		public string FilePath {
			get; set;
		}
		
		public int PublicMethodCount {
			get; private set;
		}
		
		public int PrivateMethodCount {
			get; private set;
		}
		
		public int OverloadedMethodCount {
			get; private set;
		}
		
		public int VirtualMethodCount {
			get; private set;
		}
		
		private void AddMethod ()
		{
			foreach (IMethod method in Class.Methods) {
				if(method.Name==".ctor")
					continue;
				try {
					string key = method.FullName + " " + SerializeParameters(method);
					if(Methods.ContainsKey(key))
						continue;
			 		Methods.Add(key, new MethodProperties(method));
					Methods[key].ParentClass = this;
				} catch (Exception ex) {
					Console.WriteLine("Error : " + ex);
					//TODO some error handling
				}	
			}
		}
		
		private void AddField ()
		{
			if(Fields==null)
				Fields = new Dictionary<string, FieldProperties>();
			foreach (IField field in Class.Fields) {
				try {
					Fields.Add(field.Name, new FieldProperties(field));
				} catch (Exception ex) {
					Console.WriteLine("Error : " + ex);
				}
			}
		}
		
		
#region Inner Types Count
		
		public int InnerTypeCount {
			get {
				return Class.InnerTypeCount;
			}
		}
		
		public int InnerClassCount {
			get {
				return InnerClasses.Count;
			}
		}
		
		public int InterfaceCount {
			get {
				return InnerInterfaces.Count;
			}
		}
		
		public int StructCount {
			get {
				return InnerStructs.Count;
			}
		}
		
		public int EnumCount {
			get {
				return InnerEnums.Count;
			}
		}
		
		public int DelegateCount {
			get {
				return InnerDelegates.Count;
			}
		}
		
		private void EvaluateInnerTypeCount()
		{
			foreach (IType type in Class.InnerTypes) {
				switch (type.ClassType) {
				case MonoDevelop.Projects.Dom.ClassType.Class:
					AddInnerClass(type); 
					break;
				case MonoDevelop.Projects.Dom.ClassType.Enum:
					AddInnerEnum(type);
					break;
				case MonoDevelop.Projects.Dom.ClassType.Interface:
					AddInnerInterface(type);
					break;
				case MonoDevelop.Projects.Dom.ClassType.Struct:
					AddInnerStruct(type);
					break;
				case MonoDevelop.Projects.Dom.ClassType.Delegate:
					AddInnerDelegate(type);
					break;
				}
			}
		}
		
	
		private void AddInnerClass(IType type)
		{
			if(!InnerClasses.ContainsKey(type.FullName))
				InnerClasses.Add(type.FullName, new ClassProperties(type));
		}
	
		
		private void AddInnerEnum(IType type)
		{
			if(!InnerEnums.ContainsKey(type.FullName))
				InnerEnums.Add(type.FullName, new EnumProperties(type));
		}
		
		private void AddInnerInterface(IType type)
		{
			if(!InnerInterfaces.ContainsKey(type.FullName))
				InnerInterfaces.Add(type.FullName, new InterfaceProperties(type));
		}
		
		private void AddInnerStruct(IType type)
		{
			if(!InnerStructs.ContainsKey(type.FullName))
				InnerStructs.Add(type.FullName, new StructProperties(type));
		}
		
		private void AddInnerDelegate(IType type)
		{
			if(!InnerDelegates.ContainsKey(type.FullName))
				InnerDelegates.Add(type.FullName, new DelegateProperties(type));
		}
		
#endregion
		
		public string FullName {
			get {return Class.FullName; }
		}
		
		public int MethodCount {
			get; private set;
		}
		
		public int EventCount {
			get; private set; 
		}
		
		public int FieldCount {
			get; private set;
		}
		
		public int ConstructorCount {
			get {
				return Class.ConstructorCount;
			}
		}
		
		public bool IsDocumented {
			get {
				if (Class.Documentation != "")
					return true;
				else
					return false;
			}
		}
		
		public int GetInterfacesImplemented { 
			get; private set;
		}
		
		public ClassProperties (IType c)
		{
			cls = c;
			
			InnerClasses = new Dictionary<string, ClassProperties> ();
			InnerEnums = new Dictionary<string, EnumProperties> ();
			InnerInterfaces = new Dictionary<string, InterfaceProperties>();
			InnerStructs = new Dictionary<string, StructProperties> ();
			InnerDelegates = new Dictionary<string, DelegateProperties> ();
			Methods = new Dictionary<string, MethodProperties> ();
			Fields = new Dictionary<string, FieldProperties> ();
			
			EvaluateInnerTypeCount();
			
			AddMethod ();
			AddField ();
			
			this.FieldCount = this.Fields.Count;
			this.EventCount = this.Class.EventCount;
			this.MethodCount = this.Methods.Count;
			this.StartLine = this.Class.BodyRegion.Start.Line;
			this.EndLine = this.Class.BodyRegion.End.Line;
		}
		
		public int DepthOfInheritance {
			get; internal set;
		}
		
		public int FanOut {
			get; internal set;
		}
		
		public int InheritedFieldCount {
			get; internal set;
		}
		 
		public int InheritedMethodCount {
			get; internal set;
		}
		
		public int TotalMethodCount {
			get { return  + InheritedMethodCount; }
		}
		
		public int TotalFieldCount {
			get { return this.FieldCount + InheritedFieldCount; }
		}
		
		public bool IsRoot {
			get {
				return DepthOfInheritance <=1 && FanOut > 0;
			}
		}
		
		public bool IsInnerClass {
			get; internal set;
		}
		
		public int DataAbstractionCoupling {
			get; internal set;
		}
		
		public int CyclometricComplexity {
			get; internal set;
		}
		
		public int AfferentCoupling {
			get; internal set;
		}
		
		public int EfferentCoupling {
			get; internal set;
		}
		
		public ulong LOCReal {
			get; internal set;
		}
		
		public ulong LOCComments {
			get; internal set;
		}
		
		public int ClassCoupling {
			get; internal set;
		}
		
		public double LCOM {
			get; internal set;
		}
		
		public double LCOM_HS {
			get; internal set;
		}
		
		internal void ProcessInnerClasses()
		{
			foreach(var innercls in this.InnerClasses)
			{
				RecursiveProcessClasses (innercls.Value);
				this.CyclometricComplexity += innercls.Value.CyclometricComplexity;
				this.ClassCoupling += innercls.Value.ClassCoupling;
				this.LOCReal += innercls.Value.LOCReal;
				this.LOCComments += innercls.Value.LOCComments;
				this.FieldCount += innercls.Value.FieldCount;
				this.MethodCount += innercls.Value.MethodCount;
				/*
				 * Finish adding more metrics
				 */
			}
		}
		
		private void RecursiveProcessClasses(ClassProperties cls)
		{
			foreach(var innercls in cls.InnerClasses)
			{
				RecursiveProcessClasses(innercls.Value);
				cls.CyclometricComplexity += innercls.Value.CyclometricComplexity;
				cls.ClassCoupling += innercls.Value.ClassCoupling;
				cls.LOCReal += innercls.Value.LOCReal;
				cls.LOCComments += innercls.Value.LOCComments;
				cls.FieldCount += innercls.Value.FieldCount;
				cls.MethodCount += innercls.Value.MethodCount;
			}
		}
		
		private static string SerializeParameters(IMethod method)
		{
			StringBuilder paramString = new StringBuilder();
			foreach(IParameter param in method.Parameters)
				paramString.Append(param.ReturnType.Name + " ");
			return paramString.ToString();
		}
	}
	
}

			
