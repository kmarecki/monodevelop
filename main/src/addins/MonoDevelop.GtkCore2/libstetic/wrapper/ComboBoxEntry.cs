using System;
using System.CodeDom;

namespace MonoDevelop.GtkCore2.Designer.Wrapper {

	public class ComboBoxEntry : ComboBox {

		public static new Gtk.ComboBoxEntry CreateInstance ()
		{
			Gtk.ComboBoxEntry c = Gtk.ComboBoxEntry.NewText ();
			// Make sure all children are created, so the mouse events can be
			// bound and the widget can be selected.
			c.EnsureStyle ();
			try {
				FixSensitivity (c);
			} catch {
			}
			return c;
		}
		
		internal protected override CodeExpression GenerateObjectCreation (GeneratorContext ctx)
		{
			if (IsTextCombo) {
				return new CodeMethodInvokeExpression (
					new CodeTypeReferenceExpression (new CodeTypeReference ("Gtk.ComboBoxEntry", CodeTypeReferenceOptions.GlobalReference)),
					"NewText"
				);
			} else
				return base.GenerateObjectCreation (ctx);
		}
	}
}
