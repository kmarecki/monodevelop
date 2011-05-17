
using System;

namespace MonoDevelop.GtkCore2.Designer.Wrapper
{
	public class TreeView: Container
	{
		public override void Wrap (object obj, bool initialized)
		{
			base.Wrap (obj, initialized);
			if (!initialized)
				ShowScrollbars = true;
		}

		protected override bool AllowPlaceholders {
			get {
				return false;
			}
		}
	}
}
