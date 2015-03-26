using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xunit.Vsx.Tests
{
	public class Misc
	{
		[Fact]
		public void when_waiting_or_timeout_then_can_catch_timeout ()
		{
			var result = 0;

			var tasks = new Task[] 
			{
				Task.Run(() => {
					Thread.Sleep (5000);
					result = 5;
				}),
				Task.Delay(1000).ContinueWith(t => {
					throw new TimeoutException();
				}),
			};

			try {
				var index = Task.WaitAny (tasks);
				if (tasks[index].Exception != null)
					throw tasks[index].Exception.Unwrap ();

				Assert.NotEqual (result, 5);
				Assert.False (true, "Should have failed!");
			} catch (TimeoutException te) {
				Assert.Equal (result, 0);
			}
		}

		[Fact]
		public void when_enumerating_dtes_then_succeeds ()
		{
			Assert.True (GetAllDtes ().Any ());
		}

		private IEnumerable<EnvDTE.DTE> GetAllDtes ()
		{
			IRunningObjectTable table;
			IEnumMoniker moniker;
			if (ErrorHandler.Failed (NativeMethods.GetRunningObjectTable (0, out table)))
				yield break;

			table.EnumRunning (out moniker);
			moniker.Reset ();
			var pceltFetched = IntPtr.Zero;
			var rgelt = new IMoniker[1];

			while (moniker.Next (1, rgelt, pceltFetched) == 0) {
				IBindCtx ctx;
				if (!ErrorHandler.Failed (NativeMethods.CreateBindCtx (0, out ctx))) {
					string displayName;
					rgelt[0].GetDisplayName (ctx, null, out displayName);
					if (displayName.Contains ("VisualStudio.DTE")) {
						object comObject;
						table.GetObject (rgelt[0], out comObject);
						yield return (EnvDTE.DTE)comObject;
					}
				}
			}
		}
	}
}
