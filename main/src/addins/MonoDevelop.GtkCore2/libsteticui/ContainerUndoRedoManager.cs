
using System;
using System.Xml;
using System.Collections;

using MonoDevelop.GtkCore2.Designer;
using Wrapper = MonoDevelop.GtkCore2.Designer.Wrapper;

namespace MonoDevelop.GtkCore2.Stetic
{
	class ContainerUndoRedoManager: UndoRedoManager
	{
		protected override object GetDiff (ObjectWrapper w)
		{
			// Only track changes in widgets.
			Wrapper.Widget widget = w as Wrapper.Widget;
			if (widget != null) return w.GetUndoDiff ();
			else return null;
		}
	}
}
