using System;
using System.Xml;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Unix;

using Gtk;
using MonoDevelop.GtkCore2.Designer;
using Wrapper = MonoDevelop.GtkCore2.Designer.Wrapper;

namespace MonoDevelop.GtkCore2.Stetic {

	internal static class GladeFiles {

		public static void Import (ProjectBackend project, string filename)
		{
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;
			doc.XmlResolver = null;
			doc.Load (filename);
			project.Id = System.IO.Path.GetFileName (filename);
			doc = GladeUtils.XslImportTransform (doc);

			XmlNode node = doc.SelectSingleNode ("/glade-interface");
			if (node == null)
				throw new ApplicationException (Catalog.GetString ("Not a glade file according to node name."));

			ObjectReader reader = new ObjectReader (project, FileFormat.Glade);
			foreach (XmlElement toplevel in node.SelectNodes ("widget")) {
				Wrapper.Container wrapper = ObjectWrapper.ReadObject (reader, toplevel, null) as Wrapper.Container;
				if (wrapper != null)
					project.AddWidget ((Gtk.Widget)wrapper.Wrapped);
			}
		}

		public static void Export (ProjectBackend project, string filename)
		{
			XmlDocument doc = new XmlDocument ();
			doc.PreserveWhitespace = true;

			XmlElement toplevel = doc.CreateElement ("glade-interface");
			doc.AppendChild (toplevel);

			ObjectWriter owriter = new ObjectWriter (doc, FileFormat.Glade);
			foreach (Widget w in project.Toplevels) {
				Wrapper.Container wrapper = Wrapper.Container.Lookup (w);
				if (wrapper == null)
					continue;

				XmlElement elem = wrapper.Write (owriter);
				if (elem != null)
					toplevel.AppendChild (elem);
			}
	
			doc = GladeUtils.XslExportTransform (doc);

			XmlTextWriter writer = new XmlTextWriter (filename, EncodingUtility.UTF8NoBom);
			writer.Formatting = Formatting.Indented;
			doc.Save (writer);
			writer.Close ();
		}
	}
}
