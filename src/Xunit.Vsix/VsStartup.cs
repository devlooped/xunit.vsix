using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

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
		static VsRemoteRunner runner;

		/// <summary>
		/// This is an internal method, and is not intended to be called from end-user code.
		/// </summary>
		public static bool Start ()
		{
			try {
				GlobalServices.Initialize ();

				runner = new VsRemoteRunner ();
				runner.Start ();

				LocalResolver.Initialize (Directory.GetCurrentDirectory ());

				return true;
			} catch (Exception) {
				return false;
			}
		}

		static class LocalResolver
		{
			/// <summary>
			/// Initializes the resolver to lookup assemblies from the 
			/// specified local directory.
			/// </summary>
			/// <param name="localDirectory">The local directory to add to the 
			/// assembly resolve probing.</param>
			public static void Initialize (string localDirectory)
			{
				var assemblyNames = LoadAssemblyNames (localDirectory);

				AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
					// NOTE: since we load our full names only in the local assembly set, 
					// we will only return our assembly version if it matches exactly the 
					// full name of the received arguments.
					if (assemblyNames.ContainsKey (args.Name)) {
						//var asm = Assembly.Load (assemblyNames[args.Name]);
						var asm = Assembly.LoadFrom (assemblyNames[args.Name].Item2);
						return asm;
					}

					return null;
				};
			}

			private static Dictionary<string, Tuple<AssemblyName, string>> LoadAssemblyNames (string localDirectory)
			{
				var names = new Dictionary<string, Tuple<AssemblyName, string>> ();
				foreach (var file in Directory.EnumerateFiles (localDirectory, "*.dll")) {
					try {
						names.Add (AssemblyName.GetAssemblyName (file).FullName, Tuple.Create(AssemblyName.GetAssemblyName (file), file));
					} catch (System.Security.SecurityException) {
					} catch (BadImageFormatException) {
					} catch (FileLoadException) {
					}
				}

				return names;
			}
		}
	}
}
