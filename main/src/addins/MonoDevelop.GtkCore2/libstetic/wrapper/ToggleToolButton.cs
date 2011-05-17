using System;

namespace MonoDevelop.GtkCore2.Designer.Wrapper {

	public class ToggleToolButton : ToolButton {

		public static new Gtk.ToolButton CreateInstance ()
		{
			return new Gtk.ToggleToolButton (Gtk.Stock.Bold);
		}
	}
}
