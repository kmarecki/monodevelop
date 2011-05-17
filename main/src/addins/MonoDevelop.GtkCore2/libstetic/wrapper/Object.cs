using System;
using System.Collections;

namespace MonoDevelop.GtkCore2.Designer.Wrapper {
	public abstract class Object : MonoDevelop.GtkCore2.Designer.ObjectWrapper {

		public override void Dispose ()
		{
			if (Wrapped == null)
				return;
			((GLib.Object)Wrapped).RemoveNotification (NotifyHandler);
			base.Dispose ();
		}

		internal protected override void OnDesignerAttach (IDesignArea designer)
		{
			base.OnDesignerAttach (designer);
			((GLib.Object)Wrapped).AddNotification (NotifyHandler);
		}
		
		internal protected override void OnDesignerDetach (IDesignArea designer)
		{
			base.OnDesignerDetach (designer);
			((GLib.Object)Wrapped).RemoveNotification (NotifyHandler);
		}
		
		public static Object Lookup (GLib.Object obj)
		{
			return MonoDevelop.GtkCore2.Designer.ObjectWrapper.Lookup (obj) as MonoDevelop.GtkCore2.Designer.Wrapper.Object;
		}

		void NotifyHandler (object obj, GLib.NotifyArgs args)
		{
			if (Loading)
				return;

			// Translate gtk names into descriptor names.
			foreach (ItemGroup group in ClassDescriptor.ItemGroups) {
				foreach (ItemDescriptor item in group) {
					TypedPropertyDescriptor prop = item as TypedPropertyDescriptor;
					if (prop != null && prop.GladeName == args.Property) {
						EmitNotify (prop.Name);
						return;
					}
				}
			}
		}
	}
}
