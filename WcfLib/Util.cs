using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBrad.WcfLib
{
    public class Util
    {
        public static bool Equals<T>(IEnumerable<T> c1, IEnumerable<T> c2) where T : IEquatable<T>
        {
            bool t1 = false, t2 = false;
            var e1 = c1.GetEnumerator();
            var e2 = c2.GetEnumerator();

            // while both collections have items, movenext and compare for equals
            while ( (t1 = e1.MoveNext()) && (t2 = e2.MoveNext()) )
                if (!e1.Current.Equals(e2.Current))
                    return false;

            // if either has items remaining, then they can be equal
            if (t1 || t2)
                return false;
            return true;
        }

        public static Uri GetWcfUri(Uri u)
        {
            if (!u.Scheme.Equals("tcp"))
                return u;

            UriBuilder b = new UriBuilder(u);
            b.Scheme = Uri.UriSchemeNetTcp;
            return b.Uri;
        }

        public static Uri GetWcfUri(UriBuilder b)
        {
            if (b.Scheme.Equals("tcp"))
                b.Scheme = Uri.UriSchemeNetTcp;
            return b.Uri;
        }
    }
}
