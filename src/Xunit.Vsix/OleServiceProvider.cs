﻿using System;
using System.Runtime.InteropServices;

namespace Xunit
{
    class OleServiceProvider : IServiceProvider
    {
        Interop.IServiceProvider serviceProvider;

        public OleServiceProvider(Interop.IServiceProvider serviceProvider)
            => this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        public OleServiceProvider(Interop.DTE dte)
            : this((Interop.IServiceProvider)dte) { }

        public object GetService(Type serviceType)
            => GetService((serviceType ?? throw new ArgumentNullException(nameof(serviceType))).GUID);

        object GetService(Guid guid)
        {
            if (guid == Guid.Empty)
                return null;

            if (guid == NativeMethods.IID_IServiceProvider)
                return serviceProvider;

            try
            {
                var riid = NativeMethods.IID_IUnknown;
                if (NativeMethods.Succeeded(serviceProvider.QueryService(ref guid, ref riid, out var zero)) && (IntPtr.Zero != zero))
                {
                    try
                    {
                        return Marshal.GetObjectForIUnknown(zero);
                    }
                    finally
                    {
                        Marshal.Release(zero);
                    }
                }
            }
            catch (Exception exception) when (
                exception is OutOfMemoryException ||
                exception is StackOverflowException ||
                exception is AccessViolationException ||
                exception is AppDomainUnloadedException ||
                exception is BadImageFormatException ||
                exception is DivideByZeroException)
            {
                throw;
            }

            return null;
        }
    }
}
