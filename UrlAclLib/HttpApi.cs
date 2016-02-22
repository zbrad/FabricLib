using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace UrlAclLib
{
    public class HttpApi : IDisposable
    {
        public HttpApi()
        {
            var rc = Native.Initialize();
            if (rc != Result.OK)
                throw new ApplicationException("Initialize failed: rc: " + rc);
        }

        public bool SetAcl(string url, string acl)
        {
            try
            {
                NativeAcl u = new NativeAcl();
                u.Prefix = url;
                u.Acl = acl;
                var rc = Native.SetAcl(IntPtr.Zero, Config.UrlAclInfo, ref u, NativeAcl.Length, IntPtr.Zero);
                return (int) rc == (int) Result.OK;
            }
            catch
            {
                return false;
            }
        }

        public bool DelAcl(string url, string acl)
        {
            try
            {
                NativeAcl u = new NativeAcl();
                u.Prefix = url;
                u.Acl = acl;
                var rc = Native.DelAcl(IntPtr.Zero, Config.UrlAclInfo, ref u, NativeAcl.Length, IntPtr.Zero);
                return (int)rc == (int)Result.OK;
            }
            catch
            {
                return false;
            }
        }

        public UrlAcl GetAcl(string url)
        {
            IntPtr pOut = Marshal.AllocCoTaskMem(AclBufferSize);

            try
            {
                NativeQuery q = new NativeQuery();
                q.Prefix = url;
                q.QueryDesc = QueryType.Exact;

                return getAcl(q, pOut);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pOut);
            }
        }

        const int AclBufferSize = 1024;

        static UrlAcl getAcl(NativeQuery q, IntPtr buffer)
        {

            long out1;
            var rc = Native.QueryAcl(IntPtr.Zero, Config.UrlAclInfo, ref q, NativeQuery.Length, buffer, AclBufferSize, out out1, IntPtr.Zero);
            if (rc != Result.OK)
                return null;

            var acl = Marshal.PtrToStructure<NativeAcl>(buffer);
            return new UrlAcl() { Prefix = acl.Prefix, Acl = acl.Acl };
        }

        public List<UrlAcl> GetAcl()
        {
            IntPtr buffer = Marshal.AllocCoTaskMem(AclBufferSize);

            try
            {
                NativeQuery q = new NativeQuery();
                q.Prefix = string.Empty;
                q.QueryDesc = QueryType.Next;
                q.Token = 0;

                var list = new List<UrlAcl>();
                while (true)
                {
                    var acl = getAcl(q, buffer);
                    if (acl == null)
                        break;

                    list.Add(acl);
                    q.Token++;
                }

                return list;
            }
            finally
            {
                Marshal.FreeCoTaskMem(buffer);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Native.Terminate();
                disposedValue = true;
            }
        }

        ~HttpApi()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
