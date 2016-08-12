using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace ZBrad.FabricLib.Utilities
{
    [Flags]
    enum InitializeFlags : uint
    {
        Server = 0x01,
        Config = 0x02
    }


    enum Config
    {
        IPListenList = 0,
        SSLCertInfo = 1,
        UrlAclInfo = 2,
        Max = 3
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    struct ApiVersion
    {
        public ushort HttpApiMajorVersion;
        public ushort HttpApiMinorVersion;

        public ApiVersion(ushort majorVersion, ushort minorVersion)
        {
            HttpApiMajorVersion = majorVersion;
            HttpApiMinorVersion = minorVersion;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    class UrlAcl
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Prefix;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Acl;

        public static int Length = Marshal.SizeOf(typeof(UrlAcl));
    }

    enum QueryType
    {
        Exact = 0,
        Next,
        Max
    }

    [StructLayout(LayoutKind.Sequential)]
    public class RequestQueue
    {
        public long Handle;

        public RequestQueue(long handle)
        {
            this.Handle = handle;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    class QueryUrlAcl
    {
        public QueryType QueryDesc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Prefix;
        public int Token;

        public static int Length = Marshal.SizeOf(typeof(QueryUrlAcl));
    }


    static class UnsafeNativeMethods
    {
        [DllImport("httpapi.dll", SetLastError = true, PreserveSig = true)]
        internal static extern int HttpInitialize(
            ApiVersion version,
            InitializeFlags flags,
            IntPtr reserved = default(IntPtr)
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern int HttpSetServiceConfiguration(
            IntPtr handle,
            Config configId,
            IntPtr info,
            int length,
            IntPtr overlapped = default(IntPtr)
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true, EntryPoint = "HttpSetServiceConfiguration")]
        internal static extern int SetAcl(
            IntPtr handle,
            Config configId,
            UrlAcl urlAcl,
            int length,
            IntPtr overlapped = default(IntPtr)
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern int HttpTerminate(InitializeFlags flags, IntPtr reserved = default(IntPtr));


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1901:PInvokeDeclarationsShouldBePortable", MessageId = "1", 
            Justification = "Signature has been validated against MSDN API ref: https://msdn.microsoft.com/en-us/library/windows/desktop/aa364482(v=vs.85).aspx")]
        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern int HttpCreateHttpHandle(out long queue, ulong reserved = 0);

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern int HttpQueryServiceConfiguration(
             IntPtr service,
             Config configId,
             IntPtr pInputConfigInfo,
             int InputConfigInfoLength,
             IntPtr pOutputConfigInfo,
             int OutputConfigInfoLength,
             [Optional()] out int pReturnLength,
             IntPtr overlapped = default(IntPtr));

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true, EntryPoint = "HttpQueryServiceConfiguration")]
        internal static extern int GetAcl(
            IntPtr service,
            Config configId,
            QueryUrlAcl query,
            int queryLength,
            ref UrlAcl acl,
            int aclLength,
            out long returnLength,
            IntPtr overlapped = default(IntPtr)
            );
    }

    public static class HttpApi
    {
        public static int Initialize()
        {
            ApiVersion v = new ApiVersion();
            v.HttpApiMajorVersion = 1;
            v.HttpApiMinorVersion = 0;

            return UnsafeNativeMethods.HttpInitialize(v, InitializeFlags.Config);
        }

        public static int Terminate()
        {
            return UnsafeNativeMethods.HttpTerminate(InitializeFlags.Config);
        }

        public static int SetAcl(string url, string acl)
        {
            UrlAcl u = new UrlAcl();
            u.Prefix = url;
            u.Acl = acl;
            var rc = UnsafeNativeMethods.SetAcl(IntPtr.Zero, Config.UrlAclInfo, u, UrlAcl.Length);
            return rc;
        }
        
        public static int GetAcl(string url, out string acl)
        {
            acl = null;
            QueryUrlAcl q = new QueryUrlAcl();
            q.Prefix = url;
            q.QueryDesc = QueryType.Exact;
            UrlAcl info = new UrlAcl();
            long returnLength;

            var rc = UnsafeNativeMethods.GetAcl(IntPtr.Zero, Config.UrlAclInfo, q, QueryUrlAcl.Length, ref info, UrlAcl.Length, out returnLength);
            if (rc == 0)
                acl = info.Acl;
            return rc;
        }

        public static RequestQueue GetRequestQueue()
        {
            long handle;
            var rc = UnsafeNativeMethods.HttpCreateHttpHandle(out handle);
            if (rc == 0)
                return new RequestQueue(handle);
            return null;
        }
    }
}
