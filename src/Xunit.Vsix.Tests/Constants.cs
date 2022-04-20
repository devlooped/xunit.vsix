using System.IO;

namespace Xamarin.VisualStudio
{
    static class Constants
    {
        public static string BlankSolution
        {
            get
            {
                return new FileInfo(@"Content\Blank.sln").FullName;
            }
        }

        public static string SingleProjectSolution
        {
            get
            {
                return new FileInfo(@"Content\SingleProject\SingleProject.sln").FullName;
            }
        }
    }
}
