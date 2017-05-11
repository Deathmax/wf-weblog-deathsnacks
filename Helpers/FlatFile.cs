using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Newtonsoft.Json;
using Warframe_WebLog.Classes.WorldState;

namespace Warframe_WebLog.Helpers
{
    public class FlatFile
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(FlatFile));
        public static Dictionary<Platform, Dictionary<string, Alert>> AlertDictionary;
        public static Dictionary<Platform, Dictionary<string, dynamic>> GoalsDictionary;
        public static Dictionary<Platform, Dictionary<string, Invasion>> InvasionDictionary;
        public static Dictionary<Platform, Dictionary<string, BadlandNode>> BadlandDictionary;
        public static Dictionary<Platform, Dictionary<string, DailyDeal>> DailyDealDictionary;
        public static Dictionary<string, string> PlanetRegionNames;
        public static Dictionary<string, string> ItemNames;
        public static Dictionary<string, string> NewItemNames; 
        public static Dictionary<string, string> LanguageStrings;

        public static void Initialize()
        {
            AlertDictionary = new Dictionary<Platform, Dictionary<string, Alert>>();
            GoalsDictionary = new Dictionary<Platform, Dictionary<string, dynamic>>();
            InvasionDictionary = new Dictionary<Platform, Dictionary<string, Invasion>>();
            BadlandDictionary = new Dictionary<Platform, Dictionary<string, BadlandNode>>();
            DailyDealDictionary = new Dictionary<Platform, Dictionary<string, DailyDeal>>();
            foreach (Platform platform in Enum.GetValues(typeof (Platform)))
            {
                AlertDictionary[platform] = new Dictionary<string, Alert>();
                if (!File.Exists($"events{platform}.json"))
                {
                    Log.ErrorFormat("Did not find events{0}.json, creating.", platform);
                    File.WriteAllText($"events{platform}.json",
                        JsonConvert.SerializeObject(new List<Alert>()));
                }
                else
                {
                    var alerts =
                        JsonConvert.DeserializeObject<List<Alert>>(
                            File.ReadAllText($"events{platform}.json"));
                    foreach (var alert in alerts)
                        AlertDictionary[platform].Add(alert._id.id, alert);
                    Log.InfoFormat("Loaded " + AlertDictionary[platform].Count + " alerts from file for {0}.", platform);
                }
                GoalsDictionary[platform] = new Dictionary<string, dynamic>();
                if (!File.Exists($"goals{platform}.json"))
                {
                    Log.ErrorFormat("Did not find goals{0}.json, creating.", platform);
                    File.WriteAllText($"goals{platform}.json",
                        JsonConvert.SerializeObject(new List<dynamic>()));
                }
                else
                {
                    var goals =
                        JsonConvert.DeserializeObject<List<dynamic>>(
                            File.ReadAllText($"goals{platform}.json"));
                    foreach (var goal in goals)
                        GoalsDictionary[platform].Add(goal._id.id.ToString(), goal);
                    Log.InfoFormat("Loaded " + GoalsDictionary[platform].Count + " goals from file for {0}.", platform);
                }
                InvasionDictionary[platform] = new Dictionary<string, Invasion>();
                if (!File.Exists($"invasions{platform}.json"))
                {
                    Log.ErrorFormat("Did not find invasions.json, creating.");
                    File.WriteAllText($"invasions{platform}.json",
                        JsonConvert.SerializeObject(new List<Invasion>()));
                }
                else
                {
                    var alerts =
                        JsonConvert.DeserializeObject<List<Invasion>>(
                            File.ReadAllText($"invasions{platform}.json"));
                    foreach (var alert in alerts)
                        InvasionDictionary[platform].Add(alert._id.id, alert);
                    Log.InfoFormat("Loaded " + InvasionDictionary[platform].Count + " invasions from file for {0}.", platform);
                }
                BadlandDictionary[platform] = new Dictionary<string, BadlandNode>();
                if (!File.Exists($"badlands{platform}.json"))
                {
                    Log.ErrorFormat("Did not find badlands{0}.json, creating.", platform);
                    File.WriteAllText($"badlands{platform}.json",
                        JsonConvert.SerializeObject(new List<BadlandNode>()));
                }
                else
                {
                    var badlands =
                        JsonConvert.DeserializeObject<List<BadlandNode>>(
                            File.ReadAllText($"badlands{platform}.json"));
                    foreach (var node in badlands)
                        BadlandDictionary[platform].Add(node._id.id, node);
                    Log.InfoFormat("Loaded " + BadlandDictionary[platform].Count + " badlands from file for {0}.", platform);
                }
                DailyDealDictionary[platform] = new Dictionary<string, DailyDeal>();
                if (!File.Exists($"dailydeal{platform}.json"))
                {
                    Log.ErrorFormat("Did not find dailydeal{0}.json, creating.", platform);
                    File.WriteAllText($"dailydeal{platform}.json",
                        JsonConvert.SerializeObject(new List<DailyDeal>()));
                }
                else
                {
                    var dailyDeals =
                        JsonConvert.DeserializeObject<List<DailyDeal>>(
                            File.ReadAllText($"dailydeal{platform}.json"));
                    foreach (var deal in dailyDeals)
                    {
                        DailyDealDictionary[platform].Add(
                            deal._id != null ? deal._id.id : deal.Activation.sec.ToString(), deal);
                    }
                    Log.InfoFormat("Loaded " + DailyDealDictionary[platform].Count + " daily deals from file for {0}.",
                        platform);
                }
            }
            PlanetRegionNames = new Dictionary<string, string>();
            ItemNames = new Dictionary<string, string>();
            NewItemNames = new Dictionary<string, string>();
            LanguageStrings = new Dictionary<string, string>();

            if (!File.Exists("planetnamesregion.json"))
            {
                Log.ErrorFormat("Did not find planetnamesregion.json, creating");
                File.WriteAllText("planetnamesregion.json", JsonConvert.SerializeObject(PlanetRegionNames));
            }
            else
            {
                PlanetRegionNames =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("planetnamesregion.json"));
                Log.InfoFormat("Loaded " + PlanetRegionNames.Count + " planet names with region from file.");
            }

            if (!File.Exists("names.json"))
            {
                Log.ErrorFormat("Did not find names.json, creating");
                File.WriteAllText("names.json", JsonConvert.SerializeObject(ItemNames));
            }
            else
            {
                ItemNames =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("names.json"));
                Log.InfoFormat("Loaded " + ItemNames.Count + " item names from file.");
            }

            if (!File.Exists("names_new.json"))
            {
                Log.ErrorFormat("Did not find names_new.json, creating");
                File.WriteAllText("names_new.json", JsonConvert.SerializeObject(ItemNames));
            }
            else
            {
                NewItemNames =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("names_new.json"));
                Log.InfoFormat("Loaded " + NewItemNames.Count + " new item names from file.");
            }

            if (!File.Exists("strings.json"))
            {
                Log.ErrorFormat("Did not find strings.json, creating");
                File.WriteAllText("strings.json", JsonConvert.SerializeObject(LanguageStrings));
            }
            else
            {
                LanguageStrings =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("strings.json"));
                Log.InfoFormat("Loaded " + LanguageStrings.Count + " language strings from file.");
            }

            if (!Directory.Exists("bl_logs/history"))
            {
                Directory.CreateDirectory("bl_logs/history");
            }
            if (!Directory.Exists("old_data"))
            {
                Directory.CreateDirectory("old_data");
            }
        }

        #region Status Stuff

        private static object _statusWriteLock = new object();
        public static void WriteStatus(string service, bool up)
        {
            try
            {
                var prev = true;
                if (!Directory.Exists("status_logs"))
                {
                    Directory.CreateDirectory("status_logs");
                }
                if (File.Exists("status_logs/" + service + ".txt"))
                {
                    var lines = File.ReadAllLines("status_logs/" + service + ".txt");
                    var last = lines.Last();
                    if (string.IsNullOrEmpty(last))
                        last = lines[lines.Length - 2];
                    var laststatus = last.Split('|')[1];
                    if (bool.Parse(laststatus) == up)
                    {
                        return;
                    }
                    prev = bool.Parse(laststatus);
                }
                Log.InfoFormat("{0} is now {1}. Format(Previously {2})", service, up ? "online" : "down", prev ? "online" : "down");
                File.AppendAllText("status_logs/" + service + ".txt",
                    $"{Utils.DateTimeToUnixTimestamp(DateTime.UtcNow)}|{up}\n");
                File.WriteAllText("../data/status_" + service + ".txt",
                    $"{(up ? "up" : "down")}|{Utils.DateTimeToUnixTimestamp(DateTime.UtcNow)}");
                lock (_statusWriteLock)
                {
                    var statusJson = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, dynamic>>>(File.ReadAllText("../data/status.json"));
                    statusJson[service] = new Dictionary<string, dynamic> {{"Up", up}, {"Time", DateTime.UtcNow}};
                    File.WriteAllText("../data/status.json", JsonConvert.SerializeObject(statusJson));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        #endregion

        #region Localization stuff

        public static string GetPlanetName(string node)
        {
            var initial = MobileExport.GetPlanetName(node);
            if (initial != null) return initial;
            return PlanetRegionNames.ContainsKey(node) ? PlanetRegionNames[node].Split('|')[1] : node;
        }

        public static string GetPlanetNameWithRegion(string node)
        {
            var initial = MobileExport.GetPlanetNameWithRegion(node);
            if (initial != null) return initial;
            if (!PlanetRegionNames.ContainsKey(node)) return node;
            var data = PlanetRegionNames[node].Split('|');
            return $"{data[1]} ({data[0]})";
        }

        public static string GetRegion(string node)
        {
            var initial = MobileExport.GetRegion(node);
            if (initial != null) return initial;
            if (!PlanetRegionNames.ContainsKey(node)) return "-";
            var data = PlanetRegionNames[node].Split('|');
            return data[0];
        }

        public static string GetNodeMission(string node)
        {
            var initial = MobileExport.GetNodeMission(node);
            if (initial != null) return initial;
            if (PlanetRegionNames.ContainsKey(node))
            {
                var data = PlanetRegionNames[node].Split('|');
                return Names.GetMissionType(data[2]);
            }
            return "-";
        }

        public static string GetName(string raw)
        {
            if (ItemNames.ContainsKey(raw))
            {
                var str = ItemNames[raw];
                if (str.Contains("Placeholder") || str == raw.Split('/').Last().Replace("StoreItem", ""))
                {
                    var fetch = GetMobileFetch(raw);
                    if (fetch != null)
                        return fetch;
                }
                return str;
            }
            var noMatchFetch = GetMobileFetch(raw);
            if (noMatchFetch != null)
            {
                return noMatchFetch;
            }
            return GetUnknownName(raw);
        }

        public static string GetNewName(string raw)
        {
            var initial = MobileExport.GetNewItemName(raw);
            if (initial != null) return initial;
            if (ItemNames.ContainsKey(raw))
            {
                var str = ItemNames[raw];
                if (str.Contains("Placeholder") || str == raw.Split('/').Last().Replace("StoreItem", ""))
                {
                    var fetch = GetMobileFetch(raw);
                    if (fetch != null)
                        return fetch;
                }
                return str;
            }
            var noMatchFetch = GetMobileFetch(raw);
            if (noMatchFetch != null)
            {
                return noMatchFetch;
            }
            return GetUnknownName(raw);
        }

        private static string GetMobileFetch(string raw)
        {
            if (NewItemNames.ContainsKey(raw))
            {
                return NewItemNames[raw];
            }
            var mobileFetch = MobileExport.GetItemName(raw);
            if (mobileFetch == null) return null;
            NewItemNames[raw] = mobileFetch;
            SerializeLanguageStrings();
            return mobileFetch;
        }

        public static string GetUnknownName(string raw, bool serialize = true)
        {
            var newtext = raw.Split('/').Last().Replace("StoreItem", "");
            if (serialize)
            {
                ItemNames[raw] = newtext;
                SerializeLanguageStrings();
            }
            return newtext;
        }

        public static string GetString(string raw)
        {
            if (raw == null)
                return "";
            if (LanguageStrings.ContainsKey(raw))
                return LanguageStrings[raw];
            if (raw.Contains("/"))
                return raw.Split('/').Last();
            return raw;
        }

        #endregion

        #region Serialization

        public static void SerializeAlerts(Platform platform)
        {
            AlertDictionary[platform] = AlertDictionary[platform].OrderByDescending(pair => pair.Value.Activation.sec)
                .ToDictionary(x => x.Key, x => x.Value);
            while (AlertDictionary[platform].Count >= 1000)
            {
                Log.InfoFormat("{0} alerts dictionary has exceeded 1000 Format({1}), removing 500.", platform,
                    AlertDictionary[platform].Count);
                var tempDict = AlertDictionary[platform].OrderBy(pair => pair.Value.Activation.sec).Take(500)
                    .ToDictionary(x => x.Key, x => x.Value);
                var i = 1;
                while (File.Exists($"old_data/alerts{platform}_{i}.json"))
                {
                    i++;
                }
                Log.InfoFormat("Saving old data into old_data/alerts{0}_{1}.json", platform, i);
                File.WriteAllText($"old_data/alerts{platform}_{i}.json",
                    JsonConvert.SerializeObject(tempDict.Values.ToList(), Formatting.Indented));
                foreach (var key in AlertDictionary[platform].Keys.ToArray().Where(tempDict.ContainsKey))
                {
                    AlertDictionary[platform].Remove(key);
                }
            }
            File.WriteAllText($"events{platform}.json",
                JsonConvert.SerializeObject(AlertDictionary[platform].Values.ToList()));
        }

        public static void SerializeGoals(Platform platform)
        {
            GoalsDictionary[platform] = GoalsDictionary[platform].OrderByDescending(pair => pair.Value.Activation.sec)
                .ToDictionary(x => x.Key, x => x.Value);
            while (GoalsDictionary[platform].Count >= 1000)
            {
                Log.InfoFormat("{0} goals dictionary has exceeded 1000 Format({1}), removing 500.", platform,
                    GoalsDictionary[platform].Count);
                var tempDict = GoalsDictionary[platform].OrderBy(pair => pair.Value.Activation.sec).Take(500)
                    .ToDictionary(x => x.Key, x => x.Value);
                var i = 1;
                while (File.Exists($"old_data/goals{platform}_{i}.json"))
                {
                    i++;
                }
                Log.InfoFormat("Saving old data into old_data/goals{0}_{1}.json", platform, i);
                File.WriteAllText($"old_data/goals{platform}_{i}.json",
                    JsonConvert.SerializeObject(tempDict.Values.ToList(), Formatting.Indented));
                foreach (var key in GoalsDictionary[platform].Keys.ToArray().Where(tempDict.ContainsKey))
                {
                    GoalsDictionary[platform].Remove(key);
                }
            }
            File.WriteAllText($"goals{platform}.json",
                JsonConvert.SerializeObject(GoalsDictionary[platform].Values.ToList()));
        }

        public static void SerializeInvasion(Platform platform)
        {
            InvasionDictionary[platform] =
                InvasionDictionary[platform].OrderByDescending(pair => pair.Value.Activation.sec)
                    .ToDictionary(x => x.Key, x => x.Value);
            while (InvasionDictionary[platform].Count >= 1000)
            {
                Log.InfoFormat("{0} invasions dictionary has exceeded 1000 Format({1}), removing 500.", platform,
                    InvasionDictionary[platform].Count);
                var tempDict = InvasionDictionary[platform].OrderBy(pair => pair.Value.Activation.sec).Take(500)
                    .ToDictionary(x => x.Key, x => x.Value);
                var i = 1;
                while (File.Exists($"old_data/invasions{platform}_{i}.json"))
                {
                    i++;
                }
                Log.InfoFormat("Saving old data into old_data/invasions{0}_{1}.json", platform, i);
                File.WriteAllText($"old_data/invasions{platform}_{i}.json",
                    JsonConvert.SerializeObject(tempDict.Values.ToList(), Formatting.Indented));
                foreach (var key in InvasionDictionary[platform].Keys.ToArray().Where(tempDict.ContainsKey))
                {
                    InvasionDictionary[platform].Remove(key);
                }
            }
            File.WriteAllText($"invasions{platform}.json",
                JsonConvert.SerializeObject(InvasionDictionary[platform].Values.ToList()));
        }

        public static void SerializeLatest15AndNow(Platform platform, List<Alert> now)
        {
            var alertObjects =
                AlertDictionary[platform].Values.Union(now).Distinct().OrderByDescending(pair => pair.Expiry.sec);
            File.WriteAllText($"last15events{platform}.json",
                JsonConvert.SerializeObject(alertObjects.ToList().GetRange(0, Math.Min(15, alertObjects.Count()))));
            SerializeLatest15AndNowLocalized(platform, now);
        }

        public static void SerializeLatest15AndNowLocalized(Platform platform, List<Alert> now)
        {
            var alertObjects =
                AlertDictionary[platform].Values.Union(now).Distinct().OrderByDescending(pair => pair.Expiry.sec);
            var objectsToSerialize = alertObjects.ToList().GetRange(0, Math.Min(15, alertObjects.Count()));
            var copy = JsonConvert.DeserializeObject<List<Alert>>(JsonConvert.SerializeObject(objectsToSerialize));
            for (var index = 0; index < copy.Count; index++)
            {
                var alert = copy[index];
                alert.id = alert._id.id;
                alert.MissionInfo.descText = GetString(alert.MissionInfo.descText);
                alert.MissionInfo.faction = Names.GetFaction(alert.MissionInfo.faction);
                alert.MissionInfo.location = GetPlanetNameWithRegion(alert.MissionInfo.location);
                var reward = alert.MissionInfo.missionReward;
                if (reward.countedItems != null && reward.countedItems.Count > 0)
                {
                    for (var i = 0; i < reward.countedItems.Count; i++)
                    {
                        var item = reward.countedItems[i];
                        item.ItemType = GetName(item.ItemType);
                    }
                }
                if (reward.items != null && reward.items.Count > 0)
                {
                    for (var i = 0; i < reward.items.Count; i++)
                    {
                        var item = reward.items[i];
                        reward.items[i] = GetName(item);
                    }
                }
                if (reward.items == null)
                {
                    reward.items = new List<string>();
                }
                if (reward.countedItems == null)
                {
                    reward.countedItems = new List<CountedItem>();
                }
                alert.MissionInfo.missionType =
                    ((alert.MissionInfo.nightmare ||
                      alert.MissionInfo.descText == "/Lotus/Language/Alerts/NightmareAlertDesc")
                        ? "Nightmare "
                        : "")
                    + Names.GetMissionType(alert.MissionInfo.missionType);
                if (alert.MissionInfo.archwingRequired != null && alert.MissionInfo.archwingRequired)
                {
                    alert.MissionInfo.descText = alert.MissionInfo.descText + " (Archwing)";
                }
            }
            File.WriteAllText($"last15events{platform}_localized.json",
                JsonConvert.SerializeObject(copy));
        }

        public static void SerializeCurrentAlerts(Platform platform, List<Alert> alerts)
        {
            SerializeLatest15AndNow(platform, alerts);
            File.WriteAllText($"currentevents{platform}.json", JsonConvert.SerializeObject(alerts));
        }

        public static void SerializeCurrentInvasions(Platform platform, List<Invasion> invasions)
        {
            File.WriteAllText($"currentinvasions{platform}.json",
                JsonConvert.SerializeObject(invasions));
        }

        public static void SerializeLanguageStrings()
        {
            lock (ItemNames)
            {
                File.WriteAllText("names.json", JsonConvert.SerializeObject(ItemNames));
                File.WriteAllText("names_new.json", JsonConvert.SerializeObject(NewItemNames));
            }
        }

        public static void AddAlert(Platform platform, Alert alert)
        {
            AlertDictionary[platform].Add(alert._id.id, alert);
            SerializeAlerts(platform);
        }

        public static void AddGoals(Platform platform, dynamic goal)
        {
            GoalsDictionary[platform].Add((string) goal._id.id, goal);
            SerializeGoals(platform);
        }

        public static void AddInvasion(Platform platform, Invasion invasion)
        {
            InvasionDictionary[platform].Add(invasion._id.id, invasion);
            SerializeInvasion(platform);
        }

        public static void AddBadlandNode(Platform platform, BadlandNode node)
        {
            BadlandDictionary[platform].Add(node._id.id, node);
            SerializeBadlandNodes(platform);
        }

        public static void SerializeBadlandNodes(Platform platform)
        {
            File.WriteAllText($"badlands{platform}.json",
                JsonConvert.SerializeObject(BadlandDictionary[platform].Values.ToList()));
        }

        public static void SerializeCurrentBadlandNodes(Platform platform, List<BadlandNode> nodes)
        {
            File.WriteAllText($"currentbadlands{platform}_full.json",
                JsonConvert.SerializeObject(nodes));
            nodes = JsonConvert.DeserializeObject<List<BadlandNode>>(JsonConvert.SerializeObject(nodes));
            foreach (var node in nodes)
            {
                node.DefenderInfo.MissionInfo = null;
                if (node.AttackerInfo != null)
                    node.AttackerInfo.MissionInfo = null;
                File.WriteAllText(
                    "bl_logs/history/" + node._id.id + $"_{platform.ToString().ToLower()}.json",
                    JsonConvert.SerializeObject(node.History));
                node.History = null;
            }
            File.WriteAllText($"currentbadlands{platform}.json",
                JsonConvert.SerializeObject(nodes));
        }

        public static void SerializeDailyDeals(Platform platform)
        {
            File.WriteAllText($"dailydeal{platform}.json",
                JsonConvert.SerializeObject(DailyDealDictionary[platform].Values.ToList()));
        }

        #endregion
    }

    public class VersionHistory
    {
        public string BuildLabel;
        public long DetectTime;
        public string VersionName;
    }

    public enum Platform
    {
        Pc,
        PS4,
        Xbox,
        PcChina
    }
}