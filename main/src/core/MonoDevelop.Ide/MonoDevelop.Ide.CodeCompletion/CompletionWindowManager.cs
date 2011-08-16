// 
// CompletionListWindow.cs
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
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CompletionWindowManager
	{
		static CompletionListWindow wnd;
		
		public static bool IsVisible {
			get {
				return wnd != null;
			}
		}
		
		public static CompletionListWindow Wnd {
			get { return wnd; }
		}
		
		public static int X {
			get {
				return wnd.X;
			}
		}
		
		public static int Y {
			get {
				return wnd.Y;
			}
		}
		
		public static CodeCompletionContext CodeCompletionContext {
			get {
				return wnd.CodeCompletionContext;
			}
		}
		
		static bool forceSuggestionMode;
		public static bool ForceSuggestionMode {
			get { return forceSuggestionMode; }
			set {
				forceSuggestionMode = value; 
				if (wnd != null) {
					wnd.AutoCompleteEmptyMatch = wnd.AutoSelect = !forceSuggestionMode;
				}
			}
		}
		
		static CompletionWindowManager ()
		{
		}
		
		public static bool ShowWindow (char firstChar, ICompletionDataList list, ICompletionWidget completionWidget, CodeCompletionContext completionContext, System.Action closedDelegate)
		{
			try {
				if (wnd == null) {
					wnd = new CompletionListWindow ();
					wnd.WordCompleted += HandleWndWordCompleted;
				}
				try {
					if (!wnd.ShowListWindow (firstChar, list, completionWidget, completionContext, closedDelegate)) {
						if (list is IDisposable)
							((IDisposable)list).Dispose ();
						DestroyWindow ();
						return false;
					}
					
					if (ForceSuggestionMode)
						wnd.AutoSelect = false;
					
					OnWindowShown (EventArgs.Empty);
					return true;
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
					return false;
				}
			} finally {
				ParameterInformationWindowManager.UpdateWindow (completionWidget);
			}
		}

		static void HandleWndWordCompleted (object sender, CodeCompletionContextEventArgs e)
		{
			EventHandler<CodeCompletionContextEventArgs> handler = WordCompleted;
			if (handler != null)
				handler (sender, e);
		}
		
		public static event EventHandler<CodeCompletionContextEventArgs> WordCompleted;
		
		
		static void DestroyWindow ()
		{
			if (wnd != null) {
				wnd.Destroy ();
				ParameterInformationWindowManager.UpdateWindow (wnd.CompletionWidget);
				wnd = null;
			}
			OnWindowClosed (EventArgs.Empty);
		}
		
		public static bool PreProcessKeyEvent (Gdk.Key key, char keyChar, Gdk.ModifierType modifier, out KeyActions ka)
		{
			if (wnd == null /*|| !wnd.Visible*/) {
				ka = KeyActions.None;
				return false;
			}
			return wnd.PreProcessKeyEvent (key, keyChar, modifier, out ka);
		}
		
		public static void PostProcessKeyEvent (KeyActions ka, Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			if (wnd == null)
				return;
			wnd.PostProcessKeyEvent (ka, key, keyChar, modifier);
		}
		
		public static void HideWindow ()
		{
			DestroyWindow ();
		}
		
		
		static void OnWindowClosed (EventArgs e)
		{
			EventHandler handler = WindowClosed;
			if (handler != null)
				handler (null, e);
		}

		public static event EventHandler WindowClosed;
		
		static void OnWindowShown (EventArgs e)
		{
			EventHandler handler = WindowShown;
			if (handler != null)
				handler (null, e);
		}
		
		public static event EventHandler WindowShown;
	}
}
