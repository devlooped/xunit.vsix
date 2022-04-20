using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Microsoft.VisualStudio.ComponentModelHost
{
    [Guid("FD57C398-FDE3-42c2-A358-660F269CBE43")]
    public interface SComponentModel
    {
    }
}

namespace Microsoft.VisualStudio.OLE.Interop
{
    [ComImport]
    [TypeIdentifier]
    [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IServiceProvider
    {
        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int QueryService([In][ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFGUID")] ref Guid guidService, [In][ComAliasName("Microsoft.VisualStudio.OLE.Interop.REFIID")] ref Guid riid, out IntPtr ppvObject);
    }
}

namespace Microsoft.VisualStudio.Shell.Interop
{
    //[TypeIdentifier]
    [Guid("FD9DC8E3-2FFC-446D-8C50-99CA4A3D2D1C")]
    public interface SVsShell
    {
    }

    [ComImport]
    [TypeIdentifier]
    [Guid("FD9DC8E3-2FFC-446D-8C50-99CA4A3D2D1C")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsShell
    {
        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        int GetProperty([In][ComAliasName("Microsoft.VisualStudio.Shell.Interop.VSSPROPID")] int propid, [MarshalAs(UnmanagedType.Struct)] out object pvar);
    }
}

namespace EnvDTE
{
    [TypeIdentifier]
    [ComImport]
    [TypeLibType(4160)]
    [Guid("04A72314-32E9-48E2-9B87-A63603454F3E")]
    public interface DTE
    {
        [DispId(100)]
        string Version
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(100)]
            [return: MarshalAs(UnmanagedType.BStr)]
            get;
        }
    }
}

internal static class NativeMethods
{
    public const int ERROR_INVALID_PARAMETER = 0x57;
    public const int INVALID_HANDLE_VALUE = -1;
    public const int MAX_PATH = 260;
    public const int PROCESS_QUERY_INFORMATION = 0x400;
    public const int TH32CS_SNAPPROCESS = 2;

    public static readonly Guid IID_IServiceProvider = typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider).GUID;
    public static readonly Guid IID_IObjectWithSite = new Guid("FC4801A3-2BA9-11CF-A229-00AA003D7352");
    public static Guid IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");

    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

    [DllImport("ole32.dll")]
    internal static extern int CoRegisterMessageFilter(IMessageFilter lpMessageFilter, out IMessageFilter lplpMessageFilter);
    [DllImport("ole32.dll")]
    internal static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);
    [DllImport("ole32.dll")]
    internal static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

    [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct INTERFACEINFO
    {
        [MarshalAs(UnmanagedType.IUnknown)]
        public object punk;
        public Guid iid;
        [ComAliasName("Microsoft.VisualStudio.OLE.Interop.WORD")]
        public ushort wMethod;
    }

    /// <summary>Enables handling of incoming and outgoing COM messages while waiting for responses from synchronous calls. You can use message filtering to prevent waiting on a synchronous call from blocking another application. For more information, see IMessageFilter.</summary>
    [ComImport, ComConversionLoss, InterfaceType((short)1), Guid("00000016-0000-0000-C000-000000000046")]
    public interface IMessageFilter
    {
        [return: ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint HandleInComingCall([In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwCallType, [In] IntPtr htaskCaller, [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwTickCount, [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.INTERFACEINFO"), MarshalAs(UnmanagedType.LPArray)] INTERFACEINFO[] lpInterfaceInfo);
        [return: ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint RetryRejectedCall([In] IntPtr htaskCallee, [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwTickCount, [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwRejectType);
        [return: ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")]
        [PreserveSig, MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        uint MessagePending([In] IntPtr htaskCallee, [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwTickCount, [In, ComAliasName("Microsoft.VisualStudio.OLE.Interop.DWORD")] uint dwPendingType);
    }

    internal static bool Succeeded(int hr)
    {
        return hr >= 0;
    }

    internal static bool Failed(int hr)
    {
        return hr < 0;
    }
}
