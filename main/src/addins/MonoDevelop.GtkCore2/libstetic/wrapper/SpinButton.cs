using System;

namespace MonoDevelop.GtkCore2.Designer.Wrapper {

	public class SpinButton : Widget {

		public static Gtk.SpinButton CreateInstance ()
		{
			return new Gtk.SpinButton (0.0, 100.0, 1.0);
		}
	}
}
