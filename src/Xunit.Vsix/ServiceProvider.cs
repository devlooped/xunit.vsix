using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Shell
{
    [ComVisible(true)]
    internal sealed class ServiceProvider : System.IServiceProvider, IDisposable, IObjectWithSite
    {
        private Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider;

        private bool defaultServices;

        //private static ServiceProvider globalProvider;

        //public static ServiceProvider GlobalProvider
        //{
        //    get
        //    {
        //        if (ServiceProvider.IsNullOrUnsited(ServiceProvider.globalProvider))
        //        {
        //            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider = OleServiceProvider.GlobalProvider;
        //            if (serviceProvider != null)
        //            {
        //                ServiceProvider.SetGlobalProvider(new ServiceProvider(serviceProvider));
        //            }
        //            else if (ServiceProvider.globalProvider == null)
        //            {
        //                ServiceProvider.SetGlobalProvider(new ServiceProvider());
        //            }
        //        }
        //        return ServiceProvider.globalProvider;
        //    }
        //}

        public ServiceProvider(Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp)
            : this(sp, true)
        {
        }

        public ServiceProvider(Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp, bool defaultServices)
        {
            if (sp == null)
            {
                throw new ArgumentNullException("sp");
            }
            this.serviceProvider = sp;
            this.defaultServices = defaultServices;
        }

        private ServiceProvider()
        {
        }

        public void Dispose()
        {
            if (this.serviceProvider != null)
            {
                this.serviceProvider = null;
            }
        }

        public object GetService(Type serviceType)
        {
            return this.GetService(serviceType, true);
        }

        internal object GetService(Type serviceType, bool setShellErrorInfo)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException("serviceType");
            }
            object result;
            this.QueryService(serviceType, setShellErrorInfo, out result);
            return result;
        }

        public int QueryService(Type serviceType, out object service)
        {
            return this.QueryService(serviceType, true, out service);
        }

        private int QueryService(Type serviceType, bool setShellErrorInfo, out object service)
        {
            service = null;
            if (serviceType == null)
            {
                return -2147024809;
            }
            if (this.serviceProvider == null)
            {
                return -2147418113;
            }
            return this.QueryService(serviceType.GUID, serviceType, setShellErrorInfo, out service);
        }

        public object GetService(Guid guid)
        {
            object result;
            this.QueryService(guid, out result);
            return result;
        }

        public int QueryService(Guid guid, out object service)
        {
            service = null;
            if (this.serviceProvider == null)
            {
                return -2147418113;
            }
            return this.QueryService(guid, null, out service);
        }

        private int QueryService(Guid guid, Type serviceType, out object service)
        {
            return this.QueryService(guid, serviceType, true, out service);
        }

        private int QueryService(Guid guid, Type serviceType, bool setShellErrorInfo, out object service)
        {
            service = null;
            int result;
            try
            {
                if (guid.Equals(Guid.Empty))
                {
                    result = -2147024809;
                }
                else
                {
                    if (this.defaultServices)
                    {
                        if (guid.Equals(NativeMethods.IID_IServiceProvider))
                        {
                            service = this.serviceProvider;
                            result = 0;
                            return result;
                        }
                        if (guid.Equals(NativeMethods.IID_IObjectWithSite))
                        {
                            service = this;
                            result = 0;
                            return result;
                        }
                    }
                    IntPtr zero = IntPtr.Zero;
                    Guid iID_IUnknown = NativeMethods.IID_IUnknown;
                    int num = this.serviceProvider.QueryService(ref guid, ref iID_IUnknown, out zero);
                    if (NativeMethods.Succeeded(num) && IntPtr.Zero != zero)
                    {
                        try
                        {
                            service = Marshal.GetObjectForIUnknown(zero);
                        }
                        finally
                        {
                            Marshal.Release(zero);
                        }
                    }
                    result = num;
                }
            }
            catch 
            {
                throw;
            }
            return result;
        }

        internal TInterfaceType GetService<TInterfaceType>(Type serviceType) where TInterfaceType : class
        {
            TInterfaceType tInterfaceType = this.GetService(serviceType) as TInterfaceType;
            if (tInterfaceType == null)
            {
                throw new InvalidOperationException("Missing service!: " + serviceType.FullName);
            }
            return tInterfaceType;
        }

        internal TInterfaceType TryGetService<TInterfaceType>(Type serviceType) where TInterfaceType : class
        {
            return this.GetService(serviceType) as TInterfaceType;
        }

        void IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppv)
        {
            object expr_0C = this.GetService(riid);
            if (expr_0C == null)
            {
                Marshal.ThrowExceptionForHR(-2147467262);
            }
            IntPtr expr_1E = Marshal.GetIUnknownForObject(expr_0C);
            int num = Marshal.QueryInterface(expr_1E, ref riid, out ppv);
            Marshal.Release(expr_1E);
            if (NativeMethods.Failed(num))
            {
                Marshal.ThrowExceptionForHR(num);
            }
        }

        void IObjectWithSite.SetSite(object pUnkSite)
        {
            if (pUnkSite is Microsoft.VisualStudio.OLE.Interop.IServiceProvider)
            {
                this.serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)pUnkSite;
            }
        }

        private static bool IsNullOrUnsited(ServiceProvider sp)
        {
            return sp == null || sp.serviceProvider == null;
        }

        //public static ServiceProvider CreateFromSetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp)
        //{
        //    ServiceProvider arg_2C_0 = new ServiceProvider(sp);
        //    if (ServiceProvider.IsNullOrUnsited(ServiceProvider.globalProvider))
        //    {
        //        ServiceProvider.SetGlobalProvider(new ServiceProvider(sp));
        //    }
        //    return arg_2C_0;
        //}
    }
}
