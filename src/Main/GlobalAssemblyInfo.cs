﻿// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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

/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
//                                                                                         //
// DO NOT EDIT GlobalAssemblyInfo.cs, it is recreated using AssemblyInfo.template whenever //
// ICSharpCode.Core is compiled.                                                           //
//                                                                                         //
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////

using System.Resources;
using System.Reflection;

[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: AssemblyCompany("VisualLabelDesigner")]
[assembly: AssemblyProduct("VisualLabelDesigner")]
[assembly: AssemblyCopyright("2000-2023 VisualLabelDesigner.github.io")]
[assembly: AssemblyVersion(RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision)]
[assembly: AssemblyInformationalVersion(RevisionClass.FullVersion + "-0e58df71")]
[assembly: NeutralResourcesLanguage("en-US")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly",
	Justification = "AssemblyInformationalVersion does not need to be a parsable version")]

// DO NOT FORGET TO EDIT the GlobalAssemblyInfo.vb.template!
internal static class RevisionClass
{
	public const string Major = "1";
	public const string Minor = "0";
	public const string Build = "0";
	public const string Revision = "0001";
	public const string VersionName = null; // "" is not valid for no version name, you have to use null if you don't want a version name (eg "Beta 1")
	
	public const string FullVersion = Major + "." + Minor + "." + Build + ".0001";
}
