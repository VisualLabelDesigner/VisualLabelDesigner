// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace UpdateAssemblyInfo
{
	// Updates the version numbers in the assembly information.
	class MainClass
	{
		const string BaseCommit = "4ba2785f66bebeaa21c68cf4f6733fc19ddb0d9d";
		const int BaseCommitRev = 0;
		
		const string globalAssemblyInfoTemplateFile = "src/Main/GlobalAssemblyInfo.cs.template";
		static readonly TemplateFile[] templateFiles = {
			new TemplateFile {
				Input = globalAssemblyInfoTemplateFile,
				Output = "src/Main/GlobalAssemblyInfo.cs"
			},
			new TemplateFile {
				Input = "src/Main/GlobalAssemblyInfo.vb.template",
				Output = "src/Main/GlobalAssemblyInfo.vb"
			},
			new TemplateFile {
				Input = "src/Main/VisualLabelDesigner/app.template.config",
				Output = "src/Main/VisualLabelDesigner/app.config"
			},
			new TemplateFile {
				Input = "src/Setup/VisualLabelDesigner.Setup.wixproj.user.template",
				Output = "src/Setup/VisualLabelDesigner.Setup.wixproj.user"
			},
			new TemplateFile {
				Input = "src/AddIns/Misc/UsageDataCollector/UsageDataCollector.AddIn/AnalyticsMonitor.AppProperties.template",
				Output = "src/AddIns/Misc/UsageDataCollector/UsageDataCollector.AddIn/AnalyticsMonitor.AppProperties.cs"
			}
		};
		
		class TemplateFile
		{
			public string Input, Output;
		}
		
		public static int Main(string[] args)
		{
			try {
				string exeDir = Path.GetDirectoryName(typeof(MainClass).Assembly.Location);
				bool createdNew;
				using (Mutex mutex = new Mutex(true, "VisualLabelDesignerUpdateAssemblyInfo" + exeDir.GetHashCode(), out createdNew)) {
					if (!createdNew) {
						// multiple calls in parallel?
						// it's sufficient to let one call run, so just wait for the other call to finish
						try {
							mutex.WaitOne(10000);
						} catch (AbandonedMutexException) {
						}
						return 0;
					}
					if (!File.Exists("VisualLabelDesigner.sln")) {
						string mainDir = Path.GetFullPath(Path.Combine(exeDir, "../../../../.."));
						if (File.Exists(mainDir + "\\VisualLabelDesigner.sln")) {
							Directory.SetCurrentDirectory(mainDir);
						}
					}
					if (!File.Exists("VisualLabelDesigner.sln")) {
						Console.WriteLine("Working directory must be VisualLabelDesigner!");
						return 2;
					}
					RetrieveRevisionNumber();
					for (int i = 0; i < args.Length; i++) {
						if (args[i] == "--branchname" && i + 1 < args.Length && !string.IsNullOrEmpty(args[i+1]))
							gitBranchName = args[i + 1];
					}
					UpdateFiles();
					if (args.Contains("--REVISION")) {
						var doc = new XDocument(new XElement(
							"versionInfo",
							new XElement("version", fullVersionNumber),
							new XElement("revision", revisionNumber),
							new XElement("commitHash", gitCommitHash),
							new XElement("branchName", gitBranchName),
							new XElement("versionName", versionName)
						));
						doc.Save("REVISION");
					}
					return 0;
				}
			} catch (Exception ex) {
				Console.WriteLine(ex);
				return 3;
			}
		}
		
		static void UpdateFiles()
		{
			foreach (var file in templateFiles) {
				string content;
				using (StreamReader r = new StreamReader(file.Input)) {
					content = r.ReadToEnd();
				}
				content = content.Replace("$INSERTVERSION$", fullVersionNumber);
				content = content.Replace("$INSERTMAJORVERSION$", majorVersionNumber);
				content = content.Replace("$INSERTREVISION$", revisionNumber);
				content = content.Replace("$INSERTCOMMITHASH$", gitCommitHash);
				content = content.Replace("$INSERTSHORTCOMMITHASH$", gitCommitHash.Substring(0, 8));
				content = content.Replace("$INSERTDATE$", DateTime.Now.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture));
				content = content.Replace("$INSERTYEAR$", DateTime.Now.Year.ToString());
				content = content.Replace("$INSERTBRANCHNAME$", gitBranchName);
				bool isDefaultBranch = string.IsNullOrEmpty(gitBranchName) || gitBranchName == "master" || char.IsDigit(gitBranchName, 0);
				content = content.Replace("$INSERTBRANCHPOSTFIX$", isDefaultBranch ? "" : ("-" + gitBranchName));
				
				content = content.Replace("$INSERTVERSIONNAME$", versionName ?? "");
				content = content.Replace("$INSERTVERSIONNAMEPOSTFIX$", string.IsNullOrEmpty(versionName) ? "" : "-" + versionName);
				
				if (File.Exists(file.Output)) {
					using (StreamReader r = new StreamReader(file.Output)) {
						if (r.ReadToEnd() == content) {
							// nothing changed, do not overwrite file to prevent recompilation
							// every time.
							continue;
						}
					}
				}
				using (StreamWriter w = new StreamWriter(file.Output, false, Encoding.UTF8)) {
					w.Write(content);
				}
			}
		}
		
		static void GetMajorVersion()
		{
			majorVersionNumber = "?";
			fullVersionNumber = "?";
			versionName = null;
			// Get main version from startup
			using (StreamReader r = new StreamReader(globalAssemblyInfoTemplateFile)) {
				string line;
				while ((line = r.ReadLine()) != null) {
					string search = "string Major = \"";
					int pos = line.IndexOf(search);
					if (pos >= 0) {
						int e = line.IndexOf('"', pos + search.Length + 1);
						majorVersionNumber = line.Substring(pos + search.Length, e - pos - search.Length);
					}
					search = "string Minor = \"";
					pos = line.IndexOf(search);
					if (pos >= 0) {
						int e = line.IndexOf('"', pos + search.Length + 1);
						majorVersionNumber = majorVersionNumber + "." + line.Substring(pos + search.Length, e - pos - search.Length);
					}
					search = "string Build = \"";
					pos = line.IndexOf(search);
					if (pos >= 0) {
						int e = line.IndexOf('"', pos + search.Length + 1);
						fullVersionNumber = majorVersionNumber + "." + line.Substring(pos + search.Length, e - pos - search.Length) + "." + revisionNumber;
					}
					search = "string VersionName = \"";
					pos = line.IndexOf(search);
					if (pos >= 0) {
						int e = line.IndexOf('"', pos + search.Length + 1);
						versionName = line.Substring(pos + search.Length, e - pos - search.Length);
					}
				}
			}
		}
		
		static void SetVersionInfo(string fileName, Regex regex, string replacement)
		{
			string content;
			using (StreamReader inFile = new StreamReader(fileName)) {
				content = inFile.ReadToEnd();
			}
			string newContent = regex.Replace(content, replacement);
			if (newContent == content)
				return;
			using (StreamWriter outFile = new StreamWriter(fileName, false, Encoding.UTF8)) {
				outFile.Write(newContent);
			}
		}
		
		#region Retrieve Revision Number
		static string revisionNumber;
		static string majorVersionNumber;
		static string fullVersionNumber;
		/// <summary>
		/// Descriptive version name, e.g. 'Beta 3'
		/// </summary>
		static string versionName;
		static string gitCommitHash;
		static string gitBranchName;
		
		static void RetrieveRevisionNumber()
		{
			if (revisionNumber == null) {
				if (Directory.Exists(".git")) {
					try {
						ReadRevisionNumberFromGit();
						ReadBranchNameFromGit();
					} catch (Exception ex) {
						Console.WriteLine(ex.ToString());
					}
				} else {
					Console.WriteLine("There's no git working copy in " + Path.GetFullPath("."));
				}
			}
			
			if (revisionNumber == null) {
				ReadRevisionFromFile();
			}
			GetMajorVersion();
		}
		
		static void ReadRevisionNumberFromGit()
		{
			ProcessStartInfo info = new ProcessStartInfo("cmd", "/c git rev-list " + BaseCommit + "..HEAD");
			string path = Environment.GetEnvironmentVariable("PATH");
			path += ";" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "git\\bin");
			info.EnvironmentVariables["PATH"] =  path;
			info.RedirectStandardOutput = true;
			info.UseShellExecute = false;
			using (Process p = Process.Start(info)) {
				string line;
				int revNum = BaseCommitRev;
				while ((line = p.StandardOutput.ReadLine()) != null) {
					if (gitCommitHash == null) {
						// first entry is HEAD
						gitCommitHash = line;
					}
					revNum++;
				}
				p.WaitForExit();
				if (p.ExitCode != 0)
					throw new Exception("git-rev-list exit code was " + p.ExitCode);
				// Only set revisionNuber once we ensured the operation was successful,
				// so that we retrieve the number from the REVISION file in case of errors
				revisionNumber = revNum.ToString();
			}
		}
		
		static void ReadBranchNameFromGit()
		{
			ProcessStartInfo info = new ProcessStartInfo("cmd", "/c git branch --no-color");
			string path = Environment.GetEnvironmentVariable("PATH");
			path += ";" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "git\\bin");
			info.EnvironmentVariables["PATH"] =  path;
			info.RedirectStandardOutput = true;
			info.UseShellExecute = false;
			using (Process p = Process.Start(info)) {
				string line;
				gitBranchName = "(no branch)";
				while ((line = p.StandardOutput.ReadLine()) != null) {
					if (line.StartsWith("* ", StringComparison.Ordinal)) {
						gitBranchName = line.Substring(2);
					}
				}
				p.WaitForExit();
				if (p.ExitCode != 0)
					throw new Exception("git-branch exit code was " + p.ExitCode);
			}
		}
		
		static void ReadRevisionFromFile()
		{
			try {
				XDocument doc = XDocument.Load("REVISION");
				revisionNumber = (string)doc.Root.Element("revision");
				gitCommitHash = (string)doc.Root.Element("commitHash");
				gitBranchName = (string)doc.Root.Element("branchName");
			} catch (Exception e) {
				Console.WriteLine(e.Message);
				Console.WriteLine();
				Console.WriteLine("The revision number of the SharpDevelop version being compiled could not be retrieved.");
				Console.WriteLine();
				Console.WriteLine("Build continues with revision number '0'...");
				
				revisionNumber = null;
			}
			if (revisionNumber == null || revisionNumber.Length == 0) {
				revisionNumber = "0";
				gitCommitHash = "0000000000000000000000000000000000000000";
				//throw new ApplicationException("Error reading revision number");
			}
		}
		#endregion
	}
}
