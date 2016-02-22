using System;

namespace UrlAclLib
{
    [Flags]
    internal enum InitializeFlags : uint
    {
        Server = 0x01,
        Config = 0x02
    }


    internal enum Config
    {
        IPListenList = 0,
        SSLCertInfo = 1,
        UrlAclInfo = 2,
        Max = 3
    }


    internal enum QueryType : int
    {
        Exact = 0,
        Next,
        Max
    }

    internal enum Result : int
    {
        OK = 0,
        InvalidParameter = 87,
        InsufficientBuffer = 122,
        MoreData = 234,
        NoMoreItems = 259
    }

}
