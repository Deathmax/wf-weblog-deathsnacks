using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using Warframe_WebLog.Extensions;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.Status
{
    /// <summary>
    /// <para>Deprecated. Class for handling status checks.</para>
    /// <para>Most of this code will no longer work due to security checks preventing
    /// non-residential connections from accessing endpoints such as the API and website
    /// returning 401 Forbidden, or IRC hostnames no longer existing in favour of 
    /// retrieving IRC IPs on login.</para>
    /// </summary>
    public class Status
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(Status));

        public static void UpdateStatus()
        {
            new Thread(CheckWebsite).Start();
            new Thread(CheckForums).Start();
            new Thread(CheckXB1API).Start();
            new Thread(CheckPS4API).Start();
            new Thread(CheckAPI).Start();
            new Thread(CheckIRC).Start();
            new Thread(CheckPS4IRC).Start();
            new Thread(CheckXB1IRC).Start();
            new Thread(CheckOrigin).Start();
            new Thread(CheckOriginPS4).Start();
            new Thread(CheckOriginXB1).Start();
        }

        private static void CheckWebsite()
        {
            try
            {
                var wc = new WebClientTimeout {Proxy = null, Timeout = 10000};
                var str = wc.DownloadString("https://warframe.com");
                if (!str.Contains("div"))
                    throw new Exception("No div detected in str");
                FlatFile.WriteStatus("web", true);
            }
            catch (Exception ex)
            {
                FlatFile.WriteStatus("web", false);
                Log.ErrorFormat("Website check failed: {0}", ex.Message);
            }
        }

        private static void CheckForums()
        {
            try
            {
                var wc = new WebClientTimeout {Proxy = null, Timeout = 10000};
                var str = wc.DownloadString("https://forums.warframe.com");
                if (!str.Contains("div"))
                    throw new Exception("No div detected in str");
                FlatFile.WriteStatus("forums", true);
            }
            catch (Exception ex)
            {
                FlatFile.WriteStatus("forums", false);
                Log.ErrorFormat("Forums check failed: {0}", ex.Message);
            }
        }

        private static void CheckAPI()
        {
            var status = IsLoginUp("https://api.warframe.com/");
            if (!status)
            {
                FlatFile.WriteStatus("api", false);
                Log.Error("PC API check failed.");
            }
            else
            {
                FlatFile.WriteStatus("api", true);
            }
        }

        private static void CheckPS4API()
        {
            var status = IsLoginUp("https://api.ps4.warframe.com/");
            if (!status)
            {
                FlatFile.WriteStatus("apips4", false);
                Log.Error("PS4 API check failed.");
            }
            else
            {
                FlatFile.WriteStatus("apips4", true);
            }
        }

        private static void CheckXB1API()
        {
            var status = IsLoginUp("https://api.xb1.warframe.com/");
            if (!status)
            {
                FlatFile.WriteStatus("apixb1", false);
                Log.Error("XB1 API check failed.");
            }
            else
            {
                FlatFile.WriteStatus("apixb1", true);
            }
        }

        private static void CheckOrigin()
        {
            try
            {
                var wc = new WebClientTimeout { Proxy = null, Timeout = 10000 };
                var str = wc.DownloadString("http://origin.warframe.com/index.txt.lzma");
                FlatFile.WriteStatus("origin", true);
            }
            catch (Exception ex)
            {
                FlatFile.WriteStatus("origin", false);
                Log.ErrorFormat("Origin check failed: {0}", ex.Message);
            }
        }

        private static void CheckOriginPS4()
        {
            try
            {
                var wc = new WebClientTimeout { Proxy = null, Timeout = 10000 };
                var str = wc.DownloadString("http://origin.ps4.warframe.com/index.txt.lzma");
                FlatFile.WriteStatus("originps4", true);
            }
            catch (Exception ex)
            {
                FlatFile.WriteStatus("originps4", false);
                Log.ErrorFormat("Origin PS4 check failed: {0}", ex.Message);
            }
        }

        private static void CheckOriginXB1()
        {
            try
            {
                var wc = new WebClientTimeout { Proxy = null, Timeout = 10000 };
                var str = wc.DownloadString("http://origin.xb1.warframe.com/index.txt.lzma");
                FlatFile.WriteStatus("originxb1", true);
            }
            catch (Exception ex)
            {
                FlatFile.WriteStatus("originxb1", false);
                Log.ErrorFormat("Origin XB1 check failed: {0}", ex.Message);
            }
        }

        private static void CheckIRC()
        {
            try
            {
                using (var client = new TcpClient {ReceiveTimeout = 5000, SendTimeout = 5000})
                {
                    client.Connect("irc.warframe.com", 6696);
                }
                FlatFile.WriteStatus("irc", true);
            }
            catch (Exception ex)
            {
                FlatFile.WriteStatus("irc", false);
                Log.ErrorFormat("PC IRC server check failed. {0}", ex.Message);
            }
        }

        private static void CheckPS4IRC()
        {
            try
            {
                using (var client = new TcpClient {ReceiveTimeout = 5000, SendTimeout = 5000})
                {
                    client.Connect("irc.ps4.warframe.com", 6696);
                }
                FlatFile.WriteStatus("ircps4", true);
            }
            catch (Exception ex)
            {
                FlatFile.WriteStatus("ircps4", false);
                Log.ErrorFormat("PS4 IRC server check failed. {0}", ex.Message);
            }
        }

        private static void CheckXB1IRC()
        {
            try
            {
                using (var client = new TcpClient {ReceiveTimeout = 5000, SendTimeout = 5000})
                {
                    client.Connect("irc.xb1.warframe.com", 6696);
                }
                FlatFile.WriteStatus("ircxb1", true);
            }
            catch (Exception ex)
            {
                FlatFile.WriteStatus("ircxb1", false);
                Log.ErrorFormat("XB1 IRC server check failed. {0}", ex.Message);
            }
        }

        private static bool IsLoginUp(string host)
        {
            try
            {
                using (var wc = new WebClient {Proxy = null})
                {
                    wc.DownloadString(host);
                    return true; //should not happen, must be 404
                }
            }
            catch (WebException ex)
            {
                if (ex.Response == null)
                    return false;
                var exRes = (HttpWebResponse)ex.Response;
                switch (exRes.StatusCode)
                {
                    case HttpStatusCode.NotFound:
                    case HttpStatusCode.Forbidden:
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /*private static void CheckAPI_Old()
        {
            var status = IsLoginUp("https://api.warframe.com/api/login.php");
            if (!status)
            {
                FlatFile.WriteStatus("api", false);
                Log.Error("PC API check failed.");
            }
            else
            {
                FlatFile.WriteStatus("api", true);
            }
        }

        private static void CheckPS4API_Old()
        {
            var status = IsLoginUp("https://api.ps4.warframe.com/api/login.php");
            if (!status)
            {
                FlatFile.WriteStatus("apips4", false);
                Log.Error("PS4 API check failed.");
            }
            else
            {
                FlatFile.WriteStatus("apips4", true);
            }
        }

        private static void CheckXB1API_Old()
        {
            var status = IsLoginUp("https://api.xb1.warframe.com/api/login.php");
            if (!status)
            {
                FlatFile.WriteStatus("apixb1", false);
                Log.Error("XB1 API check failed.");
            }
            else
            {
                FlatFile.WriteStatus("apixb1", true);
            }
        }

        private static void CheckPcChinaAPI_Old()
        {
            var status = IsLoginUp("https://api.zhb.warframe.com/api/login.php");
            if (!status)
            {
                FlatFile.WriteStatus("apizhb", false);
                Log.Error("PcChina API check failed.");
            }
            else
            {
                FlatFile.WriteStatus("apizhb", true);
            }
        }

        private static bool IsLoginUp_Old(string host)
        {
            const string bodyJson = @"{""email"": ""statuscheck"",""password"": ""B97DE512E91E3828B40D2B0FDCE9CEB3C4A71F9BEA8D88E75C4FA854DF36725FD2B52EB6544EDCACD6F8BEDDFEA403CB55AE31F03AD62A5EF54E42EE82C3FB35"",""time"": 0}";
            var request =
                WebRequest.Create(new Uri(host)) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = 10000;
            request.Method = "POST";
            request.UserAgent = "";
            byte[] byteData = Encoding.ASCII.GetBytes(bodyJson);
            request.ContentLength = byteData.Length;
            try
            {
                using (Stream postStream = request.GetRequestStream())
                {
                    postStream.Write(byteData, 0, byteData.Length);
                }
                try
                {
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            var text = reader.ReadToEnd();
                            Log.Debug(text);
                            return text.Contains("Login failed; unknown user");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is WebException)
                    {
                        var webEx = ex as WebException;
                        var exRes = (HttpWebResponse)webEx.Response;
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
                                            Log.Debug(response);
                                            return response.Contains("Login failed; unknown user");
                                        }
                                }
                                break;
                            default:
                                Log.Error(ex);
                                return false;
                        }
                    }
                    Log.Error(ex);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return false;
            }
            return false;
        }*/
    }
}