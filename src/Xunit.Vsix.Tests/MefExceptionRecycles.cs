using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Xunit
{
	public class MefExceptionRecycles
	{
		public MefExceptionRecycles ()
		{
			Trace.WriteLine ("Constructing...");

			var hosting = AppDomain.CurrentDomain.GetAssemblies()
				.Where(asm => asm.GetName().Name == "Microsoft.VisualStudio.ExtensibilityHosting")
				.OrderByDescending(asm => asm.GetName().Version.ToString())
				.First();
			var type = hosting.GetType("Microsoft.VisualStudio.ExtensibilityHosting.InvalidMEFCacheException", true);
			var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new [] { typeof(AssemblyName), typeof(Int64), typeof(Int64) }, null);
			var ex = (Exception)ctor.Invoke(new object[] {  null, 0L, 0L });

			throw ex;
		}

		[VsixFact(VisualStudioVersion.VS2015, Skip = "Manual faking of MEF invalid cache.")]
		public void when_throwing_mef_exception_then_recycles ()
		{
		}
	}
}
