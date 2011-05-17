using System;

namespace MonoDevelop.GtkCore2.Stetic
{
	//Provides access to informations managed by ide
	public interface IProjectDesignInfo
	{
		string ProjectName { get; }
		
		//Returns component source file for given component
		string GetComponentFile (string componentName);
		bool HasComponentFile (string componentFile);
		
		//Returns gtkx file name for given component file
		string GetDesignerFileFromComponent (string componentFile);

		//Search for all components source file folders 
		string[] GetComponentFolders ();
		
		// Checks if code generation for a component is needed
		bool ComponentNeedsCodeGeneration (string componentName);
	}
}

