using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using Xunit.Properties;

namespace Xunit
{
	/// <summary>
	/// This is an internal class, and is not intended to be called from end-user code.
	/// </summary>
	/// <devdoc>
	/// This is the static entry point class invoked by the managed injector.
	/// It doesn't do anything other than spinning up a new instance of the
	/// <see cref="VsRemoteRunner"/> which does the actual work.
	/// </devdoc>
	[EditorBrowsable (EditorBrowsableState.Never)]
	public static class VsStartup
	{
		static readonly TraceSource tracer = Constants.Tracer;
		static VsRemoteRunner runner;
		static Dictionary<string, string> localAssemblyNames;

		/// <summary>
		/// This is an internal method, and is not intended to be called from end-user code.
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public static bool Start ()
		{
			AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
			localAssemblyNames = GetLocalAssemblyNames (Path.GetDirectoryName(typeof(VsStartup).Assembly.ManifestModule.FullyQualifiedName));
			try {
				tracer.TraceEvent (TraceEventType.Verbose, 0, Strings.VsStartup.Starting);
				GlobalServices.Initialize ();

				runner = new VsRemoteRunner ();
				runner.Start ();

				tracer.TraceInformation (Strings.VsStartup.Started);
				return true;
			} catch (Exception ex) {
				tracer.TraceEvent (TraceEventType.Error, 0, Strings.VsStartup.Failed + Environment.NewLine + ex.ToString ());
				return false;
			}
		}

		static Dictionary<string, string> GetLocalAssemblyNames (string localDirectory)
		{
			var names = new Dictionary<string, string>();
			foreach (var file in Directory.EnumerateFiles (localDirectory, "*.dll")) {
				try {
					names.Add (AssemblyName.GetAssemblyName (file).FullName, file);
				} catch (SecurityException) {
				} catch (BadImageFormatException) {
				} catch (FileLoadException) {
				}
			}

			return names;
		}

		static Assembly OnAssemblyResolve (object sender, ResolveEventArgs args)
		{
			// NOTE: since we load our full names only in the local assembly set,
			// we will only return our assembly version if it matches exactly the
			// full name of the received arguments.
			if (localAssemblyNames.ContainsKey (args.Name))
				return Assembly.LoadFrom (localAssemblyNames[args.Name]);

			return null;
		}
	}
}
