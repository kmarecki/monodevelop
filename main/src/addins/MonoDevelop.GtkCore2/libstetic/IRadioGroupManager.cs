
using System;
using System.Collections;

namespace MonoDevelop.GtkCore2.Designer
{
	public delegate void GroupsChangedDelegate ();
	
	public interface IRadioGroupManagerProvider
	{
		IRadioGroupManager GetGroupManager ();
	}
	
	public interface IRadioGroupManager
	{
		event GroupsChangedDelegate GroupsChanged;
		IEnumerable GroupNames { get; }
		void Rename (string oldName, string newName);
		void Add (string group);
	}
}
