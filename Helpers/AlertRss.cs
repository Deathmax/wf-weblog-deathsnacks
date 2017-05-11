using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using Newtonsoft.Json;

namespace Warframe_WebLog.Helpers
{
    public class AlertRss
    {
        private static readonly Dictionary<Platform, string> _rssUrlDictionary = new Dictionary<Platform, string>
        {
            {Platform.Pc, "http://content.warframe.com/dynamic/rss.php"},
            {Platform.PS4, "http://content.ps4.warframe.com/dynamic/rss.php"},
            {Platform.Xbox, "http://content.xb1.warframe.com/dynamic/rss.php"},
            {Platform.PcChina, "http://content.zhb.warframe.com/dynamic/rss.php"},
        };

        // do we actually care about other platforms?
        private static dynamic GetRssFeed(Platform platform)
        {
            using (var wc = new WebClient {Proxy = null})
            {
                var rawStr = wc.DownloadString(_rssUrlDictionary[platform]);
                var doc = new XmlDocument();
                doc.LoadXml(rawStr);
                var jsonObj = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeXmlNode(doc));
                return jsonObj; //rss.channel.item for array
            }
        }

        private static string ExtractRewards()
        {
            return null;
        }
    }
}
