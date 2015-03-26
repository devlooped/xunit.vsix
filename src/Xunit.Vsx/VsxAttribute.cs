using System;

namespace Xunit
{
	/// <summary>
	/// Specifies class or assembly level defaults for all <see cref="VsxFactAttribute"/> annotated 
	/// tests.
	/// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
	public class VsxAttribute : Attribute, IVsxAttribute
	{
		/// <summary>
		/// Sets default values for properties other than the VS version.
		/// </summary>
		public VsxAttribute ()
			: this(default(string[]))
		{
		}

		/// <summary>
		/// Sets the default Visual Studio version for the entire class or assembly.
		/// </summary>
		public VsxAttribute (string visualStudioVersion)
			: this(new string[] { visualStudioVersion })
		{
		}

		/// <summary>
		/// Sets the default Visual Studio versions for the entire class or assembly.
		/// </summary>
		public VsxAttribute (params string[] visualStudioVersions)
		{
			VisualStudioVersions = visualStudioVersions;
			TimeoutSeconds = -1;
		}

		/// <summary>
		/// Versions of Visual Studio to run the given test in. See 
		/// <see cref="VisualStudioVersion"/> for available well-known values.
		/// </summary>
		public string[] VisualStudioVersions { get; private set; }

		/// <summary>
		/// The root suffix for Visual Studio, like "Exp" (the default).
		/// </summary>
		public string RootSuffix { get; set; }

		/// <summary>
		/// Whether to start a new instance of Visual Studio for each 
		/// test run.
		/// </summary>
		public bool NewIdeInstance { get; set; }

		/// <summary>
		/// Timeout in seconds for the test to complete its run, excluding the 
		/// time that it takes to launch VS and set up the test run context.
		/// </summary>
		public int TimeoutSeconds { get; set; }
	}
}
