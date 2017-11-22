using System;
using System.Diagnostics;
using Xunit.Properties;

namespace Xunit
{
    /// <summary>
    /// Exposes the global <see cref="IServiceProvider"/> for use
    /// within integration tests.
    /// </summary>
    public static class GlobalServices
    {
        private static readonly TraceSource s_tracer = Constants.Tracer;
        private static IServiceProvider s_services;

        static GlobalServices()
        {
            try
            {
                var dte = RunningObjects.GetDTE(TimeSpan.FromSeconds(5));
                if (dte == null)
                {
                    Debug.Fail(Strings.GlobalServices.NoDte);
                    s_tracer.TraceEvent(TraceEventType.Warning, 0, Strings.GlobalServices.NoDte);
                    s_services = new NullServices();
                }
                else
                {
                    s_services = new Microsoft.VisualStudio.Shell.ServiceProvider(
                        (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
                    s_tracer.TraceInformation(Strings.GlobalServices.InitializedDte(dte.Version));
                }
            }
            catch (NotSupportedException ex)
            {
                Debug.Fail(Strings.GlobalServices.NoDte);
                s_tracer.TraceEvent(TraceEventType.Warning, 0, Strings.GlobalServices.NoDte + Environment.NewLine + ex.ToString());
                s_services = new NullServices();
            }
        }

        internal static void Initialize() { }

        /// <summary>
        /// Exposes the <see cref="IServiceProvider"/> instance itself, from
        /// where services are retrieved with the static usability overloads of
        /// <c>GetService</c>.
        /// </summary>
        public static IServiceProvider Instance { get { return s_services; } }

        /// <summary>
        /// Retrieves a service with the given type from the current global <see cref="Instance"/>.
        /// </summary>
        public static TService GetService<TService>()
        {
            return Instance.GetService<TService>();
        }

        /// <summary>
        /// Retrieves a service with the given registration and casts it
        /// to the given service.
        /// </summary>
        public static TService GetService<TRegistration, TService>()
        {
            return Instance.GetService<TRegistration, TService>();
        }

        /// <summary>
        /// Retrieves a service by its COM CLSID, when its type isn't available
        /// or it's more convenient to work with it as a dynamic object.
        /// </summary>
        public static dynamic GetService(Guid clsid)
        {
            return Instance.GetService(clsid);
        }

        private class NullServices : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Usability overloads for <see cref="IServiceProvider"/>.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Retrieves a service with the given type from the provider.
        /// </summary>
        public static TService GetService<TService>(this IServiceProvider services)
        {
            return (TService)services.GetService(typeof(TService));
        }

        /// <summary>
        /// Retrieves a service from the provider with the given registration and casts it
        /// to the given service.
        /// </summary>
        public static TService GetService<TRegistration, TService>(this IServiceProvider services)
        {
            return (TService)services.GetService(typeof(TRegistration));
        }

        /// <summary>
        /// Retrieves a service from the provider by its COM CLSID, when its type isn't available
        /// or it's more convenient to work with it as a dynamic object.
        /// </summary>
        public static dynamic GetService(this IServiceProvider services, Guid clsid)
        {
            return services.GetService(Type.GetTypeFromCLSID(clsid));
        }
    }
}
