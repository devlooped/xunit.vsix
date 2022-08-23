using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using static ThisAssembly;

namespace Xunit;

/// <summary>
/// Exposes the global <see cref="IServiceProvider"/> retrieved from 
/// the current process DTE instance.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class GlobalServiceProvider
{
    static readonly System.IServiceProvider instance;
    static readonly Lazy<dynamic> components;

    static GlobalServiceProvider()
    {
        try
        {
            var dte = RunningObjects.GetDTE(TimeSpan.FromSeconds(5));
            if (dte == null)
            {
                Debug.Fail(Strings.GlobalServices.NoDte);
                instance = new NullServices();
            }
            else
            {
                instance = new OleServiceProvider(dte);
            }

            components = new Lazy<dynamic>(() =>
                instance.GetService<Interop.SComponentModel, object>().AsDynamicReflection());
        }
        catch (NotSupportedException)
        {
            Debug.Fail(Strings.GlobalServices.NoDte);
            instance = new NullServices();
        }
    }

    /// <summary>
    /// Accesses the underlying <see cref="IServiceProvider"/>.
    /// </summary>
    public static IServiceProvider Default => instance;

    /// <summary>
    /// Retrieves a service with the given type from the currently running Visual Studio.
    /// </summary>
    public static TService GetService<TService>() => instance.GetService<TService>();

    /// <summary>
    /// Retrieves a service with the given registration and casts it to the given service.
    /// </summary>
    public static TService GetService<TRegistration, TService>() => instance.GetService<TRegistration, TService>();

    /// <summary>
    /// Retrieves a service by its COM CLSID, when its type isn't available
    /// or it's more convenient to work with it as a dynamic object.
    /// </summary>
    public static dynamic GetService(Guid clsid) => instance.GetService(clsid);

    /// <summary>
    /// Retrieves an exported MEF component from the currently running Visual Studio.
    /// </summary>
    public static T GetExport<T>() => components == null ? null : components.Value?.GetService<T>();

    /// <summary>
    /// Retrieves exported MEF components from the currently running Visual Studio.
    /// </summary>
    public static IEnumerable<T> GetExports<T>() => components.Value?.GetExtensions<T>() ?? Array.Empty<T>();

    class NullServices : IServiceProvider
    {
        object IServiceProvider.GetService(Type serviceType) => null;
    }
}
