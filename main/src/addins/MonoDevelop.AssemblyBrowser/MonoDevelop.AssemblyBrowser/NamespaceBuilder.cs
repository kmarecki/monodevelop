//
// NamespaceBuilder.cs
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
using System.Linq;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui.Components;
using Mono.TextEditor;
using System.Collections.Generic;

namespace MonoDevelop.AssemblyBrowser
{
	class NamespaceBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Namespace); }
		}
		
		public NamespaceBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			Namespace ns = (Namespace)dataObject;
			return ns.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Namespace ns = (Namespace)dataObject;
			label = GLib.Markup.EscapeText (ns.Name);
			icon = Context.GetIcon (MonoDevelop.Ide.Gui.Stock.NameSpace);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			Namespace ns = (Namespace)dataObject;
			bool publicOnly = ctx.Options ["PublicApiOnly"];
			if (ns.Types != null) 
				ctx.AddChildren (publicOnly ? ns.Types.Where (t => t.IsPublic) : ns.Types);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			Namespace ns = (Namespace)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			if (!String.IsNullOrEmpty (ns.Name)) {
				result.Append ("<span font_family=\"monospace\">");
				result.Append (Ambience.GetString (ns.Name, OutputFlags.AssemblyBrowserDescription));
				result.Append ("</span>");
				result.AppendLine ();
			}
			DomTypeNodeBuilder.PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		public List<ReferenceSegment> Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
		//	bool publicOnly = navigator.Options ["PublicApiOnly"];
			Namespace ns = (Namespace)navigator.DataItem;
			
			data.Text = "// " + ns.Name;
			return null;
		}
		
		public List<ReferenceSegment> Decompile (TextEditorData data, ITreeNavigator navigator)
		{
			return Disassemble (data, navigator);
		}
		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			return null;
		}
		#endregion
	}
}
