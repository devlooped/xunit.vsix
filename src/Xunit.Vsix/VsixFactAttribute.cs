﻿using System;
using System.ComponentModel;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary>
    /// Attribute that is applied to a method to indicate that it is a Visual Studio
    /// integration fact that should be run by the test runner.
    /// </summary>
    [XunitTestCaseDiscoverer(Constants.RootNamespace + "." + nameof(VsixFactDiscoverer), Constants.ThisAssembly)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class VsixFactAttribute : FactAttribute, IVsixAttribute
    {
        /// <summary>
        /// Sets default values for properties other than the VS version.
        /// </summary>
        public VsixFactAttribute() { }

        /// <summary>
        /// Overrides the default Visual Studio version for the test.
        /// </summary>
        public VsixFactAttribute(string visualStudioVersion)
            : this(new string[] { visualStudioVersion })
        {
        }

        /// <summary>
        /// Overrides the default Visual Studio versions for the test.
        /// </summary>
        public VsixFactAttribute(params string[] visualStudioVersions)
        {
            VisualStudioVersions = visualStudioVersions;
            Timeout = 1000;
        }

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
        /// Timeout (in milliseconds) for the test to complete its run, excluding the
        /// time that it takes to launch VS and set up the test run context.
        /// </summary>
        public override int Timeout
        {
            get => base.Timeout;
            set => base.Timeout = value;
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
        /// Specify an empty string or "." for the regular Visual Studio 
        /// instance/hive.
        /// </summary>
        public string RootSuffix { get; set; }

        /// <summary>
        /// Whether to start a new instance of Visual Studio for each
        /// test run.
        /// </summary>
        public bool NewIdeInstance { get; set; }

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
