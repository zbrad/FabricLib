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
    struct QueryUrlAcl
    {
        public QueryType QueryDesc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Prefix;
        public int Token;

        public static int Length = Marshal.SizeOf(typeof(QueryUrlAcl));
    }

    internal static class HttpNative
    {
        [DllImport("httpapi.dll", SetLastError = true, PreserveSig = true)]
        internal static extern ulong HttpInitialize(
            ApiVersion version,
            InitializeFlags flags,
            IntPtr reserved
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling=true, PreserveSig=true)]
        internal static extern ulong HttpSetServiceConfiguration(
            IntPtr handle,
            Config configId,
            IntPtr info,
            int length,
            IntPtr overlapped
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true, EntryPoint="HttpSetServiceConfiguration")]
        internal static extern ulong SetAcl(
            IntPtr handle,
            Config configId,
            UrlAcl urlAcl,
            int length,
            IntPtr overlapped
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern ulong HttpTerminate(InitializeFlags flags, IntPtr mustBeZero);

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern ulong HttpCreateHttpHandle(out long queue, IntPtr mustBeZero);

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern ulong HttpQueryServiceConfiguration(
             IntPtr service,
             Config configId,
             IntPtr pInputConfigInfo,
             int InputConfigInfoLength,
             IntPtr pOutputConfigInfo,
             int OutputConfigInfoLength,
             [Optional()] out int pReturnLength,
             IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true, EntryPoint = "HttpQueryServiceConfiguration")]
        internal static extern ulong GetAcl(
            IntPtr service,
            Config configId,
            QueryUrlAcl query,
            int queryLength,
            ref UrlAcl acl,
            int aclLength,
            out long returnLength,
            IntPtr overlapped
            );
        internal static ulong Initialize()
        {
            ApiVersion v = new ApiVersion();
            v.HttpApiMajorVersion = 1;
            v.HttpApiMinorVersion = 0;

            return HttpInitialize(v, InitializeFlags.Config, IntPtr.Zero);
        }

        internal static ulong Terminate()
        {
            return HttpTerminate(InitializeFlags.Config, IntPtr.Zero);
        }
    }

    public static class HttpApi
    {
        public static void Initialize()
        {
            HttpNative.Initialize();
        }

        public static ulong SetAcl(string url, string acl)
        {
            UrlAcl u = new UrlAcl();
            u.Prefix = url;
            u.Acl = acl;
            ulong rc = HttpNative.SetAcl(IntPtr.Zero, Config.UrlAclInfo, u, UrlAcl.Length, IntPtr.Zero);
            return rc;
        }
        
        public static ulong GetAcl(string url, out string acl)
        {
            acl = null;
            QueryUrlAcl q = new QueryUrlAcl();
            q.Prefix = url;
            q.QueryDesc = QueryType.Exact;
            UrlAcl info = new UrlAcl();
            long returnLength;

            ulong rc = HttpNative.GetAcl(IntPtr.Zero, Config.UrlAclInfo, q, QueryUrlAcl.Length, ref info, UrlAcl.Length, out returnLength, IntPtr.Zero);
            if (rc == 0)
                acl = info.Acl;
            return rc;
        }

        public static RequestQueue GetRequestQueue()
        {
            long handle;
            ulong rc = HttpNative.HttpCreateHttpHandle(out handle, IntPtr.Zero);
            if (rc == 0)
                return new RequestQueue(handle);
            return null;
        }
    }
}
