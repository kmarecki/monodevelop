using System;

namespace MonoDevelop.GtkCore2.Designer 
{
	public interface IProject 
	{
		//string FileName { get; }
		string FolderName { get; }
		Gtk.Widget[] Toplevels { get; }
		Gtk.Widget GetWidget (string name);
		Gtk.Widget Selection { get; set; }
		Wrapper.ActionGroupCollection ActionGroups { get; }
		ProjectIconFactory IconFactory { get; }
		string ImagesRootPath { get; }
		string TargetGtkVersion { get; }
//		bool Modified { get; set; }
		IResourceProvider ResourceProvider { get; set; }

		void PopupContextMenu (MonoDevelop.GtkCore2.Designer.Wrapper.Widget wrapper);
		void PopupContextMenu (Placeholder ph);
		void AddWindow (Gtk.Window window);
		string ImportFile (string filePath);
		
		event Wrapper.WidgetEventHandler SelectionChanged;
		
		void NotifyObjectChanged (ObjectWrapperEventArgs args);
		void NotifyNameChanged (MonoDevelop.GtkCore2.Designer.Wrapper.WidgetNameChangedArgs args);
		void NotifySignalAdded (SignalEventArgs args);
		void NotifySignalRemoved (SignalEventArgs args);
		void NotifySignalChanged (SignalChangedEventArgs args);
		void NotifyWidgetContentsChanged (Wrapper.Widget w);
	}
}
