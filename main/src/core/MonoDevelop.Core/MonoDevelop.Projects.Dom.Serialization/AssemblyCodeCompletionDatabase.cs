//
// AssemblyCodeCompletionDatabase.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Mono.Cecil;

using MonoDevelop.Projects;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core.Assemblies;
using TargetRuntime = MonoDevelop.Core.Assemblies.TargetRuntime;
using System.Xml;
using System.Globalization;

namespace MonoDevelop.Projects.Dom.Serialization
{	
	internal class AssemblyCodeCompletionDatabase: SerializationCodeCompletionDatabase
	{
		bool useExternalProcess = true;
		string baseDir;
		bool loadError;
		bool isPackageAssembly;
		bool parsing;
		string assemblyFile;
		TargetRuntime runtime;
		TargetFramework fx;
		
		// This is the package version of the assembly. It is serialized.
		string packageVersion;
		
		public AssemblyCodeCompletionDatabase (TargetRuntime runtime, TargetFramework fx, string assemblyFile, ParserDatabase pdb): this (runtime, fx, assemblyFile, pdb, false)
		{
		}
		
		public AssemblyCodeCompletionDatabase (TargetRuntime runtime, TargetFramework fx, string assemblyFile, ParserDatabase pdb, bool isTempDatabase): base (pdb, false)
		{
			Counters.LiveAssemblyDatabases++;
			this.runtime = runtime;
			this.fx = fx;
			this.assemblyFile = assemblyFile;
			
			if (!File.Exists (assemblyFile)) {
				loadError = true;
				return;
			}
			
			string name = assemblyFile.Replace(',','_').Replace(" ","").Replace (":","").Replace(Path.DirectorySeparatorChar,'_');
			
			SystemPackage package = Runtime.SystemAssemblyService.GetPackageFromPath (assemblyFile);
			if (package != null) {
				isPackageAssembly = true;
				packageVersion = package.Name + " " + package.Version;
			}
			else
				isPackageAssembly = false;
			
			this.baseDir = ProjectDomService.CodeCompletionPath;

			if (isTempDatabase)
				SetFile (Path.GetTempFileName ());
			else {
				SetLocation (baseDir, name);
				Read ();
			}
			
			ArrayList oldFiles = new ArrayList ();
			foreach (FileEntry e in GetAllFiles ()) {
				if (e.FileName != assemblyFile)
					oldFiles.Add (e);
			}
			
			foreach (FileEntry e in oldFiles)
				RemoveFile (e.FileName);
			
			if (!files.ContainsKey (assemblyFile)) {
				AddFile (assemblyFile);
				headers ["CheckFile"] = assemblyFile;
			}
			
			FileEntry fe = GetFile (assemblyFile);
			if (IsFileModified (fe)) {
				// Update references to other assemblies
				Hashtable rs = new Hashtable ();
				foreach (string uri in ReadAssemblyReferences ()) {
					rs[uri] = null;
					if (!HasReference (uri))
						AddReference (uri);
				}
				
				foreach (ReferenceEntry re in References)
				{
					if (!rs.Contains (re.Uri))
						RemoveReference (re.Uri);
				}
			}
		}
		
		/// <summary>
		/// Searches for xml files. The .xml file extension is treated case insensitive.
		/// </summary>
		bool GetXml (string baseName, out FilePath xmlFileName)
		{
			string filePattern = Path.GetFileNameWithoutExtension (baseName) + ".*";
			foreach (string fileName in Directory.EnumerateFileSystemEntries (Path.GetDirectoryName (baseName), filePattern)) {
				if (fileName.ToLower ().EndsWith (".xml")) {
					xmlFileName = fileName;
					return true;
				}
			}
			xmlFileName = "";
			return false;
		}
		
		Dictionary<string, string> xmlDocumentation = null;
		public override string GetDocumentation (IMember member)
		{
			if (member == null)
				return null; 
			
			if (xmlDocumentation == null) {
				FilePath xmlFileName;
				GetXml (assemblyFile, out xmlFileName);
				var langCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
				
				var langDirectory = Path.Combine (Path.GetDirectoryName (assemblyFile), langCode);
				if (Directory.Exists (langDirectory)) {
					var langXmlFile = Path.Combine (langDirectory, xmlFileName.FileName);
					FilePath localizedXml;
					if (GetXml (langXmlFile, out localizedXml))
						xmlFileName = localizedXml;
				}
				
				if (!xmlFileName.IsNull && File.Exists (xmlFileName)) {
					xmlDocumentation = new Dictionary<string, string> ();
					try {
						using (var reader = new XmlTextReader (xmlFileName)) {
							while (reader.Read ()) {
								if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "member") {
									string memberName = reader.GetAttribute ("name");
									string innerXml   = reader.ReadInnerXml ();
									xmlDocumentation[memberName] = innerXml.Trim ();
								}
							}
						}
					} catch (Exception e) {
						LoggingService.LogWarning ("Can't load xml documentation: " + e.Message);
						xmlDocumentation = null;
					}
				}
			}
			if (xmlDocumentation == null)
				return null;
			string documentation;
			xmlDocumentation.TryGetValue (member.HelpUrl, out documentation);
			return documentation;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			Counters.LiveAssemblyDatabases--;
		}

		
		public override bool Write ()
		{
			if (base.Write ()) {
				Read ();
				return true;
			}
			return false;
		}
		
		static bool? regenPackageDbOnlyOnVersionChange;
		static bool RegenPackageDbOnlyOnVersionChange {
			get {
				if (!regenPackageDbOnlyOnVersionChange.HasValue)
					regenPackageDbOnlyOnVersionChange =
						String.Equals (Environment.GetEnvironmentVariable ("MD_ASM_DB_UPDATE_POLICY"), "PKGVERSION",
						               StringComparison.OrdinalIgnoreCase);
				return regenPackageDbOnlyOnVersionChange.Value;
			}
		}
		
		protected override bool IsFileModified (FileEntry file)
		{
			if (parsing)
				return false;
			
			if (!isPackageAssembly || !RegenPackageDbOnlyOnVersionChange)
				return base.IsFileModified (file);

			// This feature can be enabled by setting MD_ASM_DB_UPDATE_POLICY=PKGVERSION
			// It stops checking timestamps for packaged assemblies, and instead checks the package version.
			// This is useful for people hacking on the Mono classlibs.
			SystemPackage pkg = Runtime.SystemAssemblyService.GetPackageFromPath (assemblyFile);
			bool versionMismatch = pkg != null && packageVersion != pkg.Name + " " + pkg.Version;
			return (!file.DisableParse && (versionMismatch || file.LastParseTime == DateTime.MinValue || file.ParseErrorRetries > 0));
		}
		
		internal bool LoadError {
			get { return loadError; }
		}
		
		protected override void SerializeData (Queue dataQueue)
		{
			base.SerializeData (dataQueue);
			dataQueue.Enqueue (packageVersion);
		}
		
		protected override void DeserializeData (Queue dataQueue)
		{
			base.DeserializeData (dataQueue);
			if (isPackageAssembly) {
				if (dataQueue.Count > 0) {
					string ver = (string) dataQueue.Dequeue ();
					if (ver != null) {
						packageVersion = ver;
						return;
					}
				}
				
				// Package version not set, assume current version 
			}
		}
		
		protected override void ParseFile (string fileName, IProgressMonitor parentMonitor)
		{
			if (!File.Exists (fileName))
				return;
			IProgressMonitor monitor = parentMonitor;
			
			IDisposable timer = Counters.AssemblyParseTime.BeginTiming ();
			
			// Update the package version
			SystemPackage pkg = Runtime.SystemAssemblyService.GetPackageFromPath (assemblyFile);
			if (pkg != null)
				packageVersion = pkg.Name + " " + pkg.Version;
				
			try {
				if (parentMonitor == null)
					monitor = ProjectDomService.GetParseProgressMonitor ();
				parsing = true;
				
				if (monitor != null)
					monitor.BeginTask (string.Format (GettextCatalog.GetString ("Parsing assembly: {0}"), Path.GetFileName (fileName)), 1);
				if (useExternalProcess)
				{
					using (DatabaseGenerator helper = GetGenerator (true))
					{
						string tmpDbFile = helper.GenerateDatabase (runtime.Id, fx.Id, baseDir, assemblyFile);
						if (Disposed) {
							if (tmpDbFile != null)
								File.Delete (tmpDbFile);
							return;
						}
						if (tmpDbFile != null) {
							// GenerateDatabase generates the info in a temp file. Now we have to
							// unlock the current database file, copy the new file over it, and
							// read (and lock) the database again.
							UnlockDatabaseFile ();
							File.Delete (RealDataFile);
							File.Move (tmpDbFile, RealDataFile);
							Read ();
						}
					}
				} else {
					DomCecilCompilationUnit ainfo = DomCecilCompilationUnit.Load (fileName, false, true, false);
					
					UpdateTypeInformation (ainfo.Types, ainfo.Attributes, fileName);
					
					// Reset the error retry count, since the file has been
					// successfully parsed.
					FileEntry e = GetFile (fileName);
					e.ParseErrorRetries = 0;
					
					Flush ();
				}
			} catch (Exception ex) {
				FileEntry e = GetFile (fileName);
				e.LastParseTime = DateTime.MinValue;
				if (e.ParseErrorRetries > 0) {
					if (--e.ParseErrorRetries == 0) {
						e.DisableParse = true;
					}
				}
				else
					e.ParseErrorRetries = 3;
				if (monitor != null)
					monitor.ReportError ("Error parsing assembly: " + fileName, ex);
				throw;
			} finally {
				parsing = false;
				if (monitor != null) {
					monitor.EndTask ();
					if (parentMonitor == null) 
						monitor.Dispose ();
				}
				timer.Dispose ();
			}
			ProjectDomService.NotifyAssemblyInfoChange (fileName, assemblyFile);
		}
		
		public bool ParseInExternalProcess
		{
			get { return useExternalProcess; }
			set { useExternalProcess = value; }
		}
		
		public static void CleanDatabase (string baseDir, string name)
		{
			try {
				// Read the headers of the file without fully loading the database
				Hashtable headers = ReadHeaders (baseDir, name);
				string checkFile = (string) headers ["CheckFile"];
				int version = (int) headers ["Version"];
				if (!File.Exists (checkFile) || version != FORMAT_VERSION) {
					string dataFile = Path.Combine (baseDir, name + ".pidb");
					FileService.DeleteFile (dataFile);
					LoggingService.LogInfo ("Deleted " + dataFile);
				}
			} catch {
				// Ignore errors while cleaning
			}
		}
		
		static DatabaseGenerator GetGenerator (bool share)
		{
			if (Runtime.ProcessService != null)
				return (DatabaseGenerator) Runtime.ProcessService.CreateExternalProcessObject (typeof(DatabaseGenerator), share);
			else
				return new DatabaseGenerator ();
		}
		
		public IEnumerable<string> ReadAssemblyReferences ()
		{
			AssemblyDefinition asm = AssemblyDefinition.ReadAssembly (assemblyFile);

			foreach (AssemblyNameReference aname in asm.MainModule.AssemblyReferences) {
				string afile = runtime.AssemblyContext.GetAssemblyLocation (aname.FullName, fx);
				if (afile != null)
					yield return "Assembly:" + runtime.Id + ":" + Path.GetFullPath (afile);
			}
		}
	}
	
	internal class DatabaseGenerator: RemoteProcessObject
	{
		public string GenerateDatabase (string runtimeId, TargetFrameworkMoniker fxId, string baseDir, string assemblyFile)
		{
			try {
				Runtime.Initialize (false);
				ParserDatabase pdb = new ParserDatabase ();
				TargetRuntime runtime = Runtime.SystemAssemblyService.GetTargetRuntime (runtimeId);
				if (runtime == null)
					LoggingService.LogError ("Runtime '{0}' not found", runtimeId);
				TargetFramework fx = Runtime.SystemAssemblyService.GetTargetFramework (fxId);

				// Generate the new db in a temp file. The main process will move the file if required.
				using (var db = new AssemblyCodeCompletionDatabase (runtime, fx, assemblyFile, pdb, true)) {
					if (db.LoadError)
						throw new InvalidOperationException ("Could find assembly: " + assemblyFile);
					db.ParseInExternalProcess = false;
					db.ParseAll ();
					db.Write ();
					return db.RealDataFile;
				}
			} catch (Exception ex) {
				Console.WriteLine (ex);
				return null;
			}
		}
	}
	
}
