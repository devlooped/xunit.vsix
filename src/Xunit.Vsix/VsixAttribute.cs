using System;
using System.ComponentModel;

namespace Xunit
{
    /// <summary>
    /// Specifies class or assembly level defaults for all <see cref="VsixFactAttribute"/> annotated
    /// tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = false)]
    public class VsixAttribute : Attribute, IVsixAttribute
    {
        /// <summary>
        /// Default timeout for tests unless specified, equals 30 seconds.
        /// </summary>
        public const int DefaultTimeout = 30000;

        /// <summary>
        /// Sets default values for properties other than the VS version.
        /// </summary>
        public VsixAttribute()
            : this(default(string[]))
        {
        }

        /// <summary>
        /// Sets the default Visual Studio version for the entire class or assembly.
        /// </summary>
        public VsixAttribute(string visualStudioVersion)
            : this(new string[] { visualStudioVersion })
        {
        }

        /// <summary>
        /// Sets the default Visual Studio versions for the entire class or assembly.
        /// </summary>
        public VsixAttribute(params string[] visualStudioVersions)
        {
            if (visualStudioVersions != null && visualStudioVersions.Length != 0)
                VisualStudioVersions = visualStudioVersions;

            Timeout = DefaultTimeout;
        }

        /// <summary>
        /// Minimum Visual Studio to run the given test in. See
        /// <see cref="VisualStudioVersion"/> for available well-known values.
        /// </summary>
        public string MinimumVisualStudioVersion { get; set; }

        /// <summary>
        /// Maximum Visual Studio to run the given test in. See
        /// <see cref="VisualStudioVersion"/> for available well-known values.
        /// </summary>
        public string MaximumVisualStudioVersion { get; set; }

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
        /// test run. Default is false.
        /// </summary>
        public bool NewIdeInstance { get; set; }

        /// <summary>
        /// Timeout (in milliseconds) for the test to complete its run, excluding the
        /// time that it takes to launch VS and set up the test run context. 
        /// Default is 60 seconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Timeout in seconds for the test to complete its run, excluding the
        /// time that it takes to launch VS and set up the test run context.
        /// </summary>
        /// <devdoc>
        /// Backwards-compatible property.
        /// </devdoc>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int TimeoutSeconds
        {
            get => Timeout / 1000;
            set => Timeout = value * 1000;
        }

        /// <summary>
        /// Whether to retry once in a clean Visual Studio instance a failing
        /// test. Defaults to <see langword="false">false</see>.
        /// </summary>
        public bool RecycleOnFailure { get; set; }

        /// <summary>
        /// Whether to run the tests in the UI thread.
        /// Defaults to <see langword="false">false</see>.
        /// </summary>
        public bool RunOnUIThread { get; set; }
    }
}
