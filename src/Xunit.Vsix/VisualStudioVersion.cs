
namespace Xunit
{
    /// <summary>
    /// Suggested values for the Visual Studio Version to run tests against.
    /// </summary>
    public static class VisualStudioVersion
    {
        /// <summary>
        /// Specifies that the test should be run against all installed 
        /// Visual Studio versions.
        /// </summary>
        public const string All = "*";

        /// <summary>
        /// Specifies that the test should be run against the currently running 
        /// VS version (or the latest if not run within an IDE integrated runner).
        /// </summary>
        public const string Current = "Current";

        /// <summary>
        /// Specifies that the test should be run against the latest installed
        /// VS version (even if different from the current version when run 
        /// from within an IDE integrated runner).
        /// </summary>
        public const string Latest = "Latest";

        /// <summary>
        /// Visual Studio 2017.
        /// </summary>
        public const string VS2017 = "[15.0,15.99]";

        /// <summary>
        /// Visual Studio 2019.
        /// </summary>
        public const string VS2019 = "[16.0,16.99]";

        /// <summary>
        /// Visual Studio 2022.
        /// </summary>
        public const string VS2022 = "[17.0,17.99]";
    }
}
