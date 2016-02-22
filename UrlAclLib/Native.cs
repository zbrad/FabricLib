using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace UrlAclLib
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct ApiVersion
    {
        public ushort HttpApiMajorVersion;
        public ushort HttpApiMinorVersion;

        public ApiVersion(ushort majorVersion, ushort minorVersion)
        {
            HttpApiMajorVersion = majorVersion;
            HttpApiMinorVersion = minorVersion;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 0)]
    internal struct NativeQuery
    {
        public QueryType QueryDesc;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Prefix;
        public int Token;

        public static int Length = Marshal.SizeOf(typeof(NativeQuery));
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 0)]
    internal struct NativeAcl
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Prefix;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string Acl;

        public static int Length = Marshal.SizeOf(typeof(NativeAcl));
    }

    internal static class Native
    {
        [DllImport("httpapi.dll", SetLastError = true, PreserveSig = true)]
        internal static extern Result HttpInitialize(
            ApiVersion version,
            InitializeFlags flags,
            IntPtr reserved
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern Result HttpSetServiceConfiguration(
            IntPtr handle,
            Config configId,
            IntPtr info,
            int length,
            IntPtr overlapped
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true, EntryPoint = "HttpSetServiceConfiguration")]
        internal static extern Result SetAcl(
            IntPtr handle,
            Config configId,
            ref NativeAcl urlAcl,
            int length,
            IntPtr overlapped
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true, EntryPoint = "HttpDeleteServiceConfiguration")]
        internal static extern Result DelAcl(
            IntPtr handle,
            Config configId,
            ref NativeAcl urlAcl,
            int length,
            IntPtr overlapped
            );

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern Result HttpTerminate(InitializeFlags flags, IntPtr mustBeZero);

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern Result HttpCreateHttpHandle(out long queue, IntPtr mustBeZero);

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true)]
        internal static extern Result HttpQueryServiceConfiguration(
             IntPtr service,
             Config configId,
             IntPtr pInputConfigInfo,
             int InputConfigInfoLength,
             IntPtr pOutputConfigInfo,
             int OutputConfigInfoLength,
             [Optional()] out int pReturnLength,
             IntPtr pOverlapped);

        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true, EntryPoint = "HttpQueryServiceConfiguration")]
        internal static extern Result GetAcl(
            IntPtr service,
            Config configId,
            NativeQuery query,
            int queryLength,
            ref NativeAcl acl,
            int aclLength,
            out long returnLength,
            IntPtr overlapped
            );


        [DllImport("httpapi.dll", SetLastError = true, ExactSpelling = true, PreserveSig = true, EntryPoint = "HttpQueryServiceConfiguration")]
        internal static extern Result QueryAcl(
            IntPtr service,
            Config configId,
            ref NativeQuery query,
            int queryLength,
            IntPtr acl,
            int aclLength,
            out long returnLength,
            IntPtr overlapped
            );


        internal static Result Initialize()
        {
            ApiVersion v = new ApiVersion(1, 0);
            return HttpInitialize(v, InitializeFlags.Config, IntPtr.Zero);
        }

        internal static Result Terminate()
        {
            return HttpTerminate(InitializeFlags.Config, IntPtr.Zero);
        }
    }

}
