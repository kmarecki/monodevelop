using System;

namespace MonoDevelop.GtkCore2.Designer.Wrapper
{
	public delegate void WidgetEventHandler (object sender, WidgetEventArgs args);
	
	public class WidgetEventArgs: EventArgs
	{
		MonoDevelop.GtkCore2.Designer.Wrapper.Widget wrapper;
		Gtk.Widget widget;
		
		public WidgetEventArgs (Gtk.Widget widget)
		{
			this.widget = widget;
			wrapper = MonoDevelop.GtkCore2.Designer.Wrapper.Widget.Lookup (widget);
		}
		
		public WidgetEventArgs (MonoDevelop.GtkCore2.Designer.Wrapper.Widget wrapper)
		{
			this.wrapper = wrapper;
			if (wrapper != null)
				this.widget = wrapper.Wrapped;
		}
		
		public Gtk.Widget Widget {
			get { return widget; }
		}
		
		public Widget WidgetWrapper {
			get { return wrapper; }
		}
	}	
}
