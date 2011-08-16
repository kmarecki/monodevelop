//
// DomFieldNodeBuilder.cs
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
using System.Text;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler;
using System.Threading;
using Mono.TextEditor;
using System.Collections.Generic;

namespace MonoDevelop.AssemblyBrowser
{
	class DomFieldNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IField); }
		}
		
		public DomFieldNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			IField field = (IField)dataObject;
			return field.FullName;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IField field = (IField)dataObject;
			label = Ambience.GetString (field, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup);
			if (field.IsPrivate || field.IsInternal)
				label = DomMethodNodeBuilder.FormatPrivate (label);
			icon = ImageService.GetPixbuf (field.StockIcon, Gtk.IconSize.Menu);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is IMethod)
				return 1;
			if (otherNode.DataItem is IProperty)
				return 1;
			if (otherNode.DataItem is BaseTypeFolder)
				return 1;
			if (otherNode.DataItem is IField)
				return ((IField)thisNode.DataItem).Name.CompareTo (((IField)otherNode.DataItem).Name);
			return -1;
		}
		
		#region IAssemblyBrowserNodeBuilder
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			IField field = (IField)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<span font_family=\"monospace\">");
			result.Append (Ambience.GetString (field, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</span>");
			result.AppendLine ();
			DomMethodNodeBuilder.PrintDeclaringType (result, navigator);
			DomTypeNodeBuilder.PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			var field = (DomCecilField)navigator.DataItem;
			return DomMethodNodeBuilder.Disassemble (data, rd => rd.DisassembleField (field.FieldDefinition));
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Decompile (TextEditorData data, ITreeNavigator navigator)
		{
			var field = (DomCecilField)navigator.DataItem;
			
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetModule (navigator), ((DomCecilType)field.DeclaringType).TypeDefinition, b => b.AddField (field.FieldDefinition));
		}
		
		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			IField field = (IField)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<big>");
			result.Append (Ambience.GetString (field, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</big>");
			result.AppendLine ();
			
			AmbienceService.DocumentationFormatOptions options = new AmbienceService.DocumentationFormatOptions ();
			options.MaxLineLength = -1;
			options.BigHeadings = true;
			options.Ambience = Ambience;
			result.AppendLine ();
			
			result.Append (AmbienceService.GetDocumentationMarkup (AmbienceService.GetDocumentation (field), options));
			
			return result.ToString ();
		}
		#endregion
	}
}
