using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBrad.WcfLib
{
    public class Util
    {
        public static bool ListEquals<T>(IEnumerable<T> c1, IEnumerable<T> c2) where T : IEquatable<T>
        {
            var e1 = c1.GetEnumerator();
            var e2 = c2.GetEnumerator();

            // while both collections have items, movenext and compare for equals
            bool t1 = e1.MoveNext();
            bool t2 = e2.MoveNext();

            while (t1 && t2)
            {
                if (!e1.Current.Equals(e2.Current))
                    return false;

                t1 = e1.MoveNext();
                t2 = e2.MoveNext();
            }

            // if either has items remaining, then they cannot be equal
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
