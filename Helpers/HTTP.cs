using System;
using System.IO;
using System.Net;
using log4net;

namespace Warframe_WebLog.Helpers
{
    public class Http
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Http));
        public static string RequestAPI(string url, string args = "", string platform = "PC")
        {
            return null;
        }

        public static string RequestStatsAPI(string url, string args = "", string platform = "PC")
        {
            return null;
        }

        public static string RequestGet(string url)
        {
            var request =
                WebRequest.Create(new Uri(url)) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = 10000;
            request.Method = "GET";
            request.UserAgent = "";
            Log.Info("Requesting get: " + url);
            try
            {
                try
                {
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is WebException)
                    {
                        var webEx = ex as WebException;
                        var exRes = (HttpWebResponse) webEx.Response;
                        switch (exRes.StatusCode)
                        {
                            case HttpStatusCode.BadRequest:
                                Log.Error("400 response");
                                if (exRes != null)
                                {
                                    var stream = exRes.GetResponseStream();
                                    if (stream != null)
                                        using (var reader = new StreamReader(stream))
                                        {
                                            var response = reader.ReadToEnd();
                                            return response;
                                        }
                                }
                                break;
                            default:
                                Log.Error(webEx.Message);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return null;
            }
            return null;
        }
    }
}