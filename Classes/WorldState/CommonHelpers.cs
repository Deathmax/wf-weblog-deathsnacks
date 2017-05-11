using System;
using log4net;
using Warframe_WebLog.Helpers;

namespace Warframe_WebLog.Classes.WorldState
{
    public class InvasionStats
    {
        public int Corpus = 0;
        public int Grineer = 0;
        public int Infestation = 0;
    }

    public class InvasionJson
    {
        public long Activation;
        public int Count;
        public SideInfo DefenderInfo;
        public string Description;
        public string Eta;
        public int Goal;
        public string Id;
        public SideInfo InvaderInfo;
        public string Node;
        public double Percentage;
        public string Region;

        public class SideInfo
        {
            public string AISpec;
            public string Faction;
            public int MaxLevel;
            public int MinLevel;
            public string MissionType;
            public string Reward;
            public bool Winning;
        }
    }

    public static class CommonHelpers
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(CommonHelpers));
        public static string[] GetInvasionBadges(string def, string atk, Invasion invasion)
        {
            var rtnbadges = new[] {"", ""};
            if (invasion.Faction != "FC_INFESTATION")
                rtnbadges[0] = $"<span class=\"badge\" style=\"float:left;\">{atk}</span>";
            rtnbadges[1] = $"<span class=\"badge\" style=\"float:right;\">{def}</span>";
            return rtnbadges;
        }

        public static string GetAlertBadges(string rewards)
        {
            var parts = rewards.Split(new[] {" - "}, StringSplitOptions.None);
            var rtnstr = "";
            foreach (var part in parts)
            {
                var style = "";
                if (!part.EndsWith("cr"))
                    style = " style=\"background-color:blue;\"";
                rtnstr += string.Format("<span class=\"badge\"{1}>{0}</span>", part, style);
            }
            return rtnstr;
        }

        public static string[] GetInvasionRewards(Invasion invasion)
        {
            var atkrewtext = "";
            if (invasion.Faction != "FC_INFESTATION")
            {
                var atkreward = invasion.AttackerReward;
                if (atkreward.countedItems != null && atkreward.countedItems.Count > 0)
                {
                    var item = atkreward.countedItems[0];
                    if (item.ItemCount == 1)
                        atkrewtext = FlatFile.GetName((string)item.ItemType);
                    else
                        atkrewtext = item.ItemCount + " " + FlatFile.GetName((string)item.ItemType);
                }
                else if (atkreward.credits != null)
                    atkrewtext = ((int) atkreward.credits).ToString("n0") + "cr";
            }
            else
            {
                atkrewtext = "0cr";
            }
            var defreward = invasion.DefenderReward;
            var defrewtext = "";
            if (defreward.countedItems != null && defreward.countedItems.Count > 0)
            {
                var item = defreward.countedItems[0];
                if (item.ItemCount == 1)
                    defrewtext = FlatFile.GetName(item.ItemType);
                else
                    defrewtext = item.ItemCount + " " + FlatFile.GetName(item.ItemType);
            }
            else if (defreward.credits != null)
                defrewtext = ((int) defreward.credits).ToString("n0") + "cr";
            return new[] {atkrewtext, defrewtext};
        }

        public static string GetAlertRewards(Alert alert)
        {
            var reward = alert.MissionInfo.missionReward;
            var rtn = reward.credits != null ? reward.credits.ToString("n0") + "cr" : "0cr";
            if (reward.countedItems != null && reward.countedItems.Count > 0)
            {
                for (var i = 0; i < reward.countedItems.Count; i++)
                {
                    string rawtext;
                    var item = reward.countedItems[i];
                    if (item.ItemCount == 1)
                        rawtext = FlatFile.GetName(item.ItemType);
                    else
                        rawtext = item.ItemCount + " " +
                                  FlatFile.GetName(item.ItemType);
                    rtn += " - " + rawtext;
                }
            }
            if (reward.items != null && reward.items.Count > 0)
            {
                for (var i = 0; i < reward.items.Count; i++)
                {
                    var item = reward.items[i];
                    rtn += " - " + FlatFile.GetName(item);
                }
            }
            return rtn;
        }

        public static string GetAlertRewards(dynamic alert)
        {
            var reward = alert.MissionInfo.missionReward ?? alert.Reward;
            var rtn = reward.credits != null ? reward.credits.ToString("n0") + "cr" : "0cr";
            if (reward.countedItems != null && reward.countedItems.Count > 0)
            {
                for (var i = 0; i < reward.countedItems.Count; i++)
                {
                    string rawtext;
                    var item = reward.countedItems[i];
                    if (item.ItemCount == 1)
                        rawtext = FlatFile.GetName((string) item.ItemType);
                    else
                        rawtext = item.ItemCount + " " +
                                  FlatFile.GetName((string) item.ItemType);
                    rtn += " - " + rawtext;
                }
            }
            if (reward.items != null && reward.items.Count > 0)
            {
                for (var i = 0; i < reward.items.Count; i++)
                {
                    var item = reward.items[i];
                    rtn += " - " + FlatFile.GetName((string) item);
                }
            }
            return rtn;
        }

        public static string GetTacticalAlertDescription(dynamic alert)
        {
            return "Tactical Alert - " + FlatFile.GetString((string) alert.Desc) + " - Conclave: " + alert.MaxConclave;
        }

        public static bool HasTaxChanged(BadlandInfo oldNode, BadlandInfo newNode)
        {
            if (oldNode == null || oldNode.TaxChangeAllowedTime == null)
            {
                Log.Warn("Old node is null.");
                if (newNode != null && newNode.TaxChangeAllowedTime != null)
                    return true;
                return false;
            }
            if (newNode == null)
            {
                Log.Warn("New node is null.");
                return false;
            }
            return oldNode.TaxChangeAllowedTime.sec != newNode.TaxChangeAllowedTime.sec;
        }

        public static bool HasMOTDChanged(BadlandInfo oldNode, BadlandInfo newNode, bool overrideNull = false)
        {
            if (oldNode == null)
            {
                Log.Warn("Old node is null.");
            }
            if (newNode == null)
            {
                Log.Warn("New node is null.");
            }
            if (newNode.MOTDAuthor == null && !overrideNull)
                return false;
            return (oldNode == null && newNode != null) || oldNode.MOTD != newNode.MOTD;
        }

        public static bool HasBattlePayChanged(BadlandInfo oldNode, BadlandInfo newNode)
        {
            if (oldNode == null)
            {
                Log.Warn("Old node is null.");
            }
            if (newNode == null)
            {
                Log.Warn("New node is null.");
            }
            if (oldNode == null && newNode != null && newNode.MissionBattlePay != null)
                return true;
            return newNode.BattlePayReserve != null && oldNode.MissionBattlePay != newNode.MissionBattlePay;
        }

        public static bool HasClanNameChanged(BadlandInfo oldNode, BadlandInfo newNode)
        {
            if (oldNode == null)
            {
                Log.Warn("Old node is null.");
            }
            if (newNode == null)
            {
                Log.Warn("New node is null.");
            }
            return oldNode != null && oldNode.Name != newNode.Name;
        }
    }
}