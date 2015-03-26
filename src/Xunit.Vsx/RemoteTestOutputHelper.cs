using System;
using Xunit.Abstractions;

namespace Xunit
{
	class RemoteTestOutputHelper : MarshalByRefObject, ITestOutputHelper
	{
		ITestOutputHelper innerHelper;

		public RemoteTestOutputHelper (ITestOutputHelper innerHelper)
		{
			this.innerHelper = innerHelper;
		}

		public void WriteLine (string format, params object[] args)
		{
			innerHelper.WriteLine (format, args);
		}

		public void WriteLine (string message)
		{
			innerHelper.WriteLine (message);
		}
	}
}
