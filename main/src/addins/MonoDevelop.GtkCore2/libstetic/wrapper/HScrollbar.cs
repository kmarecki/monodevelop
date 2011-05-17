using System;
using System.CodeDom;

namespace MonoDevelop.GtkCore2.Designer.Wrapper {

	public class HScrollbar : Range {

		public static Gtk.HScrollbar CreateInstance ()
		{
			return new Gtk.HScrollbar (new Gtk.Adjustment (0.0, 0.0, 100.0, 1.0, 10.0, 10.0));
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			return new CodeObjectCreateExpression (ClassDescriptor.WrappedTypeName.ToGlobalTypeRef (), new CodePrimitiveExpression (null));
		}
	}
}
