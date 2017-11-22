using System;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary>
    /// Marks a VSIX test method as being a data theory. Data theories are tests which are fed
    /// various bits of data from a data source, mapping to parameters on the test method.
    /// If the data source contains multiple rows, then the test method is executed
    /// multiple times (once with each data row). Data is provided by attributes which
    /// derive from <see cref="DataAttribute"/> (notably, <see cref="InlineDataAttribute"/> and
    /// <see cref="MemberDataAttribute"/>).
    /// </summary>
    [XunitTestCaseDiscoverer(Constants.RootNamespace + "." + nameof(VsixTheoryDiscoverer), Constants.ThisAssembly)]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class VsixTheoryAttribute : VsixFactAttribute
    {
        /// <summary>
        /// Sets default values for properties other than the VS version.
        /// </summary>
        public VsixTheoryAttribute()
        {
        }

        /// <summary>
        /// Overrides the default Visual Studio version for the test.
        /// </summary>
        public VsixTheoryAttribute(string visualStudioVersion)
            : base(visualStudioVersion)
        {
        }

        /// <summary>
        /// Overrides the default Visual Studio versions for the test.
        /// </summary>
        public VsixTheoryAttribute(params string[] visualStudioVersions)
            : base(visualStudioVersions)
        {
        }
    }
}
