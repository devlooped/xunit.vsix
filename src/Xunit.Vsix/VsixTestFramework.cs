using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Linq;
using System.IO;
using Xunit.Properties;

namespace Xunit
{
    class VsixTestFramework : XunitTestFramework
    {
        static TraceSource tracer = Constants.Tracer;

        public VsixTestFramework(IMessageSink messageSink) : base(new TracingMessageSink(messageSink, tracer))
        {
            tracer.Switch.Level = SourceLevels.Error;
            Trace.AutoFlush = true;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            tracer.TraceEvent(TraceEventType.Error, 0, e.Exception.Flatten().InnerException.ToString());
            e.SetObserved();
        }

        void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            tracer.TraceEvent(TraceEventType.Error, 0, ((Exception)e.ExceptionObject).ToString());
        }

        protected override ITestFrameworkDiscoverer CreateDiscoverer(IAssemblyInfo assemblyInfo)
        {
            SetupTracing(assemblyInfo);

            return new XunitTestFrameworkDiscoverer(assemblyInfo, SourceInformationProvider, DiagnosticMessageSink, null);
        }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new VsixTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }

        static void SetupTracing(IAssemblyInfo assemblyInfo)
        {
            var attr = assemblyInfo.GetCustomAttributes(typeof(VsixRunnerAttribute)).FirstOrDefault();
            tracer.Switch.Level = attr?
                .GetInitializedArgument<SourceLevels?>(nameof(VsixRunnerAttribute.TraceLevel))
                .GetValueOrDefault(SourceLevels.Error) ?? SourceLevels.Error;

            var logFile = Path.ChangeExtension(assemblyInfo.AssemblyPath, ".log");
            if (File.Exists(logFile))
            {
                try
                {
                    File.Delete(logFile);
                }
                catch (IOException)
                {
                }
            }

            if (!tracer.Listeners.OfType<TraceListener>().Any(x => x.Name == assemblyInfo.Name))
            {
                var listener = new TextWriterTraceListener(logFile, assemblyInfo.Name);
                tracer.Listeners.Add(listener);
                Trace.Listeners.Add(listener);
            }
        }

        class TracingMessageSink : IMessageSink
        {
            IMessageSink innerSink;
            TraceSource tracer;

            static TracingMessageSink()
            {
#if DEBUG
                if (File.Exists("xunit.vsix.log"))
                {
                    try
                    {
                        File.Delete("xunit.vsix.log");
                    }
                    catch (IOException) { }
                }
#endif
            }

            public TracingMessageSink(IMessageSink innerSink, TraceSource tracer)
            {
                this.innerSink = innerSink;
                this.tracer = tracer;
            }

            public bool OnMessage(IMessageSinkMessage message)
            {
                var diagnostic = message as IDiagnosticMessage;
                if (diagnostic != null)
                    tracer.TraceEvent(TraceEventType.Verbose, 0, diagnostic.Message);

#if DEBUG
                try
                {
                    File.AppendAllText("xunit.vsix.log", message.GetType().FullName + Environment.NewLine);
                }
                catch (IOException) { }
#endif

                return innerSink.OnMessage(message);
            }
        }

        class VsixTestFrameworkExecutor : XunitTestFrameworkExecutor
        {
            public VsixTestFrameworkExecutor(AssemblyName assemblyName,
                                              ISourceInformationProvider sourceInformationProvider,
                                              IMessageSink diagnosticMessageSink)
                : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
            { }

            protected override async void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
            {
                SetupTracing(TestAssembly.Assembly);

                // Always run at least with one thread per VS version.
                if (executionOptions.MaxParallelThreadsOrDefault() < VsVersions.InstalledVersions.Length && !Debugger.IsAttached)
                {
                    executionOptions.SetValue("xunit.execution.MaxParallelThreads", VsVersions.InstalledVersions.Length);
                    Constants.Tracer.TraceEvent(TraceEventType.Verbose, 0, Strings.VsixTestFramework.SettingMaxThreads(VsVersions.InstalledVersions.Length));
                }
                // If debugger is attached, don't run multiple instances simultaneously since that makes debugging much harder.
                if (Debugger.IsAttached)
                {
                    executionOptions.SetValue("xunit.execution.MaxParallelThreads", 1);
                    Constants.Tracer.TraceEvent(TraceEventType.Verbose, 0, Strings.VsixTestFramework.DebugMaxThreads);
                }

                // This is the implementation of the base XunitTestFrameworkExecutor
                using (var assemblyRunner = new VsixTestAssemblyRunner(TestAssembly, testCases, DiagnosticMessageSink, new TracingMessageSink(executionMessageSink, Constants.Tracer), executionOptions))
                    await assemblyRunner.RunAsync();

                tracer.Flush();
            }
        }

        /***********************************************
		 * Showcases how to change [Fact] execution
		 * without even having to inherit the attribute
		 * *********************************************

				class VsxTestFrameworkDiscoverer : XunitTestFrameworkDiscoverer
				{
					public VsxTestFrameworkDiscoverer (IAssemblyInfo assemblyInfo,
														ISourceInformationProvider sourceProvider,
														IMessageSink diagnosticMessageSink,
														IXunitTestCollectionFactory collectionFactory = null)
						: base (assemblyInfo, sourceProvider, diagnosticMessageSink, collectionFactory)
					{ }

					protected override bool FindTestsForMethod (ITestMethod testMethod, bool includeSourceInformation, IMessageBus messageBus, ITestFrameworkDiscoveryOptions discoveryOptions)
					{
						if (testMethod.Method.GetCustomAttributes (typeof(TheoryAttribute)).Any ())
							return base.FindTestsForMethod (testMethod, includeSourceInformation, messageBus, discoveryOptions);
						else
							return base.FindTestsForMethod (new VsxTestMethod (testMethod), includeSourceInformation, messageBus, discoveryOptions);
					}

					[DebuggerDisplay(@"\{ class = {TestClass.Class.Name}, method = {Method.Name} \}")]
					class VsxTestMethod : LongLivedMarshalByRefObject, ITestMethod
					{
						ITestMethod testMethod;
						IMethodInfo methodInfo;

						public VsxTestMethod (ITestMethod testMethod)
						{
							this.testMethod = testMethod;
							this.methodInfo = new VsxMethodInfo (testMethod.Method);
						}

						public IMethodInfo Method { get { return methodInfo; } }

						public ITestClass TestClass { get { return testMethod.TestClass; } }

						public void Deserialize (IXunitSerializationInfo info)
						{
							this.testMethod.Deserialize (info);
						}

						public void Serialize (IXunitSerializationInfo info)
						{
							this.testMethod.Serialize (info);
						}

						class VsxMethodInfo : LongLivedMarshalByRefObject, IMethodInfo, IReflectionMethodInfo
						{
							IMethodInfo method;
							MethodInfo info;

							public VsxMethodInfo (IMethodInfo method)
							{
								this.method = method;
								var reflection = method as IReflectionMethodInfo;
								if (reflection != null)
									info = reflection.MethodInfo;
							}
							public IEnumerable<IAttributeInfo> GetCustomAttributes (string assemblyQualifiedAttributeTypeName)
							{
								if (assemblyQualifiedAttributeTypeName == typeof(FactAttribute).AssemblyQualifiedName) {
									var fact = method.GetCustomAttributes(assemblyQualifiedAttributeTypeName).FirstOrDefault();
									if (fact != null)
										return new IAttributeInfo[] { new VsxFactAttributeInfo (fact) };
									else
										return Enumerable.Empty<IAttributeInfo> ();
								}

								return method.GetCustomAttributes (assemblyQualifiedAttributeTypeName);
							}

							public bool IsAbstract { get { return method.IsAbstract; } }

							public bool IsGenericMethodDefinition { get { return method.IsGenericMethodDefinition; } }

							public bool IsPublic { get { return method.IsPublic; } }

							public bool IsStatic { get { return method.IsStatic; } }

							public string Name { get { return method.Name; } }

							public ITypeInfo ReturnType { get { return method.ReturnType; } }

							public ITypeInfo Type { get { return method.Type; } }

							public MethodInfo MethodInfo { get { return info; } }

							public IEnumerable<ITypeInfo> GetGenericArguments ()
							{
								return method.GetGenericArguments ();
							}

							public IEnumerable<IParameterInfo> GetParameters ()
							{
								return method.GetParameters ();
							}

							public IMethodInfo MakeGenericMethod (params ITypeInfo[] typeArguments)
							{
								return method.MakeGenericMethod (typeArguments);
							}

							class VsxFactAttributeInfo : LongLivedMarshalByRefObject, IAttributeInfo
							{
								private IAttributeInfo fact;

								public VsxFactAttributeInfo (IAttributeInfo fact)
								{
									this.fact = fact;
								}

								public IEnumerable<object> GetConstructorArguments ()
								{
									return fact.GetConstructorArguments ();
								}

								public IEnumerable<IAttributeInfo> GetCustomAttributes (string assemblyQualifiedAttributeTypeName)
								{
									if (assemblyQualifiedAttributeTypeName == typeof(XunitTestCaseDiscovererAttribute).AssemblyQualifiedName) {
										return new IAttributeInfo[] { new VsxDiscovererAttribute () };
									}

									return fact.GetCustomAttributes (assemblyQualifiedAttributeTypeName);
								}

								public TValue GetNamedArgument<TValue>(string argumentName)
								{
									return fact.GetNamedArgument<TValue> (argumentName);
								}

								class VsxDiscovererAttribute : LongLivedMarshalByRefObject, IAttributeInfo
								{
									public IEnumerable<object> GetConstructorArguments ()
									{
										return new[] {
											typeof(VsxFactDiscoverer).FullName,
											typeof(VsxFactDiscoverer).Assembly.GetName().Name,
										};
									}

									public IEnumerable<IAttributeInfo> GetCustomAttributes (string assemblyQualifiedAttributeTypeName)
									{
										yield break;
									}

									public TValue GetNamedArgument<TValue>(string argumentName)
									{
										return default(TValue);
									}
								}
							}
						}
					}
				}
		 */

    }
}
