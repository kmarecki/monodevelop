
// This file has been generated by the GUI designer. Do not modify.
namespace MonoDevelop.Ide.FindInFiles
{
	internal partial class SearchResultWidget
	{
		private global::Gtk.UIManager UIManager;
		private global::Gtk.Action ViewModeAction;
		private global::Gtk.HBox hbox1;
		private global::Gtk.VBox vbox2;
		private global::Gtk.HPaned hpaned1;
		private global::Gtk.ScrolledWindow GtkScrolledWindow;
		private global::Gtk.TreeView treeviewSearchResults;
		private global::Gtk.ScrolledWindow scrolledwindowLogView;
		private global::Gtk.TextView textviewLog;
		private global::Gtk.Label labelStatus;
		private global::Gtk.Toolbar toolbar;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget MonoDevelop.Ide.FindInFiles.SearchResultWidget
			Stetic.BinContainer w1 = global::Stetic.BinContainer.Attach (this);
			this.UIManager = new global::Gtk.UIManager ();
			global::Gtk.ActionGroup w2 = new global::Gtk.ActionGroup ("Default");
			this.ViewModeAction = new global::Gtk.Action ("ViewModeAction", global::MonoDevelop.Core.GettextCatalog.GetString ("ViewMode"), null, null);
			this.ViewModeAction.ShortLabel = global::MonoDevelop.Core.GettextCatalog.GetString ("ViewMode");
			w2.Add (this.ViewModeAction, null);
			this.UIManager.InsertActionGroup (w2, 0);
			this.Name = "MonoDevelop.Ide.FindInFiles.SearchResultWidget";
			// Container child MonoDevelop.Ide.FindInFiles.SearchResultWidget.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox ();
			this.vbox2.Name = "vbox2";
			// Container child vbox2.Gtk.Box+BoxChild
			this.hpaned1 = new global::Gtk.HPaned ();
			this.hpaned1.CanFocus = true;
			this.hpaned1.Name = "hpaned1";
			this.hpaned1.Position = 499;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow ();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeviewSearchResults = new global::Gtk.TreeView ();
			this.treeviewSearchResults.CanFocus = true;
			this.treeviewSearchResults.Name = "treeviewSearchResults";
			this.GtkScrolledWindow.Add (this.treeviewSearchResults);
			this.hpaned1.Add (this.GtkScrolledWindow);
			global::Gtk.Paned.PanedChild w4 = ((global::Gtk.Paned.PanedChild)(this.hpaned1 [this.GtkScrolledWindow]));
			w4.Resize = false;
			// Container child hpaned1.Gtk.Paned+PanedChild
			this.scrolledwindowLogView = new global::Gtk.ScrolledWindow ();
			this.scrolledwindowLogView.CanFocus = true;
			this.scrolledwindowLogView.Name = "scrolledwindowLogView";
			this.scrolledwindowLogView.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child scrolledwindowLogView.Gtk.Container+ContainerChild
			this.textviewLog = new global::Gtk.TextView ();
			this.textviewLog.CanFocus = true;
			this.textviewLog.Name = "textviewLog";
			this.textviewLog.Editable = false;
			this.scrolledwindowLogView.Add (this.textviewLog);
			this.hpaned1.Add (this.scrolledwindowLogView);
			this.vbox2.Add (this.hpaned1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.hpaned1]));
			w7.Position = 0;
			// Container child vbox2.Gtk.Box+BoxChild
			this.labelStatus = new global::Gtk.Label ();
			this.labelStatus.Name = "labelStatus";
			this.labelStatus.Xalign = 0F;
			this.vbox2.Add (this.labelStatus);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox2 [this.labelStatus]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			w8.Padding = ((uint)(3));
			this.hbox1.Add (this.vbox2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.vbox2]));
			w9.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.UIManager.AddUiFromString ("<ui><toolbar name='toolbar'/></ui>");
			this.toolbar = ((global::Gtk.Toolbar)(this.UIManager.GetWidget ("/toolbar")));
			this.toolbar.Name = "toolbar";
			this.toolbar.Orientation = ((global::Gtk.Orientation)(1));
			this.toolbar.ShowArrow = false;
			this.toolbar.ToolbarStyle = ((global::Gtk.ToolbarStyle)(0));
			this.toolbar.IconSize = ((global::Gtk.IconSize)(1));
			this.hbox1.Add (this.toolbar);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.toolbar]));
			w10.Position = 1;
			w10.Expand = false;
			w10.Fill = false;
			this.Add (this.hbox1);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			w1.SetUiManager (UIManager);
			this.Hide ();
		}
	}
}
