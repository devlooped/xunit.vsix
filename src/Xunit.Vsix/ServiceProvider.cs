using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.Shell
{
	[ComVisible (true)]
	internal sealed class ServiceProvider : System.IServiceProvider, IDisposable, IObjectWithSite
	{
		OLE.Interop.IServiceProvider serviceProvider;
		bool defaultServices;

		public ServiceProvider (OLE.Interop.IServiceProvider sp)
			: this (sp, true)
		{
		}

		public ServiceProvider (OLE.Interop.IServiceProvider sp, bool defaultServices)
		{
			if (sp == null)
				throw new ArgumentNullException ("sp");

			serviceProvider = sp;
			this.defaultServices = defaultServices;
		}

		public void Dispose ()
		{
			serviceProvider = null;
		}

		public object GetService (Type serviceType)
		{
			return GetService (serviceType, true);
		}

		public int QueryService (Type serviceType, out object service)
		{
			return QueryService (serviceType, true, out service);
		}

		public object GetService (Guid guid)
		{
			object result;
			QueryService (guid, out result);
			return result;
		}

		public int QueryService (Guid guid, out object service)
		{
			service = null;
			if (serviceProvider == null)
				return -2147418113;

			return QueryService (guid, null, out service);
		}


		object GetService (Type serviceType, bool setShellErrorInfo)
		{
			if (serviceType == null) {
				throw new ArgumentNullException ("serviceType");
			}
			object result;
			QueryService (serviceType, setShellErrorInfo, out result);
			return result;
		}

		int QueryService (Type serviceType, bool setShellErrorInfo, out object service)
		{
			service = null;
			if (serviceType == null)
				return -2147024809;

			if (serviceProvider == null)
				return -2147418113;

			return QueryService (serviceType.GUID, serviceType, setShellErrorInfo, out service);
		}

		int QueryService (Guid guid, Type serviceType, out object service)
		{
			return QueryService (guid, serviceType, true, out service);
		}

		int QueryService (Guid guid, Type serviceType, bool setShellErrorInfo, out object service)
		{
			service = null;
			int result;
			try {
				if (guid.Equals (Guid.Empty)) {
					result = -2147024809;
				} else {
					if (this.defaultServices) {
						if (guid.Equals (NativeMethods.IID_IServiceProvider)) {
							service = this.serviceProvider;
							result = 0;
							return result;
						}
						if (guid.Equals (NativeMethods.IID_IObjectWithSite)) {
							service = this;
							result = 0;
							return result;
						}
					}
					IntPtr zero = IntPtr.Zero;
					Guid iID_IUnknown = NativeMethods.IID_IUnknown;
					int num = this.serviceProvider.QueryService(ref guid, ref iID_IUnknown, out zero);
					if (NativeMethods.Succeeded (num) && IntPtr.Zero != zero) {
						try {
							service = Marshal.GetObjectForIUnknown (zero);
						} finally {
							Marshal.Release (zero);
						}
					}
					result = num;
				}
			} catch {
				throw;
			}
			return result;
		}

		void IObjectWithSite.GetSite (ref Guid riid, out IntPtr ppv)
		{
			object expr_0C = this.GetService(riid);
			if (expr_0C == null) {
				Marshal.ThrowExceptionForHR (-2147467262);
			}
			IntPtr expr_1E = Marshal.GetIUnknownForObject(expr_0C);
			int num = Marshal.QueryInterface(expr_1E, ref riid, out ppv);
			Marshal.Release (expr_1E);
			if (NativeMethods.Failed (num)) {
				Marshal.ThrowExceptionForHR (num);
			}
		}

		void IObjectWithSite.SetSite (object pUnkSite)
		{
			if (pUnkSite is OLE.Interop.IServiceProvider) {
				serviceProvider = (OLE.Interop.IServiceProvider)pUnkSite;
			}
		}
	}
}
