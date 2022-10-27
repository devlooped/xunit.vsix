﻿
namespace Xunit
{
    /// <summary>
    /// Represents the metadata available for VSX integration tests
    /// at the assembly, class or method level.
    /// </summary>
    public interface IVsixAttribute
    {
        /// <summary>
        /// Whether to start a new instance of Visual Studio for each
        /// test run.
        /// </summary>
        bool NewIdeInstance { get; }

        /// <summary>
        /// The root suffix for Visual Studio, like "Exp" (the default).
        /// </summary>
        string RootSuffix { get; }

        /// <summary>
        /// Timeout (in milliseconds) for the test to complete its run, excluding the
        /// time that it takes to launch VS and set up the test run context.
        /// </summary>
        int Timeout { get; }

        /// <summary>
        /// Specific versions of Visual Studio to run the given test in. See
        /// <see cref="VisualStudioVersion"/> for available well-known values.
        /// </summary>
        string[] VisualStudioVersions { get; }

        /// <summary>
        /// Minimum Visual Studio to run the given test in. See
        /// <see cref="VisualStudioVersion"/> for available well-known values.
        /// </summary>
        string MinimumVisualStudioVersion { get; }

        /// <summary>
        /// Maximum Visual Studio to run the given test in. See
        /// <see cref="VisualStudioVersion"/> for available well-known values.
        /// </summary>
        string MaximumVisualStudioVersion { get; }

        /// <summary>
        /// Whether to retry once in a clean Visual Studio instance a failing
        /// test. Defaults to <see langword="false">false</see>.
        /// </summary>
        bool RecycleOnFailure { get; }

        /// <summary>
        /// Whether to run the tests in the UI thread.
        /// Defaults to <see langword="false">false</see>.
        /// </summary>
        bool RunOnUIThread { get; }
    }
}