
namespace Xunit
{
    /// <summary>
    /// Suggested values for the Visual Studio Version to run tests against.
    /// </summary>
    public static class VisualStudioVersion
    {
        /// <summary>
        /// Specifies that the test should be run against all installed 
        /// Visual Studio versions that have a corresponding VSSDK installed.
        /// </summary>
        public const string All = "All";

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
        /// Visual Studio 2012.
        /// </summary>
        public const string VS2012 = "11.0";

        /// <summary>
        /// Visual Studio 2013.
        /// </summary>
        public const string VS2013 = "12.0";

        /// <summary>
        /// Visual Studio 2015.
        /// </summary>
        public const string VS2015 = "14.0";
    }
}
