using System;

namespace MonoDevelop.GtkCore2.Designer.Wrapper {

	public class FontButton : Widget {

		public bool UseFont {
			get {
				return ((Gtk.FontButton)Wrapped).UseFont;
			}
			set {
				Gtk.FontButton fb = (Gtk.FontButton)Wrapped;

				fb.UseFont = value;

				// Force it to update
				fb.ShowSize = !fb.ShowSize;
				fb.ShowSize = !fb.ShowSize;
			}
		}
	}
}
