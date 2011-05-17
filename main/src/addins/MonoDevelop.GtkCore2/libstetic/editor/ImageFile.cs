using System;

namespace MonoDevelop.GtkCore2.Designer.Editor {

	[PropertyEditor ("File", "Changed")]
	public class ImageFile : Image {

		public ImageFile () : base (false, true) { }
	}
}
