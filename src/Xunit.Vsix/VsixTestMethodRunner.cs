﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	class VsixTestMethodRunner : XunitTestMethodRunner
	{
		readonly IVsClient vsClient;


		public VsixTestMethodRunner (IVsClient vsClient, ITestMethod testMethod, IReflectionTypeInfo @class, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
			: base (testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, new object[0])
		{
			this.vsClient = vsClient;
		}

		protected override Task<RunSummary> RunTestCaseAsync (IXunitTestCase testCase)
		{
			return vsClient.RunAsync((VsixTestCase)testCase, MessageBus, Aggregator);
		}

		protected override void BeforeTestMethodFinished ()
		{
			base.BeforeTestMethodFinished ();
			if (Aggregator.HasExceptions && Aggregator.ToException () is TimeoutException)
				vsClient.Shutdown ();
		}
	}
}
