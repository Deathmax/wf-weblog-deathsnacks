using System;
using System.Net;

namespace Warframe_WebLog.Extensions
{
    public class WebClientTimeout : WebClient
    {
        public int Timeout = 5000;

        protected override WebRequest GetWebRequest(Uri uri)
        {
            var w = base.GetWebRequest(uri);
            w.Timeout = Timeout;
            return w;
        }
    }
}