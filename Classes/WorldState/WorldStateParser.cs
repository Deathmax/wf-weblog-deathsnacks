using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using log4net;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.WorldState
{
    public class WorldStateParser
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(WorldStateParser));
        private const string InvasionHtmlFormat =
            "<div class=\"invasion-entry\"><span class=\"invasion-node\">{0}</span>" +
            " (<span class=\"invasion-region\">{12}</span>) - <span class=\"invading-fc\">{1}</span>" +
            " vs <span class=\"defending-type\">{2}</span>" +
            " - <span class=\"invasion-percent\" title=\"{9}\">{3}</span>" +
            "<div class=\"progress\" title=\"{1} - {4} | {2} - {5}\"><div class=\"progress-bar progress-bar-danger {13} {1}\" style=\"width:{3}\">" +
            "<span class=\"faction-bar\">{1} - {4}</span></div><div class=\"progress-bar {14} {2}\" style=\"width:{6}\">" +
            "<span class=\"faction-bar\">{2} - {5}</span></div></div></div><hr class=\"invasion-seperator\">";

        private const string AlertHtmlFormat =
            "<li class=\"list-group-item\"><span class=\"badge time\" data-starttime=\"{0}\"" +
            " data-endtime=\"{1}\"></span>{2}<span class=\"alert-node\">{3} ({4})</span> | " +
            "<span class=\"alert-type\">{5}</span> (<span class=\"alert-fc\">{6}</span>) | Level: {7}-{8}</li>";

        private readonly Dictionary<Platform, string> _contentUrlDictionary = new Dictionary<Platform, string>
        {
            {Platform.Pc, "http://content.warframe.com/dynamic/worldState.php"},
            {Platform.PS4, "http://content.ps4.warframe.com/dynamic/worldState.php"},
            {Platform.Xbox, "http://content.xb1.warframe.com/dynamic/worldState.php"},
            {Platform.PcChina, "http://content.zhb.warframe.com/dynamic/worldState.php"}
        };

        private readonly Dictionary<Platform, string> _dataPrefixPathDictionary = new Dictionary<Platform, string>
        {
            {Platform.Pc, "../data/"},
            {Platform.PS4, "../data/ps4/"},
            {Platform.Xbox, "../data/xbox/"},
            {Platform.PcChina, "../data/china/"}
        };

        // Paths for static output files
        private readonly string _alertsHtmlPath = "alerts.txt";
        private readonly string _alertsRawPath = "alerts_raw.txt";
        private readonly string _dailyDealsPath = "daily_deals.json";
        private readonly string _flashSalesRawPath = "flashsales_raw.txt";
        private readonly string _invasionHtmlPath = "invasion.txt";
        private readonly string _invasionJsonPath = "invasion.json";
        private readonly string _invasionMiniRawPath = "invasion_mini.txt";
        private readonly string _invasionRawPath = "invasion_raw.txt";
        private readonly string _lastUpdatePath = "lastupdate.json";
        private readonly string _libraryTargetPath = "library_target.json";
        private readonly string _newsHtmlPath = "news.txt";
        private readonly string _newsRawPath = "news_raw.txt";
        private readonly string _notificationsJsonPath = "notifications.json";
        private readonly string _overrideRawPath = "node_overrides_raw.txt";
        private readonly string _versionHistoryPath = "versionhistory.json";
        private readonly string _voidTraderPath = "voidtraders.json";
        private readonly string _persistentEnemiesPath = "persistenemies.json";
        private readonly string _sortiesPath = "sorties.json";
        private readonly string _fissuresPath = "activemissions.json";
        private string _alertGcm;
        private List<Alert> _alerts;
        private MySqlConnection _connection;
        private long _earliestExpiry;
        private string _invasionGcm;
        private DateTime _lastModified = DateTime.MinValue;
        private long _lastTime;
        private bool _newStuff;
        private Regex _versionRegex = new Regex(@"(\d\d\d\d)\.(\d\d)\.(\d\d)\.(\d\d)\.(\d\d)");
        private readonly Regex _dateReplacementRegex = new Regex(@"\{""\$date"":\{""\$numberLong"":""(\d\d\d\d\d\d\d\d\d\d)(\d\d\d)""\}\}");
        public Platform CurrentPlatform;
        public WorldStateInfo CurrentWorldStateInfo;
        public List<VersionHistory> VersionHistory;

        public WorldStateParser(Platform platform)
        {
            Log.Info("Initializing " + platform + " with prefix " + _dataPrefixPathDictionary[platform]);
            CurrentPlatform = platform;
            if (!Directory.Exists(_dataPrefixPathDictionary[platform]))
                Directory.CreateDirectory(_dataPrefixPathDictionary[platform]);
            if (!Directory.Exists("invasion_logs_" + platform))
                Directory.CreateDirectory("invasion_logs_" + platform);
            _invasionHtmlPath = _dataPrefixPathDictionary[platform] + _invasionHtmlPath;
            _alertsHtmlPath = _dataPrefixPathDictionary[platform] + _alertsHtmlPath;
            _newsHtmlPath = _dataPrefixPathDictionary[platform] + _newsHtmlPath;
            _invasionRawPath = _dataPrefixPathDictionary[platform] + _invasionRawPath;
            _alertsRawPath = _dataPrefixPathDictionary[platform] + _alertsRawPath;
            _newsRawPath = _dataPrefixPathDictionary[platform] + _newsRawPath;
            _overrideRawPath = _dataPrefixPathDictionary[platform] + _overrideRawPath;
            _flashSalesRawPath = _dataPrefixPathDictionary[platform] + _flashSalesRawPath;
            _invasionMiniRawPath = _dataPrefixPathDictionary[platform] + _invasionMiniRawPath;
            _notificationsJsonPath = _dataPrefixPathDictionary[platform] + _notificationsJsonPath;
            _versionHistoryPath = _dataPrefixPathDictionary[platform] + _versionHistoryPath;
            _lastUpdatePath = _dataPrefixPathDictionary[platform] + _lastUpdatePath;
            _voidTraderPath = _dataPrefixPathDictionary[platform] + _voidTraderPath;
            _invasionJsonPath = _dataPrefixPathDictionary[platform] + _invasionJsonPath;
            _dailyDealsPath = _dataPrefixPathDictionary[platform] + _dailyDealsPath;
            _libraryTargetPath = _dataPrefixPathDictionary[platform] + _libraryTargetPath;
            _persistentEnemiesPath = _dataPrefixPathDictionary[platform] + _persistentEnemiesPath;
            _sortiesPath = _dataPrefixPathDictionary[platform] + _sortiesPath;
            _fissuresPath = _dataPrefixPathDictionary[platform] + _fissuresPath;
            Log.Info("Sample path: " + _alertsRawPath);
        }

        public void Update(bool twitter)
        {
            // Request for a fresh copy of worldstate
            var response = RequestWorldState(true);
            if (response == null)
            {
                Log.Error(CurrentPlatform + ": WorldState update failed.");
                FlatFile.WriteStatus("content" + CurrentPlatform, false);
                return;
            }
            // Write status of CDN to file
            FlatFile.WriteStatus("content" + CurrentPlatform, true);
            // Clean up worldstate for C#, since we can't have an identifier starting with $
            // Why not use JsonProperty? Did not know when this decision was first made
            var cleanedRawWorldState = response.Replace("$id", "id").Replace("$oid", "id");
            // Clean up worldstate to revert back to old timestamp format
            cleanedRawWorldState = _dateReplacementRegex.Replace(cleanedRawWorldState, "{\"sec\": $1, \"usec\": 0}");
            // Write the cleaned worldstate to a file
            File.WriteAllText("worldstatedump" + CurrentPlatform + ".txt", cleanedRawWorldState);
            // Deserialize the raw JSON into an object
            CurrentWorldStateInfo = JsonConvert.DeserializeObject<WorldStateInfo>(cleanedRawWorldState);
            dynamic ws = JsonConvert.DeserializeObject(cleanedRawWorldState);
            Log.InfoFormat(CurrentPlatform + ": Got WorldState info, current version: {0}",
                CurrentWorldStateInfo.Version);

            // Check for a time difference before working
            // The CDN caches results for 1 minute
            // If the API server goes down (api/cdn/worldState.php unreachable), then the cache will become stale
            var dtTime = Utils.UnixTimeStampToDateTime(CurrentWorldStateInfo.Time);
            Log.InfoFormat(CurrentPlatform + ": Time: {0}, Time Difference: {1}", dtTime, (DateTime.UtcNow - dtTime));
            if (_lastTime == 0)
            {
                _lastTime = CurrentWorldStateInfo.Time;
            }
            else
            {
                if (_lastTime == CurrentWorldStateInfo.Time)
                {
                    Log.Warn(CurrentPlatform + ": We are aborting worldState update due to lack of update.");
                    return;
                }
                _lastTime = CurrentWorldStateInfo.Time;
            }
            // For some reason we are deserializing and serializing the json response
            // Probably such that dump now conforms to WorldStateInfo
            var json = JsonConvert.DeserializeObject(response);
            File.WriteAllText("worldstate" + CurrentPlatform + ".txt", JsonConvert.SerializeObject(json));

            // Reset a few flags
            _newStuff = false;
            _earliestExpiry = 0;

            // Create a shallow copy of the alerts list
            _alerts = CurrentWorldStateInfo.Alerts.GetRange(0, CurrentWorldStateInfo.Alerts.Count);
            try
            {
                // Check for new alerts
                foreach (var alert in CurrentWorldStateInfo.Alerts)
                {
                    if (FlatFile.AlertDictionary[CurrentPlatform].ContainsKey(alert._id.id))
                    {
                        continue;
                    }
                    AddAlert(alert);
                }
            }
            catch (Exception ex)
            {
                Log.Error(CurrentPlatform + ": Error with looking for new alerts.");
                Log.Error(ex);
            }

            try
            {
                // Check for new Goals that we know how to parse, which isn't many
                foreach (JObject goal in CurrentWorldStateInfo.Goals)
                {
                    if (goal["Bounty"] != null && goal["MaxConclave"] != null)
                    {
                        var info = goal["MissionInfo"].ToObject<MissionInfo>();
                        info.descText = CommonHelpers.GetTacticalAlertDescription(goal);
                        info.missionReward = goal["Reward"].ToObject<MissionReward>();
                        _alerts.Add(new Alert
                        {
                            _id = goal["_id"].ToObject<Id>(),
                            Activation = goal["Activation"].ToObject<Activation>(),
                            //AllowReplay = 1,
                            Expiry = goal["Expiry"].ToObject<Expiry>(),
                            //ForceUnlock = true,
                            MissionInfo = info
                            //Twitter = 0
                        });
                    }
                    if (FlatFile.GoalsDictionary[CurrentPlatform].ContainsKey(goal["_id"]["id"].ToObject<string>()))
                    {
                        FlatFile.GoalsDictionary[CurrentPlatform][goal["_id"]["id"].ToObject<string>()] = goal;
                        continue;
                    }
                    if (goal["Bounty"] != null)
                    {
                        AddTacticalAlert(goal);
                    }
                }
                FlatFile.SerializeGoals(CurrentPlatform);
            }
            catch (Exception ex)
            {
                Log.Error(CurrentPlatform + ": Error with goals.");
                Log.Error(ex);
            }

            try
            {
                // Render the alerts
                RenderAlerts(_alerts);
            }
            catch (Exception e)
            {
                Log.Error(CurrentPlatform + ": Error with alerts.");
                Log.Error(e);
            }

            try
            {
                // Render the news
                RenderNews(CurrentWorldStateInfo.Events);
            }
            catch (Exception e)
            {
                Log.Error(CurrentPlatform + ": Error with news.");
                Log.Error(e);
            }

            try
            {
                // Render invasions
                RenderInvasions(CurrentWorldStateInfo.Invasions);
                // Try to either find new invasions or update existing invasions
                foreach (var invasion in CurrentWorldStateInfo.Invasions)
                {
                    if (twitter)
                    {
                        if (FlatFile.InvasionDictionary[CurrentPlatform].ContainsKey(invasion._id.id))
                            UpdateInvasion(invasion);
                        else
                            NewInvasion(invasion);
                    }
                    else
                    {
                        if (!FlatFile.InvasionDictionary[CurrentPlatform].ContainsKey(invasion._id.id))
                            NewInvasion(invasion);
                    }
                    DumpInvasion(invasion, CurrentWorldStateInfo.Time);
                }
            }
            catch (Exception e)
            {
                Log.Error(CurrentPlatform + ": Error with invasions.");
                Log.Error(e);
            }
            //RenderOverrideNodes(CurrentWorldStateInfo.NodeOverrides);
            try
            {
                // Render market stuff
                RenderFlashSales(CurrentWorldStateInfo.FlashSales);
            }
            catch (Exception e)
            {
                Log.Error(CurrentPlatform + ": Error with flash sales.");
                Log.Error(e);
            }
            try
            {
                // Render stuff for GCM notifications
                RenderNotifications();
            }
            catch (Exception e)
            {
                Log.Error(CurrentPlatform + ": Error with rendering notifications.");
                Log.Error(e);
            }

            try
            {
                // Render badlands
                foreach (var node in CurrentWorldStateInfo.BadlandNodes)
                {
                    if (!FlatFile.BadlandDictionary[CurrentPlatform].ContainsKey(node._id.id))
                        NewBadlandNode(node);
                    else
                    {
                        try
                        {
                            UpdateBadlandNode(node);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e);
                            Log.Error(CurrentPlatform + ": Error updating a badland node. " + node._id.id);
                        }
                    }
                }
                RenderBadlands(FlatFile.BadlandDictionary[CurrentPlatform]);
                FlatFile.SerializeBadlandNodes(CurrentPlatform);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }

            try
            {
                // Render void traders
                if (CurrentWorldStateInfo.VoidTraders != null)
                {
                    RenderVoidTraders(CurrentWorldStateInfo.VoidTraders);
                }
            }
            catch (Exception ex)
            {
                Log.Error(CurrentPlatform + ": Exception thrown during VT update.");
                Log.Error(ex);
            }
            try
            {
                if (CurrentWorldStateInfo.DailyDeals != null)
                {
                    RenderDailyDeals(CurrentWorldStateInfo.DailyDeals);
                    FlatFile.SerializeDailyDeals(CurrentPlatform);
                }
            }
            catch (Exception ex)
            {
                Log.Error(CurrentPlatform + ": Exception thrown during darvo daily deals update.");
                Log.Error(ex);
            }
            try
            {
                if (CurrentWorldStateInfo.LibraryInfo is JObject)
                {
                    dynamic libraryInfo = CurrentWorldStateInfo.LibraryInfo;
                    if (libraryInfo.CurrentTarget != null && libraryInfo.CurrentTarget.EnemyType != null)
                    {
                        libraryInfo.CurrentTarget.EnemyType =
                            FlatFile.GetUnknownName((string) libraryInfo.CurrentTarget.EnemyType, false);
                        if (libraryInfo.CurrentTarget.StartTime != null &&
                            (Utils.DateTimeToUnixTimestamp(DateTime.UtcNow) >=
                             (long) libraryInfo.CurrentTarget.StartTime.sec))
                        {
                            File.AppendAllText(
                                Path.Combine("targetlog",
                                    (string) libraryInfo.CurrentTarget.EnemyType + CurrentPlatform + ".txt"),
                                $"{CurrentWorldStateInfo.Time},{(float) libraryInfo.CurrentTarget.ProgressPercent}\n");
                        }
                    }
                    File.WriteAllText(_libraryTargetPath, JsonConvert.SerializeObject(libraryInfo));
                }
            }
            catch (Exception ex)
            {
                Log.Error(CurrentPlatform + ": Exception thrown during library info update.");
                Log.Error(ex);
            }
            try
            {
                if (CurrentWorldStateInfo.PersistentEnemies != null)
                {
                    RenderPersistentEnemies(CurrentWorldStateInfo.PersistentEnemies);
                }
            }
            catch (Exception ex)
            {
                Log.Error(CurrentPlatform + ": Exception thrown during persistent enemies update.");
                Log.Error(ex);
            }

            try
            {
                RenderSorties(CurrentWorldStateInfo.Sorties);
            }
            catch (Exception ex)
            {
                Log.Error(CurrentPlatform + ": Exception thrown during sorties update.");
                Log.Error(ex);
            }

            try
            {
                RenderFissures(CurrentWorldStateInfo.ActiveMissions);
            }
            catch (Exception ex)
            {
                Log.Error(CurrentPlatform + ": Exception thrown during void fissures update.");
                Log.Error(ex);
            }
            FlatFile.SerializeCurrentBadlandNodes(CurrentPlatform, CurrentWorldStateInfo.BadlandNodes);
            FlatFile.SerializeCurrentAlerts(CurrentPlatform, _alerts);
            FlatFile.SerializeCurrentInvasions(CurrentPlatform, CurrentWorldStateInfo.Invasions);
            File.WriteAllText(_lastUpdatePath,
                JsonConvert.SerializeObject(new Dictionary<string, long> {{"LastUpdate", CurrentWorldStateInfo.Time}}));
            CheckVersion(CurrentWorldStateInfo.BuildLabel);
            if (_newStuff)
            {
                SendDataGcm();
            }
        }

        public void DumpState()
        {
            var response = RequestWorldState();
            if (response == null)
            {
                Log.Error(CurrentPlatform + ": WorldState update failed.");
                return;
            }
            Log.Info(CurrentPlatform + ": Got WorldState info.");
            var json = JsonConvert.DeserializeObject(response);
            File.WriteAllText("worldstate.txt", JsonConvert.SerializeObject(json));
        }

        private void DumpInvasion(Invasion invasion, long time)
        {
            var goal = (double) invasion.Goal;
            var count = (double) invasion.Count;
            var node = FlatFile.GetPlanetName(invasion.Node);
            var old = FlatFile.InvasionDictionary[CurrentPlatform][invasion._id.id];
            var oldcomplete = old.Completed;
            if (invasion.Completed && !oldcomplete)
            {
                UpdateInvasion(invasion);
            }
            if (invasion.Completed)
                return;
            File.AppendAllText(
                "invasion_logs_" + CurrentPlatform + "/" + node + "_" + invasion.Activation.sec + ".csv",
                $"{time},{goal},{count}\n");
        }

        private void UpdateInvasion(Invasion invasion)
        {
            var invadingfaction = Utils.UppercaseFirst(invasion.Faction.Split('_')[1]);
            var defendingfaction = Utils.UppercaseFirst(invasion.AttackerMissionInfo.faction.Split('_')[1]);
            var goal = (double) invasion.Goal;
            var count = (double) invasion.Count;
            var node = FlatFile.GetPlanetName(invasion.Node);
            var old = FlatFile.InvasionDictionary[CurrentPlatform][invasion._id.id];
            var percent = 50 + (count/goal*50);
            var oldpercent = 50 + ((((double) old.Count)/old.Goal)*50);
            if (invasion.AttackerMissionInfo.faction == "FC_INFESTATION")
            {
                percent = count/goal*100;
                oldpercent = (((double) old.Count)/old.Goal)*100;
            }
            else if (invasion.Faction == "FC_INFESTATION")
            {
                percent = 100 + (count/goal*100);
                oldpercent = 100 + ((((double) old.Count)/old.Goal)*100);
            }
            var percentstring = percent.ToString("0.00") + "%";
            var symbol = (percent > oldpercent) ? "+" : "";
            var percentchange = (percent) - (oldpercent);
            var percentchangestring = symbol + percentchange.ToString("0.00") + "%";
            string eta;
            if (percentchange > 0)
            {
                var minutes = ((100 - percent)/percentchange)*
                              ((Utils.UnixTimeStampToDateTime(CurrentWorldStateInfo.Time) - old.LastUpdate).TotalMinutes);
                if (minutes < 120)
                    eta = (int) minutes + " mins";
                else
                    eta = (int) (minutes/60) + " hrs";
            }
            else
            {
                var minutes = (percent/Math.Abs(percentchange))*
                              ((Utils.UnixTimeStampToDateTime(CurrentWorldStateInfo.Time) - old.LastUpdate).TotalMinutes);
                if (minutes < 120)
                    eta = (int) minutes + " mins";
                else
                    eta = (int) (minutes/60) + " hrs";
            }
            invasion.LastUpdate = Utils.UnixTimeStampToDateTime(CurrentWorldStateInfo.Time);
            FlatFile.InvasionDictionary[CurrentPlatform][invasion._id.id] = invasion;
            FlatFile.SerializeInvasion(CurrentPlatform);
            var rewards = CommonHelpers.GetInvasionRewards(invasion);
            var atkrewtext = rewards[0];
            var defrewtext = rewards[1];
            string factiontext;
            string tweet;
            if (invasion.Completed && !old.Completed)
            {
                _newStuff = true;
                factiontext = ((((count/goal)*100) >= 100))
                    ? invadingfaction + " has conquered."
                    : defendingfaction + " has defended.";
                var timetaken = (double) (CurrentWorldStateInfo.Time - invasion.Activation.sec)
                                /60/60;
                tweet = string.Format("{0} ({1}|{6}) ({4} | {5}) has just completed after ~{2:0.00} hours. {3}",
                    node, invadingfaction, timetaken, factiontext, atkrewtext, defrewtext, defendingfaction);
                switch (CurrentPlatform)
                {
                    case Platform.Pc:
                        Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PCInvasionUpdate, tweet);
                        break;
                    case Platform.PS4:
                        Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PS4InvasionUpdate, tweet);
                        break;
                    case Platform.Xbox:
                        break;
                    case Platform.PcChina:
                        break;
                }
                return;
            }
            if (invasion.Completed)
                return;
            factiontext = (percent > oldpercent)
                ? (invadingfaction + " gaining")
                : (defendingfaction + " gaining");
            var tampertext = "";
            if (Math.Abs(goal - old.Goal) > 1)
                tampertext = " (TAMPERED)";
            tweet = string.Format("{0} ({1}) - {4} ({5}) - Goal: {2:n0}/{3:n0} - {6} - {7} | {8} - ETA: {9}{10}",
                node, invadingfaction, count, goal, percentstring, percentchangestring, factiontext, atkrewtext,
                defrewtext, eta, tampertext);
            if (tweet.Length > 140)
            {
                tweet = tweet.Replace(",", "");
                if (tweet.Length > 140)
                    tweet = tweet.Replace(" ", "");
            }
            switch (CurrentPlatform)
            {
                case Platform.Pc:
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PCInvasionUpdate, tweet);
                    break;
                case Platform.PS4:
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PS4InvasionUpdate, tweet);
                    break;
                case Platform.Xbox:
                    break;
                case Platform.PcChina:
                    break;
            }
        }

        private void NewInvasion(Invasion invasion)
        {
            _newStuff = true;
            invasion.LastUpdate = Utils.UnixTimeStampToDateTime(CurrentWorldStateInfo.Time);
            FlatFile.AddInvasion(CurrentPlatform, invasion);
            var invadingfaction = Utils.UppercaseFirst(invasion.Faction.Split('_')[1]);
            var defendingfaction = Utils.UppercaseFirst(invasion.AttackerMissionInfo.faction.Split('_')[1]);
            var goal = (double) invasion.Goal;
            var node = FlatFile.GetPlanetName(invasion.Node);
            if (invasion.Completed)
                return;
            var rewards = CommonHelpers.GetInvasionRewards(invasion);
            var atkrewtext = rewards[0];
            var defrewtext = rewards[1];
            var rewardtext = "";
            if (invadingfaction != "Infestation")
                rewardtext += invadingfaction + ": " + atkrewtext + ", ";
            rewardtext += defendingfaction + ": " + defrewtext;
            var desc = FlatFile.GetString(invasion.LocTag);
            var tweet = "";
            tweet = string.Format("{0} ({4}) - {1} - Goal: {2:n0} - Rewards: {3}",
                node, desc, goal, rewardtext, FlatFile.GetRegion(invasion.Node));
            switch (CurrentPlatform)
            {
                case Platform.Pc:
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PCInvasionUpdate, tweet);
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PCInvasionNew, tweet);
                    break;
                case Platform.PS4:
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PS4InvasionUpdate, tweet);
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PS4InvasionNew, tweet);
                    break;
                case Platform.Xbox:
                    break;
                case Platform.PcChina:
                    break;
            }
        }

        private void AddAlert(Alert alert)
        {
            _newStuff = true;
            FlatFile.AddAlert(CurrentPlatform, alert);
            var rewards = CommonHelpers.GetAlertRewards(alert);
            if (!rewards.Contains("-"))
                return;
            var planet = FlatFile.GetPlanetNameWithRegion(alert.MissionInfo.location);
            var mission = Names.GetMissionType(alert.MissionInfo.missionType);
            if (alert.MissionInfo.nightmare || alert.MissionInfo.descText == "/Lotus/Language/Alerts/NightmareAlertDesc")
                mission = "Nightmare " + mission;
            var faction = Names.GetFaction(alert.MissionInfo.faction);
            var starttime = Utils.UnixTimeStampToDateTime(alert.Activation.sec);
            var expiretime = Utils.UnixTimeStampToDateTime(alert.Expiry.sec);
            var startsin = (int) (starttime - DateTime.UtcNow).TotalMinutes;
            var duration = (int) (expiretime - starttime).TotalMinutes;
            var desc = FlatFile.GetString(alert.MissionInfo.descText);
            if (alert.MissionInfo.archwingRequired != null && alert.MissionInfo.archwingRequired)
            {
                desc = desc + " (Archwing)";
            }
            var tweet = "";
            tweet = string.Format("{0} | {1} ({2}) | {6} | Starts in {3}m | {4}m | {5}",
                planet, mission, faction, startsin, duration, rewards, desc);
            Log.Info(CurrentPlatform + ": Found new alert. Tweet: " + tweet);
            //if (alert.MissionInfo.missionReward.items.Count > 0 || alert.MissionInfo.missionReward.countedItems.Count > 0)
            //    Twitter.Twitter.PostStatusFilter(tweet);
            switch (CurrentPlatform)
            {
                case Platform.Pc:
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PCAlertFiltered, tweet);
                    break;
                case Platform.PS4:
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PS4AlertFiltered, tweet);
                    break;
                case Platform.Xbox:
                    break;
                case Platform.PcChina:
                    break;
            }
        }

        private void AddTacticalAlert(dynamic goal)
        {
            _newStuff = true;
            FlatFile.AddGoals(CurrentPlatform, goal);
            string rewards = CommonHelpers.GetAlertRewards(goal);
            if (!rewards.Contains("-"))
                return;
            var planet = FlatFile.GetPlanetNameWithRegion((string) goal.Node);
            var mission = Names.GetMissionType((string) goal.MissionInfo.missionType);
            var faction = Names.GetFaction((string) goal.MissionInfo.faction);
            var starttime = Utils.UnixTimeStampToDateTime((long) goal.Activation.sec);
            var expiretime = Utils.UnixTimeStampToDateTime((long) goal.Expiry.sec);
            var startsin = (int) (starttime - DateTime.UtcNow).TotalMinutes;
            var duration = (int) (expiretime - starttime).TotalMinutes;
            var desc = CommonHelpers.GetTacticalAlertDescription(goal);
            var tweet = "";
            tweet = string.Format("{0} | {1} ({2}) | {6} | Starts in {3}m | {4}m | {5}",
                planet, mission, faction, startsin, duration, rewards, desc);
            Log.Info(CurrentPlatform + ": Found new tactical alert. Tweet: " + tweet);
            //if (alert.MissionInfo.missionReward.items.Count > 0 || alert.MissionInfo.missionReward.countedItems.Count > 0)
            //    Twitter.Twitter.PostStatusFilter(tweet);
            switch (CurrentPlatform)
            {
                case Platform.Pc:
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PCAlertFiltered, tweet);
                    break;
                case Platform.PS4:
                    Twitter.Twitter.PostStatus(Twitter.Twitter.TwitterType.PS4AlertFiltered, tweet);
                    break;
                case Platform.Xbox:
                    break;
                case Platform.PcChina:
                    break;
            }
        }

        private void NewBadlandNode(BadlandNode node)
        {
            FlatFile.AddBadlandNode(CurrentPlatform, node);
        }

        private void UpdateBadlandNode(BadlandNode node)
        {
            #region Old/New Checks

            var old = FlatFile.BadlandDictionary[CurrentPlatform][node._id.id];
            var noderegion = FlatFile.GetRegion(node.Node);
            var nodename = FlatFile.GetPlanetName(node.Node);
            try
            {
                if (old.DefenderInfo.Id.id == node.DefenderInfo.Id.id)
                {
                    var id = node.DefenderInfo.Id.id;
                    var filename = $"bl_logs/clans/{id}.csv";
                    if (!File.Exists(filename))
                    {
                        File.AppendAllText(filename,
                            $"{node.DefenderInfo.Name},{(node.DefenderInfo.IsAlliance ? "A" : "C")}\n");
                    }
                    try
                    {
                        if (CommonHelpers.HasTaxChanged(old.DefenderInfo, node.DefenderInfo))
                        {
                            File.AppendAllText(filename,
                                $"{CurrentWorldStateInfo.Time},TAX_CHANGED_2,{nodename} ({noderegion}),{node.DefenderInfo.CreditsTaxRate},{node.DefenderInfo.ItemsTaxRate},{node.DefenderInfo.MemberCreditsTaxRate},{node.DefenderInfo.MemberItemsTaxRate},{node.DefenderInfo.TaxLastChangedBy},{node.DefenderInfo.TaxLastChangedByClan}\n");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(CurrentPlatform + ": Tax check failed.");
                        Log.Error(e);
                    }
                    try
                    {
                        if (CommonHelpers.HasMOTDChanged(old.DefenderInfo, node.DefenderInfo))
                        {
                            File.AppendAllText(filename,
                                $"{CurrentWorldStateInfo.Time},MOTD_CHANGED,{nodename} ({noderegion}),{node.DefenderInfo.MOTD.Replace(",", "{*}")},{node.DefenderInfo.MOTDAuthor.Replace(",", "{*}")}\n");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(CurrentPlatform + ": MOTD check failed.");
                        Log.Error(e);
                    }
                    try
                    {
                        if (CommonHelpers.HasBattlePayChanged(old.DefenderInfo, node.DefenderInfo))
                        {
                            File.AppendAllText(filename,
                                $"{CurrentWorldStateInfo.Time},BATTLE_PAY_CHANGED_2,{nodename} ({noderegion}),{node.DefenderInfo.BattlePayReserve},{node.DefenderInfo.MissionBattlePay},{node.DefenderInfo.BattlePaySetBy},{node.DefenderInfo.BattlePaySetByClan}\n");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(CurrentPlatform + ": Pay check failed.");
                        Log.Error(e);
                    }
                    try
                    {
                        if (CommonHelpers.HasClanNameChanged(old.DefenderInfo, node.DefenderInfo))
                        {
                            File.AppendAllText(filename,
                                $"{CurrentWorldStateInfo.Time},NAME_CHANGED,{node.DefenderInfo.Name}\n");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(CurrentPlatform + ": Clan name check failed.");
                        Log.Error(e);
                    }
                }
                if (node.AttackerInfo != null)
                {
                    var id = node.AttackerInfo.Id.id;
                    var filename = $"bl_logs/clans/{id}.csv";
                    if (old.AttackerInfo != null && old.AttackerInfo.Id.id != node.AttackerInfo.Id.id)
                    {
                        Log.WarnFormat(CurrentPlatform + ": We bailing out of this attacker update {0} as we changed.",
                            node.NodeRegionName);
                    }
                    else
                    {
                        if (!File.Exists(filename))
                        {
                            File.AppendAllText(filename,
                                $"{node.AttackerInfo.Name},{(node.AttackerInfo.IsAlliance ? "A" : "C")}\n");
                        }
                        try
                        {
                            if (CommonHelpers.HasTaxChanged(old.AttackerInfo, node.AttackerInfo))
                            {
                                File.AppendAllText(filename,
                                    $"{CurrentWorldStateInfo.Time},TAX_CHANGED_2,{nodename} ({noderegion}),{node.AttackerInfo.CreditsTaxRate},{node.AttackerInfo.ItemsTaxRate},{node.AttackerInfo.MemberCreditsTaxRate},{node.AttackerInfo.MemberItemsTaxRate},{node.AttackerInfo.TaxLastChangedBy},{node.AttackerInfo.TaxLastChangedByClan}\n");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(CurrentPlatform + ": Tax check failed.");
                            Log.Error(e);
                        }
                        try
                        {
                            if (CommonHelpers.HasMOTDChanged(old.AttackerInfo, node.AttackerInfo))
                            {
                                File.AppendAllText(filename,
                                    $"{CurrentWorldStateInfo.Time},MOTD_CHANGED,{nodename} ({noderegion}),{node.AttackerInfo.MOTD.Replace(",", "{*}")},{node.AttackerInfo.MOTDAuthor.Replace(",", "{*}")}\n");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(CurrentPlatform + ": MOTD check failed.");
                            Log.Error(e);
                            throw;
                        }
                        try
                        {
                            if (CommonHelpers.HasBattlePayChanged(old.AttackerInfo, node.AttackerInfo))
                            {
                                File.AppendAllText(filename,
                                    $"{CurrentWorldStateInfo.Time},BATTLE_PAY_CHANGED_2,{nodename} ({noderegion}),{node.AttackerInfo.BattlePayReserve},{node.AttackerInfo.MissionBattlePay},{node.AttackerInfo.BattlePaySetBy},{node.AttackerInfo.BattlePaySetByClan}\n");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(CurrentPlatform + ": Pay check failed.");
                            Log.Error(e);
                        }
                        try
                        {
                            if (CommonHelpers.HasClanNameChanged(old.AttackerInfo, node.AttackerInfo))
                            {
                                File.AppendAllText(filename,
                                    $"{CurrentWorldStateInfo.Time},NAME_CHANGED,{node.AttackerInfo.Name}\n");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Error(CurrentPlatform + ": Name check failed.");
                            Log.Error(e);
                        }
                    }
                    if (old.AttackerInfo == null)
                    {
                        File.AppendAllText(filename,
                            $"{CurrentWorldStateInfo.Time},ATTACKING,{nodename} ({noderegion}),{node.AttackerInfo.DeployerName}\n");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                Log.Error(CurrentPlatform + ": We got an exception while checking the node. Please check error log.");
                Log.ErrorFormat("Bad data:\n{0}", JsonConvert.SerializeObject(node));
                throw;
            }

            #endregion

            #region History Checks

            var oldhistory = FlatFile.BadlandDictionary[CurrentPlatform][node._id.id].History;
            FlatFile.BadlandDictionary[CurrentPlatform][node._id.id] = node;
            if (node.History != null)
            {
                foreach (var history in oldhistory)
                {
                    var present = false;
                    foreach (var newhistory in node.History)
                    {
                        if (newhistory.Start.sec == history.Start.sec)
                        {
                            present = true;
                            break;
                        }
                    }
                    if (!present)
                    {
                        FlatFile.BadlandDictionary[CurrentPlatform][node._id.id].History.Add(history);
                    }
                }
                FlatFile.BadlandDictionary[CurrentPlatform][node._id.id].History =
                    FlatFile.BadlandDictionary[CurrentPlatform][node._id.id].History.OrderByDescending(x => x.Start.sec)
                        .ToList();
            }

            #endregion

            #region Conflict Logging

            if (node.AttackerInfo != null &&
                node.ConflictExpiration.sec >= Utils.DateTimeToUnixTimestamp(DateTime.UtcNow) &&
                Utils.DateTimeToUnixTimestamp(DateTime.UtcNow) >= node.AttackerInfo.DeploymentActivationTime.sec)
            {
                var filename = $"bl_logs/{node._id.id}_{node.AttackerInfo.DeploymentActivationTime.sec}.csv";
                if (!File.Exists(filename))
                {
                    File.WriteAllText(filename,
                        $"{node.DefenderInfo.Name},{node.AttackerInfo.Name}\n");
                }
                File.AppendAllText(filename,
                    $"{CurrentWorldStateInfo.Time},{node.DefenderInfo.StrengthRemaining},{node.DefenderInfo.MaxStrength},{node.DefenderInfo.MissionBattlePay},{node.DefenderInfo.BattlePayReserve},{node.AttackerInfo.StrengthRemaining},{node.AttackerInfo.MaxStrength},{node.AttackerInfo.MissionBattlePay},{node.AttackerInfo.BattlePayReserve}\n");
            }

            #endregion
        }

        private void CheckVersion(string label)
        {
            var oldLabel = "";
            if (File.Exists(CurrentPlatform + "_buildlabel.txt"))
                oldLabel = File.ReadAllText(CurrentPlatform + "_buildlabel.txt");
            if (label == oldLabel)
                return;
            File.WriteAllText(CurrentPlatform + "_buildlabel.txt", label);
            VersionHistory = File.Exists(_versionHistoryPath)
                ? JsonConvert.DeserializeObject<List<VersionHistory>>(File.ReadAllText(_versionHistoryPath))
                : new List<VersionHistory>();
            var version = new VersionHistory
            {
                DetectTime = (int) Utils.DateTimeToUnixTimestamp(DateTime.UtcNow),
                BuildLabel = label
            };
            Log.InfoFormat(CurrentPlatform + ": We got new {1} build. {0}", label, CurrentPlatform);
            VersionHistory.Add(version);
            File.WriteAllText(_versionHistoryPath,
                JsonConvert.SerializeObject(VersionHistory.OrderByDescending(x => x.DetectTime), Formatting.Indented));
            VersionHistory = null;
        }

        #region GCM stuff

        private void SendDataGcm()
        {
            switch (CurrentPlatform)
            {
                case Platform.Pc:
                    break;
                case Platform.PS4:
                    break;
                case Platform.Xbox:
                    break;
                case Platform.PcChina:
                    Log.Info(CurrentPlatform + ": PC China doesn't have GCM support.");
                    return;
            }
            Log.Info(CurrentPlatform + ": Starting GCM notification with data.");
            if (string.IsNullOrWhiteSpace(Program.Config.DbAddress))
            {
                Log.Warn(CurrentPlatform + ": DbAddress is empty, skipping GCM push.");
                return;
            }
            var connectionstr =
                $"SERVER={Program.Config.DbAddress};DATABASE={Program.Config.DbTable};UID={Program.Config.DbUsername};PASSWORD={Program.Config.DbPassword};";
            _connection = new MySqlConnection(connectionstr);
            var ids = new List<string>();
            try
            {
                _connection.Open();
                var cmd = _connection.CreateCommand();
                cmd.CommandText = "SELECT id FROM gcm_ids";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        ids.Add((string) reader["id"]);
                }
                cmd.Dispose();
                _connection.Close();
            }
            catch (Exception e)
            {
                Log.Error(CurrentPlatform + ": Error with MySQL.");
                Log.Error(e);
                return;
            }
            if (ids.Count == 0)
            {
                Log.Warn(CurrentPlatform + ": Abandon ship, we have 0 push notification ids.");
                return;
            }
            var ttl = (int) (_earliestExpiry - Utils.DateTimeToUnixTimestamp(DateTime.UtcNow));
            switch (CurrentPlatform)
            {
                case Platform.Pc:
                    Push.Push.PushDataNotification(ids, "pcalerts", ttl,
                        new Dictionary<string, string> {{"alerts", _alertGcm}, {"invasions", _invasionGcm}});
                    break;
                case Platform.PS4:
                    Push.Push.PushDataNotificationPs4(ids, ttl,
                        new Dictionary<string, string> {{"alerts_ps4", _alertGcm}, {"invasions_ps4", _invasionGcm}});
                    break;
                case Platform.Xbox:
                    Push.Push.PushDataNotificationXbox(ids, ttl,
                        new Dictionary<string, string> {{"alerts_xbox", _alertGcm}, {"invasions_xbox", _invasionGcm}});
                    break;
                case Platform.PcChina:
                    return;
            }
        }

        #endregion

        #region Renderers

        private void RenderInvasions(IEnumerable<Invasion> invasions)
        {
            var ordered =
                invasions.OrderBy(x => FlatFile.GetRegion(x.Node)).ThenBy(x => x.Activation.sec).ToList();
            var newtext = "";
            var newrawtext = CurrentWorldStateInfo.Time + "\n";
            var oldtext = "";
            var minirawtext = "";
            var jsonList = new List<InvasionJson>();
            _invasionGcm = "";
            foreach (var invasion in ordered)
            {
                if (invasion.Completed)
                    continue;
                var invadingfaction = Utils.UppercaseFirst(invasion.Faction.Split('_')[1]);
                var defendingfaction = Utils.UppercaseFirst(invasion.AttackerMissionInfo.faction.Split('_')[1]);
                var invadingmission = invasion.AttackerMissionInfo.missionType != null ? 
                    Names.GetMissionType(invasion.AttackerMissionInfo.missionType) :
                    "???";
                var defendingmission = invasion.DefenderMissionInfo.missionType != null ?
                    Names.GetMissionType(invasion.DefenderMissionInfo.missionType) : 
                    "???";
                var goal = (double) invasion.Goal;
                var count = (double) invasion.Count;
                var node = FlatFile.GetPlanetName(invasion.Node);
                var region = FlatFile.GetRegion(invasion.Node);
                var percent = 50 + (count/goal*50);
                if (invasion.AttackerMissionInfo.faction == "FC_INFESTATION")
                    percent = count/goal*100;
                else if (invasion.Faction == "FC_INFESTATION")
                    percent = 100 + (count/goal*100);
                var eta = "";
                var winningfaction = "";
                if (FlatFile.InvasionDictionary[CurrentPlatform].ContainsKey(invasion._id.id))
                {
                    var old = FlatFile.InvasionDictionary[CurrentPlatform][invasion._id.id];
                    var oldpercent = 50 + ((((double) old.Count)/old.Goal)*50);
                    if (invasion.AttackerMissionInfo.faction == "FC_INFESTATION")
                        oldpercent = (((double) old.Count)/old.Goal)*100;
                    else if (invasion.Faction == "FC_INFESTATION")
                        oldpercent = 100 + ((((double) old.Count)/old.Goal)*100);
                    eta = " - ETA: ";
                    var percentchange = (percent) - (oldpercent);
                    if (percentchange > 0)
                    {
                        var minutes = ((100 - percent)/percentchange)*
                                      ((Utils.UnixTimeStampToDateTime(CurrentWorldStateInfo.Time) - old.LastUpdate)
                                          .TotalMinutes);
                        if (minutes < 120)
                            eta += (int) minutes + " mins";
                        else
                            eta += (int) (minutes/60) + " hrs";
                    }
                    else
                    {
                        var minutes = (percent/Math.Abs(percentchange))*
                                      ((Utils.UnixTimeStampToDateTime(CurrentWorldStateInfo.Time) - old.LastUpdate)
                                          .TotalMinutes);
                        if (minutes < 120)
                            eta += (int) minutes + " mins";
                        else
                            eta += (int) (minutes/60) + " hrs";
                    }
                    winningfaction = (percent > oldpercent) ? (invadingfaction) : (defendingfaction);
                }
                var invadingpercentstring = percent.ToString("0.00") + "%";
                var defendingpercentstring =
                    (100 - double.Parse(invadingpercentstring.TrimEnd('%'))).ToString("0.00") +
                    "%";
                var rewards = CommonHelpers.GetInvasionRewards(invasion);
                var atkrewtext = rewards[0];
                var defrewtext = rewards[1];
                var goaltext = invasion.Count + "/" + invasion.Goal + " - ";
                var runningtime = (DateTime.UtcNow - Utils.UnixTimeStampToDateTime(invasion.Activation.sec));
                var timetext = "Running time: ";
                if (runningtime.TotalDays > 1)
                    timetext += runningtime.TotalDays.ToString("0.00") + "d";
                else if (runningtime.TotalHours > 1)
                    timetext += runningtime.TotalHours.ToString("0.00") + "h";
                else
                    timetext += runningtime.TotalMinutes.ToString("0.00") + "m";
                var rawar = new[]
                {
                    invasion._id.id,
                    node,
                    region,
                    invadingfaction,
                    invadingfaction == "Infestation" ? invadingmission + "Infest" : invadingmission,
                    atkrewtext,
                    invasion.AttackerMissionInfo.minEnemyLevel + "-" + invasion.AttackerMissionInfo.maxEnemyLevel,
                    Names.GetEnemySpec(invasion.AttackerMissionInfo.enemySpec),
                    defendingfaction,
                    defendingfaction == "Infestation" ? defendingmission + "Infest" : defendingmission,
                    defrewtext,
                    invasion.DefenderMissionInfo.minEnemyLevel + "-" + invasion.DefenderMissionInfo.maxEnemyLevel,
                    Names.GetEnemySpec(invasion.DefenderMissionInfo.enemySpec),
                    invasion.Activation.sec.ToString(),
                    invasion.Count.ToString(),
                    invasion.Goal.ToString(),
                    percent.ToString("0.00"),
                    eta.Trim(' ', '-'),
                    FlatFile.GetString(invasion.LocTag)
                };
                var miniraw = new[]
                {
                    invasion._id.id,
                    node,
                    region,
                    invadingfaction,
                    invadingfaction == "Infestation" ? invadingmission + "Infest" : invadingmission,
                    atkrewtext,
                    defendingfaction == "Infestation" ? defendingmission + "Infest" : defendingmission,
                    defendingmission,
                    defrewtext,
                    invasion.Activation.sec.ToString(),
                    FlatFile.GetString(invasion.LocTag)
                };
                var miniminiraw = new[]
                {
                    invasion._id.id,
                    node,
                    region,
                    invadingfaction,
                    invadingfaction == "Infestation" ? invadingmission + "Infest" : invadingmission,
                    atkrewtext,
                    defendingfaction,
                    defendingfaction == "Infestation" ? defendingmission + "Infest" : defendingmission,
                    defrewtext
                };
                var badges = CommonHelpers.GetInvasionBadges(defrewtext, atkrewtext, invasion);
                var text =
                    string.Format(
                        "<div class=\"invasion-entry\"><table class=\"invasion-table\"><tbody><tr><td valign=\"bottom\" class=\"invading-cell\">{2}</td><td><span class=\"invasion-node\">{0}</span> (<span class=\"invasion-region\">{1}</span>) - ",
                        node, region, badges[0]);
                text += $"<span class=\"invasion-desc\" title=\"{FlatFile.GetString(invasion.LocTag)} - \"></span>";
                text +=
                    $" <span class\"invading-fc\">{invadingfaction}</span> <span class=\"invading-type\" title=\"Level: {invasion.AttackerMissionInfo.minEnemyLevel}-{invasion.AttackerMissionInfo.maxEnemyLevel}\" style=\"{(invadingfaction == "Infestation" ? "display:none;" : "")}\">({invadingmission})</span>";
                text +=
                    $" vs <span class\"defending-type\">{defendingfaction}</span> <span class=\"defending-fc\" title=\"Level: {invasion.DefenderMissionInfo.minEnemyLevel}-{invasion.DefenderMissionInfo.maxEnemyLevel}\" style=\"{(defendingfaction == "Infestation" ? "display:none;" : "")}\">({defendingmission})</span> - ";
                text +=
                    $"<span class=\"invasion-percent\" title=\"{goaltext}{timetext}{eta}\">{percent:0.00}%</span></td><td valign=\"bottom\" class=\"defending-cell\">{badges[1]}</td></tr></tbody></table>";
                text +=
                    string.Format(
                        "<div class=\"progress\"><div class=\"progress-bar {4} {0} progress-bar{1}\" style=\"width:{2}\"><span class=\"faction-bar\"><img src=\"img/{3}.png\" style=\"height:20px;float:left;\"></span></div>",
                        invadingfaction,
                        (invadingfaction == "Infestation" ? "-success" : (invadingfaction == "Grineer" ? "-danger" : "")),
                        invadingpercentstring, invadingfaction.ToLower(),
                        winningfaction == invadingfaction ? "arrow-right" : "");
                text +=
                    string.Format(
                        "<div class=\"progress-bar {4} {0} progress-bar{1}\" style=\"width:{2}\"><span class=\"faction-bar\"><img src=\"img/{3}.png\" style=\"height:20px;float:right;\"></span></div></div></div>",
                        defendingfaction,
                        (defendingfaction == "Infestation"
                            ? "-success"
                            : (defendingfaction == "Grineer" ? "-danger" : "")),
                        defendingpercentstring, defendingfaction.ToLower(),
                        winningfaction == defendingfaction ? "arrow-left" : "");
                newtext += text;
                var attackinglevel = "Level: " + invasion.AttackerMissionInfo.minEnemyLevel + "-" +
                                     invasion.AttackerMissionInfo.maxEnemyLevel;
                var defendinglevel = "Level: " + invasion.DefenderMissionInfo.minEnemyLevel + "-" +
                                     invasion.DefenderMissionInfo.maxEnemyLevel;
                oldtext += string.Format(InvasionHtmlFormat, node, invadingfaction, defendingfaction,
                    invadingpercentstring,
                    atkrewtext, defrewtext, defendingpercentstring, invadingmission, defendingmission,
                    goaltext + timetext + eta,
                    attackinglevel, defendinglevel, region, (invadingfaction == winningfaction ? "arrow-right" : ""),
                    (defendingfaction == winningfaction ? "arrow-left" : ""));
                newrawtext += string.Join("|", rawar) + "\n";
                minirawtext += string.Join("|", miniraw) + "\n";
                _invasionGcm += string.Join("|", miniminiraw) + "\n";
                jsonList.Add(new InvasionJson
                {
                    Id = invasion._id.id,
                    Node = node,
                    Region = region,
                    Percentage = percent,
                    Eta = eta.Trim(' ', '-'),
                    Description = FlatFile.GetString(invasion.LocTag),
                    Activation = invasion.Activation.sec,
                    Count = invasion.Count,
                    Goal = invasion.Goal,
                    InvaderInfo = new InvasionJson.SideInfo
                    {
                        Faction = invadingfaction,
                        AISpec = Names.GetEnemySpec(invasion.AttackerMissionInfo.enemySpec),
                        MissionType = invadingmission,
                        MinLevel = invasion.AttackerMissionInfo.minEnemyLevel,
                        MaxLevel = invasion.AttackerMissionInfo.maxEnemyLevel,
                        Reward = atkrewtext,
                        Winning = invadingfaction == winningfaction
                    },
                    DefenderInfo = new InvasionJson.SideInfo
                    {
                        Faction = defendingfaction,
                        AISpec = Names.GetEnemySpec(invasion.DefenderMissionInfo.enemySpec),
                        MissionType = defendingmission,
                        MinLevel = invasion.DefenderMissionInfo.minEnemyLevel,
                        MaxLevel = invasion.DefenderMissionInfo.maxEnemyLevel,
                        Reward = defrewtext,
                        Winning = defendingfaction == winningfaction
                    }
                });
            }
            if (!(from invasion in invasions
                where !invasion.Completed
                select invasion).Any())
            {
                newtext = "<div class=\"invasion-entry\">No invasions at the moment.</div>";
            }
            if (File.ReadAllText(_invasionHtmlPath + "2") != newtext)
                File.WriteAllText(_invasionHtmlPath + "2", newtext);
            if (File.ReadAllText(_invasionHtmlPath) != oldtext)
                File.WriteAllText(_invasionHtmlPath, oldtext);
            if (File.ReadAllText(_invasionRawPath) != newrawtext)
                File.WriteAllText(_invasionRawPath, newrawtext);
            if (File.ReadAllText(_invasionMiniRawPath) != minirawtext)
                File.WriteAllText(_invasionMiniRawPath, minirawtext);
            var jsonSerialized = JsonConvert.SerializeObject(jsonList);
            if ((File.Exists(_invasionJsonPath) && File.ReadAllText(_invasionJsonPath) != jsonSerialized) ||
                !File.Exists(_invasionJsonPath))
                File.WriteAllText(_invasionJsonPath, jsonSerialized);
        }

        private void RenderAlerts(IEnumerable<Alert> alerts)
        {
            var newtext = "";
            var newrawtext = "";
            _alertGcm = "";
            foreach (var alert in alerts)
            {
                if (alert.Expiry.sec < Utils.DateTimeToUnixTimestamp(DateTime.UtcNow))
                    continue;
                var rewards = CommonHelpers.GetAlertRewards(alert);
                rewards = rewards.Replace("BP", "Blueprint");
                var node = FlatFile.GetPlanetName(alert.MissionInfo.location);
                var region = FlatFile.GetRegion(alert.MissionInfo.location);
                var mission = Names.GetMissionType(alert.MissionInfo.missionType);
                if (alert.MissionInfo.nightmare ||
                    alert.MissionInfo.descText == "/Lotus/Language/Alerts/NightmareAlertDesc")
                    mission = "Nightmare " + mission;
                var faction = Names.GetFaction(alert.MissionInfo.faction);
                var leveltext = "Level: " + alert.MissionInfo.minEnemyLevel + "-" + alert.MissionInfo.maxEnemyLevel;
                var desc = FlatFile.GetString(alert.MissionInfo.descText);
                if (alert.MissionInfo.archwingRequired != null && alert.MissionInfo.archwingRequired)
                {
                    if (alert.MissionInfo.isSharkwing != null && alert.MissionInfo.isSharkwing)
                    {
                        desc += " (Sharkwing)";
                    }
                    else
                    {
                        desc += " (Archwing)";
                    }
                }
                newtext +=
                    string.Format(AlertHtmlFormat, alert.Activation.sec, alert.Expiry.sec,
                        CommonHelpers.GetAlertBadges(rewards), node,
                        region, mission, faction, alert.MissionInfo.minEnemyLevel, alert.MissionInfo.maxEnemyLevel,
                        desc);
                newrawtext += string.Join("|", alert._id.id, node, region,
                    mission, faction, alert.MissionInfo.minEnemyLevel, alert.MissionInfo.maxEnemyLevel,
                    alert.Activation.sec, alert.Expiry.sec, rewards, desc) +
                              "\n";
                _alertGcm += string.Join("|", alert._id.id, node, region,
                    mission, faction, alert.Activation.sec, alert.Expiry.sec, rewards) + "\n";
                if (_earliestExpiry == 0 || _earliestExpiry > alert.Expiry.sec)
                {
                    _earliestExpiry = alert.Expiry.sec;
                }
            }
            if (alerts.ToList().Count == 0)
            {
                newtext = "<li class=\"list-group-item\">No alerts at this time.</li>";
            }
            if (File.ReadAllText(_alertsHtmlPath) != newtext)
                File.WriteAllText(_alertsHtmlPath, newtext);
            if (File.ReadAllText(_alertsRawPath) != newrawtext)
                File.WriteAllText(_alertsRawPath, newrawtext);
        }

        private void RenderNews(IEnumerable<Event> events)
        {
            // TODO: Add support for future events + images
            var newtext = "";
            var newrawtext = "";
            events = events.OrderByDescending(x => x.Date.sec);
            foreach (var news in events)
            {
                var link = news.Prop;
                var msg = news.Msg ??
                          news.Messages[0].Message;
                var publishtime = Utils.UnixTimeStampToDateTime(news.Date.sec);
                var timesincepub = (DateTime.UtcNow - publishtime);
                var timestr = "";
                if (timesincepub.TotalDays > 1)
                    timestr = Math.Floor(timesincepub.TotalDays) + "d";
                else if (timesincepub.TotalHours > 1)
                    timestr = Math.Floor(timesincepub.TotalHours) + "h";
                else
                    timestr = Math.Floor(timesincepub.TotalMinutes) + "m";
                newtext += $"<div>[{timestr}] <a href=\"{link}\">{msg}</a></div>";
                newrawtext += string.Join("|", news._id.id, link, news.Date.sec, msg) + "\n";
            }
            if (File.ReadAllText(_newsHtmlPath) != newtext)
                File.WriteAllText(_newsHtmlPath, newtext);
            if (File.ReadAllText(_newsRawPath) != newrawtext)
                File.WriteAllText(_newsRawPath, newrawtext);
        }

        private void RenderOverrideNodes(IEnumerable<NodeOverride> nodes)
        {
            //File.Delete(OverrideRawPath);
            var ordered =
                nodes.OrderBy(x => FlatFile.GetRegion(x.Node)).ThenBy(x => FlatFile.GetPlanetName(x.Node));
            var newtext = "";
            foreach (var node in ordered)
            {
                try
                {
                    var text =
                        $"{FlatFile.GetPlanetName(node.Node)}|{FlatFile.GetRegion(node.Node)}|{Names.GetFaction(node.Faction)}|{(node.MissionType != null ? Names.GetMissionType(node.MissionType) : FlatFile.GetNodeMission(node.Node))}|{Names.GetEnemySpec(node.EnemySpec)}\n";
                    newtext += text;
                }
                catch (Exception e)
                {
                }
            }
            if (File.ReadAllText(_overrideRawPath) != newtext)
                File.WriteAllText(_overrideRawPath, newtext);
        }

        private void RenderFlashSales(IEnumerable<FlashSale> sales)
        {
            var rawtext = "";
            foreach (var sale in sales)
            {
                var objs = new[]
                {
                    FlatFile.GetNewName(sale.TypeName),
                    sale.Discount.ToString(),
                    sale.PremiumOverride.ToString(),
                    sale.RegularOverride.ToString(),
                    sale.StartDate.sec.ToString(),
                    sale.EndDate.sec.ToString()
                };
                rawtext += string.Join("|", objs) + "\n";
            }
            if (File.ReadAllText(_flashSalesRawPath) != rawtext)
                File.WriteAllText(_flashSalesRawPath, rawtext);
        }

        private void RenderBadlands(Dictionary<string, BadlandNode> badlandsNodes)
        {
            //We could just probably dump the entire JSON into a file and localize it.
            foreach (var pair in badlandsNodes)
            {
                pair.Value.NodeDisplayName = FlatFile.GetPlanetName(pair.Value.Node);
                pair.Value.NodeRegionName = FlatFile.GetRegion(pair.Value.Node);
                pair.Value.NodeGameType = FlatFile.GetNodeMission(pair.Value.Node);
            }
        }

        public void RenderNotifications()
        {
            var json =
                JsonConvert.SerializeObject(new Dictionary<string, string>
                {
                    {"alerts", _alertGcm},
                    {"invasions", _invasionGcm}
                });
            if ((File.Exists(_notificationsJsonPath) && File.ReadAllText(_notificationsJsonPath) != json) ||
                !File.Exists(_notificationsJsonPath))
                File.WriteAllText(_notificationsJsonPath, json);
        }

        public void RenderVoidTraders(IEnumerable<VoidTrader> voidTraders)
        {
            foreach (var voidTrader in voidTraders)
            {
                if (voidTrader.Manifest != null)
                {
                    foreach (var item in voidTrader.Manifest)
                    {
                        item.ItemType = FlatFile.GetNewName(item.ItemType);
                    }
                }
                if (voidTrader.Config != null && voidTrader.Config.Manifests != null)
                {
                    foreach (var manifest in voidTrader.Config.Manifests)
                    {
                        foreach (var item in manifest)
                        {
                            item.ItemType = FlatFile.GetNewName(item.ItemType);
                        }
                    }
                }
                if (voidTrader.Config != null)
                {
                    var newNodeList = voidTrader.Config.Nodes.Select(FlatFile.GetPlanetNameWithRegion).ToList();
                    voidTrader.Config.Nodes = newNodeList;
                }
                voidTrader.Node = FlatFile.GetPlanetNameWithRegion(voidTrader.Node);
            }
            File.WriteAllText(_voidTraderPath, JsonConvert.SerializeObject(voidTraders));
        }

        private void RenderDailyDeals(List<DailyDeal> dailyDeals)
        {
            var copyList = new List<DailyDeal>();
            foreach (var deal in dailyDeals)
            {
                if (deal._id != null)
                {
                    if (!FlatFile.DailyDealDictionary[CurrentPlatform].ContainsKey(deal._id.id))
                    {
                        FlatFile.DailyDealDictionary[CurrentPlatform][deal._id.id] = deal;
                    }
                }
                else
                {
                    if (!FlatFile.DailyDealDictionary[CurrentPlatform].ContainsKey(deal.Activation.sec.ToString()))
                    {
                        FlatFile.DailyDealDictionary[CurrentPlatform][deal.Activation.sec.ToString()] = deal;
                    }
                }
                var copy = JsonConvert.DeserializeObject<DailyDeal>(JsonConvert.SerializeObject(deal));
                copy.StoreItem = FlatFile.GetNewName(copy.StoreItem);
                copyList.Add(copy);
            }
            File.WriteAllText(_dailyDealsPath, JsonConvert.SerializeObject(copyList));
        }

        private void RenderPersistentEnemies(List<dynamic> enemies)
        {
            foreach (dynamic enemy in enemies)
            {
                if (enemy.AgentType != null)
                {
                    enemy.AgentType = FlatFile.GetName((string) enemy.AgentType);
                }
                if (enemy.LocTag != null)
                {
                    enemy.LocTag = FlatFile.GetString((string) enemy.LocTag);
                }
                if (enemy.LastDiscoveredLocation != null)
                {
                    enemy.LastDiscoveredLocation = FlatFile.GetPlanetName((string) enemy.LastDiscoveredLocation);
                }
            }
            File.WriteAllText(_persistentEnemiesPath, JsonConvert.SerializeObject(enemies));
        }

        private void RenderSorties(List<dynamic> sorties)
        {
            //assume there is only going to be (at least) one instance
            if (sorties.Count < 1)
            {
                Log.Warn("No sortie information.");
            }
            else
            {
                var firstSortie = sorties.First();
                if (firstSortie.Variants != null)
                {
                    foreach (dynamic variant in firstSortie.Variants)
                    {
                        if (variant.missionType != null)
                            variant.missionType = Names.GetMissionType((string) variant.missionType);
                        if (variant.node != null)
                            variant.node = FlatFile.GetPlanetNameWithRegion((string) variant.node);
                    }
                }
                File.WriteAllText(_sortiesPath, JsonConvert.SerializeObject(firstSortie));
            }
        }

        private void RenderFissures(List<dynamic> fissures)
        {
            //assume there is only going to be (at least) one instance
            if (fissures.Count < 1)
            {
                Log.Warn("No fissures information.");
            }
            else
            {
                foreach (dynamic fissure in fissures)
                {
                    if (fissure.Node != null)
                    {
                        fissure.Node = $"{FlatFile.GetPlanetNameWithRegion((string) fissure.Node)} ({FlatFile.GetNodeMission((string)fissure.Node)})";
                    }
                }
                File.WriteAllText(_fissuresPath, JsonConvert.SerializeObject(fissures));
            }
        }

        #endregion

        #region Helper functions

        private string RequestWorldState(bool cache = false)
        {
            var request =
                WebRequest.Create(new Uri(_contentUrlDictionary[CurrentPlatform])) as HttpWebRequest;
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.Timeout = 10000;
            request.Method = "GET";
            request.UserAgent = "";
            Log.Info(CurrentPlatform + ": Requesting worldState.php.");
            try
            {
                try
                {
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        using (var reader = new StreamReader(response.GetResponseStream()))
                        {
                            if (cache)
                            {
                                _lastModified = response.LastModified;
                                Log.InfoFormat(CurrentPlatform + ": Last modified: {0}", _lastModified);
                            }
                            return reader.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        Log.Error(ex.ToString());
                        if (ex is WebException)
                        {
                            var webEx = ex as WebException;
                            var exRes = (HttpWebResponse) webEx.Response;
                            switch (exRes.StatusCode)
                            {
                                case HttpStatusCode.BadRequest:
                                    Log.Error(CurrentPlatform + ": 400 response");
                                    Log.Error((new StreamReader(webEx.Response.GetResponseStream())).ReadToEnd());
                                    break;
                                case HttpStatusCode.InternalServerError:
                                    Log.Error(CurrentPlatform + ": 500 response. Invalid params?");
                                    break;
                            }
                        }
                        return null;
                    }
                    catch (Exception ex2)
                    {
                        Log.Error(ex2.ToString());
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }

        private bool HasWorldStateChanged()
        {
            if (_lastModified == DateTime.MinValue)
                return true;
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(new Uri(_contentUrlDictionary[CurrentPlatform]));
                request.IfModifiedSince = _lastModified;
                Log.InfoFormat(CurrentPlatform + ": Last modified for HEAD: {0}", _lastModified);
                request.Method = "HEAD";
                var response = (HttpWebResponse) request.GetResponse();
                return true;
            }
            catch (WebException ex)
            {
                HttpWebResponse response = null;
                Log.Warn(ex.Message);
                try
                {
                    response = (HttpWebResponse) ex.Response;
                    if (response == null)
                    {
                        Log.Warn(ex.Message);
                        Log.Warn(CurrentPlatform + ": The response is null.");
                        return true;
                    }
                    if (response.StatusCode != HttpStatusCode.NotModified)
                        Log.WarnFormat(CurrentPlatform + ": Status code is not notmodified: {0}", response.StatusCode);
                    else
                        return false;
                }
                catch (Exception ex2)
                {
                    Log.Error(ex2);
                    return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return true;
            }
        }

        #endregion
    }
}