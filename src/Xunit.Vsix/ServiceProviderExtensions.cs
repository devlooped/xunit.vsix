using System;

namespace Xunit
{
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
        internal static TService GetService<TRegistration, TService>(this IServiceProvider services)
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
