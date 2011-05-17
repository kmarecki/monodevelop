
using System;

using Editor = MonoDevelop.GtkCore2.Designer.Editor;
using Wrapper = MonoDevelop.GtkCore2.Designer.Wrapper;

namespace MonoDevelop.GtkCore2.Stetic
{
	internal class UserInterface
	{
		UserInterface()
		{
		}
		
		public static WidgetDesignerBackend CreateWidgetDesigner (Gtk.Container widget)
		{
			Wrapper.Container wc = Wrapper.Container.Lookup (widget);
			return CreateWidgetDesigner (widget, wc.DesignWidth, wc.DesignHeight);
		}
		
		public static WidgetDesignerBackend CreateWidgetDesigner (Gtk.Container widget, int designWidth, int designHeight)
		{
			return new WidgetDesignerBackend (widget, designWidth, designHeight);
		}
		
		public static ActionGroupDesignerBackend CreateActionGroupDesigner (ProjectBackend project, ActionGroupToolbar groupToolbar)
		{
			Editor.ActionGroupEditor agroupEditor = new Editor.ActionGroupEditor ();
			agroupEditor.Project = project;
			WidgetDesignerBackend groupDesign = new WidgetDesignerBackend (agroupEditor, -1, -1);
			
			groupToolbar.Bind (agroupEditor);
			
			return new ActionGroupDesignerBackend (groupDesign, agroupEditor, groupToolbar);
		}
	}
}
