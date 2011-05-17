
namespace MonoDevelop.GtkCore2.Designer.Wrapper
{
	public delegate void WidgetNameChangedHandler (object sender, WidgetNameChangedArgs args);

	public class WidgetNameChangedArgs: WidgetEventArgs
	{
		string oldName;
		string newName;
		
		public WidgetNameChangedArgs (MonoDevelop.GtkCore2.Designer.Wrapper.Widget widget, string oldName, string newName): base (widget)
		{
			this.oldName = oldName;
			this.newName = newName;
		}
		
		public string OldName {
			get { return oldName; }
		}
		
		public string NewName {
			get { return newName; }
		}
	}
}
