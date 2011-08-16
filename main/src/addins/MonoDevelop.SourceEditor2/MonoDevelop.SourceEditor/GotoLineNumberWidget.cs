//
// GotoLineNumberWidget.cs
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
using Gtk;

using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	
	partial class GotoLineNumberWidget : Gtk.Bin
	{
		SourceEditorWidget widget;
		double vSave, hSave;
		DocumentLocation caretSave;
		bool cleanExit = false;
		
		Widget container;
		void HandleViewTextEditorhandleSizeAllocated (object o, SizeAllocatedArgs args)
		{
			int newX = widget.TextEditor.Allocation.Width - this.Allocation.Width - 8;
			TextEditorContainer.EditorContainerChild containerChild = ((Mono.TextEditor.TextEditorContainer.EditorContainerChild)widget.TextEditorContainer[container]);
			if (newX != containerChild.X) {
				this.entryLineNumber.WidthRequest = widget.Vbox.Allocation.Width / 4;
				containerChild.X = newX;
				widget.TextEditorContainer.QueueResize ();
			}
		}
		
		public GotoLineNumberWidget (SourceEditorWidget widget, Widget container)
		{
			this.container = container;
			this.Build ();
			
			this.widget = widget;
			StoreWidgetState ();
			widget.TextEditorContainer.SizeAllocated += HandleViewTextEditorhandleSizeAllocated;
			
			//HACK: GTK rendering issue on Mac, images don't repaint unless we put them in visible eventboxes
			if (MonoDevelop.Core.Platform.IsMac) {
				foreach (var eb in new [] { eventbox1, eventbox2 }) {
					eb.VisibleWindow = true;
					eb.ModifyBg (StateType.Normal, new Gdk.Color (230, 230, 230));
				}
			}
			this.closeButton.Clicked += delegate {
				RestoreWidgetState ();
				widget.RemoveSearchWidget ();
			};
			
			this.buttonGoToLine.Clicked += delegate {
				cleanExit = true;
				GotoLine ();
				widget.RemoveSearchWidget ();
			};
			
			foreach (Gtk.Widget child in this.Children) {
				child.KeyPressEvent += delegate (object sender, Gtk.KeyPressEventArgs args) {
					if (args.Event.Key == Gdk.Key.Escape)  {
						RestoreWidgetState ();
						widget.RemoveSearchWidget ();
					}
				};
			}
			
			Gtk.Widget oldWidget = null;
			this.FocusChildSet += delegate (object sender, Gtk.FocusChildSetArgs args)  {
				// only store state when the focus comes from a non child widget
				if (args.Widget != null && oldWidget == null)
					StoreWidgetState ();
				oldWidget = args.Widget;
			};
			
			this.entryLineNumber.Changed +=  delegate {
				PreviewLine ();
			};
				
			this.entryLineNumber.Activated += delegate {
				cleanExit = true;
				GotoLine ();
				widget.RemoveSearchWidget ();
			};
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			widget.TextEditorContainer.SizeAllocated -= HandleViewTextEditorhandleSizeAllocated;
			
		}

		
		void StoreWidgetState ()
		{
			this.vSave  = widget.TextEditor.VAdjustment.Value;
			this.hSave  = widget.TextEditor.HAdjustment.Value;
			this.caretSave =  widget.TextEditor.Caret.Location;
		}
		
		void RestoreWidgetState ()
		{
			if (cleanExit)
				return;
			widget.TextEditor.VAdjustment.Value = this.vSave;
			widget.TextEditor.HAdjustment.Value = this.hSave;
			widget.TextEditor.Caret.Location    = this.caretSave;
		}
		
		int TargetLine {
			get {
				int line;
				try {
					line = System.Int32.Parse (entryLineNumber.Text);
				} catch (System.OverflowException) {
					line = entryLineNumber.Text.Trim ().StartsWith ("-") ? int.MinValue : int.MaxValue;
				}
				bool isRelativeJump = entryLineNumber.Text.Trim ().StartsWith ("-") || entryLineNumber.Text.Trim ().StartsWith ("+");
				return isRelativeJump ? this.caretSave.Line + line : line;
			}
		}
		
		void GotoLine ()
		{
			try {
				widget.TextEditor.Caret.Line = TargetLine;
				widget.TextEditor.CenterToCaret ();
			} catch (System.Exception) { 
			}
		}
		
		internal static readonly Gdk.Color warningColor = new Gdk.Color (210, 210, 32);
		internal static readonly Gdk.Color errorColor   = new Gdk.Color (255, 102, 102);
		
		void PreviewLine ()
		{
			if (String.IsNullOrEmpty (entryLineNumber.Text) || entryLineNumber.Text == "+" || entryLineNumber.Text == "-") {
				this.entryLineNumber.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
				RestoreWidgetState ();
				return;
			}
			try {
				int targetLine = TargetLine;
				if (targetLine >= widget.TextEditor.Document.LineCount || targetLine < 0) {
					targetLine = Math.Max (1, Math.Min (widget.TextEditor.Document.LineCount, targetLine));
					
				} else {
					this.entryLineNumber.ModifyBase (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
				}
				widget.TextEditor.Caret.Line = targetLine;
				widget.TextEditor.CenterToCaret ();
			} catch (System.Exception) { 
				this.entryLineNumber.ModifyBase (Gtk.StateType.Normal, errorColor);
			}
		}
		
		public void Focus ()
		{
			if (!this.entryLineNumber.IsFocus)
				this.entryLineNumber.IsFocus = true;
			PreviewLine ();
		}
	}
}
