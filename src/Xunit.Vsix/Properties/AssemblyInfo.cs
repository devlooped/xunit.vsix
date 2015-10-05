using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle ("Xunit.Vsix")]
[assembly: AssemblyDescription ("")]
[assembly: AssemblyConfiguration ("")]
[assembly: AssemblyCompany ("Mobile Essentials")]
[assembly: AssemblyProduct ("Xunit.Vsix")]
[assembly: AssemblyCopyright ("Copyright ©  2015")]
[assembly: AssemblyTrademark ("")]
[assembly: AssemblyCulture ("")]

[assembly: ComVisible (false)]

[assembly: Guid ("ea6250cb-f20a-4b92-ac35-365d902c225f")]

[assembly: AssemblyVersion (ThisAssembly.Git.SemVer.Major + "." + ThisAssembly.Git.SemVer.Minor + "." + ThisAssembly.Git.SemVer.Patch)]
[assembly: AssemblyInformationalVersion (
	ThisAssembly.Git.SemVer.Major + "." +
	ThisAssembly.Git.SemVer.Minor + "." +
	ThisAssembly.Git.SemVer.Patch + "-" +
	ThisAssembly.Git.Branch + "+" +
	ThisAssembly.Git.Commit)]

[assembly: InternalsVisibleTo ("xunit.vsix.tests")]