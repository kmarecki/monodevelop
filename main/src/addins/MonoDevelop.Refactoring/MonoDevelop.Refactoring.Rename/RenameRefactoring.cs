// 
// Rename.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core;
using Mono.TextEditor;
using System.Text;
using MonoDevelop.Ide;
using System.Linq;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.ProgressMonitoring;

namespace MonoDevelop.Refactoring.Rename
{
	public class RenameRefactoring : RefactoringOperation
	{
		public override string AccelKey {
			get {
				var key = IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename).AccelKey;
				return key == null ? null : key.Replace ("dead_circumflex", "^");
			}
		}
		
		public RenameRefactoring ()
		{
			Name = "Rename";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.SelectedItem is LocalVariable || options.SelectedItem is IParameter)
				return true;

			if (options.SelectedItem is IType)
				return ((IType)options.SelectedItem).SourceProject != null;

			if (options.SelectedItem is IMember) {
				IType cls = ((IMember)options.SelectedItem).DeclaringType;
				return cls != null && cls.SourceProject != null;
			}
			return false;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename).Text;
		}
		
		public override void Run (RefactoringOptions options)
		{
			if (options.SelectedItem is LocalVariable || options.SelectedItem is IParameter) {
				var col = ReferenceFinder.FindReferences (options.SelectedItem);
				if (col == null)
					return;
				TextEditorData data = options.GetTextEditorData ();
				Mono.TextEditor.TextEditor editor = data.Parent;
				if (editor == null) {
					MessageService.ShowCustomDialog (new RenameItemDialog (options, this));
					return;
				}
				
				List<TextLink> links = new List<TextLink> ();
				TextLink link = new TextLink ("name");
				int baseOffset = Int32.MaxValue;
				foreach (MemberReference r in col) {
					baseOffset = Math.Min (baseOffset, data.Document.LocationToOffset (r.Line, r.Column));
				}
				foreach (MemberReference r in col) {
					Segment segment = new Segment (data.Document.LocationToOffset (r.Line, r.Column) - baseOffset, r.Name.Length);
					if (segment.Offset <= data.Caret.Offset - baseOffset && data.Caret.Offset - baseOffset <= segment.EndOffset) {
						link.Links.Insert (0, segment); 
					} else {
						link.AddLink (segment);
					}
				}
				
				links.Add (link);
				if (editor.CurrentMode is TextLinkEditMode)
					((TextLinkEditMode)editor.CurrentMode).ExitTextLinkMode ();
				TextLinkEditMode tle = new TextLinkEditMode (editor, baseOffset, links);
				tle.SetCaretPosition = false;
				tle.SelectPrimaryLink = true;
				if (tle.ShouldStartTextLinkMode) {
					ModeHelpWindow helpWindow = new ModeHelpWindow ();
					helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
					helpWindow.TitleText = options.SelectedItem is LocalVariable ? GettextCatalog.GetString ("<b>Local Variable -- Renaming</b>") : GettextCatalog.GetString ("<b>Parameter -- Renaming</b>");
					helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Key</b>"), GettextCatalog.GetString ("<b>Behavior</b>")));
					helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Return</b>"), GettextCatalog.GetString ("<b>Accept</b> this refactoring.")));
					helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Esc</b>"), GettextCatalog.GetString ("<b>Cancel</b> this refactoring.")));
					tle.HelpWindow = helpWindow;
					tle.Cancel += delegate {
						if (tle.HasChangedText)
							editor.Document.Undo ();
					};
					tle.OldMode = data.CurrentMode;
					tle.StartMode ();
					data.CurrentMode = tle;
				}
			} else {
				MessageService.ShowCustomDialog (new RenameItemDialog (options, this));
			}
		}
		
		public class RenameProperties
		{
			public string NewName {
				get;
				set;
			}
			
			public bool RenameFile {
				get;
				set;
			}
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			RenameProperties properties = (RenameProperties)prop;
			List<Change> result = new List<Change> ();
			IEnumerable<MemberReference> col = null;
			using (var monitor = new MessageDialogProgressMonitor (true, false, false, true)) {
				col = ReferenceFinder.FindReferences (options.SelectedItem, monitor);
				if (col == null)
					return result;
					
				if (properties.RenameFile && options.SelectedItem is IType) {
					IType cls = (IType)options.SelectedItem;
					int currentPart = 1;
					HashSet<string> alreadyRenamed = new HashSet<string> ();
					foreach (IType part in cls.Parts) {
						if (part.CompilationUnit.FileName != options.Document.FileName && System.IO.Path.GetFileNameWithoutExtension (part.CompilationUnit.FileName) != System.IO.Path.GetFileNameWithoutExtension (options.Document.FileName))
							continue;
						if (alreadyRenamed.Contains (part.CompilationUnit.FileName))
							continue;
						alreadyRenamed.Add (part.CompilationUnit.FileName);
							
						string oldFileName = System.IO.Path.GetFileNameWithoutExtension (part.CompilationUnit.FileName);
						string newFileName;
						if (oldFileName.ToUpper () == properties.NewName.ToUpper () || oldFileName.ToUpper ().EndsWith ("." + properties.NewName.ToUpper ()))
							continue;
						int idx = oldFileName.IndexOf (cls.Name);
						if (idx >= 0) {
							newFileName = oldFileName.Substring (0, idx) + properties.NewName + oldFileName.Substring (idx + cls.Name.Length);
						} else {
							newFileName = currentPart != 1 ? properties.NewName + currentPart : properties.NewName;
							currentPart++;
						}
							
						int t = 0;
						while (System.IO.File.Exists (GetFullFileName (newFileName, part.CompilationUnit.FileName, t))) {
							t++;
						}
						result.Add (new RenameFileChange (part.CompilationUnit.FileName, GetFullFileName (newFileName, part.CompilationUnit.FileName, t)));
					}
				}
				
				foreach (MemberReference memberRef in col) {
					TextReplaceChange change = new TextReplaceChange ();
					change.FileName = memberRef.FileName;
					change.Offset = memberRef.Position;
					change.RemovedChars = memberRef.Name.Length;
					change.InsertedText = properties.NewName;
					change.Description = string.Format (GettextCatalog.GetString ("Replace '{0}' with '{1}'"), memberRef.Name, properties.NewName);
					result.Add (change);
				}
			}
			return result;
		}
		
		static string GetFullFileName (string fileName, string oldFullFileName, int tryCount)
		{
			StringBuilder name = new StringBuilder (fileName);
			if (tryCount > 0) {
				name.Append ("_");
				name.Append (tryCount.ToString ());
			}
			if (System.IO.Path.HasExtension (oldFullFileName))
				name.Append (System.IO.Path.GetExtension (oldFullFileName));
			
			return System.IO.Path.Combine (System.IO.Path.GetDirectoryName (oldFullFileName), name.ToString ());
		}
	}
}
