
namespace Xunit
{
	/// <summary>
	/// Special names of attributes and types.
	/// </summary>
	static class SpecialNames
	{
		/// <summary>
		/// This assembly name, for use in Xunit attributes that need to specify an
		/// assembly name.
		/// </summary>
		public const string ThisAssembly = "xunit.vsix";

		/// <summary>
		/// Type name of the <see cref="Xunit.VsixFactDiscoverer"/> class.
		/// </summary>
		public const string VsixFactDiscoverer = "Xunit.VsixFactDiscoverer";

		/// <summary>
		/// Type name of the <see cref="Xunit.VsixTheoryDiscoverer"/> class.
		/// </summary>
		public const string VsixTheoryDiscoverer = "Xunit.VsixTheoryDiscoverer";

		public static class VsixAttribute
		{
			///// <summary>
			///// Trait for requesting the test to be run for one or more specific Visual Studio versions.
			///// If not specified, defaults to the currently running version (if run from an IDE
			///// integrated runner), or the latest version that has an installed VSSDK.
			///// </summary>
			public const string VisualStudioVersions = "VisualStudioVersions";

			/// <summary>
			/// Overrides the default suffix to use for the launched Visual Studio instance, which
			/// is "Exp" (for the default VS Experimental instance).
			/// </summary>
			public const string RootSuffix = "RootSuffix";

			/// <summary>
			/// Specifies that the test should be run in its own separated Visual Studio instance,
			/// rather than reusing the IDE instance (which is automatically done for each
			/// VS version).
			/// </summary>
			public const string NewIdeInstance = "NewIdeInstance";

			/// <summary>
			/// The maximum timeout in seconds to wait for a VSX test to complete running.
			/// </summary>
			public const string TimeoutSeconds = "TimeoutSeconds";
		}
	}
}
